#if UNITY_IOS || UNITY_TVOS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using AOT;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;
using VoxelBusters.CoreLibrary.NativePlugins.iOS;
using VoxelBusters.EssentialKit;

namespace VoxelBusters.EssentialKit.GameServicesCore.iOS
{
    internal sealed class Leaderboard : LeaderboardBase
    {
#region Fields

        private     string                  m_title;
        private     Range                   m_lastRequestRange;
        private     ILeaderboardScore[]     m_scores;
        private     LeaderboardPlayerScope  m_playerScope   = LeaderboardPlayerScope.Global;
        private     LeaderboardTimeScope    m_timeScope     = LeaderboardTimeScope.AllTime;
        private     ILeaderboardScore       m_localPlayerScore;

        #endregion

        #region Constructors

        static Leaderboard()
        {
            // register callbacks
            LeaderboardBinding.NPLeaderboardRegisterCallbacks(HandleLoadLeaderboardsNativeCallback, HandleLoadScoresNativeCallback, NativeCallbackResponder.HandleLoadImageNativeCallback, HandleReportScoreCallbackInternal);
        }

        public Leaderboard(string id, string platformId) 
            : base(id, platformId)
        {
            var     nativePtr   = LeaderboardBinding.NPLeaderboardCreate(platformId);
            
            // set properties
            NativeObjectRef     = new IosNativeObjectRef(nativePtr, retain: false);
            m_title             = LeaderboardBinding.NPLeaderboardGetTitle(nativePtr);
            m_lastRequestRange  = new Range();
        }

        public Leaderboard(IntPtr nativePtr, string id, string platformId) 
            : base(id, platformId)
        {
            // set properties
            NativeObjectRef     = new IosNativeObjectRef(nativePtr);
            m_title             = LeaderboardBinding.NPLeaderboardGetTitle(nativePtr);
            m_lastRequestRange  = new Range();
        }

        ~Leaderboard()
        {
            Dispose(false);
        }

#endregion

#region Static methods

        private static Leaderboard[] CreateLeaderboardArray(ref NativeArray nativeArray)
        {
            return MarshalUtility.ConvertNativeArrayItems(
                arrayPtr: nativeArray.Pointer,
                length: nativeArray.Length, 
                converter: (input) =>
                {
                    string  platformId  = LeaderboardBinding.NPLeaderboardGetId(input);
                    var     settings    = GameServices.FindLeaderboardDefinitionWithPlatformId(platformId);
                    if (null == settings)
                    {
                        DebugLogger.LogWarning(EssentialKitDomain.Default, $"Could not find settings for specified platform id: {platformId}.");
                        return null;
                    }

                    return new Leaderboard(nativePtr: input, id: settings.Id, platformId: platformId);
                }, 
                includeNullObjects: false);
        }

        public static void LoadLeaderboards(string[] leaderboardIds, LoadLeaderboardsInternalCallback callback)
        {
            var     tagPtr      = MarshalUtility.GetIntPtr(callback);
            LeaderboardBinding.NPLeaderboardLoadLeaderboards(leaderboardIds, leaderboardIds.Length, tagPtr);
        }

        public static void ShowLeaderboardView(string leaderboardPlatformId, LeaderboardTimeScope timescope, ViewClosedInternalCallback callback)
        {
            var     gKTimeScope = GameCenterUtility.ConvertToGKLeaderboardTimeScope(timescope);
            LeaderboardBinding.NPLeaderboardShowView(leaderboardPlatformId, gKTimeScope, MarshalUtility.GetIntPtr(callback));
        }

#endregion

#region Base class methods

        protected override string GetTitleInternal()
        {
            return m_title;
        }

        protected override LeaderboardPlayerScope GetPlayerScopeInternal()
        {
            return m_playerScope;
        }

        protected override void SetPlayerScopeInternal(LeaderboardPlayerScope value)
        {
            m_playerScope = value;
        }

        protected override LeaderboardTimeScope GetTimeScopeInternal()
        {
            return m_timeScope;
        }
        
        protected override void SetTimeScopeInternal(LeaderboardTimeScope value)
        {
            m_timeScope = value;
        }

        protected override ILeaderboardScore GetLocalPlayerScoreInternal()
        {
            return m_localPlayerScore;
        }

        protected override void LoadTopScoresInternal(LoadScoresInternalCallback callback)
        {
            LoadScoreInternal(1, LoadScoresQuerySize, true, callback);
        }

        protected override void LoadPlayerCenteredScoresInternal(LoadScoresInternalCallback callback)
        {
            LoadScoreInternal(1, 1, false, (scores, localPlayerScore, error) =>
            {
                // check request status
                if (error == null)
                {
                    int     startIndex = 1;
                    if(m_localPlayerScore != null)
                    {
                        startIndex          = ((int)(m_localPlayerScore.Rank / LoadScoresQuerySize) * LoadScoresQuerySize) + 1;
                    }

                    LoadScoreInternal(startIndex, LoadScoresQuerySize, true, callback);
                }
                else
                {
                    callback(null, null, error);
                }
            });
        }

        protected override void LoadNextInternal(LoadScoresInternalCallback callback)
        {
            // check whether we have necessary data to support pagination
            if ((m_scores == null) || (m_scores.Length == 0))
            {
                LoadTopScoresInternal(callback);
                return;
            }

            // seek to next page results
            int     scoreLength             = m_scores.Length;
            var     lastScoreEntry          = m_scores[scoreLength - 1];
            long    nextPageStartIndex      = lastScoreEntry.Rank + 1;
            LoadScoreInternal(nextPageStartIndex, LoadScoresQuerySize, true, callback);
        }

        protected override void LoadPreviousInternal(LoadScoresInternalCallback callback)
        {
            // check whether we have necessary data to support pagination
            if ((m_scores == null) || (m_scores.Length == 0))
            {
                LoadTopScoresInternal(callback);
                return;
            }

            // seek to previous page results
            var     firstScoreEntry         = m_scores[0];
            long    prevPageStartIndex      = firstScoreEntry.Rank - LoadScoresQuerySize;
            if (prevPageStartIndex < 0)
            {
                prevPageStartIndex  = 1;
            }
            LoadScoreInternal(prevPageStartIndex, LoadScoresQuerySize, true, callback);
        }

        protected override void LoadImageInternal(LoadImageInternalCallback callback)
        {
            var     tagPtr      = MarshalUtility.GetIntPtr(callback);
            LeaderboardBinding.NPLeaderboardLoadImage(AddrOfNativeObject(), tagPtr);
        }

        protected override void ReportScoreInternal(long score, ReportScoreInternalCallback callback, string tag = null)
        {
            var     tagPtr      = MarshalUtility.GetIntPtr(callback);
            LeaderboardBinding.NPLeaderboardReportScore(AddrOfNativeObject(), score, tag, tagPtr);
        }

#endregion

#region Private methods

        private void LoadScoreInternal(long startIndex, int count, bool recordRequest, LoadScoresInternalCallback callback)
        {
            // record request properties, if specified
            if (recordRequest)
            {
                m_lastRequestRange.StartIndex   = startIndex;
                m_lastRequestRange.Count        = count;
            }

            // create intermediate response handler
            LoadScoresInternalCallback internalCallback = (scores, localPlayerScore, error) => {
                m_localPlayerScore = localPlayerScore;
                OnLoadScoreFinished(scores, localPlayerScore, error, callback);                    
            };

            // make request
            var     tagPtr      = MarshalUtility.GetIntPtr(internalCallback);

            var     playerScope =  GameCenterUtility.ConvertToGKLeaderboardPlayerScope(m_playerScope);
            var     timeScope   =  GameCenterUtility.ConvertToGKLeaderboardTimeScope(m_timeScope);
            

            LeaderboardBinding.NPLeaderboardLoadScores(AddrOfNativeObject(), playerScope, timeScope, startIndex, count, tagPtr);
        }

        private void OnLoadScoreFinished(ILeaderboardScore[] scores, ILeaderboardScore localPlayerScore, Error error, LoadScoresInternalCallback callback)
        {
            // check response status
            if (error == null)
            {
                m_scores        = scores;
            }

            // send result to caller object
            callback(scores, localPlayerScore, error);
        }

#endregion

#region Native callback methods

        [MonoPInvokeCallback(typeof(GameServicesLoadArrayNativeCallback))]
        private static void HandleLoadLeaderboardsNativeCallback(ref NativeArray nativeArray, NativeError error, IntPtr tagPtr)
        {
            var     tagHandle   = GCHandle.FromIntPtr(tagPtr);

            try
            {
                // send result
                var     leaderboards    = CreateLeaderboardArray(ref nativeArray);
                var     errorObj        = error.Convert(GameServicesError.kDomain);
                LoadLeaderboardsInternalCallback callback = (LoadLeaderboardsInternalCallback)tagHandle.Target;
                callback(leaderboards, errorObj);
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
            }
            finally
            {
                // release handle
                tagHandle.Free();
            }
        }

        [MonoPInvokeCallback(typeof(GameServicesLoadScoresNativeCallback))]
        private static void HandleLoadScoresNativeCallback(ref NativeArray nativeArray, IntPtr localPlayerScorePtr, NativeError error, IntPtr tagPtr)
        {
            var     tagHandle   = GCHandle.FromIntPtr(tagPtr);

            try
            {
                LoadScoresInternalCallback callback = (LoadScoresInternalCallback)tagHandle.Target;
                Leaderboard leaderboard = callback?.Target as Leaderboard;

                string   leaderboardId   = leaderboard?.Id;
                string   leaderboardPlatformId = leaderboard?.PlatformId; 

                // send result
                var     managedArray    = MarshalUtility.CreateManagedArray(nativeArray.Pointer, nativeArray.Length);
                var     scores          = (managedArray == null) ? null : Array.ConvertAll(managedArray, (nativePtr) => new LeaderboardScore(leaderboardId, leaderboardPlatformId, nativePtr));
                var     errorObj        = error.Convert(GameServicesError.kDomain);

                ILeaderboardScore  localPlayerScore = null;
                if (IntPtr.Zero != localPlayerScorePtr)
                {
                    // create score object=
                    localPlayerScore = new LeaderboardScore(leaderboardId, leaderboardPlatformId, localPlayerScorePtr);
                }

                ((LoadScoresInternalCallback)tagHandle.Target)(scores, localPlayerScore, errorObj);
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
            }
            finally
            {
                // release handle
                tagHandle.Free();
            }
        }

        [MonoPInvokeCallback(typeof(GameServicesReportNativeCallback))]
        private static void HandleReportScoreCallbackInternal(NativeError error, IntPtr tagPtr)
        {
            var     tagHandle   = GCHandle.FromIntPtr(tagPtr);
            
            try
            {
                // send result
                var     errorObj        = error.Convert(GameServicesError.kDomain);
                ((ReportScoreInternalCallback)tagHandle.Target).Invoke((errorObj == null), errorObj);
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
            }
            finally
            {
                // release handle
                tagHandle.Free();
            }
        }

        #endregion

        #region Nested types

        private class Range
        {
            public long StartIndex { get; set; }

            public int Count { get; set; }
        }

#endregion
    }
}
#endif