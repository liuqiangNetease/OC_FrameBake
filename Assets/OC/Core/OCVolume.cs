
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OC.Raster;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OC.Editor
{
    [RequireComponent(typeof(BoxCollider))]
    public class OCVolume : MonoBehaviour
    {
        //public bool SimpleGenerateCell = false;
        public float CellSize = 2.0f;
        public BoxCollider Box
        {
            get
            {   
                return GetComponent<BoxCollider>(); 
            }
        }
        
        void Start()
        {
            Box.enabled = false;
        }
       
    }
}
#endif