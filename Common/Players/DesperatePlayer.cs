using Mozandifiers.Content.Prefixes.Weapons;
using Terraria;
using Terraria.ModLoader;

namespace Mozandifiers.Common.Players;

public sealed class DesperatePlayer : ModPlayer
{
	private ulong nextSurgeAvailableTick;
	private ulong lastMinorVisualTick;
	private ulong lastSevereVisualTick;
	private ulong lastCriticalVisualTick;
	private bool pendingDirectSurgeFeedback;
	private float pendingDirectSurgeVisualMultiplier = 1f;

	internal DesperateThresholdState CurrentState { get; private set; }

	public override void PostUpdate()
	{
		CurrentState = DesperatePrefix.GetThresholdState(Player);
	}

	internal bool TryConsumeSurge()
	{
		if (CurrentState != DesperateThresholdState.Critical) {
			return false;
		}

		ulong currentTick = Main.GameUpdateCount;
		if (currentTick < nextSurgeAvailableTick) {
			return false;
		}

		nextSurgeAvailableTick = currentTick + (ulong)DesperatePrefix.SurgeCooldownTicks;
		return true;
	}

	internal bool CanEmitStateVisual()
	{
		return CurrentState switch {
			DesperateThresholdState.Minor => TryConsumeWindow(ref lastMinorVisualTick, 18),
			DesperateThresholdState.Severe => TryConsumeWindow(ref lastSevereVisualTick, 12),
			DesperateThresholdState.Critical => TryConsumeWindow(ref lastCriticalVisualTick, 8),
			_ => false
		};
	}

	internal float GetSurgeCooldownProgress()
	{
		if (CurrentState != DesperateThresholdState.Critical) {
			return 0f;
		}

		ulong currentTick = Main.GameUpdateCount;
		if (currentTick >= nextSurgeAvailableTick) {
			return 1f;
		}

		ulong ticksRemaining = nextSurgeAvailableTick - currentTick;
		return 1f - System.MathF.Min(1f, ticksRemaining / (float)DesperatePrefix.SurgeCooldownTicks);
	}

	internal void QueueDirectSurgeFeedback(float visualMultiplier)
	{
		pendingDirectSurgeFeedback = true;
		pendingDirectSurgeVisualMultiplier = System.MathF.Max(pendingDirectSurgeVisualMultiplier, visualMultiplier);
	}

	internal bool TryConsumeDirectSurgeFeedback(out float visualMultiplier)
	{
		visualMultiplier = 1f;
		if (!pendingDirectSurgeFeedback) {
			return false;
		}

		pendingDirectSurgeFeedback = false;
		visualMultiplier = pendingDirectSurgeVisualMultiplier;
		pendingDirectSurgeVisualMultiplier = 1f;
		return true;
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
