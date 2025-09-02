#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;

namespace VoxelBusters.EssentialKit.GameServicesCore.Android
{
    public class NativeGameLeaderboardScore : NativeAndroidJavaObjectWrapper
    {
        #region Static properties

         private static AndroidJavaClass m_nativeClass;

        #endregion
        #region Constructor

        // Default constructor
        public NativeGameLeaderboardScore(AndroidJavaObject androidJavaObject) : base(Native.kClassName, androidJavaObject)
        {
        }
        public NativeGameLeaderboardScore(NativeAndroidJavaObjectWrapper wrapper) : base(wrapper)
        {
        }

#if NATIVE_PLUGINS_DEBUG_ENABLED
        ~NativeGameLeaderboardScore()
        {
            DebugLogger.Log("Disposing NativeGameLeaderboardScore");
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

        public NativeDate GetLastReportedDate()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetLastReportedDate);
            NativeDate data  = new  NativeDate(nativeObj);
            return data;
        }
        public string GetLeaderboardId()
        {
            return Call<string>(Native.Method.kGetLeaderboardId);
        }
        public NativeGamePlayer GetPlayer()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetPlayer);
            NativeGamePlayer data  = new  NativeGamePlayer(nativeObj);
            return data;
        }
        public long GetRank()
        {
            return Call<long>(Native.Method.kGetRank);
        }
        public long GetRawScore()
        {
            return Call<long>(Native.Method.kGetRawScore);
        }
        public string GetTag()
        {
            return Call<string>(Native.Method.kGetTag);
        }

        #endregion

        internal class Native
        {
            internal const string kClassName = "com.voxelbusters.essentialkit.gameservices.GameLeaderboardScore";

            internal class Method
            {
                internal const string kGetLastReportedDate = "getLastReportedDate";
                internal const string kGetRawScore = "getRawScore";
                internal const string kGetRank = "getRank";
                internal const string kGetPlayer = "getPlayer";
                internal const string kGetTag = "getTag";
                internal const string kGetLeaderboardId = "getLeaderboardId";
            }

        }
    }
}
#endif