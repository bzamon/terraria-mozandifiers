using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Mozandifiers.Common.Config;
using Mozandifiers.Content.Prefixes.Common;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Weapons;

public sealed class BreachingPrefix : WeaponPrefix
{
	public static float DamageMultiplier => PrefixTuningConfig.Instance.BreachingDamageMultiplier;
	public static float KnockbackMultiplier => PrefixTuningConfig.Instance.BreachingKnockbackMultiplier;
	public static float UseTimeMultiplierValue => PrefixTuningConfig.Instance.BreachingUseTimeMultiplierValue;
	public static int CritBonus => PrefixTuningConfig.Instance.BreachingCritBonus;
	public static int ArmorPenetrationValue => PrefixTuningConfig.Instance.BreachingArmorPenetrationValue;
	public static int BreachDefenseThreshold => PrefixTuningConfig.Instance.BreachingDefenseThreshold;
	public static int BreachDamageThreshold => PrefixTuningConfig.Instance.BreachingDamageThreshold;

	protected override float PrefixRollChance => 0.85f;
	protected override float PrefixValueMultiplier => 1.4f;
	protected override int ArmorPenetrationBonus => ArmorPenetrationValue;

	public override PrefixCategory Category => PrefixCategory.AnyWeapon;

	public override bool CanRoll(Item item)
	{
		return IsBreachingWeapon(item);
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
		damageMult *= DamageMultiplier;
		knockbackMult *= KnockbackMultiplier;
		useTimeMult *= UseTimeMultiplierValue;
		critBonus += CritBonus;
	}

	protected override IEnumerable<TooltipLine> GetExtraTooltipLines(Item item)
	{
		yield return new TooltipLine(
			Mod,
			$"{Name}Breach",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.BreachTooltip",
				BreachDefenseThreshold));

		if (SupportsMergedScale(item)) {
			yield return new TooltipLine(
				Mod,
				$"{Name}Size",
				Language.GetTextValue(
					$"Mods.{Mod.Name}.Prefixes.{Name}.SizeTooltip",
					(int)((GetMergedScaleMultiplier(PrefixTuningConfig.Instance.BreachingScaleMultiplier) - 1f) * 100f)));
		}
	}

	internal static bool IsBreachingWeapon(Item item)
	{
		if (item == null
			|| item.IsAir
			|| item.channel
			|| !IsStandardWeapon(item)
			|| item.CountsAsClass(DamageClass.Magic)
			|| item.CountsAsClass(DamageClass.Summon)
			|| item.CountsAsClass(DamageClass.SummonMeleeSpeed)
			|| item.useAnimation < 25) {
			return false;
		}

		if (IsSwingingMeleeWeapon(item)) {
			return true;
		}

		if (IsProjectileMeleeWeapon(item)) {
			return item.knockBack >= 5f;
		}

		return IsRangedWeapon(item)
			&& IsProjectileWeapon(item)
			&& (item.knockBack >= 3f || item.useAmmo == AmmoID.Rocket);
	}

	internal static bool SupportsMergedScale(Item item)
	{
		return item != null
			&& !item.IsAir
			&& IsSwingingMeleeWeapon(item)
			&& item.useAnimation >= 25;
	}

	internal static float GetMergedScaleMultiplier(float breachingScaleMultiplier)
	{
		return 1f + ((breachingScaleMultiplier - 1f) * 0.75f);
	}

	internal static float GetBreachImpactMultiplier(Item item)
	{
		if (item == null || item.IsAir) {
			return 1f;
		}

		float useAnimationWeight = MathHelper.Clamp((item.useAnimation - 25f) / 25f, 0f, 1f) * 0.45f;
		float knockbackWeight = MathHelper.Clamp(item.knockBack / 10f, 0f, 1f) * 0.25f;
		float launcherWeight = item.useAmmo == AmmoID.Rocket ? 0.25f : 0f;
		float heavySizeWeight = SupportsMergedScale(item) ? 0.15f : 0f;
		return MathHelper.Clamp(1f + useAnimationWeight + knockbackWeight + launcherWeight + heavySizeWeight, 1f, 1.85f);
	}
}
