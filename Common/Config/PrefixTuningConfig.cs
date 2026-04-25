using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace Mozandifiers.Common.Config;

public sealed class PrefixTuningConfig : ModConfig
{
	public static PrefixTuningConfig Instance => ModContent.GetInstance<PrefixTuningConfig>();

	public override ConfigScope Mode => ConfigScope.ServerSide;

	[Header("Overview")]
	[SeparatePage]
	public AttunedSettings Attuned { get; set; } = new();

	[SeparatePage]
	public AwakenedSettings Awakened { get; set; } = new();

	[SeparatePage]
	public BreachingSettings Breaching { get; set; } = new();

	[SeparatePage]
	public CatalyticSettings Catalytic { get; set; } = new();

	[SeparatePage]
	public DeadeyeSettings Deadeye { get; set; } = new();

	[SeparatePage]
	public DesperateSettings Desperate { get; set; } = new();

	[SeparatePage]
	public FracturedSettings Fractured { get; set; } = new();

	[SeparatePage]
	public RadiantSettings Radiant { get; set; } = new();

	[SeparatePage]
	public ShiftingSettings Shifting { get; set; } = new();

	[SeparatePage]
	public SkirmishingSettings Skirmishing { get; set; } = new();

	[SeparatePage]
	public SpinboundSettings Spinbound { get; set; } = new();

	[SeparatePage]
	public StormforgedSettings Stormforged { get; set; } = new();

	[SeparatePage]
	public TemporalSettings Temporal { get; set; } = new();

	[SeparatePage]
	public VampiricSettings Vampiric { get; set; } = new();

	public int AttunedMaxResonanceStacks => Attuned.MaxResonanceStacks;
	public int AttunedResonanceDecayTicks => Attuned.ResonanceDecayTicks;
	public int AttunedResonanceHitGainCooldownTicks => Attuned.ResonanceHitGainCooldownTicks;
	public float AttunedEmpoweredCastShootSpeedMultiplier => Attuned.EmpoweredCastShootSpeedPercent / 100f;

	public float AwakenedDormantUseSpeedBonus => Awakened.DormantUseSpeedPercent / 100f;
	public int AwakenedDormantCritBonus => Awakened.DormantCritBonus;
	public float AwakenedDamageBonus => Awakened.AwakenedDamagePercent / 100f;
	public float AwakenedScaleBonus => Awakened.AwakenedScalePercent / 100f;
	public float AwakenedUseSpeedBonus => Awakened.AwakenedUseSpeedPercent / 100f;
	public float AwakenedSweetSpotKnockbackMultiplier => Awakened.SweetSpotKnockbackPercent / 100f;
	public int AwakenedDesiredBuildTicks => Awakened.DesiredBuildTicks;
	public int AwakenedDurationTicks => Awakened.AwakenedDurationTicks;
	public int AwakenedRecoveryLockoutTicks => Awakened.RecoveryLockoutTicks;
	public float AwakenedHpBonusDamageRatio => Awakened.HpBonusDamageRatioPerThousand / 1000f;
	public int AwakenedMinBonusDamage => Awakened.MinBonusDamage;
	public int AwakenedMaxBonusDamageAgainstNormalEnemies => Awakened.MaxBonusDamageAgainstNormalEnemies;
	public float AwakenedBossBonusDamageCapRatioPerSecond => Awakened.BossBonusDamageCapRatioPercent / 1000f;
	public int AwakenedMinBossBonusDamageCapPerSecond => Awakened.MinBossBonusDamageCapPerSecond;
	public int AwakenedMaxBossBonusDamageCapPerSecond => Awakened.MaxBossBonusDamageCapPerSecond;
	public int AwakenedBossBonusDamageWindowTicks => Awakened.BossBonusDamageWindowTicks;

	public float BreachingDamageMultiplier => Breaching.DamagePercent / 100f;
	public float BreachingKnockbackMultiplier => Breaching.KnockbackPercent / 100f;
	public float BreachingUseTimeMultiplierValue => Breaching.UseTimePercent / 100f;
	public int BreachingCritBonus => Breaching.CritBonus;
	public int BreachingArmorPenetrationValue => Breaching.ArmorPenetration;
	public int BreachingDefenseThreshold => Breaching.DefenseThreshold;
	public int BreachingDamageThreshold => Breaching.DamageThreshold;
	public int BreachingSizePercent => Breaching.SizePercent;
	public float BreachingScaleMultiplier => Breaching.SizePercent / 100f;

	public int CatalyticMarkDurationTicks => Catalytic.MarkDurationTicks;
	public int CatalyticMarkArmDelayTicks => Catalytic.MarkArmDelayTicks;
	public float CatalyticConsumeDamageBonus => Catalytic.ConsumeDamagePercent / 100f;

	public int DeadeyeReadyIntervalTicks => Deadeye.ReadyIntervalTicks;
	public int DeadeyeStationaryReadyTicks => Deadeye.StationaryReadyTicks;
	public int DeadeyeProjectileAssignmentLockoutTicks => Deadeye.ProjectileAssignmentLockoutTicks;
	public float DeadeyeStationaryTolerancePixels => Deadeye.StationaryTolerancePixels;
	public float DeadeyeShotVelocityMultiplier => Deadeye.ShotVelocityPercent / 100f;
	public int DeadeyeShotCritChanceBonus => Deadeye.ShotCritChanceBonus;
	public float DeadeyeShotCritDamageBonus => Deadeye.ShotCritDamagePercent / 100f;
	public float DeadeyeCriticalBurstDamageRatio => Deadeye.BurstDamagePercent / 100f;
	public float DeadeyeCriticalBurstRadiusPixels => Deadeye.BurstRadiusPixels;
	public float DeadeyeBossBurstDamageMultiplier => Deadeye.BossBurstDamagePercent / 100f;

	public float DesperateMaxDamageBonus => Desperate.MaxDamagePercent / 100f;
	public int DesperateCritBonus => Desperate.CritBonus;
	public float DesperateMinorLifeThreshold => Desperate.MinorLifeThresholdPercent / 100f;
	public float DesperateSevereLifeThreshold => Desperate.SevereLifeThresholdPercent / 100f;
	public float DesperateCriticalLifeThreshold => Desperate.CriticalLifeThresholdPercent / 100f;
	public int DesperateSurgeCooldownTicks => Desperate.SurgeCooldownTicks;
	public float DesperateSurgeDamageBonus => Desperate.SurgeDamagePercent / 100f;

	public int FractureIChancePercent => Fractured.FractureIChancePercent;
	public int FractureIIChancePercent => Fractured.FractureIIChancePercent;
	public int FractureIIIChancePercent => Fractured.FractureIIIChancePercent;
	public float FractureIChance => Fractured.FractureIChancePercent / 100f;
	public float FractureIIChance => Fractured.FractureIIChancePercent / 100f;
	public float FractureIIIChance => Fractured.FractureIIIChancePercent / 100f;
	public float FractureIDamageMultiplier => Fractured.FractureIDamagePercent / 100f;
	public float FractureIIDamageMultiplier => Fractured.FractureIIDamagePercent / 100f;
	public float FractureIIIDamageMultiplier => Fractured.FractureIIIDamagePercent / 100f;
	public float FractureIAngleVarianceDegrees => Fractured.FractureIAngleVarianceDegrees;
	public float FractureIIAngleVarianceDegrees => Fractured.FractureIIAngleVarianceDegrees;
	public float FractureIIIAngleVarianceDegrees => Fractured.FractureIIIAngleVarianceDegrees;
	public int FractureIIDelayMinFrames => Fractured.FractureIIDelayMinFrames;
	public int FractureIIDelayMaxFrames => Fractured.FractureIIDelayMaxFrames;
	public int FractureIIIDelayMinFrames => Fractured.FractureIIIDelayMinFrames;
	public int FractureIIIDelayMaxFrames => Fractured.FractureIIIDelayMaxFrames;

	public int RadiantOnFireDurationTicks => Radiant.OnFireDurationTicks;

	public int ShiftingShiftDurationTicks => Shifting.ShiftDurationTicks;
	public int ShiftingCombatWindowTicks => Shifting.CombatWindowTicks;
	public float ShiftingStrengthMultiplier => Shifting.StrengthPercent / 100f;

	public int SkirmishWindowTicks => Skirmishing.WindowTicks;
	public float SkirmishingMobilityMoveSpeedBonus => Skirmishing.MoveSpeedPercent / 100f;
	public float SkirmishingRunAccelerationMultiplier => Skirmishing.RunAccelerationPercent / 100f;
	public float SkirmishingMaxRunSpeedBonus => Skirmishing.MaxRunSpeedBonus / 100f;
	public float SkirmishingFollowUpShootSpeedMultiplier => Skirmishing.FollowUpShootSpeedPercent / 100f;
	public float SkirmishingFollowUpCritDamageBonus => Skirmishing.FollowUpCritDamagePercent / 100f;

	public float SpinboundEmpoweredDamageBonus => Spinbound.EmpoweredDamagePercent / 100f;
	public float SpinboundEmpoweredCritDamageBonus => Spinbound.EmpoweredCritDamagePercent / 100f;
	public float SpinboundStabilizationStrength => Spinbound.StabilizationStrengthPerThousand / 1000f;
	public float SpinboundVelocityRatioTolerance => Spinbound.VelocityRatioTolerancePercent / 100f;
	public float SpinboundAngleToleranceDegrees => Spinbound.AngleToleranceDegrees;
	public float SpinboundAnglePrecisionToleranceDegrees => Spinbound.AnglePrecisionToleranceDegrees;
	public float SpinboundSpeedRatioTolerance => Spinbound.SpeedRatioTolerancePercent / 100f;

	public int StormforgedMaxChainJumps => Stormforged.MaxChainJumps;
	public float StormforgedChainProcChance => Stormforged.ChainProcChancePercent / 100f;
	public float StormforgedChainRangePixels => Stormforged.ChainRangePixels;
	public float StormforgedChainDamageDecay => Stormforged.ChainDamageDecayPercent / 100f;

	public float TemporalAttackSpeedMultiplier => Temporal.AttackSpeedPercent / 100f;
	public float TemporalProjectileSpeedMultiplier => Temporal.ProjectileSpeedPercent / 100f;
	public float TemporalPressureMax => Temporal.PressureMax;
	public float TemporalBasePressurePerHit => Temporal.BasePressurePerHit;
	public float TemporalDamagePressureFactor => Temporal.DamagePressureFactor;
	public float TemporalTempoBonusPressure => Temporal.TempoBonusPressure;
	public int TemporalTempoBonusWindowTicks => Temporal.TempoBonusWindowTicks;
	public float TemporalBossPressureMultiplier => Temporal.BossPressureMultiplierPercent / 100f;
	public int TemporalFractureDurationTicks => Temporal.FractureDurationTicks;
	public int TemporalFractureCooldownTicks => Temporal.FractureCooldownTicks;
	public float TemporalReleaseMultiplier => Temporal.ReleaseMultiplierPercent / 100f;

	public float VampiricLifeStealMultiplier => Vampiric.LifeStealPercent / 100f;
	public float VampiricManaRestoreFromManaCost => Vampiric.ManaRestorePercent / 100f;
	public int VampiricMaxManaRestorePerHit => Vampiric.MaxManaRestorePerHit;
	public int VampiricBaseHealCapPerSecond => Vampiric.HealCapPerSecond;
	public int VampiricBaseManaRestorePerSecond => Vampiric.ManaCapPerSecond;
	public float VampiricCriticalFeedMultiplier => Vampiric.CriticalFeedPercent / 100f;
	public float VampiricWeakenedPreyThreshold => Vampiric.WeakenedThresholdPercent / 100f;
	public float VampiricBloodiedPreyThreshold => Vampiric.BloodiedThresholdPercent / 100f;
	public float VampiricCriticalPreyThreshold => Vampiric.CriticalThresholdPercent / 100f;
	public float VampiricWeakenedFeedMultiplier => Vampiric.WeakenedFeedPercent / 100f;
	public float VampiricBloodiedFeedMultiplier => Vampiric.BloodiedFeedPercent / 100f;
	public float VampiricCriticalFeedStateMultiplier => Vampiric.CriticalFeedStatePercent / 100f;
	public int VampiricFrenzyDurationTicks => Vampiric.FrenzyDurationTicks;
	public int VampiricFrenzyRetriggerCooldownTicks => Vampiric.FrenzyRetriggerCooldownTicks;
	public float VampiricFrenzyAttackSpeedBonus => Vampiric.FrenzyAttackSpeedPercent / 100f;
	public float VampiricFrenzyCapMultiplier => Vampiric.FrenzyCapPercent / 100f;
	public int VampiricFrenzyCritBonusAgainstWeakened => Vampiric.FrenzyCritBonusAgainstWeakened;
	public int VampiricFrenzyCritBonusAgainstBloodied => Vampiric.FrenzyCritBonusAgainstBloodied;
	public int VampiricFrenzyCritBonusAgainstCritical => Vampiric.FrenzyCritBonusAgainstCritical;

	// Compatibility shim for older config field names after Echoing was replaced by Fractured.
	[Browsable(false)]
	[Newtonsoft.Json.JsonProperty("EchoChancePercent")]
	public int LegacyEchoChancePercent {
		get => Fractured.FractureIChancePercent;
		set => Fractured.FractureIChancePercent = value;
	}

	[Newtonsoft.Json.JsonIgnore]
	public int EchoChancePercent {
		get => Fractured.FractureIChancePercent;
		set => Fractured.FractureIChancePercent = value;
	}

	[Newtonsoft.Json.JsonIgnore]
	public float EchoChance => FractureIChance;

	// Compatibility shim for older config field names after Colossal was merged into Breaching.
	[Newtonsoft.Json.JsonIgnore]
	public int ColossalSizePercent {
		get => Breaching.SizePercent;
		set => Breaching.SizePercent = value;
	}

	[Newtonsoft.Json.JsonIgnore]
	public float ColossalScaleMultiplier => BreachingScaleMultiplier;

	public sealed class AttunedSettings
	{
		[Header("Mechanics")]
		[DefaultValue(4)]
		[Range(1, 8)]
		[Increment(1)]
		[Slider]
		public int MaxResonanceStacks = 4;

		[DefaultValue(150)]
		[Range(30, 360)]
		[Increment(6)]
		[Slider]
		public int ResonanceDecayTicks = 150;

		[DefaultValue(12)]
		[Range(1, 60)]
		[Increment(1)]
		[Slider]
		public int ResonanceHitGainCooldownTicks = 12;

		[DefaultValue(115)]
		[Range(100, 200)]
		[Increment(1)]
		[Slider]
		public int EmpoweredCastShootSpeedPercent = 115;
	}

	public sealed class AwakenedSettings
	{
		[Header("PassiveStats")]
		[DefaultValue(6)]
		[Range(0, 30)]
		[Increment(1)]
		[Slider]
		public int DormantUseSpeedPercent = 6;

		[DefaultValue(3)]
		[Range(-10, 20)]
		[Increment(1)]
		[Slider]
		public int DormantCritBonus = 3;

		[DefaultValue(12)]
		[Range(0, 50)]
		[Increment(1)]
		[Slider]
		public int AwakenedDamagePercent = 12;

		[DefaultValue(10)]
		[Range(0, 50)]
		[Increment(1)]
		[Slider]
		public int AwakenedScalePercent = 10;

		[DefaultValue(10)]
		[Range(0, 50)]
		[Increment(1)]
		[Slider]
		public int AwakenedUseSpeedPercent = 10;

		[DefaultValue(78)]
		[Range(25, 150)]
		[Increment(1)]
		[Slider]
		public int SweetSpotKnockbackPercent = 78;

		[Header("Mechanics")]
		[DefaultValue(300)]
		[Range(60, 600)]
		[Increment(6)]
		[Slider]
		public int DesiredBuildTicks = 300;

		[DefaultValue(600)]
		[Range(120, 1200)]
		[Increment(30)]
		[Slider]
		public int AwakenedDurationTicks = 600;

		[DefaultValue(1200)]
		[Range(300, 2400)]
		[Increment(30)]
		[Slider]
		public int RecoveryLockoutTicks = 1200;

		[DefaultValue(6)]
		[Range(1, 30)]
		[Increment(1)]
		[Slider]
		public int HpBonusDamageRatioPerThousand = 6;

		[DefaultValue(8)]
		[Range(1, 50)]
		[Increment(1)]
		[Slider]
		public int MinBonusDamage = 8;

		[DefaultValue(120)]
		[Range(10, 300)]
		[Increment(5)]
		[Slider]
		public int MaxBonusDamageAgainstNormalEnemies = 120;

		[Header("Advanced")]
		[DefaultValue(12)]
		[Range(1, 50)]
		[Increment(1)]
		[Slider]
		public int BossBonusDamageCapRatioPercent = 12;

		[DefaultValue(60)]
		[Range(1, 300)]
		[Increment(1)]
		[Slider]
		public int MinBossBonusDamageCapPerSecond = 60;

		[DefaultValue(300)]
		[Range(10, 1000)]
		[Increment(5)]
		[Slider]
		public int MaxBossBonusDamageCapPerSecond = 300;

		[DefaultValue(60)]
		[Range(10, 180)]
		[Increment(1)]
		[Slider]
		public int BossBonusDamageWindowTicks = 60;
	}

	public sealed class BreachingSettings
	{
		[Header("PassiveStats")]
		[DefaultValue(120)]
		[Range(50, 200)]
		[Increment(1)]
		[Slider]
		public int DamagePercent = 120;

		[DefaultValue(118)]
		[Range(50, 200)]
		[Increment(1)]
		[Slider]
		public int KnockbackPercent = 118;

		[DefaultValue(124)]
		[Range(80, 200)]
		[Increment(1)]
		[Slider]
		public int UseTimePercent = 124;

		[DefaultValue(-4)]
		[Range(-20, 20)]
		[Increment(1)]
		[Slider]
		public int CritBonus = -4;

		[DefaultValue(12)]
		[Range(0, 50)]
		[Increment(1)]
		[Slider]
		public int ArmorPenetration = 12;

		[Header("Mechanics")]
		[DefaultValue(12)]
		[Range(0, 50)]
		[Increment(1)]
		[Slider]
		public int DefenseThreshold = 12;

		[DefaultValue(40)]
		[Range(1, 200)]
		[Increment(1)]
		[Slider]
		public int DamageThreshold = 40;

		[DefaultValue(130)]
		[Range(100, 200)]
		[Increment(1)]
		[Slider]
		public int SizePercent = 130;
	}

	public sealed class CatalyticSettings
	{
		[Header("Mechanics")]
		[DefaultValue(300)]
		[Range(60, 900)]
		[Increment(6)]
		[Slider]
		public int MarkDurationTicks = 300;

		[DefaultValue(6)]
		[Range(1, 30)]
		[Increment(1)]
		[Slider]
		public int MarkArmDelayTicks = 6;

		[DefaultValue(35)]
		[Range(0, 100)]
		[Increment(1)]
		[Slider]
		public int ConsumeDamagePercent = 35;
	}

	public sealed class DeadeyeSettings
	{
		[Header("Mechanics")]
		[DefaultValue(300)]
		[Range(60, 900)]
		[Increment(6)]
		[Slider]
		public int ReadyIntervalTicks = 300;

		[DefaultValue(120)]
		[Range(30, 300)]
		[Increment(6)]
		[Slider]
		public int StationaryReadyTicks = 120;

		[DefaultValue(6)]
		[Range(1, 30)]
		[Increment(1)]
		[Slider]
		public int ProjectileAssignmentLockoutTicks = 6;

		[DefaultValue(2)]
		[Range(0, 16)]
		[Increment(1)]
		[Slider]
		public int StationaryTolerancePixels = 2;

		[DefaultValue(120)]
		[Range(100, 200)]
		[Increment(1)]
		[Slider]
		public int ShotVelocityPercent = 120;

		[DefaultValue(20)]
		[Range(0, 50)]
		[Increment(1)]
		[Slider]
		public int ShotCritChanceBonus = 20;

		[DefaultValue(35)]
		[Range(0, 100)]
		[Increment(1)]
		[Slider]
		public int ShotCritDamagePercent = 35;

		[DefaultValue(30)]
		[Range(0, 100)]
		[Increment(1)]
		[Slider]
		public int BurstDamagePercent = 30;

		[DefaultValue(120)]
		[Range(20, 300)]
		[Increment(5)]
		[Slider]
		public int BurstRadiusPixels = 120;

		[DefaultValue(50)]
		[Range(10, 100)]
		[Increment(1)]
		[Slider]
		public int BossBurstDamagePercent = 50;
	}

	public sealed class DesperateSettings
	{
		[Header("PassiveStats")]
		[DefaultValue(80)]
		[Range(0, 150)]
		[Increment(1)]
		[Slider]
		public int MaxDamagePercent = 80;

		[DefaultValue(5)]
		[Range(-10, 20)]
		[Increment(1)]
		[Slider]
		public int CritBonus = 5;

		[Header("Mechanics")]
		[DefaultValue(50)]
		[Range(1, 100)]
		[Increment(1)]
		[Slider]
		public int MinorLifeThresholdPercent = 50;

		[DefaultValue(25)]
		[Range(1, 100)]
		[Increment(1)]
		[Slider]
		public int SevereLifeThresholdPercent = 25;

		[DefaultValue(13)]
		[Range(1, 100)]
		[Increment(1)]
		[Slider]
		public int CriticalLifeThresholdPercent = 13;

		[DefaultValue(180)]
		[Range(30, 600)]
		[Increment(6)]
		[Slider]
		public int SurgeCooldownTicks = 180;

		[DefaultValue(20)]
		[Range(0, 100)]
		[Increment(1)]
		[Slider]
		public int SurgeDamagePercent = 20;
	}

	public sealed class FracturedSettings
	{
		[Header("Mechanics")]
		[DefaultValue(25)]
		[Range(0, 100)]
		[Increment(1)]
		[Slider]
		public int FractureIChancePercent = 25;

		[DefaultValue(15)]
		[Range(0, 100)]
		[Increment(1)]
		[Slider]
		public int FractureIIChancePercent = 15;

		[DefaultValue(8)]
		[Range(0, 100)]
		[Increment(1)]
		[Slider]
		public int FractureIIIChancePercent = 8;

		[DefaultValue(45)]
		[Range(0, 100)]
		[Increment(1)]
		[Slider]
		public int FractureIDamagePercent = 45;

		[DefaultValue(35)]
		[Range(0, 100)]
		[Increment(1)]
		[Slider]
		public int FractureIIDamagePercent = 35;

		[DefaultValue(25)]
		[Range(0, 100)]
		[Increment(1)]
		[Slider]
		public int FractureIIIDamagePercent = 25;

		[Header("Advanced")]
		[DefaultValue(2)]
		[Range(0, 30)]
		[Increment(1)]
		[Slider]
		public int FractureIAngleVarianceDegrees = 2;

		[DefaultValue(4)]
		[Range(0, 30)]
		[Increment(1)]
		[Slider]
		public int FractureIIAngleVarianceDegrees = 4;

		[DefaultValue(6)]
		[Range(0, 30)]
		[Increment(1)]
		[Slider]
		public int FractureIIIAngleVarianceDegrees = 6;

		[DefaultValue(1)]
		[Range(0, 20)]
		[Increment(1)]
		[Slider]
		public int FractureIIDelayMinFrames = 1;

		[DefaultValue(3)]
		[Range(0, 20)]
		[Increment(1)]
		[Slider]
		public int FractureIIDelayMaxFrames = 3;

		[DefaultValue(2)]
		[Range(0, 20)]
		[Increment(1)]
		[Slider]
		public int FractureIIIDelayMinFrames = 2;

		[DefaultValue(5)]
		[Range(0, 20)]
		[Increment(1)]
		[Slider]
		public int FractureIIIDelayMaxFrames = 5;
	}

	public sealed class RadiantSettings
	{
		[Header("Mechanics")]
		[DefaultValue(180)]
		[Range(30, 600)]
		[Increment(6)]
		[Slider]
		public int OnFireDurationTicks = 180;
	}

	public sealed class ShiftingSettings
	{
		[Header("Mechanics")]
		[DefaultValue(600)]
		[Range(120, 1800)]
		[Increment(30)]
		[Slider]
		public int ShiftDurationTicks = 600;

		[DefaultValue(300)]
		[Range(60, 900)]
		[Increment(30)]
		[Slider]
		public int CombatWindowTicks = 300;

		[DefaultValue(150)]
		[Range(50, 300)]
		[Increment(1)]
		[Slider]
		public int StrengthPercent = 150;
	}

	public sealed class SkirmishingSettings
	{
		[Header("Mechanics")]
		[DefaultValue(90)]
		[Range(30, 300)]
		[Increment(6)]
		[Slider]
		public int WindowTicks = 90;

		[DefaultValue(12)]
		[Range(0, 50)]
		[Increment(1)]
		[Slider]
		public int MoveSpeedPercent = 12;

		[DefaultValue(112)]
		[Range(100, 200)]
		[Increment(1)]
		[Slider]
		public int RunAccelerationPercent = 112;

		[DefaultValue(50)]
		[Range(0, 200)]
		[Increment(1)]
		[Slider]
		public int MaxRunSpeedBonus = 50;

		[DefaultValue(112)]
		[Range(100, 200)]
		[Increment(1)]
		[Slider]
		public int FollowUpShootSpeedPercent = 112;

		[DefaultValue(12)]
		[Range(0, 100)]
		[Increment(1)]
		[Slider]
		public int FollowUpCritDamagePercent = 12;
	}

	public sealed class SpinboundSettings
	{
		[Header("Mechanics")]
		[DefaultValue(20)]
		[Range(0, 100)]
		[Increment(1)]
		[Slider]
		public int EmpoweredDamagePercent = 20;

		[DefaultValue(10)]
		[Range(0, 100)]
		[Increment(1)]
		[Slider]
		public int EmpoweredCritDamagePercent = 10;

		[DefaultValue(35)]
		[Range(1, 200)]
		[Increment(1)]
		[Slider]
		public int StabilizationStrengthPerThousand = 35;

		[Header("Advanced")]
		[DefaultValue(25)]
		[Range(1, 100)]
		[Increment(1)]
		[Slider]
		public int VelocityRatioTolerancePercent = 25;

		[DefaultValue(8)]
		[Range(0, 30)]
		[Increment(1)]
		[Slider]
		public int AngleToleranceDegrees = 8;

		[DefaultValue(3)]
		[Range(0, 30)]
		[Increment(1)]
		[Slider]
		public int AnglePrecisionToleranceDegrees = 3;

		[DefaultValue(35)]
		[Range(1, 100)]
		[Increment(1)]
		[Slider]
		public int SpeedRatioTolerancePercent = 35;
	}

	public sealed class StormforgedSettings
	{
		[Header("Mechanics")]
		[DefaultValue(33)]
		[Range(0, 100)]
		[Increment(1)]
		[Slider]
		public int ChainProcChancePercent = 33;

		[DefaultValue(3)]
		[Range(1, 10)]
		[Increment(1)]
		[Slider]
		public int MaxChainJumps = 3;

		[DefaultValue(160)]
		[Range(40, 800)]
		[Increment(10)]
		[Slider]
		public int ChainRangePixels = 160;

		[DefaultValue(65)]
		[Range(1, 100)]
		[Increment(1)]
		[Slider]
		public int ChainDamageDecayPercent = 65;
	}

	public sealed class TemporalSettings
	{
		[Header("PassiveStats")]
		[DefaultValue(150)]
		[Range(100, 300)]
		[Increment(1)]
		[Slider]
		public int AttackSpeedPercent = 150;

		[DefaultValue(150)]
		[Range(100, 300)]
		[Increment(1)]
		[Slider]
		public int ProjectileSpeedPercent = 150;

		[Header("Mechanics")]
		[DefaultValue(100)]
		[Range(10, 300)]
		[Increment(1)]
		[Slider]
		public int PressureMax = 100;

		[DefaultValue(20)]
		[Range(1, 100)]
		[Increment(1)]
		[Slider]
		public int BasePressurePerHit = 20;

		[DefaultValue(120)]
		[Range(1, 300)]
		[Increment(1)]
		[Slider]
		public int DamagePressureFactor = 120;

		[DefaultValue(2)]
		[Range(0, 30)]
		[Increment(1)]
		[Slider]
		public int TempoBonusPressure = 2;

		[DefaultValue(45)]
		[Range(1, 180)]
		[Increment(1)]
		[Slider]
		public int TempoBonusWindowTicks = 45;

		[DefaultValue(65)]
		[Range(1, 100)]
		[Increment(1)]
		[Slider]
		public int BossPressureMultiplierPercent = 65;

		[DefaultValue(90)]
		[Range(10, 300)]
		[Increment(1)]
		[Slider]
		public int FractureDurationTicks = 90;

		[DefaultValue(240)]
		[Range(10, 900)]
		[Increment(1)]
		[Slider]
		public int FractureCooldownTicks = 240;

		[DefaultValue(115)]
		[Range(100, 300)]
		[Increment(1)]
		[Slider]
		public int ReleaseMultiplierPercent = 115;
	}

	public sealed class VampiricSettings
	{
		[Header("Mechanics")]
		[DefaultValue(8)]
		[Range(0, 50)]
		[Increment(1)]
		[Slider]
		public int LifeStealPercent = 8;

		[DefaultValue(20)]
		[Range(0, 100)]
		[Increment(1)]
		[Slider]
		public int ManaRestorePercent = 20;

		[DefaultValue(2)]
		[Range(0, 20)]
		[Increment(1)]
		[Slider]
		public int MaxManaRestorePerHit = 2;

		[DefaultValue(5)]
		[Range(0, 50)]
		[Increment(1)]
		[Slider]
		public int HealCapPerSecond = 5;

		[DefaultValue(6)]
		[Range(0, 50)]
		[Increment(1)]
		[Slider]
		public int ManaCapPerSecond = 6;

		[DefaultValue(135)]
		[Range(100, 300)]
		[Increment(1)]
		[Slider]
		public int CriticalFeedPercent = 135;

		[DefaultValue(50)]
		[Range(1, 100)]
		[Increment(1)]
		[Slider]
		public int WeakenedThresholdPercent = 50;

		[DefaultValue(25)]
		[Range(1, 100)]
		[Increment(1)]
		[Slider]
		public int BloodiedThresholdPercent = 25;

		[DefaultValue(13)]
		[Range(1, 100)]
		[Increment(1)]
		[Slider]
		public int CriticalThresholdPercent = 13;

		[DefaultValue(115)]
		[Range(100, 300)]
		[Increment(1)]
		[Slider]
		public int WeakenedFeedPercent = 115;

		[DefaultValue(130)]
		[Range(100, 300)]
		[Increment(1)]
		[Slider]
		public int BloodiedFeedPercent = 130;

		[DefaultValue(150)]
		[Range(100, 300)]
		[Increment(1)]
		[Slider]
		public int CriticalFeedStatePercent = 150;

		[DefaultValue(180)]
		[Range(30, 900)]
		[Increment(6)]
		[Slider]
		public int FrenzyDurationTicks = 180;

		[DefaultValue(900)]
		[Range(60, 1800)]
		[Increment(30)]
		[Slider]
		public int FrenzyRetriggerCooldownTicks = 900;

		[DefaultValue(12)]
		[Range(0, 100)]
		[Increment(1)]
		[Slider]
		public int FrenzyAttackSpeedPercent = 12;

		[DefaultValue(150)]
		[Range(100, 300)]
		[Increment(1)]
		[Slider]
		public int FrenzyCapPercent = 150;

		[DefaultValue(4)]
		[Range(0, 50)]
		[Increment(1)]
		[Slider]
		public int FrenzyCritBonusAgainstWeakened = 4;

		[DefaultValue(7)]
		[Range(0, 50)]
		[Increment(1)]
		[Slider]
		public int FrenzyCritBonusAgainstBloodied = 7;

		[DefaultValue(10)]
		[Range(0, 50)]
		[Increment(1)]
		[Slider]
		public int FrenzyCritBonusAgainstCritical = 10;
	}
}
