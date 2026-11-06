using UnityEngine;

namespace TimelessEchoes.Tasks
{
    /// <summary>One adventure harvest per spawned tree. Picking preserves its trunk and timber tasks.</summary>
    public sealed class FruitHarvestTask : FarmingTask
    {
        [SerializeField] private SpriteRenderer treeRenderer;
        [SerializeField] private Sprite fruitingSprite;
        [SerializeField] private Sprite harvestedSprite;
        [SerializeField] private Transform harvestPoint;
        private Vector3 authoredPosition;
        private bool anchorCaptured;
        public override Transform Target => harvestPoint ? harvestPoint : transform;

        protected override void OnEnable()
        {
            if (treeRenderer && !anchorCaptured)
            {
                authoredPosition = treeRenderer.transform.localPosition;
                anchorCaptured = true;
            }
            base.OnEnable();
            ResetCompletionNotification();
        }

        protected override void SetGrowthSprite(Sprite sprite)
        {
            if (treeRenderer && sprite && fruitingSprite)
            {
                // Preserve the bottom ground anchor despite the authored species pivot offsets.
                var oldBottom = -fruitingSprite.pivot.y / fruitingSprite.pixelsPerUnit;
                var newBottom = -sprite.pivot.y / sprite.pixelsPerUnit;
                treeRenderer.transform.localPosition = authoredPosition + Vector3.up * (oldBottom - newBottom);
                treeRenderer.sprite = sprite;
            }
        }

        protected override void ShowCompletedVisuals()
        {
            base.ShowCompletedVisuals();
            if (treeRenderer)
            {
                treeRenderer.enabled = true;
                SetGrowthSprite(harvestedSprite);
            }
        }
    }
}
