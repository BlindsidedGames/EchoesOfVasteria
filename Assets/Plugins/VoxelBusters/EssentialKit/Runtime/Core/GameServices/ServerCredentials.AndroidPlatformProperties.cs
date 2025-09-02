using System;
using System.Collections;
using System.Text;

namespace VoxelBusters.EssentialKit
{
    public partial class ServerCredentials
    {
        /// <summary>
        /// Represents the android platform-specific properties for connecting to google play services from your backend.
        /// </summary>
        public class AndroidPlatformProperties
        {
            #region Properties

            /// <summary>
            /// The code obtained after the user has granted access to the specified scopes.
            /// </summary>
            /// <remarks>
            /// The code is only valid for a short period of time.
            /// </remarks>
            /// @remark It's possible to add refresh token to this by configuring in settings.
            public string ServerAuthCode { get; private set; }

            #endregion

            #region Constructors

            public AndroidPlatformProperties(string serverAuthCode)
            {
				ServerAuthCode	= serverAuthCode;
			}

			#endregion

		}
    }
}
