#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OC
{
    public struct VertexOrderInClip
    {
        public Vector4 pos;
        public bool IsInClip;
    }
    public class SoftRendererUtil
    {
        public static bool IsOutofClipSpace(Vector4 clipA, Vector4 clipB, Vector4 clipC)
        {
            bool ret = false;
            bool bOutOfLeft = (clipA.x < -clipA.w) && (clipB.x < -clipB.w) && (clipC.x < -clipC.w);
            bool bOutOfRight = (clipA.x > clipA.w) && (clipB.x > clipB.w) && (clipC.x > clipC.w);
            bool bOutOfTop = (clipA.y > clipA.w) && (clipB.y > clipB.w) && (clipC.y > clipC.w);
            bool bOutOfBottom = (clipA.y < -clipA.w) && (clipB.y < -clipB.w) && (clipC.y < -clipC.w);
            bool bOutOfFar = (clipA.z > clipA.w) && (clipB.z > clipB.w) && (clipC.z > clipC.w);
            bool bOutOfNear = (clipA.z < -clipA.w) && (clipB.z < -clipB.w) && (clipC.z < -clipC.w);
            //bool bOutOfNear = (clipA.z < 0) && (clipB.z < 0) && (clipC.z < 0);
            ret = bOutOfLeft || bOutOfRight || bOutOfTop || bOutOfBottom || bOutOfNear || bOutOfFar;
            return ret;
        }

        public static List<Triangle> ClipNearPlane(Vector4 clipA, Vector4 clipB, Vector4 clipC, float near)
        {
            List<Triangle> ret = CutTriWithNearPlane(clipA, clipB, clipC, near);
            return ret;
        }

        public static List<Triangle> CutTriWithNearPlane(Vector4 clipA, Vector4 clipB, Vector4 clipC, float near)
        {
            List<Triangle> ret = new List<Triangle>();
            //flag = true, which is out of clip space near plane
            bool flagA = clipA.z < -clipA.w;
            bool flagB = clipB.z < -clipB.w;
            bool flagC = clipC.z < -clipC.w;
            bool bOnSameSide = (!flagA && !flagB && !flagC) || (flagA && flagB && flagC);
            if (bOnSameSide)
            {             
                return ret;
            }          

            VertexOrderInClip[] vList = new VertexOrderInClip[3];
            int sumBool = (flagA?1:0) + (flagB ? 1 : 0) + (flagC ? 1 : 0);        
            vList[0].pos = clipA;
            vList[1].pos = clipB;
            vList[2].pos = clipC;

            if (sumBool == 2)
            {
                //make it become ( false, true, true )
                if (flagB == false)// ( true , false , true )
                {
                    vList[0].pos = clipB;
                    vList[1].pos = clipC;
                    vList[2].pos = clipA;
                }
                if(flagC == false)// ( true , true , false )
                {
                    vList[0].pos = clipC;
                    vList[1].pos = clipA;
                    vList[2].pos = clipB;
                }
                vList[0].IsInClip = false; vList[1].IsInClip = true; vList[2].IsInClip = true;
            }
            else if(sumBool == 1)
            {
                //make it become ( true , false , false )
                
                if (flagB)// ( false , true , false )
                {
                    vList[0].pos = clipB;
                    vList[1].pos = clipC;
                    vList[2].pos = clipA;
                }
                if(flagC)//( false , false , true )
                {
                    vList[0].pos = clipC;
                    vList[1].pos = clipA;
                    vList[2].pos = clipB;
                }
                vList[0].IsInClip = true; vList[1].IsInClip = false; vList[2].IsInClip = false;
            }
            else
            {
                //not possible
            }
            //now vertList is ( >=Near , <Near , <Near ) or ( <Near , >=Near , >=Near )
            //公式推导：线段AB与近平面的交点
            //Edge(t) = ( Ax + (Bx-Ax)t,Ay + (By-Ay)t ,Az + (Bz-Az)t, ,Aw + (Bw-Aw)t)
            //所以  [Az + (Bz - Az) t] / [Aw + (Bw-Aw)t)] = near = -1
            //所以 推导出 t = (Az - Aw) / [(Bz - Az) + (Bw - Aw)]
            float tAB = (near * vList[0].pos.w - vList[0].pos.z) / ((vList[1].pos.z - vList[0].pos.z) - near * (vList[1].pos.w - vList[0].pos.w));
            Vector4 intersectPosOnLineAB = vList[0].pos + (vList[1].pos - vList[0].pos) * tAB;
         
            float tAC = (near * vList[0].pos.w - vList[0].pos.z) / ((vList[2].pos.z - vList[0].pos.z) - near * (vList[2].pos.w - vList[0].pos.w));
            Vector4 intersectPosOnLineAC = vList[0].pos + (vList[2].pos - vList[0].pos) * tAC;  
            //Vector4 intersectVertOnLineAB = interpolateInLine_inClippingSpace(vList[0].pos, vList[1].pos, intersectPosOnLineAB.x, intersectPosOnLineAB.y);
            //Vector4 intersectVertOnLineAC = interpolateInLine_inClippingSpace(vList[0].pos, vList[2].pos, intersectPosOnLineAC.x, intersectPosOnLineAC.y);
            Vector4 intersectVertOnLineAB = intersectPosOnLineAB;
            Vector4 intersectVertOnLineAC = intersectPosOnLineAC;
            if (vList[0].IsInClip == false)
            {//vList is ( >=Near , <Near , <Near)
                
                Triangle tri = new Triangle();
                tri.A.point = vList[0].pos;
                tri.B.point = intersectVertOnLineAB;
                tri.C.point = intersectVertOnLineAC;
                //tri.B.point = intersectPosOnLineAB;
                //tri.C.point = intersectPosOnLineAC;
                ret.Add(tri);                
            }
            else
            {//vList is ( <Near , >=Near , >=Near )
                Triangle tri1 = new Triangle();
                tri1.A.point = intersectVertOnLineAB;
                //tri1.A.point = intersectPosOnLineAB;
                tri1.B.point = vList[1].pos;
                tri1.C.point = vList[2].pos;
                Triangle tri2 = new Triangle();
                tri2.A.point = vList[2].pos;
                tri2.B.point = intersectVertOnLineAC;
                tri2.C.point = intersectVertOnLineAB;
                //tri2.B.point = intersectPosOnLineAC;//intersectVertOnLineAC;
                //tri2.C.point = intersectPosOnLineAB;//intersectVertOnLineAB;
                ret.Add(tri1);
                ret.Add(tri2);                
            }
            return ret;
        }

        public static Vector4 interpolateInLine_inClippingSpace(Vector4 v1, Vector4 v2, float x, float y)
        {
            float x1 = v1.x;
            float y1 = v1.y;
            float z1 = v1.z;
            float w1 = v1.w;
            // float s1 = v1.m_texCoord.x();
            // float t1 = v1.m_texCoord.y();
            float x2 = v2.x;
            float y2 = v2.y;
            float z2 = v2.z;
            float w2 = v2.w;
            //float s2 = v2.m_texCoord.x();
            //float t2 = v2.m_texCoord.y();
            //----calculate interpolate factor k
            float dx = v1.x - v2.x;
            float dy = v1.y - v2.y;
            float k;
            if (Mathf.Abs(dx) > Mathf.Abs(dy))
            {
                //calculate k using xHighSubxLow
                k = (x - x2) / dx;
            }
            else
            {
                //calculate k using yHighSubyLow
                k = (y - y2) / dy;
            }
            //----calculate z
            float z = k * (z1 - z2) + z2;
            //----calculate color
            //Cc3dVector4 color = (v1.m_color - v2.m_color) * k + v2.m_color;
            //----calculate colorAdd
            //Cc3dVector4 colorAdd = (v1.m_colorAdd - v2.m_colorAdd) * k + v2.m_colorAdd;
            //----calculate s,t
            //in clipping space s,t is linear with (x,y)
            //float s = (s1 - s2) * k + s2;
            //float t = (t1 - t2) * k + t2;
            //----calculate w
            float w = k * (w1 - w2) + w2;
            //----set to v
            Vector4 v = v1;//copy v1
            v = new Vector4(x, y, z, w);
            //v.m_color=color;
            //v.m_texCoord=Cc3dVector2(s, t);
            //v.m_colorAdd=colorAdd;
            return v;
        }

        public static Vertex interpolateInLine_inClippingSpace(Vertex v1, Vertex v2,float x,float y)
        {
             float x1 = v1.point.x;
             float y1 = v1.point.y;
             float z1 = v1.point.z;
             float w1 = v1.point.w;
            // float s1 = v1.m_texCoord.x();
            // float t1 = v1.m_texCoord.y();
             float x2 = v2.point.x;
             float y2 = v2.point.y;
             float z2 = v2.point.z;
             float w2 = v2.point.w;
             //float s2 = v2.m_texCoord.x();
             //float t2 = v2.m_texCoord.y();
        //----calculate interpolate factor k
             float dx = v1.point.x - v2.point.x;
             float dy = v1.point.y - v2.point.y;
             float k;
	        if(Mathf.Abs(dx)>Mathf.Abs(dy))
            {
		        //calculate k using xHighSubxLow
		        k=(x-x2)/dx;
	        }
            else
            {
		        //calculate k using yHighSubyLow
		        k=(y-y2)/dy;
	        }
            //----calculate z
            float z = k * (z1 - z2) + z2;
            //----calculate color
            //Cc3dVector4 color = (v1.m_color - v2.m_color) * k + v2.m_color;
            //----calculate colorAdd
            //Cc3dVector4 colorAdd = (v1.m_colorAdd - v2.m_colorAdd) * k + v2.m_colorAdd;
            //----calculate s,t
            //in clipping space s,t is linear with (x,y)
            //float s = (s1 - s2) * k + s2;
            //float t = (t1 - t2) * k + t2;
            //----calculate w
            float w = k * (w1 - w2) + w2;
            //----set to v
            Vertex v = v1;//copy v1
            v.point= new Vector4(x, y, z, w);
            //v.m_color=color;
	        //v.m_texCoord=Cc3dVector2(s, t);
            //v.m_colorAdd=colorAdd;
	         return v;
        }
    }
}
#endif

