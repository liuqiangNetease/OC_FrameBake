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
        public static OCSceneConfig CreateSceneConfig( string sceneName, string scenePath, bool frameBake = true, bool bStream = false, string sceneNameTemplate = "",float cellSize = 2.0f, bool mergeCell = false, float weight = 0.9f,  int tileDim = 8, int tileSize = 1000 )
        {
            OCSceneConfig config = new OCSceneConfig();
            config.MapName = sceneName;
            config.CellSize = cellSize;
            config.MaxPlayerHeight = 2.5f;
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
            config.PerframeExecCount = 100;
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
            return config;
        }
         public static void TestCreateScensJson()
        {
            var sceneList = new List<OCSceneConfig>();

            sceneList.Add(CreateSceneConfig("sTest","Assets/Scene"));
            sceneList.Add(CreateSceneConfig("singleTest", "Assets/Scene"));

            sceneList.Add(CreateSceneConfig("S001", "Assets/Maps/maps/S001/Scenes"));
            sceneList.Add(CreateSceneConfig("S002", "Assets/Maps/maps/S002/Scenes"));
            sceneList.Add(CreateSceneConfig("S003", "Assets/Maps/maps/S003/Scenes"));
            sceneList.Add(CreateSceneConfig("M001", "Assets/Maps/maps/M001/Scenes"));
            sceneList.Add(CreateSceneConfig("M002", "Assets/Maps/maps/M002/Scenes"));
            sceneList.Add(CreateSceneConfig("M003", "Assets/Maps/maps/M003/Scenes"));
            sceneList.Add(CreateSceneConfig("M004", "Assets/Maps/maps/M004/Scenes"));
            sceneList.Add(CreateSceneConfig("M006", "Assets/Maps/maps/M006/Scenes"));

            sceneList.Add(CreateSceneConfig("002", "Assets/Maps/maps/0001/Scenes", true, true, "002 {0}x{1}",1,false,0.9f, 8, 10));
            /*config.indices.Add(new Index(2, 1));
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

            OCScenesConfig scenesConfig = new OCScenesConfig();
            scenesConfig.scenesConfig = sceneList;

            string jsonString = JsonUtility.ToJson(scenesConfig, true);           

            File.WriteAllText("Assets/Assets/CoreRes/template/OCScenesConfig.json", jsonString);

        }
        

        public static void GenerateTestStreamScenes(string path)
        {
            var template = Resources.Load("root") as GameObject;


            EditorSceneManager.OpenScene(path + "Additive.unity");

            for (int i=0; i< 8; i++)
            for(int j=0; j< 8; j++)
            {
                string sceneName = string.Format("002 {0}x{1}", i, j);
                string scenePath = path + sceneName + ".unity";                
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

                EditorSceneManager.SetActiveScene(scene);

                foreach (var root in scene.GetRootGameObjects())
                {
                    GameObject.DestroyImmediate(root);
                }

                var templateGO = GameObject.Instantiate(template);  
                templateGO.transform.position = new Vector3(i * 10, 0, j * 10);

                    var coms = templateGO.GetComponentsInChildren<MeshRenderer>();
                    

                    int count = 0;
                    foreach(var com in coms)
                    {
                        var idcom = com.gameObject.GetComponent<GameObjectID>();
                        if(idcom == null)
                            idcom = com.gameObject.AddComponent<GameObjectID>();

                        idcom.GUID = count;
                        count++;
                    }

                EditorSceneManager.SaveScene(scene);
            }

            //EditorSceneManager.SaveOpenScenes();            
        }
    
        //工作路径 D:/trunk/develop
        //总体流程参考 build_assetbundle_multi_slaves_ocgenerate.jenkinsfile
        //0 GetNode 复制OC相关的配置文件和bat脚本到相应目录 D:/trunk/oc_deploy   copy to  D:/trunk/develop

        //1 occonfiggen.bat 根据OCGenMapConfig.xml配置文件为每个烘焙进程生成一个对应的OC烘焙配置文件
        public static void TestGenerateOCGenMapConfigFile(string sceneName, int processNum)
        {
            GenerateOCGenMapConfigFile(sceneName, false, processNum);
            var config = LoadOCMapConfig("./Assets", 0);
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
            if (!OpenAllScenes(sceneName, tileX, tileY))
                return;

            //ClearLightmappingData(sceneName, tileX, tileY);
            GenerateAllSceneRenderableObjectID();
        }
        //4 ocgenerate.bat 执行并行烘焙(得到pvs数据)
        public static void TestGenerateOCData(int index)
        {
            //GenerateOCData();
            var projectAssetPath = "./Assets";// System.Environment.GetCommandLineArgs()[1];
            //var index = 0;// int.Parse(System.Environment.GetCommandLineArgs()[2]);
            //PrintArgs(2);

            Debug.LogFormat("Generate OC Data Project Asset Path {0} index {1}", projectAssetPath, index);
            var config = LoadOCMapConfig(projectAssetPath, index);
            if (config.MapName == string.Empty)
            {
                Debug.LogErrorFormat("Can not get oc map config for stream scene in oc data generation, path {0} index {1}", projectAssetPath, 0);
                ExitOnBatchMode();
                return;
            }
            if (config.IsStreamScene)
            {
                //config = LoadOCMapConfig(projectAssetPath, index);
                if (config.MapName != string.Empty)
                {
                    GenerateOCDataForStreamScene(config);
                }
                else
                {
                    Debug.LogErrorFormat("Can not get oc map config for stream scene in oc data generation, path {0} index {1}", projectAssetPath, 0);
                    ExitOnBatchMode();
                }
            }
            else if(index == 0)
            {
                GenerateOCDataForFixedScene(config);
            }
            else
            {
                ExitOnBatchMode();
            }
        }
        public static void TestBakeAll(string sceneName)
        {
            var config = GetMapConfig(sceneName);
            ConfigGenerator(config);
            var tiles = config.indices;
            if (tiles != null)
            {
                var multiScene = new MultiScene(config.SceneAssetPath, config.SceneNamePattern, config.TileDimension, config.TileSize);
                multiScene.BakeTiles(tiles, config.ComputePerframe);
            }
            else
            {
                Debug.LogErrorFormat("Can not get bake tiles for map {0}", config.MapName);
                ExitOnBatchMode();
            }
        }
        //5 ocmerge.bat  MergeOCDarta：只在流式加载的地图中才会执行，把并行烘焙的结果合并输出
        public static void TestMergeOCDataForStreamScene()
        {
            //MergeOCDataForStreamScene();
            var projectPath = "./Assets";// System.Environment.GetCommandLineArgs()[1];
            //PrintArgs(1);

            var config = LoadOCMapConfig(projectPath, 0);
            if (config.MapName == string.Empty)
            {
                Debug.LogErrorFormat("Can not get oc map config for stream scene in oc data mergence, path {0} index {1}", projectPath, 0);
                return;
            }

            if (config.IsStreamScene)
                MergeStreamSceneOCData(config.SceneAssetPath, config.SceneNamePattern, config.TemporaryContainer, config.TileDimension);
        }





        // public static void TestGenerateOCDataForFixedScene()
        // {
        //     var mapName = System.Environment.GetCommandLineArgs()[1];
        //     PrintArgs(2);

        //     var config = GetMapConfig(mapName);
        //     if (config == null)
        //     {
        //         Debug.LogErrorFormat("Can not get oc map config for stream scene, mapName {0}",  mapName);
        //         return;
        //     }

        //     GenerateOCDataForFixedScene(config);
        // }

        // public static void TestGenerateOCDataForStreamScene()
        // {
        //     var projectPath = System.Environment.GetCommandLineArgs()[1];
        //     var index = int.Parse(System.Environment.GetCommandLineArgs()[2]);
        //     var x = int.Parse(System.Environment.GetCommandLineArgs()[3]);
        //     var y = int.Parse(System.Environment.GetCommandLineArgs()[4]);
        //     PrintArgs(4);

        //     var config = LoadOCMapConfig(projectPath, index);
        //     if (config == null)
        //     {
        //         Debug.LogErrorFormat("Can not get oc map config for stream scene, path {0} index {1}", projectPath, index);
        //         return;
        //     }

        //     TestGenerateStreamSceneOCData(config.SceneAssetPath, config.SceneNamePattern, config.TemporaryContainer, x, y, config.TileDimension, config.UseComputeShader, config.UseVisbileCache);
        // }

        // public static void TestGenerateStreamSceneOCData(string path, string sceneNamePattern, string temporaryContainer, int x, int y, int tileDimension = 8, bool useComputeShader = true, bool useVisibleCache = true)
        // {
        //     Config.CustomVolume = true;
        //     Config.CustomVolumeCenter = new Vector3(100, 100, 100);
        //     Config.CustomVolumeSize = new Vector3(10, 10, 10);

        //     var config = new OCSceneConfig()
        //     {
        //         SceneAssetPath = path,
        //         SceneNamePattern = sceneNamePattern,
        //         TemporaryContainer = temporaryContainer,
        //         TileDimension = tileDimension,
        //         UseComputeShader = useComputeShader,
        //         UseVisbileCache = useVisibleCache
        //     };
            
        //     var contextIter = StreamOCBakeContextGenerator(config, x, y);
        //     var contextManager = new OCBakeContextManager(contextIter);
        //     contextManager.Bake();
        // }

        

        // public static void TestApplyOCData()
        // {
        //     ApplyOCData("002", @"D:\voyager\UnityProjects\App.Client.Unity\App.Client.Unity-Windows\");
        // }

        // public static void TestOCDataFile()
        // {

        //     using (var fs = File.Open(@"D:\voyager\UnityPackages\Assets\Maps\maps\0001\Scenes\002 ocxoc_oc.txt",
        //         FileMode.Open))
        //     {
        //         var length = (int) fs.Length;
        //         var data = new byte[length];
        //         fs.Read(data, 0, length);

        //         var multiScene = new MultiScene(@"D:\voyager\UnityPackages\Assets\Maps\maps\0001\Scenes", "002 {0}x{1}", 8, 1000, data);
        //         multiScene.Load();
        //     }
        // }

        // public static void TestCameraRender()
        // {

        //     Debug.LogFormat("Open scenes count {0}", SceneManager.sceneCount);
        //     for (int i = 0; i < SceneManager.sceneCount; ++i)
        //     {
        //         Debug.LogFormat("Opened Scene {0}", SceneManager.GetSceneAt(i).name);
        //     }
            
        //     EditorSceneManager.OpenScene("Assets/Assets/octest.unity");
        //     var cam = Camera.main;

        //     var oldRT = cam.targetTexture;
        //     var oladActiveRT = RenderTexture.active;

        //     var renderTex = RenderTexture.GetTemporary(Config.ScreenWidth, Config.ScreenHeight, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);;
        //     cam.targetTexture = renderTex;
        //     RenderTexture.active = renderTex;
            
        //     Debug.LogFormat("Start Camera Render Test datapath {0}", Application.dataPath);
        //     cam.Render();

        //     Debug.LogFormat("Finish Camera Render");

        //     Texture2D Image = new Texture2D(cam.targetTexture.width, cam.targetTexture.height);
        //     Image.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
        //     Image.Apply();

        //     RenderTexture.active = oladActiveRT;
        //     cam.targetTexture = oldRT;

        //    var Bytes = Image.EncodeToPNG();
        //     GameObject.DestroyImmediate(Image);

        //     File.WriteAllBytes(Application.dataPath + "/../" +  "camera.png", Bytes);
        // }
    }
}
