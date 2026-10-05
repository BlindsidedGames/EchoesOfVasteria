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
                    try { return operation.Result.Entry.GetLocalizedString(arguments); }
                    catch (FormatException) { /* Keep English usable when a translated template has invalid arguments. */ }
                }
            }
            try { return string.Format(LocalizationSettings.SelectedLocaleAsync.IsDone ?
                LocalizationSettings.SelectedLocaleAsync.Result?.Identifier.CultureInfo ?? CultureInfo.CurrentCulture : CultureInfo.CurrentCulture,
                english ?? string.Empty, arguments ?? Array.Empty<object>()); }
            catch (FormatException) { return english ?? string.Empty; }
        }
        public static string Name(UnityEngine.Object value, string category = "resource") =>
            value ? Text(category + "." + value.name, value.name) : string.Empty;

        public static void Bind(TextElement element, string key, string english, params object[] arguments)
        {
            BindValue(element, () => element.text = Text(key, english, arguments));
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
