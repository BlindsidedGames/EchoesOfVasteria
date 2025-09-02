#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;
namespace VoxelBusters.EssentialKit.WebViewCore.Android
{
    public class NativeWebkitWebView : NativeAndroidJavaObjectWrapper
    {
        #region Static properties

         private static AndroidJavaClass m_nativeClass;

        #endregion
        #region Private properties
        private NativeActivity Activity
        {
            get;
            set;
        }
        #endregion

        #region Constructor

        public NativeWebkitWebView(NativeContext context) : base(Native.kClassName, (object)context.NativeObject)
        {
            Activity    = new NativeActivity(context);
        }

        #endregion
        #region Static methods
        private static AndroidJavaClass GetClass()
        {
            if (m_nativeClass == null)
            {
                m_nativeClass = new AndroidJavaClass(Native.kClassName);
            }
            return m_nativeClass;
        }
        #endregion
        #region Public methods

        public void AddNewScheme(string scheme)
        {
            Activity.RunOnUiThread(() => {
#if NATIVE_PLUGINS_DEBUG_ENABLED
                DebugLogger.Log("[Class : NativeWebkitWebView][Method(RunOnUiThread) : AddNewScheme]");
#endif
                Call(Native.Method.kAddNewScheme, new object[] { scheme } );
            });
        }
        public void AdjustLayout()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeWebkitWebView][Method : AdjustLayout]");
#endif
            Call(Native.Method.kAdjustLayout);
        }
        public void ClearCache()
        {
            Activity.RunOnUiThread(() => {
#if NATIVE_PLUGINS_DEBUG_ENABLED
                DebugLogger.Log("[Class : NativeWebkitWebView][Method(RunOnUiThread) : ClearCache]");
#endif
                Call(Native.Method.kClearCache);
            });
        }
        public void ClearCookies()
        {
            Activity.RunOnUiThread(() => {
#if NATIVE_PLUGINS_DEBUG_ENABLED
                DebugLogger.Log("[Class : NativeWebkitWebView][Method(RunOnUiThread) : ClearCookies]");
#endif
                Call(Native.Method.kClearCookies);
            });
        }
        public void Destroy()
        {
            Activity.RunOnUiThread(() => {
#if NATIVE_PLUGINS_DEBUG_ENABLED
                DebugLogger.Log("[Class : NativeWebkitWebView][Method(RunOnUiThread) : Destroy]");
#endif
                Call(Native.Method.kDestroy);
            });
        }
        public void EvaluateJavaScriptFromString(string jsScript, NativeEvaluateJavaScriptListener listener)
        {
            Activity.RunOnUiThread(() => {
#if NATIVE_PLUGINS_DEBUG_ENABLED
                DebugLogger.Log("[Class : NativeWebkitWebView][Method(RunOnUiThread) : EvaluateJavaScriptFromString]");
#endif
                Call(Native.Method.kEvaluateJavaScriptFromString, new object[] { jsScript, listener } );
            });
        }
        public string GetFeatureName()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeWebkitWebView][Method : GetFeatureName]");
#endif
            return Call<string>(Native.Method.kGetFeatureName);
        }
        public double GetProgress()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeWebkitWebView][Method : GetProgress]");
#endif
            return Call<double>(Native.Method.kGetProgress);
        }
        public string GetTitle()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeWebkitWebView][Method : GetTitle]");
#endif
            return Call<string>(Native.Method.kGetTitle);
        }
        public string GetUrl()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeWebkitWebView][Method : GetUrl]");
#endif
            return Call<string>(Native.Method.kGetUrl);
        }
        public void Hide()
        {
            Activity.RunOnUiThread(() => {
#if NATIVE_PLUGINS_DEBUG_ENABLED
                DebugLogger.Log("[Class : NativeWebkitWebView][Method(RunOnUiThread) : Hide]");
#endif
                Call(Native.Method.kHide);
            });
        }
        public bool IsLoading()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeWebkitWebView][Method : IsLoading]");
#endif
            return Call<bool>(Native.Method.kIsLoading);
        }
        public void LoadData(NativeBytesWrapper data, int length, string mimeType, string encoding, string baseUrl)
        {
            Activity.RunOnUiThread(() => {
#if NATIVE_PLUGINS_DEBUG_ENABLED
                DebugLogger.Log("[Class : NativeWebkitWebView][Method(RunOnUiThread) : LoadData]");
#endif
                Call(Native.Method.kLoadData, new object[] { data?.NativeObject, length, mimeType, encoding, baseUrl } );
            });
        }
        public void LoadHtmlString(string html, string baseUrl)
        {
            Activity.RunOnUiThread(() => {
#if NATIVE_PLUGINS_DEBUG_ENABLED
                DebugLogger.Log("[Class : NativeWebkitWebView][Method(RunOnUiThread) : LoadHtmlString]");
#endif
                Call(Native.Method.kLoadHtmlString, new object[] { html, baseUrl } );
            });
        }
        public void LoadUrl(string url)
        {
            Activity.RunOnUiThread(() => {
#if NATIVE_PLUGINS_DEBUG_ENABLED
                DebugLogger.Log("[Class : NativeWebkitWebView][Method(RunOnUiThread) : LoadUrl]");
#endif
                Call(Native.Method.kLoadUrl, new object[] { url } );
            });
        }
        public void Reload()
        {
            Activity.RunOnUiThread(() => {
#if NATIVE_PLUGINS_DEBUG_ENABLED
                DebugLogger.Log("[Class : NativeWebkitWebView][Method(RunOnUiThread) : Reload]");
#endif
                Call(Native.Method.kReload);
            });
        }
        public void RemoveScheme(string scheme)
        {
            Activity.RunOnUiThread(() => {
#if NATIVE_PLUGINS_DEBUG_ENABLED
                DebugLogger.Log("[Class : NativeWebkitWebView][Method(RunOnUiThread) : RemoveScheme]");
#endif
                Call(Native.Method.kRemoveScheme, new object[] { scheme } );
            });
        }
        public void SetBackgroundColor(float red, float green, float blue, float alpha)
        {
            Activity.RunOnUiThread(() => {
#if NATIVE_PLUGINS_DEBUG_ENABLED
                DebugLogger.Log("[Class : NativeWebkitWebView][Method(RunOnUiThread) : SetBackgroundColor]");
#endif
                Call(Native.Method.kSetBackgroundColor, new object[] { red, green, blue, alpha } );
            });
        }
        public void SetBounce(bool canBounce)
        {
            Activity.RunOnUiThread(() => {
#if NATIVE_PLUGINS_DEBUG_ENABLED
                DebugLogger.Log("[Class : NativeWebkitWebView][Method(RunOnUiThread) : SetBounce]");
#endif
                Call(Native.Method.kSetBounce, new object[] { canBounce } );
            });
        }
        public void SetCanGoBack(bool canGoBack)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeWebkitWebView][Method : SetCanGoBack]");
#endif
            Call(Native.Method.kSetCanGoBack, new object[] { canGoBack } );
        }
        public void SetFrame(float x, float y, float width, float height)
        {
            Activity.RunOnUiThread(() => {
#if NATIVE_PLUGINS_DEBUG_ENABLED
                DebugLogger.Log("[Class : NativeWebkitWebView][Method(RunOnUiThread) : SetFrame]");
#endif
                Call(Native.Method.kSetFrame, new object[] { x, y, width, height } );
            });
        }
        public void SetJavaScriptEnabled(bool enable)
        {
            Activity.RunOnUiThread(() => {
#if NATIVE_PLUGINS_DEBUG_ENABLED
                DebugLogger.Log("[Class : NativeWebkitWebView][Method(RunOnUiThread) : SetJavaScriptEnabled]");
#endif
                Call(Native.Method.kSetJavaScriptEnabled, new object[] { enable } );
            });
        }
        public void SetNavigation(bool canGoBack, bool canGoForward)
        {
            Activity.RunOnUiThread(() => {
#if NATIVE_PLUGINS_DEBUG_ENABLED
                DebugLogger.Log("[Class : NativeWebkitWebView][Method(RunOnUiThread) : SetNavigation]");
#endif
                Call(Native.Method.kSetNavigation, new object[] { canGoBack, canGoForward } );
            });
        }
        public void SetScalesPageToFit(bool scaleToFit)
        {
            Activity.RunOnUiThread(() => {
#if NATIVE_PLUGINS_DEBUG_ENABLED
                DebugLogger.Log("[Class : NativeWebkitWebView][Method(RunOnUiThread) : SetScalesPageToFit]");
#endif
                Call(Native.Method.kSetScalesPageToFit, new object[] { scaleToFit } );
            });
        }
        public void SetStyle(NativeWebKitWebViewStyle style)
        {
            Activity.RunOnUiThread(() => {
#if NATIVE_PLUGINS_DEBUG_ENABLED
                DebugLogger.Log("[Class : NativeWebkitWebView][Method(RunOnUiThread) : SetStyle]");
#endif
                Call(Native.Method.kSetStyle, new object[] { NativeWebKitWebViewStyleHelper.CreateWithValue(style) } );
            });
        }
        public void SetViewListener(NativeWebViewListener viewListener)
        {
            Activity.RunOnUiThread(() => {
#if NATIVE_PLUGINS_DEBUG_ENABLED
                DebugLogger.Log("[Class : NativeWebkitWebView][Method(RunOnUiThread) : SetViewListener]");
#endif
                Call(Native.Method.kSetViewListener, new object[] { viewListener } );
            });
        }
        public void SetZoom(bool enable)
        {
            Activity.RunOnUiThread(() => {
#if NATIVE_PLUGINS_DEBUG_ENABLED
                DebugLogger.Log("[Class : NativeWebkitWebView][Method(RunOnUiThread) : SetZoom]");
#endif
                Call(Native.Method.kSetZoom, new object[] { enable } );
            });
        }
        public void Show()
        {
            Activity.RunOnUiThread(() => {
#if NATIVE_PLUGINS_DEBUG_ENABLED
                DebugLogger.Log("[Class : NativeWebkitWebView][Method(RunOnUiThread) : Show]");
#endif
                Call(Native.Method.kShow);
            });
        }
        public void StopLoading()
        {
            Activity.RunOnUiThread(() => {
#if NATIVE_PLUGINS_DEBUG_ENABLED
                DebugLogger.Log("[Class : NativeWebkitWebView][Method(RunOnUiThread) : StopLoading]");
#endif
                Call(Native.Method.kStopLoading);
            });
        }

        #endregion

        internal class Native
        {
            internal const string kClassName = "com.voxelbusters.essentialkit.webview.WebkitWebView";

            internal class Method
            {
                internal const string kGetTitle = "getTitle";
                internal const string kSetStyle = "setStyle";
                internal const string kSetFrame = "setFrame";
                internal const string kLoadData = "loadData";
                internal const string kRemoveScheme = "removeScheme";
                internal const string kAdjustLayout = "adjustLayout";
                internal const string kClearCookies = "clearCookies";
                internal const string kAddNewScheme = "addNewScheme";
                internal const string kSetCanGoBack = "setCanGoBack";
                internal const string kSetJavaScriptEnabled = "setJavaScriptEnabled";
                internal const string kSetNavigation = "setNavigation";
                internal const string kStopLoading = "stopLoading";
                internal const string kGetProgress = "getProgress";
                internal const string kLoadUrl = "loadUrl";
                internal const string kSetZoom = "setZoom";
                internal const string kDestroy = "destroy";
                internal const string kSetBackgroundColor = "setBackgroundColor";
                internal const string kSetScalesPageToFit = "setScalesPageToFit";
                internal const string kClearCache = "clearCache";
                internal const string kIsLoading = "isLoading";
                internal const string kSetBounce = "setBounce";
                internal const string kGetFeatureName = "getFeatureName";
                internal const string kLoadHtmlString = "loadHtmlString";
                internal const string kSetViewListener = "setViewListener";
                internal const string kReload = "reload";
                internal const string kGetUrl = "getUrl";
                internal const string kEvaluateJavaScriptFromString = "evaluateJavaScriptFromString";
                internal const string kHide = "hide";
                internal const string kShow = "show";
            }

        }
    }
}
#endif