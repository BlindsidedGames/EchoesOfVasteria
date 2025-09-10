#if UNITY_ANDROID
using System.Collections.Generic;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;

namespace VoxelBusters.EssentialKit.AddressBookCore.Android
{
    public sealed class AddressBookInterface : NativeAddressBookInterfaceBase
    {
#region Private fields

        private NativeAddressBook m_instance;

#endregion

#region Constructors

        public AddressBookInterface() : base(isAvailable: true)
        {

            m_instance = new NativeAddressBook(NativeUnityPluginUtility.GetContext());
        }

#endregion


#region Public methods

        public override AddressBookContactsAccessStatus GetContactsAccessStatus()
        {
            return m_instance.IsAuthorized() ? AddressBookContactsAccessStatus.Authorized : AddressBookContactsAccessStatus.Denied;
        }

        public override void ReadContacts(ReadContactsOptions options, ReadContactsInternalCallback callback)
        {
            NativeAddressBookReadOptions nativeOptions = new NativeAddressBookReadOptions
            {
                Limit = options.Limit,
                Offset = options.Offset,
                Constraints = (int)options.Constraints
            };

            m_instance.ReadContacts(nativeOptions, new NativeReadContactsListener()
            {
                onSuccessCallback = (NativeList<NativeAddressBookContact> nativeList, int nextOffset) =>
                {
                    AddressBookContact[] contacts = NativeUnityPluginUtility.Map<NativeAddressBookContact, AddressBookContact>(nativeList.Get());
                    callback(contacts, nextOffset, null);
                },
                onFailureCallback = (NativeErrorInfo errorInfo) =>
                {
                    callback(null, -1, errorInfo.Convert(AddressBookError.kDomain));
                }
            });
        }
 #endregion
    }
}
#endif