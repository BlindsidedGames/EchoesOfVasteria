#if UNITY_INCLUDE_TESTS
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    // Let Unity's runner own its empty scene; normal Play keeps the Loading bootstrap.
    public class IsolatedPlayModeScene : IPrebuildSetup, IPostBuildCleanup
    {
        private const string SavedSceneKey = "Echoes.Tests.PreviousPlayModeStartScene";

        public void Setup()
        {
#if UNITY_EDITOR
            SessionState.SetString(SavedSceneKey,
                AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            EditorSceneManager.playModeStartScene = null;
#endif
        }

        public void Cleanup()
        {
#if UNITY_EDITOR
            var path = SessionState.GetString(SavedSceneKey, "");
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(path)
                ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            SessionState.EraseString(SavedSceneKey);
#endif
        }
    }
}
#endif
