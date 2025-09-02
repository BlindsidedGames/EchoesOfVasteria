using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    public static class TaskServicesError
    {

        public const string kDomain = "Task Services";

        public static Error Unknown => CreateError(TaskServicesErrorCode.Unknown, "Unknown error");

        private static Error CreateError(TaskServicesErrorCode code, string description)
        {
            return new Error(kDomain, (int)code, description);
        }
    }
}