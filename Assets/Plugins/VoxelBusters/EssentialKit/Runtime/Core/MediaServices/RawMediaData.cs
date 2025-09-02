namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Contains information about raw media data.
    /// </summary>
    /// @ingroup MediaServices
    public class RawMediaData
    {
        /// <summary>
        /// Bytes of the raw media data.
        /// </summary>
        public byte[] Bytes { get; private set; }

        /// <summary>
        /// Mime type of the raw media data.
        /// </summary>
        public string Mime { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="bytes">Bytes of the raw media data.</param>
        /// <param name="mime">Mime type of the raw media data.</param>
        internal RawMediaData(byte[] bytes, string mime)
        {
            Bytes = bytes;
            Mime    = mime;
        }
    }
}
