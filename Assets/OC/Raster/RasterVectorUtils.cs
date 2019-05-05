using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OC.Raster
{
    internal static class RasterVectorUtils
    {
        public static readonly Vector3 MaxVector3 = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        public static readonly Vector3 MinVector3 = new Vector3(-float.MaxValue, -float.MaxValue, -float.MaxValue);

        public static Vector3 Cross(Vector3 a, Vector3 b)
        {
            return new Vector3(
                a.y * b.z - a.z * b.y,
                a.z * b.x - a.x * b.z,
                a.x * b.y - a.y * b.x);
        }

        public static Vector3 Scale(Vector3 a, float s)
        {
            return new Vector3(a.x * s, a.y * s, a.z * s);
        }

        public static Vector3 ScaleInv(Vector3 a, float s)
        {
            return new Vector3(a.x / s, a.y / s, a.z / s);
        }

        public static Vector3 Scale(Vector3 a, Vector3 s)
        {
            return new Vector3(a.x * s.x, a.y * s.y, a.z * s.z);
        }

        public static Vector3 Substract(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        public static Vector3 Add(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        public static Vector3 Min(Vector3 a, Vector3 b)
        {
            return new Vector3(
                Mathf.Min(a.x, b.x),
                Mathf.Min(a.y, b.y),
                Mathf.Min(a.z, b.z)
                );
        }


        public static Vector3 Max(Vector3 a, Vector3 b)
        {
            return new Vector3(
                Mathf.Max(a.x, b.x),
                Mathf.Max(a.y, b.y),
                Mathf.Max(a.z, b.z)
            );
        }

        public static Vector2 Scale(Vector2 a, float s)
        {
            return new Vector2(a.x * s, a.y * s);
        }

        public static Vector2 ScaleInv(Vector2 a, float s)
        {
            return new Vector2(a.x / s, a.y /s);
        }

        public static Vector2 Substract(Vector2 a, Vector2 b)
        {
            return new Vector2(a.x - b.x, a.y - b.y);
        }

        public static Vector2 Add(Vector2 a, Vector2 b)
        {
            return new Vector2(a.x + b.x, a.y + b.y);
        }
    }
}
