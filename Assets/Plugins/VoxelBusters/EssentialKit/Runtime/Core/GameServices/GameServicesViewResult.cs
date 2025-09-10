
namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// This class contains the information retrieved when game view is closed.
    /// </summary>
    /// @ingroup GameServices
    public class GameServicesViewResult
    {
        #region Properties

        public GameServicesViewResultCode ResultCode { get; private set; }

        #endregion

        #region Constructors

        internal GameServicesViewResult(GameServicesViewResultCode resultCode)
        {
            // set properties
            ResultCode  = resultCode;
        }

        #endregion
    }
}