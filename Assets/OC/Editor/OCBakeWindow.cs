using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace OC.Editor
{
    public class OCBakeWindow : EditorWindow 
    {

        string sceneName;
        int processNumber = 8;
        //int screenWidth = 600;
        //int screenHeight = 600;

        //float maxPlayAreaHeight = 2;
        //float minPlayAreaHeight = 0;
        
        //float cellSize = 2f;
        //bool IsFrameBake = true;
        //int frameCellCount = 1000;

        //bool IsStreamScene = true;
        //bool IsBakeOne= true;
        
        //bool IsMergeCell = true;
        //float cellMergeWeight = 0.8f;

        //bool IsMergeObjectID = true;
        //float mergeDistance = 1;
        //float mergeObjectSize = 1;

        //bool IsSimpleGenerateCell = false;

        //bool IsCustomVolume = false;
        //Vector3 volumeCenter = Vector3.zero;
        //Vector3 volumeSize = Vector3.one;



        [MenuItem("OC/OCBakeWindow")]
        private static void ShowWindow() {
            var window = GetWindow<OCBakeWindow>();
            window.titleContent = new GUIContent("OCBakeWindow");
            window.Show();
        }

        public void InitConfig()
        {
            //Config.CellSize = cellSize;
            //Config.IsBatchMode = false;
            //Config.UseComputeShader = true;
            //Config.mergeCell = IsMergeCell;
            //Config.CellWeight = cellMergeWeight;
            //Config.mergeObjectID = IsMergeObjectID;
            //Config.mergeObjectDistance = mergeDistance;
            //Config.mergeObjectMaxSize = mergeObjectSize;
            //Config.ScreenWidth = screenWidth;
            //Config.ScreenHeight = screenHeight;
            //Config.SimpleGenerateCell = IsSimpleGenerateCell;
            // Config.SoftRenderer = IsSoftRenderer;
            //Config.UseComputeShader = IsUseComputeShader;
            //Config.UseVisibleCache = IsUseVisibleCache;
            //Config.ComputePerframe = IsFrameBake;
            //Config.ComputeShader = ComputeShader;
            //Config.CustomVolume = IsCustomVolume;
            //Config.CustomVolumeCenter= volumeCenter;
            //Config.CustomVolumeSize = volumeSize;
            // Config.IgnoreFailureOnTileInit ;
            //Config.MaxPlayAreaHeight = maxPlayAreaHeight;
            //Config.MinPlayAreaHeight = minPlayAreaHeight;
            // Config.ClearLightProbes = ;
            //Config.PerframeExecCount = frameCellCount;
            
        }
        private void OnGUI() 
        {
             //GUILayout.Label("scene name", EditorStyles.boldLabel);
            sceneName = EditorGUILayout.TextField("scene name", sceneName);
            processNumber = EditorGUILayout.IntField("process number", processNumber);
            //cellSize = EditorGUILayout.Slider("cellSize", cellSize, 0.1f, 256);
            //screenWidth = EditorGUILayout.IntField("Screen Width", screenWidth);
            //screenHeight = EditorGUILayout.IntField("Screen Height",screenHeight);
            //maxPlayAreaHeight = EditorGUILayout.Slider("Max play area height", maxPlayAreaHeight,0, 10);
            //minPlayAreaHeight = EditorGUILayout.Slider("Min play area height", minPlayAreaHeight,0, 10);
            

            //IsFrameBake = EditorGUILayout.BeginToggleGroup("bake frame by frame", IsFrameBake);
            //frameCellCount = EditorGUILayout.IntField("frame cell count", frameCellCount);
            //EditorGUILayout.EndToggleGroup();


            //IsStreamScene = EditorGUILayout.BeginToggleGroup("Stream Scene", IsStreamScene);
            //IsBakeOne = EditorGUILayout.BeginToggleGroup("bake one tile", IsBakeOne);
            //int tileX = 0;
            //tileX = EditorGUILayout.IntField("tile x", tileX);
            //int tileY = 0;
            //tileY = EditorGUILayout.IntField("tile y", tileY);
            //EditorGUILayout.EndToggleGroup();
            //EditorGUILayout.EndToggleGroup();

            //IsMergeCell = EditorGUILayout.BeginToggleGroup("merge cell", IsMergeCell);
            //cellMergeWeight = EditorGUILayout.Slider("weight", cellMergeWeight, 0, 1);
            //EditorGUILayout.EndToggleGroup();

            //IsMergeObjectID = EditorGUILayout.BeginToggleGroup("merge object id", IsMergeObjectID);
            //mergeObjectSize = EditorGUILayout.FloatField("object size", mergeObjectSize);
            //mergeDistance = EditorGUILayout.FloatField("distance between objects", mergeDistance);
            //EditorGUILayout.EndToggleGroup();

            //IsCustomVolume = EditorGUILayout.BeginToggleGroup("Custom volume", IsCustomVolume);
            //volumeCenter = EditorGUILayout.Vector3Field("volume center", volumeCenter);
            //volumeSize = EditorGUILayout.Vector3Field("volume size", volumeSize);
            //EditorGUILayout.EndToggleGroup();

            if (GUILayout.Button("tempCreateScenesJson"))
            {
                OCGenerator.TestCreateScensJson();
            }

            if (GUILayout.Button("TestGenerateOCGenMapConfigFile"))
            {
                OCGenerator.TestGenerateOCGenMapConfigFile(sceneName, processNumber);
            }

            if (GUILayout.Button("TestInitConfig(open scenes and generate ID)"))
            {
                OCGenerator.TestInitOCGeneration(sceneName,7,7);
            }

            if (GUILayout.Button("TestBake"))
            {
                OCGenerator.TestGenerateOCData(63);
            }

            if (GUILayout.Button("BakeAll"))
            {
                OCGenerator.TestBakeAll();
            }


        }
    }
}
