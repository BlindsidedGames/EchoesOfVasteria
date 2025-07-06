using System.Collections.Generic;
using System.Diagnostics;
using QFSW.QC;
using UnityEngine;
using Debug = UnityEngine.Debug;


namespace TimelessEchoes
{
    public enum TELogCategory
    {
        General,
        Hero,
        Task,
        Combat,
        Map
    }

    public static class TELogger
    {
        private static readonly HashSet<TELogCategory> _enabledCategories = new()
        {
            TELogCategory.General,
            TELogCategory.Hero,
            TELogCategory.Task,
            TELogCategory.Combat,
            TELogCategory.Map
        };

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Log(string message, TELogCategory category = TELogCategory.General,
            Object context = null)
        {
            if (!_enabledCategories.Contains(category)) return;

            if (context != null)
                Debug.Log(message, context);
            else
                Debug.Log(message);
        }

        [Command("enable-log", "Enable logging for a category")] 
        public static void EnableLogCategory(TELogCategory category) => _enabledCategories.Add(category);

        [Command("disable-log", "Disable logging for a category")]
        public static void DisableLogCategory(TELogCategory category) => _enabledCategories.Remove(category);

        [Command("list-log-categories", "Lists currently enabled log categories")]
        public static IEnumerable<TELogCategory> ListLogCategories() => _enabledCategories;
    }
}