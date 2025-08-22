using System;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

namespace Blindsided.SaveData
{
    public static class SaveSecretManager
    {
        private const string SecretFileName = ".hmac.secret";
        private static string rootPathOverride;

        public static byte[] GetOrCreateSecret()
        {
            try
            {
                var path = GetSecretFilePath();
                if (File.Exists(path))
                {
                    try
                    {
                        var b64 = File.ReadAllText(path);
                        return Convert.FromBase64String(b64);
                    }
                    catch { }
                }

                var secret = new byte[32];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(secret);
                }

                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);
                    File.WriteAllText(path, Convert.ToBase64String(secret));
                }
                catch { }

                return secret;
            }
            catch
            {
                var fallback = new byte[32];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(fallback);
                }
                return fallback;
            }
        }

        public static void SetRootPathForTests(string path)
        {
            rootPathOverride = path;
        }

        private static string GetSecretFilePath()
        {
            var root = string.IsNullOrEmpty(rootPathOverride) ? Application.persistentDataPath : rootPathOverride;
            var savesDir = Path.Combine(root, "Saves");
            return Path.Combine(savesDir, SecretFileName);
        }
    }
}


