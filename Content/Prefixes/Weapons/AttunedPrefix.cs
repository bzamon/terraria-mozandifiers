using Mozandifiers.Content.Prefixes.Common;
using Terraria;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Weapons;

public sealed class AttunedPrefix : WeaponPrefix
{
	protected override float PrefixRollChance => 1.05f;
	protected override float PrefixValueMultiplier => 1.35f;

	public override PrefixCategory Category => PrefixCategory.Magic;

	public override bool CanRoll(Item item)
	{
		return IsMagicWeapon(item);
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
		damageMult *= 0.84f;
		knockbackMult *= 0.9f;
		useTimeMult *= 0.88f;
		shootSpeedMult *= 1.1f;
		manaMult *= 0.75f;
	}
}
