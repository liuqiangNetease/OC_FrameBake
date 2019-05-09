using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
namespace OC
{
    public enum Side
    {
        NO_SIDE,
        POSITIVE_SIDE,
        NEGATIVE_SIDE,
        BOTH_SIDE
    };
    public class MathUtil
    {

        public static float Lerp(float a, float b, float t)
        {
            if (t <= 0)
            {
                return a;
            }
            else if (t >= 1)
            {
                return b;
            }
            else
            {
                return b * t + (1 - t) * a;
            }
        }

        public static void ScreenSpaceLerpVertex(ref Vertex v, Vertex v1, Vertex v2, float t)
        {
            v.onePerZ = MathUtil.Lerp(v1.onePerZ, v2.onePerZ, t);
        }

        public static Side GetPlaneSide(Plane plane, Bounds bounds)
        {
            Vector3 center = bounds.center;
            float dist = plane.GetDistanceToPoint(center);

            float maxAbsDist = Mathf.Abs(plane.normal.x * bounds.extents.x) + Mathf.Abs(plane.normal.y * bounds.extents.y) + Mathf.Abs(plane.normal.z * bounds.extents.z);

            if (dist < -maxAbsDist)
                return Side.NEGATIVE_SIDE;
            if (dist > maxAbsDist)
                return Side.POSITIVE_SIDE;
            return Side.BOTH_SIDE;
        }
    }
}
#endif

