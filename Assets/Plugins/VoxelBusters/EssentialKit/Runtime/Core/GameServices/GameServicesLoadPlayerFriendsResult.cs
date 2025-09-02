namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// This class contains the information retrieved when load player friends operation is completed.
    /// </summary>
    /// @ingroup GameServices
    public class GameServicesLoadPlayerFriendsResult
    {
        #region Properties

        /// <summary>
        /// An array of available player friends.
        /// </summary>
        public IPlayer[] Players { get; private set; }

        #endregion

        #region Constructors

        internal GameServicesLoadPlayerFriendsResult(IPlayer[] players)
        {
            // set properties
            Players     = players;
        }

        #endregion
    }
}