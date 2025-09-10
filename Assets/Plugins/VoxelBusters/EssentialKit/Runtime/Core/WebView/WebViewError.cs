using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    public class WebViewError
    {
        public const string kDomain = "WebView";

        public static Error Unknown(string description = null) => CreateError(
            code: (int)SharingServicesErrorCode.Unknown,
            description: description ?? "Unknown error."
        );

        private static Error CreateError(int code, string description) => new Error(
            domain: kDomain,
            code: code,
            description: description);
    }
}