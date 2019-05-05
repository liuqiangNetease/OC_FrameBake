using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OC
{
    public struct Vertex
    {
        public Vector4 point;
        public float onePerZ;

        public Vertex(Vector3 point)
        {
            this.point = point;
            this.point.w = 1;         
            onePerZ = 1;
        }

        public Vertex(Vertex v)
        {
            point = v.point;
            onePerZ = 1;
        }
    }
    public class Triangle
    {
        public MeshRenderer owner;
        public Vertex A;
        public Vertex B;
        public Vertex C;
    }
}
