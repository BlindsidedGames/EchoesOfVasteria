using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// This class contains the information retrieved when <see cref="AddressBook.ReadContacts(EventCallback{AddressBookReadContactsResult})"/> operation is completed.
    /// </summary>
    /// @ingroup AddressBook
    public class AddressBookReadContactsResult
    {
        #region Properties

        /// <summary>
        /// Contains the contacts details retrieved from address book.
        /// </summary>
        /// <value>If the requested operation was successful, this property holds an array of <see cref="IAddressBookContact"/> objects; otherwise, this is null.</value>
        public IAddressBookContact[] Contacts { get; private set; }


        /// <summary>
        /// Value to pass as offset value in options for reading next set/page of contacts. This value will be -1 if no more data exists.
        /// </summary>
        public int NextOffset { get; private set; }

        #endregion

        #region Constructors

        internal AddressBookReadContactsResult(IAddressBookContact[] contacts, int nextOffset)
        {
            // Set properties
            Contacts    = contacts;
            NextOffset  = nextOffset;
        }

        #endregion
    }
}