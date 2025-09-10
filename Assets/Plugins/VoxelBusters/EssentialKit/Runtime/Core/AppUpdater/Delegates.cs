using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit.AppUpdaterCore
{
    public delegate void GetContactsAccessStatusInternalCallback(AddressBookContactsAccessStatus accessStatus);

    public delegate void ReadContactsInternalCallback(IAddressBookContact[] contacts, int nextOffset, Error error);
}