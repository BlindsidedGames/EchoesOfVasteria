using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using TimelessEchoes.Gear;
using Blindsided.Utilities;

namespace TimelessEchoes.Upgrades
{
	/// <summary>
	/// Centralized lookup for stat icons stored in the TMP Sprite Asset at Fonts/StatIcons.
	/// Provides helpers to get either a TMP <sprite> tag or a Unity Sprite for UI Images.
	/// </summary>
	public static class StatIconLookup
	{
		private const string SpriteAssetPath = "Fonts/StatIcons";
		private static TMP_SpriteAsset spriteAsset;

		public enum StatKey
		{
			Damage,
			CritChance,
			AttackRate,
			Defense,
			Health,
			Regen,
			MoveSpeed,
			UpArrow,
			DownArrow,
			RightArrow,
			Minus,
			Plus
		}

		// Indices as provided by the user for the StatIcons atlas
		private static readonly Dictionary<StatKey, int> statToIndex = new()
		{
			{ StatKey.Damage, 0 },
			{ StatKey.CritChance, 1 },
			{ StatKey.AttackRate, 2 },
			{ StatKey.Defense, 3 },
			{ StatKey.Health, 4 },
			{ StatKey.Regen, 5 },
			{ StatKey.MoveSpeed, 6 },
			{ StatKey.UpArrow, 7 },
			{ StatKey.DownArrow, 8 },
			{ StatKey.RightArrow, 9 },
			{ StatKey.Minus, 10 },
			{ StatKey.Plus, 11 }
		};

		private static TMP_SpriteAsset SpriteAsset
		{
			get
			{
				if (spriteAsset == null)
				{
					// Only load from Resources path. Do NOT fallback, to avoid swapping to other atlases.
					spriteAsset = AssetCache.GetOne<TMP_SpriteAsset>(SpriteAssetPath);
				}
				return spriteAsset;
			}
		}

		/// <summary>
		/// Returns the sprite asset used by stat icon tags so TMP texts can render them.
		/// </summary>
		public static TMP_SpriteAsset GetSpriteAsset()
		{
			return SpriteAsset;
		}

		public static bool TryGetIconIndex(StatKey key, out int index)
		{
			return statToIndex.TryGetValue(key, out index);
		}

		public static bool TryGetIconIndex(HeroStatMapping mapping, out int index)
		{
			return TryGetIconIndex(Map(mapping), out index);
		}

		public static bool TryGetIconIndex(string statName, out int index)
		{
			if (TryResolveKeyFromName(statName, out var key))
				return TryGetIconIndex(key, out index);
			index = 0;
			return false;
		}

		public static string GetIconTag(StatKey key)
		{
			return statToIndex.TryGetValue(key, out var idx) ? $"<sprite={idx}>" : string.Empty;
		}

		public static string GetIconTag(HeroStatMapping mapping)
		{
			return GetIconTag(Map(mapping));
		}

		public static string GetIconTag(string statName)
		{
			return TryGetIconIndex(statName, out var idx) ? $"<sprite={idx}>" : string.Empty;
		}

		public static bool TryGetIcon(StatKey key, out Sprite sprite)
		{
			sprite = null;
			if (!statToIndex.TryGetValue(key, out var idx)) return false;
			var asset = SpriteAsset;
			var table = asset != null ? asset.spriteCharacterTable : null;
			if (table == null || idx < 0 || idx >= table.Count) return false;
			var character = table[idx];
			var glyph = character != null ? character.glyph as TMP_SpriteGlyph : null;
			sprite = glyph != null ? glyph.sprite : null;
			return sprite != null;
		}

		public static bool TryGetIcon(HeroStatMapping mapping, out Sprite sprite)
		{
			return TryGetIcon(Map(mapping), out sprite);
		}

		public static bool TryGetIcon(string statName, out Sprite sprite)
		{
			if (TryResolveKeyFromName(statName, out var key))
				return TryGetIcon(key, out sprite);
			sprite = null;
			return false;
		}

		private static StatKey Map(HeroStatMapping mapping)
		{
			return mapping switch
			{
				HeroStatMapping.Damage => StatKey.Damage,
				HeroStatMapping.AttackRate => StatKey.AttackRate,
				HeroStatMapping.Defense => StatKey.Defense,
				HeroStatMapping.MaxHealth => StatKey.Health,
				HeroStatMapping.HealthRegen => StatKey.Regen,
				HeroStatMapping.MoveSpeed => StatKey.MoveSpeed,
				HeroStatMapping.CritChance => StatKey.CritChance,
				_ => StatKey.Damage
			};
		}

		private static bool TryResolveKeyFromName(string name, out StatKey key)
		{
			key = StatKey.Damage;
			if (string.IsNullOrWhiteSpace(name)) return false;
			var n = name.Trim().ToLowerInvariant();
			switch (n)
			{
				case "damage":
					key = StatKey.Damage; return true;
				case "crit chance":
				case "crit":
				case "critical chance":
					key = StatKey.CritChance; return true;
				case "attack speed":
				case "attack rate":
					key = StatKey.AttackRate; return true;
				case "defense":
				case "defence":
				case "armor":
				case "armour":
					key = StatKey.Defense; return true;
				case "health":
				case "max health":
				case "hitpoints":
				case "hp":
					key = StatKey.Health; return true;
				case "regen":
				case "regeneration":
					key = StatKey.Regen; return true;
				case "movement speed":
				case "move speed":
				case "movement":
					key = StatKey.MoveSpeed; return true;
				default:
					return false;
			}
		}
	}
}


