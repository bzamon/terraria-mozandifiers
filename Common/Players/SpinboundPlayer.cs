using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Mozandifiers.Content.Prefixes.Weapons;

namespace Mozandifiers.Common.Players;

public sealed class SpinboundPlayer : ModPlayer
{
	private readonly Dictionary<int, SpinboundCadenceState> spinboundCadenceByItemType = [];
	private readonly Dictionary<int, Queue<SpinboundShotContext>> spinboundShotContextsByItemType = [];

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

	public bool ResolveSpinboundShot(Item item, Vector2 velocity, bool shifted)
	{
		if (item == null || item.IsAir) {
			return false;
		}

		bool cadenceBonusTriggered = RegisterSpinboundShot(item, shifted);
		int cadenceKey = GetCadenceKey(item.type, shifted);
		if (!spinboundShotContextsByItemType.TryGetValue(cadenceKey, out Queue<SpinboundShotContext> contexts)) {
			contexts = [];
			spinboundShotContextsByItemType[cadenceKey] = contexts;
		}

		contexts.Enqueue(new SpinboundShotContext {
			Velocity = velocity,
			CadenceBonusTriggered = cadenceBonusTriggered
		});
		return cadenceBonusTriggered;
	}

	public bool TryConsumeSpinboundShotContext(Item item, bool shifted, out Vector2 velocity, out bool cadenceBonusTriggered)
	{
		velocity = Vector2.Zero;
		cadenceBonusTriggered = false;
		if (item == null || item.IsAir) {
			return false;
		}

		int cadenceKey = GetCadenceKey(item.type, shifted);
		if (!spinboundShotContextsByItemType.TryGetValue(cadenceKey, out Queue<SpinboundShotContext> contexts)
			|| contexts.Count == 0) {
			return false;
		}

		SpinboundShotContext context = contexts.Dequeue();
		velocity = context.Velocity;
		cadenceBonusTriggered = context.CadenceBonusTriggered;
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

	private struct SpinboundShotContext
	{
		public Vector2 Velocity { get; set; }
		public bool CadenceBonusTriggered { get; set; }
	}
}
