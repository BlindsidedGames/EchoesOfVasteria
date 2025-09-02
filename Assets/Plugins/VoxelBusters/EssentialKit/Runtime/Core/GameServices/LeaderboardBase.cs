using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.GameServicesCore
{
    public abstract class LeaderboardBase : NativeObjectBase, ILeaderboard
    {
        #region Constructors

        protected LeaderboardBase(string id, string platformId)
        {
            // set properties
            Id              = id;
            PlatformId      = platformId;
        }

        #endregion

        #region Abstract methods

        protected abstract string GetTitleInternal();

        protected abstract LeaderboardPlayerScope GetPlayerScopeInternal();

        protected abstract void SetPlayerScopeInternal(LeaderboardPlayerScope value);

        protected abstract LeaderboardTimeScope GetTimeScopeInternal();
        
        protected abstract void SetTimeScopeInternal(LeaderboardTimeScope value);

        protected abstract ILeaderboardScore GetLocalPlayerScoreInternal();

        protected abstract void ReportScoreInternal(long score, ReportScoreInternalCallback callback, string tag = null);

        protected abstract void LoadTopScoresInternal(LoadScoresInternalCallback callback);

        protected abstract void LoadPlayerCenteredScoresInternal(LoadScoresInternalCallback callback);

        protected abstract void LoadNextInternal(LoadScoresInternalCallback callback);

        protected abstract void LoadPreviousInternal(LoadScoresInternalCallback callback);
        
        protected abstract void LoadImageInternal(LoadImageInternalCallback callback);

        #endregion

        #region Base class methods

        public override string ToString()
        {
            return $"[Id={Id}, PlatformId={PlatformId}, Title={Title}, PlayerScope={PlayerScope}, TimeScope={TimeScope}, LoadScoresQuerySize={LoadScoresQuerySize}]";
        }

        #endregion

        #region IGameServicesLeaderboard implementation

        public string Id
        {
            get;
            internal set;
        }

        public string PlatformId
        {
            get;
            private set;
        }

        public string Title
        {
            get
            {
                return GetTitleInternal();
            }
        }

        public LeaderboardPlayerScope PlayerScope
        {
            get
            {
                return GetPlayerScopeInternal();
            }
            set
            {
                SetPlayerScopeInternal(value);
            }
        } 
            
        public LeaderboardTimeScope TimeScope
        {
            get
            {
                return GetTimeScopeInternal();
            }
            set
            {
                SetTimeScopeInternal(value);
            }
        }

        public int LoadScoresQuerySize
        {
            get;
            set;
        }

        public ILeaderboardScore LocalPlayerScore
        {
            get
            {
                return GetLocalPlayerScoreInternal();
            }
        }

        public void ReportScore(long score, CompletionCallback callback, string tag = null)
        {
            // retain object to avoid unintentional releases
            ManagedObjectReferencePool.Retain(this);
            
            ReportScoreInternal(score, (success, error) =>
            {
                // send result
                SendReportScoreResult(callback, success, error);

                // remove object from cache
                ManagedObjectReferencePool.Release(this);
            }, tag);
        }

        public void LoadTopScores(EventCallback<LeaderboardLoadScoresResult> callback)
        {
            LoadTopScoresInternal((scores, localPlayerScore, error) =>
            {
                // send result
                SendLoadScoresResult(callback, scores, localPlayerScore, error);
            });
        }

        public void LoadPlayerCenteredScores(EventCallback<LeaderboardLoadScoresResult> callback)
        {
            LoadPlayerCenteredScoresInternal((scores, localPlayerScore, error) =>
            {
                // send result
                SendLoadScoresResult(callback, scores, localPlayerScore, error);
            });
        }


        public void LoadNext(EventCallback<LeaderboardLoadScoresResult> callback)
        {
            LoadNextInternal((scores, localPlayerScore, error) =>
            {
                // send result
                SendLoadScoresResult(callback, scores, localPlayerScore, error);
            });
        }

        public void LoadPrevious(EventCallback<LeaderboardLoadScoresResult> callback)
        {
            LoadPreviousInternal((scores, localPlayerScore, error) =>
            {
                // send result
                SendLoadScoresResult(callback, scores, localPlayerScore, error);
            });
        }

        public void LoadImage(EventCallback<TextureData> callback)
        {
            // make call
            LoadImageInternal((imageData, error) =>
            {
                // send result to caller object
                var     data    = (imageData == null) ? null : new TextureData(imageData);
                CallbackDispatcher.InvokeOnMainThread(callback, data, error);
            });
        }

        #endregion

        #region Private methods

        private void SendLoadScoresResult(EventCallback<LeaderboardLoadScoresResult> callback, ILeaderboardScore[] scores, ILeaderboardScore localPlayerScore, Error error)
        {
            // fallback case to avoid null values
            scores          = scores ?? new ILeaderboardScore[0];

            // send result to caller object
            var     result = new LeaderboardLoadScoresResult(scores, localPlayerScore);
            CallbackDispatcher.InvokeOnMainThread(callback, result, error);
        }

        private void SendReportScoreResult(CompletionCallback callback, bool success, Error error)
        {
                // send result to caller object
                CallbackDispatcher.InvokeOnMainThread(callback, success, error);
        }

        #endregion
    }
}