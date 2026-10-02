using System;
using UnityEngine;

namespace TimelessEchoes.Farming
{
    /// <summary>Presentation reads committed state; the source owns eligibility and durable commands.</summary>
    public interface IFarmPresentationSource
    {
        bool Ready { get; }
        string LastError { get; }
        event Action Changed;
        FarmPresentationSnapshot CapturePresentation();
        bool PrepareBeds();
        bool Plant(string bedId);
        bool HarvestReady();
        void Focus(string bedId);
    }

    public sealed class FarmPresentationSnapshot
    {
        public bool Prepared;
        public bool TownActionsAllowed;
        public bool Discovered;
        public long SeedQuantity;
        public double LogQuantity;
        public double StickQuantity;
        public double BuildLogCost;
        public double BuildStickCost;
        public FarmBedPresentation[] Beds = Array.Empty<FarmBedPresentation>();
    }

    public sealed class FarmBedPresentation
    {
        public string Id;
        public string Title;
        public bool Planted;
        public bool Ready;
        public float Progress01;
        public double RemainingSeconds;
    }

    /// <summary>Reuses authored crop sprites at the existing bed anchors without changing task art.</summary>
    public sealed class FarmView : MonoBehaviour
    {
        [Serializable]
        public sealed class BedVisual
        {
            public string bedId;
            public SpriteRenderer[] plants = Array.Empty<SpriteRenderer>();
        }

        [SerializeField] private MonoBehaviour sourceBehaviour;
        [SerializeField] private Sprite[] radishStages = Array.Empty<Sprite>();
        [SerializeField] private BedVisual[] beds = Array.Empty<BedVisual>();
        private IFarmPresentationSource source;
        private float nextRefresh;

        public void Configure(IFarmPresentationSource presentationSource, Sprite[] stages, BedVisual[] visuals)
        {
            Unsubscribe();
            source = presentationSource;
            sourceBehaviour = presentationSource as MonoBehaviour;
            radishStages = stages ?? Array.Empty<Sprite>();
            beds = visuals ?? Array.Empty<BedVisual>();
            if (isActiveAndEnabled) Subscribe();
            Refresh();
        }

        private void OnEnable()
        {
            source ??= sourceBehaviour as IFarmPresentationSource;
            Subscribe();
            Refresh();
        }

        private void Subscribe()
        {
            if (source == null) return;
            source.Changed -= Refresh;
            source.Changed += Refresh;
        }

        private void Unsubscribe()
        {
            if (source != null) source.Changed -= Refresh;
        }

        private void OnDisable() => Unsubscribe();

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .25f;
            Refresh();
        }

        public void Refresh()
        {
            var snapshot = source != null && source.Ready ? source.CapturePresentation() : null;
            foreach (var visual in beds)
            {
                if (visual == null) continue;
                var bed = snapshot?.Beds == null ? null : Array.Find(snapshot.Beds, item => item != null && item.Id == visual.bedId);
                var visible = snapshot?.Prepared == true && bed?.Planted == true;
                var stage = bed == null ? 0 : GrowthStage(bed.Progress01, bed.Ready);
                var sprite = radishStages.Length == 4 ? radishStages[stage] : null;
                foreach (var plant in visual.plants ?? Array.Empty<SpriteRenderer>())
                {
                    if (!plant) continue;
                    plant.enabled = visible && sprite;
                    if (visible && sprite) plant.sprite = sprite;
                }
            }
        }

        public static int GrowthStage(float progress, bool ready) => ready ? 3 : Mathf.Min(2, Mathf.FloorToInt(Mathf.Clamp01(progress) * 3));
    }
}
