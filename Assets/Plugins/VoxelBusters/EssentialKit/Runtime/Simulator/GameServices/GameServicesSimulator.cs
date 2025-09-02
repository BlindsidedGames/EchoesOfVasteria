using System;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.Editor.NativePlugins;

namespace VoxelBusters.EssentialKit.GameServicesCore.Simulator
{
    public sealed class GameServicesSimulator : SingletonObject<GameServicesSimulator>
    {
        #region properties

        private     GameServicesSimulatorData       m_simulatorData;

        #endregion

        #region Constructors

        private GameServicesSimulator()
        {
            // save instance
            m_simulatorData      = LoadFromDisk() ?? CreateInstance();
        }

        #endregion

        #region Database methods

        private GameServicesSimulatorData CreateInstance()
        {
            // get information required to initialize simulator
            var     achievementSettingsArray    = GameServices.AchievementDefinitions;
            var     leaderboardSettingsArray    = GameServices.LeaderboardDefinitions;

            // create simulator specific objects
            var     descriptions    = Array.ConvertAll(achievementSettingsArray, (item) => new AchievementDescriptionData() { Id = item.Id, Title = item.Title });
            var     leaderboards    = Array.ConvertAll(leaderboardSettingsArray, (item) => new LeaderboardData() { Id = item.Id, Title = item.Title });
            return new GameServicesSimulatorData(descriptions, leaderboards);
        }

        private GameServicesSimulatorData LoadFromDisk()
        {
            return SimulatorServices.GetObject<GameServicesSimulatorData>(NativeFeatureType.kGameServices);
        }

        private void SaveData()
        {
            SimulatorServices.SetObject(NativeFeatureType.kGameServices, m_simulatorData);
        }

        public static void Reset() 
        {
            SimulatorServices.RemoveObject(NativeFeatureType.kGameServices);
        }

        #endregion

        #region Public methods

        public IEnumerator<AchievementDescriptionData> GetAchievementDescriptions(out Error error)
        {
            if (ValidateSession(out error))
            {
                return m_simulatorData.GetAchievementDescriptions();
            }

            return null;
        }

        public IEnumerator<AchievementData> GetAchievements(out Error error)
        {
            if (ValidateSession(out error))
            {
                return m_simulatorData.GetAchievements(GetLocalPlayer().Id);
            }

            return null;
        }

        public void ReportAchievementProgress(string achievementId, float percentageCompleted, out bool isCompleted, out Error error)
        {
            // set default value
            isCompleted     = false;

            // check auth and update local copy
            if (ValidateSession(out error))
            {
                // find object
                var     data    = m_simulatorData.GetAchievement(achievementId, GetLocalPlayer().Id);
                if (data == null)
                {
                    data        = new AchievementData(achievementId, GetLocalPlayer().Id);
                }

                // update properties
                data.PercentageCompleted    = percentageCompleted;
                data.IsCompleted            = Mathf.Approximately(100f, data.PercentageCompleted);
                m_simulatorData.AddAchievement(data);

                // update reference values
                isCompleted                 = data.IsCompleted;

                // save changes
                SaveData();
            }
        }

        public Texture2D GetAchievementImage(string achievementId, out Error error)
        {
            if (ValidateSession(out error))
            {
                return SimulatorServices.GetRandomImage();
            }

            return null;
        }

        #endregion

        #region Leaderboard methods

        public IEnumerator<LeaderboardData> GetLeaderboards(out Error error)
        {
            if (ValidateSession(out error))
            {
                return m_simulatorData.GetLeaderboards();
            }

            return null;
        }

        public void ReportLeaderboardScore(string leaderboardId, long value, out Error error, string tag = null)
        {
            if (ValidateSession(out error))
            {
                LeaderboardDefinition definition = GameServices.FindLeaderboardDefinitionWithId(leaderboardId);
                m_simulatorData.AddScore(new ScoreData() { LeaderboardId = leaderboardId, LeaderboardPlatformId = definition.GetPlatformIdForActivePlatform(), PlayerId = GetLocalPlayer().Id, Value = value, Tag = tag } );

                // save changes
                SaveData();
            }
        }

        public IEnumerator<ScoreData> GetLeaderboardScores(string leaderboardId, out ScoreData localPlayerScore, out Error error)
        {
            // set default value
            localPlayerScore        = null;

            if (ValidateSession(out error))
            {
                localPlayerScore    = m_simulatorData.GetLeaderboardScore(leaderboardId, GetLocalPlayerId());
                return m_simulatorData.GetLeaderboardScores(leaderboardId);
            }

            return null;
        }

        public Texture2D GetLeaderboardImage(string leaderboardId, out Error error)
        {
            if (ValidateSession(out error))
            {
                return SimulatorServices.GetRandomImage();
            }

            return null;
        }

        #endregion

        #region Player methods

        public PlayerData[] FindPlayers(string[] ids)
        {
            return Array.ConvertAll(ids, (id) => FindPlayerWithId(id));
        }

        public PlayerData FindPlayerWithId(string id)
        {
            var     player          = m_simulatorData.FindPlayer(id);
            if (player == null)
            {
                return null;
            }

            return player;
        }

        public PlayerData GetLocalPlayer()
        {
            var     localPlayer     = m_simulatorData.GetLocalPlayer();
            if (localPlayer == null)
            {
                return null;
            }

            return localPlayer;
        }

        public Texture2D GetPlayerImage(string playerId, out Error error)
        {
            if (ValidateSession(out error))
            {
                return SimulatorServices.GetRandomImage();
            }

            return null;
        }

        public bool IsAuthenticated()
        {
            Error   error;
            if (ValidateSession(out error))
            {
                return true;
            }

            return false;
        }

        public void Authenticate(bool interactive, Action<LocalPlayerAuthStatus, Error> callback)
        {
            // check whether required permission is already granted
            Error   error;
            if (ValidateSession(out error))
            {
                callback(LocalPlayerAuthStatus.Authenticated, null);
            }
            else if (!interactive)
            {
                callback(LocalPlayerAuthStatus.NotAvailable, null);
            }
            else
            {
                callback(LocalPlayerAuthStatus.Authenticating, null);

                // show prompt to user asking for required permission
                var     newAlertDialog      = new AlertDialogBuilder()
                    .SetTitle("Game Services Simulator")
                    .SetMessage("Would you like to login?")
                    .AddButton("Ok", () => 
                    { 
                        // save selection
                        PlayerData  player  = new PlayerData() { Id = "1", Name = "You" };
                        m_simulatorData.AddPlayer(player);
                        m_simulatorData.SetLocalPlayer(player);

                        // send result
                        callback(LocalPlayerAuthStatus.Authenticated, null);

                        // save changes
                        SaveData();
                    })
                    .AddCancelButton("Cancel", () => 
                    { 
                        // send result
                        callback(LocalPlayerAuthStatus.NotAvailable, null);
                    })
                    .Build();
                newAlertDialog.Show();
            }
        }

        private bool ValidateSession(out Error error)
        {
            var     localPlayer = m_simulatorData.GetLocalPlayer();
            if (localPlayer != null)
            {
                error   = null;

                return true;
            }

            error       = GameServicesError.NotAuthenticated("The requested operation could not be completed because user authentication is missing.");

            return false;
        }

        private string GetLocalPlayerId()
        {
            return m_simulatorData.GetLocalPlayerId();
        }

        private void AddSamplePlayers()
        {
            m_simulatorData.AddPlayer(new PlayerData() { Id = "2", Name = "Player 1" });
            m_simulatorData.AddPlayer(new PlayerData() { Id = "3", Name = "Player 2" });
            m_simulatorData.AddPlayer(new PlayerData() { Id = "4", Name = "Player 3" });
            m_simulatorData.AddPlayer(new PlayerData() { Id = "5", Name = "Player 4" });
            m_simulatorData.AddPlayer(new PlayerData() { Id = "6", Name = "Player 5" });
        }

        #endregion

        #region Preseantation methods

        public void ShowAchievementView(Action<Error> callback)
        {
            Error   error;
            if (ValidateSession(out error))
            {
                callback(null);
                return;
            }

            callback(error);
        }

        public void ShowLeaderboardView(Action<Error> callback)
        {
            Error   error;
            if (ValidateSession(out error))
            {
                callback(null);
                return;
            }

            callback(error);
        }

        #endregion
    }
}