using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.GameServicesCore.Simulator
{
    internal sealed class LocalPlayer : Player, ILocalPlayer
    {
        #region Static fields

        private     static  LocalPlayer                 s_localPlayer       = new LocalPlayer();

        private     static  AuthChangeInternalCallback  s_onAuthChange;

        #endregion

        #region Constructors

        private LocalPlayer(string playerId = null, string displayName = null, string alias = null) 
            : base(playerId, displayName, alias)
        { }
            
        #endregion

        #region Create methods

        private static LocalPlayer CreateLocalPlayerFromData(PlayerData data)
        {
            return new LocalPlayer(data.Id, data.Name, data.Name);
        }

        #endregion

        #region Static methods

        public static LocalPlayer GetLocalPlayer()
        {
            return s_localPlayer;
        }

        public static void Authenticate(bool interactive)
        {
            GameServicesSimulator.Instance.Authenticate(interactive, (status, error) =>
            {
                bool isLoggedIn     = (status == LocalPlayerAuthStatus.Authenticated);

                // update local references
                s_localPlayer       = isLoggedIn ? CreateLocalPlayerFromData(GameServicesSimulator.Instance.GetLocalPlayer()) : new LocalPlayer();

                // notify listeners
                if (s_onAuthChange != null)
                {
                    s_onAuthChange(status, error);
                }
            });
        }

        public static void Signout()
        {
            Diagnostics.LogNotSupported("Signout");
        }

        public static void SetAuthChangeCallback(AuthChangeInternalCallback callback)
        {
            // set value
            s_onAuthChange = callback;
        }

        #endregion

        #region ILocalPlayer implementation

        public bool IsAuthenticated
        {
            get
            {
                return GameServicesSimulator.Instance.IsAuthenticated();
            }
        }

        public bool IsUnderAge
        {
            get
            {
                return false;
            }
        }

        public void LoadFriends(EventCallback<GameServicesLoadPlayerFriendsResult> callback)
        {
            if(callback != null)
            {
                var result = new GameServicesLoadPlayerFriendsResult(CreateSampleFriends());
                callback(result, null);
            }
        }

        public void AddFriend(string playerId, EventCallback<bool> callback)
        {
            if(callback != null)
            {
                callback(true, null);
            }
        }

        #endregion

        #region Utilities

        private IPlayer[] CreateSampleFriends()
        {
            IPlayer[] friends = new IPlayer[]
            {
                new Player("11", "Friend 1", "Friend Alias 1"),
                new Player("22", "Friend 2", "Friend Alias 2"),
                new Player("33", "Friend 3", "Friend Alias 3")
            };

            return friends;
        }

        #endregion
    }
}