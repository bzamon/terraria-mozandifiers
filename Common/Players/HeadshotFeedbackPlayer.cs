using Terraria;
using Terraria.ModLoader;

namespace Mozandifiers.Common.Players;

public sealed class HeadshotFeedbackPlayer : ModPlayer
{
	private ulong lastNormalVisualTick;
	private ulong lastLongRangeVisualTick;
	private ulong lastLongRangeSoundTick;

	internal bool CanEmitNormalVisual()
	{
		return TryConsumeWindow(ref lastNormalVisualTick, 3);
	}

	internal bool CanEmitLongRangeVisual()
	{
		return TryConsumeWindow(ref lastLongRangeVisualTick, 6);
	}

	internal bool CanPlayLongRangeSound()
	{
		return TryConsumeWindow(ref lastLongRangeSoundTick, 12);
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
