#if UNITY_ANDROID
using System.Diagnostics;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;

namespace VoxelBusters.EssentialKit.GameServicesCore.Android
{
    internal sealed class LocalPlayer : Player, ILocalPlayer
    {
        #region Constructors

        public LocalPlayer() : base(null)
        {

        }

        #endregion

        #region ILocalPlayer implementation

        public bool IsAuthenticated
        {
            get
            {
                return m_instance != null;
            }
        }

        public bool IsUnderAge
        {
            get
            {
                DebugLogger.LogWarning("This always returns false on Android");
                return false;
            }
        }

        public void LoadFriends(EventCallback<GameServicesLoadPlayerFriendsResult> callback)
        {
            m_instance.LoadFriends(NativeUnityPluginUtility.GetActivity(), new NativeLoadPlayersListener() {
                onSuccessCallback = (nativePlayers) =>
                {
                    Player[] players = NativeUnityPluginUtility.Map<NativeGamePlayer, Player>(nativePlayers.Get());
                    SendLoadPlayerFriendsResult(callback, players, null);
                },
                onFailureCallback = (error) =>
                {
                    SendLoadPlayerFriendsResult(callback, null, error.Convert(GameServicesError.kDomain));
                }
            });
        }

        public void AddFriend(string playerId, EventCallback<bool> callback)
        {
            m_instance.SendFriendRequest(NativeUnityPluginUtility.GetActivity(), playerId, new NativeViewListener() {
                onCloseCallback = (error) =>
                {
                    SendViewClosedResult(callback, error != null ? error.Convert(GameServicesError.kDomain) : null);
                }
            });
        }

        #endregion

        #region Internal methods

        internal void SetPlayer(NativeGamePlayer player)
        {
            m_instance = player;
        }

        #endregion
    }
}
#endif