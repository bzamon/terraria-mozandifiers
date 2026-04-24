using Mozandifiers.Content.Prefixes.Weapons;
using Terraria;
using Terraria.ModLoader;

namespace Mozandifiers.Common.Players;

public sealed class AwakenedPlayer : ModPlayer
{
	private ulong awakenedEndTick;
	private ulong recoveryEndTick;
	private ulong lastDormantAuraVisualTick;
	private ulong lastAwakenedAuraVisualTick;
	private ulong lastBuildFeedbackTick;
	private ulong lastAwakenedStrikeVisualTick;

	internal AwakenedState CurrentState { get; private set; }
	internal float MeterProgress { get; private set; }
	internal bool IsAwakened => CurrentState == AwakenedState.Awakened;
	internal bool IsInRecoveryLockout => CurrentState == AwakenedState.RecoveryLockout;

	public override void PostUpdate()
	{
		ulong currentTick = Main.GameUpdateCount;
		if (CurrentState == AwakenedState.Awakened && currentTick >= awakenedEndTick) {
			CurrentState = AwakenedState.RecoveryLockout;
			recoveryEndTick = currentTick + (ulong)AwakenedPrefix.RecoveryLockoutTicks;
			awakenedEndTick = 0;
			MeterProgress = 0f;
			return;
		}

		if (CurrentState == AwakenedState.RecoveryLockout && currentTick >= recoveryEndTick) {
			CurrentState = AwakenedState.Dormant;
			recoveryEndTick = 0;
			MeterProgress = 0f;
		}
	}

	public override void UpdateDead()
	{
		ResetState();
	}

	internal bool RegisterSweetSpotHit(Item item)
	{
		if (CurrentState != AwakenedState.Dormant || item == null || item.IsAir) {
			return false;
		}

		MeterProgress = System.MathF.Min(1f, MeterProgress + AwakenedPrefix.GetMeterGain(item));
		if (MeterProgress < 1f) {
			return false;
		}

		CurrentState = AwakenedState.Awakened;
		awakenedEndTick = Main.GameUpdateCount + (ulong)AwakenedPrefix.AwakenedDurationTicks;
		recoveryEndTick = 0;
		MeterProgress = 0f;
		return true;
	}

	internal float GetCurrentDamageMultiplier()
	{
		return IsAwakened ? 1f + AwakenedPrefix.AwakenedDamageBonus : 1f;
	}

	internal float GetCurrentUseSpeedMultiplier()
	{
		return IsAwakened ? 1f + AwakenedPrefix.AwakenedUseSpeedBonus : 1f;
	}

	internal float GetCurrentScaleMultiplier()
	{
		return IsAwakened ? 1f + AwakenedPrefix.AwakenedScaleBonus : 1f;
	}

	internal float GetStateProgress()
	{
		ulong currentTick = Main.GameUpdateCount;
		return CurrentState switch {
			AwakenedState.Dormant => MeterProgress,
			AwakenedState.Awakened when awakenedEndTick > currentTick => 1f - ((awakenedEndTick - currentTick) / (float)AwakenedPrefix.AwakenedDurationTicks),
			AwakenedState.RecoveryLockout when recoveryEndTick > currentTick => 1f - ((recoveryEndTick - currentTick) / (float)AwakenedPrefix.RecoveryLockoutTicks),
			AwakenedState.Awakened => 1f,
			_ => 0f
		};
	}

	internal bool CanEmitDormantAuraVisual()
	{
		return TryConsumeWindow(ref lastDormantAuraVisualTick, MeterProgress >= 0.75f ? 10 : 16);
	}

	internal bool CanEmitAwakenedAuraVisual()
	{
		return TryConsumeWindow(ref lastAwakenedAuraVisualTick, 6);
	}

	internal bool CanEmitBuildFeedback()
	{
		return TryConsumeWindow(ref lastBuildFeedbackTick, 5);
	}

	internal bool CanEmitAwakenedStrikeVisual()
	{
		return TryConsumeWindow(ref lastAwakenedStrikeVisualTick, 4);
	}

	private void ResetState()
	{
		CurrentState = AwakenedState.Dormant;
		MeterProgress = 0f;
		awakenedEndTick = 0;
		recoveryEndTick = 0;
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
