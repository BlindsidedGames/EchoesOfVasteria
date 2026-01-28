using System;
using System.Collections.Generic;
using TimelessEchoes.Upgrades;
using UnityEngine;

namespace TimelessEchoes.Gear.UI
{
    /// <summary>
    /// Factory for creating configured conversion pipelines for each conversion type.
    /// </summary>
    public static class ConversionPipelineFactory
    {
        /// <summary>
        /// Creates an ingot conversion pipeline (chunks + crystals -> ingots).
        /// </summary>
        public static ConversionPipeline CreateIngotPipeline(CraftSection2x1UIReferences section)
        {
            return new ConversionPipeline(
                ConversionType.Ingot,
                section,
                canPerform: (rm, core) =>
                {
                    if (rm == null || core == null) return false;
                    if (core.chunkResource == null || core.crystalResource == null) return false;
                    if (core.chunkCostPerIngot <= 0 && core.crystalCostPerIngot <= 0) return false;
                    var haveChunks = core.chunkCostPerIngot <= 0 || rm.GetAmount(core.chunkResource) >= core.chunkCostPerIngot;
                    var haveCrystals = core.crystalCostPerIngot <= 0 || rm.GetAmount(core.crystalResource) >= core.crystalCostPerIngot;
                    return haveChunks && haveCrystals;
                },
                getMaxAmount: (rm, core, desired) =>
                {
                    if (rm == null || core == null) return 0;
                    double maxByChunks = core.chunkCostPerIngot > 0 && core.chunkResource != null
                        ? Math.Floor(rm.GetAmount(core.chunkResource) / core.chunkCostPerIngot)
                        : double.MaxValue;
                    double maxByCrystals = core.crystalCostPerIngot > 0 && core.crystalResource != null
                        ? Math.Floor(rm.GetAmount(core.crystalResource) / core.crystalCostPerIngot)
                        : double.MaxValue;
                    double max = Math.Min(maxByChunks, maxByCrystals);
                    return Math.Max(0, Math.Min(desired, max));
                },
                execute: (rm, core, amount, costs) =>
                {
                    rm.BeginBatch();
                    try
                    {
                        if (core.chunkResource != null && core.chunkCostPerIngot > 0)
                        {
                            var chunkCost = core.chunkCostPerIngot * amount;
                            rm.Spend(core.chunkResource, chunkCost);
                            costs[core.chunkResource] = chunkCost;
                        }
                        if (core.crystalResource != null && core.crystalCostPerIngot > 0)
                        {
                            var crystalCost = core.crystalCostPerIngot * amount;
                            rm.Spend(core.crystalResource, crystalCost);
                            costs[core.crystalResource] = crystalCost;
                        }
                        rm.Add(core.requiredIngot, amount, trackStats: false);
                    }
                    finally
                    {
                        rm.EndBatch();
                    }
                });
        }

        /// <summary>
        /// Creates a crystal conversion pipeline (chunks + slime -> crystals).
        /// </summary>
        public static ConversionPipeline CreateCrystalPipeline(CraftSection2x1UIReferences section, Resource slimeResource)
        {
            return new ConversionPipeline(
                ConversionType.Crystal,
                section,
                canPerform: (rm, core) =>
                {
                    if (rm == null || core == null) return false;
                    if (core.chunkResource == null || slimeResource == null) return false;
                    var haveChunks = rm.GetAmount(core.chunkResource) >= 2;
                    var haveSlime = rm.GetAmount(slimeResource) >= 1;
                    return haveChunks && haveSlime;
                },
                getMaxAmount: (rm, core, desired) =>
                {
                    if (rm == null || core == null) return 0;
                    double maxByChunks = core.chunkResource != null
                        ? Math.Floor(rm.GetAmount(core.chunkResource) / 2.0)
                        : 0;
                    double maxBySlime = slimeResource != null
                        ? Math.Floor(rm.GetAmount(slimeResource))
                        : 0;
                    double max = Math.Min(maxByChunks, maxBySlime);
                    return Math.Max(0, Math.Min(desired, max));
                },
                execute: (rm, core, amount, costs) =>
                {
                    rm.BeginBatch();
                    try
                    {
                        if (core.chunkResource != null)
                        {
                            var chunkCost = 2 * amount;
                            rm.Spend(core.chunkResource, chunkCost);
                            costs[core.chunkResource] = chunkCost;
                        }
                        if (slimeResource != null)
                        {
                            var slimeCost = 1 * amount;
                            rm.Spend(slimeResource, slimeCost);
                            costs[slimeResource] = slimeCost;
                        }
                        if (core.crystalResource != null)
                            rm.Add(core.crystalResource, amount, trackStats: false);
                    }
                    finally
                    {
                        rm.EndBatch();
                    }
                });
        }

        /// <summary>
        /// Creates a chunk conversion pipeline (crystals + stone -> chunks).
        /// </summary>
        public static ConversionPipeline CreateChunkPipeline(CraftSection2x1UIReferences section, Resource stoneResource)
        {
            return new ConversionPipeline(
                ConversionType.Chunk,
                section,
                canPerform: (rm, core) =>
                {
                    if (rm == null || core == null) return false;
                    if (core.crystalResource == null || stoneResource == null) return false;
                    var haveCrystals = rm.GetAmount(core.crystalResource) >= 1;
                    var haveStone = rm.GetAmount(stoneResource) >= 2;
                    return haveCrystals && haveStone;
                },
                getMaxAmount: (rm, core, desired) =>
                {
                    if (rm == null || core == null) return 0;
                    double maxByCrystals = core.crystalResource != null
                        ? Math.Floor(rm.GetAmount(core.crystalResource))
                        : 0;
                    double maxByStone = stoneResource != null
                        ? Math.Floor(rm.GetAmount(stoneResource) / 2.0)
                        : 0;
                    double max = Math.Min(maxByCrystals, maxByStone);
                    return Math.Max(0, Math.Min(desired, max));
                },
                execute: (rm, core, amount, costs) =>
                {
                    rm.BeginBatch();
                    try
                    {
                        if (core.crystalResource != null)
                        {
                            var crystalCost = 1 * amount;
                            rm.Spend(core.crystalResource, crystalCost);
                            costs[core.crystalResource] = crystalCost;
                        }
                        if (stoneResource != null)
                        {
                            var stoneCost = 2 * amount;
                            rm.Spend(stoneResource, stoneCost);
                            costs[stoneResource] = stoneCost;
                        }
                        if (core.chunkResource != null)
                            rm.Add(core.chunkResource, amount, trackStats: false);
                    }
                    finally
                    {
                        rm.EndBatch();
                    }
                });
        }

        /// <summary>
        /// Creates a core conversion pipeline (5 current tier + 1 next tier -> 2 next tier).
        /// </summary>
        public static ConversionPipeline CreateCorePipeline(
            CraftSection2x1UIReferences section,
            Func<CoreSO, (Resource currentCore, Resource nextCore, bool isFinalTier)> resolveResources)
        {
            return new ConversionPipeline(
                ConversionType.Core,
                section,
                canPerform: (rm, core) =>
                {
                    if (rm == null || core == null) return false;
                    var (curRes, nextRes, isFinalTier) = resolveResources(core);
                    if (isFinalTier || curRes == null || nextRes == null) return false;
                    var haveCurrent = rm.GetAmount(curRes) >= 5;
                    var haveNext = rm.GetAmount(nextRes) >= 1;
                    return haveCurrent && haveNext;
                },
                getMaxAmount: (rm, core, desired) =>
                {
                    if (rm == null || core == null) return 0;
                    var (curRes, nextRes, isFinalTier) = resolveResources(core);
                    if (isFinalTier || curRes == null || nextRes == null) return 0;
                    // Need 5 current + 1 next to produce 2 next (net +1 next)
                    double maxByCurrent = Math.Floor(rm.GetAmount(curRes) / 5.0);
                    double maxByNext = Math.Floor(rm.GetAmount(nextRes));
                    double max = Math.Min(maxByCurrent, maxByNext);
                    return Math.Max(0, Math.Min(desired, max));
                },
                execute: (rm, core, amount, costs) =>
                {
                    var (curRes, nextRes, isFinalTier) = resolveResources(core);
                    if (isFinalTier || curRes == null || nextRes == null) return;

                    rm.BeginBatch();
                    try
                    {
                        var currentCost = 5 * amount;
                        var nextCost = 1 * amount;
                        rm.Spend(curRes, currentCost);
                        rm.Spend(nextRes, nextCost);
                        rm.Add(nextRes, 2 * amount, trackStats: false);

                        costs[curRes] = currentCost;
                        costs[nextRes] = nextCost;
                    }
                    finally
                    {
                        rm.EndBatch();
                    }
                });
        }
    }
}
