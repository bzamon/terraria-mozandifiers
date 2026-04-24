using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Mozandifiers.Content.Prefixes.Weapons;
using System.Collections.Generic;
using Mozandifiers.Common.Players;

namespace Mozandifiers.Content.Prefixes.Common;

public sealed class WeaponPrefixGlobalNPC : GlobalNPC
{
	private readonly struct AwakenedBossBonusSample
	{
		public AwakenedBossBonusSample(ulong tick, int damage)
		{
			Tick = tick;
			Damage = damage;
		}

		public ulong Tick { get; }
		public int Damage { get; }
	}

	internal readonly struct TemporalPressureBuildResult
	{
		public TemporalPressureBuildResult(float pressureAdded, float currentPressure, bool startedFracture)
		{
			PressureAdded = pressureAdded;
			CurrentPressure = currentPressure;
			StartedFracture = startedFracture;
		}

		public float PressureAdded { get; }
		public float CurrentPressure { get; }
		public bool StartedFracture { get; }
	}

	private readonly ulong[] catalyticExpireTickByPlayer = new ulong[Main.maxPlayers];
	private readonly ulong[] catalyticArmAfterTickByPlayer = new ulong[Main.maxPlayers];
	private readonly ulong[] shiftedCatalyticExpireTickByPlayer = new ulong[Main.maxPlayers];
	private readonly ulong[] shiftedCatalyticArmAfterTickByPlayer = new ulong[Main.maxPlayers];
	private readonly ulong[] vampiricLastFrenzyTriggerTickByPlayer = new ulong[Main.maxPlayers];
	private readonly byte[] vampiricTriggeredThresholdMaskByPlayer = new byte[Main.maxPlayers];
	private readonly Queue<AwakenedBossBonusSample>[] awakenedBossBonusSamplesByPlayer = new Queue<AwakenedBossBonusSample>[Main.maxPlayers];
	private readonly float[] temporalPressureByPlayer = new float[Main.maxPlayers];
	private readonly ulong[] temporalLastPressureHitTickByPlayer = new ulong[Main.maxPlayers];
	private readonly ulong[] temporalFractureEndTickByPlayer = new ulong[Main.maxPlayers];
	private readonly ulong[] temporalCooldownEndTickByPlayer = new ulong[Main.maxPlayers];
	private readonly int[] temporalStoredDamageByPlayer = new int[Main.maxPlayers];

	public override bool InstancePerEntity => true;

	public override void PostAI(NPC npc)
	{
		bool hasActualMark = HasActiveCatalyticMark(false);
		bool hasShiftedMark = HasActiveCatalyticMark(true);

		if (!hasActualMark && !hasShiftedMark) {
			UpdateTemporalFractureState(npc);
			return;
		}

		bool shifted = hasShiftedMark;
		float pulse = 0.86f + 0.14f * (0.5f + 0.5f * System.MathF.Sin(Main.GlobalTimeWrappedHourly * 8f + npc.whoAmI * 0.35f));
		Lighting.AddLight(npc.Center, WeaponPrefixVisuals.GetCatalyticLightColor(shifted).ToVector3() * (0.2f * pulse));
		SpawnCatalyticMarkEffect(npc, shifted, pulse);
		UpdateTemporalFractureState(npc);
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

	internal bool TryTriggerVampiricFrenzy(int playerIndex, VampiricPreyState preyState)
	{
		if (!IsValidPlayerIndex(playerIndex) || preyState == VampiricPreyState.None) {
			return false;
		}

		ulong currentTick = Main.GameUpdateCount;
		byte thresholdMask = GetVampiricThresholdMask(preyState);
		bool thresholdAlreadyTriggered = (vampiricTriggeredThresholdMaskByPlayer[playerIndex] & thresholdMask) != 0;
		bool cooldownReady = currentTick - vampiricLastFrenzyTriggerTickByPlayer[playerIndex] >= (ulong)SanguinePrefix.FrenzyRetriggerCooldownTicks;
		if (thresholdAlreadyTriggered && !cooldownReady) {
			return false;
		}

		vampiricTriggeredThresholdMaskByPlayer[playerIndex] |= thresholdMask;
		vampiricLastFrenzyTriggerTickByPlayer[playerIndex] = currentTick;
		return true;
	}

	internal TemporalPressureBuildResult TryBuildTemporalPressure(int playerIndex, NPC npc, int damageDone)
	{
		if (!IsValidPlayerIndex(playerIndex)
			|| npc == null
			|| !npc.active
			|| npc.lifeMax <= 0
			|| damageDone <= 0) {
			return new TemporalPressureBuildResult(0f, 0f, false);
		}

		ulong currentTick = Main.GameUpdateCount;
		if (IsTemporalFracturedForPlayer(playerIndex)
			|| temporalCooldownEndTickByPlayer[playerIndex] > currentTick) {
			return new TemporalPressureBuildResult(0f, temporalPressureByPlayer[playerIndex], false);
		}

		float pressureToAdd = TemporalPrefix.BasePressurePerHit
			+ (damageDone / (float)npc.lifeMax) * TemporalPrefix.DamagePressureFactor;
		if (currentTick - temporalLastPressureHitTickByPlayer[playerIndex] <= (ulong)TemporalPrefix.TempoBonusWindowTicks) {
			pressureToAdd += TemporalPrefix.TempoBonusPressure;
		}

		if (npc.boss) {
			pressureToAdd *= TemporalPrefix.BossPressureMultiplier;
		}

		temporalLastPressureHitTickByPlayer[playerIndex] = currentTick;
		float newPressure = System.MathF.Min(TemporalPrefix.PressureMax, temporalPressureByPlayer[playerIndex] + pressureToAdd);
		temporalPressureByPlayer[playerIndex] = newPressure;
		bool startedFracture = false;
		if (newPressure >= TemporalPrefix.PressureMax) {
			TryStartTemporalFracture(playerIndex);
			startedFracture = true;
		}

		return new TemporalPressureBuildResult(
			pressureToAdd,
			startedFracture ? TemporalPrefix.PressureMax : temporalPressureByPlayer[playerIndex],
			startedFracture);
	}

	internal bool IsTemporalFracturedForPlayer(int playerIndex)
	{
		return IsValidPlayerIndex(playerIndex)
			&& temporalFractureEndTickByPlayer[playerIndex] > Main.GameUpdateCount;
	}

	internal int StoreTemporalDamage(int playerIndex, int damageDone)
	{
		if (!IsValidPlayerIndex(playerIndex) || damageDone <= 0) {
			return 0;
		}

		temporalStoredDamageByPlayer[playerIndex] += damageDone;
		return temporalStoredDamageByPlayer[playerIndex];
	}

	internal int GetTemporalStoredDamage(int playerIndex)
	{
		return IsValidPlayerIndex(playerIndex) ? temporalStoredDamageByPlayer[playerIndex] : 0;
	}

	internal bool TryTriggerTemporalCollapse(NPC npc, int playerIndex, float visualMultiplier = 1f)
	{
		if (!IsValidPlayerIndex(playerIndex)
			|| npc == null
			|| !npc.active) {
			return false;
		}

		int storedDamage = temporalStoredDamageByPlayer[playerIndex];
		temporalStoredDamageByPlayer[playerIndex] = 0;
		temporalFractureEndTickByPlayer[playerIndex] = 0;
		temporalPressureByPlayer[playerIndex] = 0f;
		temporalCooldownEndTickByPlayer[playerIndex] = Main.GameUpdateCount + (ulong)TemporalPrefix.FractureCooldownTicks;
		if (storedDamage <= 0) {
			return false;
		}

		int collapseDamage = System.Math.Max(1, (int)System.MathF.Round(storedDamage * TemporalPrefix.ReleaseMultiplier));
		if (Main.netMode != NetmodeID.MultiplayerClient) {
			npc.SimpleStrikeNPC(collapseDamage, 0);
			npc.netUpdate = true;
		}

		if (playerIndex == Main.myPlayer) {
			Player player = Main.player[playerIndex];
			if (player.active && !player.dead) {
				TemporalFeedbackPlayer temporalPlayer = player.GetModPlayer<TemporalFeedbackPlayer>();
				if (temporalPlayer.CanEmitCollapseVisual()) {
					WeaponPrefixGlobalItem.SpawnTemporalCollapseEffect(npc.Center, visualMultiplier);
				}
			}
		}

		return true;
	}

	internal int ConsumeAwakenedBossBonusBudget(int playerIndex, int requestedBonusDamage, int maxBonusDamagePerSecond)
	{
		if (!IsValidPlayerIndex(playerIndex) || requestedBonusDamage <= 0 || maxBonusDamagePerSecond <= 0) {
			return 0;
		}

		Queue<AwakenedBossBonusSample> samples = awakenedBossBonusSamplesByPlayer[playerIndex] ??= new Queue<AwakenedBossBonusSample>();
		ulong currentTick = Main.GameUpdateCount;
		TrimAwakenedBossBonusSamples(samples, currentTick);

		int spentBonusDamage = 0;
		foreach (AwakenedBossBonusSample sample in samples) {
			spentBonusDamage += sample.Damage;
		}

		int allowedBonusDamage = System.Math.Max(0, maxBonusDamagePerSecond - spentBonusDamage);
		int appliedBonusDamage = System.Math.Min(requestedBonusDamage, allowedBonusDamage);
		if (appliedBonusDamage > 0) {
			samples.Enqueue(new AwakenedBossBonusSample(currentTick, appliedBonusDamage));
		}

		return appliedBonusDamage;
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

	private static byte GetVampiricThresholdMask(VampiricPreyState preyState)
	{
		return preyState switch {
			VampiricPreyState.Weakened => 1 << 0,
			VampiricPreyState.Bloodied => 1 << 1,
			VampiricPreyState.Critical => 1 << 2,
			_ => 0
		};
	}

	private static void TrimAwakenedBossBonusSamples(Queue<AwakenedBossBonusSample> samples, ulong currentTick)
	{
		while (samples.Count > 0 && currentTick - samples.Peek().Tick >= (ulong)AwakenedPrefix.BossBonusDamageWindowTicks) {
			samples.Dequeue();
		}
	}

	private void TryStartTemporalFracture(int playerIndex)
	{
		temporalPressureByPlayer[playerIndex] = 0f;
		temporalStoredDamageByPlayer[playerIndex] = 0;
		temporalFractureEndTickByPlayer[playerIndex] = Main.GameUpdateCount + (ulong)TemporalPrefix.FractureDurationTicks;
	}

	private void UpdateTemporalFractureState(NPC npc)
	{
		ulong currentTick = Main.GameUpdateCount;
		bool hasActiveFracture = false;
		float strongestPressureRatio = 0f;

		for (int i = 0; i < Main.maxPlayers; i++) {
			if (temporalFractureEndTickByPlayer[i] > currentTick) {
				hasActiveFracture = true;
				float pressureRatio = System.MathF.Min(1f, temporalStoredDamageByPlayer[i] / System.MathF.Max(1f, npc.lifeMax));
				strongestPressureRatio = System.MathF.Max(strongestPressureRatio, pressureRatio);
				continue;
			}

			if (temporalFractureEndTickByPlayer[i] != 0 && currentTick >= temporalFractureEndTickByPlayer[i]) {
				TryTriggerTemporalCollapse(npc, i);
			}
		}

		if (!hasActiveFracture || !npc.active) {
			return;
		}

		float pulse = WeaponPrefixVisuals.GetTemporalPulse(npc.whoAmI * 0.31f);
		Color fractureColor = Color.Lerp(
			WeaponPrefixVisuals.TemporalFractureColor,
			WeaponPrefixVisuals.TemporalCollapseColor,
			strongestPressureRatio);
		Lighting.AddLight(npc.Center, fractureColor.ToVector3() * (0.2f * pulse));

		if (Main.rand.NextBool(4)) {
			Vector2 offset = Main.rand.NextVector2CircularEdge(npc.width * 0.45f, npc.height * 0.55f);
			Dust dust = Dust.NewDustPerfect(npc.Center + offset, WeaponPrefixVisuals.TemporalDustType);
			dust.noGravity = true;
			dust.velocity = offset.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2) * (0.25f + 0.15f * pulse);
			dust.scale = 0.82f + 0.18f * strongestPressureRatio;
			dust.fadeIn = 0.95f;
		}
	}
}
