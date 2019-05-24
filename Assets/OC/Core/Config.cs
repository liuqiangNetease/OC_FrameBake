using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OC
{
    public class Config
    {
        public static bool IsBatchMode = false;

        public static bool UseComputeShader = true;

        public static bool UseVisibleCache = true;

        public static bool ComputePerframe = false;
        public static int PerframeExecCount = 1000;

        public static int ScreenWidth = 640;
        public static int ScreenHeight = 480;

        public static bool mergeObjectID = false;
        public static float mergeObjectDistance = 1.0f;
        public static float mergeObjectMaxSize = 1.0f;
        public static bool mergeCell = false;
        public static float CellWeight = 0.8f;

        public static float CellSize = 2.0f;
        public static float MaxPlayAreaHeight = 2.0f;
        public static float MinPlayAreaHeight = 0.0f;

        public static bool SoftRenderer = false;
  
        public static readonly string OCPatchFileSuffix = "_oc_patch.txt";

        public static bool ChangeLODBias = false;

        public static bool ChangeCameraFOV = false;

        public static bool Use6DirLook = true;

        public static bool CameraRect = true;
    }
}

