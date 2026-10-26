using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements;
using EntryResult = UnityEngine.Localization.Settings.LocalizedDatabase<UnityEngine.Localization.Tables.StringTable, UnityEngine.Localization.Tables.StringTableEntry>.TableEntryResult;

namespace TimelessEchoes.UI.Toolkit
{
    /// <summary>Presentation adapter for the existing Unity TownUI table; game identifiers remain unchanged.</summary>
    public static class ToolkitLocalization
    {
        private static readonly Dictionary<string, AsyncOperationHandle<EntryResult>> Entries = new();
        private static bool subscribed, waiting, notificationPending;
        private static int generation;
        public static event Action Changed;
        public static int Revision { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            if (subscribed) LocalizationSettings.SelectedLocaleChanged -= LocaleChanged;
            ReleaseEntries(); subscribed = waiting = notificationPending = false; generation++; Revision = 0; Changed = null;
        }
        private static void ReleaseEntries()
        {
            foreach (var entry in Entries.Values) if (entry.IsValid()) Addressables.Release(entry);
            Entries.Clear();
        }
        private static void LocaleChanged(Locale _)
        {
            ReleaseEntries(); NotifyChanged();
        }
        private static void NotifyChanged()
        {
            if (notificationPending) return;
            notificationPending = true;
            var requestedGeneration = generation;
            void Flush()
            {
                if (requestedGeneration != generation) return;
                notificationPending = false; Revision++; Changed?.Invoke();
            }
            // Unity's main-thread context defers and coalesces cached-table completions,
            // so callbacks cannot rebuild a screen in the middle of constructing it.
            var context = SynchronizationContext.Current;
            if (context != null) context.Post(_ => Flush(), null);
            else Flush();
        }
        private static bool Ready()
        {
            if (!subscribed) { LocalizationSettings.SelectedLocaleChanged += LocaleChanged; subscribed = true; }
            var initialization = LocalizationSettings.InitializationOperation;
            if (initialization.IsDone)
            {
                var locale = LocalizationSettings.SelectedLocaleAsync;
                return initialization.Status == AsyncOperationStatus.Succeeded &&
                    locale.IsDone && locale.Status == AsyncOperationStatus.Succeeded && locale.Result != null;
            }
            if (!waiting)
            {
                waiting = true;
                initialization.Completed += _ => { waiting = false; NotifyChanged(); };
            }
            return false;
        }
        public static string Text(string key, string english, params object[] arguments)
        {
            if (!string.IsNullOrEmpty(key) && Ready())
            {
                if (!Entries.TryGetValue(key, out var operation))
                {
                    operation = LocalizationSettings.StringDatabase.GetTableEntryAsync("TownUI", key);
                    // Unity entry operations auto-release after completion; retain our cached handle.
                    Addressables.ResourceManager.Acquire(operation);
                    Entries[key] = operation;
                    if (!operation.IsDone)
                    {
                        var requestedRevision = Revision;
                        operation.Completed += _ =>
                        {
                            if (requestedRevision <= Revision && Entries.TryGetValue(key, out var current) && current.Equals(operation))
                            { NotifyChanged(); }
                        };
                    }
                }
                if (operation.IsValid() && operation.IsDone && operation.Status == AsyncOperationStatus.Succeeded &&
                    operation.Result.Entry != null && !string.IsNullOrEmpty(operation.Result.Entry.LocalizedValue))
                {
                    try { return operation.Result.Entry.IsSmart ? operation.Result.Entry.GetLocalizedString(arguments) :
                        string.Format(Blindsided.Utilities.LocaleNumbers.Culture, operation.Result.Entry.LocalizedValue, arguments ?? Array.Empty<object>()); }
                    catch (FormatException) { /* Keep English usable when a translated template has invalid arguments. */ }
                }
            }
            try { return string.Format(Blindsided.Utilities.LocaleNumbers.Culture,
                english ?? string.Empty, arguments ?? Array.Empty<object>()); }
            catch (FormatException) { return english ?? string.Empty; }
        }
        public static string Name(UnityEngine.Object value, string category = "resource")
        {
            if (!value) return string.Empty;
            if (category == "stat")
            {
                if (value is TimelessEchoes.Upgrades.BaseStat baseStat && baseStat.AssociatedStat)
                    return baseStat.AssociatedStat.GetName();
                if (value is TimelessEchoes.Gear.StatDefSO statDefinition)
                    return statDefinition.GetName();
            }
            if (value is TimelessEchoes.Tasks.TaskData task && (category == "resource" || category == "task"))
                return Text("task." + value.name, task.taskName);
            return Text(category + "." + value.name,
                category == "resource" ? ResourceNameFallback(value.name) : value.name);
        }

        // Asset names are persistent save keys. Keep the renamed ores readable even
        // before the localization tables finish loading, without renaming those keys.
        internal static string ResourceNameFallback(string name)
        {
            name = name switch
            {
                "Muddy Muck Muncher" => "Muck Muncher",
                "Sir Splashford III" => "Sir Splash",
                "Bloopicus Maximus" => "Bloopicus",
                "Niblet the Bold" => "Niblet",
                "Flippy Floppy" => "Flippy",
                _ => name
            };
            var separator = name.IndexOf(' ');
            var tier = separator < 0 ? name : name.Substring(0, separator);
            var display = tier switch
            {
                "Eznorb" => "Bronze", "Nori" => "Iron", "Dlog" => "Gold",
                "Erif" => "Fire", "Idle" => "Crimsonite", _ => tier
            };
            return display == tier ? name : display + (separator < 0 ? "" : name.Substring(separator));
        }

        public static void Bind(TextElement element, string key, string english, params object[] arguments)
        {
            BindValue(element, () =>
            {
                element.text = Text(key, english, arguments);
                if (element.text.Contains("<link=\"help:")) ToolkitHelpLinks.Attach(element);
            });
        }
        public static void BindTooltip(VisualElement element, string key, string english, params object[] arguments)
        {
            BindValue(element, () => element.tooltip = Text(key, english, arguments));
        }
        private static void BindValue(VisualElement element, Action refresh)
        {
            var attached = false;
            void Attach()
            {
                if (attached) return;
                attached = true; Changed += refresh; refresh();
            }
            void Detach()
            {
                if (!attached) return;
                attached = false; Changed -= refresh;
            }
            element.RegisterCallback<AttachToPanelEvent>(_ => Attach());
            element.RegisterCallback<DetachFromPanelEvent>(_ => Detach());
            refresh(); if (element.panel != null) Attach();
        }
    }
}
