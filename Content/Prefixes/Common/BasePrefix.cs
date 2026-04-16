using Terraria;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Common;

public abstract class BasePrefix : ModPrefix
{
	protected virtual float PrefixRollChance => 1f;
	protected virtual float PrefixValueMultiplier => 1f;

	public override float RollChance(Item item)
	{
		return PrefixRollChance;
	}

	public override void ModifyValue(ref float valueMult)
	{
		valueMult *= PrefixValueMultiplier;
	}

	protected static bool IsStandardWeapon(Item item)
	{
		return item.damage > 0 && !item.accessory;
	}
}
