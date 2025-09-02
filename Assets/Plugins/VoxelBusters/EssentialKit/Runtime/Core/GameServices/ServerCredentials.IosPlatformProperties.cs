
using System.Collections;

namespace VoxelBusters.EssentialKit
{
    public partial class ServerCredentials
    {
        /// <summary>
        /// Represents iOS platform-specific properties for server credentials.
        /// </summary>
        public class IosPlatformProperties
        {
			#region Constants

			private const string kCredentialsPublicKeyUrl = "public-key-url";
			private const string kCredentialsSignature = "signature";
			private const string kCredentialsSalt = "salt";
			private const string kCredentialsTimestamp = "timestamp";

            #endregion

            #region Fields

            /// <summary>
			/// Gets the public key URL associated with the server credentials.
			/// </summary>
			public string PublicKeyUrl { get; private set; }

			/// <summary>
			/// Gets the signature used for server authentication.
			/// </summary>
			public byte[] Signature { get; private set; }

			/// <summary>
			/// Gets the salt value used for cryptographic operations.
			/// </summary>
			public byte[] Salt { get; private set; }

			/// <summary>
			/// Gets the timestamp indicating when the credentials were generated.
			/// </summary>
			public long Timestamp { get; private set; }

			#endregion

			#region Constructors

			public IosPlatformProperties(string publicKeyUrl, byte[] signature, byte[] salt, long timestamp)
			{
				PublicKeyUrl	= publicKeyUrl;
				Signature		= signature;
				Salt			= salt;
				Timestamp		= timestamp;
			}

			#endregion

			#region Private methods

			private void Load(IDictionary json)
			{
				PublicKeyUrl = (string)json[kCredentialsPublicKeyUrl];

				var signature = (string)json[kCredentialsSignature];
				if (!string.IsNullOrEmpty(signature))
				{
					Signature = System.Convert.FromBase64String(signature);
				}

				string salt = (string)json[kCredentialsSalt];
				if (!string.IsNullOrEmpty(salt))
				{
					Salt = System.Convert.FromBase64String(salt);
				}

				string timestamp = (string)json[kCredentialsTimestamp];

				if (!string.IsNullOrEmpty(timestamp))
				{
					Timestamp = long.Parse(timestamp);
				}
			}

            #endregion
        }
    }
}
