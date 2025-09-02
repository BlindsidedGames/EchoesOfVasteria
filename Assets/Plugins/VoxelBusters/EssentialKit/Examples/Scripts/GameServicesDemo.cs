using System;
using UnityEngine;
using UnityEngine.UI;
// key namespaces
using VoxelBusters.CoreLibrary;
// internal namespace
using VoxelBusters.CoreLibrary.NativePlugins.DemoKit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace VoxelBusters.EssentialKit.Demo
{
    public class GameServicesDemo : DemoActionPanelBase<GameServicesDemoAction, GameServicesDemoActionType>
    {
        #region Fields

        [SerializeField]
        private     RectTransform[]     m_authDependentObjects                      = null;

        [SerializeField]
        private     RectTransform       m_leaderboardsNode                          = null;

        [SerializeField]
        private     RectTransform[]     m_leaderboardDependentObjects               = null;

        [SerializeField]
        private     InputField          m_leaderboardScoreInput                     = null;

        [SerializeField]
        private     RectTransform       m_yourAchivementsNode                       = null;

        [SerializeField]
        private     RectTransform[]     m_achievementDescriptionDependentObjects    = null;

        [SerializeField]
        private     InputField          m_achievementProgressInput                  = null;

        [SerializeField]
        private     Toggle              m_selectableItemPrefab                      = null;

        [SerializeField]
        private     RawImage            m_texturePreview                            = null;
        
        [SerializeField]
        private     Toggle              m_isInteractiveAuthenticationToggle         = null;

        private     int                 m_leaderboardIndex                          = 0;

        private     int                 m_achievementIndex                          = 0;
        
        #endregion

        #region Base class methods

        protected override void Start()
        {
            base.Start();

            // set default state
            SetAuthDependentObjectState(GameServices.IsAuthenticated);
            SetLeaderboardDependentObjectState(false);
            SetAchievementDependentObjectState(false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            // register for events
            GameServices.OnAuthStatusChange += OnAuthStatusChange;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            // unregister from events
            GameServices.OnAuthStatusChange -= OnAuthStatusChange;
        }

        protected override void OnActionSelectInternal(GameServicesDemoAction selectedAction)
        {
            var     currentSelectedLeaderboard   = GetCurrentLeaderboard();

            switch (selectedAction.ActionType)
            {
                case GameServicesDemoActionType.IsAuthenticated:
                    bool    isAuthenticated = GameServices.IsAuthenticated;
                    Log("Is authenticated: " + isAuthenticated);
                    SetAuthDependentObjectState(isAuthenticated);
                    break;

                case GameServicesDemoActionType.Authenticate:
                    GameServices.Authenticate(interactive: m_isInteractiveAuthenticationToggle.isOn); //pass interactive:false for silent authentication, true for prompting login dialog. Default is true.
                    break;

                case GameServicesDemoActionType.LocalPlayer:
                    Log("Local player: " + GameServices.LocalPlayer);
                    break;

                case GameServicesDemoActionType.Signout:
                    if(GameServices.LocalPlayer.IsAuthenticated)
                        Log("Trying to Signout player: " + GameServices.LocalPlayer);

                    GameServices.Signout();
                    break;

                case GameServicesDemoActionType.LoadLeaderboards:
                    GameServices.LoadLeaderboards((result, error) =>
                    {
                        if (error == null)
                        {
                            // update UI
                            SetLeaderboardDependentObjectState(true);
                            CreateLeaderboardOutlets(result.Leaderboards);

                            // show console messages
                            var     leaderboards    = result.Leaderboards;
                            Log("Request to load leaderboards finished successfully.");
                            Log("Total leaderboards fetched: " + leaderboards.Length);
                            Log("Below are the available leaderboards:");
                            for (int iter = 0; iter < leaderboards.Length; iter++)
                            {
                                var     eachLeaderboard    = leaderboards[iter];
                                Log(string.Format("[{0}]: {1}", iter, eachLeaderboard));
                                Log("------------------------------------------------");
                            }
                        }
                        else
                        {
                            Log("Request to load leaderboards failed with error. Error: " + error);
                        }
                    });

                    break;
                case GameServicesDemoActionType.LoadTopScores:
                    if (currentSelectedLeaderboard == null)
                    {
                        LogLeaderboardNotSelected();
                    }
                    else
                    {
                        LoadTopScores(currentSelectedLeaderboard.Id);
                    }
                    break;

                case GameServicesDemoActionType.LoadPlayerCenteredScores:
                    if (currentSelectedLeaderboard == null)
                    {
                        LogLeaderboardNotSelected();
                    }
                    else
                    {
                        LoadPlayerCenteredScores(currentSelectedLeaderboard.Id);
                    }
                    break;
                case GameServicesDemoActionType.ReportScore:
                    if (currentSelectedLeaderboard == null)
                    {
                        LogLeaderboardNotSelected();
                    }
                    else
                    {
                        long    score       = GetLeaderboardScoreInput();
                        GameServices.ReportScore(currentSelectedLeaderboard, score, (success, error) =>
                        {
                            if (success)
                            {
                                Log("Request to submit score finished successfully.");
                            }
                            else
                            {
                                Log("Request to submit score failed with error: " + error.Description);
                            }
                        }, tag: "ak47gun");
                    }
                    break;

                case GameServicesDemoActionType.LoadAchievementDescriptions:
                    GameServices.LoadAchievementDescriptions((result, error) =>
                    {
                        if (error == null)
                        {
                            // update UI
                            SetAchievementDependentObjectState(true);
                            CreateAchievementOutlets(result.AchievementDescriptions);

                            // show console messages
                            var     descriptions    = result.AchievementDescriptions;
                            Log("Request to load achievement descriptions finished successfully.");
                            Log("Total achievement descriptions fetched: " + descriptions.Length);
                            Log("Below are the available achievement descriptions:");
                            for (int iter = 0; iter < descriptions.Length; iter++)
                            {
                                var     description1    = descriptions[iter];
                                Log(string.Format("[{0}]: {1}", iter, description1));
                            }
                        }
                        else
                        {
                            Log("Request to load achievement descriptions failed with error. Error: " + error);
                        }
                    });
                    break;

                case GameServicesDemoActionType.LoadAchievements:
                    GameServices.LoadAchievements((result, error) =>
                    {
                        if (error == null)
                        {
                            // show console messages
                            var     achievements    = result.Achievements;
                            Log("Request to load achievements finished successfully.");
                            Log("Total achievements fetched: " + achievements.Length);
                            Log("Below are the available achievements:");
                            for (int iter = 0; iter < achievements.Length; iter++)
                            {
                                var     achievement1    = achievements[iter];
                                Log(string.Format("[{0}]: {1}", iter, achievement1));
                            }
                        }
                        else
                        {
                            Log("Request to load achievements failed with error. Error: " + error);
                        }
                    });
                    break;

                case GameServicesDemoActionType.ReportAchievementProgress:
                    var     description2    = GetCurrentAchievementDescription();
                    if (description2 == null)
                    {
                        LogAchievementNotSelected();
                    }
                    else
                    {
                        double  percentageCompleted = GetAchievementScoreInput();
                        GameServices.ReportAchievementProgress(description2, percentageCompleted, (success, error) =>
                        {
                            if (success)
                            {
                                Log("Request to submit progress finished successfully.");
                            }
                            else
                            {
                                Log("Request to submit progress failed with error. Error: " + error);
                            }
                        });
                    }
                    break;

                case GameServicesDemoActionType.GetNumOfStepsRequiredToUnlockAchievement:
                    var     description3    = GetCurrentAchievementDescription();
                    if (description3 == null)
                    {
                        LogAchievementNotSelected();
                    }
                    else
                    {
                        Log("Number of steps required to unlock achievement: " + description3.NumberOfStepsRequiredToUnlockAchievement);
                    }
                    break;

                case GameServicesDemoActionType.ShowLeaderboards:
                    GameServices.ShowLeaderboards(callback: (result, error) =>
                    {
                        LogViewClosed();
                    });
                    break;

                case GameServicesDemoActionType.ShowAchievements:
                    GameServices.ShowAchievements((result, error) =>
                    {
                        LogViewClosed();
                    });
                    break;

                case GameServicesDemoActionType.LoadServerCredentials:
                    
                    var additionalScopes = new List<ServerCredentialAdditionalScope>
                    {
                        ServerCredentialAdditionalScope.Email
                    };
                    
                    GameServices.LoadServerCredentials(additionalScopes, (result, error) =>
                    {
                        if(error == null)
                        {
                            LogServerCredentials(result.ServerCredentials);
                        }
                        else
                        {
                            Log("Loading server credentials failed with error. Error: " + error);
                        }
                    });
                    //Or call just without any additional scopes
                    //GameServices.LoadServerCredentials((result, error) =>
                    //{
                    //    if(error == null)
                    //    {
                    //        LogServerCredentials(result.ServerCredentials);
                    //    }
                    //    else
                    //    {
                    //        Log("Loading server credentials failed with error. Error: " + error);
                    //    }
                    //});
                    break;

                case GameServicesDemoActionType.LoadFriends:
                    GameServices.LoadFriends((result, error) =>
                    {
                        if(error == null)
                        {
                            Log("Received friends: " + result.Players.Length);
                            foreach(var friend in result.Players)
                            {
                                Log("Friend : " + friend);
                            }
                        }
                        else
                        {
                            Log("Loading friends failed with error. Error: " + error);
                        }
                    });
                    break;

                case GameServicesDemoActionType.AddFriend:
                    GameServices.AddFriend("player_id", (success, error) =>
                    {
                        if(success)
                        {
                            Log("Request to add friend request finished successfully.");
                        }
                        else
                        {
                            Log("Request to add friend request failed with error. Error: " + error);
                        }
                    });
                    break;

                case GameServicesDemoActionType.ResourcePage:
                    ProductResources.OpenResourcePage(NativeFeatureType.kGameServices);
                    break;
            }
        }

        #endregion

        #region Plugin callback methods

        private void OnAuthStatusChange(GameServicesAuthStatusChangeResult result, Error error)
        {
            // update ui
            SetAuthDependentObjectState(result.AuthStatus == LocalPlayerAuthStatus.Authenticated);

            // update console
            Log("Received auth status change event");
            Log("Auth status: " + result.AuthStatus);
            if(result.AuthStatus == LocalPlayerAuthStatus.Authenticated)
                Log("Local player: " + result.LocalPlayer);
        }

        private void SetAuthDependentObjectState(bool active)
        {
            foreach (var rect in m_authDependentObjects)
            {
                rect.gameObject.SetActive(active);
            }
        }

        #endregion

        #region Private methods

        private void SetLeaderboardDependentObjectState(bool active)
        {
            foreach (var rect in m_leaderboardDependentObjects)
            {
                rect.gameObject.SetActive(active);
            }
        }

        private void SetAchievementDependentObjectState(bool active)
        {
            foreach (var rect in m_achievementDescriptionDependentObjects)
            {
                rect.gameObject.SetActive(active);
            }
        }

        private void LogViewClosed()
        {
            Log("View is closed");
        }

        private void LogServerCredentials(ServerCredentials credentials)
        {
            if(credentials.IosProperties != null)
            {
                Log("Public key Url : " + credentials.IosProperties.PublicKeyUrl);
                Log("Signature length : " + credentials.IosProperties.Signature.Length);
                Log("Salt length : " + credentials.IosProperties.Salt.Length);
                Log("Timestamp : " + credentials.IosProperties.Timestamp);
            }
            else if(credentials.AndroidProperties != null)
            {
                Log("ServerAuthCode : " + credentials.AndroidProperties.ServerAuthCode);
            }
            else
            {
                Log("No credentials available");
            }
            
        }

        private IEnumerator VerifyIosServerCredentials(ServerCredentials credentials)
        {
            #pragma warning disable
            WWWForm form = new WWWForm();
            form.AddField("publicKeyUrl", credentials.IosProperties.PublicKeyUrl);
            form.AddField("timestamp", System.Convert.ToString(credentials.IosProperties.Timestamp));
            form.AddField("signature", System.Convert.ToBase64String(credentials.IosProperties.Signature));
            form.AddField("salt", System.Convert.ToBase64String(credentials.IosProperties.Salt));
            form.AddField("playerId", GameServices.LocalPlayer.Identifier);
            form.AddField("bundleId", Application.identifier);

            UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Post("https://.../authenticate", form);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log("Success response from server! " + www.downloadHandler.text);
            }
            #pragma warning restore
        }

        #endregion

        #region Leaderboard handling methods

        private void CreateLeaderboardOutlets(ILeaderboard[] leaderboards)
        {
            var     items   = leaderboards == null ? null : Array.ConvertAll(leaderboards, (item) => item.Id);
            DemoHelper.CreateItems(m_leaderboardsNode, m_selectableItemPrefab, items, SetCurrentLeaderboardIndex);
        }

        private int GetCurrentLeaderboardIndex()
        {
            return m_leaderboardIndex;
        }

        private void SetCurrentLeaderboardIndex(int index)
        {
            var     leaderboards    = GameServices.Leaderboards;
            m_leaderboardIndex      = Mathf.Clamp(index, 0, leaderboards.Length);
        }

        private ILeaderboard GetCurrentLeaderboard()
        {
            var     leaderboards    = GameServices.Leaderboards;
            if (leaderboards.Length > 0)
            {
                int     index       = GetCurrentLeaderboardIndex();
                return  leaderboards[index];
            }   

            return null;
        }

        private long GetLeaderboardScoreInput()
        {
            long        value;
            long.TryParse(m_leaderboardScoreInput.text, out value);

            return value;
        }

        private void LogLeaderboardNotSelected()
        {
            Log("Leaderboard not selected.");
        }

        #endregion

        #region Achievement handling methods

        private void CreateAchievementOutlets(IAchievementDescription[] descriptions)
        {
            var     items   = (descriptions == null) ? null : Array.ConvertAll(descriptions, (item) => item.Id);
            DemoHelper.CreateItems(m_yourAchivementsNode, m_selectableItemPrefab, items, SetCurrentAchievementIndex);
        }

        private int GetCurrentAchievementIndex()
        {
            return m_achievementIndex;
        }

        private void SetCurrentAchievementIndex(int index)
        {
            var     descriptions    = GameServices.AchievementDescriptions;
            m_achievementIndex      = Mathf.Clamp(index, 0, descriptions.Length);
        }

        private IAchievementDescription GetCurrentAchievementDescription()
        {
            var     descriptions    = GameServices.AchievementDescriptions;
            if (descriptions.Length > 0)
            {
                int     index       = GetCurrentAchievementIndex();
                return  descriptions[index];
            }

            return null;
        }

        private double GetAchievementScoreInput()
        {
            float  value;
            float.TryParse(m_achievementProgressInput.text, out value);

            return Mathf.Clamp(value, 0f, 100f);
        }

        private void LogAchievementNotSelected()
        {
            Log("Achievement not selected.");
        }

        private void LoadTopScores(string leaderboardId)
        {
            ILeaderboard leaderboard = GameServices.CreateLeaderboard(leaderboardId);
            leaderboard.LoadScoresQuerySize = 3;
            leaderboard.TimeScope = LeaderboardTimeScope.AllTime;
            leaderboard.LoadTopScores((result, error) =>
            {
                Log("Scores length : " + result.Scores.Length);
                ILeaderboardScore localPlayerScore = result.LocalPlayerScore;
                if (localPlayerScore != null)
                {
                    Log($"Local Player Score: [Player: {localPlayerScore.Player.DisplayName}, Rank: {localPlayerScore.Rank}, Value: {localPlayerScore.Value}]");
                }
                else
                {
                    Log("No local player score available!");
                }

                Log("Displaying scores..." + result.Scores.Length);
                foreach(ILeaderboardScore score in result.Scores)
                {
                    Log($"Score: [Player: {score.Player.DisplayName}, Rank: {score.Rank}, Value: {score.Value}]");
                }

                Log($"Loading leaderboard image for {leaderboardId}...");
                leaderboard.LoadImage((innerResult, innerError) =>
                {
                    if (innerError == null)
                    {
                        Log("Leaderboard Image loaded successfully");
                        m_texturePreview.texture = innerResult.GetTexture();
                    }
                    else
                    {
                        Log("Error loading leaderboard image : " + innerError.Description);
                        m_texturePreview.texture = null;
                    }
                });
            });
        }

        private void LoadPlayerCenteredScores(string leaderboardId)
        {
            ILeaderboard leaderboard = GameServices.CreateLeaderboard(leaderboardId);
            leaderboard.LoadScoresQuerySize = 3;
            leaderboard.TimeScope = LeaderboardTimeScope.AllTime;
            leaderboard.LoadPlayerCenteredScores((result, error) =>
            {
                Log("Scores length : " + result.Scores.Length);
                ILeaderboardScore localPlayerScore = result.LocalPlayerScore;
                if (localPlayerScore != null)
                {
                    Log(string.Format("Local Player Score : {0}  Rank : {1}", localPlayerScore.Value, localPlayerScore.Rank));
                }
                else
                {
                    Log("No local player score available!");
                }

                Log("Displaying scores..." + result.Scores.Length);
                foreach(ILeaderboardScore score in result.Scores)
                {
                    Log($"Score: [Player: {score.Player.DisplayName}, Rank: {score.Rank}, Value: {score.Value}]");
                }

                Log($"Loading leaderboard image for {leaderboardId}...");
                leaderboard.LoadImage((innerResult, innerError) =>
                {
                    if (innerError == null)
                    {
                        Log("Leaderboard Image loaded successfully");
                        m_texturePreview.texture = innerResult.GetTexture();
                    }
                    else
                    {
                        Log("Error loading leaderboard image : " + innerError.Description);
                        m_texturePreview.texture = null;
                    }
                });
            });
        }
        #endregion
    }
}