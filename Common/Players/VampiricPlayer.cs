using Mozandifiers.Content.Prefixes.Weapons;
using Terraria;
using Terraria.ModLoader;

namespace Mozandifiers.Common.Players;

public sealed class VampiricPlayer : ModPlayer
{
	private ulong sustainWindowStartTick;
	private ulong frenzyExpireTick;
	private ulong lastFeedVisualTick;
	private ulong lastFrenzyAuraVisualTick;
	private VampiricPreyState pendingDirectPreyState;
	private int healedThisWindow;
	private int manaRestoredThisWindow;

	internal bool IsFrenzied => Main.GameUpdateCount <= frenzyExpireTick;
	internal VampiricPreyState FrenzyState { get; private set; }

	public override void PostUpdate()
	{
		if (!IsFrenzied) {
			FrenzyState = VampiricPreyState.None;
		}
	}

	internal void TriggerFrenzy(VampiricPreyState preyState, float strengthMultiplier = 1f)
	{
		FrenzyState = preyState;
		int durationTicks = (int)System.MathF.Round(SanguinePrefix.FrenzyDurationTicks * System.MathF.Max(0.75f, strengthMultiplier));
		frenzyExpireTick = Main.GameUpdateCount + (ulong)System.Math.Max(1, durationTicks);
	}

	internal int ConsumeAvailableHeal(int requestedHeal)
	{
		return ConsumeAvailableHeal(requestedHeal, SanguinePrefix.BaseHealCapPerSecond);
	}

	internal int ConsumeAvailableHeal(int requestedHeal, int healCapPerSecond)
	{
		if (requestedHeal <= 0 || healCapPerSecond <= 0) {
			return 0;
		}

		RefreshSustainWindow();
		int effectiveCap = ApplyFrenzyCapBonus(healCapPerSecond);
		int remainingHeal = effectiveCap - healedThisWindow;
		if (remainingHeal <= 0) {
			return 0;
		}

		int appliedHeal = System.Math.Min(requestedHeal, remainingHeal);
		healedThisWindow += appliedHeal;
		return appliedHeal;
	}

	internal int ConsumeAvailableManaRestore(int requestedManaRestore)
	{
		return ConsumeAvailableManaRestore(requestedManaRestore, SanguinePrefix.BaseManaRestorePerSecond);
	}

	internal int ConsumeAvailableManaRestore(int requestedManaRestore, int manaRestoreCapPerSecond)
	{
		if (requestedManaRestore <= 0 || manaRestoreCapPerSecond <= 0) {
			return 0;
		}

		RefreshSustainWindow();
		int effectiveCap = ApplyFrenzyCapBonus(manaRestoreCapPerSecond);
		int remainingRestore = effectiveCap - manaRestoredThisWindow;
		if (remainingRestore <= 0) {
			return 0;
		}

		int appliedRestore = System.Math.Min(requestedManaRestore, remainingRestore);
		manaRestoredThisWindow += appliedRestore;
		return appliedRestore;
	}

	internal float GetFrenzyProgress()
	{
		if (!IsFrenzied) {
			return 0f;
		}

		ulong ticksRemaining = frenzyExpireTick - Main.GameUpdateCount;
		return System.MathF.Min(1f, ticksRemaining / (float)SanguinePrefix.FrenzyDurationTicks);
	}

	internal float GetFrenzyTierScale()
	{
		return SanguinePrefix.GetFrenzyTierScale(FrenzyState);
	}

	internal float GetFrenzyAttackSpeedBonus()
	{
		return SanguinePrefix.FrenzyAttackSpeedBonus * GetFrenzyTierScale();
	}

	internal float GetFrenzyCapMultiplier()
	{
		return 1f + ((SanguinePrefix.FrenzyCapMultiplier - 1f) * GetFrenzyTierScale());
	}

	internal bool CanEmitFeedVisual(bool strongerFeed)
	{
		return TryConsumeVisualWindow(ref lastFeedVisualTick, strongerFeed ? 5 : 8);
	}

	internal bool CanEmitFrenzyAuraVisual()
	{
		return TryConsumeVisualWindow(ref lastFrenzyAuraVisualTick, 7);
	}

	internal void QueuePendingDirectPreyState(VampiricPreyState preyState)
	{
		pendingDirectPreyState = preyState;
	}

	internal VampiricPreyState ConsumePendingDirectPreyState()
	{
		VampiricPreyState preyState = pendingDirectPreyState;
		pendingDirectPreyState = VampiricPreyState.None;
		return preyState;
	}

	private void RefreshSustainWindow()
	{
		ulong currentTick = Main.GameUpdateCount;
		if (currentTick - sustainWindowStartTick >= (ulong)SanguinePrefix.SustainWindowTicks) {
			sustainWindowStartTick = currentTick;
			healedThisWindow = 0;
			manaRestoredThisWindow = 0;
		}
	}

	private int ApplyFrenzyCapBonus(int baseCap)
	{
		if (!IsFrenzied) {
			return baseCap;
		}

		return System.Math.Max(baseCap, (int)System.MathF.Round(baseCap * GetFrenzyCapMultiplier()));
	}

	private static bool TryConsumeVisualWindow(ref ulong lastTick, int cooldownTicks)
	{
		ulong currentTick = Main.GameUpdateCount;
		if (currentTick - lastTick < (ulong)cooldownTicks) {
			return false;
		}

		lastTick = currentTick;
		return true;
	}
}
