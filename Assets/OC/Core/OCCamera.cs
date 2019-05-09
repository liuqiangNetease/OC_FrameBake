#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OC
{
    public class OCCamera
    {
        public Rect pixelRect;
        public float fovY;
        public float near;
        public float far;
        public float aspect;

        public Vector3 position;
        public Quaternion rot;
        public Vector3 scale;

        public Vector3 lookAt;
        public Vector3 up;
        public Vector3 right;

        public Matrix4x4 projMat;
        public Matrix4x4 viewMat;

        public void ComputerProjectMatrix()
        {
            //var proj1 = Camera.main.projectionMatrix;
            var proj2 = Matrix4x4.Perspective(fovY, aspect, near, far);            
            projMat = proj2;
        }

        public void ComputerViewMatrix()
        {
            var forward = Camera.main.transform.forward;
            var right = Camera.main.transform.right;
            var up = Camera.main.transform.up;
            var pos = Camera.main.transform.position;

            Matrix4x4 mat = Matrix4x4.identity;
            mat.SetColumn(0,new Vector4(right.x, right.y, right.z, 0));
            mat.SetColumn(1,new Vector4(up.x, up.y, up.z, 0));
            mat.SetColumn(2, new Vector4(-forward.x, -forward.y, -forward.z, 0));
            mat.SetColumn(3, new Vector4(pos.x, pos.y, pos.z, 1));

            viewMat = Matrix4x4.Inverse(mat);
            
            if(viewMat != Camera.main.worldToCameraMatrix)
            {
                Debug.LogError("view matrix is not the same with unity!");
            }
        }
        public void Init()
        {
            ComputerProjectMatrix();

            ComputerViewMatrix();
        }
        
        public Matrix4x4 GetProjectMatrix()
        {
            return projMat;
        }
        public Matrix4x4 GetViewMatrix()
        {
            return viewMat;
        }
    }
}
#endif


