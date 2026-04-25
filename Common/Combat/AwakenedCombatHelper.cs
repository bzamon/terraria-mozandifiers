using Microsoft.Xna.Framework;
using Mozandifiers.Common.Players;
using Mozandifiers.Content.Prefixes.Weapons;
using Terraria;

namespace Mozandifiers.Common.Combat;

internal static class AwakenedCombatHelper
{
	internal static bool TryGetSweetSpotSegment(Player player, Item item, float scaleMultiplier, out Vector2 start, out Vector2 sweetSpotStart, out Vector2 tipPosition)
	{
		start = Vector2.Zero;
		sweetSpotStart = Vector2.Zero;
		tipPosition = Vector2.Zero;

		if (player == null
			|| !player.active
			|| player.dead
			|| item == null
			|| item.IsAir) {
			return false;
		}

		AwakenedPlayer awakenedPlayer = player.GetModPlayer<AwakenedPlayer>();
		if (!awakenedPlayer.TryGetCurrentSwingHitbox(out Rectangle swingHitbox)) {
			return false;
		}

		start = player.RotatedRelativePoint(
			player.MountedCenter + new Vector2(player.direction * 8f, -4f * player.gravDir),
			true);

		float rotation = player.itemRotation;
		if (player.direction < 0) {
			rotation += MathHelper.Pi;
		}

		Vector2 swingDirection = rotation.ToRotationVector2();
		if (player.gravDir < 0f) {
			swingDirection.Y *= -1f;
		}

		if (!TryProjectHitboxOntoSwing(start, swingDirection, swingHitbox, out float segmentStartDistance, out float segmentLength, out _)) {
			return false;
		}

		Vector2 segmentStart = start + swingDirection * segmentStartDistance;
		tipPosition = segmentStart + swingDirection * segmentLength;
		sweetSpotStart = segmentStart + swingDirection * (segmentLength * AwakenedPrefix.SweetSpotStartNormalized);
		return true;
	}

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

		if (!TryGetSweetSpotSegment(player, item, scaleMultiplier, out Vector2 start, out _, out tipPosition)) {
			return false;
		}

		AwakenedPlayer awakenedPlayer = player.GetModPlayer<AwakenedPlayer>();
		if (!awakenedPlayer.TryGetCurrentSwingHitbox(out Rectangle swingHitbox)) {
			return false;
		}

		Vector2 swingDirection = (tipPosition - start).SafeNormalize(Vector2.UnitX);
		if (!TryProjectHitboxOntoSwing(start, swingDirection, swingHitbox, out float segmentStartDistance, out float bladeLength, out float bladeThickness)) {
			return false;
		}

		Vector2 segmentStart = start + swingDirection * segmentStartDistance;
		Vector2 end = segmentStart + swingDirection * bladeLength;

		float collisionPoint = 0f;
		bool hit = Collision.CheckAABBvLineCollision(
			target.position,
			target.Size,
			segmentStart,
			end,
			bladeThickness,
			ref collisionPoint);

		if (!hit) {
			Vector2 segment = end - start;
			float lengthSquared = segment.LengthSquared();
			if (lengthSquared <= 0.0001f) {
				return false;
			}

			float projected = Vector2.Dot(target.Center - segmentStart, segment) / lengthSquared;
			projected = MathHelper.Clamp(projected, 0f, 1f);
			Vector2 nearestPoint = segmentStart + segment * projected;
			float distanceToTarget = Vector2.Distance(nearestPoint, target.Center);
			if (distanceToTarget > bladeThickness + System.MathF.Max(target.width, target.height) * 0.2f) {
				return false;
			}

			collisionPoint = projected * bladeLength;
		}

		tipProgress = MathHelper.Clamp(collisionPoint / bladeLength, 0f, 1f);
		hitPosition = segmentStart + swingDirection * collisionPoint;
		return tipProgress >= AwakenedPrefix.SweetSpotStartNormalized;
	}

	private static bool TryProjectHitboxOntoSwing(Vector2 origin, Vector2 direction, Rectangle swingHitbox, out float segmentStartDistance, out float segmentLength, out float bladeThickness)
	{
		segmentStartDistance = 0f;
		segmentLength = 0f;
		bladeThickness = 0f;

		if (swingHitbox.Width <= 0 || swingHitbox.Height <= 0 || direction.LengthSquared() <= 0.0001f) {
			return false;
		}

		Vector2 tangent = direction.RotatedBy(MathHelper.PiOver2);
		Vector2[] corners = {
			new Vector2(swingHitbox.Left, swingHitbox.Top),
			new Vector2(swingHitbox.Right, swingHitbox.Top),
			new Vector2(swingHitbox.Left, swingHitbox.Bottom),
			new Vector2(swingHitbox.Right, swingHitbox.Bottom)
		};

		float minAlong = float.MaxValue;
		float maxAlong = float.MinValue;
		float minAcross = float.MaxValue;
		float maxAcross = float.MinValue;
		for (int i = 0; i < corners.Length; i++) {
			Vector2 relative = corners[i] - origin;
			float along = Vector2.Dot(relative, direction);
			float across = System.MathF.Abs(Vector2.Dot(relative, tangent));
			minAlong = System.MathF.Min(minAlong, along);
			maxAlong = System.MathF.Max(maxAlong, along);
			minAcross = System.MathF.Min(minAcross, across);
			maxAcross = System.MathF.Max(maxAcross, across);
		}

		segmentStartDistance = System.MathF.Max(0f, minAlong);
		float segmentEndDistance = System.MathF.Max(segmentStartDistance + 4f, maxAlong);
		segmentLength = segmentEndDistance - segmentStartDistance;
		bladeThickness = System.MathF.Max(
			AwakenedPrefix.MinSweetSpotThicknessPixels,
			(maxAcross - minAcross) * 0.5f);
		return segmentLength > 4f;
	}
}
