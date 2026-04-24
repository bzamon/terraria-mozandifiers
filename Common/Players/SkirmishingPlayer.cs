using Microsoft.Xna.Framework;
using Mozandifiers.Content.Prefixes.Weapons;
using Terraria;
using Terraria.ModLoader;

namespace Mozandifiers.Common.Players;

public sealed class SkirmishingPlayer : ModPlayer
{
	private ulong skirmishWindowExpireTick;
	private ulong followUpProjectileWindowExpireTick;
	private ulong lastWindowVisualTick;

	internal bool HasActiveSkirmishWindow => Main.GameUpdateCount <= skirmishWindowExpireTick;

	public override void PostUpdate()
	{
		if (Main.GameUpdateCount > skirmishWindowExpireTick) {
			followUpProjectileWindowExpireTick = 0;
		}
	}

	internal void RegisterQualifyingHit()
	{
		skirmishWindowExpireTick = Main.GameUpdateCount + (ulong)SkirmishingPrefix.SkirmishWindowTicks;
	}

	internal bool TryConsumeFollowUpShot()
	{
		if (!HasActiveSkirmishWindow) {
			return false;
		}

		skirmishWindowExpireTick = 0;
		followUpProjectileWindowExpireTick = Main.GameUpdateCount + 2ul;
		return true;
	}

	internal bool IsFollowUpProjectileWindowActive()
	{
		return Main.GameUpdateCount <= followUpProjectileWindowExpireTick;
	}

	internal float GetWindowProgress()
	{
		if (!HasActiveSkirmishWindow) {
			return 0f;
		}

		ulong remainingTicks = skirmishWindowExpireTick - Main.GameUpdateCount;
		return MathHelper.Clamp(remainingTicks / (float)SkirmishingPrefix.SkirmishWindowTicks, 0f, 1f);
	}

	internal bool CanEmitWindowVisual()
	{
		ulong currentTick = Main.GameUpdateCount;
		if (currentTick - lastWindowVisualTick < 8ul) {
			return false;
		}

		lastWindowVisualTick = currentTick;
		return true;
	}
}
