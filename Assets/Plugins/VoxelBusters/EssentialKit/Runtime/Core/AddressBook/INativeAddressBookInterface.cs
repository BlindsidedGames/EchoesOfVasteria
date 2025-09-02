using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.AddressBookCore
{
    public interface INativeAddressBookInterface : INativeFeatureInterface
    {
        #region Methods

        AddressBookContactsAccessStatus GetContactsAccessStatus();
                
        void ReadContacts(ReadContactsOptions options, ReadContactsInternalCallback callback);

        #endregion
    }
}