using UnityEngine;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

namespace Blindsided
{
    [CreateAssetMenu(menuName = "Timeless Echoes/Build Mode Config", fileName = "BuildModeConfig")]
    public sealed class BuildModeConfig : ScriptableObject
    {
        public const string AssetPath = "Assets/Resources/BuildModeConfig.asset";
        public const string ResourcePath = "BuildModeConfig";

        [SerializeField] private bool demo;
        [SerializeField] private bool beta;

        public bool Demo
        {
            get => demo;
            set => demo = value;
        }

        public bool Beta
        {
            get => beta;
            set => beta = value;
        }

        private static BuildModeConfig cachedInstance;

        public static BuildModeConfig Load()
        {
            if (cachedInstance == null)
            {
                cachedInstance = Resources.Load<BuildModeConfig>(ResourcePath);
#if UNITY_EDITOR
                if (cachedInstance == null)
                {
                    Debug.LogWarning($"BuildModeConfig asset not found at Resources/{ResourcePath}. Defaulting to a transient instance.");
                }
#endif
                if (cachedInstance == null)
                {
                    cachedInstance = CreateInstance<BuildModeConfig>();
                }
            }

            return cachedInstance;
        }

#if UNITY_EDITOR
        public static BuildModeConfig LoadOrCreateAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<BuildModeConfig>(AssetPath);
            if (asset != null)
                return asset;

            asset = CreateInstance<BuildModeConfig>();
            var directory = Path.GetDirectoryName(AssetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return asset;
        }
#endif
    }
}

