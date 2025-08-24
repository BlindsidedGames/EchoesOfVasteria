using System.Collections.Generic;
using UnityEngine;

namespace Blindsided.Utilities
{
    /// <summary>
    /// Centralized, static asset cache for Resources-based assets.
    /// Minimizes repeated Resources.LoadAll/Load calls which can be costly and alloc-heavy.
    /// Use GetAll<T>() for bulk categories and GetOne<T>(path) for single loads.
    /// </summary>
    public static class AssetCache
    {
        private static readonly Dictionary<System.Type, object> allCache = new();
        private static readonly Dictionary<string, Object> oneCache = new();
        // Track which Resources paths have been loaded per type so we don't repeatedly call LoadAll
        private static readonly Dictionary<System.Type, HashSet<string>> loadedPathsPerType = new();

        /// <summary>
        /// Returns cached array of all assets of type T under an optional Resources path.
        /// Empty or null path searches entire Resources.
        /// </summary>
        public static T[] GetAll<T>(string resourcesPath = "") where T : Object
        {
            var type = typeof(T);
            // Avoid repeated string allocations on hot paths
            if (resourcesPath == null) resourcesPath = string.Empty;
            if (resourcesPath.Length == 0)
            {
                if (!allCache.TryGetValue(type, out var boxedAll))
                {
                    var loadedAll = Resources.LoadAll<T>(string.Empty);
                    allCache[type] = loadedAll;
                    if (!loadedPathsPerType.TryGetValue(type, out var setAll))
                    {
                        setAll = new HashSet<string>();
                        loadedPathsPerType[type] = setAll;
                    }
                    setAll.Add(string.Empty);
                    return loadedAll;
                }
                return (T[])boxedAll;
            }
            var pathKey = string.IsNullOrEmpty(resourcesPath) ? string.Empty : resourcesPath;

            if (!allCache.TryGetValue(type, out var boxed))
            {
                var loaded = Resources.LoadAll<T>(pathKey);
                allCache[type] = loaded;
                if (!loadedPathsPerType.TryGetValue(type, out var set))
                {
                    set = new HashSet<string>();
                    loadedPathsPerType[type] = set;
                }
                set.Add(pathKey);
                return loaded;
            }

            var arr = (T[])boxed;

            if (!loadedPathsPerType.TryGetValue(type, out var pathsLoaded))
            {
                pathsLoaded = new HashSet<string>();
                loadedPathsPerType[type] = pathsLoaded;
            }

            // If we've already loaded the entire Resources for this type (empty path), nothing more to do
            if (pathsLoaded.Contains(string.Empty))
                return arr;

            // If caller requests full scan now and we haven't done it yet, do it once
            if (string.IsNullOrEmpty(pathKey) && !pathsLoaded.Contains(string.Empty))
            {
                var newlyLoadedAll = Resources.LoadAll<T>(string.Empty);
                if (newlyLoadedAll != null && newlyLoadedAll.Length > 0)
                {
                    var set = new HashSet<T>(arr);
                    foreach (var item in newlyLoadedAll)
                        if (item != null) set.Add(item);
                    var merged = new List<T>(set).ToArray();
                    allCache[type] = merged;
                    pathsLoaded.Add(string.Empty);
                    return merged;
                }
                pathsLoaded.Add(string.Empty);
                return arr;
            }

            // For non-empty paths: if this specific path hasn't been loaded yet, load and merge once
            if (!string.IsNullOrEmpty(pathKey) && !pathsLoaded.Contains(pathKey))
            {
                var newlyLoaded = Resources.LoadAll<T>(pathKey);
                if (newlyLoaded != null && newlyLoaded.Length > 0)
                {
                    var set = new HashSet<T>(arr);
                    foreach (var item in newlyLoaded)
                        if (item != null) set.Add(item);
                    var merged = new List<T>(set).ToArray();
                    allCache[type] = merged;
                    pathsLoaded.Add(pathKey);
                    return merged;
                }
                pathsLoaded.Add(pathKey);
            }

            return arr;
        }

        /// <summary>
        /// Returns a cached single asset loaded via Resources.Load at the given path.
        /// </summary>
        public static T GetOne<T>(string resourcesPath) where T : Object
        {
            if (string.IsNullOrEmpty(resourcesPath)) return null;
            if (oneCache.TryGetValue(resourcesPath, out var obj))
                return obj as T;
            var loaded = Resources.Load<T>(resourcesPath);
            oneCache[resourcesPath] = loaded;
            return loaded;
        }

        /// <summary>
        /// Clears all cached references. Useful on domain reload or explicit refresh.
        /// </summary>
        public static void Clear()
        {
            allCache.Clear();
            oneCache.Clear();
            loadedPathsPerType.Clear();
        }
    }
}