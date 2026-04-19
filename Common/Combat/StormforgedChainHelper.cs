using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Mozandifiers.Common.Players;
using Mozandifiers.Content.Prefixes.Weapons;
using Terraria;
using Terraria.ID;

namespace Mozandifiers.Common.Combat;

internal static class StormforgedChainHelper
{
	internal static void TryTriggerChain(Player owner, NPC initialTarget, int sourceDamage)
	{
		TryTriggerChain(
			owner,
			initialTarget,
			sourceDamage,
			StormforgedPrefix.ChainProcChance,
			StormforgedPrefix.MaxChainJumps,
			StormforgedPrefix.ChainRangePixels,
			StormforgedPrefix.ChainDamageDecay);
	}

	internal static void TryTriggerChain(
		Player owner,
		NPC initialTarget,
		int sourceDamage,
		float procChance,
		int maxChainJumps,
		float chainRangePixels,
		float chainDamageDecay)
	{
		if (owner.whoAmI != Main.myPlayer
			|| !owner.active
			|| owner.dead
			|| initialTarget == null
			|| !initialTarget.active
			|| sourceDamage <= 0) {
			return;
		}

		StormforgedPlayer stormforgedPlayer = owner.GetModPlayer<StormforgedPlayer>();
		if (stormforgedPlayer.SuppressStormforgedChain || Main.rand.NextFloat() >= procChance) {
			return;
		}

		HashSet<int> hitTargets = [initialTarget.whoAmI];
		NPC currentTarget = initialTarget;
		int chainDamage = (int)System.MathF.Max(1f, System.MathF.Round(sourceDamage * chainDamageDecay));

		for (int jump = 0; jump < maxChainJumps && chainDamage > 0; jump++) {
			NPC nextTarget = FindNextStormforgedTarget(currentTarget.Center, hitTargets, chainRangePixels);
			if (nextTarget == null) {
				break;
			}

			SpawnStormforgedDust(currentTarget.Center, nextTarget.Center);

			stormforgedPlayer.SuppressStormforgedChain = true;
			try {
				owner.ApplyDamageToNPC(nextTarget, chainDamage, 0f, nextTarget.Center.X >= owner.Center.X ? 1 : -1, false);
			}
			finally {
				stormforgedPlayer.SuppressStormforgedChain = false;
			}

			hitTargets.Add(nextTarget.whoAmI);
			currentTarget = nextTarget;
			chainDamage = (int)System.MathF.Max(1f, System.MathF.Round(chainDamage * chainDamageDecay));
		}
	}

	private static NPC FindNextStormforgedTarget(Vector2 origin, HashSet<int> hitTargets, float chainRangePixels)
	{
		NPC closestTarget = null;
		float closestDistanceSquared = chainRangePixels * chainRangePixels;

		for (int i = 0; i < Main.maxNPCs; i++) {
			NPC candidate = Main.npc[i];
			if (!candidate.active
				|| candidate.friendly
				|| candidate.dontTakeDamage
				|| !candidate.CanBeChasedBy()
				|| hitTargets.Contains(candidate.whoAmI)) {
				continue;
			}

			float distanceSquared = Vector2.DistanceSquared(origin, candidate.Center);
			if (distanceSquared > closestDistanceSquared) {
				continue;
			}

			closestDistanceSquared = distanceSquared;
			closestTarget = candidate;
		}

		return closestTarget;
	}

	private static void SpawnStormforgedDust(Vector2 start, Vector2 end)
	{
		Vector2 offset = end - start;
		int dustCount = 8;

		for (int i = 0; i <= dustCount; i++) {
			Vector2 position = Vector2.Lerp(start, end, i / (float)dustCount);
			Dust dust = Dust.NewDustPerfect(position, DustID.Electric);
			dust.noGravity = true;
			dust.velocity = offset.SafeNormalize(Vector2.UnitX).RotatedByRandom(0.35f) * Main.rand.NextFloat(0.4f, 1.1f);
			dust.scale = 0.9f;
		}
	}
}
