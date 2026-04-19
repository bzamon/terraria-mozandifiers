using Mozandifiers.Common.Config;
using Mozandifiers.Content.Prefixes.Common;
using Terraria;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Weapons;

public sealed class ColossalPrefix : WeaponPrefix
{
	protected override float PrefixRollChance => 1.1f;
	protected override float PrefixValueMultiplier => 1.45f;

	public override PrefixCategory Category => PrefixCategory.Melee;

	public override bool CanRoll(Item item)
	{
		return false;
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
		damageMult *= 1.25f;
		knockbackMult *= 1.2f;
		useTimeMult *= 1.25f;
		scaleMult *= PrefixTuningConfig.Instance.ColossalScaleMultiplier;
		critBonus += 5;
	}
}
