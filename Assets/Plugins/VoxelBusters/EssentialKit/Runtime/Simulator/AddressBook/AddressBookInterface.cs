using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VoxelBusters.EssentialKit.AddressBookCore.Simulator
{
    public sealed class AddressBookInterface : NativeAddressBookInterfaceBase, INativeAddressBookInterface
    {
        #region Constructors

        public AddressBookInterface()
            : base(isAvailable: true)
        { }

        #endregion

        #region Base methods

        public override AddressBookContactsAccessStatus GetContactsAccessStatus()
        {
            return AddressBookSimulator.Instance.GetContactsAccessStatus();
        }

        public override void ReadContacts(ReadContactsOptions optons, ReadContactsInternalCallback callback)
        {
            AddressBookSimulator.Instance.ReadContacts(optons, (contacts, nextOffset, error) => callback(contacts, nextOffset, error));
        }

        #endregion
    }
}