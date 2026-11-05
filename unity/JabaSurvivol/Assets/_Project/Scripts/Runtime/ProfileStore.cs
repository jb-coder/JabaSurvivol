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
                Debug.LogWarning($"Profile load failed and was reset: {exception.Message}");
                BackupCorruptFile();
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
                if (File.Exists(_path))
                {
                    File.Delete(_path);
                }

                File.Move(temporaryPath, _path);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
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
