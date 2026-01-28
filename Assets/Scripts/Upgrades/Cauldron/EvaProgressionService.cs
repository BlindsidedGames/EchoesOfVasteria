using System;
using UnityEngine;

namespace TimelessEchoes.Upgrades.Cauldron
{
    /// <summary>
    /// Manages Eva's level progression and XP.
    /// Extracted from CauldronManager to provide standalone leveling logic.
    /// </summary>
    public class EvaProgressionService
    {
        /// <summary>
        /// Fired when Eva gains a level.
        /// </summary>
        public event Action OnLevelUp;

        private int _level = 1;
        private double _xp;

        /// <summary>
        /// Eva's current level. Minimum value is 1.
        /// </summary>
        public int Level
        {
            get => _level;
            private set => _level = Math.Max(1, value);
        }

        /// <summary>
        /// Eva's current XP toward the next level.
        /// </summary>
        public double Xp
        {
            get => _xp;
            private set => _xp = Math.Max(0, value);
        }

        /// <summary>
        /// The total XP required to advance from the current level to the next.
        /// Formula: 50 + 10 * (level - 1)
        /// </summary>
        public double XpToNextLevel => GetXpToNextLevel(Level);

        /// <summary>
        /// Normalized progress (0..1) toward the next level.
        /// </summary>
        public float XpProgress => XpToNextLevel > 0 ? (float)(Xp / XpToNextLevel) : 0f;

        /// <summary>
        /// Gets the XP required to advance from a given level.
        /// </summary>
        /// <param name="level">The level to calculate XP requirement for.</param>
        /// <returns>XP needed to go from this level to the next.</returns>
        public double GetXpToNextLevel(int level)
        {
            // Simple progression: 50 + 10*(level-1)
            return 50 + 10 * Math.Max(0, level - 1);
        }

        /// <summary>
        /// Adds XP to Eva, potentially leveling up multiple times if enough XP is gained.
        /// </summary>
        /// <param name="amount">The amount of XP to add.</param>
        public void GainXp(double amount)
        {
            if (amount <= 0) return;

            Xp += amount;
            while (Xp >= GetXpToNextLevel(Level))
            {
                Xp -= GetXpToNextLevel(Level);
                Level++;
                OnLevelUp?.Invoke();
            }
        }

        /// <summary>
        /// Loads Eva's progression state from save data.
        /// </summary>
        /// <param name="level">The saved level.</param>
        /// <param name="xp">The saved XP.</param>
        public void LoadFromSave(int level, double xp)
        {
            _level = Math.Max(1, level);
            _xp = Math.Max(0, xp);
        }

        /// <summary>
        /// Gets the current state for saving.
        /// </summary>
        /// <param name="level">Output: Current level.</param>
        /// <param name="xp">Output: Current XP.</param>
        public void GetSaveData(out int level, out double xp)
        {
            level = _level;
            xp = _xp;
        }

        /// <summary>
        /// Resets Eva's progression to starting values.
        /// </summary>
        public void Reset()
        {
            _level = 1;
            _xp = 0;
        }

        /// <summary>
        /// Gets the total XP required to reach a target level from level 1.
        /// </summary>
        /// <param name="targetLevel">The target level.</param>
        /// <returns>Total cumulative XP needed.</returns>
        public double GetTotalXpToLevel(int targetLevel)
        {
            if (targetLevel <= 1) return 0;

            double total = 0;
            for (int lvl = 1; lvl < targetLevel; lvl++)
            {
                total += GetXpToNextLevel(lvl);
            }
            return total;
        }
    }
}
