using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OC.Core
{
    internal struct OCVolume
    {
        public Vector3 Center;
        public Vector3 Size;
        public Transform Transform;

        public OCVolume(Vector3 center, Vector3 size, Transform transform)
        {
            Center = center;
            Size = size;
            Transform = transform;
        }

        public static readonly OCVolume Empty = new OCVolume(Vector3.zero, Vector3.zero, null);
    }

    public class OCVolumeScript : MonoBehaviour
    {
        internal OCVolume GetVolume()
        {
            var boxCollider = GetComponent<BoxCollider>();

            if (boxCollider == null)
            {
                Debug.LogErrorFormat("Can not found Box Collider!");
                return OCVolume.Empty;
            }

            return new OCVolume(boxCollider.center, boxCollider.size, boxCollider.transform);
        }
    }
}
