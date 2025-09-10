#if UNITY_IOS || UNITY_TVOS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.EssentialKit;
using VoxelBusters.CoreLibrary.NativePlugins.iOS;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.GameServicesCore.iOS
{
    internal sealed class LocalPlayer : Player, ILocalPlayer
    {
        #region Static fields

        private     static  readonly    LocalPlayer     s_localPlayer       = null;

        #endregion

        #region Static event

        private     static  AuthChangeInternalCallback  s_onAuthChange;

        #endregion

        #region Constructors

        static LocalPlayer()
        {
            // create local player
            s_localPlayer   = new LocalPlayer();

            // register
            PlayerBinding.NPLocalPlayerRegisterCallbacks(HandleAuthChangeNativeCallback);

            // update properties
            s_localPlayer.UpdateNativeReference();
        }

        ~LocalPlayer()
        {
            Dispose(false);
        }

        #endregion

        #region Static methods

        public static LocalPlayer GetLocalPlayer()
        {
            return s_localPlayer;
        }

        public static void Authenticate(bool interactive)
        {
            PlayerBinding.NPLocalPlayerAuthenticate(interactive);
        }

        public static void SetAuthChangeCallback(AuthChangeInternalCallback callback)
        {
            // set value
            s_onAuthChange  = callback;
        }

        #endregion

        #region Override-able base class methods

        protected override string GetIdentifierInternal()
        {
            return !IsAuthenticated ? null : base.GetIdentifierInternal();

        }
        protected override string GetDeveloperScopeIdentifierInternal()
        {
            return !IsAuthenticated ? null : base.GetDeveloperScopeIdentifierInternal();

        }

        protected override string GetLegacyIdentifierInternal()
        {
            return !IsAuthenticated ? null : base.GetLegacyIdentifierInternal();
        }
        
        protected override string GetAliasInternal()
        {
            return !IsAuthenticated ? null : base.GetAliasInternal();
        }
        
        protected override string GetDisplayNameInternal()
        {
            return !IsAuthenticated ? null : base.GetDisplayNameInternal();
        }

        #endregion

        #region Private methods

        private void UpdateNativeReference()
        {
            var     localPlayerPtr  = PlayerBinding.NPLocalPlayerGetLocalPlayer();

            // set properties
            NativeObjectRef         = new IosNativeObjectRef(localPlayerPtr);
        }

        #endregion

        #region ILocalPlayer implementation

        public bool IsAuthenticated
        {
            get
            {
                return PlayerBinding.NPLocalPlayerIsAuthenticated();
            }
        }

        public bool IsUnderAge
        {
            get
            {
                return PlayerBinding.NPLocalPlayerIsUnderage();
            }
        }

        public void LoadFriends(EventCallback<GameServicesLoadPlayerFriendsResult> callback)
        {
            LoadPlayersInternalCallback callbackWrapper = (players, error) => SendLoadPlayerFriendsResult(callback, players, error);
            var     tagPtr  = MarshalUtility.GetIntPtr(callbackWrapper);
            PlayerBinding.NPLocalPlayerLoadFriends(tagPtr);
        }

        public void AddFriend(string playerId, EventCallback<bool> callback)
        {
            ViewClosedInternalCallback callbackWrapper = (error) => SendViewClosedResult(callback, error);
            PlayerBinding.NPLocalPlayerShowAddFriendRequestView(playerId, MarshalUtility.GetIntPtr(callbackWrapper));
        }

        #endregion

        #region Native callback methods

        [MonoPInvokeCallback(typeof(GameServicesAuthStateChangeNativeCallback))]
        private static void HandleAuthChangeNativeCallback(GKLocalPlayerAuthState state, NativeError error)
        {
            s_localPlayer.UpdateNativeReference();

            // send result
            var     authStatus  = GameCenterUtility.ConvertToLocalPlayerAuthStatus(state);
            var     errorObj    = error.Convert(GameServicesError.kDomain);
            s_onAuthChange(authStatus, errorObj);
        }

        #endregion
    }
}
#endif