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
	public const float DamageMultiplier = 1.2f;
	public const float KnockbackMultiplier = 1.18f;
	public const float UseTimeMultiplierValue = 1.24f;
	public const int CritBonus = -4;
	public const int ArmorPenetrationValue = 12;
	public const int BreachDefenseThreshold = 12;
	public const int BreachDamageThreshold = 40;

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
					(int)((GetMergedScaleMultiplier(PrefixTuningConfig.Instance.ColossalScaleMultiplier) - 1f) * 100f)));
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

	internal static float GetMergedScaleMultiplier(float colossalScaleMultiplier)
	{
		return 1f + ((colossalScaleMultiplier - 1f) * 0.75f);
	}

	internal static float GetBreachImpactMultiplier(Item item)
	{
		if (item == null || item.IsAir) {
			return 1f;
		}

		float useAnimationWeight = MathHelper.Clamp((item.useAnimation - 25f) / 25f, 0f, 1f) * 0.45f;
		float knockbackWeight = MathHelper.Clamp(item.knockBack / 10f, 0f, 1f) * 0.25f;
		float launcherWeight = item.useAmmo == AmmoID.Rocket ? 0.25f : 0f;
		float colossalWeight = SupportsMergedScale(item) ? 0.15f : 0f;
		return MathHelper.Clamp(1f + useAnimationWeight + knockbackWeight + launcherWeight + colossalWeight, 1f, 1.85f);
	}
}
