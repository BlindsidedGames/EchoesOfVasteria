using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    public static class AppShortcutsError
    {

        public const string kDomain = "Task Services";

        public static Error Unknown => CreateError(AppShortcutsErrorCode.Unknown, "Unknown error");

        private static Error CreateError(AppShortcutsErrorCode code, string description)
        {
            return new Error(kDomain, (int)code, description);
        }
    }
}