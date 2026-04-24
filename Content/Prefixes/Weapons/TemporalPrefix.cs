using System.Collections.Generic;
using Mozandifiers.Content.Prefixes.Common;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Weapons;

public sealed class TemporalPrefix : WeaponPrefix
{
	public const float AttackSpeedMultiplier = 2f;
	public const float ProjectileSpeedMultiplier = 2f;
	public const float PressureMax = 100f;
	public const float BasePressurePerHit = 4f;
	public const float DamagePressureFactor = 120f;
	public const float TempoBonusPressure = 2f;
	public const int TempoBonusWindowTicks = 45;
	public const float BossPressureMultiplier = 0.65f;
	public const int FractureDurationTicks = 90;
	public const int FractureCooldownTicks = 240;
	public const float ReleaseMultiplier = 1.15f;

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
		damageMult *= (1f / AttackSpeedMultiplier);
		useTimeMult *= (1f / AttackSpeedMultiplier);
		shootSpeedMult *= ProjectileSpeedMultiplier;
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
