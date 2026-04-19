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
	public bool IsSanguineProjectile { get; set; }
	public bool IsRadiantProjectile { get; set; }
	public bool IsBreachingProjectile { get; set; }
	public bool IsTemporalProjectile { get; set; }
	public bool IsStormforgedProjectile { get; set; }
	public bool IsSiphoningProjectile { get; set; }
	public bool HasGrantedSiphoningMana { get; set; }
	public int SiphoningManaRestoreAmount { get; set; }
	public bool IsCatalyticProjectile { get; set; }
	public bool IsHeadshotProjectile { get; set; }
	public Vector2 HeadshotSpawnPosition { get; set; }
	public bool IsSpinboundProjectile { get; set; }
	public float BreachImpactScale { get; set; }
	public float SpinboundReferenceSpeed { get; set; }
	public int GoldenSpinTier { get; set; }
	public bool HasConsumedSpinboundHitBonus { get; set; }
	public float EchoDamageMultiplier { get; set; }
	internal ShiftingSimulationId ShiftedSimulationId { get; set; }
	internal float ShiftedStrengthMultiplier { get; set; }
	internal Vector2 ShiftedHeadshotSpawnPosition { get; set; }
	internal int ShiftedSiphoningManaRestoreAmount { get; set; }
	internal bool HasGrantedShiftedSiphoningMana { get; set; }
	internal bool IsShiftedSpinboundProjectile { get; set; }
	internal float ShiftedSpinboundReferenceSpeed { get; set; }
	internal int ShiftedGoldenSpinTier { get; set; }
	internal bool HasConsumedShiftedSpinboundHitBonus { get; set; }

	private bool pendingCatalyticConsume;

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
			}

			if (item.prefix == ModContent.PrefixType<RadiantPrefix>()) {
				IsRadiantProjectile = true;
			}

			if (item.prefix == ModContent.PrefixType<BreachingPrefix>()) {
				IsBreachingProjectile = true;
				BreachImpactScale = BreachingPrefix.GetBreachImpactMultiplier(item);
			}

			if (item.prefix == ModContent.PrefixType<TemporalPrefix>()) {
				IsTemporalProjectile = true;
			}

			if (item.prefix == ModContent.PrefixType<StormforgedPrefix>()) {
				IsStormforgedProjectile = true;
			}

			if (item.prefix == ModContent.PrefixType<SiphoningPrefix>()) {
				IsSiphoningProjectile = true;
				SiphoningManaRestoreAmount = WeaponPrefixGlobalItem.GetSiphoningManaRestoreAmount(item);
			}

			if (item.prefix == ModContent.PrefixType<CatalyticPrefix>()) {
				IsCatalyticProjectile = true;
			}

			if (item.prefix == ModContent.PrefixType<HeadshotPrefix>()) {
				IsHeadshotProjectile = true;
				HeadshotSpawnPosition = projectile.Center;
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
					TrySpawnEchoProjectile(
						projectile,
						ShiftingSimulationCatalog.GetShiftedEchoChance(),
						ShiftingSimulationCatalog.GetShiftedEchoDamageMultiplier());
				}
			}
		}
		else if (source is EntitySource_Parent parentSource && parentSource.Entity is Projectile parentProjectile) {
			WeaponPrefixGlobalProjectile parentGlobal = parentProjectile.GetGlobalProjectile<WeaponPrefixGlobalProjectile>();
			if (parentGlobal.IsSanguineProjectile) {
				IsSanguineProjectile = true;
			}

			if (parentGlobal.IsRadiantProjectile) {
				IsRadiantProjectile = true;
			}

			if (parentGlobal.IsStormforgedProjectile) {
				IsStormforgedProjectile = true;
			}

			if (parentGlobal.IsHeadshotProjectile) {
				IsHeadshotProjectile = true;
				HeadshotSpawnPosition = projectile.Center;
			}
		}
		else {
			return;
		}

		if (item == null || item.prefix != ModContent.PrefixType<EchoingPrefix>()) {
			return;
		}

		TrySpawnEchoProjectile(projectile, PrefixTuningConfig.Instance.EchoChance, EchoingPrefix.EchoDamageMultiplier);
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
				WeaponPrefixGlobalItem.TryApplySanguineHeal(owner, damageDone);
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

		if (IsSiphoningProjectile
			&& !HasGrantedSiphoningMana
			&& damageDone > 0
			&& target.active) {
			Player owner = Main.player[projectile.owner];
			if (owner.active && !owner.dead) {
				int manaRestored = WeaponPrefixGlobalItem.TryApplySiphoningManaRestore(owner, SiphoningManaRestoreAmount);
				if (manaRestored > 0) {
					HasGrantedSiphoningMana = true;
					projectile.netUpdate = true;
				}
			}
		}

		if (ShiftedSimulationId == ShiftingSimulationId.Sanguine) {
			Player owner = Main.player[projectile.owner];
			if (owner.active && !owner.dead) {
				WeaponPrefixGlobalItem.TryApplySanguineHeal(
					owner,
					damageDone,
					ShiftingSimulationCatalog.GetShiftedSanguineLifeStealMultiplier(),
					ShiftingSimulationCatalog.GetShiftedSanguineHealCapPerSecond());
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

		if (ShiftedSimulationId == ShiftingSimulationId.Siphoning
			&& !HasGrantedShiftedSiphoningMana
			&& damageDone > 0
			&& target.active) {
			Player owner = Main.player[projectile.owner];
			if (owner.active && !owner.dead) {
				int manaRestored = WeaponPrefixGlobalItem.TryApplySiphoningManaRestore(
					owner,
					ShiftedSiphoningManaRestoreAmount,
					ShiftingSimulationCatalog.GetShiftedSiphoningMaxRestorePerSecond());
				if (manaRestored > 0) {
					HasGrantedShiftedSiphoningMana = true;
					projectile.netUpdate = true;
				}
			}
		}

		if (damageDone > 0 && target.active) {
			if (IsHeadshotProjectile) {
				EmitHeadshotFeedback(projectile, target, Vector2.Distance(HeadshotSpawnPosition, projectile.Center));
			}

			if (ShiftedSimulationId == ShiftingSimulationId.Headshot) {
				EmitHeadshotFeedback(
					projectile,
					target,
					Vector2.Distance(ShiftedHeadshotSpawnPosition, projectile.Center),
					true);
			}
		}
	}

	public override void AI(Projectile projectile)
	{
		if (IsRadiantProjectile) {
			UpdateRadiantProjectileVisual(projectile, 1f);
		}

		if (IsTemporalProjectile) {
			UpdateTemporalProjectileVisual(projectile, 1f);
		}

		if (ShiftedSimulationId == ShiftingSimulationId.Radiant) {
			UpdateRadiantProjectileVisual(projectile, ShiftingSimulationCatalog.GetShiftedRadiantLightMultiplier());
		}

		if (ShiftedSimulationId == ShiftingSimulationId.Temporal) {
			UpdateTemporalProjectileVisual(projectile, ShiftingPrefix.ShiftingStrengthMultiplier);
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

		if (projectile.numUpdates == 0 && projectile.timeLeft % 6 == 0) {
			Dust dust = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, WeaponPrefixVisuals.EchoDustType);
			dust.noGravity = true;
			dust.velocity = projectile.velocity * 0.08f;
			dust.scale = 0.7f;
			dust.fadeIn = 0.85f;
		}

		if (projectile.numUpdates == 0 && projectile.timeLeft % 7 == 0) {
			Vector2 orbitOffset = Main.rand.NextVector2CircularEdge(projectile.width * 0.55f, projectile.height * 0.55f);
			Dust auraDust = Dust.NewDustPerfect(projectile.Center + orbitOffset, WeaponPrefixVisuals.EchoDustType);
			auraDust.noGravity = true;
			auraDust.velocity = projectile.velocity * 0.02f;
			auraDust.scale = 0.75f;
			auraDust.fadeIn = 0.9f;
		}
	}

	public override Color? GetAlpha(Projectile projectile, Color lightColor)
	{
		if (!IsEchoVisualProjectile) {
			return null;
		}

		return WeaponPrefixVisuals.EchoTint;
	}

	public override bool PreDraw(Projectile projectile, ref Color lightColor)
	{
		if (IsEchoVisualProjectile) {
			Texture2D texture = TextureAssets.Projectile[projectile.type].Value;
			Rectangle frame = texture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
			Vector2 origin = frame.Size() * 0.5f;
			Vector2 basePosition = GetProjectileDrawPosition(projectile);
			float pulse = 1f + 0.015f * System.MathF.Sin(Main.GlobalTimeWrappedHourly * 10f);
			Color preDrawTint = Color.Lerp(lightColor, WeaponPrefixVisuals.EchoTint, 0.2f);
			Color underlayTint = WeaponPrefixVisuals.EchoAfterimageTint * 0.4f;

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

		return true;
	}

	public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
	{
		if (IsEchoDamageProjectile) {
			modifiers.SourceDamage *= EchoDamageMultiplier > 0f ? EchoDamageMultiplier : EchoingPrefix.EchoDamageMultiplier;
		}

		if (ShiftedSimulationId == ShiftingSimulationId.Breaching) {
			modifiers.ArmorPenetration += ShiftingSimulationCatalog.GetShiftedArmorPenetrationBonus(ShiftedSimulationId);
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

		if (!IsHeadshotProjectile) {
			goto Spinbound;
		}

		float travelledDistance = Vector2.Distance(HeadshotSpawnPosition, projectile.Center);
		float distanceDamageBonus = GetHeadshotDistanceDamageBonus(travelledDistance);
		if (distanceDamageBonus > 0f) {
			modifiers.SourceDamage *= 1f + distanceDamageBonus;
		}

		modifiers.CritDamage += travelledDistance >= HeadshotPrefix.LongRangeCritThresholdPixels
			? HeadshotPrefix.LongRangeCritDamageBonus
			: HeadshotPrefix.CritDamageBonus;

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

		if (ShiftedSimulationId != ShiftingSimulationId.Headshot) {
			return;
		}

		float shiftedTravelledDistance = Vector2.Distance(ShiftedHeadshotSpawnPosition, projectile.Center);
		float shiftedDistanceDamageBonus = GetHeadshotDistanceDamageBonus(
			shiftedTravelledDistance,
			ShiftingSimulationCatalog.GetShiftedHeadshotMaxDistanceDamageBonus());
		if (shiftedDistanceDamageBonus > 0f) {
			modifiers.SourceDamage *= 1f + shiftedDistanceDamageBonus;
		}

		modifiers.CritDamage += shiftedTravelledDistance >= HeadshotPrefix.LongRangeCritThresholdPixels
			? ShiftingSimulationCatalog.GetShiftedHeadshotLongRangeCritDamageBonus()
			: ShiftingSimulationCatalog.GetShiftedHeadshotCritDamageBonus();
	}

	public override void ModifyHitPlayer(Projectile projectile, Player target, ref Player.HurtModifiers modifiers)
	{
		if (IsEchoDamageProjectile) {
			modifiers.SourceDamage *= EchoDamageMultiplier > 0f ? EchoDamageMultiplier : EchoingPrefix.EchoDamageMultiplier;
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
		bitWriter.WriteBit(IsTemporalProjectile);
		bitWriter.WriteBit(IsStormforgedProjectile);
		bitWriter.WriteBit(IsSiphoningProjectile);
		bitWriter.WriteBit(HasGrantedSiphoningMana);
		bitWriter.WriteBit(IsCatalyticProjectile);
		bitWriter.WriteBit(IsHeadshotProjectile);
		bitWriter.WriteBit(IsSpinboundProjectile);
		bitWriter.WriteBit(HasConsumedSpinboundHitBonus);
		bitWriter.WriteBit(HasGrantedShiftedSiphoningMana);
		bitWriter.WriteBit(IsShiftedSpinboundProjectile);
		bitWriter.WriteBit(HasConsumedShiftedSpinboundHitBonus);
		binaryWriter.Write(EchoDamageMultiplier);
		binaryWriter.Write(SiphoningManaRestoreAmount);
		binaryWriter.Write(HeadshotSpawnPosition.X);
		binaryWriter.Write(HeadshotSpawnPosition.Y);
		binaryWriter.Write(BreachImpactScale);
		binaryWriter.Write(SpinboundReferenceSpeed);
		binaryWriter.Write(GoldenSpinTier);
		binaryWriter.Write((byte)ShiftedSimulationId);
		binaryWriter.Write(ShiftedStrengthMultiplier);
		binaryWriter.Write(ShiftedHeadshotSpawnPosition.X);
		binaryWriter.Write(ShiftedHeadshotSpawnPosition.Y);
		binaryWriter.Write(ShiftedSiphoningManaRestoreAmount);
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
		IsTemporalProjectile = bitReader.ReadBit();
		IsStormforgedProjectile = bitReader.ReadBit();
		IsSiphoningProjectile = bitReader.ReadBit();
		HasGrantedSiphoningMana = bitReader.ReadBit();
		IsCatalyticProjectile = bitReader.ReadBit();
		IsHeadshotProjectile = bitReader.ReadBit();
		IsSpinboundProjectile = bitReader.ReadBit();
		HasConsumedSpinboundHitBonus = bitReader.ReadBit();
		HasGrantedShiftedSiphoningMana = bitReader.ReadBit();
		IsShiftedSpinboundProjectile = bitReader.ReadBit();
		HasConsumedShiftedSpinboundHitBonus = bitReader.ReadBit();
		EchoDamageMultiplier = binaryReader.ReadSingle();
		SiphoningManaRestoreAmount = binaryReader.ReadInt32();
		HeadshotSpawnPosition = new Vector2(binaryReader.ReadSingle(), binaryReader.ReadSingle());
		BreachImpactScale = binaryReader.ReadSingle();
		SpinboundReferenceSpeed = binaryReader.ReadSingle();
		GoldenSpinTier = binaryReader.ReadInt32();
		ShiftedSimulationId = (ShiftingSimulationId)binaryReader.ReadByte();
		ShiftedStrengthMultiplier = binaryReader.ReadSingle();
		ShiftedHeadshotSpawnPosition = new Vector2(binaryReader.ReadSingle(), binaryReader.ReadSingle());
		ShiftedSiphoningManaRestoreAmount = binaryReader.ReadInt32();
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

	private static float GetHeadshotDistanceDamageBonus(float travelledDistance)
	{
		return GetHeadshotDistanceDamageBonus(travelledDistance, HeadshotPrefix.MaxDistanceDamageBonus);
	}

	private static float GetHeadshotDistanceDamageBonus(float travelledDistance, float maxDistanceDamageBonus)
	{
		if (travelledDistance <= 0f) {
			return 0f;
		}

		float distanceRatio = System.MathF.Min(1f, travelledDistance / HeadshotPrefix.FullDistanceBonusPixels);
		return distanceRatio * maxDistanceDamageBonus;
	}

	private static void EmitHeadshotFeedback(Projectile projectile, NPC target, float travelledDistance, bool shifted = false)
	{
		if (projectile.owner != Main.myPlayer
			|| projectile.owner < 0
			|| projectile.owner >= Main.maxPlayers) {
			return;
		}

		Player owner = Main.player[projectile.owner];
		if (!owner.active || owner.dead) {
			return;
		}

		HeadshotFeedbackPlayer feedbackPlayer = owner.GetModPlayer<HeadshotFeedbackPlayer>();
		bool longRange = travelledDistance >= HeadshotPrefix.LongRangeCritThresholdPixels;
		float visualMultiplier = shifted ? 1.12f : 1f;

		if (longRange) {
			if (feedbackPlayer.CanEmitLongRangeVisual()) {
				SpawnHeadshotImpactEffect(target.Center, projectile.velocity, true, visualMultiplier);
			}

			if (feedbackPlayer.CanPlayLongRangeSound()) {
				SoundEngine.PlaySound(SoundID.Item153, target.Center);
			}

			return;
		}

		if (feedbackPlayer.CanEmitNormalVisual()) {
			SpawnHeadshotImpactEffect(target.Center, projectile.velocity, false, visualMultiplier);
		}
	}

	private static void SpawnHeadshotImpactEffect(Vector2 position, Vector2 projectileVelocity, bool longRange, float visualMultiplier)
	{
		Vector2 direction = projectileVelocity.SafeNormalize(Vector2.UnitX);
		Vector2 tangent = direction.RotatedBy(MathHelper.PiOver2);
		Color lightColor = longRange ? WeaponPrefixVisuals.HeadshotLongRangeTint : WeaponPrefixVisuals.HeadshotTint;
		float lightStrength = longRange ? 0.32f : 0.18f;
		Lighting.AddLight(position, lightColor.ToVector3() * (lightStrength * visualMultiplier));

		int burstCount = longRange ? 8 : 4;
		for (int i = 0; i < burstCount; i++) {
			float side = i % 2 == 0 ? -1f : 1f;
			float lane = 1f + i / 2f;
			Vector2 velocity = direction * (longRange ? 1.35f : 0.8f) + tangent * (0.3f * lane * side);
			Dust dust = Dust.NewDustPerfect(
				position + tangent * (3f * lane * side),
				longRange && i % 3 == 0 ? WeaponPrefixVisuals.HeadshotLongRangeDustType : WeaponPrefixVisuals.HeadshotDustType,
				velocity,
				0,
				lightColor,
				(longRange ? 1f : 0.8f) * visualMultiplier);
			dust.noGravity = true;
			dust.fadeIn = longRange ? 0.95f : 0.85f;
		}

		if (!longRange) {
			return;
		}

		for (int i = 0; i < 6; i++) {
			float angle = MathHelper.TwoPi * i / 6f;
			Vector2 offset = angle.ToRotationVector2() * 8f;
			Dust dust = Dust.NewDustPerfect(
				position + offset,
				i % 2 == 0 ? WeaponPrefixVisuals.HeadshotLongRangeDustType : WeaponPrefixVisuals.HeadshotDustType,
				offset.SafeNormalize(Vector2.UnitX) * 0.9f,
				0,
				WeaponPrefixVisuals.HeadshotLongRangeTint,
				0.95f * visualMultiplier);
			dust.noGravity = true;
			dust.fadeIn = 0.95f;
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
			case ShiftingSimulationId.Headshot:
				ShiftedHeadshotSpawnPosition = projectile.Center;
				break;
			case ShiftingSimulationId.Siphoning:
				ShiftedSiphoningManaRestoreAmount = item != null
					? WeaponPrefixGlobalItem.GetSiphoningManaRestoreAmount(
						item,
						ShiftingSimulationCatalog.GetShiftedSiphoningManaRestoreRatio(),
						ShiftingSimulationCatalog.GetShiftedSiphoningMaxRestorePerHit())
					: parentGlobal?.ShiftedSiphoningManaRestoreAmount ?? 0;
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
			or ShiftingSimulationId.Stormforged
			or ShiftingSimulationId.Siphoning
			or ShiftingSimulationId.Headshot;
	}

	private void TrySpawnEchoProjectile(Projectile projectile, float procChance, float echoDamageMultiplier)
	{
		if (projectile == null || !projectile.active || Main.rand.NextFloat() >= procChance) {
			return;
		}

		bool sourceUsesLocalNPCImmunity = projectile.usesLocalNPCImmunity;
		int sourceLocalNPCHitCooldown = projectile.localNPCHitCooldown;
		bool sourceUsesIDStaticNPCImmunity = projectile.usesIDStaticNPCImmunity;
		int sourceIDStaticNPCHitCooldown = projectile.idStaticNPCHitCooldown;

		NormalizeEchoHitTracking(
			projectile,
			sourceUsesLocalNPCImmunity,
			sourceLocalNPCHitCooldown,
			sourceUsesIDStaticNPCImmunity,
			sourceIDStaticNPCHitCooldown);

		Vector2 echoedVelocity = projectile.velocity.RotatedByRandom(MathHelper.ToRadians(5f));
		int echoIndex = Projectile.NewProjectile(
			projectile.GetSource_FromThis("MozandifiersEchoProjectile"),
			projectile.Center,
			echoedVelocity,
			projectile.type,
			projectile.damage,
			projectile.knockBack,
			projectile.owner);

		if (echoIndex < 0 || echoIndex >= Main.maxProjectiles) {
			return;
		}

		Projectile echoedProjectile = Main.projectile[echoIndex];
		NormalizeEchoHitTracking(
			echoedProjectile,
			sourceUsesLocalNPCImmunity,
			sourceLocalNPCHitCooldown,
			sourceUsesIDStaticNPCImmunity,
			sourceIDStaticNPCHitCooldown);

		WeaponPrefixGlobalProjectile echoGlobal = echoedProjectile.GetGlobalProjectile<WeaponPrefixGlobalProjectile>();
		echoGlobal.IsSpawnedEchoProjectile = true;
		echoGlobal.IsEchoDamageProjectile = true;
		echoGlobal.IsEchoVisualProjectile = true;
		echoGlobal.EchoDamageMultiplier = echoDamageMultiplier;
		SpawnEchoSpawnEffect(echoedProjectile);
		echoedProjectile.netUpdate = true;
	}

	private static void SpawnEchoSpawnEffect(Projectile projectile)
	{
		for (int i = 0; i < 6; i++) {
			Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(0.8f, 1.8f);
			Dust dust = Dust.NewDustPerfect(projectile.Center, WeaponPrefixVisuals.EchoDustType, velocity);
			dust.noGravity = true;
			dust.scale = 0.8f;
			dust.fadeIn = 0.85f;
		}
	}
}
