using UnityEngine;

namespace TimelessEchoes.NpcGeneration
{
    /// <summary>
    /// Updates a town crop sprite based on its disciple generator progress.
    /// </summary>
    public class DiscipleCropGrowth : MonoBehaviour
    {
        [SerializeField] private Disciple disciple;
        [SerializeField] private DiscipleGenerator generator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite[] growthStages = new Sprite[4];

        private int currentStage = -1;

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            if (generator == null)
                FindGenerator();
        }

        private void Update()
        {
            if (generator == null)
            {
                FindGenerator();
                if (generator == null)
                    return;
            }

            if (growthStages == null || growthStages.Length == 0 || generator.Interval <= 0f)
                return;

            float pct = Mathf.Clamp01(generator.Progress / generator.Interval);
            int stage = Mathf.Clamp(Mathf.FloorToInt(pct * growthStages.Length), 0, growthStages.Length - 1);

            if (stage != currentStage)
            {
                currentStage = stage;
                if (spriteRenderer != null && currentStage < growthStages.Length)
                    spriteRenderer.sprite = growthStages[currentStage];
            }
        }

        private void FindGenerator()
        {
            if (disciple == null)
                return;

            var manager = DiscipleGenerationManager.Instance;
            if (manager == null)
                return;

            foreach (var gen in manager.Generators)
            {
                if (gen != null && gen.DiscipleName == disciple.name)
                {
                    generator = gen;
                    break;
                }
            }
        }
    }
}
