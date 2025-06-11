using System.Collections.Specialized;
using Chunks;
using Unity.Mathematics;
using UnityEngine;

namespace Utility
{
    public static class Math
    {
        public static float Round(float x, float multiple)
        {
            float below = multiple * Mathf.FloorToInt(x / multiple);
            float above = multiple * Mathf.CeilToInt(x / multiple);
            if (x - below < above - x)
            {
                return below;
            }

            return above;
        }

        public static int GetMultiple(float x, float multiple)
        {
            return Mathf.FloorToInt(x / multiple + 0.5f);
        }

        public static int GetMultipleFloored(float x, float multiple)
        {
            return Mathf.FloorToInt(x / multiple);
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

        public static Adjacencies IntToAdjacency(int2 value)
        {
            return value.x switch
            {
                1 => Adjacencies.East,
                -1 => Adjacencies.West,
                _ => value.y switch
                {
                    1 => Adjacencies.North,
                    -1 => Adjacencies.South,
                    _ => Adjacencies.None
                }
            };
        }
    }
}