using Mozandifiers.Content.Prefixes.Weapons;
using Terraria;
using Terraria.ModLoader;

namespace Mozandifiers.Common.Players;

public sealed class AttunedPlayer : ModPlayer
{
	private ulong resonanceExpireTick;
	private ulong empoweredProjectileWindowExpireTick;
	private ulong lastAuraVisualTick;
	private ulong lastHitResonanceGainTick;

	internal int ResonanceStacks { get; private set; }
	internal bool HasEmpoweredCastReady { get; private set; }

	public override void PostUpdate()
	{
		if (Main.GameUpdateCount > resonanceExpireTick) {
			ResetResonance();
		}
	}

	internal bool RegisterQualifyingCast()
	{
		ulong currentTick = Main.GameUpdateCount;
		if (currentTick > resonanceExpireTick) {
			ResetResonance();
		}

		resonanceExpireTick = currentTick + (ulong)AttunedPrefix.ResonanceDecayTicks;
		if (HasEmpoweredCastReady) {
			HasEmpoweredCastReady = false;
			ResonanceStacks = 0;
			empoweredProjectileWindowExpireTick = currentTick + 2ul;
			return true;
		}

		if (ResonanceStacks < AttunedPrefix.MaxResonanceStacks) {
			ResonanceStacks++;
		}

		if (ResonanceStacks >= AttunedPrefix.MaxResonanceStacks) {
			HasEmpoweredCastReady = true;
		}

		return false;
	}

	internal void RegisterQualifyingHit()
	{
		ulong currentTick = Main.GameUpdateCount;
		if (currentTick > resonanceExpireTick) {
			ResetResonance();
		}

		resonanceExpireTick = currentTick + (ulong)AttunedPrefix.ResonanceDecayTicks;
		if (HasEmpoweredCastReady || currentTick - lastHitResonanceGainTick < (ulong)AttunedPrefix.ResonanceHitGainCooldownTicks) {
			return;
		}

		lastHitResonanceGainTick = currentTick;
		if (ResonanceStacks < AttunedPrefix.MaxResonanceStacks) {
			ResonanceStacks++;
		}

		if (ResonanceStacks >= AttunedPrefix.MaxResonanceStacks) {
			HasEmpoweredCastReady = true;
		}
	}

	internal bool IsEmpoweredProjectileWindowActive()
	{
		return Main.GameUpdateCount <= empoweredProjectileWindowExpireTick;
	}

	internal float GetResonanceProgress()
	{
		if (HasEmpoweredCastReady) {
			return 1f;
		}

		return ResonanceStacks / (float)AttunedPrefix.MaxResonanceStacks;
	}

	internal bool CanEmitAuraVisual()
	{
		ulong currentTick = Main.GameUpdateCount;
		int cooldownTicks = HasEmpoweredCastReady ? 6 : 10;
		if (currentTick - lastAuraVisualTick < (ulong)cooldownTicks) {
			return false;
		}

		lastAuraVisualTick = currentTick;
		return true;
	}

	private void ResetResonance()
	{
		ResonanceStacks = 0;
		HasEmpoweredCastReady = false;
		empoweredProjectileWindowExpireTick = 0;
		resonanceExpireTick = 0;
	}
}
