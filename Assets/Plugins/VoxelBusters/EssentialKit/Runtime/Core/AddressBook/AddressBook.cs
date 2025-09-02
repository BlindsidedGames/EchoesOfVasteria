using System;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;
using VoxelBusters.EssentialKit.AddressBookCore;

namespace VoxelBusters.EssentialKit
{
    /** 
     * @defgroup AddressBook AddressBook
     * @brief Provides cross-platform interface to access the contact information.
     */

    /// <summary>
    /// The <see cref="AddressBook"/> class provides cross-platform interface to access the contact information.
    /// </summary>
    /// <description> 
    /// <para>
    /// In iOS/Android platform, users can grant or deny access to contact data on a per-application basis. 
    /// And the user is prompted only the first time <see cref="ReadContacts(EventCallback{AddressBookReadContactsResult})"/> is requested; any subsequent calls use the existing permissions.
    /// You can provide custom usage description in Address Book settings of Essential Kit window.
    /// </para>
    /// </description> 
    /// @ingroup AddressBook
    public static class AddressBook
    {
        #region Static fields

        [ClearOnReload]
        private     static  INativeAddressBookInterface    s_nativeInterface    = null;

        #endregion

        #region Static properties

        public static AddressBookUnitySettings UnitySettings { get; private set; }

        #endregion

        #region Static methods

        public static bool IsAvailable()
        {
            return (s_nativeInterface != null) && s_nativeInterface.IsAvailable;
        }

        ///@name Advanced Usage
        ///@{
        /// <summary>
        /// [Advanced] Initializes the address book module with the given settings. This call is optional and only need to be called if you have custom settings to initialize this feature.
        /// </summary>
        /// <param name="settings">The settings to be used for initialization.</param>
        /// <remarks>
        /// <para>
        /// The settings configure the default image to be used for address book contacts.
        /// </para>
        /// </remarks>
        public static void Initialize(AddressBookUnitySettings settings)
        {
            Assert.IsArgNotNull(settings, nameof(settings));

            // Set default properties
            UnitySettings                       = settings;
            AddressBookContactBase.defaultImage = settings.DefaultImage;

            // Configure interface
            s_nativeInterface       = NativeFeatureActivator.CreateInterface<INativeAddressBookInterface>(ImplementationSchema.AddressBook, true);
        }
        ///@}

        /// <summary>
        /// Returns the current permission status provided to access the contact data.
        /// </summary>
        /// <description>
        /// To see different authorization status, see <see cref="AddressBookContactsAccessStatus"/>.
        /// </description>
        /// <returns>The current permission status to access the contact data.</returns>
        public static AddressBookContactsAccessStatus GetContactsAccessStatus()
        {
            try
            {
                // make request
                return s_nativeInterface.GetContactsAccessStatus();
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
                return AddressBookContactsAccessStatus.NotDetermined;
            }
        }

        /// <summary>
        /// Once after getting the user permission, retrieves all the contact information saved in address book database.
        /// </summary>
        /// <param name="options">The options to customize the retrieval of contacts. Can be created with <see cref="ReadContactsOptions.Builder"/>.</param>
        /// <param name="callback">The delegate <see cref="VoxelBusters.CoreLibrary.EventCallback<TResult>"/> callback that will be executed after the operation has a result or error.</param>
        /// <example>
        /// <code>
        /// // example usage
        /// void OnReadContactsFinished(AddressBookReadContactsResult result, Error error)
        /// {
        ///     // code to handle the retrieved contacts
        /// }
        ///
        /// // usage
        /// AddressBook.ReadContacts(new ReadContactsOptions.Builder().WithLimit(10).Build(), OnReadContactsFinished);
        /// </code>
        /// </example>
        public static void ReadContacts(ReadContactsOptions options, EventCallback<AddressBookReadContactsResult> callback)
        {
            try
            {
                // make request
                s_nativeInterface.ReadContacts(options, (contacts, nextOffset, error) => SendReadContactsResult(callback, contacts, nextOffset, error));
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
            }
        }

        #endregion


        #region Callback methods

        private static void SendReadContactsResult(EventCallback<AddressBookReadContactsResult> callback, IAddressBookContact[] contacts, int nextOffset, Error error)
        {
            // send result to caller object
            var     result  = new AddressBookReadContactsResult(contacts: contacts ?? new IAddressBookContact[0], nextOffset);
            CallbackDispatcher.InvokeOnMainThread(callback, result, error);
        }

        #endregion
    }
}