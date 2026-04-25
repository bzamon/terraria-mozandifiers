using Terraria.Localization;

namespace Mozandifiers.Content.Prefixes.Common;

internal static class ShiftingSimulationDisplay
{
	internal static string GetDisplayName(string modName, ShiftingSimulationId simulationId)
	{
		string prefixKey = simulationId switch {
			ShiftingSimulationId.Breaching => "BreachingPrefix",
			ShiftingSimulationId.Attuned => "AttunedPrefix",
			ShiftingSimulationId.Skirmishing => "SkirmishingPrefix",
			ShiftingSimulationId.Desperate => "DesperatePrefix",
			ShiftingSimulationId.Radiant => "RadiantPrefix",
			ShiftingSimulationId.Vampiric => "SanguinePrefix",
			ShiftingSimulationId.Deadeye => "DeadeyePrefix",
			ShiftingSimulationId.Temporal => "TemporalPrefix",
			ShiftingSimulationId.Fractured => "EchoingPrefix",
			ShiftingSimulationId.Stormforged => "StormforgedPrefix",
			ShiftingSimulationId.Catalytic => "CatalyticPrefix",
			ShiftingSimulationId.Spinbound => "SpinboundPrefix",
			_ => string.Empty
		};

		if (string.IsNullOrEmpty(prefixKey)) {
			return string.Empty;
		}

		return Language.GetTextValue($"Mods.{modName}.Prefixes.{prefixKey}.DisplayName");
	}
}
