using Mozandifiers.Content.Prefixes.Common;
using Terraria;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Weapons;

public sealed class SkirmishingPrefix : WeaponPrefix
{
	protected override float PrefixRollChance => 0.95f;
	protected override float PrefixValueMultiplier => 1.3f;

	public override PrefixCategory Category => PrefixCategory.Ranged;

	public override bool CanRoll(Item item)
	{
		return IsRangedWeapon(item) && IsProjectileWeapon(item);
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
		damageMult *= 0.86f;
		knockbackMult *= 0.85f;
		useTimeMult *= 0.82f;
		shootSpeedMult *= 1.18f;
		critBonus -= 4;
	}
}
