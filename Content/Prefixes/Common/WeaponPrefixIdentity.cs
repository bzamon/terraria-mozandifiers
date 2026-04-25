using Mozandifiers.Common.Players;
using Mozandifiers.Content.Prefixes.Weapons;
using Terraria;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Common;

internal static class WeaponPrefixIdentity
{
	internal static int VampiricPrefixId => ModContent.PrefixType<SanguinePrefix>();
	internal static int DeadeyePrefixId => ModContent.PrefixType<DeadeyePrefix>();
	internal static int AwakenedPrefixId => ModContent.PrefixType<AwakenedPrefix>();
	internal static int RadiantPrefixId => ModContent.PrefixType<RadiantPrefix>();
	internal static int AttunedPrefixId => ModContent.PrefixType<AttunedPrefix>();
	internal static int SkirmishingPrefixId => ModContent.PrefixType<SkirmishingPrefix>();
	internal static int StormforgedPrefixId => ModContent.PrefixType<StormforgedPrefix>();
	internal static int DesperatePrefixId => ModContent.PrefixType<DesperatePrefix>();
	internal static int ShiftingPrefixId => ModContent.PrefixType<ShiftingPrefix>();
	internal static int SpinboundPrefixId => ModContent.PrefixType<SpinboundPrefix>();
	internal static int BreachingPrefixId => ModContent.PrefixType<BreachingPrefix>();
	internal static int TemporalPrefixId => ModContent.PrefixType<TemporalPrefix>();
	internal static int FracturedPrefixId => ModContent.PrefixType<EchoingPrefix>();
	internal static int CatalyticPrefixId => ModContent.PrefixType<CatalyticPrefix>();

	internal static bool HasVampiricPrefix(Item item) => item?.prefix == VampiricPrefixId;
	internal static bool HasDeadeyePrefix(Item item) => item?.prefix == DeadeyePrefixId;
	internal static bool HasAwakenedPrefix(Item item) => item?.prefix == AwakenedPrefixId;
	internal static bool HasRadiantPrefix(Item item) => item?.prefix == RadiantPrefixId;
	internal static bool HasAttunedPrefix(Item item) => item?.prefix == AttunedPrefixId;
	internal static bool HasSkirmishingPrefix(Item item) => item?.prefix == SkirmishingPrefixId;
	internal static bool HasStormforgedPrefix(Item item) => item?.prefix == StormforgedPrefixId;
	internal static bool HasDesperatePrefix(Item item) => item?.prefix == DesperatePrefixId;
	internal static bool HasShiftingPrefix(Item item) => item?.prefix == ShiftingPrefixId;
	internal static bool HasSpinboundPrefix(Item item) => item?.prefix == SpinboundPrefixId;
	internal static bool HasBreachingPrefix(Item item) => item?.prefix == BreachingPrefixId;
	internal static bool HasTemporalPrefix(Item item) => item?.prefix == TemporalPrefixId;
	internal static bool HasFracturedPrefix(Item item) => item?.prefix == FracturedPrefixId;
	internal static bool HasCatalyticPrefix(Item item) => item?.prefix == CatalyticPrefixId;

	internal static bool TryGetShiftingSimulation(Item item, Player player, out ShiftingSimulationId simulationId)
	{
		simulationId = ShiftingSimulationId.None;
		return player != null
			&& player.active
			&& player.GetModPlayer<ShiftingPlayer>().TryGetActiveSimulation(item, out simulationId);
	}

	internal static bool TryGetShiftingSimulation(Item item, int ownerIndex, out ShiftingSimulationId simulationId)
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
}
