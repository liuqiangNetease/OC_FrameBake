
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
//using ArtPlugins;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Assertions.Comparers;
using UnityEngine.SceneManagement;

namespace OC.Editor
{
    public partial class OCGenerator
    {
        //工作路径 D:/trunk/develop
        //总体流程参考 build_assetbundle_multi_slaves_ocgenerate.jenkinsfile
        //0 GetNode 复制OC相关的配置文件和bat脚本到相应目录 D:/trunk/oc_deploy   copy to  D:/trunk/develop

        //1 occonfiggen.bat 根据OCGenMapConfig.xml配置文件为每个烘焙进程生成一个对应的OC烘焙配置文件
        public static void TestGenerateOCGenMapConfigFile(string sceneName, int processNum)
        {
            GenerateOCGenMapConfigFile(sceneName, false, processNum);
            var config = LoadSceneConfig("./Assets", 0);
            Debug.LogFormat("Config Is {0}", config);
        }
        //2 ocprojectgen.bat 为每个烘焙进程生成一个项目，其中大部分资源目录都是软链接到原始项目目录的
        //3 coinitgen.bat 删除需要烘焙的场景文件的全局光照信息，为场景文件中可渲染game object生成render id
        public static void TestInitOCGeneration(string sceneName)
        {
            //InitOCGeneration();   
            OpenAllScenesAndGernerateGUID(sceneName);          
        }
        //4 ocgenerate.bat 执行并行烘焙(得到pvs数据)
        public static void TestGenerateOCData(int index)
        {
            //GenerateOCData();
            var projectAssetPath = "./Assets";// System.Environment.GetCommandLineArgs()[1];
            //var index = 0;// int.Parse(System.Environment.GetCommandLineArgs()[2]);
            //PrintArgs(2);

            Debug.LogFormat("Generate OC Data Project Asset Path {0} index {1}", projectAssetPath, index);
            var config = LoadSceneConfig(projectAssetPath, index);
            if (string.IsNullOrEmpty(config.MapName))
            {
                Debug.LogErrorFormat("Can not get oc map config for stream scene in oc data generation, path {0} index {1}", projectAssetPath, 0);
                ExitOnBatchMode();
                return;
            }
            if (config.IsStreamScene)
            {
                //config = LoadSceneConfig(projectAssetPath, index);
                if (string.IsNullOrEmpty(config.MapName) == false)
                {
                    BakeStreamSceneByConfig(config);
                }
                else
                {
                    Debug.LogErrorFormat("Can not get oc map config for stream scene in oc data generation, path {0} index {1}", projectAssetPath, 0);
                    ExitOnBatchMode();
                }
            }
            else if(index == 0)
            {
                BakeSingleSceneByConfig(config);
            }
            else
            {
                ExitOnBatchMode();
            }
        }

        public static void TestBakeOneTile(string sceneName, int x, int y)
        {
            //GenerateOCDataForTile();
            var projectPath = "./Assets";
            var config = LoadSceneConfig(projectPath, 0);
            if (string.IsNullOrEmpty(config.MapName))
            {
                Debug.LogErrorFormat("Can not get oc map config for stream scene in oc data generation, path {0} index {1}", projectPath, 0);
                ExitOnBatchMode();
                return;
            }

            if (x < 0 || x >= config.TileDimension || y < 0 || y >= config.TileDimension)
            {
                Debug.LogErrorFormat("The Sepcified Tile Index [{0}, {1}] exceeds the Tile Dimension [{3}, {4}] in map {5}",
                    x, y, config.TileDimension, config.TileDimension, config.MapName);
                ExitOnBatchMode();
                return;
            }

            if (config.IsStreamScene)
            {
                ConfigGenerator(config);
                var streamScene = new MultiScene(config.GetSceneAssetPath(), config.SceneNamePattern, config.TileDimension, config.TileSize);
                var tileList = new List<Index>();
                tileList.Add(new Index(x, y));
                streamScene.BakeTiles(tileList, config.ComputePerframe, config.TemporaryContainer);
            }
            else
            {
                BakeSingleSceneByConfig(config);
            }            
        }

        //5 ocmerge.bat  MergeOCDarta：只在流式加载的地图中才会执行，把并行烘焙的结果合并输出
        public static void TestMergeOCDataForStreamScene()
        {
            //MergeOCDataForStreamScene();
            var projectPath = "./Assets";// System.Environment.GetCommandLineArgs()[1];
            //PrintArgs(1);

            var config = LoadSceneConfig(projectPath, 0);
            if (string.IsNullOrEmpty(config.MapName))
            {
                Debug.LogErrorFormat("Can not get oc map config for stream scene in oc data mergence, path {0} index {1}", projectPath, 0);
                return;
            }

            if (config.IsStreamScene)
                MergeStreamSceneOCData(config);
        }


        //-----------------------------------------------

        public static OCSceneConfig CreateSceneConfig(string sceneName, string scenePath, bool frameBake = true, bool bStream = false, string sceneNameTemplate = "",
                                                               float cellSize = 2.0f, bool mergeCell = false, float weight = 0.9f, int tileDim = 8, int tileSize = 1000)
        {
            OCSceneConfig config = new OCSceneConfig();
            config.MapName = sceneName;
            config.CellSize = cellSize;
            config.MaxPlayerHeight = 2.0f;
            config.MinPlayerHeight = 0;
            config.ScreenHeight = 600;
            config.ScreenWidth = 600;
            config.MergeCell = mergeCell;
            config.MergeCellWeight = weight;
            config.MergeObjectID = false;
            config.MergeObjectSize = 1;
            config.MergeObjectDistance = 1;
            config.TileDimension = tileDim;
            config.TileSize = tileSize;
            config.IsStreamScene = bStream;
            config.UseComputeShader = true;
            config.UseVisbileCache = true;
            config.SceneAssetPath = scenePath;
            config.SceneNamePattern = sceneNameTemplate;
            config.TemporaryContainer = "D:/OCTemp";
            config.ComputePerframe = frameBake;
            config.PerframeExecCount = 1000;
            if (bStream)
            {
                config.indices = new List<Index>();
                for (int i = 0; i < config.TileDimension; i++)
                    for (int j = 0; j < config.TileDimension; j++)
                        config.indices.Add(new Index(i, j));
            }
            else
            {
                config.SceneNamePattern = config.MapName;
                config.TileDimension = 1;
            }

            return config;
        }
        public static void TestCreateScensJson()
        {
            var sceneList = new List<OCSceneConfig>();

            sceneList.Add(CreateSceneConfig("S001", "Assets/Maps/maps/S001/Scenes"));
            sceneList.Add(CreateSceneConfig("S002", "Assets/Maps/maps/S002/Scenes"));
            sceneList.Add(CreateSceneConfig("S003", "Assets/Maps/maps/S003/Scenes"));
            sceneList.Add(CreateSceneConfig("M001", "Assets/Maps/maps/M001/Scenes"));
            sceneList.Add(CreateSceneConfig("M002", "Assets/Maps/maps/M002/Scenes"));
            sceneList.Add(CreateSceneConfig("M003", "Assets/Maps/maps/M003/Scenes"));
            sceneList.Add(CreateSceneConfig("M004", "Assets/Maps/maps/M004/Scenes"));
            sceneList.Add(CreateSceneConfig("M006", "Assets/Maps/maps/M006/Scenes"));

            var config = CreateSceneConfig("002", "Assets/Maps/maps/0001/Scenes", true, true, "002 {0}x{1}", 256, false);
            config.indices.Clear();
            config.indices.Add(new Index(2, 1));
            config.indices.Add(new Index(2, 2));
            config.indices.Add(new Index(2, 5));
            config.indices.Add(new Index(3, 1));
            config.indices.Add(new Index(3, 2));
            config.indices.Add(new Index(3, 3));
            config.indices.Add(new Index(4, 3));
            config.indices.Add(new Index(4, 5));
            config.indices.Add(new Index(4, 6));
            config.indices.Add(new Index(5, 4));
            config.indices.Add(new Index(5, 5));
            config.indices.Add(new Index(5, 7));
            sceneList.Add(config);

        
           

            OCScenesConfig scenesConfig = new OCScenesConfig();
            scenesConfig.scenesConfig = sceneList;

            string jsonString = JsonUtility.ToJson(scenesConfig, true);

            File.WriteAllText("Assets/Assets/CoreRes/template/OCScenesConfig.json", jsonString);

        }


        /*public static void OpenScenes(string name)
        {
            OCSceneConfig config = OCGenerator.GetSceneConfig(name);
            string path = config.GetSceneAssetPath();
            path += "/";


            if (config.IsStreamScene)
            {
                int tileDim = config.TileDimension;
                int tileSize = config.TileSize;

                var mainScene = SceneManager.GetSceneByName("AdditiveScene.unity");
                if(mainScene.isLoaded == false)
                    EditorSceneManager.OpenScene(path + "AdditiveScene.unity", OpenSceneMode.Additive);

                foreach (var index in config.indices)
                {
                    string sceneName = string.Format("{0} {1}x{2}", name, index.x , index.y);
                    string scenePath = path + sceneName + ".unity";

                    var scene = SceneManager.GetSceneByName(sceneName);
                    if (scene.isLoaded == false)
                        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                }

            }
            else
            {
                EditorSceneManager.OpenScene(path + name +".unity");
            }
        }*/

        public static void GenerateTestStreamScenes(string name)
        {
            OCSceneConfig config = OCGenerator.GetSceneConfig(name);
            string path = config.GetSceneAssetPath();

            path += "/";

            int tileDim = config.TileDimension;
            int tileSize = config.TileSize;

            var template = Resources.Load("root") as GameObject;

            var mainScene = EditorSceneManager.OpenScene(path + "AdditiveScene.unity");
            foreach (var root in mainScene.GetRootGameObjects())
            {
                GameObject.DestroyImmediate(root);
            }
            var mainCamera = Resources.Load("MainCamera") as GameObject;
            var cam = GameObject.Instantiate(mainCamera);           


            for (int i = 0; i < tileDim; i++)
                for (int j = 0; j < tileDim; j++)
                {
                    string sceneName = string.Format("{0} {1}x{2}", name, i, j);
                    string scenePath = path + sceneName + ".unity";
                    var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

                    EditorSceneManager.SetActiveScene(scene);

                    foreach (var root in scene.GetRootGameObjects())
                    {
                        GameObject.DestroyImmediate(root);
                    }

                    var templateGO = GameObject.Instantiate(template);
                    templateGO.transform.localScale = new Vector3(tileSize / 10, tileSize / 10, tileSize / 10);
                    templateGO.transform.position = new Vector3(i * tileSize, 0, j * tileSize);
                    var pos = templateGO.transform.position;
                    float offsetValue = -tileSize * tileDim * 0.5f;
                    Vector3 offset = new Vector3(offsetValue, 0, offsetValue);
                    templateGO.transform.position = pos + offset;

                    var coms = templateGO.GetComponentsInChildren<MeshRenderer>();

                    int count = 0;
                    foreach (var com in coms)
                    {
                        var idcom = com.gameObject.GetComponent<GameObjectID>();
                        if (idcom == null)
                            idcom = com.gameObject.AddComponent<GameObjectID>();

                        idcom.GUID = count;
                        count++;
                    }

                    EditorSceneManager.SaveScene(scene);
                }

            //EditorSceneManager.SaveOpenScenes();            
        }
        public static void TestBakeAll(string sceneName)
        {
            var config = GetSceneConfig(sceneName);
            ConfigGenerator(config);
            
            if (config.IsStreamScene)
            {
                var multiScene = new MultiScene(config.GetSceneAssetPath(), config.SceneNamePattern, config.TileDimension, config.TileSize);                
                multiScene.BakeTiles(config.indices, config.ComputePerframe, config.TemporaryContainer);
            }
            else
            {
                var scene = new SingleScene(config.GetSceneAssetPath(), config.SceneNamePattern, Index.InValidIndex);
                scene.Bake(true, "D:/OCTemp");
            }
        }

        public static void TestApplyOCData(string sceneName)
        {
            var config = GetSceneConfig(sceneName);
             ApplyOCData(sceneName, config.GetSceneAssetPath());
        }

        public static void WriteJsonConfig(string sceneName, bool IsStream, int screenWidth, int screenHeight, float maxPlayAreaHeight, float minPlayAreaHeight, float cellSize, 
            bool IsFrameBake, int frameCellCount, bool IsMergeCell, float cellMergeWeight, bool IsMergeObjectID, float mergeDistance, float mergeObjectSize, 
             List<Index> indices, string path = "D:/OCTemp",bool useCacheCell = true, bool useComputeShader = true)
        {
            var config = GetSceneConfig(sceneName);
            config.MapName = sceneName;
            config.ScreenWidth = screenWidth;
            config.ScreenHeight = screenHeight;
            config.MaxPlayerHeight = maxPlayAreaHeight;
            config.MinPlayerHeight = minPlayAreaHeight;
            config.CellSize = cellSize;
            config.ComputePerframe = IsFrameBake;
            config.PerframeExecCount = frameCellCount;        
            config.MergeCell = IsMergeCell;
            config.MergeCellWeight = cellMergeWeight;
            config.MergeObjectID = IsMergeObjectID;
            config.MergeObjectDistance = mergeDistance;
            config.MergeObjectSize = mergeObjectSize;
            config.IsStreamScene = IsStream;
            config.UseComputeShader = useComputeShader;
            config.UseVisbileCache = useCacheCell;
            config.TemporaryContainer = path;

            config.indices = indices;
          
            OCScenesConfig scenesConfig = GetScenesConfig();           
        
            for(int i=0;  i< scenesConfig.scenesConfig.Count; i++)
            {
                if(scenesConfig.scenesConfig[i].SceneNamePattern == config.SceneNamePattern)
                {        
                    scenesConfig.scenesConfig[i] = config;
                    break;
                }
            }


            //write
            string jsonString = JsonUtility.ToJson(scenesConfig, true);
            File.WriteAllText("Assets/Assets/CoreRes/template/OCScenesConfig.json", jsonString);
        }

        public static OCScenesConfig GetScenesConfig()
        {
            OCScenesConfig ret = new OCScenesConfig();
            try
            {
                var filePath = "Assets/template/OCScenesConfig.json";                
              
                if (!File.Exists(filePath))
                {
                    var otherFilePath = "Assets/Assets/CoreRes/template/OCScenesConfig.json";
                    if (!File.Exists(otherFilePath))
                    {
                        Debug.LogErrorFormat("Can not found config file: \"OCScenesConfig.json\" from path {0} or {1}", filePath, otherFilePath);
                        return ret;
                    }

                    filePath = otherFilePath;
                }              

                string templateContent = Util.LoadJson(filePath);

                ret = JsonUtility.FromJson<OCScenesConfig>(templateContent);

            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            return ret;
        }

        public static bool OpenAllScenesAndGernerateGUID(string mapName)
        {
            var config = GetSceneConfig(mapName);
            if (string.IsNullOrEmpty(config.MapName))
            {
                return false;
            }

            if (config.IsStreamScene)
            {
                Util.ClearScenes();

                var tiles = config.indices;

                var multiScene = new MultiScene(config.GetSceneAssetPath(), config.SceneNamePattern, config.TileDimension, config.TileSize);

                foreach (var tile in tiles)
                {
                    int tileX = tile.x;
                    int tileY = tile.y;

                    int tileDimension = config.TileDimension;
                    for (int x = 0; x < tileDimension; ++x)
                    {
                        for (int y = 0; y < tileDimension; ++y)
                        {
                            if (Math.Abs(x - tileX) > 1 || Math.Abs(y - tileY) > 1)
                            {
                                continue;
                            }

                            //string scenePath = string.Format("{0}/{1}.unity", config.GetSceneAssetPath(), string.Format(config.SceneNamePattern, x, y));

                            Util.OpenScene(config.GetSceneAssetPath(), string.Format(config.SceneNamePattern, x, y), OpenSceneMode.Single);
                            Util.OpenScene(config.GetSceneAssetPath() + "/Streaming", string.Format(config.SceneNamePattern, x, y) + SingleScene.StreamSuffix, OpenSceneMode.Additive);

                            SingleScene scene = new SingleScene(config.GetSceneAssetPath(), string.Format(config.SceneNamePattern, x, y), new Index(x, y), null, multiScene);
                            scene.GeneraterRenderableObjectID();

                            multiScene.AddMaxGUID(x, y, scene.MaxGameObjectIDCount);
                        }
                    }
                }

                multiScene.Save();
            }
            else
            {
                //string scenePath = string.Format("{0}/{1}.unity", config.GetSceneAssetPath(), config.SceneNamePattern);
                Util.OpenScene(config.GetSceneAssetPath(), config.SceneNamePattern, OpenSceneMode.Single);

                var singleScene = new SingleScene(config.GetSceneAssetPath(), config.SceneNamePattern, Index.InValidIndex);
                singleScene.GeneraterRenderableObjectID();
                singleScene.Save();
            }


           

            return true;
        }

        public static bool OpenScene(string mapName, Index index)
        {
            var config = GetSceneConfig(mapName);
            if (string.IsNullOrEmpty(config.MapName))
            {
                return false;
            }

            if (config.IsStreamScene)
            {
                string path = config.GetSceneAssetPath();              
                Util.OpenScene(path , "AdditiveScene", OpenSceneMode.Additive);

                var tiles = config.indices;             

                foreach (var tile in tiles)
                {
                    int tileX = tile.x;
                    int tileY = tile.y;

                    if (tileX == index.x && tileY == index.y)
                    {
                        int tileDimension = config.TileDimension;
                        for (int x = 0; x < tileDimension; ++x)
                        {
                            for (int y = 0; y < tileDimension; ++y)
                            {
                                if (Math.Abs(x - tileX) > 1 || Math.Abs(y - tileY) > 1)
                                {
                                    continue;
                                }

                                //string scenePath = string.Format("{0}/{1}.unity", config.GetSceneAssetPath(), string.Format(config.SceneNamePattern, x, y));

                                Util.OpenScene(path, string.Format(config.SceneNamePattern, x, y),OpenSceneMode.Additive);
                                Util.OpenScene(path + "/Streaming", string.Format(config.SceneNamePattern, x, y) + SingleScene.StreamSuffix, OpenSceneMode.Additive);
                            }
                        }
                    }
                }
            }
            else
            {
                //string scenePath = string.Format("{0}/{1}.unity", config.GetSceneAssetPath(), config.SceneNamePattern);
                Util.OpenScene(config.GetSceneAssetPath(), config.SceneNamePattern, OpenSceneMode.Single);              
            }

            return true;
        }
    }
}
