using System;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Represents constraints for reading contacts.
    /// note: You can set multiple constraints at once.
    /// </summary>
    /// <remarks>
    /// By default, the module will return all the contacts.
    /// </remarks>
    [Flags]
    public enum ReadContactsConstraint
    {
        /// <summary>
        /// No constraints.
        /// </summary>
        None = 0,
        /// <summary>
        /// Must include name of the contact.
        /// </summary>
        MustIncludeName = 1 << 0,
        /// <summary>
        /// Must include phone number of the contact.
        /// </summary>
        MustIncludePhoneNumber = 1 << 1,
        /// <summary>
        /// Must include email of the contact.
        /// </summary>
        MustIncludeEmail = 1 << 2
    }
}