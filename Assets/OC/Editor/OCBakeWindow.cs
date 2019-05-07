using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace OC.Editor
{
    public class OCBakeWindow : EditorWindow 
    {
        MultiScene streamScene;

        SingleScene singleScene;

        string sceneName = "002";
        int processNumber = 4;
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

            if (GUILayout.Button("test create stream scene"))
            {              
                OCGenerator.GenerateTestStreamScenes("Assets/Maps/maps/0001/Scenes/");
            }

            if (GUILayout.Button("TestCreateAllScenesJson"))
            {
                OCGenerator.TestCreateScensJson();               
            }

            if (GUILayout.Button("TestGenerateOCGenMapConfigFile"))
            {
                OCGenerator.TestGenerateOCGenMapConfigFile(sceneName, processNumber);
            }

            if (GUILayout.Button("TestInitConfig(open scenes and generate ID)"))
            {
                OCGenerator.TestInitOCGeneration(sceneName,0,0);
            }

            if (GUILayout.Button("TestBake"))
            {
                OCGenerator.TestGenerateOCData(0);
            }

            if (GUILayout.Button("BakeAll"))
            {
                OCGenerator.TestBakeAll(sceneName);
            }

            if(GUILayout.Button("MergeOCData"))
            {
                OCGenerator.TestMergeOCDataForStreamScene();
            }

            if (GUILayout.Button("TestPVS"))
            {
                OCSceneConfig config = OCGenerator.GetMapConfig(sceneName);
                PVSTest test = new PVSTest(Camera.main, config);
                test.Test(-1);
            }

            if (GUILayout.Button("TestLoadOCData"))
            {

                OCSceneConfig config = OCGenerator.GetMapConfig(sceneName);
                if (config.IsStreamScene)
                {
                    var ocDataFilePath = MultiScene.GetOCDataFilePath(config.SceneAssetPath, config.SceneNamePattern);
                    if (!File.Exists(ocDataFilePath))
                    {
                        EditorUtility.DisplayDialog("文件不存在", string.Format("OC 数据文件 {0} 不存在!", ocDataFilePath), "确定");
                        return;
                    }
                    int TileDimension = config.TileDimension;
                    byte[] data = null;
                    using (var fileStream = File.Open(ocDataFilePath, FileMode.Open))
                    {
                        data = new byte[fileStream.Length];
                        if (fileStream.Read(data, 0, data.Length) != data.Length)
                        {
                            EditorUtility.DisplayDialog("文件读取失败", string.Format("读取 OC 数据文件 {0} 失败!", ocDataFilePath), "确定");
                            return;
                        }
                    }

                    streamScene = new MultiScene( config.SceneAssetPath, config.SceneNamePattern, TileDimension, config.TileSize, data);
                    for(int i=0; i< 2; i++)
                        for(int j=0; j< 2; j++)
                            streamScene.Load(i, j);
                  
                }
                else
                {
                    singleScene = new OC.SingleScene(config.SceneAssetPath, config.SceneNamePattern, null);
                    singleScene.TestLoad();
                }
            }

            if (GUILayout.Button("LoadAllOCData"))
            {
                OCSceneConfig config = OCGenerator.GetMapConfig(sceneName);
                if (config.IsStreamScene)
                {
                    var ocDataFilePath = MultiScene.GetOCDataFilePath(config.SceneAssetPath, config.SceneNamePattern);
                    if (!File.Exists(ocDataFilePath))
                    {
                        EditorUtility.DisplayDialog("文件不存在", string.Format("OC 数据文件 {0} 不存在!", ocDataFilePath), "确定");
                        return;
                    }
                    int TileDimension = config.TileDimension;
                    byte[] data = null;
                    using (var fileStream = File.Open(ocDataFilePath, FileMode.Open))
                    {
                        data = new byte[fileStream.Length];
                        if (fileStream.Read(data, 0, data.Length) != data.Length)
                        {
                            EditorUtility.DisplayDialog("文件读取失败", string.Format("读取 OC 数据文件 {0} 失败!", ocDataFilePath), "确定");
                            return;
                        }
                    }

                    streamScene = new MultiScene(config.SceneAssetPath, config.SceneNamePattern, TileDimension, config.TileSize, data);
                    for (int i = 0; i < 8; i++)
                        for (int j = 0; j < 8; j++)
                            streamScene.Load(i, j);

                }
                else
                {
                    singleScene = new OC.SingleScene(config.SceneAssetPath, config.SceneNamePattern, null);
                    singleScene.TestLoad();
                }

            }

            if (GUILayout.Button("EnableOC"))
            {
                OCSceneConfig config = OCGenerator.GetMapConfig(sceneName);
                if(config.IsStreamScene)
                {
                    if (streamScene == null)
                        Debug.LogError("stream scene is null!");
                    else
                    {
                        streamScene.UndoDisabledObjects();
                        streamScene.DoCulling(Camera.main.transform.position);
                    }
                }
                else
                {
                    if (singleScene == null)
                        Debug.LogError("stream scene is null!");
                    else
                    {
                        singleScene.UndoDisabledObjects();
                        singleScene.DoCulling(Camera.main.transform.position);
                    }
                }
                
            }
            if (GUILayout.Button("DisableOC"))
            {
                OCSceneConfig config = OCGenerator.GetMapConfig(sceneName);
                if (config.IsStreamScene)
                {
                    if (streamScene == null)
                        Debug.LogError("stream scene is null!");
                    else
                    {
                        streamScene.UndoDisabledObjects();                       
                    }
                }
                else
                {
                    if (singleScene == null)
                        Debug.LogError("stream scene is null!");
                    else
                    {
                        singleScene.UndoDisabledObjects();                        
                    }
                }
            }
        }
    }
}
