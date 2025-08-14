using UnityEngine;

namespace TimelessEchoes.Upgrades
{
	/// <summary>
	/// Global feature toggle to soft-disable the stat upgrade system at runtime
	/// without removing any underlying functionality or data.
	/// </summary>
	public static class UpgradeFeatureToggle
	{
		/// <summary>
		/// When true, stat upgrades (levels and costs) are ignored and upgrade UI is disabled.
		/// Base values defined on <see cref="StatUpgrade"/> assets and bonuses from skills/gear remain.
		/// </summary>
		public static bool DisableStatUpgrades = true;

		/// <summary>
		/// When true, disables the crafting pity system (minimum rarity clamping based on recent crafts).
		/// Crafting rarity odds will use only core/base weights without any pity floor.
		/// </summary>
		public static bool DisableCraftingPity = true;
	}
}


