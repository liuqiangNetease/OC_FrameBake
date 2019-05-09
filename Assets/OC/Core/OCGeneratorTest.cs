using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using ArtPlugins;
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
        public static void TestInitOCGeneration(string sceneName, int tileX, int tileY)
        {
            //InitOCGeneration();
            PrintSystemInfo();
            //var mapName = "002";//System.Environment.GetCommandLineArgs()[1];
            //var tileX = 0;//int.Parse(System.Environment.GetCommandLineArgs()[2]);
            //var tileY = 0;// int.Parse(System.Environment.GetCommandLineArgs()[3]);
            //if (!OpenAllScenes(sceneName, tileX, tileY))
                //return;
            //ClearLightmappingData();
            //GenerateAllSceneRenderableObjectID();
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
                                                               float cellSize = 2.0f, bool mergeCell = false, float weight = 0.9f, int tileDim = 8, int tileSize = 1000,
                                                               bool customVolume = false, Vector3 center = default(Vector3), Vector3 size= default(Vector3))
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
                config.TileDimension = 0;
            }

            config.CustomVolume = customVolume;
            config.VolumeCenter = center;
            config.VolumeSize = size;

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

            var config = CreateSceneConfig("002", "Assets/Maps/maps/0001/Scenes", true, true, "002 {0}x{1}", 56, false);
            /*config.indices.Clear();
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
            config.indices.Add(new Index(5, 7));*/
            sceneList.Add(config);

            sceneList.Add(CreateSceneConfig("002", "Assets/LQ/Scenes/testStream", true, true, "002 {0}x{1}", 2, false, 0.9f, 8, 10));
            sceneList.Add(CreateSceneConfig("testSimple", "Assets/LQ/Scenes"));
           

            OCScenesConfig scenesConfig = new OCScenesConfig();
            scenesConfig.scenesConfig = sceneList;

            string jsonString = JsonUtility.ToJson(scenesConfig, true);

            File.WriteAllText("Assets/Assets/CoreRes/template/OCScenesConfig.json", jsonString);

        }

        public static void OpenScenes(string name)
        {
            OCSceneConfig config = OCGenerator.GetSceneConfig(name);
            string path = config.GetSceneAssetPath();
            path += "/";


            if (config.IsStreamScene)
            {
                int tileDim = config.TileDimension;
                int tileSize = config.TileSize;

                var mainScene = EditorSceneManager.OpenScene(path + "AdditiveScene.unity");

                for (int i = 0; i < tileDim; i++)
                    for (int j = 0; j < tileDim; j++)
                    {
                        string sceneName = string.Format("{0} {1}x{2}", name, i, j);
                        string scenePath = path + sceneName + ".unity";
                        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                    }
            }
            else
            {
                EditorSceneManager.OpenScene(path + name +".unity");
            }
        }

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
    }
}
