using System.Collections.Generic;
using Mozandifiers.Content.Prefixes.Common;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Weapons;

internal enum DesperateThresholdState : byte
{
	None,
	Minor,
	Severe,
	Critical
}

public sealed class DesperatePrefix : WeaponPrefix
{
	public const float MaxDamageBonus = 0.8f;
	public const int CritBonus = 5;
	public const float MinorLifeThreshold = 0.5f;
	public const float SevereLifeThreshold = 0.25f;
	public const float CriticalLifeThreshold = 0.125f;
	public const int SurgeCooldownTicks = 180;
	public const float SurgeDamageBonus = 0.2f;

	protected override float PrefixRollChance => 0.75f;
	protected override float PrefixValueMultiplier => 1.4f;

	public override PrefixCategory Category => PrefixCategory.AnyWeapon;

	public override bool CanRoll(Item item)
	{
		return IsStandardCombatWeapon(item);
	}

	public override void SetStats(
		ref float damageMult,
		ref float knockbackMult,
		ref float useTimeMult,
		ref float scaleMult,
		ref float shootSpeedMult,
		ref float manaMult,
		ref int critBonus)
	{
		damageMult *= 0.90f;
		critBonus += CritBonus;
	}

	protected override IEnumerable<TooltipLine> GetExtraTooltipLines(Item item)
	{
		yield return new TooltipLine(
			Mod,
			$"{Name}Desperate",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.DesperateTooltip",
				(int)(MaxDamageBonus * 100f)));

		yield return new TooltipLine(
			Mod,
			$"{Name}Thresholds",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.ThresholdTooltip",
				(int)(MinorLifeThreshold * 100f),
				(int)(SevereLifeThreshold * 100f),
				(int)(CriticalLifeThreshold * 100f)));

		yield return new TooltipLine(
			Mod,
			$"{Name}Surge",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.SurgeTooltip",
				SurgeCooldownTicks / 60f,
				(int)(SurgeDamageBonus * 100f)));
	}

	internal static DesperateThresholdState GetThresholdState(Player player)
	{
		if (player == null || !player.active || player.dead || player.statLifeMax2 <= 0) {
			return DesperateThresholdState.None;
		}

		float currentLifeRatio = player.statLife / (float)player.statLifeMax2;
		if (currentLifeRatio <= CriticalLifeThreshold) {
			return DesperateThresholdState.Critical;
		}

		if (currentLifeRatio <= SevereLifeThreshold) {
			return DesperateThresholdState.Severe;
		}

		if (currentLifeRatio <= MinorLifeThreshold) {
			return DesperateThresholdState.Minor;
		}

		return DesperateThresholdState.None;
	}
}
