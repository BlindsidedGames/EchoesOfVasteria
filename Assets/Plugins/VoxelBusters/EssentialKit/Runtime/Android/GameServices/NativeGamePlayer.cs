#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;

namespace VoxelBusters.EssentialKit.GameServicesCore.Android
{
    public class NativeGamePlayer : NativeAndroidJavaObjectWrapper
    {
        #region Static properties

         private static AndroidJavaClass m_nativeClass;

        #endregion
        #region Constructor

        // Default constructor
        public NativeGamePlayer(AndroidJavaObject androidJavaObject) : base(Native.kClassName, androidJavaObject)
        {
        }
        public NativeGamePlayer(NativeAndroidJavaObjectWrapper wrapper) : base(wrapper)
        {
        }

#if NATIVE_PLUGINS_DEBUG_ENABLED
        ~NativeGamePlayer()
        {
            DebugLogger.Log("Disposing NativeGamePlayer");
        }
#endif
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

        public string GetDisplayName()
        {
            return Call<string>(Native.Method.kGetDisplayName);
        }
        public string GetId()
        {
            return Call<string>(Native.Method.kGetId);
        }
        public string GetName()
        {
            return Call<string>(Native.Method.kGetName);
        }
        public string GetTitle()
        {
            return Call<string>(Native.Method.kGetTitle);
        }
        public void LoadFriends(NativeActivity activity, NativeLoadPlayersListener listener)
        {
            activity.RunOnUiThread(() => {
#if NATIVE_PLUGINS_DEBUG_ENABLED
                DebugLogger.Log("[Class : NativeGamePlayer][Method(RunOnUiThread) : LoadFriends]");
#endif
                Call(Native.Method.kLoadFriends, listener);
            });
        }
        public void LoadProfileImage(bool needHighRes, NativeLoadAssetListener listener)
        {
            Call(Native.Method.kLoadProfileImage, needHighRes, listener);
        }
        public void SendFriendRequest(NativeActivity activity, string playerId, NativeViewListener listener)
        {
            activity.RunOnUiThread(() => {
#if NATIVE_PLUGINS_DEBUG_ENABLED
                DebugLogger.Log("[Class : NativeGamePlayer][Method(RunOnUiThread) : SendFriendRequest]");
#endif
                Call(Native.Method.kSendFriendRequest, playerId, listener);
            });
        }

        #endregion

        internal class Native
        {
            internal const string kClassName = "com.voxelbusters.essentialkit.gameservices.GamePlayer";

            internal class Method
            {
                internal const string kGetTitle = "getTitle";
                internal const string kGetName = "getName";
                internal const string kLoadFriends = "loadFriends";
                internal const string kGetDisplayName = "getDisplayName";
                internal const string kGetId = "getId";
                internal const string kSendFriendRequest = "SendFriendRequest";
                internal const string kLoadProfileImage = "LoadProfileImage";
            }

        }
    }
}
#endif