#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blindsided.Utilities.Pooling;
using NUnit.Framework;
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
        public IEnumerator MemoryCleanupDiscardsIdleRunPoolsButPreservesLiveInstances()
        {
            var prefab = new GameObject("Memory cleanup template");
            GameObject first = null, second = null, replacement = null;
            try
            {
                first = PoolManager.Get(prefab);
                second = PoolManager.Get(prefab);
                PoolManager.Release(second);
                PoolManager.TrimInactive();
                yield return null;
                Assert.True(first && first.activeSelf, "Cleanup must never remove a live gameplay object.");
                PoolManager.Release(first);
                PoolManager.TrimInactive();
                yield return null;
                Assert.True(first == null && second == null, "The completed run must not retain its idle instances.");
                replacement = PoolManager.Get(prefab);
                Assert.True(replacement && replacement.activeSelf, "The same pool must remain usable next run.");
            }
            finally
            {
                if (first) PoolManager.Release(first);
                if (second && second.activeSelf) PoolManager.Release(second);
                if (replacement) PoolManager.Release(replacement);
                PoolManager.TrimInactive();
                Object.DestroyImmediate(prefab);
            }
        }

        [UnityTest]
        public IEnumerator MemoryCleanupClosesBookPanelsAndPreservesReopening()
        {
            var books = new List<GameObject>();
            var baselinePanels = Resources.FindObjectsOfTypeAll<PanelSettings>().Count(p => !EditorUtility.IsPersistent(p));
            try
            {
                for (var i = 0; i < 4; i++)
                {
                    var host = new GameObject("Memory book " + i, typeof(UIDocument));
                    books.Add(host);
                    var book = host.AddComponent<ToolkitBookScreen>();
                    var data = new SerializedObject(book);
                    data.FindProperty("definition").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ToolkitBookDefinition>("Assets/UI/Toolkit/Library.asset");
                    data.FindProperty("theme").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ToolkitTheme>("Assets/UI/Toolkit/Theme.asset");
                    data.FindProperty("template").objectReferenceValue = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Toolkit/Book.uxml");
                    data.FindProperty("runtimeTheme").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/UI/Toolkit/Runtime.tss");
                    data.FindProperty("textSettings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PanelTextSettings>("Assets/UI/Toolkit/TextSettings.asset");
                    data.ApplyModifiedPropertiesWithoutUndo();
                    Assert.True(book.Show());
                    yield return null;
                    book.Hide();
                    yield return null;
                }
                Assert.LessOrEqual(Resources.FindObjectsOfTypeAll<PanelSettings>().Count(p =>
                    !EditorUtility.IsPersistent(p)), baselinePanels + 1,
                    "Closed menus must not accumulate their own render panels and atlases.");
                var reopened = books[0].GetComponent<ToolkitBookScreen>();
                Assert.True(reopened.Show());
                yield return null;
                Assert.NotNull(books[0].GetComponent<UIDocument>().rootVisualElement.Q("book-frame"));
            }
            finally { foreach (var host in books) if (host) Object.DestroyImmediate(host); }
        }
    }
}
#endif
