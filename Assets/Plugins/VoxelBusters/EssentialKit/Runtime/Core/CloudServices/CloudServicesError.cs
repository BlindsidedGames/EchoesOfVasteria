
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    public class CloudServicesError
    {
        public const string kDomain = "Cloud Services(Essential Kit)";

        public static Error Unknown => CreateError(CloudServicesErrorCode.Unknown, "Unknown error");

        private static Error CreateError(CloudServicesErrorCode code, string description)
        {
            return new Error(kDomain, (int)code, description);
        }
    }
}