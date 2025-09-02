#if UNITY_ANDROID
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;

namespace VoxelBusters.EssentialKit.GameServicesCore.Android
{
    public sealed class GameServicesInterface : NativeGameServicesInterfaceBase 
    {
#region Fields

        private NativeGameServices m_instance;
        private AuthChangeInternalCallback m_authChangeCallback;
        private LocalPlayer m_localPlayer;

#endregion

#region Constructors

        public GameServicesInterface()
            : base(isAvailable: true)
        {
            m_instance = new NativeGameServices(NativeUnityPluginUtility.GetContext());
            m_localPlayer = new LocalPlayer();
        }

#endregion

#region INativeGameServicesInterface implementation

        public override void LoadLeaderboards(LeaderboardDefinition[] leaderboardDefinitions, LoadLeaderboardsInternalCallback callback)
        {
            m_instance.LoadLeaderboards(new NativeLoadLeaderboardsListener()
            {
                onSuccessCallback = (nativeList) =>
                {
                    //Filter achievements which are listed in the settings only
                    List<NativeGameLeaderboard> nativeLeaderboards = nativeList.Get();
                    List<Leaderboard> filteredLeaderboards = new List<Leaderboard>();

                    foreach (NativeGameLeaderboard each in nativeLeaderboards)
                    {
                        var settings = GameServices.FindLeaderboardDefinitionWithPlatformId(each.GetId());
                        if (settings != null)
                        {
                            filteredLeaderboards.Add(new Leaderboard(settings.Id, each));
                        }
                    }

                    Leaderboard[] leaderboards = filteredLeaderboards.ToArray();
                    callback(leaderboards, null);
                },
                onFailureCallback = (error) =>
                {
                    callback(null, error.Convert(GameServicesError.kDomain));
                }
            });
        }

        public override void ShowLeaderboard(string leaderboardId, string leaderboardPlatformId, LeaderboardTimeScope timeScope, ViewClosedInternalCallback callback)
        {
            m_instance.ShowLeaderboards(leaderboardPlatformId, Converter.from(timeScope), new NativeViewListener()
            {
                onCloseCallback = (error) =>
                {
                    callback(error.Convert(GameServicesError.kDomain));
                }
            });
        }

        public override ILeaderboard CreateLeaderboard(string id, string platformId)
        {
            NativeGameLeaderboard nativeGameLeaderboard = m_instance.CreateLeaderboard(platformId);
            return new Leaderboard(id, nativeGameLeaderboard);
        }

        public override void LoadAchievementDescriptions(LoadAchievementDescriptionsInternalCallback callback)
        {
            AchievementDescription.SetAchievedDescriptionFormats(GameServices.UnitySettings.AndroidProperties.AchievedDescriptionFormats);
            m_instance.LoadAchievements(new NativeLoadAchievementsListener()
            {
                onSuccessCallback = (NativeArrayBuffer<NativeGameAchievement> nativeArray)  =>
                {
                    Callback action = () =>
                    {

                        List<AchievementDescription> filteredAchievements = new List<AchievementDescription>();
                        int count = nativeArray.Size();
                        for (int i = 0; i < count; i++)
                        {
                            NativeGameAchievement each = nativeArray.Get(i);
                            var settings = GameServices.FindAchievementDefinitionWithPlatformId(each.GetId());
                            if (settings != null)
                            {
                                filteredAchievements.Add(new AchievementDescription(settings.Id, each, settings.NumOfStepsToUnlock));
                            }
                        }
                        callback(filteredAchievements.ToArray(), null);
                    };

                    CallbackDispatcher.InvokeOnMainThread(action);

                },
                onFailureCallback = (error) =>
                {
                    callback(null, error.Convert(GameServicesError.kDomain));
                }
            });
        }

        public override void LoadAchievements(LoadAchievementsInternalCallback callback)
        {
            m_instance.LoadAchievements(new NativeLoadAchievementsListener()
            {
                onSuccessCallback = (NativeArrayBuffer<NativeGameAchievement> nativeArray) =>
                {
                    Callback action = () =>
                    {
                        List<Achievement> filteredAchievements = new List<Achievement>();
                        int count = nativeArray.Size();
                        for (int i = 0; i < count; i++)
                        {
                            NativeGameAchievement each = nativeArray.Get(i);
                            var settings = GameServices.FindAchievementDefinitionWithPlatformId(each.GetId());
                            if (settings == null)
                            {
                                DebugLogger.LogWarning($"Could not find settings for achievement with platform id {each.GetId()}. Cross check your settings for the achievement with this platform id.");
                            }
                            
                            if (settings != null && (!each.GetLastReportedDate().IsNull()))
                            {
                                filteredAchievements.Add(new Achievement(settings.Id, each));
                            }
                        }
                        callback(filteredAchievements.ToArray(), null);
                    };

                    CallbackDispatcher.InvokeOnMainThread(action);
                },
                onFailureCallback = (error) =>
                {
                    callback(null, error.Convert(GameServicesError.kDomain));
                }
            });
        }

        public override void ShowAchievements(ViewClosedInternalCallback callback)
        {
            m_instance.ShowAchievements(new NativeViewListener()
            {
                onCloseCallback = (error) =>
                {
                    callback(error.Convert(GameServicesError.kDomain));
                }
            });
        }

        public override void SetCanShowAchievementCompletionBanner(bool value)
        {
            DebugLogger.LogWarning("This operation is not allowed on Android");
        }

        public override IAchievement CreateAchievement(string id, string platformId)
        {
            NativeGameAchievement nativeGameAchievement = m_instance.CreateAchievement(platformId);
            return new Achievement(id, nativeGameAchievement);
        }

        public override void LoadPlayers(string[] playerIds, LoadPlayersInternalCallback callback)
        {
            m_instance.LoadPlayers(playerIds, new NativeLoadPlayersListener()
            {
                onSuccessCallback = (nativePlayers) =>
                {
                    Player[] players = NativeUnityPluginUtility.Map<NativeGamePlayer, Player>(nativePlayers.Get());
                    callback(players, null);
                },
                onFailureCallback = (error) =>
                {
                    callback(null, error.Convert(GameServicesError.kDomain));
                }
            });
        }

        public override void SetAuthChangeCallback(AuthChangeInternalCallback callback)
        {
            m_authChangeCallback = callback;
        }

        public override void Authenticate(bool interactive)
        {
            m_instance.Authenticate(interactive,new NativePlayerAuthenticationListener()
            {
                onSuccessCallback = (player) =>
                {
                    if(m_authChangeCallback != null)
                    {
                        if(player == null || player.IsNull())
                        {
                            m_localPlayer.SetPlayer(null);
                            m_authChangeCallback(LocalPlayerAuthStatus.NotAvailable, null);
                        }
                        else
                        {
                            m_localPlayer.SetPlayer(player);
                            m_authChangeCallback(LocalPlayerAuthStatus.Authenticated, null);
                        }
                    }
                },
                onFailureCallback = (error) =>
                {
                    if(m_authChangeCallback != null)
                    {
                        m_localPlayer.SetPlayer(null);
                        m_authChangeCallback(LocalPlayerAuthStatus.NotAvailable, error.Convert(GameServicesError.kDomain));
                    }
                }
            });
        }

        public override void Signout()
        {
            m_instance.Signout();
        }

        public override ILocalPlayer GetLocalPlayer()
        {
            return m_localPlayer;
        }

        public override void LoadServerCredentials(List<ServerCredentialAdditionalScope> additionalScopes, LoadServerCredentialsInternalCallback callback)
        {
            int[] scopesArray = additionalScopes.Select( x => (int)GetNativeServerCredentialAdditionalScope(x)).ToArray();
            m_instance.LoadServerCredentials(serverClientId: GameServices.UnitySettings.AndroidProperties.ServerClientId,forceRefreshToken: true, scopesArray, new NativeLoadServerCredentials()
            {
                onSuccessCallback = (serverAuthCode, serverCredentialAdditionalScopes) =>
                {
                    if (callback != null)
                    {
                        ServerCredentials.AndroidPlatformProperties properties = new ServerCredentials.AndroidPlatformProperties(serverAuthCode);
                        ServerCredentials credentials = new ServerCredentials(androidProperties: properties);

                        callback(credentials, serverCredentialAdditionalScopes.Get().Select(x => (ServerCredentialAdditionalScope)x).ToList() ,null);
                    }
                },
                onFailureCallback = (error) =>
                {
                    if(callback != null)
                    {
                        callback(null, null, error.Convert(GameServicesError.kDomain));
                    }
                }
            });
            
        }

#endregion

#region Private Methods

        private NativeServerCredentialAdditionalScope GetNativeServerCredentialAdditionalScope(ServerCredentialAdditionalScope scope)
        {
            switch (scope)
            {
                case ServerCredentialAdditionalScope.None:
                    return NativeServerCredentialAdditionalScope.None;
                case ServerCredentialAdditionalScope.Email:
                    return NativeServerCredentialAdditionalScope.Email;
                case ServerCredentialAdditionalScope.OpenId:
                    return NativeServerCredentialAdditionalScope.OpenId;
                case ServerCredentialAdditionalScope.Profile:
                    return NativeServerCredentialAdditionalScope.Profile;
                default:
                    return NativeServerCredentialAdditionalScope.None;
            }
        }

#endregion
    }
}
#endif