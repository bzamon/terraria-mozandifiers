using Mozandifiers.Content.Prefixes.Weapons;
using Terraria;
using Terraria.ModLoader;

namespace Mozandifiers.Common.Players;

public sealed class SiphoningPlayer : ModPlayer
{
	private ulong windowStartTick;
	private int manaRestoredThisWindow;

	public int ConsumeAvailableManaRestore(int requestedManaRestore)
	{
		return ConsumeAvailableManaRestore(requestedManaRestore, SiphoningPrefix.MaxManaRestorePerSecond);
	}

	public int ConsumeAvailableManaRestore(int requestedManaRestore, int maxManaRestorePerSecond)
	{
		if (requestedManaRestore <= 0 || maxManaRestorePerSecond <= 0) {
			return 0;
		}

		ulong currentTick = Main.GameUpdateCount;
		if (currentTick - windowStartTick >= (ulong)SiphoningPrefix.ManaRestoreWindowTicks) {
			windowStartTick = currentTick;
			manaRestoredThisWindow = 0;
		}

		int remainingManaRestore = maxManaRestorePerSecond - manaRestoredThisWindow;
		if (remainingManaRestore <= 0) {
			return 0;
		}

		int appliedManaRestore = System.Math.Min(requestedManaRestore, remainingManaRestore);
		manaRestoredThisWindow += appliedManaRestore;
		return appliedManaRestore;
	}
}
