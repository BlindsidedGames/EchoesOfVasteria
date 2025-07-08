using System.Collections.Generic;
using UnityEngine;

namespace TimelessEchoes.NpcGeneration
{
    /// <summary>
    /// Central manager that updates all NPC resource generators and applies offline progress.
    /// </summary>
    public class GenerationManager : MonoBehaviour
    {
        public static GenerationManager Instance { get; private set; }
        [SerializeField] private List<NPCResourceGenerator> generators = new();

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
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


        public NPCResourceGenerator GetGenerator(string npcId)
        {
            return generators.Find(g => g != null && g.NpcId == npcId);
        }
    }
}
