using Mozandifiers.Content.Prefixes.Common;
using Terraria;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Weapons;

public sealed class BreachingPrefix : WeaponPrefix
{
	protected override float PrefixRollChance => 0.85f;
	protected override float PrefixValueMultiplier => 1.4f;
	protected override int ArmorPenetrationBonus => 10;

	public override PrefixCategory Category => PrefixCategory.AnyWeapon;

	public override bool CanRoll(Item item)
	{
		return IsHeavyWeapon(item);
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
		damageMult *= 1.14f;
		knockbackMult *= 1.12f;
		useTimeMult *= 1.18f;
		critBonus -= 6;
	}
}
