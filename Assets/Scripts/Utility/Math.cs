using Unity.Mathematics;
using UnityEngine;
using Chunks;

namespace Utility
{
    public static class Math
    {
        public static float Round(float x, float multiple)
        {
            float below = multiple * math.floor(x / multiple);
            float above = multiple * math.ceil(x / multiple);
            if (x - below < above - x)
            {
                return below;
            }

            return above;
        }

        public static int GetMultiple(float x, float multiple)
        {
            return (int)math.floor(x / multiple + 0.5f);
        }

        public static int GetMultipleFloored(float x, float multiple)
        {
            return (int)math.floor(x / multiple);
        }
        
        public static int GetMultipleCeil(float x, float multiple)
        {
            return (int)math.ceil(x / multiple);
        }

        public static float3 CubicLerp(float3 a, float3 b, float3 c, float t)
        {
            //return math.lerp(math.lerp(a, c, t), math.lerp(c, b, t), t);
            //t = math.clamp(t, 0f, 1f);

            // Quadratic Bézier interpolation formula:
            // P(t) = (1 - t)^2 * a + 2 * (1 - t) * t * c + t^2 * b
            float oneMinusT = 1f - t;
            float oneMinusTSquared = oneMinusT * oneMinusT;
            float tSquared = t * t;

            // Compute the interpolated value
            return oneMinusTSquared * a + 2f * oneMinusT * t * c + tSquared * b;
        }

        public static Vector3 GetGroundIntersectionPoint(Camera camera, Vector2 mousePosition)
        {
            Ray ray = camera.ScreenPointToRay(mousePosition);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (groundPlane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }

            return Vector3.zero;
        }
        
        public static Vector2 RotateVector2(Vector2 vector, float angle)
        {
            float x = vector.x * math.cos(angle) - vector.y * math.sin(angle);
            float y = vector.x * math.sin(angle) + vector.y * math.cos(angle);
            return new Vector2(x, y);
        }
        
        public static int2 Rotate90Int2(int2 v, int steps)
        {
            // Normalize steps to [0..3]
            steps = ((steps % 4) + 4) % 4;

            switch (steps)
            {
                case 1: // 90° clockwise
                    return new int2(v.y, -v.x);
                case 2: // 180°
                    return new int2(-v.x, -v.y);
                case 3: // 270° clockwise (or 90° CCW)
                    return new int2(-v.y, v.x);
                default: // 0° (no rotation)
                    return v;
            }
        }
        
        public static float2 Rotate90Float2(float2 v, int steps)
        {
            // Normalize steps to [0..3]
            steps = ((steps % 4) + 4) % 4;

            switch (steps)
            {
                case 1: // 90° clockwise
                    return new float2(v.y, -v.x);
                case 2: // 180°
                    return new float2(-v.x, -v.y);
                case 3: // 270° clockwise (or 90° CCW)
                    return new float2(-v.y, v.x);
                default: // 0° (no rotation)
                    return v;
            }
        }
    }
}