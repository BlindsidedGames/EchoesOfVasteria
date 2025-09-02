#if UNITY_IOS || UNITY_TVOS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.AddressBookCore.iOS
{
    public sealed class AddressBookInterface : NativeAddressBookInterfaceBase, INativeAddressBookInterface
    {
        #region Constructors

        static AddressBookInterface()
        {
        }

        public AddressBookInterface()
            : base(isAvailable: true)
        { }

        #endregion

        #region Base class methods

        public override AddressBookContactsAccessStatus GetContactsAccessStatus()
        {
            var     authorizationStatus     = AddressBookBinding.NPAddressBookGetAuthorizationStatus();
            var     accessStatus            = AddressBookUtility.ConvertToAddressBookContactsAccessStatus(authorizationStatus);

            return accessStatus;
        }

        public override void ReadContacts(ReadContactsOptions options, ReadContactsInternalCallback callback)
        {
            // make call
            var     tagPtr  = MarshalUtility.GetIntPtr(callback);
            AddressBookBinding.NPAddressBookReadContacts(AddressBookUtility.From(options), tagPtr, HandleReadContactsCallbackInternal);
        }

        #endregion

        #region Native callback methods

        [MonoPInvokeCallback(typeof(ReadContactsNativeCallback))]
        private static void HandleReadContactsCallbackInternal(IntPtr contactsPtr, int count, int nextOffset, NativeError nativeError, IntPtr tagPtr)
        {
            var     tagHandle       = GCHandle.FromIntPtr(tagPtr);

            try
            {
                // send result
                var     contacts    = AddressBookUtility.ConvertNativeDataArrayToContactsArray(contactsPtr, count);
                var     errorObj    = nativeError.Convert(AddressBookError.kDomain);
                ((ReadContactsInternalCallback)tagHandle.Target).Invoke(contacts, nextOffset, errorObj);
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
            }
            finally
            {
                // release handle
                tagHandle.Free();
            }
        }

        #endregion
    }
}
#endif