#if UNITY_IOS || UNITY_TVOS
using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace VoxelBusters.EssentialKit.AddressBookCore.iOS
{
    internal static class AddressBookBinding
    {
  
        [DllImport("__Internal")]
        public static extern CNAuthorizationStatus NPAddressBookGetAuthorizationStatus();
        
        [DllImport("__Internal")]
        public static extern void NPAddressBookReadContacts(NativeReadContactsOptionsData options, IntPtr tagPtr, ReadContactsNativeCallback readContactsCallback);       
    }
}
#endif