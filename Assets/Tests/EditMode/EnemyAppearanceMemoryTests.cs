#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TimelessEchoes.Enemies;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace Tests.EditMode
{
    public sealed class EnemyAppearanceMemoryTests
    {
        [TestCase("Pig")]
        [TestCase("Chicken")]
        [TestCase("Cow")]
        [TestCase("Sheep")]
        public void DormantEnemyKeepsOnlyItsDefaultAppearanceAndOtherSkinsRemainLoadable(string species)
        {
            var path = "Assets/Prefabs/Tasks/Enemies/Farmlands/" + species + ".prefab";
            var retained = AssetDatabase.GetDependencies(path, true).Count(p =>
                Path.GetFileName(p).StartsWith(species + "_") && p.EndsWith(".spriteLib"));
            Assert.LessOrEqual(retained, 1, "Loading enemy definitions must not load their whole appearance wardrobe.");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var data = new SerializedObject(prefab.GetComponentInChildren<Enemy>(true));
            var regular = data.FindProperty("spriteLibraryVariantPaths");
            var weighted = data.FindProperty("weightedSpriteLibraryVariants");
            var paths = new HashSet<string>();
            for (var i = 0; i < regular.arraySize; i++) paths.Add(regular.GetArrayElementAtIndex(i).stringValue);
            for (var i = 0; i < weighted.arraySize; i++) paths.Add(weighted.GetArrayElementAtIndex(i).FindPropertyRelative("resourcePath").stringValue);
            paths.RemoveWhere(string.IsNullOrEmpty);
            Assert.Greater(paths.Count, 1, "The memory repair must retain the authored variety.");
            foreach (var resourcePath in paths)
            {
                var library = Resources.Load<SpriteLibraryAsset>(resourcePath);
                Assert.NotNull(library, resourcePath + " must resolve in a player.");
                Assert.True(library.GetCategoryNames().Any(category => library.GetCategoryLabelNames(category)
                    .Any(label => library.GetSprite(category, label))), resourcePath + " must still contain usable animation frames.");
            }
        }
    }
}
#endif
