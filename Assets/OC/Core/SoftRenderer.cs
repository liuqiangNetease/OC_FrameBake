#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace OC
{
    public struct MeshTriangle
    {
        public MeshRenderer renderer;
        public List<Triangle> triangles;
    }
    public class SoftRenderer
    {
#region [ Static ]
        private static readonly SoftRenderer _instance = new SoftRenderer();

        public static SoftRenderer Instance
        {
            get { return _instance; }
        }
#endregion
        struct TagZBuffer
        {
            public float zValue; // 1/z 
            public MeshRenderer renderer;
        }

        public int Width, Height;

        private TagZBuffer[,] _zBuff;//z缓冲

        private Color[,] _frameBuff;

        public Dictionary<MeshRenderer, MeshTriangle> meshTriangles = new Dictionary<MeshRenderer, MeshTriangle>();


        public OCCamera _camera;

        public SoftRenderer()
        {
        }

        private void ClearBuff()
        {
            Array.Clear(_zBuff, 0, _zBuff.Length);
            Array.Clear(_frameBuff, 0, _frameBuff.Length);
        }

        public void Finish()
        {

        }
        public void Prepare()
        {
            var unityCamera = Camera.main;

            _camera = new OCCamera();
            _camera.pixelRect = unityCamera.pixelRect;
            _camera.aspect = unityCamera.aspect;
            _camera.far = unityCamera.farClipPlane;
            _camera.near = unityCamera.nearClipPlane;
            _camera.fovY = unityCamera.fieldOfView;
            _camera.position = unityCamera.transform.position;
            _camera.rot = unityCamera.transform.rotation;
            _camera.scale = unityCamera.transform.localScale;

            _camera.lookAt = unityCamera.transform.forward;
            _camera.up = unityCamera.transform.up;
            _camera.right = unityCamera.transform.right;
            _camera.Init();

            Width = (int)Camera.main.pixelRect.width;
            Height = (int)Camera.main.pixelRect.height;

            _zBuff = new TagZBuffer[Width, Height];
            _frameBuff = new Color[Width, Height];

            ClearBuff();

            GetAllMeshTriangles();
        }

        public void GetAllMeshTriangles()
        {
            meshTriangles.Clear();

            MeshRenderer[] mrs = UnityEngine.Object.FindObjectsOfType<MeshRenderer>();
            for (int i = 0; i < mrs.Length; i++)
            {
                MeshRenderer mr = mrs[i];
                if (mr == null || !mr.gameObject.activeSelf || !mr.enabled || mr.sharedMaterials.Length <= 0) continue;

                MeshFilter mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;

                //if (Util.TryIgnoreTransparent(mr))
                    //continue;

                var tris = GetMeshTriangles(mr);

                var meshTri = new MeshTriangle();
                meshTri.renderer = mr;
                meshTri.triangles = tris;
                meshTriangles.Add(mr, meshTri);           
            }
        }

        void ProcessAddTriangle(Triangle tri)
        {
            Vector4 clipA = tri.A.point; Vector4 clipB = tri.B.point; Vector4 clipC = tri.C.point;
            Vector3 screenNewA = Clip2Screen(clipA);
            Vector3 screenNewB = Clip2Screen(clipB);
            Vector3 screenNewC = Clip2Screen(clipC);


            Triangle transformedTriangle = new Triangle();
            transformedTriangle.A.point = screenNewA;
            transformedTriangle.B.point = screenNewB;
            transformedTriangle.C.point = screenNewC;

            transformedTriangle.A.onePerZ = 1 / screenNewA.z;
            transformedTriangle.B.onePerZ = 1 / screenNewB.z;
            transformedTriangle.C.onePerZ = 1 / screenNewC.z;

            transformedTriangle.owner = tri.owner;

            TriangleRasterization(transformedTriangle, tri.owner);
        }
        void ProcessTriangle(Triangle tri)
        {
            try
            {
                Vector3 worldPosA = tri.owner.transform.localToWorldMatrix.MultiplyPoint(tri.A.point);
                Vector3 worldPosB = tri.owner.transform.localToWorldMatrix.MultiplyPoint(tri.B.point);
                Vector3 worldPosC = tri.owner.transform.localToWorldMatrix.MultiplyPoint(tri.C.point);

                //在世界空间进行背面消隐, 节省顶点变换到相机空间
                if (BackFaceCulling(worldPosA, worldPosB, worldPosC))
                    return;

                Vector4 clipA = World2Clip(worldPosA);
                Vector4 clipB = World2Clip(worldPosB);
                Vector4 clipC = World2Clip(worldPosC);


                if (SoftRendererUtil.IsOutofClipSpace(clipA, clipB, clipC))
                    return;


                List<Triangle> addTris = SoftRendererUtil.ClipNearPlane(clipA, clipB, clipC, -1);
                for (int i = 0; i < addTris.Count; i++)
                {
                    var addTri = addTris[i];
                    addTri.owner = tri.owner;
                    ProcessAddTriangle(addTri);
                }
                if (addTris.Count > 0)
                    return;

                Vector3 screenNewA = Clip2Screen(clipA);
                Vector3 screenNewB = Clip2Screen(clipB);
                Vector3 screenNewC = Clip2Screen(clipC);

                Vector3 screenPosA = Camera.main.WorldToScreenPoint(worldPosA);
                if ((screenPosA - screenNewA).magnitude > 0.3f)
                {
                    Debug.LogError("screen pos error!");
                    return;
                }

                Vector3 cameraSpacePos = -World2Camera(worldPosA);
                if (cameraSpacePos.z != clipA.w)
                    Debug.LogError("error z!");
                if (cameraSpacePos.z != screenNewA.z)
                    Debug.LogError("error z!");

                Triangle transformedTriangle = new Triangle();
                transformedTriangle.A.point = screenNewA;
                transformedTriangle.B.point = screenNewB;
                transformedTriangle.C.point = screenNewC;

                transformedTriangle.A.onePerZ = 1 / screenNewA.z;
                transformedTriangle.B.onePerZ = 1 / screenNewB.z;
                transformedTriangle.C.onePerZ = 1 / screenNewC.z;

                transformedTriangle.owner = tri.owner;

                TriangleRasterization(transformedTriangle, tri.owner);
            }
            catch (UnityException e)
            {
                Debug.LogError(e);
            }
        }

        public void Draw()
        {
            ClearBuff();
            foreach(var meshTri in meshTriangles)
            {
                var renderer = meshTri.Key;
                if (IsVisible(renderer) == false)
                    continue;

                var tris = meshTri.Value.triangles;

                for (int j = 0; j < tris.Count; j++)
                {
                    var tri = tris[j];
                    ProcessTriangle(tri);
                }
            }           

            for (int i = 0; i < Width; i++)
                for (int j = 0; j < Height; j++)
                {
                    var color = _frameBuff[i, j];
                    if (_zBuff[i, j].zValue > 0)
                    {
                        var z = 1 / _zBuff[i, j].zValue;
                        DrawScreenPoint(new Vector3(i, j, z), color);
                    }
                }
        }

        public HashSet<MeshRenderer> GetVisibleModels(List<MeshRenderer> filterMeshRenderers)
        {
            ClearBuff();

            _camera.aspect = Camera.main.aspect;
            _camera.far = Camera.main.farClipPlane;
            _camera.near = Camera.main.nearClipPlane;
            _camera.fovY = Camera.main.fieldOfView;
            _camera.position = Camera.main.transform.position;
            _camera.rot = Camera.main.transform.rotation;
            _camera.scale = Camera.main.transform.localScale;

            _camera.Init();

            HashSet<MeshRenderer> ret = new HashSet<MeshRenderer>();

            if (filterMeshRenderers.Count == 0)
                return ret;



            List<MeshTriangle> filterMeshTris = new List<MeshTriangle>();

            foreach (var mr in filterMeshRenderers)
            {
                MeshTriangle meshTri;
                if( meshTriangles.TryGetValue(mr, out meshTri))
                {                   
                    filterMeshTris.Add(meshTri);
                }              
            }

            for (int i = 0; i < filterMeshTris.Count; i++)
            {
                //var renderer = filterMeshTris[i].renderer;

                //if (IsVisible(renderer) == false)
                    //continue;

                var tris = filterMeshTris[i].triangles;

                for (int j = 0; j < tris.Count; j++)
                {
                    var tri = tris[j];
                    ProcessTriangle(tri);
                }
            }

#if false
            for (int i = 0; i < meshTriangles.Count; i++)
            {
                var renderer = meshTriangles[i].renderer;

                if (IsVisible(renderer) == false)
                    continue;

                var tris = meshTriangles[i].triangles;

                for (int j = 0; j < tris.Count; j++)
                {
                    var tri = tris[j];
                    ProcessTriangle(tri);
                }
            }
#endif



            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    TagZBuffer tag = _zBuff[i, j];
                    if (tag.renderer != null)
                    {
                        if (ret.Contains(tag.renderer) == false)
                            ret.Add(tag.renderer);
                    }
                }
            }
            return ret;

        }

        private bool BackFaceCulling(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            Vector3 v1 = p2 - p1;
            Vector3 v2 = p3 - p2;
            Vector3 normal = Vector3.Cross(v1, v2);

            //Vector3 viewDir = Camera.main.transform.position - p1;
            Vector3 viewDir = _camera.position - p1;
            if (Vector3.Dot(normal, viewDir) > 0)
            {               
                return false;
            }
            return true;

        }

        public void DrawScreenPoint(Vector3 pos, Color color)
        {
            UnityEngine.Object obj = Resources.Load("Quad");
            if (obj == null)
                Debug.Log("Load null!");
            //UnityEngine.Object obj = GameObject.Find("Quad");
            GameObject go = GameObject.Instantiate(obj) as GameObject;
            go.transform.position = Camera.main.ScreenToWorldPoint(pos);
            go.GetComponent<MeshRenderer>().sharedMaterial.color = color;
        }

        private Vector3 World2Camera(Vector3 pos)
        {
            //return Camera.main.worldToCameraMatrix.MultiplyPoint(pos);
            //return Camera.main.transform.worldToLocalMatrix.MultiplyPoint(pos);
            return _camera.GetViewMatrix().MultiplyPoint(pos);
        }


        private Vector4 World2Clip(Vector3 position)
        {
            //clipPos = gl_Position
            Vector4 pos = new Vector4(position.x, position.y, position.z, 1);
            Vector4 clipPos = _camera.GetProjectMatrix() * _camera.GetViewMatrix() * pos;
            return clipPos;
        }

        //ScreenPosZ = (farPosZ - nearPosZ)* NDCPosZ + (nearPosZ + farPosZ)/2
        //NDCPosZ (-1,1)或(0,1)
        //farPosZ 远平面映射到屏幕上的位置，Unity中farPosZ = farPlane
        //又因为在推导透视矩阵时，可以看到zBuffer = clipPos.w = -CameraSpacePos.z = Camera.main.WorldToScreenPos().z(即在相机空间中点到相机位置的距离 )
        //我们这里存的是1/z, 可以提升精度，因为1/z是很小的数字，一般小于1，如果存z，比如110.435 这样的浮点数会被110的整数部分占据从而降低了精度（浪费了整数的部分）
        private Vector3 Clip2Screen(Vector4 pos)
        {
            Vector3 ret = Vector3.zero;
            //NDC (-1,1)
            Vector3 ndcPos = new Vector3(pos.x / pos.w, pos.y / pos.w, pos.z / pos.w);
            //remap to (0,1)
            float x = ndcPos.x * 0.5f + 0.5f;
            float y = ndcPos.y * 0.5f + 0.5f;
            //float z = ndcPos.z * 0.5f + 0.5f;

            x = x * _camera.pixelRect.width + _camera.pixelRect.x;
            y = y * _camera.pixelRect.height + _camera.pixelRect.y;
            float z = pos.w;//clipSpacePos.w = -cameraSpacePos.z(the same value)


            ret = new Vector3(x, y, z);
            return ret;
        }

        private Vector3 World2ViewportPoint(Vector3 position)
        {
            Vector4 pos = new Vector4(position.x, position.y, position.z, 1);
            Vector4 clipPos = _camera.GetProjectMatrix() * _camera.GetViewMatrix() * pos;
            //convert to NDC and remap to (0,1)
            float x = clipPos.x / clipPos.w * 0.5f + 0.5f;
            float y = clipPos.y / clipPos.w * 0.5f + 0.5f;
            float z = clipPos.w;
            return new Vector3(x, y, z);

        }

        private Vector3 Viewport2Screen(Vector3 pos)
        {
            Vector3 ret = Vector3.zero;
            float x = pos.x * _camera.pixelRect.width + _camera.pixelRect.x;
            float y = pos.y * _camera.pixelRect.height + _camera.pixelRect.y;
            float z = pos.z;
            ret = new Vector3(x, y, z);
            return ret;
        }

        private List<Triangle> GetMeshTriangles(MeshRenderer owner)
        {
            MeshFilter mf = owner.GetComponent<MeshFilter>();
            Mesh mesh = mf.sharedMesh;

            List<Triangle> ret = new List<Triangle>();

            Vector3[] vertices = mesh.vertices;

            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                int[] indices = mesh.GetIndices(i);
                //int[] tris = mesh.GetTriangles(i);

                for (int j = 0; j < indices.Length; j++)
                {
                    int indice = indices[j];
                    Vector3 vertice = vertices[indice];
                    Vertex v = new Vertex(vertice);
                    if (j % 3 == 0)
                    {
                        Triangle tri = new Triangle();
                        tri.A = v;
                        tri.owner = owner;
                        ret.Add(tri);
                    }
                    else if (j % 3 == 1)
                    {
                        var t = ret[ret.Count - 1];
                        t.B = v;
                        ret[ret.Count - 1] = t;
                    }
                    else
                    {
                        var t = ret[ret.Count - 1];
                        t.C = v;
                        ret[ret.Count - 1] = t;
                    }
                }
            }
            return ret;
        }

        bool IsVisible(MeshRenderer renderer)
        {
            bool ret = true;
            Bounds meshBound = renderer.bounds;

            Vector3 cameraSpaceBoundCenter = World2Camera(renderer.bounds.center);

            meshBound.center = cameraSpaceBoundCenter;

            Vector3[] outFarCorners = new Vector3[4];
            Camera.main.CalculateFrustumCorners(new Rect(0, 0, 1, 1), Camera.main.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, outFarCorners);
            Vector3[] outNearCorners = new Vector3[4];
            Camera.main.CalculateFrustumCorners(new Rect(0, 0, 1, 1), Camera.main.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, outNearCorners);

            List<Plane> planes = new List<Plane>();
            Plane nearPlane = new Plane(outNearCorners[2], outNearCorners[1], outNearCorners[0]);
            planes.Add(nearPlane);
            Plane leftPlane = new Plane(outFarCorners[0], outNearCorners[0], outNearCorners[1]);
            planes.Add(leftPlane);
            Plane rightPlane = new Plane(outNearCorners[2], outNearCorners[3], outFarCorners[3]);
            planes.Add(rightPlane);
            Plane topPlane = new Plane(outNearCorners[1], outNearCorners[2], outFarCorners[2]);
            planes.Add(topPlane);
            Plane bottomPlane = new Plane(outNearCorners[3], outNearCorners[0], outFarCorners[0]);
            planes.Add(bottomPlane);

            for (int i = 0; i < 5; ++i)
            {
                Plane plane = planes[i];
                Side side = MathUtil.GetPlaneSide(plane, meshBound);
                if (side == Side.POSITIVE_SIDE)
                {
                    ret = false;
                    break;
                }

            }
            return ret;
        }



        public List<Triangle> GetAllTriangles()
        {
            List<Triangle> ret = new List<Triangle>();

            List<MeshRenderer> visMesh = new List<MeshRenderer>();

            MeshRenderer[] mrs = UnityEngine.Object.FindObjectsOfType<MeshRenderer>();
            for (int i = 0; i < mrs.Length; i++)
            {
                MeshRenderer mr = mrs[i];
                if (mr == null || !mr.gameObject.activeSelf || !mr.enabled || mr.sharedMaterials.Length <= 0) continue;

                MeshFilter mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;

                if (IsVisible(mr))
                {
                    var tris = GetMeshTriangles(mr);
                    ret.AddRange(tris);

                    visMesh.Add(mr);

                }
            }
            return ret;
        }



        private void TriangleRasterization(Triangle tri, MeshRenderer owner)
        {
            Vertex p1 = tri.A; Vertex p2 = tri.B; Vertex p3 = tri.C;
            if (p1.point.y == p2.point.y)
            {
                if (p1.point.y < p3.point.y)
                {//平顶
                    DrawTriangleTop(p1, p2, p3, owner);
                }
                else
                {//平底
                    DrawTriangleBottom(p3, p1, p2, owner);
                }
            }
            else if (p1.point.y == p3.point.y)
            {
                if (p1.point.y < p2.point.y)
                {//平顶
                    DrawTriangleTop(p1, p3, p2, owner);
                }
                else
                {//平底
                    DrawTriangleBottom(p2, p1, p3, owner);
                }
            }
            else if (p2.point.y == p3.point.y)
            {
                if (p2.point.y < p1.point.y)
                {//平顶
                    DrawTriangleTop(p2, p3, p1, owner);
                }
                else
                {//平底
                    DrawTriangleBottom(p1, p2, p3, owner);
                }
            }
            else
            {//分割三角形
                Vertex top;

                Vertex bottom;
                Vertex middle;
                if (p1.point.y > p2.point.y && p2.point.y > p3.point.y)
                {
                    top = p3;
                    middle = p2;
                    bottom = p1;
                }
                else if (p3.point.y > p2.point.y && p2.point.y > p1.point.y)
                {
                    top = p1;
                    middle = p2;
                    bottom = p3;
                }
                else if (p2.point.y > p1.point.y && p1.point.y > p3.point.y)
                {
                    top = p3;
                    middle = p1;
                    bottom = p2;
                }
                else if (p3.point.y > p1.point.y && p1.point.y > p2.point.y)
                {
                    top = p2;
                    middle = p1;
                    bottom = p3;
                }
                else if (p1.point.y > p3.point.y && p3.point.y > p2.point.y)
                {
                    top = p2;
                    middle = p3;
                    bottom = p1;
                }
                else if (p2.point.y > p3.point.y && p3.point.y > p1.point.y)
                {
                    top = p1;
                    middle = p3;
                    bottom = p2;
                }
                else
                {
                    //三点共线
                    return;
                }



                //插值求中间点x
                float middlex = (middle.point.y - top.point.y) * (bottom.point.x - top.point.x) / (bottom.point.y - top.point.y) + top.point.x;
                float dy = middle.point.y - top.point.y;
                float t = dy / (bottom.point.y - top.point.y);
                //插值生成左右顶点
                Vertex newMiddle = new Vertex();
                newMiddle.point.x = middlex;
                newMiddle.point.y = middle.point.y;
                MathUtil.ScreenSpaceLerpVertex(ref newMiddle, top, bottom, t);

                //平底
                DrawTriangleBottom(top, newMiddle, middle, owner);
                //平顶
                DrawTriangleTop(newMiddle, middle, bottom, owner);
            }
        }
        //x = (y-y1) * (x2-x1) / (y2-y1) + x1
        /// <summary>
        /// 平顶，p1,p2,p3为下顶点
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        private void DrawTriangleTop(Vertex p1, Vertex p2, Vertex p3, MeshRenderer owner)
        {
            for (float y = p1.point.y; y <= p3.point.y; y += 0.5f)
            {
                int yIndex = (int)(System.Math.Round(y, MidpointRounding.AwayFromZero));
                if (yIndex >= 0 && yIndex < Height)
                {
                    float xl = (y - p1.point.y) * (p3.point.x - p1.point.x) / (p3.point.y - p1.point.y) + p1.point.x;
                    float xr = (y - p2.point.y) * (p3.point.x - p2.point.x) / (p3.point.y - p2.point.y) + p2.point.x;

                    float dy = y - p1.point.y;
                    float t = dy / (p3.point.y - p1.point.y);
                    //插值生成左右顶点
                    Vertex new1 = new Vertex();
                    new1.point.x = xl;
                    new1.point.y = y;
                    MathUtil.ScreenSpaceLerpVertex(ref new1, p1, p3, t);
                    //
                    Vertex new2 = new Vertex();
                    new2.point.x = xr;
                    new2.point.y = y;
                    MathUtil.ScreenSpaceLerpVertex(ref new2, p2, p3, t);
                    //扫描线填充
                    if (new1.point.x < new2.point.x)
                    {
                        ScanlineFill(new1, new2, yIndex, owner);
                    }
                    else
                    {
                        ScanlineFill(new2, new1, yIndex, owner);
                    }
                }
            }
        }
        /// <summary>
        /// 平底，p1为上顶点,p2，p3
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>

        private void DrawTriangleBottom(Vertex p1, Vertex p2, Vertex p3, MeshRenderer owner)
        {
            for (float y = p1.point.y; y <= p2.point.y; y += 0.5f)
            {
                int yIndex = (int)(System.Math.Round(y, MidpointRounding.AwayFromZero));
                if (yIndex >= 0 && yIndex < Height)
                {
                    float xl = (y - p1.point.y) * (p2.point.x - p1.point.x) / (p2.point.y - p1.point.y) + p1.point.x;
                    float xr = (y - p1.point.y) * (p3.point.x - p1.point.x) / (p3.point.y - p1.point.y) + p1.point.x;

                    float dy = y - p1.point.y;
                    float t = dy / (p2.point.y - p1.point.y);
                    //插值生成左右顶点
                    Vertex new1 = new Vertex();
                    new1.point.x = xl;
                    new1.point.y = y;
                    MathUtil.ScreenSpaceLerpVertex(ref new1, p1, p2, t);
                    //
                    Vertex new2 = new Vertex();
                    new2.point.x = xr;
                    new2.point.y = y;
                    MathUtil.ScreenSpaceLerpVertex(ref new2, p1, p3, t);
                    //扫描线填充
                    if (new1.point.x < new2.point.x)
                    {
                        ScanlineFill(new1, new2, yIndex, owner);
                    }
                    else
                    {
                        ScanlineFill(new2, new1, yIndex, owner);
                    }
                }
            }
        }

        /// <summary>
        /// 扫描线填充
        /// </summary>
        /// <param name="left">左端点，值已经经过插值</param>
        /// <param name="right">右端点，值已经经过插值</param>
        private void ScanlineFill(Vertex left, Vertex right, int yIndex, MeshRenderer owner)
        {
            float dx = right.point.x - left.point.x;
            float step = 1;
            if (dx != 0)
            {
                step = 1 / dx;
            }
            for (float x = left.point.x; x <= right.point.x; x += 0.5f)
            {
                int xIndex = (int)(x + 0.5f);
                if (xIndex >= 0 && xIndex < Width)
                {
                    float lerpFactor = 0;
                    if (dx != 0)
                    {
                        lerpFactor = (x - left.point.x) / dx;
                    }
                    //1/z’与x’和y'是线性关系的
                    float onePreZ = MathUtil.Lerp(left.onePerZ, right.onePerZ, lerpFactor);

                    if (onePreZ > 1 / _camera.near)
                        Debug.Log("z<=near !");

                    if (onePreZ <= 0)
                        Debug.Log("z<=0!");
                    if (onePreZ <= 1 / _camera.near && onePreZ >= _zBuff[xIndex, yIndex].zValue)//使用1/z进行深度测试
                    {
                        //通过测试                      
                        _zBuff[xIndex, yIndex].zValue = onePreZ;
                        _zBuff[xIndex, yIndex].renderer = owner;
                        //_frameBuff.SetPixel(xIndex, yIndex, finalColor.TransFormToSystemColor());

                        _frameBuff[xIndex, yIndex] = new Color(1, 0, 0);
                        //DrawScreenPoint(new Vector3(xIndex, yIndex, 1/onePreZ), Color.red);
                    }
                }
            }

        }
    }
}
#endif

