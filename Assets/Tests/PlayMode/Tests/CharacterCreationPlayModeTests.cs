#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Blindsided;
using Blindsided.SaveData;
using NUnit.Framework;
using TimelessEchoes.Characters;
using TimelessEchoes.Gear;
using TimelessEchoes.Hero;
using TimelessEchoes.Quests;
using TimelessEchoes.Stats;
using TimelessEchoes.Tasks;
using TimelessEchoes.UI;
using TimelessEchoes.UI.Toolkit;
using TimelessEchoes.Upgrades;
using UnityEditor;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
    public sealed partial class FieldsProductionPlayModeTests
    {
        [UnityTest]
        public IEnumerator SwungToolsFollowTheirOwnSkillAndRefreshAtTheSameAnimationFrame()
        {
            var owner = new GameObject("Isolated skill tool visuals"); owner.SetActive(false);
            try
            {
                var catalog = CharacterCatalog.Load();
                oracle.saveData.Character = catalog.NewProfile(); oracle.saveData.Character.Created = true;
                var skills = owner.AddComponent<TimelessEchoes.Skills.SkillController>();
                var settings = new SerializedObject(skills); var list = settings.FindProperty("skills");
                var names = new[] { "Woodcutting", "Mining", "Farming", "Fishing", "Looting", "Combat" };
                var definitions = names.Select(name => AssetDatabase.LoadAssetAtPath<TimelessEchoes.Skills.Skill>("Assets/Scriptables/Skills/" + name + ".asset")).ToArray();
                list.arraySize = definitions.Length;
                for (var i = 0; i < definitions.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
                settings.ApplyModifiedPropertiesWithoutUndo(); owner.SetActive(true); skills.enabled = false;
                SpriteRenderer Actor(string name) {
                    var actor = new GameObject(name); actor.transform.SetParent(owner.transform);
                    var renderer = actor.AddComponent<SpriteRenderer>(); CharacterAppearance.Attach(renderer); return renderer;
                }
                var player = Actor("Player"); var echo = Actor("Echo"); echo.color = new Color(1, 1, 1, .5f);
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Art/Character/BasicCharacter/MiningUp.anim");
                var binding = AnimationUtility.GetObjectReferenceCurveBindings(clip).First(b => b.propertyName == "m_Sprite");
                player.sprite = echo.sprite = (Sprite)AnimationUtility.GetObjectReferenceCurve(clip, binding)[0].value;
                catalog.TrySource(player.sprite, out var frame);
                var levels = new[] { 1, 6, 7, 13, 14, 19, 20, 26, 27, 35, 36, 45, 46, 55, 56, 67, 68, 100 };
                var tiers = new[] { -1, -1, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7 };
                var skillsScreenObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Skills.prefab"), owner.transform);
                var skillsScreen = skillsScreenObject.GetComponent<ToolkitSkillsScreen>();
                Assert.True(skillsScreen.Show());
                var skillsUi = skillsScreenObject.GetComponent<UIDocument>().rootVisualElement;
                for (var i = 0; i < levels.Length; i++) {
                    skills.GetProgress(definitions[1]).Level = levels[i];
                    yield return null; yield return null;
                    var expected = catalog.Find(tiers[i] < 0 ? "Tool/SkillBrown" : "Tool/SkillMetal" + tiers[i]).At(frame);
                    Assert.NotNull(expected);
                    Assert.AreSame(expected, player.transform.Find("Character appearance/Tool").GetComponent<SpriteRenderer>().sprite);
                    AssertRenderedPart(echo, CharacterSlot.Tool, expected);
                    // Reopening must show the matching saved/current level in the actual Skills selector.
                    skillsScreen.Hide(); Assert.True(skillsScreen.Show());
                    skillsUi = skillsScreenObject.GetComponent<UIDocument>().rootVisualElement;
                    var icon = skillsUi.Q<Button>("skill-Mining").Q<Image>(className: "skill-selector-icon");
                    var expectedRow = new[] { 8, 0, 1, 2, 3, 4, 5, 6, 7 }[tiers[i] + 1];
                    Assert.AreEqual(208 - (expectedRow + 1) * 16, icon.sprite.rect.y);
                }
                // Support skill icons use the same authored metal palette without changing animations.
                foreach (var viewport in new[] { new Vector2Int(1170, 2532), new Vector2Int(1920, 1080) })
                {
                    PlayModeWindow.SetCustomRenderingResolution((uint)viewport.x, (uint)viewport.y, "Support skill tiers");
                    foreach (var level in new[] { 1, 7, 14, 20, 27, 36, 46, 56, 68 })
                    {
                        for (var skillIndex = 4; skillIndex < 6; skillIndex++) skills.GetProgress(definitions[skillIndex]).Level = level;
                        skillsScreen.Hide(); Assert.True(skillsScreen.Show());
                        yield return null; yield return null;
                        skillsUi = skillsScreenObject.GetComponent<UIDocument>().rootVisualElement;
                        for (var skillIndex = 4; skillIndex < 6; skillIndex++)
                        {
                            var icon = skillsUi.Q<Button>("skill-" + names[skillIndex]).Q<Image>(className: "skill-selector-icon").sprite;
                            if (level == 1) Assert.AreSame(AssetDatabase.LoadAssetAtPath<ToolkitSkillsDefinition>("Assets/UI/Toolkit/Skills.asset").icons[skillIndex == 4 ? 5 : 0], icon);
                            else
                            {
                                var tierIndex = System.Array.IndexOf(new[] { 7, 14, 20, 27, 36, 46, 56, 68 }, level);
                                Assert.AreEqual(192 - tierIndex * 16, icon.rect.y);
                                Assert.AreEqual(skillIndex == 4 ? 144 : 96, icon.rect.x);
                            }
                        }
                    }
                    CaptureFeedback(viewport.x < viewport.y ? "support-skill-tiers-portrait.png" : "support-skill-tiers-landscape.png");
                    yield return null;
                }
                // Other skills remain brown even though Mining is now at the final tier.
                foreach (var pose in new[] { "Chop_Down", "Watering_Down", "Fishing_Cast_Down" }) {
                    var actionFrame = catalog.Frame(pose, 0);
                    Assert.AreSame(catalog.Find("Tool/SkillBrown").At(actionFrame),
                        CharacterSkillTools.SpriteFor(catalog, oracle.saveData.Character, actionFrame));
                }
                var sword = catalog.NewProfile(); sword.Tool = "Tool/Metal2";
                var swordFrame = catalog.Frame("SwordAttack_1_Down", 0);
                Assert.AreSame(catalog.SpriteFor(sword, CharacterSlot.Tool, swordFrame), CharacterSkillTools.SpriteFor(catalog, sword, swordFrame));
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [UnityTest]
        public IEnumerator CharacterCreatorPrecedesOrinAndPresetsSurviveStartingAnotherSave()
        {
            var owner = new GameObject("Protected character creation"); GameObject intro = null;
            try
            {
                // Exercise the production reset/new-save factory without resetting a real player slot.
                var fresh = (GameData)typeof(Oracle).GetMethod("CreateFreshGameData", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { null });
                oracle.saveData.Character = fresh.Character;
                owner.AddComponent<ResourceManager>(); owner.AddComponent<GameplayStatTracker>().enabled = false;
                owner.AddComponent<QuestManager>().enabled = false;
                owner.AddComponent<TownWindowManager>().enabled = false;
                var gm = owner.AddComponent<TimelessEchoes.GameManager>(); gm.enabled = false;
                intro = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Intro.prefab"));
                yield return new WaitForSecondsRealtime(.2f);
                var creator = intro.GetComponentInChildren<ToolkitCharacterCreator>(); Assert.True(creator.IsOpen);
                var cameraObject = new GameObject("Town camera input during creation"); cameraObject.transform.SetParent(owner.transform);
                var outputCamera = cameraObject.AddComponent<Camera>(); outputCamera.enabled = false;
                var townPan = cameraObject.AddComponent<TimelessEchoes.TownCameraPan>();
                var virtualCamera = cameraObject.GetComponent<CinemachineCamera>();
                virtualCamera.Lens.OrthographicSize = 7;
                var target = new GameObject("Pan target"); target.transform.SetParent(owner.transform); target.transform.position = new Vector3(12, 7, 0);
                var landBounds = owner.AddComponent<BoxCollider2D>(); landBounds.size = new Vector2(100, 100);
                typeof(TimelessEchoes.TownCameraPan).GetField("panTarget", Private).SetValue(townPan, target.transform);
                typeof(TimelessEchoes.TownCameraPan).GetField("landBounds", Private).SetValue(townPan, landBounds);
                typeof(TimelessEchoes.TownCameraPan).GetField("outputCamera", Private).SetValue(townPan, outputCamera);
                townPan.Focus(Vector2.zero); yield return null;
                Assert.AreEqual(new Vector3(12, 7, 0), target.transform.position);
                Assert.AreEqual(7, virtualCamera.Lens.OrthographicSize, "Creator input cannot pan, zoom or refocus the town camera.");
                var ui = GameObject.Find("Character creation panel").GetComponent<UIDocument>().rootVisualElement;
                Assert.IsNull(Object.FindAnyObjectByType<ToolkitMeetingScreen>(), "Create a character before Orin's opening dialogue.");
                Assert.False(gm.BeginAdventure(AssetDatabase.LoadAssetAtPath<TimelessEchoes.MapGeneration.MapGenerationConfig>("Assets/Scriptables/MapSettings/Farmlands.asset")));
                FieldsClick(ui.Q<Button>("character-confirm")); yield return null;
                Assert.True(creator.IsOpen); Assert.False(oracle.saveData.Character.Created);
                var input = ui.Q<TextField>("character-name"); input.value = "Aster";
                FieldsClick(ui.Q<Button>("character-preview-left")); yield return null; yield return null;
                Assert.Less(ui.Q("character-image").resolvedStyle.scale.value.x, 0, "Turning left previews the left-facing artwork.");
                FieldsClick(ui.Q<Button>("character-preview-right")); yield return null; yield return null;
                Assert.Greater(ui.Q("character-image").resolvedStyle.scale.value.x, 0);
                FieldsClick(ui.Q<Button>("character-preview-previous-action"));
                Assert.AreEqual("Combat", ui.Q<Label>("character-preview-pose").text);
                FieldsClick(ui.Q<Button>("character-preview-next-action"));
                Assert.AreEqual("Idle", ui.Q<Label>("character-preview-pose").text);
                FieldsClick(ui.Q<Button>("character-colour-picker-Head")); yield return null;
                Assert.True(ui.Q<Button>("character-colour-Head-Brown").ClassListContains("selected"));
                var hairColours = ui.Q("character-colours-Head").Query<Button>().ToList();
                Assert.AreEqual(12, hairColours.Count);
                Assert.AreEqual("Black", hairColours[0].tooltip);
                Assert.AreEqual("Dark Brown", hairColours[1].tooltip);
                Assert.AreEqual("Blue", hairColours[11].tooltip);
                Assert.That(ui.Q(className: "character-colour-menu").worldBound.xMax,
                    Is.EqualTo(ui.Q<Button>("character-colour-picker-Head").worldBound.xMax).Within(1), "The palette expands left from the dropdown's right edge.");
                Assert.That(hairColours[3].worldBound.y, Is.EqualTo(hairColours[0].worldBound.y).Within(1));
                Assert.Greater(hairColours[4].worldBound.y, hairColours[0].worldBound.y, "Colour choices wrap after four swatches.");
                CaptureFeedback("character-colour-grid-dropdown.png"); yield return null; yield return null;
                FieldsClick(ui.Q<Button>("character-colour-Head-Purple"));
                FieldsClick(ui.Q<Button>("character-next-Head")); yield return null;
                Assert.AreEqual("Purple", ui.Q<Label>("character-colour-Head").text, "New hair colours remain selected when switching styles.");
                FieldsClick(ui.Q<Button>("character-colour-picker-Head")); yield return null;
                FieldsClick(ui.Q<Button>("character-colour-Head-Blue"));
                Assert.AreEqual("Blue", ui.Q<Label>("character-colour-Head").text);
                FieldsClick(ui.Q<Button>("character-previous-Head")); yield return null;
                FieldsClick(ui.Q<Button>("character-colour-picker-Head")); yield return null;
                FieldsClick(ui.Q<Button>("character-colour-Head-Ginger"));
                Assert.IsNull(ui.Q("character-colour-popup"), "The swatch grid closes after selecting a colour.");
                FieldsClick(ui.Q<Button>("character-next-Head"));
                Assert.AreEqual("Hair 2", ui.Q<Label>("character-style-Head").text);
                Assert.AreEqual("Ginger", ui.Q<Label>("character-colour-Head").text, "Changing hairstyle preserves hair colour.");
                FieldsClick(ui.Q<Button>("character-next-Chest"));
                yield return null;
                Assert.AreEqual("Green", ui.Q<Label>("character-colour-Chest").text);
                FieldsClick(ui.Q<Button>("character-colour-picker-Chest")); yield return null;
                FieldsClick(ui.Q<Button>("character-colour-Chest-Pink"));
                Assert.AreEqual("Pink", ui.Q<Label>("character-colour-Chest").text);
                FieldsClick(ui.Q<Button>("character-next-Chest"));
                Assert.AreEqual("Pink", ui.Q<Label>("character-colour-Chest").text);
                FieldsClick(ui.Q<Button>("character-next-Chest"));
                Assert.AreEqual("Royal Shirt", ui.Q<Label>("character-style-Chest").text, "Armour is excluded from appearance styles.");
                Assert.AreEqual("Green", ui.Q<Label>("character-colour-Chest").text, "Royal clothing has no pink variant, so uses the slot's default colour.");
                ui.Q<ScrollView>("character-options-scroll").ScrollTo(ui.Q<Button>("character-colour-picker-Feet"));
                yield return null; yield return null;
                FieldsClick(ui.Q<Button>("character-colour-picker-Feet")); yield return null;
                FieldsClick(ui.Q<Button>("character-colour-Feet-Green"));
                Assert.AreEqual("Green", ui.Q<Label>("character-colour-Feet").text, "Shoes retain their own colour selector.");
                FieldsClick(ui.Q<Button>("character-save-preset-0")); yield return null;
                Assert.True(CharacterPresetStore.TryLoad(0, out var first, out var error), error); Assert.AreEqual("Aster", first.Name);
                input.value = "Briar"; FieldsClick(ui.Q<Button>("character-next-Chest")); FieldsClick(ui.Q<Button>("character-save-preset-1"));
                var armoured = first.Copy(); armoured.Name = "Clover";
                var catalog = CharacterCatalog.Load();
                foreach (var slot in new[] { CharacterSlot.Head, CharacterSlot.Chest, CharacterSlot.Legs })
                    armoured.Set(slot, catalog.parts.First(p => p.slot == slot && p.IsArmour).id);
                Assert.True(CharacterPresetStore.TrySave(2, armoured, out error), error);
                FieldsClick(ui.Q<Button>("character-load-preset-2"));
                Assert.AreEqual("Hair 1", ui.Q<Label>("character-style-Head").text);
                Assert.AreEqual("Farmer Shirt", ui.Q<Label>("character-style-Chest").text);
                Assert.AreEqual("Farmer Pants", ui.Q<Label>("character-style-Legs").text, "Older presets cannot bypass the creator's armour restriction.");
                FieldsClick(ui.Q<Button>("character-save-preset-2"));
                FieldsClick(ui.Q<Button>("character-load-preset-0")); yield return new WaitForSecondsRealtime(.1f);
                Assert.AreEqual("Aster", ui.Q<TextField>("character-name").value);
                Assert.AreEqual("Hair 2", ui.Q<Label>("character-style-Head").text);
                Assert.AreEqual("Ginger", ui.Q<Label>("character-colour-Head").text, "Preset loading refreshes both selectors.");
                ui.Q<ScrollView>("character-options-scroll").scrollOffset = Vector2.zero;
                yield return null; yield return null;
                CaptureFeedback("character-creator.png"); yield return null; yield return null;
                foreach (var size in new[] { new Vector2Int(844, 390), new Vector2Int(720, 1280) })
                {
                    PlayModeWindow.SetCustomRenderingResolution((uint)size.x, (uint)size.y, "Character creator layout");
                    yield return null; yield return null;
                    var bounds = ui.Q(className: "character-sheet").worldBound;
                    foreach (var button in new[] { "character-confirm", "character-load-preset-0", "character-save-preset-2" })
                        Assert.True(bounds.Contains(ui.Q<Button>(button).worldBound.center), "Creator control stays inside the sheet: " + button);
                    Assert.True(bounds.Contains(ui.Q<TextField>("character-name").worldBound.center));
                    Assert.Greater(ui.Q<ScrollView>("character-options-scroll").worldBound.height, 40);
                    FieldsClick(ui.Q<Button>("character-colour-picker-Head")); yield return null;
                    var grid = ui.Q("character-colours-Head").worldBound;
                    Assert.True(ui.worldBound.Contains(grid.min)); Assert.True(ui.worldBound.Contains(grid.max));
                    Assert.That(ui.Q<Button>("character-colour-Head-Ginger").worldBound.width, Is.EqualTo(ui.Q<Button>("character-colour-Head-Ginger").worldBound.height).Within(432f / Screen.height + .01f), "Swatches stay square within one rendered pixel of rounding.");
                    if (size.x == 720) { CaptureFeedback("character-colour-grid-dropdown-portrait.png"); yield return null; yield return null; }
                    FieldsClick(ui.Q<Button>("character-colour-Head-Ginger"));
                    CaptureFeedback("character-creator-" + size.x + "x" + size.y + ".png"); yield return null; yield return null;
                }
                FieldsClick(ui.Q<Button>("character-confirm")); yield return new WaitForSecondsRealtime(.2f);
                Assert.False(creator.IsOpen); Assert.True(oracle.saveData.Character.Created);
                townPan.Focus(new Vector2(4, 6)); Assert.AreEqual(new Vector3(4, 6, 0), target.transform.position, "Town camera input resumes after creation.");
                Assert.AreEqual("Aster", oracle.saveData.Character.Name); Assert.AreEqual(first.Head, oracle.saveData.Character.Head);
                Assert.NotNull(Object.FindAnyObjectByType<ToolkitMeetingScreen>(), "Orin begins only after the character is saved.");
                var load = SaveManager.Instance.LoadDetailedAsync("Save" + (oracle.CurrentSlot + 1));
                while (!load.IsCompleted) yield return null;
                Assert.True(load.Result.Succeeded); Assert.AreEqual("Aster", load.Result.Data.Character.Name);
                Assert.AreEqual(first.Head, load.Result.Data.Character.Head);
                Object.DestroyImmediate(intro); intro = null;
                foreach (var meeting in Object.FindObjectsByType<ToolkitMeetingScreen>()) Object.DestroyImmediate(meeting.gameObject);
                oracle.saveData.Character = CharacterProfile.Fresh();
                intro = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Intro.prefab"));
                yield return new WaitForSecondsRealtime(.15f);
                creator = intro.GetComponentInChildren<ToolkitCharacterCreator>();
                ui = GameObject.Find("Character creation panel").GetComponent<UIDocument>().rootVisualElement;
                Assert.True(creator.IsOpen);
                Assert.AreEqual("Clover", ui.Q<Button>("character-load-preset-2").text);
                FieldsClick(ui.Q<Button>("character-load-preset-1")); yield return null;
                Assert.AreEqual("Briar", ui.Q<TextField>("character-name").value);
                Assert.False(oracle.saveData.Character.Created, "Loading a preset previews it; confirmation still belongs to the new save.");
            }
            finally {
                if (intro) Object.DestroyImmediate(intro);
                foreach (var meeting in Object.FindObjectsByType<ToolkitMeetingScreen>()) Object.DestroyImmediate(meeting.gameObject);
                Object.DestroyImmediate(owner);
            }
        }

        [UnityTest]
        public IEnumerator CharacterAppearanceMatchesPlayerAndEchoAcrossPoolingAndProfileChanges()
        {
            var owner = new GameObject("Protected character visuals"); GameObject navigation = null;
            try
            {
                var catalog = CharacterCatalog.Load(); Assert.NotNull(catalog);
                oracle.saveData.Character = catalog.NewProfile(); oracle.saveData.Character.Created = true;
                oracle.saveData.Character.Name = "Aster";
                owner.AddComponent<ResourceManager>(); owner.AddComponent<GameplayStatTracker>().enabled = false;
                CreateGuidancePathfinder(owner); owner.AddComponent<TaskController>().enabled = false;
                var playerObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Caleb.prefab"), owner.transform);
                var echoObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Echo.prefab"), owner.transform);
                var player = playerObject.GetComponent<HeroController>(); var echo = echoObject.GetComponent<EchoController>();
                player.enabled = false; echo.enabled = false;
                player.MovementController.SetActiveState(false, player.Animator); echo.MovementController.SetActiveState(false, echo.Animator);
                playerObject.transform.position = Vector3.zero; echoObject.transform.position = new Vector3(2, 0, 0);
                var controller = player.Animator.runtimeAnimatorController;
                player.Animator.enabled = echo.Animator.enabled = false;
                var source = player.Animator.GetComponent<SpriteRenderer>(); var echoSource = echo.Animator.GetComponent<SpriteRenderer>();
                foreach (var action in new[] { "IdleDown", "RunRight", "Water_Down", "Fishing_Cast_down", "Fishing_Catch_down", "MiningUp", "Attack_Right", "OpenChestDown" })
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Art/Character/BasicCharacter/" + action + ".anim");
                    var binding = AnimationUtility.GetObjectReferenceCurveBindings(clip).First(b => b.propertyName == "m_Sprite");
                    var key = (Sprite)AnimationUtility.GetObjectReferenceCurve(clip, binding)[0].value;
                    source.sprite = echoSource.sprite = key; echoSource.color = new Color(1, 1, 1, .7f);
                    yield return null; yield return null;
                    var playerLayers = source.GetComponent<CharacterAppearance>().GetComponentsInChildren<SpriteRenderer>();
                    var echoLayers = echoSource.GetComponent<CharacterAppearance>().GetComponentsInChildren<SpriteRenderer>();
                    foreach (var slot in new[] { "Body", "Head", "Chest", "Legs", "Feet", "Hands", "Tool" })
                    {
                        var a = playerLayers.Single(s => s.gameObject.name == slot); var b = VisibleAppearanceRenderer(echoSource, slot);
                        AssertRenderedPart(echoSource, (CharacterSlot)System.Enum.Parse(typeof(CharacterSlot), slot), a.sprite);
                        Assert.That(b.color.a, Is.EqualTo(.7f).Within(.001f));
                        if (slot != "Tool") Assert.NotNull(a.sprite, action + " / " + slot);
                    }
                }
                var idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Art/Character/BasicCharacter/IdleDown.anim");
                var idleBinding = AnimationUtility.GetObjectReferenceCurveBindings(idleClip).First(b => b.propertyName == "m_Sprite");
                source.sprite = echoSource.sprite = (Sprite)AnimationUtility.GetObjectReferenceCurve(idleClip, idleBinding)[0].value;
                yield return VerifyEchoTransparencyPixels(source, echoSource, owner);
                Assert.True(source.forceRenderingOff); Assert.AreSame(controller, player.Animator.runtimeAnimatorController);
                var gm = owner.AddComponent<TimelessEchoes.GameManager>(); gm.enabled = false;
                typeof(TimelessEchoes.GameManager).GetProperty("CurrentMap").SetValue(gm, owner);
                navigation = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Navigation.prefab"));
                yield return null; yield return null;
                var portrait = navigation.GetComponent<UIDocument>().rootVisualElement.Q<Image>("progress-hero");
                Assert.That(portrait.Q("character-image").worldBound.width, Is.EqualTo(portrait.worldBound.width * 2).Within(1), "Padded modular frames retain the original HUD pixel scale.");
                Assert.AreEqual(source.transform.Find("Character appearance/Head").GetComponent<SpriteRenderer>().sprite.texture,
                    portrait.Q<Image>("appearance-Head").sprite.texture);
                playerObject.transform.position += Vector3.right; yield return null;
                Assert.Greater(portrait.resolvedStyle.scale.value.x, 0);
                playerObject.transform.position += Vector3.left; yield return null;
                Assert.Less(portrait.resolvedStyle.scale.value.x, 0, "The route portrait turns left when walking backwards.");
                yield return null; yield return null;
                Assert.Less(portrait.resolvedStyle.scale.value.x, 0, "Stopping preserves the last horizontal facing.");
                typeof(HeroBase).GetProperty("ReaperSpawnedByDistance").SetValue(player, true);
                yield return null; yield return null;
                var reaperPortrait = navigation.GetComponent<UIDocument>().rootVisualElement.Q<Image>("progress-reaper");
                var navigationArt = AssetDatabase.LoadAssetAtPath<ToolkitNavigationDefinition>("Assets/UI/Toolkit/Navigation.asset");
                Assert.Contains(reaperPortrait.sprite, navigationArt.progressReaperReapFrames);
                Assert.Less(reaperPortrait.resolvedStyle.scale.value.x, 0, "Carl's swing faces left into the route.");
                Assert.IsNull(portrait.sprite, "The character remains alive during the swing's wind-up.");
                yield return new WaitForSeconds((navigationArt.progressReapImpactFrame + 1f) / navigationArt.progressFramesPerSecond);
                Assert.NotNull(navigationArt.progressHeroGravestone);
                Assert.AreEqual(navigationArt.progressHeroGravestone, portrait.sprite, "The impact changes the route portrait to a gravestone.");
                Assert.AreEqual(DisplayStyle.None, portrait.Q("character-image").resolvedStyle.display);
                Assert.Less(portrait.parent.IndexOf(portrait), portrait.parent.IndexOf(reaperPortrait), "Carl must draw in front of the gravestone.");
                typeof(HeroBase).GetProperty("ReaperSpawnedByDistance").SetValue(player, false);
                yield return null; yield return null;
                Assert.IsNull(portrait.sprite);
                Assert.AreEqual(DisplayStyle.Flex, portrait.Q("character-image").resolvedStyle.display, "A new run restores the character portrait.");
                var cameraObject = new GameObject("Character visual evidence", typeof(Camera)); cameraObject.transform.SetParent(owner.transform);
                var camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 2;
                camera.depth = 10; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.44f, .55f, .2f);
                camera.transform.position = new Vector3(1, 0, -10);
                CaptureFeedback("character-player-and-echo.png"); yield return null; yield return null;
                var oldHead = source.transform.Find("Character appearance/Head").GetComponent<SpriteRenderer>().sprite;
                echoObject.SetActive(false);
                var replacement = oracle.saveData.Character.Copy();
                replacement.Head = catalog.parts.First(p => p.slot == CharacterSlot.Head && p.id != replacement.Head).id;
                oracle.saveData.Character = replacement;
                echoObject.SetActive(true); echo.enabled = false; echo.Animator.enabled = false;
                yield return null; yield return null;
                var newHead = source.transform.Find("Character appearance/Head").GetComponent<SpriteRenderer>().sprite;
                Assert.AreNotEqual(oldHead, newHead);
                AssertRenderedPart(echoSource, CharacterSlot.Head, newHead);
                source.flipX = echoSource.flipX = true; yield return null;
                Assert.True(VisibleAppearanceRenderer(echoSource, "Chest").flipX);
                var gear = owner.AddComponent<EquipmentController>();
                var rarities = Resources.LoadAll<RaritySO>("").OrderBy(r => r.tierIndex).ToArray();
                var visibility = CharacterEquipmentAppearance.Visibility;
                Assert.False(visibility.Helmet);
                var clothes = oracle.saveData.Character.Copy();
                foreach (var rarity in rarities) {
                    foreach (var slot in new[] { "Helmet", "Chest", "Boots", "Weapon" }) Assert.True(gear.Equip(new GearItem { slot = slot, rarity = rarity }));
                    yield return null; yield return null;
                    var look = CharacterEquipmentAppearance.Resolve(oracle.saveData.Character);
                    Assert.AreEqual(clothes.Head, look.Head, "Helmet is hidden by default.");
                    Assert.AreNotEqual(clothes.Chest, look.Chest); Assert.AreNotEqual(clothes.Legs, look.Legs);
                    foreach (var slot in new[] { CharacterSlot.Chest, CharacterSlot.Legs, CharacterSlot.Feet }) {
                        Assert.NotNull(catalog.SpriteFor(look, slot, 0), rarity.GetName() + " / " + slot);
                        var a = source.transform.Find("Character appearance/" + slot).GetComponent<SpriteRenderer>();
                        AssertRenderedPart(echoSource, slot, a.sprite);
                    }
                    Assert.NotNull(catalog.SpriteFor(look, CharacterSlot.Tool, catalog.Frame("SwordAttack_1_Down", 0)));
                }
                visibility.Helmet = true; yield return null; yield return null;
                Assert.AreNotEqual(newHead, source.transform.Find("Character appearance/Head").GetComponent<SpriteRenderer>().sprite);
                visibility.Chest = false; visibility.Boots = false;
                yield return null; yield return null;
                var hidden = CharacterEquipmentAppearance.Resolve(oracle.saveData.Character);
                Assert.AreEqual(clothes.Chest, hidden.Chest); Assert.AreEqual(clothes.Legs, hidden.Legs); Assert.AreEqual(clothes.Feet, hidden.Feet);
                Assert.NotNull(catalog.SpriteFor(hidden, CharacterSlot.Tool, catalog.Frame("SwordAttack_1_Down", 0)), "Armour visibility must not hide the equipped sword.");
                Assert.NotNull(catalog.SpriteFor(hidden, CharacterSlot.Tool, catalog.Frame("Watering_Down", 0)), "The equipped sword must retain skilling tools.");
                Object.DestroyImmediate(gear);
                oracle.saveData.Character = CharacterProfile.Legacy(); yield return null;
                Assert.False(source.forceRenderingOff); Assert.False(echoSource.forceRenderingOff);
            }
            finally { if (navigation) Object.DestroyImmediate(navigation); Object.DestroyImmediate(owner); }
        }

        [UnityTest]
        public IEnumerator CharacterEquipmentInventoryVisibilityPersistsWithoutChangingStats()
        {
            var owner = new GameObject("Protected equipment preview"); GameObject view = null;
            try {
                var resources = owner.AddComponent<ResourceManager>();
                var equipment = owner.AddComponent<EquipmentController>();
                var catalog = CharacterCatalog.Load();
                oracle.saveData.Character = catalog.NewProfile(); oracle.saveData.Character.Created = true;
                oracle.saveData.Character.Name = "Aster"; oracle.saveData.Character.Head = "Head/Hair_2/Hair_2_Purple";
                var rarity = Resources.LoadAll<RaritySO>("").Single(r => r.tierIndex == 2);
                var health = Resources.LoadAll<StatDefSO>("").First(s => s.heroMapping == HeroStatMapping.MaxHealth);
                foreach (var slot in equipment.Slots) equipment.Equip(new GearItem { slot = slot, rarity = rarity,
                    affixes = new System.Collections.Generic.List<GearAffix> { new() { stat = health, value = 25 } } });
                var definition = AssetDatabase.LoadAssetAtPath<ToolkitResourceInventoryDefinition>("Assets/UI/Toolkit/ResourceInventory.asset");
                foreach (var resource in definition.resources.Take(16)) resources.Add(resource, 12, trackStats: false, eligibleForTierRoll: false);
                view = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/ResourceInventory.prefab"));
                var screen = view.GetComponent<ToolkitResourceInventoryScreen>(); screen.Bounds = new Rect(36, 28, 696, 376);
                Assert.True(screen.Show()); yield return null; yield return null;
                var ui = view.GetComponent<UIDocument>().rootVisualElement;
                var head = ui.Q("inventory-character-preview").Q<Image>("appearance-Head");
                var hair = head.sprite;
                Assert.NotNull(hair); Assert.False(ui.Q<Button>("inventory-visibility-Helmet").ClassListContains("shown"));
                Assert.IsNull(ui.Q<Button>("inventory-visibility-Weapon"), "Sword appearance always follows the equipped weapon.");
                Assert.IsNull(ui.Q("inventory-character-preview").Q<Image>("appearance-Tool").sprite,
                    "The inventory portrait omits the equipped sword while world appearance still uses it.");
                CaptureFeedback("inventory-character-equipment.png"); yield return null; yield return null;
                FieldsClick(ui.Q<Button>("inventory-visibility-Helmet"));
                yield return null; yield return null;
                Assert.AreNotEqual(hair, head.sprite);
                FieldsClick(ui.Q<Button>("inventory-visibility-Chest"));
                yield return null; yield return null;
                Assert.AreEqual(oracle.saveData.Character.Legs, CharacterEquipmentAppearance.Resolve(oracle.saveData.Character).Legs);
                Assert.AreEqual(100, equipment.GetTotalForMapping(HeroStatMapping.MaxHealth), "Visibility changes do not unequip items or change bonuses.");
                var load = SaveManager.Instance.LoadDetailedAsync("Save" + (oracle.CurrentSlot + 1));
                while (!load.IsCompleted) yield return null;
                Assert.True(load.Result.Succeeded); Assert.True(load.Result.Data.EquipmentVisibility.Helmet); Assert.False(load.Result.Data.EquipmentVisibility.Chest);
                oracle.saveData = load.Result.Data;
                Blindsided.EventHandler.LoadData();
                yield return null; yield return null;
                Assert.True(ui.Q<Button>("inventory-visibility-Helmet").ClassListContains("shown")); Assert.False(ui.Q<Button>("inventory-visibility-Chest").ClassListContains("shown"));
                screen.Hide(); Assert.True(screen.Show()); yield return null; yield return null;
                ui = view.GetComponent<UIDocument>().rootVisualElement;
                Assert.True(ui.Q<Button>("inventory-visibility-Helmet").ClassListContains("shown"));
            }
            finally { if (view) Object.DestroyImmediate(view); Object.DestroyImmediate(owner); }
        }

        [Test]
        public void CharacterPresetStorePreservesUnreadableFilesAndRejectsInvalidNames()
        {
            Assert.False(CharacterProfile.TryName(" \n ", out _, out _));
            Assert.False(CharacterProfile.TryName("Name\nBreak", out _, out _));
            Assert.True(CharacterProfile.TryName("  Éowyn  ", out var name, out _)); Assert.AreEqual("Éowyn", name);
            Assert.False(CharacterProfile.TryName(new string('a', 25), out _, out _));
            Directory.CreateDirectory(SaveManager.SharedProfileDirectory);
            var path = Path.Combine(SaveManager.SharedProfileDirectory, "character-presets.json");
            File.WriteAllText(path, "not valid preset data");
            Assert.False(CharacterPresetStore.TrySave(0, CharacterCatalog.Load().NewProfile(), out _));
            Assert.AreEqual("not valid preset data", File.ReadAllText(path));
            Assert.False(CharacterPresetStore.TryLoad(0, out _, out _));
            Assert.True(new GameData().Character.Created, "Legacy deserialization must preserve existing characters without restarting onboarding.");
        }

        private static SpriteRenderer VisibleAppearanceRenderer(SpriteRenderer actor, string slot)
        {
            var composite = actor.transform.Find("Character appearance/Composite")?.GetComponent<SpriteRenderer>();
            return composite && composite.enabled ? composite : actor.transform.Find("Character appearance/" + slot).GetComponent<SpriteRenderer>();
        }

        private static void AssertRenderedPart(SpriteRenderer actor, CharacterSlot slot, Sprite expected)
        {
            var visible = VisibleAppearanceRenderer(actor, slot.ToString());
            if (visible.name != "Composite") { Assert.AreEqual(expected, visible.sprite); return; }
            var properties = new MaterialPropertyBlock(); visible.GetPropertyBlock(properties);
            var rect = properties.GetVector("_LayerRect" + (int)slot);
            if (!expected) { Assert.AreEqual(Vector4.zero, rect); return; }
            Assert.AreEqual(expected.texture, properties.GetTexture("_Layer" + (int)slot));
            Assert.AreEqual(new Vector4(expected.rect.x / expected.texture.width, expected.rect.y / expected.texture.height,
                expected.rect.width / expected.texture.width, expected.rect.height / expected.texture.height), rect);
        }

        // Observe real GPU output at a hair/body overlap, rather than reasserting renderer tint values.
        private static IEnumerator VerifyEchoTransparencyPixels(SpriteRenderer player, SpriteRenderer echo, GameObject owner)
        {
            var cameraObject = new GameObject("Echo transparency evidence", typeof(Camera)); cameraObject.transform.SetParent(owner.transform);
            var camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 2;
            camera.transform.position = new Vector3(1, 0, -10); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.2f, .45f, .7f, 1);
            var target = new RenderTexture(640, 320, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var readback = new Texture2D(640, 320, TextureFormat.RGBA32, false, true);
            var hairPixels = new Texture2D(2, 2); var bodyPixels = new Texture2D(2, 2); var originalPixels = new Texture2D(2, 2);
            var previousTarget = RenderTexture.active; var previousColour = echo.color; var previousFlip = player.flipX;
            try {
                target.Create(); camera.targetTexture = target;
                var catalog = CharacterCatalog.Load(); var profile = CharacterEquipmentAppearance.Resolve(CharacterCreation.Current);
                var frame = catalog.Frame("Idle_Down", 0);
                var hair = catalog.SpriteFor(profile, CharacterSlot.Head, frame); var body = catalog.SpriteFor(profile, CharacterSlot.Body, frame);
                Assert.True(hairPixels.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(hair.texture))));
                Assert.True(bodyPixels.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(body.texture))));
                Assert.True(originalPixels.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(player.sprite.texture))));
                float FootY(Sprite sprite, Texture2D pixels) {
                    for (var y = 0; y < sprite.rect.height; y++)
                        for (var x = 0; x < sprite.rect.width; x++)
                            if (pixels.GetPixel((int)sprite.rect.x + x, (int)sprite.rect.y + y).a > .99f)
                                return (y - sprite.pivot.y) / sprite.pixelsPerUnit;
                    Assert.Fail("Character artwork must contain visible pixels."); return 0;
                }
                var originalFeet = player.transform.TransformPoint(new Vector3(0, FootY(player.sprite, originalPixels)));
                foreach (var actor in new[] { player, echo }) {
                    var bodyLayer = actor.transform.Find("Character appearance");
                    var modularFeet = bodyLayer.TransformPoint(new Vector3(0, FootY(body, bodyPixels)));
                    Assert.That(modularFeet.y - actor.transform.position.y,
                        Is.EqualTo(originalFeet.y - player.transform.position.y).Within(.001f),
                        "Modular feet must preserve the original ground anchor used by task targets and Echo UI.");
                }
                Vector2? overlap = null;
                for (var y = 32; y < hair.rect.height && overlap == null; y++)
                    for (var x = 0; x < hair.rect.width; x++) {
                        var h = hairPixels.GetPixel((int)hair.rect.x + x, (int)hair.rect.y + y);
                        var b = bodyPixels.GetPixel((int)body.rect.x + x, (int)body.rect.y + y);
                        if (h.a > .99f && b.a > .99f && h.r > .2f && Mathf.Abs(h.r - b.r) > .1f) {
                            overlap = (new Vector2(x + .5f, y + .5f) - hair.pivot) / hair.pixelsPerUnit; break;
                        }
                    }
                Assert.True(overlap.HasValue, "The authored hairstyle must cover part of the head for this regression.");
                foreach (var flipped in new[] { false, true }) foreach (var alpha in new[] { .7f, .35f }) {
                    player.flipX = echo.flipX = flipped; echo.color = new Color(1, 1, 1, alpha);
                    yield return null; yield return null;
                    RenderTexture.active = target; readback.ReadPixels(new Rect(0, 0, 640, 320), 0, 0); readback.Apply(); RenderTexture.active = previousTarget;
                    var local = new Vector3(overlap.Value.x * (flipped ? -1 : 1), overlap.Value.y);
                    Color Sample(SpriteRenderer actor) {
                        var point = camera.WorldToViewportPoint(actor.transform.Find("Character appearance").TransformPoint(local));
                        return readback.GetPixel(Mathf.FloorToInt(point.x * 640), Mathf.FloorToInt(point.y * 320));
                    }
                    var expected = Color.Lerp(readback.GetPixel(0, 0), Sample(player), alpha);
                    var actual = Sample(echo);
                    Assert.That(actual.r, Is.EqualTo(expected.r).Within(.015f), "Echo hair must fade once, without the head bleeding through.");
                    Assert.That(actual.g, Is.EqualTo(expected.g).Within(.015f));
                    Assert.That(actual.b, Is.EqualTo(expected.b).Within(.015f));
                }
            }
            finally {
                echo.color = previousColour; player.flipX = echo.flipX = previousFlip;
                camera.targetTexture = null; RenderTexture.active = previousTarget;
                Object.DestroyImmediate(cameraObject); target.Release(); Object.DestroyImmediate(target);
                Object.DestroyImmediate(readback); Object.DestroyImmediate(hairPixels); Object.DestroyImmediate(bodyPixels); Object.DestroyImmediate(originalPixels);
            }
        }
    }
}
#endif
