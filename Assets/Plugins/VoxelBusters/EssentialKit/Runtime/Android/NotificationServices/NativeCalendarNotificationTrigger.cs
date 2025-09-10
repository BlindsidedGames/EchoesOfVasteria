#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;

namespace VoxelBusters.EssentialKit.NotificationServicesCore.Android
{
    public class NativeCalendarNotificationTrigger : NativeAndroidJavaObjectWrapper
    {
        #region Static properties

         private static AndroidJavaClass m_nativeClass;

        #endregion
        #region Public properties

        public bool Repeat
        {
            get
            {
                return Get<bool>("repeat");
            }

            set
            {
                Set<bool>("repeat", value);
            }
        }

        #endregion
        #region Constructor

        // Default constructor
        public NativeCalendarNotificationTrigger(AndroidJavaObject androidJavaObject) : base(Native.kClassName, androidJavaObject)
        {
        }
        public NativeCalendarNotificationTrigger(NativeAndroidJavaObjectWrapper wrapper) : base(wrapper)
        {
        }
        public NativeCalendarNotificationTrigger(int calendarType, bool repeat) : base(Native.kClassName ,(object)calendarType, (object)repeat)
        {
        }

#if NATIVE_PLUGINS_DEBUG_ENABLED
        ~NativeCalendarNotificationTrigger()
        {
            DebugLogger.Log("Disposing NativeCalendarNotificationTrigger");
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
        public static NativeCalendarNotificationTrigger FromJson(NativeJSONObject jsonObject)
        {
            AndroidJavaObject nativeObj = GetClass().CallStatic<AndroidJavaObject>(Native.Method.kFromJson, jsonObject.NativeObject);
            NativeCalendarNotificationTrigger data  = new  NativeCalendarNotificationTrigger(nativeObj);
            return data;
        }

        #endregion
        #region Public methods

        public int GetCalendarType()
        {
            return Call<int>(Native.Method.kGetCalendarType);
        }
        public int GetDay()
        {
            return Call<int>(Native.Method.kGetDay);
        }
        public int GetHour()
        {
            return Call<int>(Native.Method.kGetHour);
        }
        public int GetMinute()
        {
            return Call<int>(Native.Method.kGetMinute);
        }
        public int GetMonth()
        {
            return Call<int>(Native.Method.kGetMonth);
        }
        public int GetNanosecond()
        {
            return Call<int>(Native.Method.kGetNanosecond);
        }
        public NativeDate GetNextTriggerDate()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetNextTriggerDate);
            NativeDate data  = new  NativeDate(nativeObj);
            return data;
        }
        public int GetSecond()
        {
            return Call<int>(Native.Method.kGetSecond);
        }
        public int GetWeekOfMonth()
        {
            return Call<int>(Native.Method.kGetWeekOfMonth);
        }
        public int GetWeekOfYear()
        {
            return Call<int>(Native.Method.kGetWeekOfYear);
        }
        public int GetWeekday()
        {
            return Call<int>(Native.Method.kGetWeekday);
        }
        public int GetYear()
        {
            return Call<int>(Native.Method.kGetYear);
        }
        public void SetDay(int day)
        {
            Call(Native.Method.kSetDay, day);
        }
        public void SetHour(int hour)
        {
            Call(Native.Method.kSetHour, hour);
        }
        public void SetMinute(int minute)
        {
            Call(Native.Method.kSetMinute, minute);
        }
        public void SetMonth(int month)
        {
            Call(Native.Method.kSetMonth, month);
        }
        public void SetNanosecond(int nanosecond)
        {
            Call(Native.Method.kSetNanosecond, nanosecond);
        }
        public void SetSecond(int second)
        {
            Call(Native.Method.kSetSecond, second);
        }
        public void SetWeekOfMonth(int weekOfMonth)
        {
            Call(Native.Method.kSetWeekOfMonth, weekOfMonth);
        }
        public void SetWeekOfYear(int weekOfYear)
        {
            Call(Native.Method.kSetWeekOfYear, weekOfYear);
        }
        public void SetWeekday(int weekday)
        {
            Call(Native.Method.kSetWeekday, weekday);
        }
        public void SetYear(int year)
        {
            Call(Native.Method.kSetYear, year);
        }
        public NativeJSONObject ToJson()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kToJson);
            NativeJSONObject data  = new  NativeJSONObject(nativeObj);
            return data;
        }

        #endregion

        internal class Native
        {
            internal const string kClassName = "com.voxelbusters.essentialkit.notificationservices.datatypes.CalendarNotificationTrigger";

            internal class Method
            {
                internal const string kGetCalendarType = "GetCalendarType";
                internal const string kSetMonth = "setMonth";
                internal const string kFromJson = "fromJson";
                internal const string kGetMonth = "getMonth";
                internal const string kGetNanosecond = "getNanosecond";
                internal const string kGetWeekOfYear = "getWeekOfYear";
                internal const string kSetNanosecond = "setNanosecond";
                internal const string kSetWeekOfYear = "setWeekOfYear";
                internal const string kSetHour = "setHour";
                internal const string kSetYear = "setYear";
                internal const string kGetHour = "getHour";
                internal const string kGetYear = "getYear";
                internal const string kGetNextTriggerDate = "getNextTriggerDate";
                internal const string kSetWeekday = "setWeekday";
                internal const string kSetMinute = "setMinute";
                internal const string kSetSecond = "setSecond";
                internal const string kGetWeekday = "getWeekday";
                internal const string kGetSecond = "getSecond";
                internal const string kGetMinute = "getMinute";
                internal const string kSetWeekOfMonth = "setWeekOfMonth";
                internal const string kGetWeekOfMonth = "getWeekOfMonth";
                internal const string kSetDay = "setDay";
                internal const string kToJson = "toJson";
                internal const string kGetDay = "getDay";
            }

        }
    }
}
#endif