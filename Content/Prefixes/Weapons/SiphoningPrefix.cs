using System.Collections.Generic;
using Mozandifiers.Content.Prefixes.Common;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Weapons;

public sealed class SiphoningPrefix : WeaponPrefix
{
	public const float ManaRestoreFromManaCost = 0.25f;
	public const int MaxManaRestorePerHit = 3;
	public const int MaxManaRestorePerSecond = 6;
	public const int ManaRestoreWindowTicks = 60;

	protected override float PrefixRollChance => 0.75f;
	protected override float PrefixValueMultiplier => 1.45f;

	public override PrefixCategory Category => PrefixCategory.AnyWeapon;

	public override bool CanRoll(Item item)
	{
		return IsStandardMagicProjectileWeapon(item);
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
		damageMult *= 0.9f;
		critBonus += 2;
	}

	protected override IEnumerable<TooltipLine> GetExtraTooltipLines(Item item)
	{
		yield return new TooltipLine(
			Mod,
			$"{Name}Siphoning",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.SiphoningTooltip",
				MaxManaRestorePerHit,
				MaxManaRestorePerSecond));
	}
}
