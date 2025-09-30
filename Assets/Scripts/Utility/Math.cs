using Unity.Mathematics;
using UnityEngine;
using Chunks;
using Unity.Burst;

namespace Utility
{
    [BurstCompile]
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
        
        public static Vector3 GetGroundIntersectionPoint(Vector3 pos, Vector3 dir)
        {
            Ray ray = new Ray(pos, dir);
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

        [BurstCompile]
        public static float InOutSine(float value)
        {
            return -(math.cos(math.PI * value) - 1.0f) / 2.0f;
        }

        
        private const float c4 = (2.0f * math.PI) / 3.0f;
        [BurstCompile]
        public static float EaseOutElastic(float value)
        {
            return math.pow(2, -10 * value) * math.sin((value * 10f - 0.75f) * c4) + 1.0f;
        }
        
        [BurstCompile]
        public static float EaseOutElastic_Exact(float value)
        {
            return value == 0 
                ? 0
                : Mathf.Approximately(value, 1)
                ? 1
                : math.pow(2, -10 * value) * math.sin((value * 10f - 0.75f) * c4) + 1.0f;
        }

        [BurstCompile]
        public static float PunchLinear(float value)
        {
            return -math.abs(2 * value - 1) + 1;
        }
        
        [BurstCompile]
        public static float PunchInOut(float value)
        {
            return value > 0.09f
                ? 1.0f - math.sin(((value - 0.09f) * math.PI * 1.098901099f) / 2.0f)
                : math.log(value + 0.01f) + 2.0f;
        }
    }
}