using System;

namespace JabaSurvivol.Core
{

    /// <summary>
    /// Engine-independent math used by input, movement, and combat.
    /// </summary>
    public static class CombatMath
    {
        /// <summary>
        /// Normalizes a two-axis stick reading with a dead zone. The output is
        /// clamped to a unit circle so diagonal input is not faster.
        /// </summary>
        public static bool TryNormalizeStick(float x, float y, float deadZone, out float normalizedX, out float normalizedY)
        {
            normalizedX = 0f;
            normalizedY = 0f;

            var length = (float)Math.Sqrt((x * x) + (y * y));
            if (length <= deadZone || length <= float.Epsilon)
            {
                return false;
            }

            var scale = length > 1f ? 1f / length : 1f;
            normalizedX = x * scale;
            normalizedY = y * scale;
            return true;
        }

        /// <summary>
        /// Returns a unit direction, or false when the delta is effectively zero.
        /// </summary>
        public static bool TryDirection(float deltaX, float deltaY, out float normalizedX, out float normalizedY)
        {
            normalizedX = 0f;
            normalizedY = 0f;

            var length = (float)Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
            if (length <= 0.0001f)
            {
                return false;
            }

            normalizedX = deltaX / length;
            normalizedY = deltaY / length;
            return true;
        }

        public static float Clamp(float value, float minimum, float maximum)
        {
            if (minimum > maximum)
            {
                throw new ArgumentOutOfRangeException(nameof(minimum));
            }

            return value < minimum ? minimum : value > maximum ? maximum : value;
        }

        /// <summary>
        /// Clamps a position to an arena while keeping the actor radius inside the
        /// bounds.
        /// </summary>
        public static void ClampToArena(
            float x,
            float y,
            float halfWidth,
            float halfHeight,
            float radius,
            out float clampedX,
            out float clampedY)
        {
            var limitX = Math.Max(0f, halfWidth - radius);
            var limitY = Math.Max(0f, halfHeight - radius);

            clampedX = Clamp(x, -limitX, limitX);
            clampedY = Clamp(y, -limitY, limitY);
        }

        /// <summary>
        /// Applies a damage multiplier while never returning a negative result.
        /// </summary>
        public static float ApplyDamageMultiplier(float baseDamage, float multiplier)
        {
            if (baseDamage <= 0f)
            {
                return 0f;
            }

            return baseDamage * Math.Max(0f, multiplier);
        }

        /// <summary>
        /// Returns true when two circles overlap.
        /// </summary>
        public static bool CirclesOverlap(
            float ax,
            float ay,
            float aRadius,
            float bx,
            float by,
            float bRadius)
        {
            var dx = ax - bx;
            var dy = ay - by;
            var radius = aRadius + bRadius;
            return (dx * dx) + (dy * dy) <= radius * radius;
        }
    }
}
