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
	private readonly int[] catalyticAppliedProjectileIdentityByPlayer = new int[Main.maxPlayers];
	private readonly ulong[] shiftedCatalyticExpireTickByPlayer = new ulong[Main.maxPlayers];
	private readonly ulong[] shiftedCatalyticArmAfterTickByPlayer = new ulong[Main.maxPlayers];
	private readonly int[] shiftedCatalyticAppliedProjectileIdentityByPlayer = new int[Main.maxPlayers];


    public override bool InstancePerEntity => true;

	public override void PostAI(NPC npc)
	{
		bool hasActualMark = HasActiveCatalyticMark(false);
		bool hasShiftedMark = HasActiveCatalyticMark(true);

		if (!hasActualMark && !hasShiftedMark) {
			return;
		}

		SpawnCatalyticMarkEffect(npc, hasShiftedMark);
	}

	internal void ApplyCatalyticMark(int playerIndex, Projectile projectile)
	{
		ApplyCatalyticMark(playerIndex, projectile, false, CatalyticPrefix.MarkDurationTicks, CatalyticPrefix.MarkArmDelayTicks);
	}

	internal void ApplyCatalyticMark(int playerIndex, Projectile projectile, bool shifted, int durationTicks, int armDelayTicks)
	{
		ClearCatalyticMark(playerIndex, shifted);

        if (!IsValidPlayerIndex(playerIndex) || projectile == null) {
			return;
		}

		ulong currentTick = Main.GameUpdateCount;
		ulong[] expireTicks = shifted ? shiftedCatalyticExpireTickByPlayer : catalyticExpireTickByPlayer;
		ulong[] armAfterTicks = shifted ? shiftedCatalyticArmAfterTickByPlayer : catalyticArmAfterTickByPlayer;
		int[] projectileIdentities = shifted ? shiftedCatalyticAppliedProjectileIdentityByPlayer : catalyticAppliedProjectileIdentityByPlayer;
		expireTicks[playerIndex] = currentTick + (ulong)durationTicks;
		armAfterTicks[playerIndex] = currentTick + (ulong)armDelayTicks;
		projectileIdentities[playerIndex] = projectile.identity + 1;
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
		int[] projectileIdentities = shifted ? shiftedCatalyticAppliedProjectileIdentityByPlayer : catalyticAppliedProjectileIdentityByPlayer;
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

		int dustType = shifted ? DustID.GoldFlame : DustID.Enchanted_Pink;
		for (int i = 0; i < 10; i++) {
			Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.4f, 2.8f);
			Dust dust = Dust.NewDustPerfect(npc.Center, dustType, velocity);
			dust.noGravity = true;
			dust.scale = Main.rand.NextFloat(1f, 1.25f);
			dust.fadeIn = 1f;
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
		int[] projectileIdentities = shifted ? shiftedCatalyticAppliedProjectileIdentityByPlayer : catalyticAppliedProjectileIdentityByPlayer;
		expireTicks[playerIndex] = 0;
		armAfterTicks[playerIndex] = 0;
		projectileIdentities[playerIndex] = 0;
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

	private static void SpawnCatalyticMarkEffect(NPC npc, bool shifted)
	{
		if (npc == null
			|| !npc.active
			|| Main.rand.NextBool(5)) {
			return;
		}

		int dustType = shifted ? DustID.GoldFlame : DustID.Enchanted_Pink;
		Vector2 offset = Main.rand.NextVector2CircularEdge(npc.width * 0.45f, npc.height * 0.55f);
		Dust dust = Dust.NewDustPerfect(npc.Center + offset, dustType);
		dust.noGravity = true;
		dust.velocity = new Vector2(0f, -0.25f) + offset.SafeNormalize(Vector2.UnitY) * 0.35f;
		dust.scale = shifted ? 0.95f : 0.85f;
		dust.fadeIn = 0.9f;
	}
}
