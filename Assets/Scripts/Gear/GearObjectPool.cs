using System.Collections.Generic;

namespace TimelessEchoes.Gear
{
    /// <summary>
    /// Simple object pool for GearItem and GearAffix to reduce allocations during autocrafting.
    /// </summary>
    public static class GearObjectPool
    {
        private static readonly Stack<GearItem> _itemPool = new(32);
        private static readonly Stack<GearAffix> _affixPool = new(128);

        /// <summary>
        /// Gets a GearItem from the pool or creates a new one.
        /// Remember to call Release when done with the item.
        /// </summary>
        public static GearItem GetItem()
        {
            if (_itemPool.Count > 0)
            {
                var item = _itemPool.Pop();
                item.Reset();
                return item;
            }
            return new GearItem();
        }

        /// <summary>
        /// Gets a GearAffix from the pool or creates a new one.
        /// </summary>
        public static GearAffix GetAffix()
        {
            if (_affixPool.Count > 0)
            {
                var affix = _affixPool.Pop();
                affix.stat = null;
                affix.value = 0f;
                return affix;
            }
            return new GearAffix();
        }

        /// <summary>
        /// Returns a GearItem to the pool.
        /// </summary>
        public static void ReleaseItem(GearItem item)
        {
            if (item == null) return;

            // Release all affixes first
            if (item.affixes != null)
            {
                foreach (var affix in item.affixes)
                    ReleaseAffix(affix);
                item.affixes.Clear();
            }

            item.Reset();
            _itemPool.Push(item);
        }

        /// <summary>
        /// Returns a GearAffix to the pool.
        /// </summary>
        public static void ReleaseAffix(GearAffix affix)
        {
            if (affix == null) return;
            affix.stat = null;
            affix.value = 0f;
            _affixPool.Push(affix);
        }

        /// <summary>
        /// Clears all pooled objects.
        /// </summary>
        public static void Clear()
        {
            _itemPool.Clear();
            _affixPool.Clear();
        }

        /// <summary>
        /// Pre-warms the pool with a number of items and affixes.
        /// </summary>
        public static void Prewarm(int items = 16, int affixes = 64)
        {
            for (int i = 0; i < items; i++)
                _itemPool.Push(new GearItem());

            for (int i = 0; i < affixes; i++)
                _affixPool.Push(new GearAffix());
        }

        /// <summary>
        /// Gets the current pool sizes (for debugging/monitoring).
        /// </summary>
        public static (int items, int affixes) GetPoolSize() => (_itemPool.Count, _affixPool.Count);
    }
}
