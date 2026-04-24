using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace Mozandifiers.Common.Config;

public sealed class PrefixTuningConfig : ModConfig
{
	public static PrefixTuningConfig Instance => ModContent.GetInstance<PrefixTuningConfig>();

	public override ConfigScope Mode => ConfigScope.ServerSide;

	[Header("WeaponPrefixTuning")]
	[DefaultValue(25)]
	[Range(0, 100)]
	[Increment(1)]
	[Slider]
	public int FractureIChancePercent;

	[DefaultValue(15)]
	[Range(0, 100)]
	[Increment(1)]
	[Slider]
	public int FractureIIChancePercent;

	[DefaultValue(8)]
	[Range(0, 100)]
	[Increment(1)]
	[Slider]
	public int FractureIIIChancePercent;

	[DefaultValue(130)]
	[Range(100, 200)]
	[Increment(1)]
	[Slider]
	public int BreachingSizePercent;

	public float FractureIChance => FractureIChancePercent / 100f;

	public float FractureIIChance => FractureIIChancePercent / 100f;

	public float FractureIIIChance => FractureIIIChancePercent / 100f;

	public float BreachingScaleMultiplier => BreachingSizePercent / 100f;

	// Compatibility shim for older config field names after Echoing was replaced by Fractured.
	[System.ComponentModel.Browsable(false)]
	[Newtonsoft.Json.JsonProperty("EchoChancePercent")]
	public int LegacyEchoChancePercent {
		get => FractureIChancePercent;
		set => FractureIChancePercent = value;
	}

	[Newtonsoft.Json.JsonIgnore]
	public int EchoChancePercent {
		get => FractureIChancePercent;
		set => FractureIChancePercent = value;
	}

	[Newtonsoft.Json.JsonIgnore]
	public float EchoChance => FractureIChance;

	// Compatibility shim for older config field names after Colossal was merged into Breaching.
	[Newtonsoft.Json.JsonIgnore]
	public int ColossalSizePercent {
		get => BreachingSizePercent;
		set => BreachingSizePercent = value;
	}

	[Newtonsoft.Json.JsonIgnore]
	public float ColossalScaleMultiplier => BreachingScaleMultiplier;
}
