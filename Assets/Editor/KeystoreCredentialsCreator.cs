using System.IO;
using UnityEditor;
using UnityEngine;

namespace TimelessEchoes.Editor
{
    public class KeystoreCredentialsCreator : EditorWindow
    {
        private string keystorePath = "";
        private string aliasName = "";
        private string keystorePass = "";
        private string keyPass = "";

        [MenuItem("Tools/Create Keystore Credentials")]
        public static void ShowWindow()
        {
            GetWindow<KeystoreCredentialsCreator>(true, "Keystore Credentials", true);
        }

        private void OnGUI()
        {
            GUILayout.Label("Create .keystore_credentials", EditorStyles.boldLabel);
            keystorePath = EditorGUILayout.TextField("Keystore Path", keystorePath);
            aliasName = EditorGUILayout.TextField("Key Alias Name", aliasName);
            keystorePass = EditorGUILayout.PasswordField("Keystore Password", keystorePass);
            keyPass = EditorGUILayout.PasswordField("Key Alias Password", keyPass);

            GUILayout.Space(10);
            if (GUILayout.Button("Save"))
            {
                SaveCredentials();
            }
        }

        private void SaveCredentials()
        {
            var root = Path.Combine(Application.dataPath, "..", ".keystore_credentials");
            var lines = new[]
            {
                $"UNITY_KEYSTORE_PATH={keystorePath}",
                $"UNITY_KEY_ALIAS_NAME={aliasName}",
                $"UNITY_KEYSTORE_PASS={keystorePass}",
                $"UNITY_KEY_PASS={keyPass}"
            };
            File.WriteAllLines(root, lines);

#if !UNITY_EDITOR_WIN
            try
            {
                var chmod = new System.Diagnostics.ProcessStartInfo("chmod", $"600 \"{root}\"")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };
                System.Diagnostics.Process.Start(chmod)?.WaitForExit();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to set permissions: {e.Message}");
            }
#endif
            Debug.Log($"Credentials saved to {root}");
            Close();
            AssetDatabase.Refresh();
        }
    }
}
