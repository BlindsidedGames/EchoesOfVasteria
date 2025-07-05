using System.Collections.Generic;
using UnityEngine;
using static Blindsided.EventHandler;

namespace TimelessEchoes.NpcGeneration
{
    /// <summary>
    /// Central manager that updates all NPC resource generators and applies offline progress.
    /// </summary>
    public class GenerationManager : MonoBehaviour
    {
        [SerializeField] private List<NPCResourceGenerator> generators = new();

        private void OnEnable()
        {
            AwayFor += OnAwayForTime;
        }

        private void OnDisable()
        {
            AwayFor -= OnAwayForTime;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            foreach (var gen in generators)
            {
                if (gen != null)
                    gen.Tick(dt);
            }
        }

        private void OnAwayForTime(float seconds)
        {
            foreach (var gen in generators)
            {
                if (gen != null)
                    gen.ApplyOfflineProgress(seconds);
            }
        }

        public NPCResourceGenerator GetGenerator(string npcId)
        {
            return generators.Find(g => g != null && g.NpcId == npcId);
        }
    }
}
