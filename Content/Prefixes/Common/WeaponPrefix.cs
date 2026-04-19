using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Common;

internal enum EchoingWeaponMode
{
	None,
	RangedProjectile,
	MagicProjectile,
	MeleeSwing,
	MeleeThrust,
	MeleeBoomerang,
	MeleeFlail,
	MeleeChannelled,
	MeleeSpecial
}

public abstract class WeaponPrefix : BasePrefix
{
	protected virtual int ArmorPenetrationBonus => 0;

	public override void Apply(Item item)
	{
		if (ArmorPenetrationBonus != 0) {
			item.ArmorPenetration += ArmorPenetrationBonus;
		}
	}

	public override bool AllStatChangesHaveEffectOn(Item item)
	{
		return ArmorPenetrationBonus == 0 || item.ArmorPenetration + ArmorPenetrationBonus != item.ArmorPenetration;
	}

	public override IEnumerable<TooltipLine> GetTooltipLines(Item item)
	{
		if (ArmorPenetrationBonus > 0) {
			yield return new TooltipLine(
				Mod,
				$"{Name}ArmorPenetration",
				Language.GetTextValue($"Mods.{Mod.Name}.Prefixes.{Name}.ArmorPenetrationTooltip", ArmorPenetrationBonus));
		}

		foreach (TooltipLine line in GetExtraTooltipLines(item)) {
			yield return line;
		}
	}

	protected virtual IEnumerable<TooltipLine> GetExtraTooltipLines(Item item)
	{
		yield break;
	}

	internal static bool IsMagicWeapon(Item item)
	{
		return IsStandardWeapon(item) && item.CountsAsClass(DamageClass.Magic) && item.mana > 0;
	}

	internal static bool IsRangedWeapon(Item item)
	{
		return IsStandardWeapon(item) && item.CountsAsClass(DamageClass.Ranged);
	}

	internal static bool IsProjectileWeapon(Item item)
	{
		return item.shoot > ProjectileID.None;
	}

	internal static Projectile GetProjectileSample(Item item)
	{
		return ContentSamples.ProjectilesByType[item.shoot];
	}

	internal static bool IsProjectileMeleeWeapon(Item item)
	{
		return IsStandardWeapon(item) && item.CountsAsClass(DamageClass.Melee) && IsProjectileWeapon(item);
	}

	internal static bool IsSwingingMeleeWeapon(Item item)
	{
		return IsStandardWeapon(item)
			&& item.CountsAsClass(DamageClass.Melee)
			&& item.useStyle == ItemUseStyleID.Swing
			&& !item.noMelee
			&& !item.noUseGraphic
			&& !IsProjectileWeapon(item);
	}

	internal static bool IsHeavyWeapon(Item item)
	{
		return IsStandardWeapon(item)
			&& !item.CountsAsClass(DamageClass.Summon)
			&& !item.CountsAsClass(DamageClass.SummonMeleeSpeed)
			&& item.useAnimation >= 25;
	}

	internal static bool IsStandardProjectileCombatWeapon(Item item)
	{
		return (IsRangedWeapon(item) || IsMagicWeapon(item))
			&& IsProjectileWeapon(item)
			&& !item.channel;
	}

	internal static bool IsStandardMagicProjectileWeapon(Item item)
	{
		return IsMagicWeapon(item)
			&& IsProjectileWeapon(item)
			&& !item.channel;
	}

	internal static bool IsStandardDirectCombatWeapon(Item item)
	{
		return IsSwingingMeleeWeapon(item) || IsStandardProjectileCombatWeapon(item);
	}

	internal static bool IsStandardCombatWeapon(Item item)
	{
		return IsStandardWeapon(item)
			&& (item.CountsAsClass(DamageClass.Melee)
				|| item.CountsAsClass(DamageClass.Ranged)
				|| item.CountsAsClass(DamageClass.Magic));
	}

	internal static EchoingWeaponMode GetEchoingWeaponMode(Item item)
	{
		if (IsRangedWeapon(item) && IsProjectileWeapon(item)) {
			return EchoingWeaponMode.RangedProjectile;
		}

		if (IsMagicWeapon(item) && IsProjectileWeapon(item)) {
			return EchoingWeaponMode.MagicProjectile;
		}

		if (IsProjectileMeleeWeapon(item)) {
			return GetProjectileMeleeEchoMode(ContentSamples.ProjectilesByType[item.shoot], item.channel);
		}

		return EchoingWeaponMode.None;
	}

	internal static EchoingWeaponMode GetProjectileMeleeEchoMode(Projectile projectile, bool itemChannels = false)
	{
		if (!projectile.CountsAsClass(DamageClass.Melee)) {
			return EchoingWeaponMode.None;
		}

		return projectile.aiStyle switch {
			ProjAIStyleID.Spear or ProjAIStyleID.NorthPoleSpear => EchoingWeaponMode.MeleeThrust,
			ProjAIStyleID.Boomerang => EchoingWeaponMode.MeleeBoomerang,
			ProjAIStyleID.Flail => EchoingWeaponMode.MeleeFlail,
			ProjAIStyleID.Yoyo => EchoingWeaponMode.MeleeChannelled,
			ProjAIStyleID.Drill or ProjAIStyleID.HeldProjectile => EchoingWeaponMode.MeleeSpecial,
			_ => itemChannels ? EchoingWeaponMode.MeleeChannelled : EchoingWeaponMode.MeleeSpecial
		};
	}

	internal static bool IsImplementedEchoingMode(EchoingWeaponMode mode)
	{
		return mode is EchoingWeaponMode.RangedProjectile
			or EchoingWeaponMode.MagicProjectile
			or EchoingWeaponMode.MeleeThrust
			or EchoingWeaponMode.MeleeBoomerang;
	}
}
