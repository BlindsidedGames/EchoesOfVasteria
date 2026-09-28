using System;
using TimelessEchoes.Upgrades;
using UnityEngine;
namespace TimelessEchoes.Gear
{
    /// <summary>Forge resource mappings independent of any UI hierarchy.</summary>
    [CreateAssetMenu(menuName = "Timeless Echoes/Forge Catalog")]
    public sealed class ForgeCatalog : ScriptableObject
    {
        [Serializable] public sealed class CoreBinding { public CoreSO core; public Resource coreResource, ingotResource; }
        public CoreBinding[] cores = Array.Empty<CoreBinding>();
        public Resource slime, stone;
        public CoreBinding Find(CoreSO core) => Array.Find(cores, x => x.core == core);
        public (Resource currentCore, Resource nextCore, bool finalTier) ConversionResources(CoreSO core)
        {
            var index = Array.FindIndex(cores, x => x.core == core);
            if (index < 0) return (null, null, true);
            return (cores[index].coreResource, index + 1 < cores.Length ? cores[index + 1].coreResource : null, index + 1 >= cores.Length);
        }
    }
}
