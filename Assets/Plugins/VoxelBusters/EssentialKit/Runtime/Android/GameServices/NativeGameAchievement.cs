#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;

namespace VoxelBusters.EssentialKit.GameServicesCore.Android
{
    public class NativeGameAchievement : NativeAndroidJavaObjectWrapper
    {
        #region Static properties

         private static AndroidJavaClass m_nativeClass;

        #endregion
        #region Constructor

        // Default constructor
        public NativeGameAchievement(AndroidJavaObject androidJavaObject) : base(Native.kClassName, androidJavaObject)
        {
        }
        public NativeGameAchievement(NativeAndroidJavaObjectWrapper wrapper) : base(wrapper)
        {
        }

#if NATIVE_PLUGINS_DEBUG_ENABLED
        ~NativeGameAchievement()
        {
            DebugLogger.Log("Disposing NativeGameAchievement");
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

        public int GetCurrentSteps()
        {
            return Call<int>(Native.Method.kGetCurrentSteps);
        }
        public string GetDescription()
        {
            return Call<string>(Native.Method.kGetDescription);
        }
        public string GetId()
        {
            return Call<string>(Native.Method.kGetId);
        }
        public NativeDate GetLastReportedDate()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetLastReportedDate);
            NativeDate data  = new  NativeDate(nativeObj);
            return data;
        }
        public string GetName()
        {
            return Call<string>(Native.Method.kGetName);
        }
        public int GetTotalSteps()
        {
            return Call<int>(Native.Method.kGetTotalSteps);
        }
        public bool IsHidden()
        {
            return Call<bool>(Native.Method.kIsHidden);
        }
        public bool IsRevealed()
        {
            return Call<bool>(Native.Method.kIsRevealed);
        }
        public bool IsUnlocked()
        {
            return Call<bool>(Native.Method.kIsUnlocked);
        }
        public void LoadRevealedImage(NativeLoadAssetListener listener)
        {
            Call(Native.Method.kLoadRevealedImage, listener);
        }
        public void LoadUnlockedImage(NativeLoadAssetListener listener)
        {
            Call(Native.Method.kLoadUnlockedImage, listener);
        }
        public void ReportProgress(int stepsToSet, NativeReportProgressListener listener)
        {
            Call(Native.Method.kReportProgress, stepsToSet, listener);
        }

        #endregion

        internal class Native
        {
            internal const string kClassName = "com.voxelbusters.essentialkit.gameservices.GameAchievement";

            internal class Method
            {
                internal const string kIsHidden = "isHidden";
                internal const string kGetTotalSteps = "getTotalSteps";
                internal const string kGetLastReportedDate = "getLastReportedDate";
                internal const string kGetName = "getName";
                internal const string kIsUnlocked = "isUnlocked";
                internal const string kIsRevealed = "isRevealed";
                internal const string kGetDescription = "getDescription";
                internal const string kReportProgress = "reportProgress";
                internal const string kGetCurrentSteps = "getCurrentSteps";
                internal const string kLoadUnlockedImage = "loadUnlockedImage";
                internal const string kLoadRevealedImage = "loadRevealedImage";
                internal const string kGetId = "getId";
            }

        }
    }
}
#endif