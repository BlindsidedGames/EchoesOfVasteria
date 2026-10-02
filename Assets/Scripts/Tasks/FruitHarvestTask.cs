using TimelessEchoes.Hero;
using UnityEngine;

namespace TimelessEchoes.Tasks
{
    /// <summary>One adventure harvest per spawned tree. Picking preserves its trunk and timber tasks.</summary>
    public sealed class FruitHarvestTask : ContinuousTask
    {
        [SerializeField] private SpriteRenderer treeRenderer;
        [SerializeField] private Sprite fruitingSprite;
        [SerializeField] private Sprite harvestedSprite;
        [SerializeField] private Transform harvestPoint;
        private Vector3 authoredPosition;
        private bool anchorCaptured;
        protected override string AnimationName => "Loot";
        protected override string InterruptTriggerName => "StopLooting";
        public override Transform Target => harvestPoint ? harvestPoint : transform;

        protected override void OnEnable()
        {
            base.OnEnable();
            ResetCompletionNotification();
            if (treeRenderer)
            {
                if (!anchorCaptured) { authoredPosition = treeRenderer.transform.localPosition; anchorCaptured = true; }
                treeRenderer.transform.localPosition = authoredPosition;
                treeRenderer.enabled = true; treeRenderer.sprite = fruitingSprite;
            }
            IsExhausted = false;
        }
        public override void StartTask() { base.StartTask(); IsExhausted = false; }
        protected override void OnTaskCompleted(HeroBase hero)
        {
            if (treeRenderer && harvestedSprite && fruitingSprite)
            {
                // Preserve the bottom ground anchor despite the authored species pivot offsets.
                var oldBottom = -fruitingSprite.pivot.y / fruitingSprite.pixelsPerUnit;
                var newBottom = -harvestedSprite.pivot.y / harvestedSprite.pixelsPerUnit;
                treeRenderer.transform.localPosition = authoredPosition + Vector3.up * (oldBottom - newBottom);
                treeRenderer.sprite = harvestedSprite;
            }
            IsExhausted = true;
        }
    }
}
