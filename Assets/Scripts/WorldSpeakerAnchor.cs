using System.Collections.Generic;
using UnityEngine;

namespace TimelessEchoes
{
    /// <summary>An authored world subject for introductions without a run task.</summary>
    public sealed class WorldSpeakerAnchor : MonoBehaviour
    {
        public string speakerId;
        [SerializeField] private Vector2 framingCenter = new(0, .5f);
        [SerializeField] private Vector2 framingSize = new(2.5f, 2.5f);
        // Authored character bounds exclude animation-sheet padding and scenery.
        public Bounds FramingBounds => new(transform.TransformPoint(framingCenter),
            new Vector3(framingSize.x * Mathf.Abs(transform.lossyScale.x), framingSize.y * Mathf.Abs(transform.lossyScale.y), 0));
        private static readonly List<WorldSpeakerAnchor> Anchors = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() => Anchors.Clear();
        private void OnEnable() { if (!Anchors.Contains(this)) Anchors.Add(this); }
        private void OnDisable() => Anchors.Remove(this);

        public static Transform Find(string npcId)
        {
            var identity = Canonical(npcId);
            if (string.IsNullOrEmpty(identity)) return null;
            foreach (var anchor in Anchors)
                if (anchor && anchor.isActiveAndEnabled && Canonical(anchor.speakerId) == identity)
                    return anchor.transform;
            return null;
        }

        private static string Canonical(string id) => id switch
        {
            "Witch1" or "EvaWateringCanIntroduction" => "Eva",
            "Farmers1" => "Farmers",
            "Barkley1" => "Barkley", "Gill1" => "Gill", "Ivan1" => "Ivan",
            "OldTimer1" => "OldTimer", "Reaper1" => "Carl", "OrinHubIntroduction" => "Orin",
            _ => id
        };
    }
}
