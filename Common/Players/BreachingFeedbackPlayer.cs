using Terraria;
using Terraria.ModLoader;

namespace Mozandifiers.Common.Players;

public sealed class BreachingFeedbackPlayer : ModPlayer
{
	private ulong lastBreachVisualTick;

	internal bool CanEmitBreachVisual()
	{
		return TryConsumeWindow(ref lastBreachVisualTick, 8);
	}

	private static bool TryConsumeWindow(ref ulong lastTick, int cooldownTicks)
	{
		ulong currentTick = Main.GameUpdateCount;
		if (currentTick - lastTick < (ulong)cooldownTicks) {
			return false;
		}

		lastTick = currentTick;
		return true;
	}
}
