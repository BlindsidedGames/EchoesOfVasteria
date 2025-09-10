namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Represents a set of options that can be used to read contacts from the user's device.
    /// </summary>
    /// @ingroup AddressBook
    public class ReadContactsOptions
    {
        /// <summary>
        /// The maximum number of contacts to return.
        /// </summary>
        /// <remarks>
        /// If the value is < 1, all contacts will be retrieved.
        /// </remarks>
        public int Limit { get; private set; }

        /// <summary>
        /// The number of contacts to skip.
        /// </summary>
        /// <remarks>
        /// This value is used to implement pagination.
        /// </remarks>
        public int Offset { get; private set; }

        /// <summary>
        /// The constraints for the contacts to be retrieved.
        /// </summary>
        public ReadContactsConstraint Constraints { get; private set; }

        private ReadContactsOptions() {}

        public class Builder
        {
            private ReadContactsOptions m_options;

            public Builder()
            {
                m_options = new ReadContactsOptions();
            }

            /// <summary>
            /// Sets the maximum number of contacts to return.
            /// </summary>
            /// <param name="limit">The maximum number of contacts to return.</param>
            /// <returns>The current Builder instance.</returns>
            public Builder WithLimit(int limit)
            {
                m_options.Limit = limit;
                return this;
            }

            /// <summary>
            /// Sets the number of contacts to skip.
            /// </summary>
            /// <param name="offset">The number of contacts to skip.</param>
            /// <returns>The current Builder instance.</returns>
            public Builder WithOffset(int offset)
            {
                m_options.Offset = offset;
                return this;
            }

            /// <summary>
            /// Sets the constraints for the contacts to be retrieved.
            /// </summary>
            /// <param name="constraints">The constraints for the contacts to be retrieved.</param>
            /// <returns>The current Builder instance.</returns>
            public Builder WithConstraints(ReadContactsConstraint constraints)
            {
                m_options.Constraints = constraints;
                return this;
            }

            /// <summary>
            /// Builds a <see cref="ReadContactsOptions"/> instance.
            /// </summary>
            /// <returns>The built <see cref="ReadContactsOptions"/> instance.</returns>
            public ReadContactsOptions Build()
            {
                return m_options;
            }
        }
    }
}
