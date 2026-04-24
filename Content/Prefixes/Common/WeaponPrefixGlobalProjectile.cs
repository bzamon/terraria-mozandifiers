using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mozandifiers.Common.Combat;
using Mozandifiers.Common.Config;
using Mozandifiers.Common.Players;
using Mozandifiers.Content.Prefixes.Weapons;
using System.IO;
using System.Text;
using Terraria.Audio;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Mozandifiers.Content.Prefixes.Common;

public sealed class WeaponPrefixGlobalProjectile : GlobalProjectile
{
	public override bool InstancePerEntity => true;

	public bool IsEchoDamageProjectile { get; set; }
	public bool IsEchoVisualProjectile { get; set; }
	public bool IsSpawnedEchoProjectile { get; set; }
	public byte EchoFractureTier { get; set; }
	public bool IsSanguineProjectile { get; set; }
	public int VampiricManaRestoreAmount { get; set; }
	public bool IsRadiantProjectile { get; set; }
	public bool IsBreachingProjectile { get; set; }
	public bool IsDesperateProjectile { get; set; }
	public bool IsTemporalProjectile { get; set; }
	public bool IsSkirmishingProjectile { get; set; }
	public bool IsSkirmishFollowUpProjectile { get; set; }
	public bool IsAttunedProjectile { get; set; }
	public bool IsEmpoweredAttunedProjectile { get; set; }
	public bool IsStormforgedProjectile { get; set; }
	public bool IsCatalyticProjectile { get; set; }
	public bool IsDeadeyeProjectile { get; set; }
	public bool IsSpinboundProjectile { get; set; }
	public float BreachImpactScale { get; set; }
	public float SpinboundReferenceSpeed { get; set; }
	public int GoldenSpinTier { get; set; }
	public bool HasConsumedSpinboundHitBonus { get; set; }
	public float EchoDamageMultiplier { get; set; }
	internal ShiftingSimulationId ShiftedSimulationId { get; set; }
	internal float ShiftedStrengthMultiplier { get; set; }
	internal int ShiftedVampiricManaRestoreAmount { get; set; }
	internal bool IsShiftedSpinboundProjectile { get; set; }
	internal float ShiftedSpinboundReferenceSpeed { get; set; }
	internal int ShiftedGoldenSpinTier { get; set; }
	internal bool HasConsumedShiftedSpinboundHitBonus { get; set; }
	internal bool IsShiftedSkirmishFollowUpProjectile { get; set; }
	internal bool IsEmpoweredShiftedAttunedProjectile { get; set; }
	internal bool IsShiftedDeadeyeProjectile { get; set; }
	internal bool HasPendingDesperateSurgeFeedback { get; set; }

	private bool pendingCatalyticConsume;
	private VampiricPreyState pendingVampiricPreyState;
	private int pendingFractureIIFrames;
	private int pendingFractureIIIFrames;
	private bool hasPendingFractureII;
	private bool hasPendingFractureIII;
	private Vector2 fractureSpawnPosition;
	private Vector2 fractureBaseVelocity;
	private bool fractureUsesLocalNPCImmunity;
	private int fractureLocalNPCHitCooldown;
	private bool fractureUsesIDStaticNPCImmunity;
	private int fractureIDStaticNPCHitCooldown;

	[System.Flags]
	private enum GoldenSpinMatchFlags : byte
	{
		None = 0,
		VelocityVector = 1 << 0,
		AngleGolden = 1 << 1,
		SpeedPrecision = 1 << 2,
		AnglePrecision = 1 << 3,
		PrecisionRelease = 1 << 4
	}

	private readonly struct GoldenSpinEvaluation
	{
		public GoldenSpinMatchFlags Matches { get; init; }
		public int Tier { get; init; }
		public float PlayerSpeed { get; init; }
		public Vector2 Velocity { get; init; }
		public float VelocityVectorRatio { get; init; }
		public float FoldedAngleDegrees { get; init; }
		public float Speed { get; init; }
		public float SpeedRatio { get; init; }
	}

	public override void OnSpawn(Projectile projectile, IEntitySource source)
	{
		TryInitializeShiftingState(projectile, source);

		if (IsSpawnedEchoProjectile || projectile.owner != Main.myPlayer) {
			return;
		}

		Item item = null;
		if (source is IEntitySource_WithStatsFromItem itemSource) {
			item = itemSource.Item;
			if (item == null || item.IsAir) {
				return;
			}

			if (item.prefix == ModContent.PrefixType<SanguinePrefix>()) {
				IsSanguineProjectile = true;
				if (WeaponPrefix.IsMagicWeapon(item)) {
					VampiricManaRestoreAmount = WeaponPrefixGlobalItem.GetVampiricManaRestoreAmount(item);
				}
			}

			if (item.prefix == ModContent.PrefixType<RadiantPrefix>()) {
				IsRadiantProjectile = true;
			}

			if (item.prefix == ModContent.PrefixType<BreachingPrefix>()) {
				IsBreachingProjectile = true;
				BreachImpactScale = BreachingPrefix.GetBreachImpactMultiplier(item);
			}

			if (item.prefix == ModContent.PrefixType<DesperatePrefix>()) {
				IsDesperateProjectile = true;
			}

			if (item.prefix == ModContent.PrefixType<TemporalPrefix>()) {
				IsTemporalProjectile = true;
			}

			if (item.prefix == ModContent.PrefixType<SkirmishingPrefix>()) {
				IsSkirmishingProjectile = true;
				Player owner = Main.player[projectile.owner];
				if (owner.active && !owner.dead) {
					IsSkirmishFollowUpProjectile = owner.GetModPlayer<SkirmishingPlayer>().IsFollowUpProjectileWindowActive();
				}
			}

			if (item.prefix == ModContent.PrefixType<AttunedPrefix>()) {
				IsAttunedProjectile = true;
				Player owner = Main.player[projectile.owner];
				if (owner.active && !owner.dead) {
					IsEmpoweredAttunedProjectile = owner.GetModPlayer<AttunedPlayer>().IsEmpoweredProjectileWindowActive();
				}
			}

			if (item.prefix == ModContent.PrefixType<StormforgedPrefix>()) {
				IsStormforgedProjectile = true;
			}

			if (item.prefix == ModContent.PrefixType<CatalyticPrefix>()) {
				IsCatalyticProjectile = true;
			}

			if (item.prefix == ModContent.PrefixType<DeadeyePrefix>()) {
				Player owner = Main.player[projectile.owner];
				if (owner.active && !owner.dead && owner.GetModPlayer<DeadeyePlayer>().TryConsumeReadyShot()) {
					IsDeadeyeProjectile = true;
					projectile.velocity *= DeadeyePrefix.DeadeyeShotVelocityMultiplier;
				}
			}

			if (item.prefix == ModContent.PrefixType<SpinboundPrefix>()) {
				Player owner = Main.player[projectile.owner];
				Vector2 snapshotVelocity = projectile.velocity;
				bool triggered;
				owner.GetModPlayer<SpinboundPlayer>().TryConsumeSpinboundShotContext(item, false, out snapshotVelocity, out triggered);
				if (triggered) {
					IsSpinboundProjectile = true;
					SpinboundReferenceSpeed = snapshotVelocity.Length();
					GoldenSpinEvaluation goldenSpin = EvaluateGoldenSpin(owner, snapshotVelocity);
					GoldenSpinTier = goldenSpin.Tier;
					PlaySpinboundSpawnSounds(projectile, GoldenSpinTier);
					SpawnSpinboundReleaseEffect(projectile, GoldenSpinTier);
					ShowSpinboundCombatText(projectile, goldenSpin);
				}
			}

			if (TryGetShiftedSimulation(item, projectile.owner, out ShiftingSimulationId shiftedSimulationId)) {
				if (shiftedSimulationId == ShiftingSimulationId.Breaching) {
					BreachImpactScale = BreachingPrefix.GetBreachImpactMultiplier(item) * ShiftingPrefix.ShiftingStrengthMultiplier;
				}

				if (shiftedSimulationId == ShiftingSimulationId.Echoing) {
					TryScheduleFractureCascade(
						projectile,
						ShiftingSimulationCatalog.GetShiftedFractureIChance(),
						ShiftingSimulationCatalog.GetShiftedFractureIIChance(),
						ShiftingSimulationCatalog.GetShiftedFractureIIIChance());
				}

				if (shiftedSimulationId == ShiftingSimulationId.Deadeye) {
					Player owner = Main.player[projectile.owner];
					if (owner.active && !owner.dead && owner.GetModPlayer<DeadeyePlayer>().TryConsumeReadyShot()) {
						IsShiftedDeadeyeProjectile = true;
						projectile.velocity *= ShiftingSimulationCatalog.GetShiftedDeadeyeVelocityMultiplier();
					}
				}
			}
		}
		else if (source is EntitySource_Parent parentSource && parentSource.Entity is Projectile parentProjectile) {
			WeaponPrefixGlobalProjectile parentGlobal = parentProjectile.GetGlobalProjectile<WeaponPrefixGlobalProjectile>();
			if (parentGlobal.IsSanguineProjectile) {
				IsSanguineProjectile = true;
				VampiricManaRestoreAmount = parentGlobal.VampiricManaRestoreAmount;
			}

			if (parentGlobal.IsRadiantProjectile) {
				IsRadiantProjectile = true;
			}

			if (parentGlobal.IsStormforgedProjectile) {
				IsStormforgedProjectile = true;
			}

		}
		else {
			return;
		}

		if (item == null || item.prefix != ModContent.PrefixType<EchoingPrefix>()) {
			return;
		}

		TryScheduleFractureCascade(
			projectile,
			PrefixTuningConfig.Instance.FractureIChance,
			PrefixTuningConfig.Instance.FractureIIChance,
			PrefixTuningConfig.Instance.FractureIIIChance);
	}

	public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (ShiftedSimulationId != ShiftingSimulationId.None && damageDone > 0) {
			Player owner = Main.player[projectile.owner];
			if (owner.active && !owner.dead) {
				owner.GetModPlayer<ShiftingPlayer>().RegisterSuccessfulHit(owner.HeldItem);
			}
		}

		bool shiftedCatalytic = ShiftedSimulationId == ShiftingSimulationId.Catalytic;
		if (damageDone > 0 && (IsCatalyticProjectile || shiftedCatalytic)) {
			HandleCatalyticHit(
				projectile,
				target,
				shiftedCatalytic,
				shiftedCatalytic ? ShiftingSimulationCatalog.GetShiftedCatalyticMarkDurationTicks() : CatalyticPrefix.MarkDurationTicks,
				shiftedCatalytic ? ShiftingSimulationCatalog.GetShiftedCatalyticArmDelayTicks() : CatalyticPrefix.MarkArmDelayTicks);
		}
		else {
			ClearPendingCatalyticConsume();
		}

		if (IsSanguineProjectile) {
			Player owner = Main.player[projectile.owner];
			if (owner.active && !owner.dead) {
				WeaponPrefixGlobalItem.ApplyVampiricProjectileFeed(
					owner,
					target,
					damageDone,
					hit.Crit,
					pendingVampiricPreyState,
					VampiricManaRestoreAmount);
			}
		}

		if (IsRadiantProjectile && target.active) {
			WeaponPrefixGlobalItem.ApplyRadiantHitEffects(target);
		}

		if (IsBreachingProjectile) {
			Player owner = Main.player[projectile.owner];
			if (owner.active && !owner.dead) {
				WeaponPrefixGlobalItem.TryTriggerBreachingImpact(owner, target, projectile.velocity, damageDone, BreachImpactScale);
			}
		}

		if (IsSkirmishingProjectile && damageDone > 0) {
			Player owner = Main.player[projectile.owner];
			if (owner.active && !owner.dead) {
				owner.GetModPlayer<SkirmishingPlayer>().RegisterQualifyingHit();
			}
		}

		if (IsAttunedProjectile && damageDone > 0) {
			Player owner = Main.player[projectile.owner];
			if (owner.active && !owner.dead) {
				owner.GetModPlayer<AttunedPlayer>().RegisterQualifyingHit();
			}
		}

		if (IsTemporalProjectile && damageDone > 0) {
			Player owner = Main.player[projectile.owner];
			if (owner.active && !owner.dead) {
				WeaponPrefixGlobalItem.HandleTemporalHit(owner, target, damageDone, 1f);
			}
		}

		if (IsStormforgedProjectile) {
			Player owner = Main.player[projectile.owner];
			if (owner.active && !owner.dead) {
				StormforgedChainHelper.TryTriggerChain(owner, target, damageDone);
			}
		}

		if (ShiftedSimulationId == ShiftingSimulationId.Stormforged) {
			Player owner = Main.player[projectile.owner];
			if (owner.active && !owner.dead) {
				StormforgedChainHelper.TryTriggerChain(
					owner,
					target,
					damageDone,
					ShiftingSimulationCatalog.GetShiftedStormforgedProcChance(),
					ShiftingSimulationCatalog.GetShiftedStormforgedMaxChainJumps(),
					ShiftingSimulationCatalog.GetShiftedStormforgedChainRangePixels(),
					ShiftingSimulationCatalog.GetShiftedStormforgedChainDamageDecay());
			}
		}

		if (ShiftedSimulationId == ShiftingSimulationId.Sanguine) {
			Player owner = Main.player[projectile.owner];
			if (owner.active && !owner.dead) {
				WeaponPrefixGlobalItem.ApplyVampiricProjectileFeed(
					owner,
					target,
					damageDone,
					hit.Crit,
					pendingVampiricPreyState,
					ShiftedVampiricManaRestoreAmount,
					ShiftingPrefix.ShiftingStrengthMultiplier,
					true);
			}
		}

		if (ShiftedSimulationId == ShiftingSimulationId.Radiant && target.active) {
			WeaponPrefixGlobalItem.ApplyRadiantHitEffects(
				target,
				ShiftingSimulationCatalog.GetShiftedRadiantDurationTicks(),
				ShiftingSimulationCatalog.GetShiftedRadiantLightMultiplier());
		}

		if (ShiftedSimulationId == ShiftingSimulationId.Breaching) {
			Player owner = Main.player[projectile.owner];
			if (owner.active && !owner.dead) {
				WeaponPrefixGlobalItem.TryTriggerBreachingImpact(
					owner,
					target,
					projectile.velocity,
					damageDone,
					BreachImpactScale > 0f ? BreachImpactScale : 1f);
			}
		}

		if (ShiftedSimulationId == ShiftingSimulationId.Skirmishing && damageDone > 0) {
			Player owner = Main.player[projectile.owner];
			if (owner.active && !owner.dead) {
				owner.GetModPlayer<SkirmishingPlayer>().RegisterQualifyingHit();
			}
		}

		if (ShiftedSimulationId == ShiftingSimulationId.Attuned && damageDone > 0) {
			Player owner = Main.player[projectile.owner];
			if (owner.active && !owner.dead) {
				owner.GetModPlayer<AttunedPlayer>().RegisterQualifyingHit();
			}
		}

		if (ShiftedSimulationId == ShiftingSimulationId.Temporal && damageDone > 0) {
			Player owner = Main.player[projectile.owner];
			if (owner.active && !owner.dead) {
				WeaponPrefixGlobalItem.HandleTemporalHit(
					owner,
					target,
					damageDone,
					ShiftingPrefix.ShiftingStrengthMultiplier,
					true);
			}
		}

		if (HasPendingDesperateSurgeFeedback && damageDone > 0 && target.active) {
			float visualMultiplier = ShiftedSimulationId == ShiftingSimulationId.Desperate
				? ShiftingPrefix.ShiftingStrengthMultiplier * 1.12f
				: 1f;
			WeaponPrefixGlobalItem.SpawnDesperateSurgeImpactEffect(target.Center, projectile.velocity, visualMultiplier);
			HasPendingDesperateSurgeFeedback = false;
		}

		pendingVampiricPreyState = VampiricPreyState.None;

		if (damageDone > 0 && target.active && projectile.owner == Main.myPlayer) {
			if (IsEmpoweredAttunedProjectile) {
				SpawnAttunedImpactEffect(target.Center, projectile.velocity, false, 1f);
			}

			if (ShiftedSimulationId == ShiftingSimulationId.Attuned && IsEmpoweredShiftedAttunedProjectile) {
				SpawnAttunedImpactEffect(target.Center, projectile.velocity, true, ShiftingPrefix.ShiftingStrengthMultiplier);
			}
		}

		if (damageDone > 0 && target.active) {
			if (IsDeadeyeProjectile) {
				HandleDeadeyeHit(projectile, target, hit, damageDone, 1f);
			}

			if (IsShiftedDeadeyeProjectile) {
				HandleDeadeyeHit(projectile, target, hit, damageDone, ShiftingPrefix.ShiftingStrengthMultiplier, true);
			}
		}
	}

	public override void AI(Projectile projectile)
	{
		if (!IsSpawnedEchoProjectile) {
			ProcessPendingFractures(projectile);
		}

		if (IsSanguineProjectile || ShiftedSimulationId == ShiftingSimulationId.Sanguine) {
			UpdateVampiricProjectileVisual(
				projectile,
				VampiricManaRestoreAmount > 0 || ShiftedVampiricManaRestoreAmount > 0,
				ShiftedSimulationId == ShiftingSimulationId.None ? 1f : ShiftingPrefix.ShiftingStrengthMultiplier);
		}

		if (IsRadiantProjectile) {
			UpdateRadiantProjectileVisual(projectile, 1f);
		}

		if (IsTemporalProjectile) {
			UpdateTemporalProjectileVisual(projectile, 1f);
		}

		if (IsSkirmishFollowUpProjectile) {
			UpdateSkirmishingProjectileVisual(projectile, 1f);
		}

		if (IsAttunedProjectile) {
			UpdateAttunedProjectileVisual(projectile, IsEmpoweredAttunedProjectile, 1f);
		}

		if (IsDeadeyeProjectile) {
			UpdateDeadeyeProjectileVisual(projectile, 1f);
		}

		if (ShiftedSimulationId == ShiftingSimulationId.Radiant) {
			UpdateRadiantProjectileVisual(projectile, ShiftingSimulationCatalog.GetShiftedRadiantLightMultiplier());
		}

		if (ShiftedSimulationId == ShiftingSimulationId.Temporal) {
			UpdateTemporalProjectileVisual(projectile, ShiftingPrefix.ShiftingStrengthMultiplier);
		}

		if (ShiftedSimulationId == ShiftingSimulationId.Skirmishing && IsShiftedSkirmishFollowUpProjectile) {
			UpdateSkirmishingProjectileVisual(projectile, ShiftingPrefix.ShiftingStrengthMultiplier);
		}

		if (ShiftedSimulationId == ShiftingSimulationId.Attuned) {
			UpdateAttunedProjectileVisual(
				projectile,
				IsEmpoweredShiftedAttunedProjectile,
				ShiftingPrefix.ShiftingStrengthMultiplier);
		}

		if (IsShiftedDeadeyeProjectile) {
			UpdateDeadeyeProjectileVisual(projectile, ShiftingPrefix.ShiftingStrengthMultiplier);
		}

		if (IsDesperateProjectile || ShiftedSimulationId == ShiftingSimulationId.Desperate) {
			UpdateDesperateProjectileVisual(
				projectile,
				IsDesperateProjectile ? 1f : ShiftingPrefix.ShiftingStrengthMultiplier);
		}

		if (IsSpinboundProjectile) {
			ApplySpinboundStabilization(projectile);
			AddSpinboundLight(projectile, GoldenSpinTier);
			SpawnSpinboundTrail(projectile, GoldenSpinTier);
		}

		if (IsShiftedSpinboundProjectile) {
			ApplySpinboundStabilization(projectile, ShiftedSpinboundReferenceSpeed);
			AddSpinboundLight(projectile, ShiftedGoldenSpinTier);
			SpawnSpinboundTrail(projectile, ShiftedGoldenSpinTier);
		}

		if (!IsEchoVisualProjectile) {
			return;
		}

		Lighting.AddLight(projectile.Center, 0.035f, 0.05f, 0.09f);
		Color fractureTint = WeaponPrefixVisuals.GetFracturedTint(EchoFractureTier);
		int fractureTier = System.Math.Max(1, (int)EchoFractureTier);
		float tierScale = 0.7f + 0.15f * fractureTier;
		Lighting.AddLight(projectile.Center, fractureTint.ToVector3() * (0.06f * tierScale));

		if (projectile.numUpdates == 0 && projectile.timeLeft % 6 == 0) {
			Dust dust = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, WeaponPrefixVisuals.EchoDustType);
			dust.noGravity = true;
			dust.velocity = projectile.velocity * 0.08f;
			dust.color = fractureTint;
			dust.scale = 0.66f + 0.08f * fractureTier;
			dust.fadeIn = 0.88f + 0.02f * EchoFractureTier;
		}

		if (projectile.numUpdates == 0 && projectile.timeLeft % 7 == 0) {
			Vector2 orbitOffset = Main.rand.NextVector2CircularEdge(projectile.width * 0.55f, projectile.height * 0.55f);
			Dust auraDust = Dust.NewDustPerfect(projectile.Center + orbitOffset, WeaponPrefixVisuals.EchoDustType);
			auraDust.noGravity = true;
			auraDust.velocity = projectile.velocity * 0.02f;
			auraDust.color = fractureTint;
			auraDust.scale = 0.7f + 0.08f * fractureTier;
			auraDust.fadeIn = 0.92f + 0.02f * EchoFractureTier;
		}
	}

	public override Color? GetAlpha(Projectile projectile, Color lightColor)
	{
		if (!IsEchoVisualProjectile) {
			return null;
		}

		return WeaponPrefixVisuals.GetFracturedTint(EchoFractureTier);
	}

	public override bool PreDraw(Projectile projectile, ref Color lightColor)
	{
		if (IsEchoVisualProjectile) {
			Texture2D texture = TextureAssets.Projectile[projectile.type].Value;
			Rectangle frame = texture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
			Vector2 origin = frame.Size() * 0.5f;
			Vector2 basePosition = GetProjectileDrawPosition(projectile);
			float pulse = 1f + 0.015f * System.MathF.Sin(Main.GlobalTimeWrappedHourly * 10f);
			Color fractureTint = WeaponPrefixVisuals.GetFracturedTint(EchoFractureTier);
			Color preDrawTint = Color.Lerp(lightColor, fractureTint, 0.24f);
			Color underlayTint = WeaponPrefixVisuals.GetFracturedAfterimageTint(EchoFractureTier) * (0.34f + 0.04f * System.Math.Max(1, (int)EchoFractureTier));

			DrawEchoSprite(projectile, texture, frame, origin, basePosition, underlayTint, projectile.rotation, projectile.scale * (pulse + 0.01f));
			DrawEchoSprite(projectile, texture, frame, origin, basePosition - projectile.velocity * 0.1f, underlayTint * 0.6f, projectile.rotation, projectile.scale * pulse);

			lightColor = preDrawTint;
			return true;
		}

		if (IsTemporalProjectile || ShiftedSimulationId == ShiftingSimulationId.Temporal) {
			Texture2D texture = TextureAssets.Projectile[projectile.type].Value;
			Rectangle frame = texture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
			Vector2 origin = frame.Size() * 0.5f;
			Vector2 basePosition = GetProjectileDrawPosition(projectile);
			float strength = ShiftedSimulationId == ShiftingSimulationId.Temporal ? ShiftingPrefix.ShiftingStrengthMultiplier : 1f;
			float pulse = WeaponPrefixVisuals.GetTemporalPulse(projectile.identity * 0.19f) * strength;
			Color underlayTint = WeaponPrefixVisuals.TemporalAfterimageTint * (0.26f + 0.05f * System.MathF.Min(strength, 1.5f));

			DrawEchoSprite(
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

		if (IsSkirmishFollowUpProjectile || (ShiftedSimulationId == ShiftingSimulationId.Skirmishing && IsShiftedSkirmishFollowUpProjectile)) {
			Texture2D texture = TextureAssets.Projectile[projectile.type].Value;
			Rectangle frame = texture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
			Vector2 origin = frame.Size() * 0.5f;
			Vector2 basePosition = GetProjectileDrawPosition(projectile);
			float strength = ShiftedSimulationId == ShiftingSimulationId.Skirmishing && IsShiftedSkirmishFollowUpProjectile
				? ShiftingPrefix.ShiftingStrengthMultiplier
				: 1f;
			Color underlayTint = WeaponPrefixVisuals.SkirmishingTracerTint * (0.28f + 0.04f * System.MathF.Min(strength, 1.5f));

			DrawEchoSprite(
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

		if (IsDeadeyeProjectile || IsShiftedDeadeyeProjectile) {
			Texture2D texture = TextureAssets.Projectile[projectile.type].Value;
			Rectangle frame = texture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
			Vector2 origin = frame.Size() * 0.5f;
			Vector2 basePosition = GetProjectileDrawPosition(projectile);
			float strength = IsShiftedDeadeyeProjectile ? ShiftingPrefix.ShiftingStrengthMultiplier : 1f;
			Color underlayTint = WeaponPrefixVisuals.DeadeyeAfterimageTint * (0.32f + 0.05f * System.MathF.Min(strength, 1.5f));

			DrawEchoSprite(
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

	public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
	{
		if (IsEchoDamageProjectile) {
			modifiers.SourceDamage *= EchoDamageMultiplier > 0f ? EchoDamageMultiplier : EchoingPrefix.FractureIDamageMultiplier;
		}

		if (IsSanguineProjectile) {
			pendingVampiricPreyState = SanguinePrefix.GetPreyState(target);
			TryApplyVampiricFrenzyCrit(projectile, pendingVampiricPreyState, ref modifiers, 1f);
		}
		else if (ShiftedSimulationId == ShiftingSimulationId.Sanguine) {
			pendingVampiricPreyState = SanguinePrefix.GetPreyState(target);
			TryApplyVampiricFrenzyCrit(projectile, pendingVampiricPreyState, ref modifiers, ShiftingPrefix.ShiftingStrengthMultiplier);
		}
		else {
			pendingVampiricPreyState = VampiricPreyState.None;
		}

		if (ShiftedSimulationId == ShiftingSimulationId.Breaching) {
			modifiers.ArmorPenetration += ShiftingSimulationCatalog.GetShiftedArmorPenetrationBonus(ShiftedSimulationId);
		}

		if (IsDesperateProjectile) {
			TryApplyDesperateSurge(projectile, ref modifiers, 1f);
		}

		if (IsSkirmishFollowUpProjectile) {
			modifiers.CritDamage += SkirmishingPrefix.FollowUpCritDamageBonus;
		}

		if (ShiftedSimulationId == ShiftingSimulationId.Desperate) {
			TryApplyDesperateSurge(projectile, ref modifiers, ShiftingPrefix.ShiftingStrengthMultiplier, true);
		}

		if (ShiftedSimulationId == ShiftingSimulationId.Skirmishing && IsShiftedSkirmishFollowUpProjectile) {
			modifiers.CritDamage += ShiftingSimulationCatalog.GetShiftedSkirmishingFollowUpCritDamageBonus();
		}

		bool shiftedCatalytic = ShiftedSimulationId == ShiftingSimulationId.Catalytic;

		if ((!IsCatalyticProjectile && !shiftedCatalytic) || !IsCatalyticEligibleProjectile(projectile)) {
			ClearPendingCatalyticConsume();
		}
		else {
			WeaponPrefixGlobalNPC targetGlobal = target.GetGlobalNPC<WeaponPrefixGlobalNPC>();
			if (!targetGlobal.ShouldConsumeCatalyticMark(projectile.owner, projectile, shiftedCatalytic)) {
				ClearPendingCatalyticConsume();
			}
			else {
				modifiers.SourceDamage *= 1f + (shiftedCatalytic
					? ShiftingSimulationCatalog.GetShiftedCatalyticConsumeDamageBonus()
					: CatalyticPrefix.ConsumeDamageBonus);
				pendingCatalyticConsume = true;
			}
		}

		if (!IsDeadeyeProjectile) {
			goto Spinbound;
		}

		TryApplyDeadeyeCrit(projectile, ref modifiers, 1f);
		modifiers.CritDamage += DeadeyePrefix.DeadeyeShotCritDamageBonus;

Spinbound:
		if (!IsSpinboundProjectile || HasConsumedSpinboundHitBonus) {
			goto Shifting;
		}

		float spinboundMultiplier = GetSpinboundSpecialMultiplier(GoldenSpinTier);
		modifiers.SourceDamage *= 1f + SpinboundPrefix.EmpoweredDamageBonus * spinboundMultiplier;
		modifiers.CritDamage += SpinboundPrefix.EmpoweredCritDamageBonus * spinboundMultiplier;
		HasConsumedSpinboundHitBonus = true;
		projectile.netUpdate = true;

Shifting:
		if (ShiftedSimulationId == ShiftingSimulationId.Spinbound && IsShiftedSpinboundProjectile && !HasConsumedShiftedSpinboundHitBonus) {
			float shiftedSpinboundMultiplier = GetSpinboundSpecialMultiplier(ShiftedGoldenSpinTier);
			modifiers.SourceDamage *= 1f + ShiftingSimulationCatalog.GetShiftedSpinboundEmpoweredDamageBonus() * shiftedSpinboundMultiplier;
			modifiers.CritDamage += ShiftingSimulationCatalog.GetShiftedSpinboundEmpoweredCritDamageBonus() * shiftedSpinboundMultiplier;
			modifiers.ArmorPenetration += ShiftingSimulationCatalog.GetShiftedSpinboundArmorPenetrationBonus();
			HasConsumedShiftedSpinboundHitBonus = true;
			projectile.netUpdate = true;
		}

		if (!IsShiftedDeadeyeProjectile) {
			return;
		}

		TryApplyDeadeyeCrit(projectile, ref modifiers, ShiftingPrefix.ShiftingStrengthMultiplier, true);
		modifiers.CritDamage += ShiftingSimulationCatalog.GetShiftedDeadeyeCritDamageBonus();
	}

	public override void ModifyHitPlayer(Projectile projectile, Player target, ref Player.HurtModifiers modifiers)
	{
		if (IsEchoDamageProjectile) {
			modifiers.SourceDamage *= EchoDamageMultiplier > 0f ? EchoDamageMultiplier : EchoingPrefix.FractureIDamageMultiplier;
		}
	}

	public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
	{
		bitWriter.WriteBit(IsEchoDamageProjectile);
		bitWriter.WriteBit(IsEchoVisualProjectile);
		bitWriter.WriteBit(IsSpawnedEchoProjectile);
		bitWriter.WriteBit(IsSanguineProjectile);
		bitWriter.WriteBit(IsRadiantProjectile);
		bitWriter.WriteBit(IsBreachingProjectile);
		bitWriter.WriteBit(IsDesperateProjectile);
		bitWriter.WriteBit(IsTemporalProjectile);
		bitWriter.WriteBit(IsSkirmishingProjectile);
		bitWriter.WriteBit(IsSkirmishFollowUpProjectile);
		bitWriter.WriteBit(IsAttunedProjectile);
		bitWriter.WriteBit(IsEmpoweredAttunedProjectile);
		bitWriter.WriteBit(IsStormforgedProjectile);
		bitWriter.WriteBit(IsCatalyticProjectile);
		bitWriter.WriteBit(IsDeadeyeProjectile);
		bitWriter.WriteBit(IsSpinboundProjectile);
		bitWriter.WriteBit(HasConsumedSpinboundHitBonus);
		bitWriter.WriteBit(IsShiftedSpinboundProjectile);
		bitWriter.WriteBit(HasConsumedShiftedSpinboundHitBonus);
		bitWriter.WriteBit(IsShiftedSkirmishFollowUpProjectile);
		bitWriter.WriteBit(IsEmpoweredShiftedAttunedProjectile);
		bitWriter.WriteBit(IsShiftedDeadeyeProjectile);
		binaryWriter.Write(EchoFractureTier);
		binaryWriter.Write(EchoDamageMultiplier);
		binaryWriter.Write(VampiricManaRestoreAmount);
		binaryWriter.Write(BreachImpactScale);
		binaryWriter.Write(SpinboundReferenceSpeed);
		binaryWriter.Write(GoldenSpinTier);
		binaryWriter.Write((byte)ShiftedSimulationId);
		binaryWriter.Write(ShiftedStrengthMultiplier);
		binaryWriter.Write(ShiftedVampiricManaRestoreAmount);
		binaryWriter.Write(ShiftedSpinboundReferenceSpeed);
		binaryWriter.Write(ShiftedGoldenSpinTier);
	}

	public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
	{
		IsEchoDamageProjectile = bitReader.ReadBit();
		IsEchoVisualProjectile = bitReader.ReadBit();
		IsSpawnedEchoProjectile = bitReader.ReadBit();
		IsSanguineProjectile = bitReader.ReadBit();
		IsRadiantProjectile = bitReader.ReadBit();
		IsBreachingProjectile = bitReader.ReadBit();
		IsDesperateProjectile = bitReader.ReadBit();
		IsTemporalProjectile = bitReader.ReadBit();
		IsSkirmishingProjectile = bitReader.ReadBit();
		IsSkirmishFollowUpProjectile = bitReader.ReadBit();
		IsAttunedProjectile = bitReader.ReadBit();
		IsEmpoweredAttunedProjectile = bitReader.ReadBit();
		IsStormforgedProjectile = bitReader.ReadBit();
		IsCatalyticProjectile = bitReader.ReadBit();
		IsDeadeyeProjectile = bitReader.ReadBit();
		IsSpinboundProjectile = bitReader.ReadBit();
		HasConsumedSpinboundHitBonus = bitReader.ReadBit();
		IsShiftedSpinboundProjectile = bitReader.ReadBit();
		HasConsumedShiftedSpinboundHitBonus = bitReader.ReadBit();
		IsShiftedSkirmishFollowUpProjectile = bitReader.ReadBit();
		IsEmpoweredShiftedAttunedProjectile = bitReader.ReadBit();
		IsShiftedDeadeyeProjectile = bitReader.ReadBit();
		EchoFractureTier = binaryReader.ReadByte();
		EchoDamageMultiplier = binaryReader.ReadSingle();
		VampiricManaRestoreAmount = binaryReader.ReadInt32();
		BreachImpactScale = binaryReader.ReadSingle();
		SpinboundReferenceSpeed = binaryReader.ReadSingle();
		GoldenSpinTier = binaryReader.ReadInt32();
		ShiftedSimulationId = (ShiftingSimulationId)binaryReader.ReadByte();
		ShiftedStrengthMultiplier = binaryReader.ReadSingle();
		ShiftedVampiricManaRestoreAmount = binaryReader.ReadInt32();
		ShiftedSpinboundReferenceSpeed = binaryReader.ReadSingle();
		ShiftedGoldenSpinTier = binaryReader.ReadInt32();
	}

	private static Vector2 GetProjectileDrawPosition(Projectile projectile)
	{
		return projectile.Center - Main.screenPosition + new Vector2(0f, projectile.gfxOffY);
	}

	private static void NormalizeEchoHitTracking(
		Projectile projectile,
		bool sourceUsesLocalNPCImmunity,
		int sourceLocalNPCHitCooldown,
		bool sourceUsesIDStaticNPCImmunity,
		int sourceIDStaticNPCHitCooldown)
	{
		int localNPCHitCooldown = sourceUsesLocalNPCImmunity
			? sourceLocalNPCHitCooldown
			: sourceUsesIDStaticNPCImmunity
				? sourceIDStaticNPCHitCooldown
				: -1;

		projectile.usesIDStaticNPCImmunity = false;
		projectile.idStaticNPCHitCooldown = -1;
		projectile.usesLocalNPCImmunity = true;
		projectile.localNPCHitCooldown = localNPCHitCooldown;
	}

	private static void DrawEchoSprite(Projectile projectile, Texture2D texture, Rectangle frame, Vector2 origin, Vector2 drawPosition, Color color, float rotation, float scale)
	{
		SpriteEffects spriteEffects = projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
		Main.EntitySpriteDraw(texture, drawPosition, frame, color, rotation, origin, scale, spriteEffects);
	}

	private static void TryApplyVampiricFrenzyCrit(Projectile projectile, VampiricPreyState preyState, ref NPC.HitModifiers modifiers, float strengthMultiplier)
	{
		if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers || preyState == VampiricPreyState.None) {
			return;
		}

		Player owner = Main.player[projectile.owner];
		if (!owner.active || owner.dead || !owner.GetModPlayer<VampiricPlayer>().IsFrenzied) {
			return;
		}

		VampiricPlayer vampiricPlayer = owner.GetModPlayer<VampiricPlayer>();
		int critBonus = (int)System.MathF.Round(SanguinePrefix.GetFrenzyCritBonus(preyState) * vampiricPlayer.GetFrenzyTierScale());
		if (critBonus <= 0) {
			return;
		}

		float critChance = System.MathF.Min(0.95f, (critBonus * System.MathF.Max(1f, strengthMultiplier)) / 100f);
		if (Main.rand.NextFloat() < critChance) {
			modifiers.SetCrit();
		}
	}

	private static void TryApplyDeadeyeCrit(Projectile projectile, ref NPC.HitModifiers modifiers, float strengthMultiplier, bool shifted = false)
	{
		if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers) {
			return;
		}

		Player owner = Main.player[projectile.owner];
		if (!owner.active || owner.dead) {
			return;
		}

		int critBonus = shifted
			? ShiftingSimulationCatalog.GetShiftedDeadeyeCritChanceBonus()
			: DeadeyePrefix.DeadeyeShotCritChanceBonus;
		if (critBonus <= 0) {
			return;
		}

		float critChance = System.MathF.Min(0.95f, (critBonus * System.MathF.Max(1f, strengthMultiplier)) / 100f);
		if (Main.rand.NextFloat() < critChance) {
			modifiers.SetCrit();
		}
	}

	private void TryApplyDesperateSurge(Projectile projectile, ref NPC.HitModifiers modifiers, float visualMultiplier, bool shifted = false)
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
			? ShiftingSimulationCatalog.GetShiftedDesperateSurgeDamageBonus()
			: DesperatePrefix.SurgeDamageBonus;
		modifiers.SourceDamage *= 1f + surgeDamageBonus;
		HasPendingDesperateSurgeFeedback = true;
	}

	private static void HandleDeadeyeHit(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone, float strengthMultiplier, bool shifted = false)
	{
		SpawnDeadeyeImpactEffect(target.Center, projectile.velocity, strengthMultiplier, hit.Crit);
		if (!hit.Crit) {
			return;
		}

		TryTriggerDeadeyeBurst(projectile, target, damageDone, strengthMultiplier, shifted);
	}

	private static void UpdateDeadeyeProjectileVisual(Projectile projectile, float strengthMultiplier)
	{
		float pulse = 0.8f + 0.2f * (0.5f + 0.5f * System.MathF.Sin(Main.GlobalTimeWrappedHourly * 10.5f + projectile.identity * 0.23f));
		Lighting.AddLight(projectile.Center, WeaponPrefixVisuals.DeadeyeShotColor.ToVector3() * (0.14f * pulse * strengthMultiplier));

		if (projectile.owner != Main.myPlayer || projectile.numUpdates != 0) {
			return;
		}

		if (projectile.timeLeft % 5 == 0) {
			Dust shotDust = Dust.NewDustPerfect(
				projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
				WeaponPrefixVisuals.DeadeyeShotDustType,
				projectile.velocity * -0.06f + Main.rand.NextVector2Circular(0.1f, 0.1f),
				0,
				WeaponPrefixVisuals.DeadeyeShotColor,
				0.8f * strengthMultiplier);
			shotDust.noGravity = true;
			shotDust.fadeIn = 0.92f;
		}

		if (projectile.timeLeft % 9 == 0) {
			Dust leafDust = Dust.NewDustPerfect(
				projectile.Center + Main.rand.NextVector2Circular(3f, 3f),
				WeaponPrefixVisuals.DeadeyeLeafDustType,
				projectile.velocity * -0.03f + Main.rand.NextVector2Circular(0.08f, 0.08f),
				0,
				WeaponPrefixVisuals.DeadeyeLeafAccentColor,
				0.72f * strengthMultiplier);
			leafDust.noGravity = true;
			leafDust.fadeIn = 0.9f;
		}
	}

	private static void SpawnDeadeyeImpactEffect(Vector2 position, Vector2 projectileVelocity, float strengthMultiplier, bool critical)
	{
		Vector2 direction = projectileVelocity.SafeNormalize(Vector2.UnitX);
		Vector2 tangent = direction.RotatedBy(MathHelper.PiOver2);
		Color impactColor = critical ? WeaponPrefixVisuals.DeadeyeLeafAccentColor : WeaponPrefixVisuals.DeadeyeShotColor;
		Lighting.AddLight(position, impactColor.ToVector3() * ((critical ? 0.22f : 0.14f) * strengthMultiplier));

		int dustCount = critical ? 7 : 4;
		for (int i = 0; i < dustCount; i++) {
			float side = i % 2 == 0 ? -1f : 1f;
			float lane = 1f + i / 2f;
			Vector2 velocity = direction * (critical ? 1.2f : 0.8f) + tangent * (0.24f * lane * side);
			Dust dust = Dust.NewDustPerfect(
				position + tangent * (2.5f * lane * side),
				critical && i % 2 == 0 ? WeaponPrefixVisuals.DeadeyeLeafDustType : WeaponPrefixVisuals.DeadeyeShotDustType,
				velocity,
				0,
				critical && i % 2 == 0 ? WeaponPrefixVisuals.DeadeyeLeafAccentColor : impactColor,
				(critical ? 0.95f : 0.78f) * strengthMultiplier);
			dust.noGravity = true;
			dust.fadeIn = critical ? 0.95f : 0.88f;
		}
	}

	private static void TryTriggerDeadeyeBurst(Projectile projectile, NPC primaryTarget, int damageDone, float strengthMultiplier, bool shifted)
	{
		if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers || damageDone <= 0 || primaryTarget == null || !primaryTarget.active) {
			return;
		}

		Player owner = Main.player[projectile.owner];
		if (!owner.active || owner.dead) {
			return;
		}

		int baseBurstDamage = (int)System.MathF.Max(
			1f,
			System.MathF.Round(damageDone * (shifted
				? ShiftingSimulationCatalog.GetShiftedDeadeyeBurstDamageRatio()
				: DeadeyePrefix.CriticalBurstDamageRatio)));

		SpawnDeadeyeBurstEffect(primaryTarget.Center, strengthMultiplier);
		if (owner.whoAmI == Main.myPlayer && owner.GetModPlayer<DeadeyePlayer>().CanPlayCritBurstSound()) {
			SoundEngine.PlaySound(SoundID.Item27 with { Pitch = 0.12f, Volume = 0.55f }, primaryTarget.Center);
		}

		if (owner.whoAmI != Main.myPlayer) {
			return;
		}

		float maxDistanceSquared = DeadeyePrefix.CriticalBurstRadiusPixels * DeadeyePrefix.CriticalBurstRadiusPixels;
		foreach (NPC candidate in Main.npc) {
			if (!candidate.active
				|| candidate.whoAmI == primaryTarget.whoAmI
				|| candidate.friendly
				|| candidate.dontTakeDamage
				|| candidate.type == NPCID.TargetDummy
				|| (!candidate.CanBeChasedBy() && !candidate.immortal)
				|| Vector2.DistanceSquared(primaryTarget.Center, candidate.Center) > maxDistanceSquared) {
				continue;
			}

			int burstDamage = candidate.boss
				? (int)System.MathF.Max(1f, System.MathF.Round(baseBurstDamage * DeadeyePrefix.BossBurstDamageMultiplier))
				: baseBurstDamage;
			owner.ApplyDamageToNPC(candidate, burstDamage, 0f, candidate.Center.X >= owner.Center.X ? 1 : -1, false);
		}
	}

	private static void SpawnDeadeyeBurstEffect(Vector2 position, float strengthMultiplier)
	{
		Lighting.AddLight(position, WeaponPrefixVisuals.DeadeyeLeafBurstColor.ToVector3() * (0.24f * strengthMultiplier));
		for (int i = 0; i < 10; i++) {
			float angle = MathHelper.TwoPi * i / 10f;
			Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(0.8f, 1.8f) * strengthMultiplier;

			Dust leafDust = Dust.NewDustPerfect(position, WeaponPrefixVisuals.DeadeyeLeafDustType, velocity, 0, WeaponPrefixVisuals.DeadeyeLeafBurstColor, 0.92f * strengthMultiplier);
			leafDust.noGravity = true;
			leafDust.fadeIn = 0.95f;

			if (i % 2 == 0) {
				Dust ghostDust = Dust.NewDustPerfect(position, WeaponPrefixVisuals.DeadeyeShotDustType, velocity * 0.6f, 0, WeaponPrefixVisuals.DeadeyeShotColor, 0.72f * strengthMultiplier);
				ghostDust.noGravity = true;
				ghostDust.fadeIn = 0.9f;
			}
		}
	}

	private static void ApplySpinboundStabilization(Projectile projectile)
	{
		ApplySpinboundStabilization(projectile, projectile.GetGlobalProjectile<WeaponPrefixGlobalProjectile>().SpinboundReferenceSpeed);
	}

	private static void ApplySpinboundStabilization(Projectile projectile, float targetSpeed)
	{
		if (projectile == null || !projectile.active || projectile.velocity.LengthSquared() <= 0.0001f) {
			return;
		}

		float currentSpeed = projectile.velocity.Length();
		if (currentSpeed <= 0f) {
			return;
		}

		if (targetSpeed <= 0f) {
			return;
		}

		float stabilizedSpeed = MathHelper.Lerp(currentSpeed, targetSpeed, SpinboundPrefix.StabilizationStrength);
		Vector2 desiredVelocity = projectile.velocity.SafeNormalize(Vector2.UnitX * projectile.direction) * stabilizedSpeed;
		projectile.velocity = Vector2.Lerp(projectile.velocity, desiredVelocity, SpinboundPrefix.StabilizationStrength);
	}

	private static void UpdateVampiricProjectileVisual(Projectile projectile, bool magicFeed, float intensityMultiplier)
	{
		float pulse = WeaponPrefixVisuals.GetVampiricPulse(projectile.identity * 0.14f) * intensityMultiplier;
		Color feedColor = WeaponPrefixVisuals.GetVampiricFeedColor(VampiricPreyState.None, magicFeed);
		Lighting.AddLight(projectile.Center, feedColor.ToVector3() * (0.1f * pulse));

		if (projectile.owner != Main.myPlayer
			|| projectile.numUpdates != 0
			|| projectile.timeLeft % (magicFeed ? 6 : 8) != 0) {
			return;
		}

		Vector2 lateralOffset = projectile.velocity.SafeNormalize(Vector2.UnitX * projectile.direction)
			.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-4f, 4f);
		Dust bloodDust = Dust.NewDustPerfect(projectile.Center + lateralOffset, WeaponPrefixVisuals.VampiricDustType);
		bloodDust.noGravity = true;
		bloodDust.velocity = projectile.velocity * -0.03f + Main.rand.NextVector2Circular(0.08f, 0.08f);
		bloodDust.scale = 0.72f * intensityMultiplier;
		bloodDust.fadeIn = 0.94f;

		if (!magicFeed || projectile.timeLeft % 12 != 0) {
			return;
		}

		Dust manaDust = Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(3f, 3f), WeaponPrefixVisuals.VampiricMagicDustType);
		manaDust.noGravity = true;
		manaDust.velocity = projectile.velocity * -0.025f + Main.rand.NextVector2Circular(0.06f, 0.06f);
		manaDust.scale = 0.7f * intensityMultiplier;
		manaDust.fadeIn = 0.9f;
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

	private static void UpdateTemporalProjectileVisual(Projectile projectile, float intensityMultiplier)
	{
		float pulse = WeaponPrefixVisuals.GetTemporalPulse(projectile.identity * 0.19f) * intensityMultiplier;
		Lighting.AddLight(projectile.Center, 0.15f * pulse, 0.24f * pulse, 0.36f * pulse);

		if (projectile.numUpdates == 0 && projectile.timeLeft % 6 == 0) {
			Vector2 lateralOffset = projectile.velocity.SafeNormalize(Vector2.UnitX * projectile.direction)
				.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-6f, 6f);
			Dust dust = Dust.NewDustPerfect(projectile.Center + lateralOffset, WeaponPrefixVisuals.TemporalDustType);
			dust.noGravity = true;
			dust.velocity = projectile.velocity * -0.035f + Main.rand.NextVector2Circular(0.12f, 0.12f);
			dust.scale = 0.72f + 0.08f * intensityMultiplier;
			dust.fadeIn = 0.95f;
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

	private static void SpawnAttunedImpactEffect(Vector2 position, Vector2 projectileVelocity, bool shifted, float visualMultiplier)
	{
		if (Main.myPlayer < 0 || Main.myPlayer >= Main.maxPlayers) {
			return;
		}

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

	private static void SpawnSpinboundReleaseEffect(Projectile projectile, int goldenSpinTier)
	{
		if (projectile == null || !projectile.active) {
			return;
		}

		float tierThreePlusMultiplier = goldenSpinTier >= 3 ? 1.5f : 1f;
		float glowStrength = goldenSpinTier switch {
			<= 1 => 0.55f,
			<= 3 => 0.78f,
			_ => 1.05f
		};
		float haloStrength = goldenSpinTier >= 4 ? 0.18f + (goldenSpinTier - 4) * 0.06f : 0f;
		Lighting.AddLight(
			projectile.Center,
			0.7f * glowStrength + haloStrength,
			0.56f * glowStrength + haloStrength * 0.75f,
			0.14f * glowStrength + haloStrength * 0.3f);

		int coreBurstCount = ScalePositiveRoundedInt(4 + goldenSpinTier, tierThreePlusMultiplier);
		int ringCount = ScalePositiveRoundedInt(5 + goldenSpinTier, tierThreePlusMultiplier);
		float ringRadius = 8f + goldenSpinTier * 1.7f;
		Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX * projectile.direction);
		Vector2 side = forward.RotatedBy(MathHelper.PiOver2);

		for (int i = 0; i < coreBurstCount; i++) {
			float sideDirection = i % 2 == 0 ? -1f : 1f;
			float layer = 1f + i / 2f;
			Vector2 offset = side * (7f + layer * 3f) * sideDirection;
			int dustType = goldenSpinTier >= 4 && i % 3 == 0 ? DustID.YellowTorch : DustID.GoldFlame;
			Dust dust = Dust.NewDustPerfect(projectile.Center + offset, dustType);
			dust.noGravity = true;
			dust.velocity = forward * (1.4f + goldenSpinTier * 0.08f) + side * (0.45f * sideDirection * layer);
			dust.scale = 0.9f + goldenSpinTier * 0.06f;
			dust.fadeIn = 1.1f;
			ApplySpinboundDustPersistence(dust, goldenSpinTier);
		}

		for (int i = 0; i < ringCount; i++) {
			float angle = MathHelper.TwoPi * i / ringCount;
			Vector2 offset = angle.ToRotationVector2() * ringRadius;
			int dustType = goldenSpinTier >= 4 && i % 4 == 0 ? DustID.YellowTorch : DustID.GoldFlame;
			Dust dust = Dust.NewDustPerfect(projectile.Center + offset, dustType);
			dust.noGravity = true;
			dust.velocity = offset.SafeNormalize(Vector2.UnitX) * (1.15f + goldenSpinTier * 0.12f);
			dust.scale = 0.72f + goldenSpinTier * 0.05f;
			dust.fadeIn = 0.9f;
			ApplySpinboundDustPersistence(dust, goldenSpinTier);
		}

		if (goldenSpinTier >= 5) {
			int outerRingCount = ScalePositiveRoundedInt(10, tierThreePlusMultiplier);
			float outerRingRadius = ringRadius + 8f;
			for (int i = 0; i < outerRingCount; i++) {
				float angle = MathHelper.TwoPi * i / outerRingCount;
				Vector2 offset = angle.ToRotationVector2() * outerRingRadius;
				Dust dust = Dust.NewDustPerfect(projectile.Center + offset, i % 2 == 0 ? DustID.YellowTorch : DustID.GoldFlame);
				dust.noGravity = true;
				dust.velocity = offset.SafeNormalize(Vector2.UnitX) * 1.9f;
				dust.scale = 1.02f;
				dust.fadeIn = 1f;
				ApplySpinboundDustPersistence(dust, goldenSpinTier);
			}
		}
	}

	private static void SpawnSpinboundTrail(Projectile projectile, int goldenSpinTier)
	{
		if (projectile == null
			|| !projectile.active
			|| projectile.numUpdates != 0
			|| projectile.timeLeft % 3 != 0) {
			return;
		}

		float tierThreePlusMultiplier = goldenSpinTier >= 3 ? 1.5f : 1f;
		int sideCount = goldenSpinTier >= 5 ? ScalePositiveRoundedInt(3, tierThreePlusMultiplier) : goldenSpinTier >= 3 ? ScalePositiveRoundedInt(2, tierThreePlusMultiplier) : 1;
		Vector2 tangent = projectile.velocity.SafeNormalize(Vector2.UnitX * projectile.direction).RotatedBy(MathHelper.PiOver2);
		for (int layer = 0; layer < sideCount; layer++) {
			float offsetDistance = 6f + layer * 3f;
			for (int i = -1; i <= 1; i += 2) {
				int dustType = goldenSpinTier >= 4 && (layer + (i > 0 ? 1 : 0)) % 2 == 0 ? DustID.YellowTorch : DustID.GoldFlame;
				Dust dust = Dust.NewDustPerfect(projectile.Center + tangent * (offsetDistance * i), dustType);
				dust.noGravity = true;
				dust.velocity = projectile.velocity * (0.08f + goldenSpinTier * 0.005f) + tangent * ((0.45f + layer * 0.08f) * i);
				dust.scale = 0.7f + goldenSpinTier * 0.04f;
				dust.fadeIn = goldenSpinTier >= 4 ? 1.02f : 0.9f;
				ApplySpinboundDustPersistence(dust, goldenSpinTier);
			}
		}

		if (goldenSpinTier >= 3) {
			float orbitAngle = projectile.timeLeft * 0.3f + projectile.identity * 0.17f;
			float orbitRadius = 7f + goldenSpinTier * 1.2f;
			int spiralCount = goldenSpinTier >= 5 ? ScalePositiveRoundedInt(2, tierThreePlusMultiplier) : ScalePositiveRoundedInt(1, tierThreePlusMultiplier);
			for (int i = 0; i < spiralCount; i++) {
				float angle = orbitAngle + MathHelper.Pi * i;
				Vector2 offset = angle.ToRotationVector2() * orbitRadius;
				Dust spiralDust = Dust.NewDustPerfect(
					projectile.Center + offset,
					goldenSpinTier >= 4 && i == 0 ? DustID.YellowTorch : DustID.GoldFlame);
				spiralDust.noGravity = true;
				spiralDust.velocity = projectile.velocity * 0.06f + offset.SafeNormalize(Vector2.UnitX) * 0.45f;
				spiralDust.scale = 0.68f + goldenSpinTier * 0.035f;
				spiralDust.fadeIn = 0.95f + goldenSpinTier * 0.03f;
				ApplySpinboundDustPersistence(spiralDust, goldenSpinTier);
			}
		}
	}

	private static void AddSpinboundLight(Projectile projectile, int goldenSpinTier)
	{
		float strength = 0.12f + goldenSpinTier * 0.025f;
		float halo = goldenSpinTier >= 4 ? 0.04f + (goldenSpinTier - 4) * 0.015f : 0f;
		Lighting.AddLight(
			projectile.Center,
			0.28f + strength + halo,
			0.21f + strength * 0.75f + halo * 0.7f,
			0.05f + goldenSpinTier * 0.007f + halo * 0.2f);
	}

	private static void PlaySpinboundSpawnSounds(Projectile projectile, int goldenSpinTier)
	{
		if (projectile == null || !projectile.active || projectile.owner != Main.myPlayer) {
			return;
		}

		SoundEngine.PlaySound(SoundID.Item42, projectile.Center);
		if (goldenSpinTier >= 4) {
			SoundEngine.PlaySound(SoundID.Item20, projectile.Center);
		}

		if (goldenSpinTier >= 5) {
			SoundEngine.PlaySound(SoundID.Item113, projectile.Center);
		}
	}

	private static void ApplySpinboundDustPersistence(Dust dust, int goldenSpinTier)
	{
		if (goldenSpinTier < 3) {
			return;
		}

		dust.velocity *= 0.67f;
		dust.scale *= 1.12f;
		dust.fadeIn += 0.25f;
	}

	private static int ScalePositiveRoundedInt(int value, float multiplier)
	{
		return System.Math.Max(1, (int)System.MathF.Round(value * multiplier));
	}

	private static GoldenSpinEvaluation EvaluateGoldenSpin(Player owner, Vector2 shotVelocity)
	{
		Vector2 velocity = shotVelocity;
		float speed = velocity.Length();
		float playerSpeed = owner?.active == true ? owner.velocity.Length() : 0f;
		float velocityVectorRatio = System.MathF.Abs(velocity.X) / System.MathF.Max(System.MathF.Abs(velocity.Y), 0.001f);
		float foldedAngleDegrees = MathHelper.ToDegrees(System.MathF.Atan2(System.MathF.Abs(velocity.Y), System.MathF.Abs(velocity.X)));
		float speedRatio = playerSpeed / System.MathF.Max(speed, 0.001f);
		float angleDeltaDegrees = GetGoldenAngleDeltaDegrees(foldedAngleDegrees);

		GoldenSpinMatchFlags matches = GoldenSpinMatchFlags.None;
		bool velocityVectorMatch = MatchesGoldenRatio(velocityVectorRatio, SpinboundPrefix.VelocityRatioTolerance);
		if (velocityVectorMatch) {
			matches |= GoldenSpinMatchFlags.VelocityVector;
		}

		bool angleGoldenMatch = angleDeltaDegrees <= SpinboundPrefix.AngleToleranceDegrees;
		if (angleGoldenMatch) {
			matches |= GoldenSpinMatchFlags.AngleGolden;
		}

		if (MatchesGoldenRatio(speedRatio, SpinboundPrefix.SpeedRatioTolerance)) {
			matches |= GoldenSpinMatchFlags.SpeedPrecision;
		}

		if (angleDeltaDegrees <= SpinboundPrefix.AnglePrecisionToleranceDegrees) {
			matches |= GoldenSpinMatchFlags.AnglePrecision;
		}

		if (velocityVectorMatch && angleGoldenMatch) {
			matches |= GoldenSpinMatchFlags.PrecisionRelease;
		}

		return new GoldenSpinEvaluation {
			Matches = matches,
			Tier = CountGoldenSpinMatches(matches),
			PlayerSpeed = playerSpeed,
			Velocity = velocity,
			VelocityVectorRatio = velocityVectorRatio,
			FoldedAngleDegrees = foldedAngleDegrees,
			Speed = speed,
			SpeedRatio = speedRatio
		};
	}

	private static bool MatchesGoldenRatio(float ratio, float tolerance)
	{
		return MatchesGoldenValue(ratio, SpinboundPrefix.GoldenRatio, tolerance)
			|| MatchesGoldenValue(ratio, SpinboundPrefix.InverseGoldenRatio, tolerance);
	}

	private static bool MatchesGoldenValue(float value, float target, float tolerance)
	{
		return System.MathF.Abs(value - target) <= tolerance;
	}

	private static float GetGoldenAngleDeltaDegrees(float foldedAngleDegrees)
	{
		float lowAngleDelta = System.MathF.Abs(foldedAngleDegrees - SpinboundPrefix.LowGoldenAngleDegrees);
		float highAngleDelta = System.MathF.Abs(foldedAngleDegrees - SpinboundPrefix.HighGoldenAngleDegrees);
		return System.MathF.Min(lowAngleDelta, highAngleDelta);
	}

	private static float GetSpinboundSpecialMultiplier(int goldenSpinTier)
	{
		return System.Math.Min(SpinboundPrefix.MaxGoldenSpinMultiplier, 1 + goldenSpinTier);
	}

	private static int CountGoldenSpinMatches(GoldenSpinMatchFlags matches)
	{
		int tier = 0;
		if ((matches & GoldenSpinMatchFlags.VelocityVector) != 0) {
			tier++;
		}

		if ((matches & GoldenSpinMatchFlags.AngleGolden) != 0) {
			tier++;
		}

		if ((matches & GoldenSpinMatchFlags.SpeedPrecision) != 0) {
			tier++;
		}

		if ((matches & GoldenSpinMatchFlags.AnglePrecision) != 0) {
			tier++;
		}

		if ((matches & GoldenSpinMatchFlags.PrecisionRelease) != 0) {
			tier++;
		}

		return System.Math.Min(SpinboundPrefix.MaxGoldenSpinTier, tier);
	}

	private static void ShowSpinboundCombatText(Projectile projectile, GoldenSpinEvaluation goldenSpin)
	{
		if (projectile.owner != Main.myPlayer) {
			return;
		}

		StringBuilder text = new($"Spin G{goldenSpin.Tier}");
		AppendGoldenSpinTag(text, goldenSpin.Matches, GoldenSpinMatchFlags.VelocityVector, "VV");
		AppendGoldenSpinTag(text, goldenSpin.Matches, GoldenSpinMatchFlags.AngleGolden, "AG");
		AppendGoldenSpinTag(text, goldenSpin.Matches, GoldenSpinMatchFlags.SpeedPrecision, "SP");
		AppendGoldenSpinTag(text, goldenSpin.Matches, GoldenSpinMatchFlags.AnglePrecision, "AP");
		AppendGoldenSpinTag(text, goldenSpin.Matches, GoldenSpinMatchFlags.PrecisionRelease, "PR");
		CombatText.NewText(projectile.Hitbox, Color.Gold, text.ToString());
		Main.NewText(BuildGoldenSpinDebugText(goldenSpin), Color.Gold);
	}

	private static void AppendGoldenSpinTag(StringBuilder text, GoldenSpinMatchFlags matches, GoldenSpinMatchFlags flag, string tag)
	{
		if ((matches & flag) == 0) {
			return;
		}

		text.Append(' ');
		text.Append(tag);
	}

	private static string BuildGoldenSpinDebugText(GoldenSpinEvaluation goldenSpin)
	{
		StringBuilder text = new();
		text.AppendLine($"Spin G{goldenSpin.Tier}");
		text.AppendLine($"VV={FormatGoldenSpinValue(goldenSpin.VelocityVectorRatio)} need 0.618/1.618 tol {SpinboundPrefix.VelocityRatioTolerance:0.##}");
		text.AppendLine($"AG={FormatGoldenSpinValue(goldenSpin.FoldedAngleDegrees)} need {SpinboundPrefix.LowGoldenAngleDegrees:0.##}/{SpinboundPrefix.HighGoldenAngleDegrees:0.##} tol {SpinboundPrefix.AngleToleranceDegrees:0.##}");
		text.AppendLine($"SP={FormatGoldenSpinValue(goldenSpin.SpeedRatio)} need 0.618/1.618 tol {SpinboundPrefix.SpeedRatioTolerance:0.##} (shotSpeed={FormatGoldenSpinValue(goldenSpin.Speed)} playerSpeed={FormatGoldenSpinValue(goldenSpin.PlayerSpeed)})");
		text.AppendLine($"AP={FormatGoldenSpinValue(goldenSpin.FoldedAngleDegrees)} need {SpinboundPrefix.LowGoldenAngleDegrees:0.##}/{SpinboundPrefix.HighGoldenAngleDegrees:0.##} tol {SpinboundPrefix.AnglePrecisionToleranceDegrees:0.##}");
		text.AppendLine($"PR={(((goldenSpin.Matches & GoldenSpinMatchFlags.PrecisionRelease) != 0) ? "hit" : "miss")} need VV+AG");
		text.Append($"velocity=({FormatGoldenSpinValue(goldenSpin.Velocity.X)},{FormatGoldenSpinValue(goldenSpin.Velocity.Y)})");
		return text.ToString();
	}

	private static string FormatGoldenSpinValue(float value)
	{
		return float.IsFinite(value) ? value.ToString("0.##") : "n/a";
	}

	private void HandleCatalyticHit(Projectile projectile, NPC target)
	{
		HandleCatalyticHit(projectile, target, false, CatalyticPrefix.MarkDurationTicks, CatalyticPrefix.MarkArmDelayTicks);
	}

	private void HandleCatalyticHit(Projectile projectile, NPC target, bool shifted, int durationTicks, int armDelayTicks)
	{
		if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers || target == null || !target.active) {
			ClearPendingCatalyticConsume();
			return;
		}

		WeaponPrefixGlobalNPC targetGlobal = target.GetGlobalNPC<WeaponPrefixGlobalNPC>();


		if (pendingCatalyticConsume
			&& targetGlobal.ShouldConsumeCatalyticMark(projectile.owner, projectile, shifted)) {
			targetGlobal.ConsumeCatalyticMark(projectile.owner, shifted);
			targetGlobal.SpawnCatalyticConsumeEffect(target, shifted);

			ClearPendingCatalyticConsume();

			return;
		}

		targetGlobal.ApplyCatalyticMark(projectile.owner, projectile, shifted, durationTicks, armDelayTicks);

		pendingCatalyticConsume = true;
	}

	private static bool IsCatalyticEligibleProjectile(Projectile projectile)
	{
		return projectile != null
			&& projectile.active;
	}

	
	private void ClearPendingCatalyticConsume()
	{
		pendingCatalyticConsume = false;
	}

    private void TryInitializeShiftingState(Projectile projectile, IEntitySource source)
	{
		if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers) {
			return;
		}

		Player owner = Main.player[projectile.owner];
		if (source is IEntitySource_WithStatsFromItem itemSource) {
			Item item = itemSource.Item;
			if (item == null
				|| item.IsAir
				|| item.prefix != ModContent.PrefixType<ShiftingPrefix>()
				|| !owner.active
				|| owner.dead
				|| !owner.GetModPlayer<ShiftingPlayer>().TryGetActiveSimulation(item, out ShiftingSimulationId simulationId)) {
				return;
			}

			ShiftedSimulationId = simulationId;
			ShiftedStrengthMultiplier = ShiftingPrefix.ShiftingStrengthMultiplier;
			InitializeShiftedSimulationSnapshots(projectile, item);
			return;
		}

		if (source is EntitySource_Parent parentSource && parentSource.Entity is Projectile parentProjectile) {
			WeaponPrefixGlobalProjectile parentGlobal = parentProjectile.GetGlobalProjectile<WeaponPrefixGlobalProjectile>();
			if (parentGlobal.ShiftedSimulationId == ShiftingSimulationId.None
				|| !CanInheritShiftedSimulation(parentGlobal.ShiftedSimulationId)) {
				return;
			}

			ShiftedSimulationId = parentGlobal.ShiftedSimulationId;
			ShiftedStrengthMultiplier = parentGlobal.ShiftedStrengthMultiplier;
			InitializeShiftedSimulationSnapshots(projectile, null, parentGlobal);
		}
	}

	private void InitializeShiftedSimulationSnapshots(Projectile projectile, Item item, WeaponPrefixGlobalProjectile parentGlobal = null)
	{
		switch (ShiftedSimulationId) {
			case ShiftingSimulationId.Echoing:
				break;
			case ShiftingSimulationId.Skirmishing:
				if (item != null) {
					Player owner = Main.player[projectile.owner];
					if (owner.active && !owner.dead) {
						IsShiftedSkirmishFollowUpProjectile = owner.GetModPlayer<SkirmishingPlayer>().IsFollowUpProjectileWindowActive();
					}
				}
				break;
			case ShiftingSimulationId.Attuned:
				if (item != null) {
					Player owner = Main.player[projectile.owner];
					if (owner.active && !owner.dead) {
						IsEmpoweredShiftedAttunedProjectile = owner.GetModPlayer<AttunedPlayer>().IsEmpoweredProjectileWindowActive();
					}
				}
				break;
			case ShiftingSimulationId.Deadeye:
				break;
			case ShiftingSimulationId.Sanguine:
				ShiftedVampiricManaRestoreAmount = item != null && WeaponPrefix.IsMagicWeapon(item)
					? WeaponPrefixGlobalItem.GetVampiricManaRestoreAmount(
						item,
						ShiftingSimulationCatalog.GetShiftedVampiricManaRestoreRatio(),
						ShiftingSimulationCatalog.GetShiftedVampiricMaxRestorePerHit())
					: parentGlobal?.ShiftedVampiricManaRestoreAmount ?? 0;
				break;
			case ShiftingSimulationId.Spinbound:
				if (item != null) {
					Vector2 snapshotVelocity = projectile.velocity;
					bool triggered;
					Main.player[projectile.owner].GetModPlayer<SpinboundPlayer>().TryConsumeSpinboundShotContext(item, true, out snapshotVelocity, out triggered);
					if (triggered) {
						IsShiftedSpinboundProjectile = true;
						ShiftedSpinboundReferenceSpeed = snapshotVelocity.Length();
						ShiftedGoldenSpinTier = EvaluateGoldenSpin(Main.player[projectile.owner], snapshotVelocity).Tier;
						PlaySpinboundSpawnSounds(projectile, ShiftedGoldenSpinTier);
						SpawnSpinboundReleaseEffect(projectile, ShiftedGoldenSpinTier);
					}
				}
				break;
		}
	}

	private static bool TryGetShiftedSimulation(Item item, int ownerIndex, out ShiftingSimulationId simulationId)
	{
		simulationId = ShiftingSimulationId.None;
		if (ownerIndex < 0 || ownerIndex >= Main.maxPlayers) {
			return false;
		}

		Player owner = Main.player[ownerIndex];
		return owner.active
			&& !owner.dead
			&& owner.GetModPlayer<ShiftingPlayer>().TryGetActiveSimulation(item, out simulationId);
	}

	private static bool CanInheritShiftedSimulation(ShiftingSimulationId simulationId)
	{
		return simulationId is ShiftingSimulationId.Sanguine
			or ShiftingSimulationId.Radiant
			or ShiftingSimulationId.Stormforged;
	}

	private void TryScheduleFractureCascade(Projectile projectile, float fractureIChance, float fractureIIChance, float fractureIIIChance)
	{
		if (projectile == null
			|| !projectile.active
			|| IsSpawnedEchoProjectile
			|| Main.rand.NextFloat() >= fractureIChance) {
			return;
		}

		InitializeFractureSourceState(projectile);
		SpawnFracturedProjectile(projectile, 1);

		if (Main.rand.NextFloat() >= fractureIIChance) {
			return;
		}

		hasPendingFractureII = true;
		pendingFractureIIFrames = Main.rand.Next(EchoingPrefix.FractureIIDelayMinFrames, EchoingPrefix.FractureIIDelayMaxFrames + 1);
		if (Main.rand.NextFloat() >= fractureIIIChance) {
			return;
		}

		hasPendingFractureIII = true;
		int fractureIIIMinFrames = System.Math.Min(
			EchoingPrefix.FractureIIIDelayMaxFrames,
			System.Math.Max(EchoingPrefix.FractureIIIDelayMinFrames, pendingFractureIIFrames + 1));
		pendingFractureIIIFrames = Main.rand.Next(fractureIIIMinFrames, EchoingPrefix.FractureIIIDelayMaxFrames + 1);
	}

	private void InitializeFractureSourceState(Projectile projectile)
	{
		fractureSpawnPosition = projectile.Center;
		fractureBaseVelocity = projectile.velocity;
		fractureUsesLocalNPCImmunity = projectile.usesLocalNPCImmunity;
		fractureLocalNPCHitCooldown = projectile.localNPCHitCooldown;
		fractureUsesIDStaticNPCImmunity = projectile.usesIDStaticNPCImmunity;
		fractureIDStaticNPCHitCooldown = projectile.idStaticNPCHitCooldown;

		NormalizeEchoHitTracking(
			projectile,
			fractureUsesLocalNPCImmunity,
			fractureLocalNPCHitCooldown,
			fractureUsesIDStaticNPCImmunity,
			fractureIDStaticNPCHitCooldown);
	}

	private void ProcessPendingFractures(Projectile projectile)
	{
		if (projectile == null
			|| !projectile.active
			|| projectile.owner != Main.myPlayer
			|| projectile.numUpdates != 0) {
			return;
		}

		if (hasPendingFractureII && --pendingFractureIIFrames <= 0) {
			hasPendingFractureII = false;
			SpawnFracturedProjectile(projectile, 2);
		}

		if (hasPendingFractureIII && --pendingFractureIIIFrames <= 0) {
			hasPendingFractureIII = false;
			SpawnFracturedProjectile(projectile, 3);
		}
	}

	private void SpawnFracturedProjectile(Projectile projectile, int fractureTier)
	{
		if (projectile == null || !projectile.active) {
			return;
		}

		Vector2 baseVelocity = fractureBaseVelocity.LengthSquared() > 0.0001f ? fractureBaseVelocity : projectile.velocity;
		Vector2 spawnPosition = fractureSpawnPosition == Vector2.Zero ? projectile.Center : fractureSpawnPosition;
		Vector2 fracturedVelocity = baseVelocity.RotatedByRandom(MathHelper.ToRadians(GetFractureAngleVarianceDegrees(fractureTier)));
		int fractureIndex = Projectile.NewProjectile(
			projectile.GetSource_FromThis("MozandifiersFracturedProjectile"),
			spawnPosition,
			fracturedVelocity,
			projectile.type,
			projectile.damage,
			projectile.knockBack,
			projectile.owner);

		if (fractureIndex < 0 || fractureIndex >= Main.maxProjectiles) {
			return;
		}

		Projectile fracturedProjectile = Main.projectile[fractureIndex];
		NormalizeEchoHitTracking(
			fracturedProjectile,
			fractureUsesLocalNPCImmunity,
			fractureLocalNPCHitCooldown,
			fractureUsesIDStaticNPCImmunity,
			fractureIDStaticNPCHitCooldown);

		WeaponPrefixGlobalProjectile echoGlobal = fracturedProjectile.GetGlobalProjectile<WeaponPrefixGlobalProjectile>();
		echoGlobal.IsSpawnedEchoProjectile = true;
		echoGlobal.IsEchoDamageProjectile = true;
		echoGlobal.IsEchoVisualProjectile = true;
		echoGlobal.EchoFractureTier = (byte)fractureTier;
		echoGlobal.EchoDamageMultiplier = GetFractureDamageMultiplier(fractureTier);
		SpawnFractureSpawnEffect(fracturedProjectile, fractureTier);
		fracturedProjectile.netUpdate = true;
	}

	private static float GetFractureDamageMultiplier(int fractureTier)
	{
		return fractureTier switch {
			2 => EchoingPrefix.FractureIIDamageMultiplier,
			>= 3 => EchoingPrefix.FractureIIIDamageMultiplier,
			_ => EchoingPrefix.FractureIDamageMultiplier
		};
	}

	private static float GetFractureAngleVarianceDegrees(int fractureTier)
	{
		return fractureTier switch {
			2 => EchoingPrefix.FractureIIAngleVarianceDegrees,
			>= 3 => EchoingPrefix.FractureIIIAngleVarianceDegrees,
			_ => EchoingPrefix.FractureIAngleVarianceDegrees
		};
	}

	private static void SpawnFractureSpawnEffect(Projectile projectile, int fractureTier)
	{
		Color fractureColor = WeaponPrefixVisuals.GetFracturedTint(fractureTier);
		int dustCount = fractureTier switch {
			2 => 7,
			>= 3 => 8,
			_ => 6
		};

		Lighting.AddLight(projectile.Center, fractureColor.ToVector3() * (0.14f + 0.03f * fractureTier));
		for (int i = 0; i < dustCount; i++) {
			Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(0.7f + fractureTier * 0.1f, 1.6f + fractureTier * 0.15f);
			Dust dust = Dust.NewDustPerfect(projectile.Center, WeaponPrefixVisuals.EchoDustType, velocity, 0, fractureColor, 0.72f + 0.08f * fractureTier);
			dust.noGravity = true;
			dust.fadeIn = 0.9f + 0.02f * fractureTier;
		}

		if (projectile.owner == Main.myPlayer && fractureTier == 1) {
			SoundEngine.PlaySound(SoundID.Item8, projectile.Center);
		}
	}
}
