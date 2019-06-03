using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.SceneManagement;
using OC;

namespace OC.Editor
{
    public class OCBakeWindow : EditorWindow 
    {
        //MultiScene streamScene;

        //SingleScene singleScene;

        string sceneName = "002";
        string assetPath;
        int processNumber = 4;
        int screenWidth = 600;
        int screenHeight = 600;

        float maxPlayAreaHeight = 2;
        float minPlayAreaHeight = 0;
        
        float cellSize = 2f;
        bool IsFrameBake = true;
        int frameCellCount = 1000;

        bool IsStreamScene = true;
        bool IsBakeOne= false;
        
        bool IsMergeCell = true;
        float cellMergeWeight = 0.9f;

        bool IsMergeObjectID = false;
        float mergeDistance = 1;
        float mergeObjectSize = 1;
        
        List<Index> indices = new List<Index>();

        int tileX;
        int tileY;

        int jsonI;

        bool bTest = true;

        void ReadJsonConfig(string sceneName)
        {
            //open scene to get 
            var config = OCGenerator.GetSceneConfig(sceneName);
            cellSize = config.CellSize;
            IsFrameBake = config.ComputePerframe;

            indices = config.indices;

            IsStreamScene = config.IsStreamScene;
            maxPlayAreaHeight = config.MaxPlayerHeight;
            minPlayAreaHeight = config.MinPlayerHeight;
            IsMergeCell = config.MergeCell;
            cellMergeWeight = config.MergeCellWeight;
            mergeDistance = config.MergeObjectDistance;
            mergeObjectSize = config.MergeObjectSize;
            IsMergeObjectID = config.MergeObjectID;
            frameCellCount = config.PerframeExecCount;
            assetPath = config.SceneAssetPath;
        }

        [MenuItem("OC/OCBakeWindow")]
        private static void ShowWindow() {
            var window = GetWindow<OCBakeWindow>();
            window.titleContent = new GUIContent("OCBakeWindow");
            window.Show();
        }
    
        private void OnGUI() 
        {
            GUILayout.BeginHorizontal("Scene");
            sceneName = EditorGUILayout.TextField("场景名字", sceneName);
            assetPath = EditorGUILayout.TextField("场景路径", assetPath);
            if(GUILayout.Button("打开场景"))
            {
                OCGenerator.OpenScene(sceneName, new Index(tileX, tileY));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal("base setting");
            processNumber = EditorGUILayout.IntField("进程数", processNumber);
            cellSize = EditorGUILayout.FloatField("Cell大小", cellSize);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal("Screen");
            screenWidth = EditorGUILayout.IntField("屏幕宽", screenWidth);
            screenHeight = EditorGUILayout.IntField("屏幕高",screenHeight);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal("Player");
            maxPlayAreaHeight = EditorGUILayout.Slider("玩家最大可到达高度", maxPlayAreaHeight,0, 10);
            minPlayAreaHeight = EditorGUILayout.Slider("玩家最小不可到达高度", minPlayAreaHeight,0, 10);
            GUILayout.EndHorizontal();

            IsFrameBake = EditorGUILayout.BeginToggleGroup("分帧烘焙", IsFrameBake);
            frameCellCount = EditorGUILayout.IntField("每帧烘焙cell的个数", frameCellCount);
            EditorGUILayout.EndToggleGroup();

            IsMergeCell = EditorGUILayout.BeginToggleGroup("是否融合Cell", IsMergeCell);
            cellMergeWeight = EditorGUILayout.Slider("融合相似度", cellMergeWeight, 0, 1);
            EditorGUILayout.EndToggleGroup();

            IsMergeObjectID = EditorGUILayout.BeginToggleGroup("是否融合GUID", IsMergeObjectID);
            GUILayout.BeginHorizontal("Merge Object");
            mergeObjectSize = EditorGUILayout.FloatField("对象大小", mergeObjectSize);
            mergeDistance = EditorGUILayout.FloatField("对象距离", mergeDistance);
            GUILayout.EndHorizontal();
            EditorGUILayout.EndToggleGroup();


            //----------------
            IsStreamScene = EditorGUILayout.BeginToggleGroup("是否是流式场景", IsStreamScene);
          
            IsBakeOne = EditorGUILayout.BeginToggleGroup("只烘焙一小块地形（流式）", IsBakeOne);
            GUILayout.BeginHorizontal("tile index");
            
            tileX = EditorGUILayout.IntField("宽度索引（流式）", tileX);
            
            tileY = EditorGUILayout.IntField("高度索引（流式）", tileY);
            if (GUILayout.Button("烘焙单块（流式）"))
            {
                if (IsBakeOne)
                {
                    OCGenerator.TestBakeOneTile(sceneName, tileX, tileY);
                }
            }
            GUILayout.EndHorizontal();
            EditorGUILayout.EndToggleGroup();//end IsBakeOne
            EditorGUILayout.EndToggleGroup();//end IsStreamScene

            
            /*for (int i = 0; i < indices.Count; i++)
            {
                var index = indices[i];
                Vector2 tile = new Vector2(index.x, index.y);
                EditorGUILayout.Vector2Field("bake tile", tile);
               
            }*/


            bTest = EditorGUILayout.BeginToggleGroup("测试", bTest);

            if (GUILayout.Button("创建测试所用的场景"))
            {
                OCGenerator.GenerateTestStreamScenes(sceneName);
            }

            GUILayout.BeginHorizontal("Json");

            if (GUILayout.Button("创建Json文件"))
            {
               
                OCGenerator.TestCreateScensJson();
            }

            if (GUILayout.Button("写Json配置"))
            {
                //OCGenerator.OpenScenes(sceneName);
         
                OCGenerator.WriteJsonConfig(sceneName, IsStreamScene, screenWidth, screenHeight, maxPlayAreaHeight, minPlayAreaHeight, cellSize,
                    IsFrameBake, frameCellCount, IsMergeCell, cellMergeWeight, IsMergeObjectID, mergeDistance, mergeObjectSize, indices );
            }
            if (GUILayout.Button("读Json配置"))
            {
                //OCGenerator.OpenScenes(sceneName);
                ReadJsonConfig(sceneName);
            }

            GUILayout.EndHorizontal();
            


            GUILayout.BeginHorizontal("test");
            if (GUILayout.Button("根据进程数生成配置文件"))
            {
                OCGenerator.TestGenerateOCGenMapConfigFile(sceneName, processNumber);
            }

            if (GUILayout.Button("TestInitConfig(open scenes and generate ID)"))
            {
                OCGenerator.TestInitOCGeneration(sceneName);
            }

            if (GUILayout.Button("程序化自动测试数据"))
            {
                OCSceneConfig config = OCGenerator.GetSceneConfig(sceneName);
                PVSTest test = new PVSTest(Camera.main, config);
                test.Test();
            }
            

            /*if (GUILayout.Button("打开场景并加载OC数据"))
            {
                OCGenerator.OpenScenes(sceneName);
                OCSceneConfig config = OCGenerator.GetSceneConfig(sceneName);
                if (config.IsStreamScene)
                {
                    var ocDataFilePath = MultiScene.GetOCDataFilePath(config.GetSceneAssetPath(), config.SceneNamePattern);
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

                    streamScene = new MultiScene(config.GetSceneAssetPath(), config.SceneNamePattern, TileDimension, config.TileSize, data);
                    //for (int i = 0; i < TileDimension; i++)
                        //for (int j = 0; j < TileDimension; j++)
                            //streamScene.Load(i, j);
                    foreach(var index in config.indices )
                    {
                        streamScene.Load(index.x, index.y);
                    }

                }
                else
                {
                    
                    singleScene = new OC.SingleScene(config.GetSceneAssetPath(), config.SceneNamePattern, Index.InValidIndex);
                    singleScene.TestLoad();
                }

            }*/
            GUILayout.EndHorizontal();

            /*GUILayout.BeginHorizontal("OC");
            if (GUILayout.Button("打开OC"))
            {
                OCSceneConfig config = OCGenerator.GetSceneConfig(sceneName);
                if (config.IsStreamScene)
                {
                    if (streamScene == null)
                        Debug.LogError("stream scene is null!");
                    else
                    {                        
                        streamScene.DoCulling(Camera.main.transform.position);
                    }
                }
                else
                {
                    if (singleScene == null)
                        Debug.LogError("stream scene is null!");
                    else
                    {                       
                        singleScene.DoCulling(Camera.main.transform.position);
                    }
                }

            }
            if (GUILayout.Button("关闭OC"))
            {
                OCSceneConfig config = OCGenerator.GetSceneConfig(sceneName);
                if (config.IsStreamScene)
                {
                    if (streamScene == null)
                        Debug.LogError("stream scene is null!");
                    else
                    {
                        streamScene.UndoCulling();
                    }
                }
                else
                {
                    if (singleScene == null)
                        Debug.LogError("stream scene is null!");
                    else
                    {
                        singleScene.UndoCulling();
                    }
                }
            }

            GUILayout.EndHorizontal();*/
            if (GUILayout.Button("TestApplyOCData"))
            {
                OCGenerator.TestApplyOCData(sceneName);
            }


            EditorGUILayout.EndToggleGroup();



            GUILayout.Label("烘焙", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal("bake");

            if(GUILayout.Button("产生所有场景ID并保存"))
            {
                OCGenerator.OpenAllScenesAndGernerateGUID(sceneName);
            }
            
            jsonI = EditorGUILayout.IntField("第几个Json配置文件", jsonI);
            if (GUILayout.Button("烘焙配置文件"))
            {
                //EditorSceneManager.SaveOpenScenes();

                //OCGenerator.WriteJsonConfig(sceneName, IsStreamScene, screenWidth, screenHeight, maxPlayAreaHeight, minPlayAreaHeight, cellSize,
                    //IsFrameBake, frameCellCount, IsMergeCell, cellMergeWeight, IsMergeObjectID, mergeDistance, mergeObjectSize, indices);

                OCGenerator.TestGenerateOCData(jsonI);
            }

            GUILayout.EndHorizontal();

            if (GUILayout.Button("烘焙所有"))
            {                
                //OCGenerator.WriteJsonConfig(sceneName, IsStreamScene, screenWidth, screenHeight, maxPlayAreaHeight, minPlayAreaHeight, cellSize,
                    //IsFrameBake, frameCellCount, IsMergeCell, cellMergeWeight, IsMergeObjectID, mergeDistance, mergeObjectSize, indices);
                //EditorSceneManager.SaveOpenScenes();
                OCGenerator.TestBakeAll(sceneName);
                //OCGenerator.TestMergeOCDataForStreamScene();
            }

            if(GUILayout.Button("融合OC数据"))
            {
                OCGenerator.TestMergeOCDataForStreamScene();
            }
        }
    }
}
