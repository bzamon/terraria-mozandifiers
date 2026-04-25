using Microsoft.Xna.Framework;
using Mozandifiers.Content.Prefixes.Weapons;
using Terraria;
using Terraria.ID;

namespace Mozandifiers.Content.Prefixes.Common;

internal static class WeaponPrefixVisuals
{
	internal static readonly Color AttunedBaseColor = new(124, 204, 255);
	internal static readonly Color AttunedReadyColor = new(210, 244, 255);
	internal static readonly Color AwakenedDormantColor = new(164, 40, 52);
	internal static readonly Color AwakenedActiveColor = new(244, 72, 64);
	internal static readonly Color AwakenedSealedColor = new(38, 10, 16);
	internal static readonly Color SkirmishingWindowColor = new(198, 226, 162);
	internal static readonly Color SkirmishingFollowUpColor = new(236, 251, 198);
	internal static readonly Color SkirmishingTracerTint = new(214, 244, 170, 56);
	internal static readonly Color VampiricFeedColor = new(188, 70, 84);
	internal static readonly Color VampiricFrenzyColor = new(240, 96, 104);
	internal static readonly Color VampiricArcaneColor = new(164, 126, 228);
	internal static readonly Color FracturedTierOneTint = new(174, 222, 255, 68);
	internal static readonly Color FracturedTierTwoTint = new(148, 176, 236, 84);
	internal static readonly Color FracturedTierThreeTint = new(186, 138, 236, 98);
	internal static readonly Color FracturedTierOneAfterimageTint = new(198, 228, 255, 28);
	internal static readonly Color FracturedTierTwoAfterimageTint = new(154, 186, 244, 36);
	internal static readonly Color FracturedTierThreeAfterimageTint = new(198, 148, 246, 44);
	internal static readonly Color DeadeyeReadyColor = new(164, 122, 230);
	internal static readonly Color DeadeyeShotColor = new(188, 148, 255, 80);
	internal static readonly Color DeadeyeAfterimageTint = new(206, 176, 255, 42);
	internal static readonly Color DeadeyeLeafBurstColor = new(142, 224, 126);
	internal static readonly Color DeadeyeLeafAccentColor = new(198, 244, 164);
	internal static readonly Color BreachingDustColor = new(214, 194, 158);
	internal static readonly Color BreachingSmokeColor = new(124, 112, 102);
	internal static readonly Color DesperateMinorColor = new(176, 82, 92);
	internal static readonly Color DesperateSevereColor = new(214, 74, 88);
	internal static readonly Color DesperateCriticalColor = new(255, 104, 96);
	internal static readonly Color TemporalTint = new(124, 198, 255, 84);
	internal static readonly Color TemporalFractureColor = new(156, 228, 255);
	internal static readonly Color TemporalCollapseColor = new(216, 244, 255);
	internal static readonly Color TemporalAfterimageTint = new(192, 232, 255, 40);
	internal static readonly Color ShiftingBaseColor = new(195, 160, 255);
	internal static readonly Color ShiftingMeleeAccent = new(224, 190, 128);
	internal static readonly Color ShiftingRangedAccent = new(244, 214, 120);
	internal static readonly Color ShiftingMagicAccent = new(120, 220, 255);

	internal const int EchoDustType = DustID.Shadowflame;
	internal const int DeadeyeReadyDustType = DustID.PurpleTorch;
	internal const int DeadeyeShotDustType = DustID.Shadowflame;
	internal const int DeadeyeLeafDustType = DustID.JungleSpore;
	internal const int BreachingDustType = DustID.Stone;
	internal const int BreachingSmokeDustType = DustID.Smoke;
	internal const int DesperateDustType = DustID.CrimsonTorch;
	internal const int AttunedDustType = DustID.MagicMirror;
	internal const int AwakenedDustType = DustID.Electric;
	internal const int SkirmishingDustType = DustID.Cloud;
	internal const int VampiricDustType = DustID.CrimsonTorch;
	internal const int VampiricMagicDustType = DustID.MagicMirror;
	internal const int RadiantDustType = DustID.GoldFlame;
	internal const int TemporalDustType = DustID.GemSapphire;

	internal static int GetCatalyticDustType(bool shifted)
	{
		return shifted ? DustID.GoldFlame : DustID.Enchanted_Pink;
	}

	internal static Color GetCatalyticLightColor(bool shifted)
	{
		return shifted ? new Color(255, 214, 110) : new Color(255, 110, 214);
	}

	internal static Color GetShiftingAccentColor(ShiftingWeaponMode mode)
	{
		return mode switch {
			ShiftingWeaponMode.MeleeSwing => ShiftingMeleeAccent,
			ShiftingWeaponMode.RangedProjectile => ShiftingRangedAccent,
			ShiftingWeaponMode.MagicProjectile => ShiftingMagicAccent,
			_ => ShiftingBaseColor
		};
	}

	internal static float GetRadiantPulse(float timeOffset = 0f)
	{
		return 0.9f + 0.1f * (0.5f + 0.5f * System.MathF.Sin(Main.GlobalTimeWrappedHourly * 7f + timeOffset));
	}

	internal static float GetTemporalPulse(float timeOffset = 0f)
	{
		return 0.82f + 0.18f * (0.5f + 0.5f * System.MathF.Sin(Main.GlobalTimeWrappedHourly * 9f + timeOffset));
	}

	internal static float GetDesperatePulse(float timeOffset = 0f)
	{
		return 0.78f + 0.22f * (0.5f + 0.5f * System.MathF.Sin(Main.GlobalTimeWrappedHourly * 8.5f + timeOffset));
	}

	internal static float GetAttunedPulse(float timeOffset = 0f)
	{
		return 0.84f + 0.16f * (0.5f + 0.5f * System.MathF.Sin(Main.GlobalTimeWrappedHourly * 8f + timeOffset));
	}

	internal static float GetAwakenedPulse(float timeOffset = 0f)
	{
		return 0.82f + 0.18f * (0.5f + 0.5f * System.MathF.Sin(Main.GlobalTimeWrappedHourly * 9.5f + timeOffset));
	}

	internal static float GetSkirmishingPulse(float timeOffset = 0f)
	{
		return 0.82f + 0.18f * (0.5f + 0.5f * System.MathF.Sin(Main.GlobalTimeWrappedHourly * 10f + timeOffset));
	}

	internal static float GetVampiricPulse(float timeOffset = 0f)
	{
		return 0.8f + 0.2f * (0.5f + 0.5f * System.MathF.Sin(Main.GlobalTimeWrappedHourly * 7.8f + timeOffset));
	}

	internal static Color GetAttunedColor(float progress, bool empowered)
	{
		return Color.Lerp(AttunedBaseColor, empowered ? AttunedReadyColor : AttunedBaseColor, System.MathF.Min(1f, progress * 0.8f));
	}

	internal static Color GetAwakenedColor(AwakenedState state, float progress)
	{
		return state switch {
			AwakenedState.Dormant => Color.Lerp(Color.Transparent, AwakenedDormantColor, System.MathF.Min(1f, progress)),
			AwakenedState.Awakened => Color.Lerp(AwakenedDormantColor, AwakenedActiveColor, 0.75f + 0.25f * progress),
			AwakenedState.RecoveryLockout => Color.Lerp(AwakenedSealedColor, AwakenedDormantColor, 0.22f + 0.38f * (1f - progress)),
			_ => Color.Transparent
		};
	}

	internal static Color GetSkirmishingColor(float progress, bool followUp)
	{
		return Color.Lerp(
			SkirmishingWindowColor,
			followUp ? SkirmishingFollowUpColor : SkirmishingWindowColor,
			System.MathF.Min(1f, 0.35f + progress * 0.65f));
	}

	internal static Color GetDesperateColor(DesperateThresholdState state)
	{
		return state switch {
			DesperateThresholdState.Minor => DesperateMinorColor,
			DesperateThresholdState.Severe => DesperateSevereColor,
			DesperateThresholdState.Critical => DesperateCriticalColor,
			_ => Color.Transparent
		};
	}

	internal static Color GetVampiricFeedColor(VampiricPreyState preyState, bool magicFeed)
	{
		Color baseColor = preyState switch {
			VampiricPreyState.Weakened => Color.Lerp(VampiricFeedColor, VampiricFrenzyColor, 0.25f),
			VampiricPreyState.Bloodied => Color.Lerp(VampiricFeedColor, VampiricFrenzyColor, 0.45f),
			VampiricPreyState.Critical => VampiricFrenzyColor,
			_ => VampiricFeedColor
		};

		return magicFeed ? Color.Lerp(baseColor, VampiricArcaneColor, 0.38f) : baseColor;
	}

	internal static Color GetTemporalPressureColor(float pressureRatio)
	{
		return Color.Lerp(TemporalTint, TemporalFractureColor, MathHelper.Clamp(pressureRatio, 0f, 1f));
	}

	internal static Color GetFracturedTint(int tier)
	{
		return tier switch {
			>= 3 => FracturedTierThreeTint,
			2 => FracturedTierTwoTint,
			_ => FracturedTierOneTint
		};
	}

	internal static Color GetFracturedAfterimageTint(int tier)
	{
		return tier switch {
			>= 3 => FracturedTierThreeAfterimageTint,
			2 => FracturedTierTwoAfterimageTint,
			_ => FracturedTierOneAfterimageTint
		};
	}
}
