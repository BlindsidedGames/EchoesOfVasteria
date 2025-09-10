#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;

namespace VoxelBusters.EasyMLKit.NativePlugins.Android
{
    public class NativeRectF : NativeAndroidJavaObjectWrapper
    {
        #region Static properties

         private static AndroidJavaClass m_nativeClass;

        #endregion
        #region Public properties

        public float Bottom
        {
            get
            {
                return Get<float>("bottom");
            }

            set
            {
                Set<float>("bottom", value);
            }
        }


        public float Left
        {
            get
            {
                return Get<float>("left");
            }

            set
            {
                Set<float>("left", value);
            }
        }


        public float Right
        {
            get
            {
                return Get<float>("right");
            }

            set
            {
                Set<float>("right", value);
            }
        }


        public float Top
        {
            get
            {
                return Get<float>("top");
            }

            set
            {
                Set<float>("top", value);
            }
        }


        public const int CONTENTS_FILE_DESCRIPTOR = 1;

        public const int PARCELABLE_WRITE_RETURN_VALUE = 1;

        #endregion

        #region Constructor

        // Wrapper constructors
        public NativeRectF(AndroidJavaObject androidJavaObject) : base(Native.kClassName, androidJavaObject)
        {
        }

        public NativeRectF(NativeAndroidJavaObjectWrapper wrapper) : base(wrapper)
        {
        }

        public NativeRectF() : base(Native.kClassName)
        {
        }

#if NATIVE_PLUGINS_DEBUG
        ~NativeRectF()
        {
            DebugLogger.Log("Disposing NativeRectF");
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
        public static bool Intersects(NativeRectF arg0, NativeRectF arg1)
        {
            return GetClass().CallStatic<bool>(Native.Method.kIntersects, arg0.NativeObject, arg1.NativeObject);
        }

        #endregion
        #region Public methods

        public bool Contains(NativeRectF arg0)
        {
            return Call<bool>(Native.Method.kContains, arg0.NativeObject);
        }
        public bool Contains(float arg0, float arg1)
        {
            return Call<bool>(Native.Method.kContains, arg0, arg1);
        }
        public bool Contains(float arg0, float arg1, float arg2, float arg3)
        {
            return Call<bool>(Native.Method.kContains, arg0, arg1, arg2, arg3);
        }
        public int DescribeContents()
        {
            return Call<int>(Native.Method.kDescribeContents);
        }
        public bool Equals(NativeObject arg0)
        {
            return Call<bool>(Native.Method.kEquals, arg0.NativeObject);
        }
        public int HashCode()
        {
            return Call<int>(Native.Method.kHashCode);
        }
        public void Inset(float arg0, float arg1)
        {
            Call(Native.Method.kInset, arg0, arg1);
        }
        public bool Intersect(float arg0, float arg1, float arg2, float arg3)
        {
            return Call<bool>(Native.Method.kIntersect, arg0, arg1, arg2, arg3);
        }
        public bool Intersect(NativeRectF arg0)
        {
            return Call<bool>(Native.Method.kIntersect, arg0.NativeObject);
        }
        public bool Intersects(float arg0, float arg1, float arg2, float arg3)
        {
            return Call<bool>(Native.Method.kIntersects, arg0, arg1, arg2, arg3);
        }
        public void Offset(float arg0, float arg1)
        {
            Call(Native.Method.kOffset, arg0, arg1);
        }
        public void OffsetTo(float arg0, float arg1)
        {
            Call(Native.Method.kOffsetTo, arg0, arg1);
        }
        public void ReadFromParcel(NativeParcel arg0)
        {
            Call(Native.Method.kReadFromParcel, arg0.NativeObject);
        }
        public void Round(NativeRect arg0)
        {
            Call(Native.Method.kRound, arg0.NativeObject);
        }
        public void RoundOut(NativeRect arg0)
        {
            Call(Native.Method.kRoundOut, arg0.NativeObject);
        }
        public void Set(NativeRectF arg0)
        {
            Call(Native.Method.kSet, arg0.NativeObject);
        }
        public void Set(NativeRect arg0)
        {
            Call(Native.Method.kSet, arg0.NativeObject);
        }
        public void Set(float arg0, float arg1, float arg2, float arg3)
        {
            Call(Native.Method.kSet, arg0, arg1, arg2, arg3);
        }
        public void SetEmpty()
        {
            Call(Native.Method.kSetEmpty);
        }
        public bool SetIntersect(NativeRectF arg0, NativeRectF arg1)
        {
            return Call<bool>(Native.Method.kSetIntersect, arg0.NativeObject, arg1.NativeObject);
        }
        public void Sort()
        {
            Call(Native.Method.kSort);
        }
        public string ToShortString()
        {
            return Call<string>(Native.Method.kToShortString);
        }
        public new string ToString()
        {
            return Call<string>(Native.Method.kToString);
        }
        public void Union(float arg0, float arg1, float arg2, float arg3)
        {
            Call(Native.Method.kUnion, arg0, arg1, arg2, arg3);
        }
        public void Union(float arg0, float arg1)
        {
            Call(Native.Method.kUnion, arg0, arg1);
        }
        public void Union(NativeRectF arg0)
        {
            Call(Native.Method.kUnion, arg0.NativeObject);
        }
        public void WriteToParcel(NativeParcel arg0, int arg1)
        {
            Call(Native.Method.kWriteToParcel, arg0.NativeObject, arg1);
        }

        #endregion

        internal class Native
        {
            internal const string kClassName = "android.graphics.RectF";

            internal class Method
            {
                internal const string kToString = "toString";
                internal const string kContains = "contains";
                internal const string kHashCode = "hashCode";
                internal const string kRoundOut = "roundOut";
                internal const string kSetEmpty = "setEmpty";
                internal const string kOffsetTo = "offsetTo";
                internal const string kSetIntersect = "setIntersect";
                internal const string kWriteToParcel = "writeToParcel";
                internal const string kToShortString = "toShortString";
                internal const string kIntersects = "intersects";
                internal const string kIntersect = "intersect";
                internal const string kReadFromParcel = "readFromParcel";
                internal const string kOffset = "offset";
                internal const string kEquals = "equals";
                internal const string kDescribeContents = "describeContents";
                internal const string kRound = "round";
                internal const string kUnion = "union";
                internal const string kInset = "inset";
                internal const string kSort = "sort";
                internal const string kSet = "set";
            }

        }
    }
}
#endif