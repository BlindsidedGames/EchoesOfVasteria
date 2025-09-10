using UnityEngine;
using System.Collections;

namespace VoxelBusters.CoreLibrary
{
    public class ApplicationPrivacyConfiguration
    {
        #region Properties

        public ConsentStatus UsageConsent { get; private set; } //GDPR

        public bool? IsAgeRestrictedUser { get; private set; } //COPPA  (Directed to children)

        public ContentRating? PreferredContentRating { get; private set; } 

        public bool? DoNotSell { get; set; } //U.S. State Privacy Laws

        public string Version { get; private set; }

        #endregion

        #region Constructors

        public ApplicationPrivacyConfiguration(ConsentStatus usageConsent,
                                               bool? isAgeRestrictedUser = null,
                                               ContentRating? preferredContentRating = null,
                                               bool? doNotSell = null,
                                               string version = null)
        {
            // Set properties
            UsageConsent            = usageConsent;
            IsAgeRestrictedUser     = isAgeRestrictedUser;
            PreferredContentRating  = preferredContentRating;
            DoNotSell               = doNotSell;
            Version                 = version;
        }

        #endregion

        #region Public methods

        public bool? IsCoppaApplicable()
        {
            if (IsAgeRestrictedUser == null) return null;

            return IsAgeRestrictedUser.Value || (UsageConsent != ConsentStatus.Authorized);
        }

        #endregion

    }
}