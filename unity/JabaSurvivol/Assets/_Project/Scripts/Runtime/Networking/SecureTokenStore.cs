using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace JabaSurvivol.Gameplay
{
    public interface ISecureTokenStore
    {
        string Load();
        void Save(string token);
        void Clear();
    }

    public sealed class InMemorySecureTokenStore : ISecureTokenStore
    {
        private string _token;

        public string Load() => _token;

        public void Save(string token) => _token = token;

        public void Clear() => _token = null;
    }

    public static class SecureTokenStore
    {
        public static ISecureTokenStore CreateDefault(string path = null)
        {
            var filePath = string.IsNullOrWhiteSpace(path)
                ? Path.Combine(Application.persistentDataPath, "session.bin")
                : path;

#if UNITY_ANDROID && !UNITY_EDITOR
            return new AndroidKeystoreTokenStore(filePath);
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return new WindowsDpapiTokenStore(filePath);
#else
            return new InMemorySecureTokenStore();
#endif
        }
    }

    public abstract class FileSecureTokenStore : ISecureTokenStore
    {
        private readonly string _path;

        protected FileSecureTokenStore(string path)
        {
            _path = path;
        }

        public string Load()
        {
            if (!File.Exists(_path))
            {
                return null;
            }

            try
            {
                var encoded = File.ReadAllText(_path);
                if (string.IsNullOrWhiteSpace(encoded))
                {
                    return null;
                }

                var encrypted = Convert.FromBase64String(encoded);
                return Encoding.UTF8.GetString(Decrypt(encrypted));
            }
            catch (Exception)
            {
                Clear();
                return null;
            }
        }

        public void Save(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                Clear();
                return;
            }

            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var protectedBytes = Encrypt(Encoding.UTF8.GetBytes(token));
            var temporaryPath = _path + ".tmp";
            File.WriteAllText(temporaryPath, Convert.ToBase64String(protectedBytes));
            if (File.Exists(_path))
            {
                File.Replace(temporaryPath, _path, null);
            }
            else
            {
                File.Move(temporaryPath, _path);
            }
        }

        public void Clear()
        {
            try
            {
                if (File.Exists(_path))
                {
                    File.Delete(_path);
                }
            }
            catch (Exception)
            {
                // A stale token file is retried on the next load.
            }
        }

        protected abstract byte[] Encrypt(byte[] plainText);
        protected abstract byte[] Decrypt(byte[] cipherText);
    }

    public sealed class WindowsDpapiTokenStore : FileSecureTokenStore
    {
        private const int CryptProtectUiForbidden = 0x1;

        public WindowsDpapiTokenStore(string path) : base(path)
        {
        }

        protected override byte[] Encrypt(byte[] plainText)
        {
            return Transform(plainText, protect: true);
        }

        protected override byte[] Decrypt(byte[] cipherText)
        {
            return Transform(cipherText, protect: false);
        }

        private static byte[] Transform(byte[] input, bool protect)
        {
            var inputPointer = Marshal.AllocHGlobal(input.Length);
            try
            {
                Marshal.Copy(input, 0, inputPointer, input.Length);
                var inputBlob = new DataBlob { cbData = input.Length, pbData = inputPointer };
                DataBlob outputBlob;

                var success = protect
                    ? CryptProtectData(ref inputBlob, "JabaSurvivol refresh token", IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, CryptProtectUiForbidden, out outputBlob)
                    : CryptUnprotectData(ref inputBlob, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, CryptProtectUiForbidden, out outputBlob);

                if (!success)
                {
                    throw new CryptographicException(Marshal.GetLastWin32Error());
                }

                try
                {
                    var output = new byte[outputBlob.cbData];
                    Marshal.Copy(outputBlob.pbData, output, 0, outputBlob.cbData);
                    return output;
                }
                finally
                {
                    LocalFree(outputBlob.pbData);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(inputPointer);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DataBlob
        {
            public int cbData;
            public IntPtr pbData;
        }

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CryptProtectData(
            ref DataBlob dataIn,
            string description,
            IntPtr optionalEntropy,
            IntPtr reserved,
            IntPtr prompt,
            int flags,
            out DataBlob dataOut);

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CryptUnprotectData(
            ref DataBlob dataIn,
            IntPtr description,
            IntPtr optionalEntropy,
            IntPtr reserved,
            IntPtr prompt,
            int flags,
            out DataBlob dataOut);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LocalFree(IntPtr memory);
    }

    public sealed class AndroidKeystoreTokenStore : FileSecureTokenStore
    {
        private const string KeyAlias = "jaba_survivol_refresh_token";

        public AndroidKeystoreTokenStore(string path) : base(path)
        {
        }
        private const int CipherEncryptMode = 1;
        private const int CipherDecryptMode = 2;
        private const int GcmTagBits = 128;
        private const int IvLength = 12;

        protected override byte[] Encrypt(byte[] plainText)
        {
            using var cipher = new AndroidJavaObject("javax.crypto.Cipher", "AES/GCM/NoPadding");
            using var key = GetOrCreateKey();
            cipher.Call("init", CipherEncryptMode, key);
            var iv = cipher.Call<byte[]>("getIV");
            var encrypted = cipher.Call<byte[]>("doFinal", plainText);

            var result = new byte[iv.Length + encrypted.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(encrypted, 0, result, iv.Length, encrypted.Length);
            return result;
        }

        protected override byte[] Decrypt(byte[] cipherText)
        {
            if (cipherText.Length <= IvLength)
            {
                throw new CryptographicException("The stored token is invalid.");
            }

            var iv = new byte[IvLength];
            var encrypted = new byte[cipherText.Length - IvLength];
            Buffer.BlockCopy(cipherText, 0, iv, 0, IvLength);
            Buffer.BlockCopy(cipherText, IvLength, encrypted, 0, encrypted.Length);

            using var cipher = new AndroidJavaObject("javax.crypto.Cipher", "AES/GCM/NoPadding");
            using var key = GetOrCreateKey();
            using var spec = new AndroidJavaObject("javax.crypto.spec.GCMParameterSpec", GcmTagBits, iv);
            cipher.Call("init", CipherDecryptMode, key, spec);
            return cipher.Call<byte[]>("doFinal", encrypted);
        }

        private static AndroidJavaObject GetOrCreateKey()
        {
            using var keyStoreClass = new AndroidJavaClass("java.security.KeyStore");
            using var keyStore = keyStoreClass.CallStatic<AndroidJavaObject>("getInstance", "AndroidKeyStore");
            keyStore.Call("load", (AndroidJavaObject)null);

            if (!keyStore.Call<bool>("containsAlias", KeyAlias))
            {
                using var builder = new AndroidJavaObject(
                    "android.security.keystore.KeyGenParameterSpec$Builder",
                    KeyAlias,
                    3);
                builder.Call<AndroidJavaObject>("setBlockModes", "GCM");
                builder.Call<AndroidJavaObject>("setEncryptionPaddings", "NoPadding");
                builder.Call<AndroidJavaObject>("setKeySize", 256);
                using var spec = builder.Call<AndroidJavaObject>("build");
                using var generator = new AndroidJavaObject("javax.crypto.KeyGenerator", "AES");
                generator.Call("init", spec);
                generator.Call<AndroidJavaObject>("generateKey");
            }

            return keyStore.Call<AndroidJavaObject>("getKey", KeyAlias, (AndroidJavaObject)null);
        }
    }
}
