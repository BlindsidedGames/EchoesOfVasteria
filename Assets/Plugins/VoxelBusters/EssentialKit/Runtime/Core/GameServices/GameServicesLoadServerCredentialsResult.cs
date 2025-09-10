using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// This class contains the information retrieved when <see cref="GameServices.LoadServerCredentials(EventCallback{GameServicesLoadServerCredentialsResult})"/> operation is completed.
    /// </summary>
    /// @ingroup GameServices
    public class GameServicesLoadServerCredentialsResult
    {
		#region Properties

		/// <summary>
		/// Server Credentials.
		/// </summary>
		public ServerCredentials ServerCredentials { get; private set; }
		
		/// <summary>
		///	Additional scopes granted to the game.
		/// </summary>
		public List<ServerCredentialAdditionalScope> AdditionalGrantedScopes { get; private set; }

        #endregion

        #region Constructors

        internal GameServicesLoadServerCredentialsResult(ServerCredentials serverCredentials, List<ServerCredentialAdditionalScope> additionalGrantedScopes)
        {
            // set properties
            ServerCredentials   = serverCredentials;
			AdditionalGrantedScopes = additionalGrantedScopes;
        }

        #endregion
	}
}