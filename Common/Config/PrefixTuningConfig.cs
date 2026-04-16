using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace Mozandifiers.Common.Config;

public sealed class PrefixTuningConfig : ModConfig
{
	public static PrefixTuningConfig Instance => ModContent.GetInstance<PrefixTuningConfig>();

	public override ConfigScope Mode => ConfigScope.ServerSide;

	[Header("WeaponPrefixTuning")]
	[DefaultValue(12)]
	[Range(0, 100)]
	[Increment(1)]
	[Slider]
	public int EchoChancePercent;

	[DefaultValue(130)]
	[Range(100, 200)]
	[Increment(1)]
	[Slider]
	public int ColossalSizePercent;

	public float EchoChance => EchoChancePercent / 100f;

	public float ColossalScaleMultiplier => ColossalSizePercent / 100f;
}
