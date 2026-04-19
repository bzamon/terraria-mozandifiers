using Terraria;
using Terraria.ModLoader;

namespace Mozandifiers.Common.Players;

public sealed class SanguineHealPlayer : ModPlayer
{
	private const int HealCapPerSecond = 5;
	private const ulong HealWindowTicks = 60;

	private ulong windowStartTick;
	private int healedThisWindow;

	public int ConsumeAvailableHeal(int requestedHeal)
	{
		return ConsumeAvailableHeal(requestedHeal, HealCapPerSecond);
	}

	public int ConsumeAvailableHeal(int requestedHeal, int healCapPerSecond)
	{
		if (requestedHeal <= 0 || healCapPerSecond <= 0) {
			return 0;
		}

		ulong currentTick = Main.GameUpdateCount;
		if (currentTick - windowStartTick >= HealWindowTicks) {
			windowStartTick = currentTick;
			healedThisWindow = 0;
		}

		int remainingHeal = healCapPerSecond - healedThisWindow;
		if (remainingHeal <= 0) {
			return 0;
		}

		int appliedHeal = System.Math.Min(requestedHeal, remainingHeal);
		healedThisWindow += appliedHeal;
		return appliedHeal;
	}
}
