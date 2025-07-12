using System.Collections.Generic;
using UnityEngine;

namespace TimelessEchoes.NpcGeneration
{
    /// <summary>
    /// Central manager that updates all NPC resource generators and applies offline progress.
    /// </summary>
    [DefaultExecutionOrder(-1)]
    public class DiscipleGenerationManager : MonoBehaviour
    {
        public static DiscipleGenerationManager Instance { get; private set; }
        [SerializeField] private List<DiscipleGenerator> generators = new();

        public IReadOnlyList<DiscipleGenerator> Generators => generators;

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
    }
}
