using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using TimelessEchoes.UI;
using TimelessEchoes.UI.Toolkit;
using References.UI;
using Image = UnityEngine.UI.Image;
namespace TimelessEchoes.EditorTools
{
    public static class ToolkitCauldronMigration
    {
        [MenuItem("Tools/UI Toolkit/Import Cauldron reference")]
        public static void Import()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Main Edit mode required");
            const string path = "Assets/UI/Toolkit/";
            var ui = UnityEngine.Object.FindAnyObjectByType<CauldronWindowUI>(FindObjectsInactive.Include);
            var so = new SerializedObject(ui);
            T Ref<T>(string field) where T : UnityEngine.Object => (T)so.FindProperty(field).objectReferenceValue;
            var window = ((GameObject)new SerializedObject(UnityEngine.Object.FindAnyObjectByType<TownWindowManager>(FindObjectsInactive.Include)).FindProperty("cauldron").FindPropertyRelative("window").objectReferenceValue).transform;
            Sprite SpriteAt(string relative) => window.Find("Image/" + relative).GetComponent<Image>().sprite;
            var d = AssetDatabase.LoadAssetAtPath<ToolkitCauldronDefinition>(path + "Cauldron.asset");
            if (!d) { d = ScriptableObject.CreateInstance<ToolkitCauldronDefinition>(); AssetDatabase.CreateAsset(d, path + "Cauldron.asset"); }
            var slot = (CauldronMixItemUIReferences)so.FindProperty("mixSlots").GetArrayElementAtIndex(0).objectReferenceValue;
            d.config = Ref<TimelessEchoes.Upgrades.CauldronConfig>("config");
            d.frame = SpriteAt("Mock/Stockpile"); d.slot = slot.GetComponent<Image>().sprite;
            d.inset = SpriteAt("CollectionRight/Image/Image (1)");
            d.darkInset = SpriteAt("Mock/StockpileControls/DrinkMiddle/Hori (1)/Vert/Image");
            d.button = Ref<UnityEngine.UI.Button>("mixButton").GetComponent<Image>().sprite;
            d.plus = window.GetComponentsInChildren<Image>(true).First(i => i.sprite && i.sprite.name == "UI_Icons_17").sprite;
            d.arrowGreen = Ref<Sprite>("mixArrowGreenSprite"); d.arrowRed = Ref<Sprite>("mixArrowRedSprite");
            d.selectionGreen = slot.selectionImageGreen.sprite; d.selectionWhite = slot.selectionImageWhite.sprite;
            d.xpFill = Ref<Blindsided.Utilities.SlicedFilledImage>("evaXpBar").sprite;
            d.xpColor = Ref<Blindsided.Utilities.SlicedFilledImage>("evaXpBar").color;
            d.xpTrack = SpriteAt("Mock/Stockpile/Detail/Experience Bar");
            d.countColor = slot.countText.color;
            d.hover = window.GetComponentsInChildren<Image>(true).First(i => i.sprite && i.sprite.name == "UI_Icons_12").sprite;
            var collection = new SerializedObject(UnityEngine.Object.FindAnyObjectByType<CollectionsWindowUI>(FindObjectsInactive.Include));
            var card = (CollectionItemUIReferences)collection.FindProperty("itemPrefab").objectReferenceValue;
            d.collectionFill = card.tierFillImage.sprite; d.collectionFillColor = card.tierFillImage.color;
            d.collectionCountColor = card.countText.color; d.disabledColor = Ref<UnityEngine.UI.Button>("mixButton").colors.disabledColor;
            d.portrait = Animation(window.Find("Image/Mock/Stockpile/Image/Image"));
            d.pot = Animation(window.Find("Image/Mock/StockpileControls/MixL/Hori/Vert/Horizontal/Cauldronart"));
            d.tierBackgrounds = Enumerable.Range(0, so.FindProperty("tierSprites").arraySize).Select(i => (Sprite)so.FindProperty("tierSprites").GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
            d.tierBorders = Enumerable.Range(0, so.FindProperty("borderTierSprites").arraySize).Select(i => (Sprite)so.FindProperty("borderTierSprites").GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
            var texts = window.GetComponentsInChildren<TMPro.TMP_Text>(true);
            d.mixingHelp = texts.First(t => t.text.StartsWith("Pick two resources")).text;
            d.rewardHelp = texts.First(t => t.text.StartsWith("Low Card")).text;
            var drinking = Ref<CauldronDrinkingUIReferences>("drinking");
            d.showTaste = drinking.tasteButton.gameObject.activeSelf; d.showStop = drinking.stopButton.gameObject.activeSelf;
            EditorUtility.SetDirty(d);
            var go = new GameObject("Toolkit Cauldron");
            try
            {
                var screen = go.AddComponent<ToolkitCauldronScreen>(); var native = new SerializedObject(screen);
                native.FindProperty("definition").objectReferenceValue = d;
                native.FindProperty("theme").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ToolkitTheme>(path + "Theme.asset");
                native.FindProperty("runtimeTheme").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(path + "Runtime.tss");
                native.FindProperty("textSettings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PanelTextSettings>(path + "TextSettings.asset");
                native.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(go, path + "Cauldron.prefab"); AssetDatabase.SaveAssets();
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        private static ToolkitCauldronDefinition.SpriteAnimation Animation(Transform transform)
        {
            var result = new ToolkitCauldronDefinition.SpriteAnimation();
            var animator = transform.GetComponent<Animator>();
            var clip = animator.runtimeAnimatorController.animationClips.First();
            var binding = AnimationUtility.GetObjectReferenceCurveBindings(clip).First(b => b.propertyName == "m_Sprite");
            var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            result.frames = keys.Select(k => (Sprite)k.value).ToArray(); result.times = keys.Select(k => k.time).ToArray(); result.duration = clip.length;
            return result;
        }
    }
}
