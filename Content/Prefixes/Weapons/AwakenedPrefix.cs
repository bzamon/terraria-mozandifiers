using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Mozandifiers.Common.Config;
using Mozandifiers.Content.Prefixes.Common;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Weapons;

internal enum AwakenedState : byte
{
	Dormant,
	Awakened,
	RecoveryLockout
}

public sealed class AwakenedPrefix : WeaponPrefix
{
	public static float DormantUseSpeedBonus => PrefixTuningConfig.Instance.AwakenedDormantUseSpeedBonus;
	public static int DormantCritBonus => PrefixTuningConfig.Instance.AwakenedDormantCritBonus;
	public static float AwakenedDamageBonus => PrefixTuningConfig.Instance.AwakenedDamageBonus;
	public static float AwakenedScaleBonus => PrefixTuningConfig.Instance.AwakenedScaleBonus;
	public static float AwakenedUseSpeedBonus => PrefixTuningConfig.Instance.AwakenedUseSpeedBonus;
	public static float AwakenedSweetSpotKnockbackMultiplier => PrefixTuningConfig.Instance.AwakenedSweetSpotKnockbackMultiplier;
	public static int DesiredAwakenBuildTicks => PrefixTuningConfig.Instance.AwakenedDesiredBuildTicks;
	public static int AwakenedDurationTicks => PrefixTuningConfig.Instance.AwakenedDurationTicks;
	public static int RecoveryLockoutTicks => PrefixTuningConfig.Instance.AwakenedRecoveryLockoutTicks;
	public const float SweetSpotStartNormalized = 0.72f;
	public const float SweetSpotThicknessMultiplier = 0.45f;
	public const float MinSweetSpotThicknessPixels = 16f;
	public const float BladeLengthPaddingPixels = 12f;
	public static float HpBonusDamageRatio => PrefixTuningConfig.Instance.AwakenedHpBonusDamageRatio;
	public static int MinAwakenedBonusDamage => PrefixTuningConfig.Instance.AwakenedMinBonusDamage;
	public static int MaxAwakenedBonusDamageAgainstNormalEnemies => PrefixTuningConfig.Instance.AwakenedMaxBonusDamageAgainstNormalEnemies;
	public static float BossBonusDamageCapRatioPerSecond => PrefixTuningConfig.Instance.AwakenedBossBonusDamageCapRatioPerSecond;
	public static int MinBossBonusDamageCapPerSecond => PrefixTuningConfig.Instance.AwakenedMinBossBonusDamageCapPerSecond;
	public static int MaxBossBonusDamageCapPerSecond => PrefixTuningConfig.Instance.AwakenedMaxBossBonusDamageCapPerSecond;
	public static int BossBonusDamageWindowTicks => PrefixTuningConfig.Instance.AwakenedBossBonusDamageWindowTicks;

	protected override float PrefixRollChance => 0.7f;
	protected override float PrefixValueMultiplier => 1.5f;

	public override PrefixCategory Category => PrefixCategory.AnyWeapon;

	public override bool CanRoll(Item item)
	{
		return IsStandardSwordWeapon(item);
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
		useTimeMult *= 1f - DormantUseSpeedBonus;
		critBonus += DormantCritBonus;
	}

	protected override IEnumerable<TooltipLine> GetExtraTooltipLines(Item item)
	{
		yield return new TooltipLine(
			Mod,
			$"{Name}Dormant",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.DormantTooltip",
				(int)(DormantUseSpeedBonus * 100f),
				DormantCritBonus));

		yield return new TooltipLine(
			Mod,
			$"{Name}Build",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.BuildTooltip",
				DesiredAwakenBuildTicks / 60f));

		yield return new TooltipLine(
			Mod,
			$"{Name}Awakened",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.AwakenedTooltip",
				AwakenedDurationTicks / 60f,
				(int)(AwakenedDamageBonus * 100f),
				(int)(AwakenedScaleBonus * 100f),
				(int)(AwakenedUseSpeedBonus * 100f)));

		yield return new TooltipLine(
			Mod,
			$"{Name}Strike",
			Language.GetTextValue($"Mods.{Mod.Name}.Prefixes.{Name}.StrikeTooltip"));

		yield return new TooltipLine(
			Mod,
			$"{Name}Lockout",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.LockoutTooltip",
				RecoveryLockoutTicks / 60f));
	}

	internal static float GetMeterGain(Item item)
	{
		if (item == null || item.IsAir) {
			return 0f;
		}

		float effectiveSwingTicks = System.MathF.Max(item.useAnimation, item.useTime);
		return MathHelper.Clamp(effectiveSwingTicks / DesiredAwakenBuildTicks, 0.04f, 0.2f);
	}

	internal static int GetAwakenedBonusDamage(NPC target)
	{
		if (target == null || !target.active || target.lifeMax <= 0) {
			return 0;
		}

		int requestedBonusDamage = (int)System.MathF.Round(target.lifeMax * HpBonusDamageRatio);
		return System.Math.Clamp(requestedBonusDamage, MinAwakenedBonusDamage, MaxAwakenedBonusDamageAgainstNormalEnemies);
	}

	internal static int GetBossBonusDamageCapPerSecond(NPC target)
	{
		if (target == null || !target.active || target.lifeMax <= 0) {
			return 0;
		}

		int cap = (int)System.MathF.Round(target.lifeMax * BossBonusDamageCapRatioPerSecond);
		return System.Math.Clamp(cap, MinBossBonusDamageCapPerSecond, MaxBossBonusDamageCapPerSecond);
	}
}
