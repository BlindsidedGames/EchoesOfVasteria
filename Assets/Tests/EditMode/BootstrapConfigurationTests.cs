#if UNITY_INCLUDE_TESTS
using System.Linq;
using NUnit.Framework;

namespace Tests.EditMode
{
    public class BootstrapConfigurationTests
    {
        [Test]
        public void EditorPlayAndBuildBootstrapThroughLoadingScene()
        {
            const string loadingScenePath = "Assets/Scenes/Loading.unity";
            Assert.AreEqual(
                loadingScenePath,
                UnityEditor.AssetDatabase.GetAssetPath(
                    UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene),
                "Editor Play must use the same verified-save entry scene as a player build.");

            var firstEnabledScene = UnityEditor.EditorBuildSettings.scenes
                .FirstOrDefault(scene => scene.enabled);
            Assert.IsNotNull(firstEnabledScene);
            Assert.AreEqual(loadingScenePath, firstEnabledScene.path);
        }
    }
}
#endif
