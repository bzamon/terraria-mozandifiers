using System;
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

		SpawnStormforgedBurst(initialTarget.Center, 1.15f);

		HashSet<int> hitTargets = [initialTarget.whoAmI];
		NPC currentTarget = initialTarget;
		int chainDamage = (int)System.MathF.Max(1f, System.MathF.Round(sourceDamage * chainDamageDecay));

		for (int jump = 0; jump < maxChainJumps && chainDamage > 0; jump++) {
			NPC nextTarget = FindNextStormforgedTarget(currentTarget.Center, hitTargets, chainRangePixels);
			
			if (nextTarget == null) {

                break;
			}

            SpawnStormforgedDust(currentTarget.Center, nextTarget.Center);
			SpawnStormforgedBurst(nextTarget.Center, 0.95f);

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




        foreach (NPC candidate in Main.npc) {
			//NPC candidate = Main.npc[i];

			if (!candidate.active
				|| candidate.friendly
				|| candidate.dontTakeDamage
				|| (!candidate.CanBeChasedBy() && !candidate.immortal)
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
		Vector2 direction = offset.SafeNormalize(Vector2.UnitX);
		Vector2 tangent = direction.RotatedBy(MathHelper.PiOver2);
		int dustCount = System.Math.Max(8, (int)(offset.Length() / 24f));

		for (int i = 0; i <= dustCount; i++) {
			float progress = i / (float)dustCount;
			float zigZagOffset = (i % 2 == 0 ? 1f : -1f) * 4f;
			Vector2 position = Vector2.Lerp(start, end, progress) + tangent * zigZagOffset;

			Dust electricDust = Dust.NewDustPerfect(position, DustID.Electric);
			electricDust.noGravity = true;
			electricDust.velocity = direction.RotatedByRandom(0.35f) * Main.rand.NextFloat(0.4f, 1.1f);
			electricDust.scale = 0.95f;

			if (i % 2 == 0) {
				Dust sparkDust = Dust.NewDustPerfect(position + tangent * 1.5f, DustID.BlueTorch);
				sparkDust.noGravity = true;
				sparkDust.velocity = tangent * Main.rand.NextFloat(-0.5f, 0.5f);
				sparkDust.scale = 0.75f;
				sparkDust.fadeIn = 0.85f;
			}
		}
	}

	private static void SpawnStormforgedBurst(Vector2 position, float scale)
	{
		Lighting.AddLight(position, 0.18f * scale, 0.28f * scale, 0.34f * scale);

		for (int i = 0; i < 6; i++) {
			Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(0.9f, 1.9f) * scale;

			Dust electricDust = Dust.NewDustPerfect(position, DustID.Electric, velocity);
			electricDust.noGravity = true;
			electricDust.scale = 1f * scale;

			Dust sparkDust = Dust.NewDustPerfect(position, DustID.BlueTorch, velocity * 0.65f);
			sparkDust.noGravity = true;
			sparkDust.scale = 0.75f * scale;
			sparkDust.fadeIn = 0.9f;
		}
	}
}
