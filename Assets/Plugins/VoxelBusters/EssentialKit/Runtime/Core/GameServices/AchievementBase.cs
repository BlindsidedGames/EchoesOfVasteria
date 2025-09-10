using System;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.GameServicesCore
{
    public abstract class AchievementBase : NativeObjectBase, IAchievement
    {
        #region Constructors

        protected AchievementBase(string id, string platformId)
        {
            // set properties
            Id              = id;
            PlatformId      = platformId;
        }

        #endregion

        #region Abstract methods

        protected abstract double GetPercentageCompletedInternal();

        protected abstract void SetPercentageCompletedInternal(double value);
        
        protected abstract bool GetIsCompletedInternal();

        protected abstract DateTime GetLastReportedDateInternal();

        protected abstract void ReportProgressInternal(ReportAchievementProgressInternalCallback callback);

        #endregion

        #region Base class methods

        public override string ToString()
        {
            return $"[Id={Id}, PlatformId={PlatformId}, PercentageCompleted={PercentageCompleted}, IsCompleted={IsCompleted}, LastReportedDate={LastReportedDate}]";
        }

        #endregion

        #region IGameServicesAchievement implementation

        public string Id { get; internal set; }

        public string PlatformId { get; internal set; }

        public double PercentageCompleted
        {
            get => GetPercentageCompletedInternal();
            set => SetPercentageCompletedInternal(value);
        }

        public bool IsCompleted => GetIsCompletedInternal();

        public DateTime LastReportedDate => GetLastReportedDateInternal();

        public void ReportProgress(CompletionCallback callback)
        {
            // retain object to avoid unintentional release
            ManagedObjectReferencePool.Retain(this);

            // make call
            ReportProgressInternal((success, error) =>
            {
                // send result to caller object
                CallbackDispatcher.InvokeOnMainThread(callback, success, error);

                // remove object from cache
                ManagedObjectReferencePool.Release(this);
            });
        }

        #endregion
    }
}