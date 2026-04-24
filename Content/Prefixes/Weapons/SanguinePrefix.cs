using System.Collections.Generic;
using Mozandifiers.Content.Prefixes.Common;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Weapons;

internal enum VampiricPreyState : byte
{
	None,
	Weakened,
	Bloodied,
	Critical
}

public sealed class SanguinePrefix : WeaponPrefix
{
	public const float LifeStealMultiplier = 0.08f;
	public const float ManaRestoreFromManaCost = 0.2f;
	public const int MaxManaRestorePerHit = 2;
	public const int BaseHealCapPerSecond = 5;
	public const int BaseManaRestorePerSecond = 6;
	public const int SustainWindowTicks = 60;
	public const float CriticalFeedMultiplier = 1.35f;
	public const float WeakenedPreyThreshold = 0.5f;
	public const float BloodiedPreyThreshold = 0.25f;
	public const float CriticalPreyThreshold = 0.125f;
	public const float WeakenedFeedMultiplier = 1.15f;
	public const float BloodiedFeedMultiplier = 1.3f;
	public const float CriticalFeedStateMultiplier = 1.5f;
	public const int FrenzyDurationTicks = 180;
	public const int FrenzyRetriggerCooldownTicks = 900;
	public const float FrenzyAttackSpeedBonus = 0.12f;
	public const float FrenzyCapMultiplier = 1.5f;
	public const int FrenzyCritBonusAgainstWeakened = 4;
	public const int FrenzyCritBonusAgainstBloodied = 7;
	public const int FrenzyCritBonusAgainstCritical = 10;

	protected override float PrefixRollChance => 0.8f;
	protected override float PrefixValueMultiplier => 1.45f;

	public override PrefixCategory Category => PrefixCategory.AnyWeapon;

	public override bool CanRoll(Item item)
	{
		return IsStandardWeapon(item)
			&& (item.CountsAsClass(DamageClass.Melee)
				|| item.CountsAsClass(DamageClass.Ranged)
				|| item.CountsAsClass(DamageClass.Magic));
	}

	protected override IEnumerable<TooltipLine> GetExtraTooltipLines(Item item)
	{
		yield return new TooltipLine(
			Mod,
			$"{Name}VampiricLifeSteal",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.LifeStealTooltip",
				(int)(LifeStealMultiplier * 100f)));

		yield return new TooltipLine(
			Mod,
			$"{Name}VampiricMana",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.ManaSiphonTooltip",
				MaxManaRestorePerHit));

		yield return new TooltipLine(
			Mod,
			$"{Name}VampiricCriticalFeed",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.CriticalFeedTooltip",
				(int)((CriticalFeedMultiplier - 1f) * 100f)));

		yield return new TooltipLine(
			Mod,
			$"{Name}VampiricThresholds",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.ThresholdTooltip",
				(int)(WeakenedPreyThreshold * 100f),
				(int)(BloodiedPreyThreshold * 100f),
				(int)(CriticalPreyThreshold * 100f)));

		yield return new TooltipLine(
			Mod,
			$"{Name}VampiricFrenzy",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.FrenzyTooltip",
				FrenzyDurationTicks / 60f,
				(int)(FrenzyAttackSpeedBonus * 100f),
				FrenzyCritBonusAgainstWeakened,
				FrenzyCritBonusAgainstBloodied,
				FrenzyCritBonusAgainstCritical));
	}

	internal static VampiricPreyState GetPreyState(NPC target)
	{
		if (target == null || !target.active || target.lifeMax <= 0) {
			return VampiricPreyState.None;
		}

		return GetPreyState(target.life / (float)target.lifeMax);
	}

	internal static VampiricPreyState GetPreyState(float currentLifeRatio)
	{
		if (currentLifeRatio <= CriticalPreyThreshold) {
			return VampiricPreyState.Critical;
		}

		if (currentLifeRatio <= BloodiedPreyThreshold) {
			return VampiricPreyState.Bloodied;
		}

		if (currentLifeRatio <= WeakenedPreyThreshold) {
			return VampiricPreyState.Weakened;
		}

		return VampiricPreyState.None;
	}

	internal static float GetFeedMultiplier(VampiricPreyState preyState)
	{
		return preyState switch {
			VampiricPreyState.Weakened => WeakenedFeedMultiplier,
			VampiricPreyState.Bloodied => BloodiedFeedMultiplier,
			VampiricPreyState.Critical => CriticalFeedStateMultiplier,
			_ => 1f
		};
	}

	internal static int GetFrenzyCritBonus(VampiricPreyState preyState)
	{
		return preyState switch {
			VampiricPreyState.Weakened => FrenzyCritBonusAgainstWeakened,
			VampiricPreyState.Bloodied => FrenzyCritBonusAgainstBloodied,
			VampiricPreyState.Critical => FrenzyCritBonusAgainstCritical,
			_ => 0
		};
	}

	internal static float GetFrenzyTierScale(VampiricPreyState preyState)
	{
		return preyState switch {
			VampiricPreyState.Weakened => 0.6f,
			VampiricPreyState.Bloodied => 0.8f,
			VampiricPreyState.Critical => 1f,
			_ => 0f
		};
	}
}
