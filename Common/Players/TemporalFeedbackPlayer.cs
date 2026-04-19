using Terraria;
using Terraria.ModLoader;

namespace Mozandifiers.Common.Players;

public sealed class TemporalFeedbackPlayer : ModPlayer
{
	private ulong lastUsePulseTick;

	internal bool CanEmitUsePulse()
	{
		return TryConsumeWindow(ref lastUsePulseTick, 6);
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
