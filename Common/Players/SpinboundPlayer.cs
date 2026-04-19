using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Mozandifiers.Content.Prefixes.Weapons;

namespace Mozandifiers.Common.Players;

public sealed class SpinboundPlayer : ModPlayer
{
	private readonly Dictionary<int, SpinboundCadenceState> spinboundCadenceByItemType = [];

	public bool RegisterSpinboundShot(Item item)
	{
		return RegisterSpinboundShot(item, false);
	}

	public bool RegisterSpinboundShot(Item item, bool shifted)
	{
		if (item == null || item.IsAir) {
			return false;
		}

		int cadenceKey = GetCadenceKey(item.type, shifted);
		if (!spinboundCadenceByItemType.TryGetValue(cadenceKey, out SpinboundCadenceState state)) {
			spinboundCadenceByItemType[cadenceKey] = new SpinboundCadenceState {
				NextGapIndex = 1,
				ShotsRemainingUntilTrigger = SpinboundPrefix.FibonacciShotGaps[0]
			};
			return true;
		}

		if (state.ShotsRemainingUntilTrigger > 0) {
			state.ShotsRemainingUntilTrigger--;
			spinboundCadenceByItemType[cadenceKey] = state;
			return false;
		}

		int nextGap = SpinboundPrefix.FibonacciShotGaps[state.NextGapIndex];
		state.NextGapIndex = (state.NextGapIndex + 1) % SpinboundPrefix.FibonacciShotGaps.Length;
		state.ShotsRemainingUntilTrigger = nextGap;
		spinboundCadenceByItemType[cadenceKey] = state;
		return true;
	}

	private static int GetCadenceKey(int itemType, bool shifted)
	{
		return (itemType << 1) | (shifted ? 1 : 0);
	}

	private struct SpinboundCadenceState
	{
		public int NextGapIndex { get; set; }
		public int ShotsRemainingUntilTrigger { get; set; }
	}
}
