#if UNITY_EDITOR
using System;
using UnityEditor;

namespace TimelessEchoes.Editor
{
    [InitializeOnLoad]
    public static class KeystoreCredentialsLoader
    {
        static KeystoreCredentialsLoader()
        {
            Apply();
        }

        [MenuItem("Tools/Load Keystore Credentials")]
        public static void Apply()
        {
            var keystorePass = Environment.GetEnvironmentVariable("UNITY_KEYSTORE_PASS");
            if (!string.IsNullOrEmpty(keystorePass))
            {
                PlayerSettings.Android.keystorePass = keystorePass;
            }

            var keyPass = Environment.GetEnvironmentVariable("UNITY_KEY_PASS");
            if (!string.IsNullOrEmpty(keyPass))
            {
                PlayerSettings.Android.keyaliasPass = keyPass;
            }

            var keystorePath = Environment.GetEnvironmentVariable("UNITY_KEYSTORE_PATH");
            if (!string.IsNullOrEmpty(keystorePath))
            {
                PlayerSettings.Android.keystoreName = keystorePath;
            }

            var aliasName = Environment.GetEnvironmentVariable("UNITY_KEY_ALIAS_NAME");
            if (!string.IsNullOrEmpty(aliasName))
            {
                PlayerSettings.Android.keyaliasName = aliasName;
            }
        }
    }
}
#endif
