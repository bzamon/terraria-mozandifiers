using Terraria.ModLoader;

namespace Mozandifiers.Common.Players;

public sealed class StormforgedPlayer : ModPlayer
{
	public bool SuppressStormforgedChain { get; set; }

	public override void ResetEffects()
	{
		SuppressStormforgedChain = false;
	}
}
