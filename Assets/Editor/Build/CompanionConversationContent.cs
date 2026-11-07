using System.Collections.Generic;
using System.IO;
using System.Linq;
using TimelessEchoes.Conversations;
using TimelessEchoes.Farming;
using TimelessEchoes.NPC;
using TimelessEchoes.Quests;
using TimelessEchoes.Tasks;
using TimelessEchoes.UI.Toolkit;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace TimelessEchoes.EditorTools
{
    public static class CompanionConversationContent
    {
        [MenuItem("Tools/Timeless Echoes/Rebuild companion and conversation content")]
        public static void Build()
        {
            BuildDogs();
            BuildConversations();
            AssetDatabase.SaveAssets();
        }

        private static void BuildDogs()
        {
            const string directory = "Assets/Art/Dogs/LilDoggies";
            var baseController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Art/Cat/Mildred/Cat_Commission.controller");
            var palette = AssetDatabase.LoadAssetAtPath<MildredColours>("Assets/Resources/MildredColours/Colours.asset");
            palette.dogs = new MildredColours.Dog[5];
            for (var dog = 1; dog <= 5; dog++)
            {
                var idle = ImportFrames($"{directory}/lildoggie{dog:00}_idle_blink_strip8.png", 8);
                var run = ImportFrames($"{directory}/lildoggie{dog:00}_run_strip4.png", 4);
                var idleClip = Clip($"{directory}/Dog{dog}Idle.anim", idle);
                var runClip = Clip($"{directory}/Dog{dog}Run.anim", run);
                var path = $"{directory}/Dog{dog}.overrideController";
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
                if (!controller)
                {
                    controller = new AnimatorOverrideController(baseController);
                    AssetDatabase.CreateAsset(controller, path);
                }
                var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                controller.GetOverrides(pairs);
                for (var i = 0; i < pairs.Count; i++)
                    pairs[i] = new KeyValuePair<AnimationClip, AnimationClip>(pairs[i].Key,
                        pairs[i].Key.name.ToLowerInvariant().Contains("idle") ? idleClip : runClip);
                controller.ApplyOverrides(pairs);
                EditorUtility.SetDirty(controller);
                palette.dogs[dog - 1] = new MildredColours.Dog { idle = idle, controller = controller };
            }
            EditorUtility.SetDirty(palette);
        }

        private static Sprite[] ImportFrames(string path, int count)
        {
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            var frames = new SpriteMetaData[count];
            for (var i = 0; i < count; i++)
                frames[i] = new SpriteMetaData { name = $"{Path.GetFileNameWithoutExtension(path)}_{i:00}",
                    rect = new Rect(i * 32, 0, 32, 32), alignment = (int)SpriteAlignment.Center, pivot = new Vector2(.5f, .5f) };
#pragma warning disable CS0618
            importer.spritesheet = frames;
#pragma warning restore CS0618
            importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        }

        private static AnimationClip Clip(string path, Sprite[] frames)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (!clip) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
            clip.frameRate = 12;
            var keys = frames.Select((s, i) => new ObjectReferenceKeyframe { time = i / 12f, value = s }).ToList();
            keys.Add(new ObjectReferenceKeyframe { time = frames.Length / 12f, value = frames[0] });
            AnimationUtility.SetObjectReferenceCurve(clip,
                EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys.ToArray());
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static void BuildConversations()
        {
            var entries = new List<ConversationCatalog.Entry>();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Tasks/NPC" }))
            {
                var task = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)).GetComponent<TalkToNpcTask>();
                if (!task || task.Lines.Count == 0) continue;
                entries.Add(new ConversationCatalog.Entry { id = "meeting:" + task.NpcId, npcId = task.NpcId,
                    titleKey = "conversations.first-meeting", title = "First meeting", task = task });
            }
            foreach (var introduction in Resources.LoadAll<ToolkitNpcIntroduction>("NPC Introductions"))
                entries.Add(new ConversationCatalog.Entry { id = "introduction:" + introduction.npcId, npcId = introduction.npcId,
                    titleKey = "conversations.intro." + introduction.npcId, title = introduction.name, introduction = introduction });
            var story = Resources.Load<FarmStoryDefinition>("Farming/FarmStory");
            var quests = Resources.LoadAll<QuestData>("Quests");
            foreach (var conversation in story.conversations)
                entries.Add(new ConversationCatalog.Entry { id = "story:" + conversation.id, npcId = conversation.npcId,
                    titleKey = "conversations.town-fields", title = "Our first field", story = story, storyId = conversation.id,
                    quest = conversation.openingQuest ? conversation.openingQuest : quests.FirstOrDefault(q => q.questId == conversation.id) });
            const string directory = "Assets/Resources/Conversations";
            if (!AssetDatabase.IsValidFolder(directory)) AssetDatabase.CreateFolder("Assets/Resources", "Conversations");
            const string path = directory + "/Catalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<ConversationCatalog>(path);
            if (!catalog) { catalog = ScriptableObject.CreateInstance<ConversationCatalog>(); AssetDatabase.CreateAsset(catalog, path); }
            catalog.entries = entries.ToArray();
            EditorUtility.SetDirty(catalog);
        }
    }
}
