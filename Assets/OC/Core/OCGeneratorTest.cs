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
    // public class SceneConfig
    // {
    //     public string sceneName;
    //     public bool IsStreamScene;
    //     public string path;
    //     //public string SceneNamePattern;
    //     //public string TemporaryContainer;
    //     public int TileDimension;
    //     public bool UseComputeShader;
    //     public bool UseVisbileCache;
    //     public bool ComputePerframe;
    //     public int PerframeExecCount;

    //     public bool bakeAll;

    //     public List<Index> Tiles;

        

       
    //     public List<Index> GetBakeTiles()
    //     {
    //         var tiles = Tiles;
    //         //if (tiles == null)
    //         if(bakeAll && IsStreamScene)
    //         {
    //             //bake all tiles if there is no any tile specified to bake
    //             tiles = new List<Index>();
    //             var dimension = TileDimension;
    //             for (int x = 0; x < dimension; ++x)
    //             {
    //                 for (int y = 0; y < dimension; ++y)
    //                 {
    //                     tiles.Add(new Index(x, y));
    //                 }
    //             }
    //         }

    //         return tiles;
    //     }

        // public string GetSceneName(int x, int y)
        // {
        //     if (IsStreamScene)
        //     {
        //         return MultiScene.GetSceneName(x, y);
        //     }

        //     return SceneNamePattern;
        // }

        // public string GetOCDataFilePath()
        // {
        //     return System.IO.Path.Combine(TemporaryContainer, GetOCDataFileName());
        // }

        // public string GetOCDataFileName()
        // {
        //     string fileName;
        //     if (IsStreamScene)
        //     {
        //         fileName = MultiScene.GetOCDataFileName(SceneNamePattern);
        //     }
        //     else
        //     {
        //         fileName = SingleScene.GetOCDataFileName(SceneNamePattern);
        //     }

        //     return fileName;
        // }
    //}
    public partial class OCGenerator
    {
        //         private static OCMapConfig LoadOCMapConfig(string projectAssetPath, int index)
//         {
//             var filePath = Path.Combine(projectAssetPath, String.Format("OCGenMapConfig_{0}.xml", index));
            
//             if (!File.Exists(filePath))
//             {
//                 Debug.LogErrorFormat("oc gen map config file {0} does not exist, path {1} index {2}", filePath, projectAssetPath, index);
//                 return null;
//             }

//             var doc = new XmlDocument();
//             doc.Load(filePath);
//             var root = doc.DocumentElement;
            
//             var config = new OCMapConfig();
//             config.MapName = ParseXmlNode<string>(root, "MapName");
//             ParseOCMapConfig(root, config);
//             return config;
//         }
       public string LoadJson(string path, int index)
       {
           string ret = null;
           string filePath = Path.Combine(path, "sceneConfig.json");
          
           if(index >=0 )
                filePath = Path.Combine(path, string.Format("sceneConfig{0}.json", index));
  
            if (!File.Exists(filePath))
             {
                 Debug.LogErrorFormat("oc gen map config file {0} does not exist, path {1} index {2}", filePath, path, index);
                 return ret;
             }

             var stream = File.Open(filePath, FileMode.Open);
            ret = stream.ToString();
           return ret;
       }
       public void BakeAll(string worldName, int tileSize, int tileDimension)
       {
           InitConfig();

            var multiScene = new MultiScene(GetScenePath(), StreamSceneNamePattern, tileDimension, tileSize);

            multiScene.BakeAll(tileSize, tileDimension);
       }
        public void GenerateTestStreamScenes(string path)
        {
            // var emptyScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // var obj = Resources.Load("root");
            // var root = GameObject.
            for(int i=0; i< 8; i++)
            for(int j=0; j< 8; j++)
            {
                string sceneName = string.Format("002 {0}x{1}", i, j);

                string scenePath = path + sceneName + ".unity";
                
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

                foreach(var root in scene.GetRootGameObjects())
                {
                    if(root.name == "root")
                    {
                        root.transform.position = new Vector3(i * 10, 0, j * 10);
                        var cam = root.GetComponentInChildren<Camera>();
                        if(cam != null)
                        GameObject.DestroyImmediate(cam.gameObject);
                        var dirLight = root.GetComponentInChildren<Light>();
                        if(dirLight != null)
                        GameObject.DestroyImmediate(dirLight.gameObject);
                    } 
                }

                EditorSceneManager.SaveScene(scene);
            }

            EditorSceneManager.SaveOpenScenes();
            
        }
    
        //工作路径 D:/trunk/develop
        //总体流程参考 build_assetbundle_multi_slaves_ocgenerate.jenkinsfile
        //0 GetNode 复制OC相关的配置文件和bat脚本到相应目录 D:/trunk/oc_deploy   copy to  D:/trunk/develop

        //1 occonfiggen.bat 根据OCGenMapConfig.xml配置文件为每个烘焙进程生成一个对应的OC烘焙配置文件
        public static void TestGenerateOCGenMapConfigFile()
        {
            GenerateOCGenMapConfigFile("002", true, 8);
            var config = LoadOCMapConfig(".\\Assets", 0);
            Debug.LogFormat("Config Is {0}", config);
        }
        //2 ocprojectgen.bat 为每个烘焙进程生成一个项目，其中大部分资源目录都是软链接到原始项目目录的
        //3 coinitgen.bat 删除需要烘焙的场景文件的全局光照信息，为场景文件中可渲染game object生成render id
        public static void TestInitOCGeneration()
        {
            //InitOCGeneration();
            PrintSystemInfo();

            var mapName = "002";//System.Environment.GetCommandLineArgs()[1];
            var tileX = 0;//int.Parse(System.Environment.GetCommandLineArgs()[2]);
            var tileY = 0;// int.Parse(System.Environment.GetCommandLineArgs()[3]);
            if (!OpenAllScenes(mapName, tileX, tileY))
                return;
            
            //ClearLightmappingData(mapName, tileX, tileY);
            GenerateAllSceneRenderableObjectID();
        }
        //4 ocgenerate.bat 执行并行烘焙(得到pvs数据)
        public static void TestGenerateOCData()
        {
            //GenerateOCData();
           var projectAssetPath = System.Environment.GetCommandLineArgs()[1];
            var index = int.Parse(System.Environment.GetCommandLineArgs()[2]);
            PrintArgs(2);

            Debug.LogFormat("Generate OC Data Project Asset Path {0} index {1}", projectAssetPath, index);
            var config = LoadOCMapConfig(projectAssetPath, 0);
            if (config == null)
            {
                Debug.LogErrorFormat("Can not get oc map config for stream scene in oc data generation, path {0} index {1}", projectAssetPath, 0);
                ExitOnBatchMode();
                return;
            }

            //SetTestData();

            if (config.IsStreamScene)
            {
                config = LoadOCMapConfig(projectAssetPath, index);
                if (config != null)
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
        //5 ocmerge.bat  MergeOCDarta：只在流式加载的地图中才会执行，把并行烘焙的结果合并输出
        public static void TestMergeOCDataForStreamScene()
        {
            //MergeOCDatatForStreamScene();
            var projectPath = "";// System.Environment.GetCommandLineArgs()[1];
            //PrintArgs(1);

            var config = LoadOCMapConfig(projectPath, 0);
            if (config == null)
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

        //     var config = new OCMapConfig()
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
