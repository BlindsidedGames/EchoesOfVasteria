using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Represents a product offer for a billing product.
    /// </summary>
    /// @ingroup BillingServices
    public class BillingProductOffer
    {
        /// <summary>
        /// The product offer identifier.
        /// </summary>
        public string   Id { get; private set; }

        /// <summary>
        /// The product offer category to know if its an introductory offer or promotional offer.
        /// </summary>
        public BillingProductOfferCategory      Category {get; private set; } 

        /// <summary>
        /// Phases of how the pricing is calculated. 
        /// note: On Android there can be more than one pricing phase but on iOS it's only one.
        /// </summary>
        public IOrderedEnumerable<BillingProductOfferPricingPhase> PricingPhases { get; private set; }  
        
        public BillingProductOffer(string id, BillingProductOfferCategory category, List<BillingProductOfferPricingPhase> pricingPhases)
        {
            Id = id;
            Category = category;
            PricingPhases = pricingPhases.OrderBy(x => pricingPhases.IndexOf(x));
        }

        public override string ToString()
        {
            return string.Format("[Id={0}, Category={1}, PricingPhases={2}]", Id, Category, string.Join(", ", PricingPhases));
        }
    }
}