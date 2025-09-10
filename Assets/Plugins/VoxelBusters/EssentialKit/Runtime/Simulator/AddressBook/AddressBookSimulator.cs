using System.IO;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;
using VoxelBusters.CoreLibrary.Editor.NativePlugins;
using System.Linq;

namespace VoxelBusters.EssentialKit.AddressBookCore.Simulator
{
    public sealed class AddressBookSimulator : SingletonObject<AddressBookSimulator>
    {
	    #region Constants

	    // messages
        private 	const 	    string    					kUnauthorizedAccessError    = "Unauthorized access! You need permission to access the contacts.";

        #endregion

        #region Fields

        private     readonly    AddressBookContact[]        m_contacts			        = null;

        private     readonly    AddressBookSimulatorData    m_simulatorData             = null;

        #endregion

        #region Delegates

        public delegate void RequestContactsAccessCallback(AddressBookContactsAccessStatus accessStatus, Error error);

        public delegate void ReadContactsCallback(IAddressBookContact[] contacts, int nextOffset, Error error);
        
        #endregion

        #region Constructors

        private AddressBookSimulator()
        {
            // set properties
            m_contacts          = GetDummyContacts();
            m_simulatorData     = LoadData() ?? new AddressBookSimulatorData();
        }

        #endregion

        #region Database methods

        private AddressBookSimulatorData LoadData()
        {
            return SimulatorServices.GetObject<AddressBookSimulatorData>(NativeFeatureType.kAddressBook);
        }

        private void SaveData()
        {
            SimulatorServices.SetObject(NativeFeatureType.kAddressBook, m_simulatorData);
        }

        public static void Reset() 
        {
            SimulatorServices.RemoveObject(NativeFeatureType.kAddressBook);
        }

        #endregion

        #region Public static methods

        public AddressBookContactsAccessStatus GetContactsAccessStatus()
        {
            return m_simulatorData.ContactsAccessStatus;
        }

        public void RequestContactsAccess(RequestContactsAccessCallback callback)
        {
            // check whether required permission is already granted
            var     accessStatus    = GetContactsAccessStatus();
            if (AddressBookContactsAccessStatus.Authorized == accessStatus)
            {
                callback(AddressBookContactsAccessStatus.Authorized, null);
            }
            else
            {
                // show prompt to user asking for required permission
                var     applicationSettings = EssentialKitSettings.Instance.ApplicationSettings;
                var     usagePermission     = applicationSettings.UsagePermissionSettings.AddressBookUsagePermission;

                var     newAlertDialog      = new AlertDialogBuilder()
                    .SetTitle("Address Book Simulator")
                    .SetMessage(usagePermission.GetDescriptionForActivePlatform())
                    .AddButton("Authorise", () => 
                    { 
                        // save selection
                        m_simulatorData.ContactsAccessStatus    = AddressBookContactsAccessStatus.Authorized;
                        SaveData();

                        // send result
                        callback(AddressBookContactsAccessStatus.Authorized, null);
                    })
                    .AddCancelButton("Cancel", () => 
                    { 
                        // save selection
                        m_simulatorData.ContactsAccessStatus    = AddressBookContactsAccessStatus.Denied;
                        SaveData();
                        
                        // send result
                        callback(AddressBookContactsAccessStatus.Denied, null);
                    }).
                    Build();
                newAlertDialog.Show();
            }
        }

        public void ReadContacts(ReadContactsOptions options, ReadContactsCallback callback)
        {
            RequestContactsAccess((status, error) => {
                var     accessStatus    = GetContactsAccessStatus();
                if (AddressBookContactsAccessStatus.Authorized == accessStatus)
                {
                    var contacts = m_contacts.Skip(options.Offset).Take(options.Limit).ToArray();
                    var nextOffset  = (contacts.Length < options.Limit) ? -1 : (options.Limit + options.Offset);
                    nextOffset      = HasMoreContacts(nextOffset) ? nextOffset : -1; //Checking one entry forward so that we can know if there are any existing.
                    callback(contacts, nextOffset, null);
                }
                else
                {
                    callback(null, -1, new Error(AddressBookError.kDomain, code: (int)AddressBookErrorCode.PermissionDenied, description: kUnauthorizedAccessError));
                }
            });
        }

        #endregion

        #region Private methods

        private AddressBookContact[] GetDummyContacts()
        {
            // create fake contacts
            int     count           = 20;
            var     newContacts     = new AddressBookContact[count];
            for (int iter = 0; iter < count; iter++)
            {
                newContacts[iter]   = new AddressBookContact(
                    firstName: Path.GetRandomFileName(),
                    lastName: Path.GetRandomFileName(),
                    phoneNumbers: Random.value > 0.5f ? new string[] { (9876543200 + iter).ToString() } : new string[] {},
                    emailAddresses: Random.value > 0.5f ? new string[] { "abc" + iter.ToString() + "@xyz.com" } : new string[] {}
                    );
            }
            return newContacts;
        }

        private bool HasMoreContacts(int fromOffset)
        {
            if(fromOffset == -1)
                return false;

            var contacts = m_contacts.Skip(fromOffset).Take(1).ToArray();
            return contacts.Length > 0;
        }

        #endregion
	}
}