using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    public static class AppUpdaterError
    {

        private static string kDomain = "App Updater";

        public static Error Unknown
        {
            get
            {
                return CreateError(AppUpdaterErrorCode.Unknown, "Unknown error");
            }
        }

        public static Error NetworkIssue
        {
            get
            {
                return CreateError(AppUpdaterErrorCode.NetworkIssue, "Network issue");
            }
        }

        public static Error UpdateNotCompatible
        {
            get
            {
                return CreateError(AppUpdaterErrorCode.UpdateNotCompatible, "Update not compatible");
            }
        }

        public static Error UpdateInfoNotAvailable
        {
            get
            {
                return CreateError(AppUpdaterErrorCode.UpdateInfoNotAvailable, "Update info not available. You need to request it before prompting an update.");
            }
        }

        public static Error UpdateNotAvailable
        {
            get
            {
                return CreateError(AppUpdaterErrorCode.UpdateNotAvailable, "Update not available");
            }
        }   

        public static Error UpdateInProgress
        {
            get
            {
                return CreateError(AppUpdaterErrorCode.UpdateInProgress, "Update in progress");
            }
        }   

        public static Error UpdateCancelled
        {
            get
            {
                return CreateError(AppUpdaterErrorCode.UpdateCancelled, "Update cancelled");
            }
        }   

        public static Error CreateError(AppUpdaterErrorCode code, string description)
        {
            return new Error(kDomain, (int)code, description);
        }
    }
}