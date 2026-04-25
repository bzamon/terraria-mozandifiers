using System.Collections.Generic;
using Mozandifiers.Common.Config;
using Mozandifiers.Content.Prefixes.Common;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Weapons;

public sealed class TemporalPrefix : WeaponPrefix
{
	public static float AttackSpeedMultiplier => PrefixTuningConfig.Instance.TemporalAttackSpeedMultiplier;
	public static float ProjectileSpeedMultiplier => PrefixTuningConfig.Instance.TemporalProjectileSpeedMultiplier;
	public static float PressureMax => PrefixTuningConfig.Instance.TemporalPressureMax;
	public static float BasePressurePerHit => PrefixTuningConfig.Instance.TemporalBasePressurePerHit;
	public static float DamagePressureFactor => PrefixTuningConfig.Instance.TemporalDamagePressureFactor;
	public static float TempoBonusPressure => PrefixTuningConfig.Instance.TemporalTempoBonusPressure;
	public static int TempoBonusWindowTicks => PrefixTuningConfig.Instance.TemporalTempoBonusWindowTicks;
	public static float BossPressureMultiplier => PrefixTuningConfig.Instance.TemporalBossPressureMultiplier;
	public static int FractureDurationTicks => PrefixTuningConfig.Instance.TemporalFractureDurationTicks;
	public static int FractureCooldownTicks => PrefixTuningConfig.Instance.TemporalFractureCooldownTicks;
	public static float ReleaseMultiplier => PrefixTuningConfig.Instance.TemporalReleaseMultiplier;

	protected override float PrefixRollChance => 0.6f;
	protected override float PrefixValueMultiplier => 1.5f;

	public override PrefixCategory Category => PrefixCategory.AnyWeapon;

	public override bool CanRoll(Item item)
	{
		return IsStandardWeapon(item);
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
		damageMult = (1f / AttackSpeedMultiplier);
		useTimeMult = (1f / AttackSpeedMultiplier);
		shootSpeedMult = ProjectileSpeedMultiplier;
	}

	protected override IEnumerable<TooltipLine> GetExtraTooltipLines(Item item)
	{
		yield return new TooltipLine(
			Mod,
			$"{Name}Temporal",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.TemporalTooltip",
				(int)((AttackSpeedMultiplier - 1f) * 100f),
				(int)((ProjectileSpeedMultiplier - 1f) * 100f)));

		yield return new TooltipLine(
			Mod,
			$"{Name}Fracture",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.FractureTooltip",
				(int)PressureMax,
				FractureDurationTicks / 60f,
				(int)((ReleaseMultiplier - 1f) * 100f),
				FractureCooldownTicks / 60f));
	}
}
