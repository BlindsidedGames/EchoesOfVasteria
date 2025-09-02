using System;
using UnityEngine;
using UnityEngine.UI;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.DemoKit;

// internal namespace
namespace VoxelBusters.EssentialKit.Demo
{
    public class AddressBookDemo : DemoActionPanelBase<AddressBookDemoAction, AddressBookDemoActionType>
    {
        #region Fields

        [Header("Read Contacts Options")]
        [SerializeField]
        private Dropdown m_limit;

        [SerializeField]
        private Toggle m_mustIncludeNameOption;

        [SerializeField]
        private Toggle m_mustIncludeEmailOption;

        [SerializeField]
        private Toggle m_mustIncludePhoneNumberOption;

        [Space]
        [Space]
        [SerializeField]
        private Button m_resetOffset;

        private int m_currentOffset = 0;

        #endregion


        #region Base class methods

        protected override void Start()
        {
            m_resetOffset.onClick.AddListener(ResetOffset);
        }

        protected override void OnActionSelectInternal(AddressBookDemoAction selectedAction)
        {
            switch (selectedAction.ActionType)
            {
                case AddressBookDemoActionType.GetContactsAccessStatus:
                    var status = AddressBook.GetContactsAccessStatus();
                    Log("Address book permission status: " + status);
                    break;

                case AddressBookDemoActionType.ReadContacts:

                    ReadContactsOptions options = new ReadContactsOptions.Builder()
                                    .WithLimit(GetLimit())
                                    .WithOffset(m_currentOffset)
                                    .WithConstraints(GetReadContactsConstraints())
                                    .Build();
                    Log($"Reading {options.Limit} contacts from {options.Offset} offset.");
                    AddressBook.ReadContacts(options, OnReadContactsFinish);
                    break;

                case AddressBookDemoActionType.ResourcePage:
                    ProductResources.OpenResourcePage(NativeFeatureType.kAddressBook);
                    break;

                default:
                    break;
            }
        }

        private int GetLimit()
        {
            string limitStr = m_limit.options[m_limit.value].text;

            int limit;
            if(!int.TryParse(limitStr, out limit))
            {
                limit = 10;
            }

            return limit;
        }

        private ReadContactsConstraint GetReadContactsConstraints()
        {
            ReadContactsConstraint constraints = ReadContactsConstraint.None;

            if (m_mustIncludeNameOption.isOn)
            {
                constraints |= ReadContactsConstraint.MustIncludeName;
            }

            if (m_mustIncludeEmailOption.isOn)
            {
                constraints |= ReadContactsConstraint.MustIncludeEmail;
            }

            if (m_mustIncludePhoneNumberOption.isOn)
            {
                constraints |= ReadContactsConstraint.MustIncludePhoneNumber;
            }

            return constraints;
        }

        #endregion

        #region Plugin callback methods

        private void OnReadContactsFinish(AddressBookReadContactsResult result, Error error)
        {
            if (error == null)
            {
                var     contacts    = result.Contacts;
                m_currentOffset     = result.NextOffset;
                Log("Request to read contacts finished successfully.");
                Log($"Total contacts fetched: {contacts.Length}. Next offset: {result.NextOffset}");

                for (int iter = 0; iter < contacts.Length; iter++)
                {
                    Log(string.Format("[{0}]: {1}", iter, contacts[iter]));
                }
            }
            else
            {
                Log("Request to read contacts failed with error. Error: " + error);
            }
        }

        #endregion

        #region Helpers

        private void ResetOffset()
        {
            m_currentOffset = 0;
        }

        #endregion
    }
}
