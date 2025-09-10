using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    public class NotificationServicesError
    {
        public const string kDomain = "[Essential Kit] Notification Services";

        public static Error Unknown(string description = null) => CreateError(
            code: (int)NotificationServicesErrorCode.Unknown,
            description: description ?? "Unknown error."
        );

        public static Error PermissionNotAvailable(string description = null) => CreateError(
            code: (int)NotificationServicesErrorCode.PermissionNotAvailable,
            description: description ?? "Permission not available."
        );

        public static Error TriggerNotValid(string description = null) => CreateError(
            code: (int)NotificationServicesErrorCode.TriggerNotValid,
            description: description ?? "Trigger not valid."
        );

        public static Error ConfigurationError(string description = null) => CreateError(
            code: (int)NotificationServicesErrorCode.ConfigurationError,
            description: description ?? "Configuration error."
        );

        public static Error ScheduledTimeNotValid(string description = null) => CreateError(
            code: (int)NotificationServicesErrorCode.ScheduledTimeNotValid,
            description: description ?? "Scheduled time not valid."
        );

        private static Error CreateError(int code, string description) => new Error(
            domain: kDomain,
            code: code,
            description: description);
    }
}