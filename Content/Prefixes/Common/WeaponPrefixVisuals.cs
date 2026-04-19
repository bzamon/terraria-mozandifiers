using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace Mozandifiers.Content.Prefixes.Common;

internal static class WeaponPrefixVisuals
{
	internal static readonly Color EchoTint = new(106, 132, 205, 76);
	internal static readonly Color EchoAfterimageTint = new(136, 160, 224, 34);
	internal static readonly Color HeadshotTint = new(170, 225, 255);
	internal static readonly Color HeadshotLongRangeTint = new(235, 245, 255);
	internal static readonly Color BreachingDustColor = new(214, 194, 158);
	internal static readonly Color BreachingSmokeColor = new(124, 112, 102);
	internal static readonly Color TemporalTint = new(124, 198, 255, 84);
	internal static readonly Color TemporalAfterimageTint = new(192, 232, 255, 40);
	internal static readonly Color ShiftingBaseColor = new(195, 160, 255);
	internal static readonly Color ShiftingMeleeAccent = new(224, 190, 128);
	internal static readonly Color ShiftingRangedAccent = new(244, 214, 120);
	internal static readonly Color ShiftingMagicAccent = new(120, 220, 255);

	internal const int EchoDustType = DustID.BlueTorch;
	internal const int HeadshotDustType = DustID.BlueTorch;
	internal const int HeadshotLongRangeDustType = DustID.Electric;
	internal const int BreachingDustType = DustID.Stone;
	internal const int BreachingSmokeDustType = DustID.Smoke;
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
}
