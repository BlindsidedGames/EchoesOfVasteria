#if UNITY_ANDROID
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelBusters.CoreLibrary.NativePlugins.Android
{
    public class NativeArrayBuffer<T> : NativeAndroidJavaObjectWrapper
    {
        public NativeArrayBuffer(AndroidJavaObject androidJavaObject) : base("com.voxelbusters.android.essentialkit.common.ArrayBuffer", androidJavaObject)
        {
        }

        public int Size()
        {
            return m_nativeObject.Call<int>("size");
        }

        public T Get(int index)
        {
            if (m_nativeObject == null)
                return default(T);

            T instance;
            if(IsStringOrPrimitive(typeof(T)))
            {
                instance = Call<T>("get", index);
            }
            else
            {
                AndroidJavaObject androidJavaObject = Call<AndroidJavaObject>("get", index);
                instance = (T)Activator.CreateInstance(typeof(T), new object[] { androidJavaObject });
            }
            
            return instance;        
        }

        public T[] GetArray()
        {
            if (NativeObject == null)
                return default(T[]);

            List<T> list = new List<T>();
            int size = Size();
            for (int i = 0; i < size; i++)
            {
                T each = Get(i);
                list.Add(each);
            }

            return list.ToArray();
        }

        private static bool IsStringOrPrimitive(Type type)
        {
            // Check if the type is string
            if (type == typeof(string))
            {
                return true;
            }

            // Check if the type is a primitive type
            if (type.IsPrimitive)
            {
                return true;
            }

            return false;
        }
    }
}
#endif