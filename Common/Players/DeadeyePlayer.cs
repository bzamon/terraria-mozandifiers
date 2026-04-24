using Microsoft.Xna.Framework;
using Mozandifiers.Content.Prefixes.Weapons;
using Terraria;
using Terraria.ModLoader;

namespace Mozandifiers.Common.Players;

public sealed class DeadeyePlayer : ModPlayer
{
	private ulong chargeStartTick;
	private Vector2 lastTrackedPosition;
	private bool hasTrackedPosition;
	private ulong lastReadyVisualTick;
	private ulong lastReadySoundTick;
	private ulong lastCritBurstSoundTick;

	internal bool DeadeyeReady { get; private set; }
	internal ulong ReadyAtTick { get; private set; }
	internal ulong LastDeadeyeHoldTick { get; private set; }
	internal ulong StationaryStartTick { get; private set; }
	internal ulong AssignmentLockoutEndTick { get; private set; }

	public override void PostUpdate()
	{
		if (LastDeadeyeHoldTick != Main.GameUpdateCount) {
			ResetState();
		}
	}

	internal void RegisterHeldDeadeyeWeapon(Player player)
	{
		ulong currentTick = Main.GameUpdateCount;
		if (LastDeadeyeHoldTick + 1ul < currentTick) {
			ResetState();
		}

		LastDeadeyeHoldTick = currentTick;
		if (chargeStartTick == 0) {
			chargeStartTick = currentTick;
		}

		UpdateStationaryState(player, currentTick);
		if (DeadeyeReady) {
			return;
		}

		if (currentTick - chargeStartTick >= (ulong)DeadeyePrefix.ReadyIntervalTicks
			|| (StationaryStartTick > 0 && currentTick - StationaryStartTick >= (ulong)DeadeyePrefix.StationaryReadyTicks)) {
			ArmDeadeye(currentTick);
		}
	}

	internal bool TryConsumeReadyShot()
	{
		ulong currentTick = Main.GameUpdateCount;
		if (!DeadeyeReady || currentTick < AssignmentLockoutEndTick) {
			return false;
		}

		DeadeyeReady = false;
		ReadyAtTick = 0;
		chargeStartTick = currentTick;
		StationaryStartTick = currentTick;
		AssignmentLockoutEndTick = currentTick + (ulong)DeadeyePrefix.ProjectileAssignmentLockoutTicks;
		hasTrackedPosition = false;
		return true;
	}

	internal float GetChargeProgress()
	{
		if (DeadeyeReady) {
			return 1f;
		}

		ulong currentTick = Main.GameUpdateCount;
		if (chargeStartTick == 0 || currentTick <= chargeStartTick) {
			return 0f;
		}

		return MathHelper.Clamp(
			(currentTick - chargeStartTick) / (float)DeadeyePrefix.ReadyIntervalTicks,
			0f,
			1f);
	}

	internal bool WasReadiedThisTick()
	{
		return DeadeyeReady && ReadyAtTick == Main.GameUpdateCount;
	}

	internal bool CanEmitReadyVisual()
	{
		return TryConsumeWindow(ref lastReadyVisualTick, 10);
	}

	internal bool CanPlayReadySound()
	{
		return TryConsumeWindow(ref lastReadySoundTick, 16);
	}

	internal bool CanPlayCritBurstSound()
	{
		return TryConsumeWindow(ref lastCritBurstSoundTick, 8);
	}

	private void UpdateStationaryState(Player player, ulong currentTick)
	{
		Vector2 currentPosition = player.Center;
		if (!hasTrackedPosition) {
			lastTrackedPosition = currentPosition;
			hasTrackedPosition = true;
			StationaryStartTick = currentTick;
			return;
		}

		if (Vector2.DistanceSquared(currentPosition, lastTrackedPosition) > DeadeyePrefix.StationaryTolerancePixels * DeadeyePrefix.StationaryTolerancePixels) {
			StationaryStartTick = currentTick;
			lastTrackedPosition = currentPosition;
			return;
		}

		lastTrackedPosition = currentPosition;
	}

	private void ArmDeadeye(ulong currentTick)
	{
		DeadeyeReady = true;
		ReadyAtTick = currentTick;
	}

	private void ResetState()
	{
		DeadeyeReady = false;
		ReadyAtTick = 0;
		LastDeadeyeHoldTick = 0;
		StationaryStartTick = 0;
		AssignmentLockoutEndTick = 0;
		chargeStartTick = 0;
		hasTrackedPosition = false;
		lastTrackedPosition = Vector2.Zero;
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
