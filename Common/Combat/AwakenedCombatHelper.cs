using Microsoft.Xna.Framework;
using Mozandifiers.Content.Prefixes.Weapons;
using Terraria;

namespace Mozandifiers.Common.Combat;

internal static class AwakenedCombatHelper
{
	internal static bool TryEvaluateSweetSpot(Player player, Item item, NPC target, float scaleMultiplier, out float tipProgress, out Vector2 hitPosition, out Vector2 tipPosition)
	{
		tipProgress = 0f;
		hitPosition = Vector2.Zero;
		tipPosition = Vector2.Zero;

		if (player == null
			|| !player.active
			|| player.dead
			|| item == null
			|| item.IsAir
			|| target == null
			|| !target.active) {
			return false;
		}

		Vector2 start = player.RotatedRelativePoint(
			player.MountedCenter + new Vector2(player.direction * 8f, -4f * player.gravDir),
			true);
		float effectiveScale = player.GetAdjustedItemScale(item) * scaleMultiplier;
		float bladeLength = System.MathF.Max(item.width, item.height) * effectiveScale + AwakenedPrefix.BladeLengthPaddingPixels;
		float bladeThickness = System.MathF.Max(
			AwakenedPrefix.MinSweetSpotThicknessPixels,
			System.MathF.Min(item.width, item.height) * effectiveScale * AwakenedPrefix.SweetSpotThicknessMultiplier);

		float rotation = player.itemRotation;
		if (player.direction < 0) {
			rotation += MathHelper.Pi;
		}

		Vector2 swingDirection = rotation.ToRotationVector2();
		if (player.gravDir < 0f) {
			swingDirection.Y *= -1f;
		}

		Vector2 end = start + swingDirection * bladeLength;
		tipPosition = end;

		float collisionPoint = 0f;
		bool hit = Collision.CheckAABBvLineCollision(
			target.position,
			target.Size,
			start,
			end,
			bladeThickness,
			ref collisionPoint);

		if (!hit) {
			Vector2 segment = end - start;
			float lengthSquared = segment.LengthSquared();
			if (lengthSquared <= 0.0001f) {
				return false;
			}

			float projected = Vector2.Dot(target.Center - start, segment) / lengthSquared;
			projected = MathHelper.Clamp(projected, 0f, 1f);
			Vector2 nearestPoint = start + segment * projected;
			float distanceToTarget = Vector2.Distance(nearestPoint, target.Center);
			if (distanceToTarget > bladeThickness + System.MathF.Max(target.width, target.height) * 0.2f) {
				return false;
			}

			collisionPoint = projected * bladeLength;
		}

		tipProgress = MathHelper.Clamp(collisionPoint / bladeLength, 0f, 1f);
		hitPosition = start + swingDirection * collisionPoint;
		return tipProgress >= AwakenedPrefix.SweetSpotStartNormalized;
	}
}
