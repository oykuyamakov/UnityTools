using UnityEngine;

namespace Cadi.Scripts.Utility.Extensions
{
    public static class Vector3Extensions
    {
        // -------------------------
        // Axis setters
        // -------------------------

        public static Vector3 WithX(this Vector3 vector, float x)
        {
            vector.x = x;
            return vector;
        }

        public static Vector3 WithY(this Vector3 vector, float y)
        {
            vector.y = y;
            return vector;
        }

        public static Vector3 WithZ(this Vector3 vector, float z)
        {
            vector.z = z;
            return vector;
        }

        public static Vector3 WithXY(this Vector3 vector, float x, float y)
        {
            vector.x = x;
            vector.y = y;
            return vector;
        }

        public static Vector3 WithXZ(this Vector3 vector, float x, float z)
        {
            vector.x = x;
            vector.z = z;
            return vector;
        }

        public static Vector3 WithYZ(this Vector3 vector, float y, float z)
        {
            vector.y = y;
            vector.z = z;
            return vector;
        }

        public static Vector3 WithXYZ(this Vector3 vector, float x, float y, float z)
        {
            vector.x = x;
            vector.y = y;
            vector.z = z;
            return vector;
        }
        
        public static Vector3 WithXYRelative(this Vector3 v, float x, float y)
        {
            v.x += x;
            v.y += y;
            return v;
        }

        public static Vector3 WithYZRelative(this Vector3 v, float y, float z)
        {
            v.y += y;
            v.z += z;
            return v;
        }

        public static Vector3 WithXZRelative(this Vector3 v, float x, float z)
        {
            v.x += x;
            v.z += z;
            return v;
        }
        
        public static Vector3 WithXRelative(this Vector3 v, float x)
        {
            v.x += x;
            return v;
        }

        public static Vector3 WithYRelative(this Vector3 v, float y)
        {
            v.y += y;
            return v;
        }

        public static Vector3 WithZRelative(this Vector3 v, float z)
        {
            v.z += z;
            return v;
        }

        // -------------------------
        // Axis adders
        // -------------------------

        public static Vector3 AddX(this Vector3 vector, float x)
        {
            vector.x += x;
            return vector;
        }

        public static Vector3 AddY(this Vector3 vector, float y)
        {
            vector.y += y;
            return vector;
        }

        public static Vector3 AddZ(this Vector3 vector, float z)
        {
            vector.z += z;
            return vector;
        }

        // -------------------------
        // Axis multipliers
        // -------------------------

        public static Vector3 MultiplyX(this Vector3 vector, float x)
        {
            vector.x *= x;
            return vector;
        }

        public static Vector3 MultiplyY(this Vector3 vector, float y)
        {
            vector.y *= y;
            return vector;
        }

        public static Vector3 MultiplyZ(this Vector3 vector, float z)
        {
            vector.z *= z;
            return vector;
        }

        // -------------------------
        // Flat helpers
        // -------------------------

        public static Vector3 WithZeroX(this Vector3 vector)
        {
            vector.x = 0f;
            return vector;
        }

        public static Vector3 WithZeroY(this Vector3 vector)
        {
            vector.y = 0f;
            return vector;
        }

        public static Vector3 WithZeroZ(this Vector3 vector)
        {
            vector.z = 0f;
            return vector;
        }

        public static Vector3 FlattenY(this Vector3 vector)
        {
            vector.y = 0f;
            return vector;
        }

        public static Vector3 FlattenX(this Vector3 vector)
        {
            vector.x = 0f;
            return vector;
        }

        public static Vector3 FlattenZ(this Vector3 vector)
        {
            vector.z = 0f;
            return vector;
        }

        // -------------------------
        // Clamp helpers
        // -------------------------

        public static Vector3 ClampX(this Vector3 vector, float min, float max)
        {
            vector.x = Mathf.Clamp(vector.x, min, max);
            return vector;
        }

        public static Vector3 ClampY(this Vector3 vector, float min, float max)
        {
            vector.y = Mathf.Clamp(vector.y, min, max);
            return vector;
        }

        public static Vector3 ClampZ(this Vector3 vector, float min, float max)
        {
            vector.z = Mathf.Clamp(vector.z, min, max);
            return vector;
        }

        // -------------------------
        // Absolute / sign helpers
        // -------------------------

        public static Vector3 Abs(this Vector3 vector)
        {
            vector.x = Mathf.Abs(vector.x);
            vector.y = Mathf.Abs(vector.y);
            vector.z = Mathf.Abs(vector.z);
            return vector;
        }

        public static Vector3 Sign(this Vector3 vector)
        {
            vector.x = Mathf.Sign(vector.x);
            vector.y = Mathf.Sign(vector.y);
            vector.z = Mathf.Sign(vector.z);
            return vector;
        }

        // -------------------------
        // Conversions
        // -------------------------

        public static Vector2 ToXZ(this Vector3 vector)
        {
            return new Vector2(vector.x, vector.z);
        }

        public static Vector2 ToXY(this Vector3 vector)
        {
            return new Vector2(vector.x, vector.y);
        }

        public static Vector2 ToYZ(this Vector3 vector)
        {
            return new Vector2(vector.y, vector.z);
        }

        public static Vector3Int RoundToInt(this Vector3 vector)
        {
            return new Vector3Int(
                Mathf.RoundToInt(vector.x),
                Mathf.RoundToInt(vector.y),
                Mathf.RoundToInt(vector.z));
        }

        public static Vector3Int FloorToInt(this Vector3 vector)
        {
            return new Vector3Int(
                Mathf.FloorToInt(vector.x),
                Mathf.FloorToInt(vector.y),
                Mathf.FloorToInt(vector.z));
        }

        public static Vector3Int CeilToInt(this Vector3 vector)
        {
            return new Vector3Int(
                Mathf.CeilToInt(vector.x),
                Mathf.CeilToInt(vector.y),
                Mathf.CeilToInt(vector.z));
        }

        // -------------------------
        // Distance helpers
        // -------------------------

        public static float DistanceXZ(this Vector3 from, Vector3 to)
        {
            from.y = 0f;
            to.y = 0f;
            return Vector3.Distance(from, to);
        }

        public static float SqrDistanceXZ(this Vector3 from, Vector3 to)
        {
            from.y = 0f;
            to.y = 0f;
            return (from - to).sqrMagnitude;
        }

        public static Vector3 DirectionTo(this Vector3 from, Vector3 to)
        {
            return (to - from).normalized;
        }

        public static Vector3 DirectionToXZ(this Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            direction.y = 0f;
            return direction.normalized;
        }

        // -------------------------
        // Approx helpers
        // -------------------------

        public static bool Approximately(this Vector3 a, Vector3 b, float tolerance = 0.0001f)
        {
            return (a - b).sqrMagnitude <= tolerance * tolerance;
        }

        // -------------------------
        // Safe normalization
        // -------------------------

        public static Vector3 SafeNormalized(this Vector3 vector)
        {
            if (vector.sqrMagnitude < 0.000001f)
            {
                return Vector3.zero;
            }

            return vector.normalized;
        }
    }
}