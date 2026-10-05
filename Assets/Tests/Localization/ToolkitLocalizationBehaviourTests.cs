#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TimelessEchoes.UI.Toolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace TimelessEchoes.Tests.Localization
{
    // Real Unity StringDatabase + StringTables. Only table delivery is controlled:
    // formatting, entry lookup, locale notifications and UI panel events are not mocked.
    public class ToolkitLocalizationBehaviourTests
    {
        private LocalizationSettings previous;
        private FixtureSettings settings;
        private Tables tables;
        private Locale english, russian;
        private EditorWindow window;
        private string previousEditorLocale;
        private static string EditorLocaleCode
        {
            get => (string)typeof(LocalizationSettings).GetProperty("EditorLocaleCode", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null);
            set => typeof(LocalizationSettings).GetProperty("EditorLocaleCode", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).SetValue(null, value);
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previous = LocalizationSettings.GetInstanceDontCreateDefault();
            previousEditorLocale = EditorLocaleCode;
            settings = ScriptableObject.CreateInstance<FixtureSettings>();
            tables = new Tables();
            english = Locale.CreateLocale("en");
            russian = Locale.CreateLocale("ru");
            settings.SetAvailableLocales(new FixtureLocales(english, russian));
            settings.GetStringDatabase().TableProvider = tables;
            settings.GetStringDatabase().UseFallback = false;
            LocalizationSettings.Instance = settings;
            LocalizationSettings.SelectedLocale = english;
            window = ScriptableObject.CreateInstance<FixtureWindow>();
            window.Show();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            window.Close();
            // Switching locale retires the adapter's entry leases through its normal path.
            LocalizationSettings.SelectedLocale = russian;
            yield return null;
            // Invoke Unity's subsystem reset callback solely for fixture isolation: assertions
            // depend on public behaviour, and no production testing API is added.
            typeof(ToolkitLocalization).GetMethod("Reset", System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Static)?.Invoke(null, null);
            LocalizationSettings.Instance = previous;
            EditorLocaleCode = previousEditorLocale;
            ((IDisposable)settings).Dispose();
            Object.DestroyImmediate(settings);
            tables.Dispose();
            Object.DestroyImmediate(english);
            Object.DestroyImmediate(russian);
        }

        [UnityTest]
        public IEnumerator BoundLabelShowsEnglishUntilTableArrivesThenRefreshes()
        {
            tables.Add(english, "test.delayed", "Delivered translation");
            tables.Delay = true;
            var label = new Label();
            ToolkitLocalization.Bind(label, "test.delayed", "Usable fallback");
            window.rootVisualElement.Add(label);
            Assert.That(label.text, Is.EqualTo("Usable fallback"));
            yield return null;
            tables.Deliver();
            for (var frame = 0; frame < 10 && label.text != "Delivered translation"; frame++) yield return null;
            Assert.That(label.text, Is.EqualTo("Delivered translation"));
        }

        [UnityTest]
        public IEnumerator OpenLabelSurvivesCacheAutoReleaseAndDetachedLabelWaitsUntilReopened()
        {
            tables.Add(english, "test.switch", "English view");
            tables.Add(russian, "test.switch", "Русский вид");
            var label = new Label();
            ToolkitLocalization.Bind(label, "test.switch", "Fallback");
            window.rootVisualElement.Add(label);
            for (var i = 0; i < 5; i++) yield return null;
            Assert.That(label.text, Is.EqualTo("English view"));
            Assert.That(ToolkitLocalization.Text("test.switch", "Fallback"), Is.EqualTo("English view"));
            LocalizationSettings.SelectedLocale = russian;
            for (var i = 0; i < 5; i++) yield return null;
            Assert.That(label.text, Is.EqualTo("Русский вид"));
            label.RemoveFromHierarchy();
            LocalizationSettings.SelectedLocale = english;
            for (var i = 0; i < 5; i++) yield return null;
            Assert.That(label.text, Is.EqualTo("Русский вид"), "Closed UI must stop receiving locale updates.");
            window.rootVisualElement.Add(label);
            for (var i = 0; i < 5; i++) yield return null;
            Assert.That(label.text, Is.EqualTo("English view"));
        }

        [UnityTest]
        public IEnumerator DynamicArgumentsUseTranslationAndBrokenTranslationKeepsEnglishUsable()
        {
            tables.Add(english, "test.quantity", "Collected {0} of {1}");
            tables.Add(english, "test.broken", "Invalid {2}");
            ToolkitLocalization.Text("test.quantity", "{0}/{1}", 3, 8);
            ToolkitLocalization.Text("test.broken", "Need {0} logs", 4);
            for (var i = 0; i < 5; i++) yield return null;
            Assert.That(ToolkitLocalization.Text("test.quantity", "{0}/{1}", 3, 8), Is.EqualTo("Collected 3 of 8"));
            Assert.That(ToolkitLocalization.Text("test.broken", "Need {0} logs", 4), Is.EqualTo("Need 4 logs"));
        }

        [UnityTest]
        public IEnumerator DisplayNameTranslationPreservesResourceIdentity()
        {
            var resource = ScriptableObject.CreateInstance<SharedTableData>();
            resource.name = "StableResourceId";
            try
            {
                tables.Add(english, "resource.StableResourceId", "Player-facing name");
                ToolkitLocalization.Name(resource);
                for (var i = 0; i < 5; i++) yield return null;
                Assert.That(ToolkitLocalization.Name(resource), Is.EqualTo("Player-facing name"));
                Assert.That(resource.name, Is.EqualTo("StableResourceId"));
            }
            finally { Object.DestroyImmediate(resource); }
        }

        public class FixtureWindow : EditorWindow { }
        public class FixtureSettings : LocalizationSettings
        {
            private AsyncOperationHandle<LocalizationSettings> ready;
            public override AsyncOperationHandle<LocalizationSettings> GetInitializationOperation()
            {
                if (!ready.IsValid()) ready = Addressables.ResourceManager.CreateCompletedOperation<LocalizationSettings>(this, null);
                return ready;
            }
            private void OnDestroy() { if (ready.IsValid()) Addressables.Release(ready); }
        }
        private sealed class FixtureLocales : ILocalesProvider
        {
            public List<Locale> Locales { get; } = new List<Locale>();
            public FixtureLocales(params Locale[] locales) { Locales.AddRange(locales); }
            public Locale GetLocale(LocaleIdentifier id) => Locales.Find(locale => locale.Identifier == id);
            public void AddLocale(Locale locale) => Locales.Add(locale);
            public bool RemoveLocale(Locale locale) => Locales.Remove(locale);
        }
        private sealed class Delivery<T> : AsyncOperationBase<T>
        {
            protected override void Execute() { }
            public void Deliver(T value) => Complete(value, true, null);
        }
        private sealed class Tables : ITableProvider, IDisposable
        {
            private readonly Dictionary<string, StringTable> tables = new Dictionary<string, StringTable>();
            private readonly List<Action> deliveries = new List<Action>();
            public bool Delay;
            public void Add(Locale locale, string key, string value)
            {
                if (!tables.TryGetValue(locale.Identifier.Code, out var table))
                {
                    table = ScriptableObject.CreateInstance<StringTable>();
                    table.SharedData = ScriptableObject.CreateInstance<SharedTableData>();
                    table.SharedData.TableCollectionName = "TownUI";
                    table.LocaleIdentifier = locale.Identifier;
                    tables.Add(locale.Identifier.Code, table);
                }
                table.AddEntry(key, value);
            }
            public AsyncOperationHandle<T> ProvideTableAsync<T>(string name, Locale locale) where T : LocalizationTable
            {
                Assert.That(name, Is.EqualTo("TownUI"));
                tables.TryGetValue(locale.Identifier.Code, out var table);
                var operation = new Delivery<T>();
                var handle = Addressables.ResourceManager.StartOperation(operation, default);
                if (Delay) deliveries.Add(() => operation.Deliver(table as T));
                else operation.Deliver(table as T);
                return handle;
            }
            public void Deliver() { foreach (var delivery in deliveries) delivery(); deliveries.Clear(); }
            public void Dispose()
            {
                foreach (var table in tables.Values)
                { Object.DestroyImmediate(table.SharedData); Object.DestroyImmediate(table); }
            }
        }
    }
}
#endif
