using System;
using System.IO;
using JabaSurvivol.Core;
using UnityEngine;

namespace JabaSurvivol.Gameplay
{
    /// <summary>
    /// Atomic JSON persistence for persistent progression. The file survives
    /// app restarts and is validated on every load.
    /// </summary>
    public sealed class ProfileStore
    {
        public const string FileName = "profile.json";

        private readonly string _path;

        public ProfileStore(string path = null)
        {
            _path = string.IsNullOrWhiteSpace(path)
                ? Path.Combine(Application.persistentDataPath, FileName)
                : path;
        }

        public string FilePath => _path;

        public static string Serialize(Profile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            profile.Validate();
            return JsonUtility.ToJson(profile, true);
        }

        public void SaveFromJson(string json)
        {
            var profile = JsonUtility.FromJson<Profile>(json);
            if (profile == null)
            {
                throw new InvalidOperationException("The cloud save document is not a valid profile.");
            }

            profile.Validate();
            Save(profile);
        }

        public Profile Load()
        {
            if (!File.Exists(_path))
            {
                return new Profile();
            }

            try
            {
                var json = File.ReadAllText(_path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new Profile();
                }

                var profile = JsonUtility.FromJson<Profile>(json);
                if (profile == null)
                {
                    return new Profile();
                }

                profile.Validate();
                return profile;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Profile load failed: {exception.Message}");
                var recovered = TryLoadBackup();
                BackupCorruptFile();

                if (recovered != null)
                {
                    RestoreMainFromBackup();
                    return recovered;
                }

                return new Profile();
            }
        }

        public void Save(Profile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            profile.Validate();

            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var temporaryPath = _path + ".tmp";
            var backupPath = _path + ".bak";
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(profile, true));

            try
            {
                if (File.Exists(_path))
                {
                    File.Replace(temporaryPath, _path, backupPath);
                }
                else
                {
                    File.Move(temporaryPath, _path);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Atomic profile save fallback used: {exception.Message}");
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }

                if (File.Exists(_path))
                {
                    File.Move(_path, backupPath);
                }

                try
                {
                    File.Move(temporaryPath, _path);
                }
                catch
                {
                    if (!File.Exists(_path) && File.Exists(backupPath))
                    {
                        File.Move(backupPath, _path);
                    }

                    throw;
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        private Profile TryLoadBackup()
        {
            var backupPath = _path + ".bak";
            if (!File.Exists(backupPath))
            {
                return null;
            }

            try
            {
                var profile = JsonUtility.FromJson<Profile>(File.ReadAllText(backupPath));
                if (profile == null)
                {
                    return null;
                }

                profile.Validate();
                return profile;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Profile backup load failed: {exception.Message}");
                return null;
            }
        }

        private void RestoreMainFromBackup()
        {
            try
            {
                var backupPath = _path + ".bak";
                if (File.Exists(backupPath))
                {
                    File.Copy(backupPath, _path, true);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Profile backup restore failed: {exception.Message}");
            }
        }

        private void BackupCorruptFile()
        {
            try
            {
                if (!File.Exists(_path))
                {
                    return;
                }

                var backupPath = _path + ".corrupt";
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }

                File.Move(_path, backupPath);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Corrupt profile backup failed: {exception.Message}");
            }
        }
    }
}
