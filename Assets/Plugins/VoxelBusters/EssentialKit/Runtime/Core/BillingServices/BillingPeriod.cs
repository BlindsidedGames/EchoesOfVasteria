using System.Diagnostics;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Represents duration of billing product subscription period.
    /// </summary>
    /// @ingroup BillingServices
    public class BillingPeriod
    {
        /// <summary>
        /// Gets the duration of the subscription period.
        /// </summary>
        public double Duration { get; private set; }
        
        /// <summary>
        /// Gets the unit of the subscription period.
        /// </summary>
        public BillingPeriodUnit Unit { get; private set; }
        
        public BillingPeriod(double duration, BillingPeriodUnit unit)
        {
            Duration = duration;
            Unit = unit;
        }

        public override string ToString()
        {
            return $"[BillingPeriod: Duration={Duration}, Unit={Unit}]";
        }
    }
}