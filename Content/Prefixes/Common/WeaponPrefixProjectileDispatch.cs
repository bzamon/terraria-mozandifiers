using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mozandifiers.Common.Combat;
using Mozandifiers.Common.Config;
using Mozandifiers.Common.Players;
using Mozandifiers.Content.Prefixes.Weapons;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader.IO;

namespace Mozandifiers.Content.Prefixes.Common;

internal static class WeaponPrefixProjectileSpawnDispatch
{
	internal static void OnSpawn(WeaponPrefixGlobalProjectile global, Projectile projectile, IEntitySource source)
	{
		WeaponPrefixProjectileSyncDispatch.TryInitializeShiftedState(global, projectile, source);

		if (global.IsSpawnedFracturedProjectile || projectile.owner != Main.myPlayer) {
			return;
		}

		Item item = null;
		if (source is IEntitySource_WithStatsFromItem itemSource) {
			item = itemSource.Item;
			if (item == null || item.IsAir) {
				return;
			}

			if (WeaponPrefixIdentity.HasVampiricPrefix(item)) {
				global.IsVampiricProjectile = true;
				if (WeaponPrefix.IsMagicWeapon(item)) {
					global.VampiricManaRestoreAmount = VampiricRuntime.GetManaRestoreAmount(item);
				}
			}

			if (WeaponPrefixIdentity.HasRadiantPrefix(item)) {
				global.IsRadiantProjectile = true;
			}

			if (WeaponPrefixIdentity.HasBreachingPrefix(item)) {
				global.IsBreachingProjectile = true;
				global.BreachImpactScale = BreachingPrefix.GetBreachImpactMultiplier(item);
			}

			if (WeaponPrefixIdentity.HasDesperatePrefix(item)) {
				global.IsDesperateProjectile = true;
			}

			if (WeaponPrefixIdentity.HasTemporalPrefix(item)) {
				global.IsTemporalProjectile = true;
			}

			if (WeaponPrefixIdentity.HasSkirmishingPrefix(item)) {
				global.IsSkirmishingProjectile = true;
				Player owner = Main.player[projectile.owner];
				if (owner.active && !owner.dead) {
					global.IsSkirmishFollowUpProjectile = owner.GetModPlayer<SkirmishingPlayer>().IsFollowUpProjectileWindowActive();
				}
			}

			if (WeaponPrefixIdentity.HasAttunedPrefix(item)) {
				global.IsAttunedProjectile = true;
				Player owner = Main.player[projectile.owner];
				if (owner.active && !owner.dead) {
					global.IsEmpoweredAttunedProjectile = owner.GetModPlayer<AttunedPlayer>().IsEmpoweredProjectileWindowActive();
				}
			}

			if (WeaponPrefixIdentity.HasStormforgedPrefix(item)) {
				global.IsStormforgedProjectile = true;
			}

			if (WeaponPrefixIdentity.HasCatalyticPrefix(item)) {
				global.IsCatalyticProjectile = true;
			}

			if (WeaponPrefixIdentity.HasDeadeyePrefix(item)) {
				Player owner = Main.player[projectile.owner];
				if (owner.active && !owner.dead && owner.GetModPlayer<DeadeyePlayer>().TryConsumeReadyShot()) {
					global.IsDeadeyeProjectile = true;
					projectile.velocity *= DeadeyePrefix.DeadeyeShotVelocityMultiplier;
				}
			}

			if (WeaponPrefixIdentity.HasSpinboundPrefix(item)) {
				SpinboundRuntime.TryInitializeLiveProjectile(global, projectile, item);
			}

			if (WeaponPrefixIdentity.TryGetShiftingSimulation(item, projectile.owner, out ShiftingSimulationId shiftedSimulationId)) {
				if (shiftedSimulationId == ShiftingSimulationId.Breaching) {
					global.BreachImpactScale = BreachingPrefix.GetBreachImpactMultiplier(item) * ShiftingPrefix.ShiftingStrengthMultiplier;
				}

				if (shiftedSimulationId == ShiftingSimulationId.Fractured) {
					FracturedRuntime.ScheduleCascade(
						global,
						projectile,
						ShiftingSimulationEffectScaling.GetShiftedFractureIChance(),
						ShiftingSimulationEffectScaling.GetShiftedFractureIIChance(),
						ShiftingSimulationEffectScaling.GetShiftedFractureIIIChance());
				}

				if (shiftedSimulationId == ShiftingSimulationId.Deadeye) {
					Player owner = Main.player[projectile.owner];
					if (owner.active && !owner.dead && owner.GetModPlayer<DeadeyePlayer>().TryConsumeReadyShot()) {
						global.IsShiftedDeadeyeProjectile = true;
						projectile.velocity *= ShiftingSimulationEffectScaling.GetShiftedDeadeyeVelocityMultiplier();
					}
				}
			}
		}
		else if (source is EntitySource_Parent parentSource && parentSource.Entity is Projectile parentProjectile) {
			WeaponPrefixGlobalProjectile parentGlobal = parentProjectile.GetGlobalProjectile<WeaponPrefixGlobalProjectile>();
			if (parentGlobal.IsVampiricProjectile) {
				global.IsVampiricProjectile = true;
				global.VampiricManaRestoreAmount = parentGlobal.VampiricManaRestoreAmount;
			}

			if (parentGlobal.IsRadiantProjectile) {
				global.IsRadiantProjectile = true;
			}

			if (parentGlobal.IsStormforgedProjectile) {
				global.IsStormforgedProjectile = true;
			}
		}
		else {
			return;
		}

		if (item == null || !WeaponPrefixIdentity.HasFracturedPrefix(item)) {
			return;
		}

		FracturedRuntime.ScheduleCascade(
			global,
			projectile,
			PrefixTuningConfig.Instance.FractureIChance,
			PrefixTuningConfig.Instance.FractureIIChance,
			PrefixTuningConfig.Instance.FractureIIIChance);
	}
}

internal static class WeaponPrefixProjectileHitDispatch
{
	internal static void OnHitNPC(WeaponPrefixGlobalProjectile global, Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (global.ShiftedSimulationId != ShiftingSimulationId.None && damageDone > 0) {
			Player owner = Main.player[projectile.owner];
			if (owner.active && !owner.dead) {
				owner.GetModPlayer<ShiftingPlayer>().RegisterSuccessfulHit(owner.HeldItem);
			}
		}

		CatalyticRuntime.HandleHit(global, projectile, target, damageDone);

		if (global.IsVampiricProjectile) {
			Player owner = Main.player[projectile.owner];
			if (owner.active && !owner.dead) {
				VampiricRuntime.ApplyProjectileFeed(
					owner,
					target,
					damageDone,
					hit.Crit,
					global.pendingVampiricPreyState,
					global.VampiricManaRestoreAmount);
			}
		}

		if (global.IsRadiantProjectile && target.active) {
			WeaponPrefixItemHitDispatch.ApplyRadiantHitEffects(target);
		}

		if (global.IsBreachingProjectile) {
			Player owner = Main.player[projectile.owner];
			if (owner.active && !owner.dead) {
				WeaponPrefixItemHitDispatch.TryTriggerBreachingImpact(owner, target, projectile.velocity, damageDone, global.BreachImpactScale);
			}
		}

		if (global.IsSkirmishingProjectile && damageDone > 0) {
			Player owner = Main.player[projectile.owner];
			if (owner.active && !owner.dead) {
				owner.GetModPlayer<SkirmishingPlayer>().RegisterQualifyingHit();
			}
		}

		if (global.IsAttunedProjectile && damageDone > 0) {
			Player owner = Main.player[projectile.owner];
			if (owner.active && !owner.dead) {
				owner.GetModPlayer<AttunedPlayer>().RegisterQualifyingHit();
			}
		}

		if (global.IsTemporalProjectile && damageDone > 0) {
			Player owner = Main.player[projectile.owner];
			if (owner.active && !owner.dead) {
				TemporalRuntime.HandleHit(owner, target, damageDone, 1f);
			}
		}

		if (global.IsStormforgedProjectile) {
			Player owner = Main.player[projectile.owner];
			if (owner.active && !owner.dead) {
				StormforgedChainHelper.TryTriggerChain(owner, target, damageDone);
			}
		}

		if (global.ShiftedSimulationId == ShiftingSimulationId.Stormforged) {
			Player owner = Main.player[projectile.owner];
			if (owner.active && !owner.dead) {
				StormforgedChainHelper.TryTriggerChain(
					owner,
					target,
					damageDone,
					ShiftingSimulationEffectScaling.GetShiftedStormforgedProcChance(),
					ShiftingSimulationEffectScaling.GetShiftedStormforgedMaxChainJumps(),
					ShiftingSimulationEffectScaling.GetShiftedStormforgedChainRangePixels(),
					ShiftingSimulationEffectScaling.GetShiftedStormforgedChainDamageDecay());
			}
		}

		if (global.ShiftedSimulationId == ShiftingSimulationId.Vampiric) {
			Player owner = Main.player[projectile.owner];
			if (owner.active && !owner.dead) {
				VampiricRuntime.ApplyProjectileFeed(
					owner,
					target,
					damageDone,
					hit.Crit,
					global.pendingVampiricPreyState,
					global.ShiftedVampiricManaRestoreAmount,
					ShiftingPrefix.ShiftingStrengthMultiplier,
					true);
			}
		}

		if (global.ShiftedSimulationId == ShiftingSimulationId.Radiant && target.active) {
			WeaponPrefixItemHitDispatch.ApplyRadiantHitEffects(
				target,
				ShiftingSimulationEffectScaling.GetShiftedRadiantDurationTicks(),
				ShiftingSimulationEffectScaling.GetShiftedRadiantLightMultiplier());
		}

		if (global.ShiftedSimulationId == ShiftingSimulationId.Breaching) {
			Player owner = Main.player[projectile.owner];
			if (owner.active && !owner.dead) {
				WeaponPrefixItemHitDispatch.TryTriggerBreachingImpact(
					owner,
					target,
					projectile.velocity,
					damageDone,
					global.BreachImpactScale > 0f ? global.BreachImpactScale : 1f);
			}
		}

		if (global.ShiftedSimulationId == ShiftingSimulationId.Skirmishing && damageDone > 0) {
			Player owner = Main.player[projectile.owner];
			if (owner.active && !owner.dead) {
				owner.GetModPlayer<SkirmishingPlayer>().RegisterQualifyingHit();
			}
		}

		if (global.ShiftedSimulationId == ShiftingSimulationId.Attuned && damageDone > 0) {
			Player owner = Main.player[projectile.owner];
			if (owner.active && !owner.dead) {
				owner.GetModPlayer<AttunedPlayer>().RegisterQualifyingHit();
			}
		}

		if (global.ShiftedSimulationId == ShiftingSimulationId.Temporal && damageDone > 0) {
			Player owner = Main.player[projectile.owner];
			if (owner.active && !owner.dead) {
				TemporalRuntime.HandleHit(
					owner,
					target,
					damageDone,
					ShiftingPrefix.ShiftingStrengthMultiplier,
					true);
			}
		}

		if (global.HasPendingDesperateSurgeFeedback && damageDone > 0 && target.active) {
			float visualMultiplier = global.ShiftedSimulationId == ShiftingSimulationId.Desperate
				? ShiftingPrefix.ShiftingStrengthMultiplier * 1.12f
				: 1f;
			WeaponPrefixItemHitDispatch.SpawnDesperateSurgeImpactEffect(target.Center, projectile.velocity, visualMultiplier);
			global.HasPendingDesperateSurgeFeedback = false;
		}

		global.pendingVampiricPreyState = VampiricPreyState.None;

		if (damageDone > 0 && target.active && projectile.owner == Main.myPlayer) {
			if (global.IsEmpoweredAttunedProjectile) {
				WeaponPrefixProjectileVisualDispatch.SpawnAttunedImpactEffect(target.Center, projectile.velocity, false, 1f);
			}

			if (global.ShiftedSimulationId == ShiftingSimulationId.Attuned && global.IsEmpoweredShiftedAttunedProjectile) {
				WeaponPrefixProjectileVisualDispatch.SpawnAttunedImpactEffect(target.Center, projectile.velocity, true, ShiftingPrefix.ShiftingStrengthMultiplier);
			}
		}

		if (damageDone > 0 && target.active) {
			if (global.IsDeadeyeProjectile) {
				DeadeyeRuntime.HandleProjectileHit(projectile, target, hit, damageDone, 1f);
			}

			if (global.IsShiftedDeadeyeProjectile) {
				DeadeyeRuntime.HandleProjectileHit(projectile, target, hit, damageDone, ShiftingPrefix.ShiftingStrengthMultiplier, true);
			}
		}
	}

	internal static void ModifyHitNPC(WeaponPrefixGlobalProjectile global, Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
	{
		if (global.IsTemporalProjectile && projectile.owner >= 0 && projectile.owner < Main.maxPlayers) {
			Player owner = Main.player[projectile.owner];
			if (owner.active && !owner.dead) {
				TemporalRuntime.PrepareFracturedHit(owner, target, ref modifiers, 1f);
			}
		}

		if (global.ShiftedSimulationId == ShiftingSimulationId.Temporal && projectile.owner >= 0 && projectile.owner < Main.maxPlayers) {
			Player owner = Main.player[projectile.owner];
			if (owner.active && !owner.dead) {
				TemporalRuntime.PrepareFracturedHit(owner, target, ref modifiers, ShiftingPrefix.ShiftingStrengthMultiplier);
			}
		}

		FracturedRuntime.ApplyDamageModifier(global, ref modifiers);

		if (global.IsVampiricProjectile) {
			global.pendingVampiricPreyState = SanguinePrefix.GetPreyState(target);
			VampiricRuntime.TryApplyProjectileFrenzyCrit(projectile, global.pendingVampiricPreyState, ref modifiers, 1f);
		}
		else if (global.ShiftedSimulationId == ShiftingSimulationId.Vampiric) {
			global.pendingVampiricPreyState = SanguinePrefix.GetPreyState(target);
			VampiricRuntime.TryApplyProjectileFrenzyCrit(projectile, global.pendingVampiricPreyState, ref modifiers, ShiftingPrefix.ShiftingStrengthMultiplier);
		}
		else {
			global.pendingVampiricPreyState = VampiricPreyState.None;
		}

		if (global.ShiftedSimulationId == ShiftingSimulationId.Breaching) {
			modifiers.ArmorPenetration += ShiftingSimulationStatScaling.GetShiftedArmorPenetrationBonus(global.ShiftedSimulationId);
		}

		if (global.IsDesperateProjectile) {
			TryApplyDesperateSurge(global, projectile, ref modifiers, 1f);
		}

		if (global.IsSkirmishFollowUpProjectile) {
			modifiers.CritDamage += SkirmishingPrefix.FollowUpCritDamageBonus;
		}

		if (global.ShiftedSimulationId == ShiftingSimulationId.Desperate) {
			TryApplyDesperateSurge(global, projectile, ref modifiers, ShiftingPrefix.ShiftingStrengthMultiplier, true);
		}

		if (global.ShiftedSimulationId == ShiftingSimulationId.Skirmishing && global.IsShiftedSkirmishFollowUpProjectile) {
			modifiers.CritDamage += ShiftingSimulationEffectScaling.GetShiftedSkirmishingFollowUpCritDamageBonus();
		}

		CatalyticRuntime.PrepareHit(global, projectile, target, ref modifiers);

		if (global.IsDeadeyeProjectile) {
			DeadeyeRuntime.TryApplyProjectileCrit(projectile, ref modifiers, 1f);
			modifiers.CritDamage += DeadeyePrefix.DeadeyeShotCritDamageBonus;
		}

		SpinboundRuntime.ApplyLiveHitBonus(global, projectile, ref modifiers);
		SpinboundRuntime.ApplyShiftedHitBonus(global, projectile, ref modifiers);

		if (!global.IsShiftedDeadeyeProjectile) {
			return;
		}

		DeadeyeRuntime.TryApplyProjectileCrit(projectile, ref modifiers, ShiftingPrefix.ShiftingStrengthMultiplier, true);
		modifiers.CritDamage += ShiftingSimulationEffectScaling.GetShiftedDeadeyeCritDamageBonus();
	}

	internal static void ModifyHitPlayer(WeaponPrefixGlobalProjectile global, ref Player.HurtModifiers modifiers)
	{
		FracturedRuntime.ApplyPlayerDamageModifier(global, ref modifiers);
	}

	private static void TryApplyDesperateSurge(WeaponPrefixGlobalProjectile global, Projectile projectile, ref NPC.HitModifiers modifiers, float visualMultiplier, bool shifted = false)
	{
		if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers) {
			return;
		}

		Player owner = Main.player[projectile.owner];
		if (!owner.active || owner.dead) {
			return;
		}

		DesperatePlayer desperatePlayer = owner.GetModPlayer<DesperatePlayer>();
		if (!desperatePlayer.TryConsumeSurge()) {
			return;
		}

		float surgeDamageBonus = shifted
			? ShiftingSimulationEffectScaling.GetShiftedDesperateSurgeDamageBonus()
			: DesperatePrefix.SurgeDamageBonus;
		modifiers.SourceDamage *= 1f + surgeDamageBonus;
		global.HasPendingDesperateSurgeFeedback = true;
	}
}

internal static class WeaponPrefixProjectileVisualDispatch
{
	internal static void AI(WeaponPrefixGlobalProjectile global, Projectile projectile)
	{
		if (!global.IsSpawnedFracturedProjectile) {
			FracturedRuntime.ProcessPending(global, projectile);
		}

		if (global.IsVampiricProjectile || global.ShiftedSimulationId == ShiftingSimulationId.Vampiric) {
			VampiricRuntime.UpdateProjectileVisual(
				projectile,
				global.VampiricManaRestoreAmount > 0 || global.ShiftedVampiricManaRestoreAmount > 0,
				global.ShiftedSimulationId == ShiftingSimulationId.None ? 1f : ShiftingPrefix.ShiftingStrengthMultiplier);
		}

		if (global.IsRadiantProjectile) {
			UpdateRadiantProjectileVisual(projectile, 1f);
		}

		if (global.IsTemporalProjectile) {
			TemporalRuntime.UpdateProjectileVisual(projectile, 1f);
		}

		if (global.IsSkirmishFollowUpProjectile) {
			UpdateSkirmishingProjectileVisual(projectile, 1f);
		}

		if (global.IsAttunedProjectile) {
			UpdateAttunedProjectileVisual(projectile, global.IsEmpoweredAttunedProjectile, 1f);
		}

		if (global.IsDeadeyeProjectile) {
			DeadeyeRuntime.UpdateProjectileVisual(projectile, 1f);
		}

		if (global.ShiftedSimulationId == ShiftingSimulationId.Radiant) {
			UpdateRadiantProjectileVisual(projectile, ShiftingSimulationEffectScaling.GetShiftedRadiantLightMultiplier());
		}

		if (global.ShiftedSimulationId == ShiftingSimulationId.Temporal) {
			TemporalRuntime.UpdateProjectileVisual(projectile, ShiftingPrefix.ShiftingStrengthMultiplier);
		}

		if (global.ShiftedSimulationId == ShiftingSimulationId.Skirmishing && global.IsShiftedSkirmishFollowUpProjectile) {
			UpdateSkirmishingProjectileVisual(projectile, ShiftingPrefix.ShiftingStrengthMultiplier);
		}

		if (global.ShiftedSimulationId == ShiftingSimulationId.Attuned) {
			UpdateAttunedProjectileVisual(
				projectile,
				global.IsEmpoweredShiftedAttunedProjectile,
				ShiftingPrefix.ShiftingStrengthMultiplier);
		}

		if (global.IsShiftedDeadeyeProjectile) {
			DeadeyeRuntime.UpdateProjectileVisual(projectile, ShiftingPrefix.ShiftingStrengthMultiplier);
		}

		if (global.IsDesperateProjectile || global.ShiftedSimulationId == ShiftingSimulationId.Desperate) {
			UpdateDesperateProjectileVisual(
				projectile,
				global.IsDesperateProjectile ? 1f : ShiftingPrefix.ShiftingStrengthMultiplier);
		}

		if (global.IsSpinboundProjectile) {
			SpinboundRuntime.UpdateProjectileVisual(global, projectile, false);
		}

		if (global.IsShiftedSpinboundProjectile) {
			SpinboundRuntime.UpdateProjectileVisual(global, projectile, true);
		}

		FracturedRuntime.UpdateVisual(global, projectile);
	}

	internal static Color? GetAlpha(WeaponPrefixGlobalProjectile global)
	{
		return FracturedRuntime.GetAlpha(global);
	}

	internal static bool PreDraw(WeaponPrefixGlobalProjectile global, Projectile projectile, ref Color lightColor)
	{
		if (!FracturedRuntime.PreDraw(global, projectile, ref lightColor)) {
			return false;
		}

		if (global.IsTemporalProjectile || global.ShiftedSimulationId == ShiftingSimulationId.Temporal) {
			Texture2D texture = TextureAssets.Projectile[projectile.type].Value;
			Rectangle frame = texture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
			Vector2 origin = frame.Size() * 0.5f;
			Vector2 basePosition = FracturedRuntime.GetProjectileDrawPosition(projectile);
			float strength = global.ShiftedSimulationId == ShiftingSimulationId.Temporal ? ShiftingPrefix.ShiftingStrengthMultiplier : 1f;
			float pulse = WeaponPrefixVisuals.GetTemporalPulse(projectile.identity * 0.19f) * strength;
			Color underlayTint = WeaponPrefixVisuals.TemporalAfterimageTint * (0.26f + 0.05f * System.MathF.Min(strength, 1.5f));

			FracturedRuntime.DrawAfterimage(
				projectile,
				texture,
				frame,
				origin,
				basePosition - projectile.velocity * (0.08f + 0.02f * strength),
				underlayTint,
				projectile.rotation,
				projectile.scale * (1f + 0.015f * pulse));

			lightColor = Color.Lerp(lightColor, WeaponPrefixVisuals.TemporalTint, 0.18f * System.MathF.Min(strength, 1.25f));
		}

		if (global.IsSkirmishFollowUpProjectile || (global.ShiftedSimulationId == ShiftingSimulationId.Skirmishing && global.IsShiftedSkirmishFollowUpProjectile)) {
			Texture2D texture = TextureAssets.Projectile[projectile.type].Value;
			Rectangle frame = texture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
			Vector2 origin = frame.Size() * 0.5f;
			Vector2 basePosition = FracturedRuntime.GetProjectileDrawPosition(projectile);
			float strength = global.ShiftedSimulationId == ShiftingSimulationId.Skirmishing && global.IsShiftedSkirmishFollowUpProjectile
				? ShiftingPrefix.ShiftingStrengthMultiplier
				: 1f;
			Color underlayTint = WeaponPrefixVisuals.SkirmishingTracerTint * (0.28f + 0.04f * System.MathF.Min(strength, 1.5f));

			FracturedRuntime.DrawAfterimage(
				projectile,
				texture,
				frame,
				origin,
				basePosition - projectile.velocity * (0.11f + 0.02f * strength),
				underlayTint,
				projectile.rotation,
				projectile.scale * (1f + 0.01f * strength));

			lightColor = Color.Lerp(lightColor, WeaponPrefixVisuals.GetSkirmishingColor(1f, true), 0.16f * System.MathF.Min(strength, 1.2f));
		}

		if (global.IsDeadeyeProjectile || global.IsShiftedDeadeyeProjectile) {
			Texture2D texture = TextureAssets.Projectile[projectile.type].Value;
			Rectangle frame = texture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
			Vector2 origin = frame.Size() * 0.5f;
			Vector2 basePosition = FracturedRuntime.GetProjectileDrawPosition(projectile);
			float strength = global.IsShiftedDeadeyeProjectile ? ShiftingPrefix.ShiftingStrengthMultiplier : 1f;
			Color underlayTint = WeaponPrefixVisuals.DeadeyeAfterimageTint * (0.32f + 0.05f * System.MathF.Min(strength, 1.5f));

			FracturedRuntime.DrawAfterimage(
				projectile,
				texture,
				frame,
				origin,
				basePosition - projectile.velocity * (0.12f + 0.02f * strength),
				underlayTint,
				projectile.rotation,
				projectile.scale * (1f + 0.012f * strength));

			lightColor = Color.Lerp(lightColor, WeaponPrefixVisuals.DeadeyeShotColor, 0.18f * System.MathF.Min(strength, 1.25f));
		}

		return true;
	}

	internal static void SpawnAttunedImpactEffect(Vector2 position, Vector2 projectileVelocity, bool shifted, float visualMultiplier)
	{
		Vector2 direction = projectileVelocity.SafeNormalize(Vector2.UnitX);
		Vector2 tangent = direction.RotatedBy(MathHelper.PiOver2);
		Color impactColor = WeaponPrefixVisuals.GetAttunedColor(1f, true);
		float strength = shifted ? 0.28f : 0.22f;
		Lighting.AddLight(position, impactColor.ToVector3() * (strength * visualMultiplier));

		for (int i = 0; i < 5; i++) {
			float side = i % 2 == 0 ? -1f : 1f;
			float lane = 1f + i / 2f;
			Dust dust = Dust.NewDustPerfect(
				position + tangent * (2.5f * lane * side),
				WeaponPrefixVisuals.AttunedDustType,
				direction * (0.7f + 0.08f * lane) + tangent * (0.18f * lane * side),
				0,
				impactColor,
				0.86f * visualMultiplier);
			dust.noGravity = true;
			dust.fadeIn = 0.96f;
		}
	}

	private static void UpdateRadiantProjectileVisual(Projectile projectile, float intensityMultiplier)
	{
		float pulse = WeaponPrefixVisuals.GetRadiantPulse(projectile.identity * 0.21f) * intensityMultiplier;
		Lighting.AddLight(projectile.Center, 1f * pulse, 0.74f * pulse, 0.24f * pulse);

		if (projectile.numUpdates == 0 && projectile.timeLeft % 5 == 0) {
			Vector2 offset = Main.rand.NextVector2Circular(6f, 6f);
			Dust dust = Dust.NewDustPerfect(projectile.Center + offset, WeaponPrefixVisuals.RadiantDustType);
			dust.noGravity = true;
			dust.velocity = projectile.velocity * 0.05f + new Vector2(0f, -0.35f);
			dust.scale = 0.8f * intensityMultiplier;
			dust.fadeIn = 1f;
		}
	}

	private static void UpdateSkirmishingProjectileVisual(Projectile projectile, float intensityMultiplier)
	{
		float pulse = WeaponPrefixVisuals.GetSkirmishingPulse(projectile.identity * 0.13f) * intensityMultiplier;
		Color tracerColor = WeaponPrefixVisuals.GetSkirmishingColor(1f, true);
		Lighting.AddLight(projectile.Center, tracerColor.ToVector3() * (0.12f * pulse));

		if (projectile.owner != Main.myPlayer
			|| projectile.numUpdates != 0
			|| projectile.timeLeft % 6 != 0) {
			return;
		}

		Vector2 velocityDirection = projectile.velocity.SafeNormalize(Vector2.UnitX * projectile.direction);
		Vector2 tangent = velocityDirection.RotatedBy(MathHelper.PiOver2);
		Vector2 offset = tangent * Main.rand.NextFloat(-4f, 4f);
		Dust dust = Dust.NewDustPerfect(
			projectile.Center + offset,
			WeaponPrefixVisuals.SkirmishingDustType,
			projectile.velocity * -0.045f + tangent * Main.rand.NextFloat(-0.12f, 0.12f),
			0,
			tracerColor,
			0.78f * intensityMultiplier);
		dust.noGravity = true;
		dust.fadeIn = 0.92f;
	}

	private static void UpdateAttunedProjectileVisual(Projectile projectile, bool empowered, float intensityMultiplier)
	{
		float pulse = WeaponPrefixVisuals.GetAttunedPulse(projectile.identity * 0.17f) * intensityMultiplier;
		Color attunedColor = WeaponPrefixVisuals.GetAttunedColor(empowered ? 1f : 0.65f, empowered);
		float lightStrength = empowered ? 0.22f : 0.12f;
		Lighting.AddLight(projectile.Center, attunedColor.ToVector3() * (lightStrength * pulse));

		int dustInterval = empowered ? 5 : 8;
		if (projectile.owner != Main.myPlayer
			|| projectile.numUpdates != 0
			|| projectile.timeLeft % dustInterval != 0) {
			return;
		}

		Vector2 lateralOffset = projectile.velocity.SafeNormalize(Vector2.UnitX * projectile.direction)
			.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-4f, 4f);
		Dust dust = Dust.NewDustPerfect(projectile.Center + lateralOffset, WeaponPrefixVisuals.AttunedDustType);
		dust.noGravity = true;
		dust.velocity = projectile.velocity * -0.035f + Main.rand.NextVector2Circular(0.1f, 0.1f);
		dust.scale = (empowered ? 0.96f : 0.72f) * intensityMultiplier;
		dust.fadeIn = empowered ? 1.05f : 0.92f;
	}

	private static void UpdateDesperateProjectileVisual(Projectile projectile, float intensityMultiplier)
	{
		if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers) {
			return;
		}

		Player owner = Main.player[projectile.owner];
		if (!owner.active || owner.dead) {
			return;
		}

		DesperatePlayer desperatePlayer = owner.GetModPlayer<DesperatePlayer>();
		DesperateThresholdState state = desperatePlayer.CurrentState;
		if (state == DesperateThresholdState.None) {
			return;
		}

		float pulse = WeaponPrefixVisuals.GetDesperatePulse(projectile.identity * 0.23f) * intensityMultiplier;
		float surgeReadyStrength = state == DesperateThresholdState.Critical
			? 0.18f + 0.2f * desperatePlayer.GetSurgeCooldownProgress()
			: 0f;
		Color stateColor = WeaponPrefixVisuals.GetDesperateColor(state);
		Lighting.AddLight(projectile.Center, stateColor.ToVector3() * ((0.08f + surgeReadyStrength) * pulse));

		if (projectile.owner != Main.myPlayer
			|| projectile.numUpdates != 0
			|| projectile.timeLeft % (state == DesperateThresholdState.Critical ? 6 : state == DesperateThresholdState.Severe ? 8 : 12) != 0) {
			return;
		}

		Dust dust = Dust.NewDustPerfect(
			projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
			WeaponPrefixVisuals.DesperateDustType);
		dust.noGravity = true;
		dust.velocity = projectile.velocity * -0.04f + Main.rand.NextVector2Circular(0.14f, 0.14f);
		dust.scale = (0.62f + 0.08f * (int)state + surgeReadyStrength * 0.2f) * intensityMultiplier;
		dust.fadeIn = 0.92f;
	}
}

internal static class WeaponPrefixProjectileSyncDispatch
{
	internal static void SendExtraAI(WeaponPrefixGlobalProjectile global, BitWriter bitWriter, BinaryWriter binaryWriter)
	{
		bitWriter.WriteBit(global.IsFracturedDamageProjectile);
		bitWriter.WriteBit(global.IsFracturedVisualProjectile);
		bitWriter.WriteBit(global.IsSpawnedFracturedProjectile);
		bitWriter.WriteBit(global.IsVampiricProjectile);
		bitWriter.WriteBit(global.IsRadiantProjectile);
		bitWriter.WriteBit(global.IsBreachingProjectile);
		bitWriter.WriteBit(global.IsDesperateProjectile);
		bitWriter.WriteBit(global.IsTemporalProjectile);
		bitWriter.WriteBit(global.IsSkirmishingProjectile);
		bitWriter.WriteBit(global.IsSkirmishFollowUpProjectile);
		bitWriter.WriteBit(global.IsAttunedProjectile);
		bitWriter.WriteBit(global.IsEmpoweredAttunedProjectile);
		bitWriter.WriteBit(global.IsStormforgedProjectile);
		bitWriter.WriteBit(global.IsCatalyticProjectile);
		bitWriter.WriteBit(global.IsDeadeyeProjectile);
		bitWriter.WriteBit(global.IsSpinboundProjectile);
		bitWriter.WriteBit(global.HasConsumedSpinboundHitBonus);
		bitWriter.WriteBit(global.IsShiftedSpinboundProjectile);
		bitWriter.WriteBit(global.HasConsumedShiftedSpinboundHitBonus);
		bitWriter.WriteBit(global.IsShiftedSkirmishFollowUpProjectile);
		bitWriter.WriteBit(global.IsEmpoweredShiftedAttunedProjectile);
		bitWriter.WriteBit(global.IsShiftedDeadeyeProjectile);
		binaryWriter.Write(global.FractureTier);
		binaryWriter.Write(global.FracturedDamageMultiplier);
		binaryWriter.Write(global.VampiricManaRestoreAmount);
		binaryWriter.Write(global.BreachImpactScale);
		binaryWriter.Write(global.SpinboundReferenceSpeed);
		binaryWriter.Write(global.GoldenSpinTier);
		binaryWriter.Write((byte)global.ShiftedSimulationId);
		binaryWriter.Write(global.ShiftedStrengthMultiplier);
		binaryWriter.Write(global.ShiftedVampiricManaRestoreAmount);
		binaryWriter.Write(global.ShiftedSpinboundReferenceSpeed);
		binaryWriter.Write(global.ShiftedGoldenSpinTier);
	}

	internal static void ReceiveExtraAI(WeaponPrefixGlobalProjectile global, BitReader bitReader, BinaryReader binaryReader)
	{
		global.IsFracturedDamageProjectile = bitReader.ReadBit();
		global.IsFracturedVisualProjectile = bitReader.ReadBit();
		global.IsSpawnedFracturedProjectile = bitReader.ReadBit();
		global.IsVampiricProjectile = bitReader.ReadBit();
		global.IsRadiantProjectile = bitReader.ReadBit();
		global.IsBreachingProjectile = bitReader.ReadBit();
		global.IsDesperateProjectile = bitReader.ReadBit();
		global.IsTemporalProjectile = bitReader.ReadBit();
		global.IsSkirmishingProjectile = bitReader.ReadBit();
		global.IsSkirmishFollowUpProjectile = bitReader.ReadBit();
		global.IsAttunedProjectile = bitReader.ReadBit();
		global.IsEmpoweredAttunedProjectile = bitReader.ReadBit();
		global.IsStormforgedProjectile = bitReader.ReadBit();
		global.IsCatalyticProjectile = bitReader.ReadBit();
		global.IsDeadeyeProjectile = bitReader.ReadBit();
		global.IsSpinboundProjectile = bitReader.ReadBit();
		global.HasConsumedSpinboundHitBonus = bitReader.ReadBit();
		global.IsShiftedSpinboundProjectile = bitReader.ReadBit();
		global.HasConsumedShiftedSpinboundHitBonus = bitReader.ReadBit();
		global.IsShiftedSkirmishFollowUpProjectile = bitReader.ReadBit();
		global.IsEmpoweredShiftedAttunedProjectile = bitReader.ReadBit();
		global.IsShiftedDeadeyeProjectile = bitReader.ReadBit();
		global.FractureTier = binaryReader.ReadByte();
		global.FracturedDamageMultiplier = binaryReader.ReadSingle();
		global.VampiricManaRestoreAmount = binaryReader.ReadInt32();
		global.BreachImpactScale = binaryReader.ReadSingle();
		global.SpinboundReferenceSpeed = binaryReader.ReadSingle();
		global.GoldenSpinTier = binaryReader.ReadInt32();
		global.ShiftedSimulationId = (ShiftingSimulationId)binaryReader.ReadByte();
		global.ShiftedStrengthMultiplier = binaryReader.ReadSingle();
		global.ShiftedVampiricManaRestoreAmount = binaryReader.ReadInt32();
		global.ShiftedSpinboundReferenceSpeed = binaryReader.ReadSingle();
		global.ShiftedGoldenSpinTier = binaryReader.ReadInt32();
	}

	internal static void TryInitializeShiftedState(WeaponPrefixGlobalProjectile global, Projectile projectile, IEntitySource source)
	{
		if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers) {
			return;
		}

		Player owner = Main.player[projectile.owner];
		if (source is IEntitySource_WithStatsFromItem itemSource) {
			Item item = itemSource.Item;
			if (item == null
				|| item.IsAir
				|| !WeaponPrefixIdentity.HasShiftingPrefix(item)
				|| !owner.active
				|| owner.dead
				|| !owner.GetModPlayer<ShiftingPlayer>().TryGetActiveSimulation(item, out ShiftingSimulationId simulationId)) {
				return;
			}

			global.ShiftedSimulationId = simulationId;
			global.ShiftedStrengthMultiplier = ShiftingPrefix.ShiftingStrengthMultiplier;
			InitializeShiftedSimulationSnapshots(global, projectile, item);
			return;
		}

		if (source is EntitySource_Parent parentSource && parentSource.Entity is Projectile parentProjectile) {
			WeaponPrefixGlobalProjectile parentGlobal = parentProjectile.GetGlobalProjectile<WeaponPrefixGlobalProjectile>();
			if (parentGlobal.ShiftedSimulationId == ShiftingSimulationId.None
				|| !CanInheritShiftedSimulation(parentGlobal.ShiftedSimulationId)) {
				return;
			}

			global.ShiftedSimulationId = parentGlobal.ShiftedSimulationId;
			global.ShiftedStrengthMultiplier = parentGlobal.ShiftedStrengthMultiplier;
			InitializeShiftedSimulationSnapshots(global, projectile, null, parentGlobal);
		}
	}

	private static void InitializeShiftedSimulationSnapshots(WeaponPrefixGlobalProjectile global, Projectile projectile, Item item, WeaponPrefixGlobalProjectile parentGlobal = null)
	{
		switch (global.ShiftedSimulationId) {
			case ShiftingSimulationId.Fractured:
			case ShiftingSimulationId.Deadeye:
				break;
			case ShiftingSimulationId.Skirmishing:
				if (item != null) {
					Player owner = Main.player[projectile.owner];
					if (owner.active && !owner.dead) {
						global.IsShiftedSkirmishFollowUpProjectile = owner.GetModPlayer<SkirmishingPlayer>().IsFollowUpProjectileWindowActive();
					}
				}
				break;
			case ShiftingSimulationId.Attuned:
				if (item != null) {
					Player owner = Main.player[projectile.owner];
					if (owner.active && !owner.dead) {
						global.IsEmpoweredShiftedAttunedProjectile = owner.GetModPlayer<AttunedPlayer>().IsEmpoweredProjectileWindowActive();
					}
				}
				break;
			case ShiftingSimulationId.Vampiric:
				global.ShiftedVampiricManaRestoreAmount = item != null && WeaponPrefix.IsMagicWeapon(item)
					? VampiricRuntime.GetManaRestoreAmount(
						item,
						ShiftingSimulationEffectScaling.GetShiftedVampiricManaRestoreRatio(),
						ShiftingSimulationEffectScaling.GetShiftedVampiricMaxRestorePerHit())
					: parentGlobal?.ShiftedVampiricManaRestoreAmount ?? 0;
				break;
			case ShiftingSimulationId.Spinbound:
				if (item != null) {
					SpinboundRuntime.TryInitializeShiftedProjectile(global, projectile, item);
				}
				break;
		}
	}

	private static bool CanInheritShiftedSimulation(ShiftingSimulationId simulationId)
	{
		return simulationId is ShiftingSimulationId.Vampiric
			or ShiftingSimulationId.Radiant
			or ShiftingSimulationId.Stormforged;
	}
}
