using System;
using System.Collections.Generic;
using TimelessEchoes.Upgrades;
using UnityEngine;

namespace TimelessEchoes.Gear.UI
{
    /// <summary>
    /// Represents a conversion operation in the forge (ingot, crystal, chunk, or core conversion).
    /// Encapsulates the logic for checking affordability, calculating amounts, and executing conversions.
    /// </summary>
    [Serializable]
    public class ConversionPipeline
    {
        public ConversionType Type { get; private set; }
        public CraftSection2x1UIReferences UISection { get; private set; }

        // Cost formula delegates
        public Func<ResourceManager, CoreSO, bool> CanPerform { get; private set; }
        public Func<ResourceManager, CoreSO, double, double> GetMaxAmount { get; private set; }
        public Action<ResourceManager, CoreSO, double, Dictionary<Resource, double>> Execute { get; private set; }

        // Amount tracking
        public double DesiredAmount { get; set; } = 1;

        /// <summary>
        /// Creates a new conversion pipeline.
        /// </summary>
        public ConversionPipeline(
            ConversionType type,
            CraftSection2x1UIReferences uiSection,
            Func<ResourceManager, CoreSO, bool> canPerform,
            Func<ResourceManager, CoreSO, double, double> getMaxAmount,
            Action<ResourceManager, CoreSO, double, Dictionary<Resource, double>> execute)
        {
            Type = type;
            UISection = uiSection;
            CanPerform = canPerform;
            GetMaxAmount = getMaxAmount;
            Execute = execute;
        }

        /// <summary>
        /// Performs the conversion and records telemetry.
        /// </summary>
        /// <returns>True if conversion was performed, false otherwise.</returns>
        public bool PerformConversion(ResourceManager rm, CoreSO core)
        {
            if (rm == null || core == null) return false;
            if (CanPerform == null || !CanPerform(rm, core)) return false;

            var amount = GetMaxAmount?.Invoke(rm, core, DesiredAmount) ?? 0;
            if (amount <= 0) return false;

            // Track costs for analytics
            var costs = new Dictionary<Resource, double>();

            // Execute the conversion
            Execute?.Invoke(rm, core, amount, costs);

            // Record telemetry via ForgeAnalyticsService
            ForgeAnalyticsService.Instance?.RecordConversion(Type, core, amount, costs);

            return true;
        }

        /// <summary>
        /// Gets the maximum craftable amount based on current resources.
        /// </summary>
        public double GetCalculatedMaxAmount(ResourceManager rm, CoreSO core)
        {
            if (rm == null || core == null || GetMaxAmount == null) return 0;
            return GetMaxAmount(rm, core, double.MaxValue);
        }

        /// <summary>
        /// Clamps the desired amount to valid bounds.
        /// </summary>
        public double ClampDesiredAmount(double maxAmount)
        {
            DesiredAmount = Math.Max(1, Math.Min(DesiredAmount, Math.Max(1, maxAmount)));
            return DesiredAmount;
        }
    }
}
