#if UNITY_INCLUDE_TESTS
using System.Reflection;
using NUnit.Framework;
using TMPro;
using TimelessEchoes.Skills;
using TimelessEchoes.UI;
using UnityEngine;

namespace TimelessEchoes.Tests.UI
{
    public class ResourceTierUpPopupUITests
    {
        [Test]
        public void SkillLevelUpToast_ShowsFullLevelRangeWhenMultipleLevelsGained()
        {
            var popupRoot = new GameObject("PopupRoot");
            var popup = popupRoot.AddComponent<ResourceTierUpPopupUI>();

            var popupObject = new GameObject("PopupObject");
            var tierTextObject = new GameObject("TierText", typeof(RectTransform), typeof(TextMeshProUGUI));
            tierTextObject.transform.SetParent(popupObject.transform);
            var tierText = tierTextObject.GetComponent<TextMeshProUGUI>();

            var popupObjectField = typeof(ResourceTierUpPopupUI).GetField("popupObject", BindingFlags.Instance | BindingFlags.NonPublic);
            var tierTextField = typeof(ResourceTierUpPopupUI).GetField("tierText", BindingFlags.Instance | BindingFlags.NonPublic);
            popupObjectField.SetValue(popup, popupObject);
            tierTextField.SetValue(popup, tierText);
            popupObject.SetActive(false);

            var skill = ScriptableObject.CreateInstance<Skill>();
            skill.skillName = "Farming";

            var onSkillLevelUp = typeof(ResourceTierUpPopupUI).GetMethod("OnSkillLevelUp", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(onSkillLevelUp, "Expected to reflect ResourceTierUpPopupUI.OnSkillLevelUp.");

            for (var level = 4; level <= 9; level++)
                onSkillLevelUp.Invoke(popup, new object[] { skill, level });

            var processMethod = typeof(ResourceTierUpPopupUI).GetMethod("ProcessPendingLevelRange", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(processMethod, "Expected to reflect ResourceTierUpPopupUI.ProcessPendingLevelRange.");
            processMethod.Invoke(popup, new object[] { skill });

            Assert.IsTrue(popupObject.activeSelf, "Popup should activate for a level-up.");
            Assert.AreEqual("Farming Lv 3<sprite=194>9", tierText.text, "Toast should reflect the full level range.");

            Object.DestroyImmediate(skill);
            Object.DestroyImmediate(tierTextObject);
            Object.DestroyImmediate(popupObject);
            Object.DestroyImmediate(popupRoot);
        }
    }
}
#endif
