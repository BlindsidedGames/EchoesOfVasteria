using System;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;
using VoxelBusters.EssentialKit.GameServicesCore;

namespace VoxelBusters.EssentialKit
{
    /** @defgroup GameServices Game Services
    *   @brief Provides cross-platform interface to easily integrate popular social gaming functionalities such as achievements, leaderboards.
    */
    /// <summary>
    /// Provides cross-platform interface to easily integrate popular social gaming functionalities such as achievements, leaderboards on your mobile games.
    /// </summary>
    /// <description>
    /// <para>
    /// Internally, game services feature uses native game servers for handling functionalities. 
    /// So while running your game on iOS devices, Game Center servers will be used. Whereas on Android platform, Play Game Services server will be used. 
    /// </para>
    /// <para>
    /// Goto <a href="https://developer.apple.com/library/ios/documentation/LanguagesUtilities/Conceptual/iTunesConnectGameCenter_Guide/AccessAndEnable/AccessAndEnable.html">iTune's Connect</a> to configure leaderboard, achievemnts for your iOS game. 
    /// And for Android, add records at <a href="https://developers.google.com/games/services/android/quickstart">Google Developer Console</a>.
    /// </para>
    /// </description>
    /// @ingroup GameServices
    public static class GameServices
    {
        #region Static fields

        [ClearOnReload]
        private     static  INativeGameServicesInterface    s_nativeInterface;

        #endregion

        #region Static properties

        public static GameServicesUnitySettings UnitySettings { get; private set; }

        public static LeaderboardDefinition[] LeaderboardDefinitions { get; private set; }

        public static AchievementDefinition[] AchievementDefinitions { get; private set; }

        /// <summary>
        /// Returns the local player.
        /// </summary>
        /// <value>The local player.</value>
        public static ILocalPlayer LocalPlayer
        {
            get
            {
                try
                {
                    return (s_nativeInterface != null) ? s_nativeInterface.GetLocalPlayer() : new NullLocalPlayer();
                }
                catch (Exception exception)
                {
                    DebugLogger.LogException(EssentialKitDomain.Default, exception);
                    return null;
                }
            }
        }

        /// <summary>
        /// A boolean value indicating whether this local player is authenticated.
        /// </summary>
        /// <value><c>true</c> if is authenticated; otherwise, <c>false</c>.</value>
        public static bool IsAuthenticated
        {
            get
            {
                try
                {
                    return LocalPlayer.IsAuthenticated;
                }
                catch (Exception exception)
                {
                    DebugLogger.LogException(EssentialKitDomain.Default, exception);
                    return false;
                }
            }
        }

        /// <summary>
        /// Returns the cached leaderboards array.
        /// </summary>
        /// <remarks>
        /// \note This property is invalid until a call to <see cref="LoadLeaderboards(EventCallback{GameServicesLoadLeaderboardsResult})"/> is completed.
        /// </remarks>
        public static ILeaderboard[] Leaderboards { get; private set; }

        /// <summary>
        /// Returns the cached achievement description array.
        /// </summary>
        /// <remarks>
        /// \note This property is invalid until a call to <see cref="LoadAchievementDescriptions(EventCallback{GameServicesLoadAchievementDescriptionsResult})"/> is completed.
        /// </remarks>
        public static IAchievementDescription[] AchievementDescriptions { get; private set; }

        /// <summary>
        /// Returns the cached achievements array.
        /// </summary>
        /// <remarks>
        /// \note This property is invalid until a call to <see cref="LoadAchievements(EventCallback{GameServicesLoadAchievementsResult})"/> is completed.
        /// </remarks>
        public static IAchievement[] Achievements { get; private set; }

        #endregion

        #region Static events

        /// <summary>
        /// Event called on local player auth change.
        /// </summary>
        public static event EventCallback<GameServicesAuthStatusChangeResult> OnAuthStatusChange;

        #endregion

        #region Setup methods

        public static bool IsAvailable()
        {
            return (s_nativeInterface != null) && s_nativeInterface.IsAvailable;
        }
        /// @name Advanced Usage
        /// @{

        /// <summary>
        /// Initializes game services with custom settings created at runtime.
        /// </summary>
        /// <param name="settings">Settings for the game services.</param>
        public static void Initialize(GameServicesUnitySettings settings)
        {
            Assert.IsArgNotNull(settings, nameof(settings));

            // Reset event properties
            OnAuthStatusChange      = null;

            // Set properties
            UnitySettings           = settings;
            Leaderboards            = new ILeaderboard[0];
            AchievementDescriptions = new IAchievementDescription[0];
            Achievements            = new IAchievement[0];
            LeaderboardDefinitions  = settings.Leaderboards;
            AchievementDefinitions  = settings.Achievements;

            // Configure interface
            s_nativeInterface       = NativeFeatureActivator.CreateInterface<INativeGameServicesInterface>(ImplementationSchema.GameServices, true);
            s_nativeInterface.SetCanShowAchievementCompletionBanner(UnitySettings.ShowAchievementCompletionBanner);
            s_nativeInterface.SetAuthChangeCallback(HandleAuthChangeInternalCallback);
        }
        /// @}

        internal static LeaderboardDefinition FindLeaderboardDefinitionWithId(string leaderboardId)
        {
            return Array.Find(LeaderboardDefinitions, (item) => string.Equals(item.Id, leaderboardId));
        }

        internal static LeaderboardDefinition FindLeaderboardDefinitionWithPlatformId(string leaderboardPlatformId)
        {
            return Array.Find(LeaderboardDefinitions, (item) => string.Equals(item.GetPlatformIdForActivePlatform(), leaderboardPlatformId));
        }

        internal static AchievementDefinition FindAchievementDefinitionWithId(string achievementId)
        {
            return Array.Find(AchievementDefinitions, (item) => string.Equals(item.Id, achievementId));
        }

        internal static AchievementDefinition FindAchievementDefinitionWithPlatformId(string achievementPlatformId)
        {
            return Array.Find(AchievementDefinitions, (item) => string.Equals(item.GetPlatformIdForActivePlatform(), achievementPlatformId));
        }

        #endregion

        #region Leaderboard methods

        /// <summary>
        /// Creates a new instance of leaderboard object.
        /// </summary>
        /// <param name="leaderboardId">A string used to uniquely identify the leaderboard.</param>
        public static ILeaderboard CreateLeaderboard(string leaderboardId)
        {
            // find settings information for specified id
            var     settings    = FindLeaderboardDefinitionWithId(leaderboardId);
            Assert.IsFalse(null == settings, "Could not find settings for specified id: " + leaderboardId);

            try
            {
                // create object
                return s_nativeInterface.CreateLeaderboard(settings.Id, settings.GetPlatformIdForActivePlatform());
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
                return null;
            }
        }

        /// <summary>
        /// Loads the leaderboards.
        /// </summary>
        /// <param name="callback">Callback method that will be invoked after operation is completed.</param>
        public static void LoadLeaderboards(EventCallback<GameServicesLoadLeaderboardsResult> callback)
        {
            try
            {
                // make request
                s_nativeInterface.LoadLeaderboards(LeaderboardDefinitions, (leaderboards, error) => SendLoadLeaderboardsResult(callback, leaderboards, error));
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
            }
        }

        #endregion

        #region Achievement methods

        /// <summary>
        /// Creates a new instance of achievement object.
        /// </summary>
        /// <param name="achievementId">A string used to uniquely identify the achievement.</param>
        public static IAchievement CreateAchievement(string achievementId)
        {
            // find settings information for specified id
            var     settings    = FindAchievementDefinitionWithId(achievementId);
            Assert.IsFalse(null == settings, "Could not find settings for specified id: " + achievementId);

            try
            {
                // make request
                return s_nativeInterface.CreateAchievement(settings.Id, settings.GetPlatformIdForActivePlatform());
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
                return null;
            }
        }

        /// <summary>
        /// Loads the achievement descriptions from game server.
        /// </summary>
        /// <param name="callback">Callback method that will be invoked after operation is completed.</param>
        public static void LoadAchievementDescriptions(EventCallback<GameServicesLoadAchievementDescriptionsResult> callback)
        {
            try
            {
                // make request
                s_nativeInterface.LoadAchievementDescriptions((descriptions, error) => SendLoadAchievementDescriptionsResult(callback, descriptions, error));
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
            }
        }

        /// <summary>
        /// Loads previously submitted achievement progress for the current local player.
        /// </summary>
        /// <param name="callback">Callback method that will be invoked after operation is completed.</param>
        public static void LoadAchievements(EventCallback<GameServicesLoadAchievementsResult> callback)
        {
            try
            {
                // make request
                s_nativeInterface.LoadAchievements((achievements, error) => SendLoadAchievementsResult(callback, achievements, error));
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
            }
        }

        /// <summary>
        /// Reports the local user's achievement progress to game server, using platform specific id.
        /// </summary>
        /// <param name="achievementId">A string used to uniquely identify the achievement.</param>
        /// <param name="percentageCompleted">The value indicates how far the player has progressed.</param>
        /// <param name="callback">Callback that will be called after operation is completed.</param>
        public static void ReportAchievementProgress(string achievementId, double percentageCompleted, CompletionCallback callback)
        {
            var     achievement     = CreateAchievement(achievementId);
            ReportAchievementProgress(achievement, percentageCompleted, callback);
        }

        /// <summary>
        /// Reports the local user's achievement progress to game server.
        /// </summary>
        /// <param name="achievementDescription">The achievement description object.</param>
        /// <param name="percentageCompleted">The value indicates how far the player has progressed.</param>
        /// <param name="callback">Callback that will be called after operation is completed.</param>
        public static void ReportAchievementProgress(IAchievementDescription achievementDescription, double percentageCompleted, CompletionCallback callback)
        {
            var     achievement     = CreateAchievement(achievementDescription.Id);
            ReportAchievementProgress(achievement, percentageCompleted, callback);
        }

        /// <summary>
        /// Reports the local user's achievement progress to game server.
        /// </summary>
        /// <param name="achievement">The achievement object.</param>
        /// <param name="percentageCompleted">The value indicates how far the player has progressed.</param>
        /// <param name="callback">Callback that will be called after operation is completed.</param>
        public static void ReportAchievementProgress(IAchievement achievement, double percentageCompleted, CompletionCallback callback)
        {
            Assert.IsArgNotNull(achievement, "achievement");

            // submit information
            achievement.PercentageCompleted = percentageCompleted;
            achievement.ReportProgress(callback);
        }


        /// <summary>
        /// Loads the list of friends for the authenticated local player.
        /// </summary>
        /// <param name="callback">
        /// The callback that will be called after operation is completed.
        /// If successful, it will contain the list of friends.
        /// </param>
        public static void LoadFriends(EventCallback<GameServicesLoadPlayerFriendsResult> callback)
        {
            // Check if the local player is authenticated. If not, return error.
            if(!IsAuthenticated)
            {
                DebugLogger.LogError("Player is not authenticated");
                callback?.Invoke(null, new Error("Player is not authenticated"));
                return;
            }

            LocalPlayer.LoadFriends(callback);
        }

        /// <summary>
        /// Initiates a friend request to the specified player.
        /// </summary>
        /// <param name="playerId">The id of the player to send the request to.</param>
        /// <param name="callback">
        /// The callback that will be called after operation is completed.
        /// </param>
        public static void AddFriend(string playerId, EventCallback<bool> callback)
        {
            // Check if the local player is authenticated. If not, return error.
            if(!IsAuthenticated)
            {
                DebugLogger.LogError("Player is not authenticated");
                callback?.Invoke(false, new Error("Player is not authenticated"));
                return;
            }

            LocalPlayer.AddFriend(playerId, callback);
        }

        #endregion

        #region Player methods

        /// <summary>
        /// Initiates authentication process for the local player on the device.
        /// </summary>
        /// <param name="interactive">Indicates whether the authentication process should be interactive or not. If false, the authentication will be silent and will not prompt the user with a login dialog.</param>   
        public static void Authenticate(bool interactive = true)
        {
            try
            {
                s_nativeInterface.Authenticate(interactive);
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
            }
        }

        /// <summary>
        /// Signout the local player on the device.
        /// </summary>
        public static void Signout()
        {
            try
            {
                s_nativeInterface.Signout();
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
            }
        }

        #endregion

        #region Score methods

        /// <summary>
        /// Reports the score to game server.
        /// </summary>
        /// <param name="leaderboardId">A string used to uniquely identify the leaderboard.</param>
        /// <param name="value">The value of the score.</param>
        /// <param name="callback">Callback that will be called after operation is completed.</param>
        /// <param name="tag">The tag used to identify the score (optional). This needs to be of max length 8 characters in ascii characters.</param>
        public static void ReportScore(string leaderboardId, long value, CompletionCallback callback, string tag = null)
        {
            ReportScore(CreateLeaderboard(leaderboardId), value, callback, tag);
        }

        /// <summary>
        /// Reports the score to game server.
        /// </summary>
        /// <param name="leaderboard">The leaderboard object.</param>
        /// <param name="value">The value of the score.</param>
        /// <param name="callback">Callback that will be called after operation is completed.</param>
        /// <param name="tag">The tag used to identify the score (optional). This needs to be of max length 8 characters in ascii characters.</param>
        public static void ReportScore(ILeaderboard leaderboard, long value, CompletionCallback callback, string tag = null)
        {
            // create score object
            Assert.IsPropertyNotNull(leaderboard, "leaderboard");
            
            // submit score
            leaderboard.ReportScore(value, callback, tag);
        }

        #endregion

        #region View methods

        /// <summary>
        /// Opens the standard view to display all the leaderboards.
        /// </summary>
        /// <param name="timescope">A time filter used to restrict which scores are displayed to the player.</param>
        /// <param name="callback">Callback that will be called after operation is completed.</param>
        public static void ShowLeaderboards(LeaderboardTimeScope timescope = LeaderboardTimeScope.AllTime, EventCallback<GameServicesViewResult> callback = null)
        {
            try
            {
                // make request
                s_nativeInterface.ShowLeaderboard(
                    leaderboardId: null,
                    leaderboardPlatformId: null,
                    timeScope: timescope,
                    callback: (error) => SendViewClosedResult(callback, error));
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
            }
        }

        /// <summary>
        /// Opens the standard view to display leaderboard scores corresponding to given id.
        /// </summary>
        /// <param name="leaderboardId">A string used to identify the leaderboard.</param>
        /// <param name="timescope">A time filter used to restrict which scores are displayed to the player.</param>
        /// <param name="callback">Callback that will be called after operation is completed.</param>
        /// <remarks>
        /// \note Incase, if you want to list out all the leaderboards that are used in your game, then pass <c>null</c> for leaderboard identifier.
        /// </remarks>
        public static void ShowLeaderboard(string leaderboardId, LeaderboardTimeScope timescope = LeaderboardTimeScope.AllTime, EventCallback<GameServicesViewResult> callback = null)
        {
            // find settings information for specified id
            var     settings    = FindLeaderboardDefinitionWithId(leaderboardId);
            Assert.IsFalse(null == settings, "Could not find settings for specified id: " + leaderboardId);

            try
            {
                // make request
                s_nativeInterface.ShowLeaderboard(
                    leaderboardId: leaderboardId,
                    leaderboardPlatformId: settings.GetPlatformIdForActivePlatform(),
                    timeScope: timescope,
                    callback: (error) => SendViewClosedResult(callback, error));
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
            }
        }

        /// <summary>
        /// Opens the standard view to display leaderboard scores corresponding to given leaderboard.
        /// </summary>
        /// <param name="leaderboard">The leaderboard object.</param>
        /// <param name="timescope">A time filter used to restrict which scores are displayed to the player.</param>
        /// <param name="callback">Callback that will be called after operation is completed.</param>
        /// <remarks>
        /// \note Incase, if you want to list out all the leaderboards that are used in your game, then pass <c>null</c> for leaderboard identifier.
        /// </remarks>
        public static void ShowLeaderboard(ILeaderboard leaderboard, LeaderboardTimeScope timescope = LeaderboardTimeScope.AllTime, EventCallback<GameServicesViewResult> callback = null)
        {
            // validate arguments
            Assert.IsArgNotNull(leaderboard, "leaderboard");

            try
            {
                // make request
                s_nativeInterface.ShowLeaderboard(
                    leaderboardId: leaderboard.Id,
                    leaderboardPlatformId: leaderboard.PlatformId,
                    timeScope: timescope,
                    callback: (error) => SendViewClosedResult(callback, error));
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
            }
        }

        /// <summary>
        /// Opens the standard view to display achievement progress screen for the local player.
        /// </summary>
        /// <param name="callback">Callback that will be called after operation is completed.</param>
        public static void ShowAchievements(EventCallback<GameServicesViewResult> callback = null)
        {
            try
            {
                s_nativeInterface.ShowAchievements((error) => SendViewClosedResult(callback, error));
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
            }
        }

        #endregion

        #region Misc methods


        /// <summary>
        /// Loads the server credentials.
        /// </summary>
        /// <param name="callback">Callback that will be called after operation is completed.</param>
        /// <remarks>
        /// The server credentials is a set of properties that can be used to connect to game service from your backend.
        /// </remarks>
        public static void LoadServerCredentials(EventCallback<GameServicesLoadServerCredentialsResult> callback)
        {
            LoadServerCredentials(new List<ServerCredentialAdditionalScope>(), callback);
        }


        /// <summary>
        /// Loads the server credentials with additional scopes.
        /// </summary>
        /// <param name="additionalScopes">Pass additional scopes required for the game to access from your backend.</param>
        /// <param name="callback">Callback that will be called after operation is completed.</param>
        /// <remarks>
        /// The server credentials is a set of properties that can be used to connect to game service from your backend.
        /// </remarks>
        public static void LoadServerCredentials(List<ServerCredentialAdditionalScope> additionalScopes, EventCallback<GameServicesLoadServerCredentialsResult>  callback)
        {
            try
            {
                s_nativeInterface.LoadServerCredentials(additionalScopes, (credentials, additionalGrantedScopes, error) => SendLoadServerCredentialsResult(callback, credentials, additionalGrantedScopes, error));
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
            }
        }
        
        

        #endregion

        #region Callback methods

        private static void HandleAuthChangeInternalCallback(LocalPlayerAuthStatus authStatus, Error error)
        {
            // invoke event
            var     result      = new GameServicesAuthStatusChangeResult(LocalPlayer, authStatus);
            CallbackDispatcher.InvokeOnMainThread(OnAuthStatusChange, result, error);
        }

        private static void SendLoadLeaderboardsResult(EventCallback<GameServicesLoadLeaderboardsResult> callback, ILeaderboard[] leaderboards, Error error)
        {
            // check whether fetched value is null
            leaderboards        = leaderboards ?? new ILeaderboard[0];

            // cache data
            Leaderboards        = leaderboards;

            // send result to caller object
            var     result      = new GameServicesLoadLeaderboardsResult(leaderboards);
            CallbackDispatcher.InvokeOnMainThread(callback, result, error);
        }

        private static void SendLoadAchievementDescriptionsResult(EventCallback<GameServicesLoadAchievementDescriptionsResult> callback, IAchievementDescription[] descriptions, Error error)
        {
            // check whether fetched value is null
            descriptions        = descriptions ?? new IAchievementDescription[0];

            // cache data
            AchievementDescriptions = descriptions;

            // send result to caller object
            var     result      = new GameServicesLoadAchievementDescriptionsResult(descriptions);
            CallbackDispatcher.InvokeOnMainThread(callback, result, error);
        }

        private static void SendLoadAchievementsResult(EventCallback<GameServicesLoadAchievementsResult> callback, IAchievement[] achievements, Error error)
        {
            // check whether fetched value is null
            achievements        = achievements ?? new IAchievement[0];

            // update cached data
            Achievements        = achievements;

            // send result to caller object
            var     result      = new GameServicesLoadAchievementsResult(achievements);
            CallbackDispatcher.InvokeOnMainThread(callback, result, error);
        }

        private static void SendLoadPlayersResult(EventCallback<GameServicesLoadPlayersResult> callback, IPlayer[] players, Error error)
        {
            // send result to caller object
            var     result      = new GameServicesLoadPlayersResult(players ?? new IPlayer[0]);
            CallbackDispatcher.InvokeOnMainThread(callback, result, error);
        }

        private static void SendViewClosedResult(EventCallback<GameServicesViewResult> callback, Error error)
        {
            // send result to caller object
            var     resultCode  = (error == null) ? GameServicesViewResultCode.Done : GameServicesViewResultCode.Unknown;
            var     result      = new GameServicesViewResult(resultCode);
            CallbackDispatcher.InvokeOnMainThread(callback, result, error);
        }

        private static void SendLoadServerCredentialsResult(EventCallback<GameServicesLoadServerCredentialsResult> callback, ServerCredentials serverCredentials, List<ServerCredentialAdditionalScope> additionalGrantedScopes, Error error)
        {
            // send result to caller object
            var     result      = new GameServicesLoadServerCredentialsResult(serverCredentials, additionalGrantedScopes);
            CallbackDispatcher.InvokeOnMainThread(callback, result, error);
        }

        #endregion

        #region Obsolete methods

        /// @name Obsolete methods
        /// @{
        
        /// <summary>
        /// Loads the player details from game server.
        /// </summary>
        /// <param name="playerIds">An array of player id's whose details has to be retrieved from game server.</param>
        /// <param name="callback">Callback that will be called after operation is completed.</param>
        [Obsolete("This method is obsolete due to privacy restrictions on supported platforms.")]
        public static void LoadPlayers(string[] playerIds, EventCallback<GameServicesLoadPlayersResult> callback)
        {
            // validate arguments
            Assert.IsNotNullOrEmpty(playerIds, "playerIds");

            try
            {
                // make request
                s_nativeInterface.LoadPlayers(playerIds, (players, error) => SendLoadPlayersResult(callback, players, error));
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
            }
        }

        /// <summary>
        /// Creates the score for specified leaderboard.
        /// </summary>
        /// <returns>The score object.</returns>
        /// <param name="leaderboardId">A string used to uniquely identify the leaderboard.</param>
        [Obsolete("Use ReportScore(string, long, CompletionCallback, string) or ILeaderboard.ReportScore(long, CompletionCallback) instead for submitting scores.", true)]
        public static IScore CreateScore(string leaderboardId)
        {
            // find settings information for specified id
            var     settings    = FindLeaderboardDefinitionWithId(leaderboardId);
            Assert.IsFalse(null == settings, "Could not find settings for specified id: " + leaderboardId);

            return null;
        }

        /// <summary>
        /// Creates the score for specified leaderboard.
        /// </summary>
        /// <returns>The score object.</returns>
        /// <param name="leaderboard">The leaderboard object.</param>
        [Obsolete("Use ReportScore(string, long, CompletionCallback, string) instead for submitting scores.", true)]
        public static IScore CreateScore(ILeaderboard leaderboard)
        {
            // validate arguments
            Assert.IsArgNotNull(leaderboard, "leaderboard");
            return null;
        }

        /// @}
        #endregion
    }
}
