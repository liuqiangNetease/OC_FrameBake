
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OC.Raster
{
    internal interface IRasterMesh
    {
        int TriangleCount { get; }
        void GetTriangle(int index, Vector3[] vertices);
        RasterBounds WorldBounds { get; }
    }

    internal static class RasterMeshFactory
    {
        public static IRasterMesh CreateRasterMesh(Collider collider)
        {
            if (collider is MeshCollider)
            {
                var meshCollider = collider as MeshCollider;
                var mesh = meshCollider.sharedMesh;
                if (mesh != null)
                {
                    return new RasterMesh(meshCollider.sharedMesh, collider.transform);
                }
                else
                {
                    Debug.LogErrorFormat("The shared mesh is null for mesh collider {0}", collider.name);
                    return DummyMesh.Instance;
                }
                
            }else if (collider is BoxCollider)
            {
                return new RasterBoxMesh(collider as BoxCollider);
            }else if (collider is SphereCollider)
            {
                return new RasterSphereMesh(collider as SphereCollider);
            }
            else if (collider is CapsuleCollider)
            {
                return new RasterCapsuleMesh(collider as CapsuleCollider);
            }
            else if (collider is TerrainCollider)
            {
                return new RasterTerrainMesh(collider as TerrainCollider);
            }

            Debug.LogErrorFormat("Can not create RasterMesh for collider {0} type {1}", collider.name, collider.GetType());
            return DummyMesh.Instance;
        }
    }

    internal class RasterBaseMesh : IRasterMesh
    {
        private RasterBounds _bounds;
        protected int[] _triangles;
        protected Vector3[] _vertices;
        protected Matrix4x4 _localToWorldMatrix;

        public int TriangleCount
        {
            get { return _triangles.Length / 3; }
        }

        public void GetTriangle(int index, Vector3[] vertices)
        {
            vertices[0] = ToWorldVertex(_vertices[_triangles[3 * index]]);
            vertices[1] = ToWorldVertex(_vertices[_triangles[3 * index + 1]]);
            vertices[2] = ToWorldVertex(_vertices[_triangles[3 * index + 2]]);
        }

        private Vector3 ToWorldVertex(Vector3 v)
        {
            return TransformVertex1(_localToWorldMatrix.MultiplyPoint3x4(TransformVertex0(v)));
        }

        protected virtual Vector3 TransformVertex0(Vector3 v)
        {
            return v;
        }

        protected virtual Vector3 TransformVertex1(Vector3 v)
        {
            return v;
        }

        public RasterBounds WorldBounds
        {
            get { return _bounds; }
        }

        public RasterBaseMesh(Transform transform)
        {
            _localToWorldMatrix = transform.localToWorldMatrix;
        }

        public RasterBaseMesh(Matrix4x4 mat)
        {
            _localToWorldMatrix = mat;
        }

        protected void RecalcBounds()
        {
            var bmins = RasterVectorUtils.MaxVector3;
            var bmaxs = RasterVectorUtils.MinVector3;

            for (int vidx = 0; vidx < _vertices.Length; ++vidx)
            {
                var v = ToWorldVertex(_vertices[vidx]);
                bmins = RasterVectorUtils.Min(bmins, v);
                bmaxs = RasterVectorUtils.Max(bmaxs, v);
            }

            if (_vertices.Length > 0)
            {
                if (bmins.x.Equals(bmaxs.x))
                {
                    bmins.x -= 0.1f;
                    bmaxs.x += 0.1f;
                }


                if (bmins.y.Equals(bmaxs.y))
                {
                    bmins.y -= 0.1f;
                    bmaxs.y += 0.1f;
                }


                if (bmins.z.Equals(bmaxs.z))
                {
                    bmins.z -= 0.1f;
                    bmaxs.z += 0.1f;
                }
            }

            _bounds = _vertices.Length > 0 ? new RasterBounds(bmins, bmaxs) : RasterBounds.ZeroBounds;
        }
    }

    internal class DummyMesh : RasterBaseMesh
    {
        public static DummyMesh Instance = new DummyMesh();

        public DummyMesh(): base(Matrix4x4.identity)
        {
            _vertices = new Vector3[0];
            _triangles = new int[0];
            RecalcBounds();
        }
    }

    internal class RasterMesh : RasterBaseMesh
    {
        public RasterMesh(Mesh mesh, Transform transform): base(transform)
        {
            _vertices = mesh.vertices;
            _triangles = mesh.triangles;
            RecalcBounds();
        }
    }

    internal class RasterBoxMesh : RasterBaseMesh
    {
        private static int[] BoxTriangles = new int[]
        {
            0, 2, 1, //face front
            0, 3, 2,
            2, 3, 4, //face top
            2, 4, 5,
            1, 2, 5, //face right
            1, 5, 6,
            0, 7, 4, //face left
            0, 4, 3,
            5, 4, 7, //face back
            5, 7, 6,
            0, 6, 7, //face bottom
            0, 1, 6
        };

        private static Vector3[] BoxVertices = new Vector3[]
        {

            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3 (0.5f, -0.5f, -0.5f),
            new Vector3 (0.5f, 0.5f, -0.5f),
            new Vector3 (-0.5f, 0.5f, -0.5f),
            new Vector3 (-0.5f, 0.5f, 0.5f),
            new Vector3 (0.5f, 0.5f, 0.5f),
            new Vector3 (0.5f, -0.5f, 0.5f),
            new Vector3 (-0.5f, -0.5f, 0.5f)
        };

        private readonly Vector3 _center;
        private readonly Vector3 _size;
        public RasterBoxMesh(BoxCollider collider): base(collider.transform)
        {
            _center = collider.center;
            _size = collider.size;

            _triangles = BoxTriangles;
            _vertices = BoxVertices;

            RecalcBounds();
        }

        protected override Vector3 TransformVertex0(Vector3 v)
        {
            return RasterVectorUtils.Scale(v, _size);
            
        }

        protected override Vector3 TransformVertex1(Vector3 v)
        {
            return RasterVectorUtils.Add(v, _center);
        }
    }

    internal class RasterSphereMesh : RasterBaseMesh
    {
        private static Vector3[] SphereVertices;
        private static int[] SphereTriangles;

        private readonly Vector3 _center;
        private readonly float _radius;

        public RasterSphereMesh(SphereCollider collider) : base(collider.transform)
        {
            SetupSphereMesh();

            _center = collider.center;
            _radius = collider.radius;

            _triangles = SphereTriangles;
            _vertices = SphereVertices;

            RecalcBounds();
        }

        private static void SetupSphereMesh()
        {
            if (SphereVertices != null)
                return;

            float radius = 1f;
            // Longitude |||
            int nbLong = 24;
            // Latitude ---
            int nbLat = 16;

#region Vertices
            Vector3[] vertices = new Vector3[(nbLong + 1) * nbLat + 2];
            float _pi = Mathf.PI;
            float _2pi = _pi * 2f;

            vertices[0] = Vector3.up * radius;
            for (int lat = 0; lat < nbLat; lat++)
            {
                float a1 = _pi * (float)(lat + 1) / (nbLat + 1);
                float sin1 = Mathf.Sin(a1);
                float cos1 = Mathf.Cos(a1);

                for (int lon = 0; lon <= nbLong; lon++)
                {
                    float a2 = _2pi * (float)(lon == nbLong ? 0 : lon) / nbLong;
                    float sin2 = Mathf.Sin(a2);
                    float cos2 = Mathf.Cos(a2);

                    vertices[lon + lat * (nbLong + 1) + 1] = new Vector3(sin1 * cos2, cos1, sin1 * sin2) * radius;
                }
            }
            vertices[vertices.Length - 1] = Vector3.up * -radius;
#endregion


#region Triangles
            int nbFaces = vertices.Length;
            int nbTriangles = nbFaces * 2;
            int nbIndexes = nbTriangles * 3;
            int[] triangles = new int[nbIndexes];

            //Top Cap
            int i = 0;
            for (int lon = 0; lon < nbLong; lon++)
            {
                triangles[i++] = lon + 2;
                triangles[i++] = lon + 1;
                triangles[i++] = 0;
            }

            //Middle
            for (int lat = 0; lat < nbLat - 1; lat++)
            {
                for (int lon = 0; lon < nbLong; lon++)
                {
                    int current = lon + lat * (nbLong + 1) + 1;
                    int next = current + nbLong + 1;

                    triangles[i++] = current;
                    triangles[i++] = current + 1;
                    triangles[i++] = next + 1;

                    triangles[i++] = current;
                    triangles[i++] = next + 1;
                    triangles[i++] = next;
                }
            }

            //Bottom Cap
            for (int lon = 0; lon < nbLong; lon++)
            {
                triangles[i++] = vertices.Length - 1;
                triangles[i++] = vertices.Length - (lon + 2) - 1;
                triangles[i++] = vertices.Length - (lon + 1) - 1;
            }
#endregion

            SphereVertices = vertices;
            SphereTriangles = triangles;
        }

        protected override Vector3 TransformVertex0(Vector3 v)
        {
            return RasterVectorUtils.Scale(v, _radius);
        }

        protected override Vector3 TransformVertex1(Vector3 v)
        {
            return RasterVectorUtils.Add(v, _center);
        }
    }

    internal class RasterCapsuleMesh : RasterBaseMesh
    {
        private readonly Vector3 _center;
        public RasterCapsuleMesh(CapsuleCollider collider) : base(collider.transform)
        {
            _center = collider.center;
            SetupCapsuleMesh(collider.radius, collider.height, collider.direction);
        }

        private void SetupCapsuleMesh(float radius, float height, int direction)
        {
            const int SEGMENTS = 24;
            var segments = SEGMENTS;
            // make segments an even number
            if (segments % 2 != 0)
                segments++;

            // extra vertex on the seam
            int points = segments + 1;

            // calculate points around a circle
            float[] pX = new float[points];
            float[] pZ = new float[points];
            float[] pY = new float[points];
            float[] pR = new float[points];

            float calcH = 0f;
            float calcV = 0f;

            for (int i = 0; i < points; i++)
            {
                pX[i] = Mathf.Sin(calcH * Mathf.Deg2Rad);
                pZ[i] = Mathf.Cos(calcH * Mathf.Deg2Rad);
                pY[i] = Mathf.Cos(calcV * Mathf.Deg2Rad);
                pR[i] = Mathf.Sin(calcV * Mathf.Deg2Rad);

                calcH += 360f / (float)segments;
                calcV += 180f / (float)segments;
            }

            // - Vertices -
            Vector3[] vertices = new Vector3[points * (points + 1)];
            int ind = 0;

            // Y-offset is half the height minus the diameter
            float yOff = (height - (radius * 2f)) * 0.5f;
            if (yOff < 0)
                yOff = 0;

            // Top Hemisphere
            int top = Mathf.CeilToInt((float)points * 0.5f);

            for (int y = 0; y < top; y++)
            {
                for (int x = 0; x < points; x++)
                {
                    vertices[ind] = new Vector3(pX[x] * pR[y], pY[y], pZ[x] * pR[y]) * radius;
                    vertices[ind].y = yOff + vertices[ind].y;

                    ind++;
                }
            }

            // Bottom Hemisphere
            int btm = Mathf.FloorToInt((float)points * 0.5f);

            for (int y = btm; y < points; y++)
            {
                for (int x = 0; x < points; x++)
                {
                    vertices[ind] = new Vector3(pX[x] * pR[y], pY[y], pZ[x] * pR[y]) * radius;
                    vertices[ind].y = -yOff + vertices[ind].y;

                    ind++;
                }
            }


            // - Triangles -

            int[] triangles = new int[(segments * (segments + 1) * 2 * 3)];

            for (int y = 0, t = 0; y < segments + 1; y++)
            {
                for (int x = 0; x < segments; x++, t += 6)
                {
                    triangles[t + 0] = ((y + 0) * (segments + 1)) + x + 0;
                    triangles[t + 1] = ((y + 1) * (segments + 1)) + x + 0;
                    triangles[t + 2] = ((y + 1) * (segments + 1)) + x + 1;

                    triangles[t + 3] = ((y + 0) * (segments + 1)) + x + 1;
                    triangles[t + 4] = ((y + 0) * (segments + 1)) + x + 0;
                    triangles[t + 5] = ((y + 1) * (segments + 1)) + x + 1;
                }
            }

            //x-axis
            if (direction == 0)
            {
                for (int i = 0; i < vertices.Length; ++i)
                {
                    var v = vertices[i];

                    var x = v.x;
                    v.x = v.y;
                    v.y = v.z;
                    v.z = x;
                    vertices[i] = v;
                }
            }
            //z-axis
            else if (direction == 2)
            {
                for (int i = 0; i < vertices.Length; i++)
                {
                    var v = vertices[i];

                    var z = v.z;
                    v.z = v.y;
                    v.y = v.x;
                    v.x = z;
                    vertices[i] = v;
                }
            }

            _vertices = vertices;
            _triangles = triangles;
            
            RecalcBounds();
        }

        protected override Vector3 TransformVertex1(Vector3 v)
        {
            return RasterVectorUtils.Add(v, _center);
        }
    }

    internal class RasterTerrainMesh : RasterBaseMesh
    {
        public RasterTerrainMesh(TerrainCollider collider) : base(collider.transform)
        {
            SetpUpTerrainMesh(collider.terrainData, out _vertices, out _triangles);
            RecalcBounds();
        }

        private static void SetpUpTerrainMesh(TerrainData terrainData, out Vector3[] outVerts, out int[] outTris)
        {

            // Create all lists needed
            List<int> indices = new List<int>();
            List<Vector3> vertices = new List<Vector3>();
            //List<Vector3> normals = new List<Vector3>();

            Vector3 heightmapScale = terrainData.heightmapScale;

            // Compute fractions in x and z direction
            float dx = 1f / (terrainData.heightmapWidth - 1);
            float dz = 1f / (terrainData.heightmapHeight - 1);

            for (int ix = 0; ix < terrainData.heightmapWidth; ix++)
            {
                float x = ix * heightmapScale.x;
                float ddx = ix * dx;

                for (int iz = 0; iz < terrainData.heightmapHeight; iz++)
                {
                    float z = iz * heightmapScale.z;
                    float ddz = iz * dz;

                    // Sample height and normal at dx, dz
                    Vector3 point = new Vector3(x, terrainData.GetInterpolatedHeight(ddx, ddz), z);
                    //Vector3 normal = terrainData.GetInterpolatedNormal(ddx, ddz);

                    // Add vertex and normal to the lists
                    vertices.Add(point);

                    //normals.Add(normal);
                }
            }

            int w = terrainData.heightmapWidth;
            int h = terrainData.heightmapHeight;

            // Add triangle pairs (quad)
            for (int xx = 0; xx < w - 1; xx++)
            {
                for (int zz = 0; zz < h - 1; zz++)
                {
                    int a = zz + xx * w;
                    int b = a + w + 1;
                    int c = a + w;
                    int d = a + 1;

                    // Add indices in clockwise order
                    indices.Add(a);
                    indices.Add(b);
                    indices.Add(c);

                    indices.Add(a);
                    indices.Add(d);
                    indices.Add(b); 
                }
            }

            outVerts = vertices.ToArray();
            //mesh.normals = normals.ToArray();
            outTris = indices.ToArray();
        }
    }


}
#endif
