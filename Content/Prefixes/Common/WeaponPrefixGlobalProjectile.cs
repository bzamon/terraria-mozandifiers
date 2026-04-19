using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mozandifiers.Common.Combat;
using Mozandifiers.Common.Config;
using Mozandifiers.Common.Players;
using Mozandifiers.Content.Prefixes.Weapons;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Mozandifiers.Content.Prefixes.Common;

public sealed class WeaponPrefixGlobalProjectile : GlobalProjectile
{
	private static readonly Color EchoTint = new(70, 95, 120, 52);
	private static readonly Color EchoAfterimageTint = new(90, 105, 130, 28);

	public override bool InstancePerEntity => true;

	public bool IsEchoDamageProjectile { get; set; }
	public bool IsEchoVisualProjectile { get; set; }
	public bool IsSpawnedEchoProjectile { get; set; }
	public bool IsSanguineProjectile { get; set; }
	public bool IsRadiantProjectile { get; set; }
	public bool IsStormforgedProjectile { get; set; }
	public bool IsSiphoningProjectile { get; set; }
	public bool HasGrantedSiphoningMana { get; set; }
	public int SiphoningManaRestoreAmount { get; set; }
	public bool IsCatalyticProjectile { get; set; }
	public bool IsHeadshotProjectile { get; set; }
	public Vector2 HeadshotSpawnPosition { get; set; }
	public bool IsSpinboundProjectile { get; set; }
	public Vector2 SpinboundSpawnPosition { get; set; }
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
	private int pendingCatalyticConsumeTargetWhoAmI = -1;

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

			if (item.prefix == ModContent.PrefixType<SpinboundPrefix>()
				&& Main.player[projectile.owner].GetModPlayer<SpinboundPlayer>().RegisterSpinboundShot(item)) {
				IsSpinboundProjectile = true;
				SpinboundSpawnPosition = projectile.Center;
				SpinboundReferenceSpeed = projectile.velocity.Length();
				GoldenSpinTier = EvaluateGoldenSpinTier(Main.player[projectile.owner], projectile);
				SpawnSpinboundReleaseEffect(projectile);
			}

			if (TryGetShiftedSimulation(item, projectile.owner, out ShiftingSimulationId shiftedSimulationId)
				&& shiftedSimulationId == ShiftingSimulationId.Echoing) {
				TrySpawnEchoProjectile(
					projectile,
					ShiftingSimulationCatalog.GetShiftedEchoChance(),
					ShiftingSimulationCatalog.GetShiftedEchoDamageMultiplier());
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
			WeaponPrefixGlobalItem.ApplyRadiantHitEffects(target, ShiftingSimulationCatalog.GetShiftedRadiantDurationTicks());
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
	}

	public override void AI(Projectile projectile)
	{
		if (IsRadiantProjectile) {
			Lighting.AddLight(projectile.Center, 0.75f, 0.55f, 0.18f);
		}

		if (ShiftedSimulationId == ShiftingSimulationId.Radiant) {
			float lightMultiplier = ShiftingSimulationCatalog.GetShiftedRadiantLightMultiplier();
			Lighting.AddLight(projectile.Center, 0.75f * lightMultiplier, 0.55f * lightMultiplier, 0.18f * lightMultiplier);
		}

		if (IsSpinboundProjectile) {
			ApplySpinboundStabilization(projectile);
			SpawnSpinboundTrail(projectile);
		}

		if (IsShiftedSpinboundProjectile) {
			ApplySpinboundStabilization(projectile, ShiftedSpinboundReferenceSpeed);
			SpawnSpinboundTrail(projectile);
		}

		if (!IsEchoVisualProjectile) {
			return;
		}

		Lighting.AddLight(projectile.Center, 0.015f, 0.015f, 0.04f);

		if (projectile.numUpdates == 0 && projectile.timeLeft % 6 == 0) {
			Dust dust = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, DustID.Shadowflame);
			dust.noGravity = true;
			dust.velocity = projectile.velocity * 0.05f;
			dust.scale = 0.55f;
			dust.fadeIn = 0.65f;
		}

		if (projectile.numUpdates == 0 && projectile.timeLeft % 7 == 0) {
			Vector2 orbitOffset = Main.rand.NextVector2CircularEdge(projectile.width * 0.55f, projectile.height * 0.55f);
			Dust auraDust = Dust.NewDustPerfect(projectile.Center + orbitOffset, DustID.Shadowflame);
			auraDust.noGravity = true;
			auraDust.velocity = projectile.velocity * 0.02f;
			auraDust.scale = 0.6f;
			auraDust.fadeIn = 0.7f;
		}
	}

	public override Color? GetAlpha(Projectile projectile, Color lightColor)
	{
		if (!IsEchoVisualProjectile) {
			return null;
		}

		return EchoTint;
	}

	public override bool PreDraw(Projectile projectile, ref Color lightColor)
	{
		if (!IsEchoVisualProjectile) {
			return true;
		}

		Texture2D texture = TextureAssets.Projectile[projectile.type].Value;
		Rectangle frame = texture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
		Vector2 origin = frame.Size() * 0.5f;
		Vector2 basePosition = GetProjectileDrawPosition(projectile);
		float pulse = 1f + 0.015f * System.MathF.Sin(Main.GlobalTimeWrappedHourly * 10f);
		Color preDrawTint = Color.Lerp(lightColor, EchoTint, 0.2f);
		Color underlayTint = EchoAfterimageTint * 0.4f;

		DrawEchoSprite(projectile, texture, frame, origin, basePosition, underlayTint, projectile.rotation, projectile.scale * (pulse + 0.01f));
		DrawEchoSprite(projectile, texture, frame, origin, basePosition - projectile.velocity * 0.1f, underlayTint * 0.6f, projectile.rotation, projectile.scale * pulse);

		lightColor = preDrawTint;
		return true;
	}

	public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
	{
		if (IsEchoDamageProjectile) {
			modifiers.SourceDamage *= EchoDamageMultiplier > 0f ? EchoDamageMultiplier : EchoingPrefix.EchoDamageMultiplier;
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
				pendingCatalyticConsumeTargetWhoAmI = target.whoAmI;
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
		binaryWriter.Write(SpinboundSpawnPosition.X);
		binaryWriter.Write(SpinboundSpawnPosition.Y);
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
		SpinboundSpawnPosition = new Vector2(binaryReader.ReadSingle(), binaryReader.ReadSingle());
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

	private static void SpawnSpinboundReleaseEffect(Projectile projectile)
	{
		if (projectile == null || !projectile.active) {
			return;
		}

		Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX * projectile.direction);
		Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
		for (int i = -1; i <= 1; i++) {
			Vector2 offset = side * (10f * i);
			Dust dust = Dust.NewDustPerfect(projectile.Center + offset, DustID.GoldFlame);
			dust.noGravity = true;
			dust.velocity = forward * 1.6f + side * (0.8f * i);
			dust.scale = 0.95f;
			dust.fadeIn = 1.1f;
		}
	}

	private static void SpawnSpinboundTrail(Projectile projectile)
	{
		if (projectile == null
			|| !projectile.active
			|| projectile.numUpdates != 0
			|| projectile.timeLeft % 3 != 0) {
			return;
		}

		Vector2 tangent = projectile.velocity.SafeNormalize(Vector2.UnitX * projectile.direction).RotatedBy(MathHelper.PiOver2);
		for (int i = -1; i <= 1; i += 2) {
			Dust dust = Dust.NewDustPerfect(projectile.Center + tangent * (6f * i), DustID.GoldFlame);
			dust.noGravity = true;
			dust.velocity = projectile.velocity * 0.08f + tangent * (0.45f * i);
			dust.scale = 0.7f;
			dust.fadeIn = 0.9f;
		}
	}

	private static int EvaluateGoldenSpinTier(Player owner, Projectile projectile)
	{
		if (owner == null || !owner.active || projectile == null || !projectile.active) {
			return 0;
		}

		Vector2 spawnOffset = projectile.Center - owner.Center;
		Vector2 velocity = projectile.velocity;
		float speed = velocity.Length();
		float spawnDistance = spawnOffset.Length();
		if (speed <= 0f || spawnDistance <= 0f) {
			return 0;
		}

		int tier = 0;
		if (MatchesGoldenRatio(spawnDistance / speed, SpinboundPrefix.DistanceToSpeedTolerance)) {
			tier++;
		}

		if (MatchesGoldenRatioComponentRatio(System.MathF.Abs(spawnOffset.X), System.MathF.Abs(spawnOffset.Y))) {
			tier++;
		}

		if (MatchesGoldenRatioComponentRatio(System.MathF.Abs(velocity.X), System.MathF.Abs(velocity.Y))) {
			tier++;
		}

		Vector2 offsetDirection = spawnOffset.SafeNormalize(Vector2.Zero);
		Vector2 launchDirection = velocity.SafeNormalize(Vector2.Zero);
		float alignment = System.MathF.Abs(Vector2.Dot(offsetDirection, launchDirection));
		if (MatchesGoldenValue(alignment, SpinboundPrefix.InverseGoldenRatio, SpinboundPrefix.AlignmentTolerance)) {
			tier++;
		}

		float perpendicularAlignment = System.MathF.Abs(offsetDirection.X * launchDirection.Y - offsetDirection.Y * launchDirection.X);
		if (MatchesGoldenValue(perpendicularAlignment, SpinboundPrefix.InverseGoldenRatio, SpinboundPrefix.AlignmentTolerance)) {
			tier++;
		}

		return System.Math.Min(SpinboundPrefix.MaxGoldenSpinTier, tier);
	}

	private static bool MatchesGoldenRatio(float ratio, float tolerance)
	{
		return MatchesGoldenValue(ratio, SpinboundPrefix.GoldenRatio, tolerance)
			|| MatchesGoldenValue(ratio, SpinboundPrefix.InverseGoldenRatio, tolerance);
	}

	private static bool MatchesGoldenRatioComponentRatio(float numerator, float denominator)
	{
		if (denominator < SpinboundPrefix.MinimumGoldenRatioDenominator) {
			return false;
		}

		return MatchesGoldenRatio(numerator / denominator, SpinboundPrefix.ComponentRatioTolerance);
	}

	private static bool MatchesGoldenValue(float value, float target, float tolerance)
	{
		return System.MathF.Abs(value - target) <= tolerance;
	}

	private static float GetSpinboundSpecialMultiplier(int goldenSpinTier)
	{
		return System.Math.Min(SpinboundPrefix.MaxGoldenSpinMultiplier, 1 + goldenSpinTier);
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
        pendingCatalyticConsumeTargetWhoAmI = target.whoAmI;

	}

	private static bool IsCatalyticEligibleProjectile(Projectile projectile)
	{
		return projectile != null
			&& projectile.active;
	}

	
    private void ClearPendingCatalyticConsume()
    {
        pendingCatalyticConsume = false;
        pendingCatalyticConsumeTargetWhoAmI = -1;
		
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
				if (item != null && Main.player[projectile.owner].GetModPlayer<SpinboundPlayer>().RegisterSpinboundShot(item, true)) {
					IsShiftedSpinboundProjectile = true;
					ShiftedSpinboundReferenceSpeed = projectile.velocity.Length();
					ShiftedGoldenSpinTier = EvaluateGoldenSpinTier(Main.player[projectile.owner], projectile);
					SpawnSpinboundReleaseEffect(projectile);
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
		echoedProjectile.netUpdate = true;
	}
}
