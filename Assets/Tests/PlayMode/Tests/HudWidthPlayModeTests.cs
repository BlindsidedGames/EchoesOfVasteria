#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using System.IO;
using Blindsided.SaveData;
using NUnit.Framework;
using TimelessEchoes;
using TimelessEchoes.Buffs;
using TimelessEchoes.UI.Toolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Tests.PlayMode
{
    public sealed partial class FieldsProductionPlayModeTests
    {
        [UnityTest]
        public IEnumerator HealthPanelKeepsItsWidthWhenAbilitiesUnlockAcrossOrientations()
        {
            var owner = new GameObject("HUD width fixture");
            GameObject view = null;
            try
            {
                var gm = owner.AddComponent<GameManager>(); gm.enabled = false;
                typeof(GameManager).GetProperty("CurrentMap").SetValue(gm, owner);
                typeof(GameManager).GetProperty("IsInTown").SetValue(gm, false);
                var buffs = owner.AddComponent<BuffManager>(); buffs.enabled = false;
                view = CreateGuidanceRunView();
                var ui = view.GetComponent<UIDocument>().rootVisualElement;
                var health = ui.Q("run-health-strip");
                var abilities = ui.Q("run-abilities");
                var evidence = Path.GetFullPath("Artifacts/ScreenshotStudio/validation"); Directory.CreateDirectory(evidence);
                foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1206, 2622), new Vector2Int(1920, 1080) })
                {
                    PlayModeWindow.SetCustomRenderingResolution((uint)size.x, (uint)size.y, "HUD width check");
                    oracle.saveData.Quests.Clear();
                    yield return new WaitForSecondsRealtime(.2f);
                    Assert.False(buffs.HasUnlockedAbilities, "The locked case must actually hide the ability row.");
                    Assert.AreEqual(DisplayStyle.None, abilities.resolvedStyle.display);
                    var lockedWidth = health.worldBound.width;
                    Assert.Greater(lockedWidth, 0);
                    ScreenCapture.CaptureScreenshot(Path.Combine(evidence, $"health-locked-{size.x}.png"));
                    yield return null;
                    foreach (var recipe in BuffRecipe.LoadAvailable())
                        if (recipe.requiredQuest) oracle.saveData.Quests[recipe.requiredQuest.questId] = new GameData.QuestRecord { Completed = true };
                    yield return new WaitForSecondsRealtime(.2f);
                    Assert.True(buffs.HasUnlockedAbilities);
                    Assert.AreEqual(DisplayStyle.Flex, abilities.resolvedStyle.display);
                    Assert.That(health.worldBound.width, Is.EqualTo(lockedWidth).Within(.5f), "Unlocking buffs must not resize health.");
                    if (size.x > size.y)
                        Assert.That(health.worldBound.xMax, Is.EqualTo(ui.Q("run-buff-4").worldBound.xMax).Within(.5f), "Health and the fifth ability must end together.");
                    ScreenCapture.CaptureScreenshot(Path.Combine(evidence, $"health-unlocked-{size.x}.png"));
                    yield return null;
                }
            }
            finally { if (view) Object.DestroyImmediate(view); Object.DestroyImmediate(owner); }
        }
    }
}
#endif
