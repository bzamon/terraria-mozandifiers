using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Mozandifiers.Content.Prefixes.Weapons;

namespace Mozandifiers.Content.Prefixes.Common;

public sealed class WeaponPrefixGlobalNPC : GlobalNPC
{
	private readonly ulong[] catalyticExpireTickByPlayer = new ulong[Main.maxPlayers];
	private readonly ulong[] catalyticArmAfterTickByPlayer = new ulong[Main.maxPlayers];
	private readonly ulong[] shiftedCatalyticExpireTickByPlayer = new ulong[Main.maxPlayers];
	private readonly ulong[] shiftedCatalyticArmAfterTickByPlayer = new ulong[Main.maxPlayers];

	public override bool InstancePerEntity => true;

	public override void PostAI(NPC npc)
	{
		bool hasActualMark = HasActiveCatalyticMark(false);
		bool hasShiftedMark = HasActiveCatalyticMark(true);

		if (!hasActualMark && !hasShiftedMark) {
			return;
		}

		bool shifted = hasShiftedMark;
		float pulse = 0.86f + 0.14f * (0.5f + 0.5f * System.MathF.Sin(Main.GlobalTimeWrappedHourly * 8f + npc.whoAmI * 0.35f));
		Lighting.AddLight(npc.Center, WeaponPrefixVisuals.GetCatalyticLightColor(shifted).ToVector3() * (0.2f * pulse));
		SpawnCatalyticMarkEffect(npc, shifted, pulse);
	}

	internal void ApplyCatalyticMark(int playerIndex, Projectile projectile)
	{
		ApplyCatalyticMark(playerIndex, projectile, false, CatalyticPrefix.MarkDurationTicks, CatalyticPrefix.MarkArmDelayTicks);
	}

	internal void ApplyCatalyticMark(int playerIndex, Projectile projectile, bool shifted, int durationTicks, int armDelayTicks)
	{
		if (!IsValidPlayerIndex(playerIndex) || projectile == null) {
			return;
		}

		ulong currentTick = Main.GameUpdateCount;
		ulong[] expireTicks = shifted ? shiftedCatalyticExpireTickByPlayer : catalyticExpireTickByPlayer;
		ulong[] armAfterTicks = shifted ? shiftedCatalyticArmAfterTickByPlayer : catalyticArmAfterTickByPlayer;
		bool hadActiveMark = expireTicks[playerIndex] > currentTick;
		expireTicks[playerIndex] = currentTick + (ulong)durationTicks;
		armAfterTicks[playerIndex] = currentTick + (ulong)armDelayTicks;

		if (!hadActiveMark) {
			SpawnCatalyticApplyEffect(projectile.Center, shifted);
		}
	}

	internal bool ShouldConsumeCatalyticMark(int playerIndex, Projectile projectile)
	{
		return ShouldConsumeCatalyticMark(playerIndex, projectile, false);
	}

	internal bool ShouldConsumeCatalyticMark(int playerIndex, Projectile projectile, bool shifted)
	{
		if (!IsValidPlayerIndex(playerIndex) || projectile == null) {
			return false;
		}

		ulong[] expireTicks = shifted ? shiftedCatalyticExpireTickByPlayer : catalyticExpireTickByPlayer;
		ulong[] armAfterTicks = shifted ? shiftedCatalyticArmAfterTickByPlayer : catalyticArmAfterTickByPlayer;
		ulong currentTick = Main.GameUpdateCount;
		
		if (expireTicks[playerIndex] <= currentTick) {
			ClearCatalyticMark(playerIndex, shifted);
			return false;
		}

		return armAfterTicks[playerIndex] <= currentTick;
	}

	internal void ConsumeCatalyticMark(int playerIndex)
	{
		ConsumeCatalyticMark(playerIndex, false);
	}

	internal void ConsumeCatalyticMark(int playerIndex, bool shifted)
	{
		ClearCatalyticMark(playerIndex, shifted);
	}

	internal void SpawnCatalyticConsumeEffect(NPC npc, bool shifted)
	{
		if (npc == null || !npc.active) {
			return;
		}

		int dustType = WeaponPrefixVisuals.GetCatalyticDustType(shifted);
		Lighting.AddLight(npc.Center, WeaponPrefixVisuals.GetCatalyticLightColor(shifted).ToVector3() * 0.32f);

		for (int i = 0; i < 14; i++) {
			Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.8f, 3.2f);
			Dust dust = Dust.NewDustPerfect(npc.Center, dustType, velocity);
			dust.noGravity = true;
			dust.scale = Main.rand.NextFloat(1f, 1.35f);
			dust.fadeIn = 1f;
		}

		for (int i = 0; i < 8; i++) {
			float angle = MathHelper.TwoPi * i / 8f;
			Vector2 offset = angle.ToRotationVector2() * 14f;
			Dust dust = Dust.NewDustPerfect(npc.Center + offset, dustType, offset.SafeNormalize(Vector2.UnitX) * 1.2f);
			dust.noGravity = true;
			dust.scale = 1.1f;
			dust.fadeIn = 0.95f;
		}
	}

	private void ClearCatalyticMark(int playerIndex)
	{
		ClearCatalyticMark(playerIndex, false);
	}

	public void ClearCatalyticMark(int playerIndex, bool shifted)
	{
		if (!IsValidPlayerIndex(playerIndex)) {
			return;
		}

		ulong[] expireTicks = shifted ? shiftedCatalyticExpireTickByPlayer : catalyticExpireTickByPlayer;
		ulong[] armAfterTicks = shifted ? shiftedCatalyticArmAfterTickByPlayer : catalyticArmAfterTickByPlayer;
		expireTicks[playerIndex] = 0;
		armAfterTicks[playerIndex] = 0;
	}

    private static bool IsValidPlayerIndex(int playerIndex)
	{
		return playerIndex >= 0 && playerIndex < Main.maxPlayers;
	}

	private bool HasActiveCatalyticMark(bool shifted)
	{
		ulong currentTick = Main.GameUpdateCount;
		ulong[] expireTicks = shifted ? shiftedCatalyticExpireTickByPlayer : catalyticExpireTickByPlayer;
		for (int i = 0; i < Main.maxPlayers; i++) {
			if (expireTicks[i] > currentTick) {
				return true;
			}
		}

		return false;
	}

	private static void SpawnCatalyticApplyEffect(Vector2 position, bool shifted)
	{
		int dustType = WeaponPrefixVisuals.GetCatalyticDustType(shifted);
		for (int i = 0; i < 6; i++) {
			float angle = MathHelper.TwoPi * i / 6f;
			Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(0.8f, 1.6f);
			Dust dust = Dust.NewDustPerfect(position, dustType, velocity);
			dust.noGravity = true;
			dust.scale = shifted ? 1.1f : 0.95f;
			dust.fadeIn = 0.95f;
		}
	}

	private static void SpawnCatalyticMarkEffect(NPC npc, bool shifted, float pulse)
	{
		if (npc == null
			|| !npc.active
			|| Main.rand.NextBool(4)) {
			return;
		}

		int dustType = WeaponPrefixVisuals.GetCatalyticDustType(shifted);
		Vector2 offset = Main.rand.NextVector2CircularEdge(npc.width * 0.45f, npc.height * 0.55f);
		Dust dust = Dust.NewDustPerfect(npc.Center + offset, dustType);
		dust.noGravity = true;
		dust.velocity = new Vector2(0f, -0.2f) + offset.SafeNormalize(Vector2.UnitY) * (0.25f + pulse * 0.18f);
		dust.scale = (shifted ? 0.95f : 0.85f) * pulse;
		dust.fadeIn = 0.9f;
	}
}
