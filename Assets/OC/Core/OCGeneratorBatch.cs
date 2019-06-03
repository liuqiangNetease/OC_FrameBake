
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

#if UNITY_EDITOR
        //jenkins interface 产生配置文件
        public static void GenerateOCGenMapConfigFile()
        {
            Config.IsBatchMode = true;
            var mapName = System.Environment.GetCommandLineArgs()[1];
            var bakeForTile = bool.Parse(System.Environment.GetCommandLineArgs()[2]);
            var processorNum = int.Parse(System.Environment.GetCommandLineArgs()[3]);
            PrintArgs(2);

            GenerateOCGenMapConfigFile(mapName, bakeForTile, processorNum);
        }
        private static void GenerateOCGenMapConfigFile(string mapName, bool bakeForTile, int processorNum)
        {
            var config = GetSceneConfig(mapName);
            if (string.IsNullOrEmpty(config.MapName))
            {
                Debug.LogErrorFormat("batch mode Can not found oc map config item for map {0}", mapName);
                return;
            }

            DeleteAllSceneConfigFile("./Assets");

            GenerateAllSceneConfigFile(config, bakeForTile, processorNum);
        }

        private static void DeleteAllSceneConfigFile(string path)
        {
            //delete origin oc generation files
            var configFiles = Directory.GetFiles(path, "OCSceneConfig*.json");
            foreach (var file in configFiles)
            {
                File.Delete(file);
            }
        }

        private static void GenerateAllSceneConfigFile(OCSceneConfig config, bool bakeForTile, int processorNum)
        {
            if (config.IsStreamScene)
            {
                var bakeTiles = config.indices;
                var tileCount = bakeTiles.Count;
                processorNum = processorNum > tileCount ? tileCount : processorNum;
                if (processorNum == 0)
                    processorNum = 1;
                var perCountArray = new int[processorNum];
                for (int i = 0; i < tileCount; ++i)
                {
                    perCountArray[i % processorNum] += 1;
                }

                var startTile = 0;
                for (int index = 0; index < processorNum; ++index)
                {
                    Debug.LogFormat("batch mode Baking Tile Count for Processor {0} is {1}", index, perCountArray[index]);
                    if (bakeForTile)
                    {
                        WriteJsonFile("./Assets", index, config);
                    }
                    else
                    {
                        var tiles = GetConfigTiles(config.indices, startTile, perCountArray[index]);

                        OCSceneConfig tempConfig = config;
                        tempConfig.indices = tiles;
                        
                        WriteJsonFile("./Assets", index, tempConfig);
                        startTile += perCountArray[index];
                    }

                }
            }
            else
            {
                WriteJsonFile("./Assets", 0, config);
            }

        }


        //---------------------------------------------------
        ////jenkins interface 
        public static void InitOCGeneration()
        {
            Config.IsBatchMode = true;
            PrintSystemInfo();

            var mapName = System.Environment.GetCommandLineArgs()[1];
            var tileX = int.Parse(System.Environment.GetCommandLineArgs()[2]);
            var tileY = int.Parse(System.Environment.GetCommandLineArgs()[3]);
            if (!OpenAllScenes(mapName, tileX, tileY))
                return;

            ClearLightmappingData();
            GenerateAllSceneRenderableObjectID();
        }        

        private static bool OpenAllScenes(string mapName, int tileX, int tileY)
        {
            //close existed scenes 
            Util.ClearScenes();

            //open new scenes
            var config = GetSceneConfig(mapName);
            if (string.IsNullOrEmpty(config.MapName))
            {
                return false;
            }

            var sceneNames = new List<string>();
            if (config.IsStreamScene)
            {
                int tileDimension = config.TileDimension;
                for (int x = 0; x < tileDimension; ++x)
                {
                    for (int y = 0; y < tileDimension; ++y)
                    {
                        if (tileX >= 0 && tileY >= 0)
                        {
                            if (Math.Abs(x - tileX) > 1 || Math.Abs(y - tileY) > 1)
                            {
                                continue;
                            }
                        }

                        sceneNames.Add(String.Format("{0}/{1}.unity", config.GetSceneAssetPath(),
                            String.Format(config.SceneNamePattern, x, y)));
                    }
                }
            }
            else
            {
                sceneNames.Add(String.Format("{0}/{1}.unity", config.GetSceneAssetPath(), config.SceneNamePattern));
            }

            foreach (var sceneName in sceneNames)
            {
                if (!Util.IsSceneOpened(sceneName))
                {
                    Debug.LogFormat("batch mode Open Scene {0}...", sceneName);
                    EditorSceneManager.OpenScene(sceneName, OpenSceneMode.Additive);
                }

                if (Config.PreProcess && config.IsStreamScene)
                {
                    string streamSceneName = sceneName + SingleScene.StreamSuffix;
                    if (!Util.IsSceneOpened(streamSceneName))
                    {
                        Debug.LogFormat("batch mode Open Scene {0}...", streamSceneName);
                        EditorSceneManager.OpenScene(streamSceneName, OpenSceneMode.Additive);
                    }
                }
            }
            return true;
        }
        public static void ClearLightmappingData()
        {
            Debug.Log("Clear Lighting Data Asset ...");
            Lightmapping.ClearLightingDataAsset();
            Lightmapping.Clear();
            Debug.Log("Clear Lighting Data Asset Successfully!");
        }

        private static void GenerateAllSceneRenderableObjectID()
        {
            var sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name.Equals(String.Empty))
                {
                    continue;
                }
                if(scene.name.EndsWith(SingleScene.StreamSuffix))
                {
                    continue;
                }

                var singleScene = new SingleScene(scene.path, scene.name, Index.InValidIndex);
                singleScene.GeneraterRenderableObjectID();
                singleScene.Save();
            }
        }

        //------------------------------
        //jenkins interface computer pvs data for json config
        public static void GenerateOCData()
        {
            Config.IsBatchMode = true;
            var projectAssetPath = System.Environment.GetCommandLineArgs()[1];
            var index = int.Parse(System.Environment.GetCommandLineArgs()[2]);
            PrintArgs(2);

            Debug.LogFormat("batch mode Generate OC Data Project Asset Path {0} index {1}", projectAssetPath, index);
            var config = LoadSceneConfig(projectAssetPath, 0);
            if (string.IsNullOrEmpty(config.MapName))
            {
                Debug.LogErrorFormat("batch mode Can not find json file, path {0} index {1}", projectAssetPath, 0);
                ExitOnBatchMode();
                return;
            }

            //SetTestData();

            if (config.IsStreamScene)
            {
                config = LoadSceneConfig(projectAssetPath, index);
                if (string.IsNullOrEmpty(config.MapName) == false)
                {
                    BakeStreamSceneByConfig(config);
                }
                else
                {                    
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

        private static void BakeSingleSceneByConfig(OCSceneConfig config)
        {
            Debug.Log("batch mode Do bake single");
            ConfigGenerator(config);
            if (!Util.IsSceneOpened(config.SceneNamePattern))
            {
                Debug.LogFormat("batch mode Open Scene {0}", config.SceneNamePattern);
                EditorSceneManager.OpenScene(String.Format("{0}/{1}.unity", config.GetSceneAssetPath(), config.SceneNamePattern));
            }


            var scene = new SingleScene(config.GetSceneAssetPath(), config.SceneNamePattern, Index.InValidIndex);
            scene.tempPath = config.TemporaryContainer;
            scene.Bake(config.ComputePerframe, config.TemporaryContainer);
        }
        private static void BakeStreamSceneByConfig(OCSceneConfig config)
        {
            Debug.Log("batch mode Do bake stream!");
            ConfigGenerator(config);
            var tiles = config.indices;
            if (tiles != null)
            {
                var multiScene = new MultiScene(config.GetSceneAssetPath(), config.SceneNamePattern, config.TileDimension, config.TileSize);
                multiScene.BakeTiles(tiles, config.ComputePerframe, config.TemporaryContainer);
            }
            else
            {
                Debug.LogErrorFormat("batch mode Can not get bake tiles for map {0}", config.MapName);
                ExitOnBatchMode();
            }
        }

        //jenkins interface computer pvs data for one tile
        public static void GenerateOCDataForTile()
        {
            Config.IsBatchMode = true;
            var projectPath = System.Environment.GetCommandLineArgs()[1];
            var index = int.Parse(System.Environment.GetCommandLineArgs()[2]);
            var processorNum = int.Parse(System.Environment.GetCommandLineArgs()[3]);
            var x = int.Parse(System.Environment.GetCommandLineArgs()[4]);
            var y = int.Parse(System.Environment.GetCommandLineArgs()[5]);
            PrintArgs(5);

            //Config.CustomVolume = true;
            //Config.CustomVolumeCenter = new Vector3(100, 100, 100);
            //Config.CustomVolumeSize = new Vector3(10, 10, 10);

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
            else if(index == 0)
            {
                BakeSingleSceneByConfig(config);
            }
            else
            {
                ExitOnBatchMode();
            }
        }

        //jenkins interface merge oc data into one file
        public static void MergeOCDataForStreamScene()
        {
            Config.IsBatchMode = true;
            var projectPath = System.Environment.GetCommandLineArgs()[1];
            PrintArgs(1);

            var config = LoadSceneConfig(projectPath, 0);
            if (string.IsNullOrEmpty(config.MapName))
            {
                Debug.LogErrorFormat("Can not get oc map config for stream scene in oc data mergence, path {0} index {1}", projectPath, 0);
                return;
            }

            if (config.IsStreamScene)
                MergeStreamSceneOCData(config);
        }

        public static void MergeStreamSceneOCData(OCSceneConfig config)
        {
            var scene = new MultiScene(config.GetSceneAssetPath(), config.SceneNamePattern, config.TileDimension, config.TileSize);
            scene.MergeOCData(config.TemporaryContainer);
            scene.CopyOCDataTo(config.TemporaryContainer);
        }



        //----------------------------
       
        public static OCSceneConfig GetSceneConfig(string mapName)
        {
            OCSceneConfig ret = new OCSceneConfig();

            try
            {
                var filePath = "Assets/template/OCScenesConfig.json";
                if (UnityEditorInternal.InternalEditorUtility.inBatchMode)
                {
                    if (!File.Exists(filePath))
                    {
                        var otherFilePath = "Assets/CoreRes/template/OCScenesConfig.json";
                        if (!File.Exists(otherFilePath))
                        {
                            Debug.LogErrorFormat("batch mode  Can not found config file: \"OCScenesConfig.json\" from path {0} or {1}", filePath, otherFilePath);
                            return ret;
                        }

                        filePath = otherFilePath;
                    }
                }
                else
                {
                    if (!File.Exists(filePath))
                    {
                        var otherFilePath = "Assets/Assets/CoreRes/template/OCScenesConfig.json";
                        if (!File.Exists(otherFilePath))
                        {
                            //Debug.LogErrorFormat("Can not found config file: \"OCScenesConfig.json\" from path {0} or {1}", filePath, otherFilePath);
                            //return ret;
                            otherFilePath = "Assets/CoreRes/template/OCScenesConfig.json";
                        }

                        filePath = otherFilePath;
                    }
                }

                string templateContent = Util.LoadJson(filePath);

                var scenesConfig = JsonUtility.FromJson<OCScenesConfig>(templateContent);


                foreach (var sceneConfig in scenesConfig.scenesConfig)
                {
                    if (sceneConfig.MapName == mapName)
                    {
                        ret = sceneConfig;
                        break;
                    }
                }
            }
            catch(Exception e)
            {
                Debug.LogError(e);
            }

            return ret;
        }

        public static List<Index> GetConfigTiles(List<Index> indeices, int start, int count)
        {
            List<Index> ret = new List<Index>();

            int iCount = 0;
            for (int i = 0; i < indeices.Count; i++)
            {
                var index = indeices[i];
                if (i >= start && iCount < count)
                {
                    ret.Add(index);
                    iCount++;
                }
            }

            return ret;
        }

        private static void WriteJsonFile(string path, int index, OCSceneConfig config)
        {
            try
            {
                var fileName = String.Format("OCSceneConfig{0}.json", index);
                var filePath = Path.Combine(path, fileName);

                string jsonText = JsonUtility.ToJson(config, true);

                File.WriteAllText(filePath, jsonText);
            }
            catch(Exception e)
            {
                Debug.LogError("batch mode:" + e);
            }
        }

        public static OCSceneConfig LoadSceneConfig(string projectAssetPath, int index)
        {
            OCSceneConfig ret = new OCSceneConfig();

            try
            {
                var filePath = Path.Combine(projectAssetPath, String.Format("OCSceneConfig{0}.json", index));

                if (!File.Exists(filePath))
                {
                    Debug.LogErrorFormat("batch mode json file {0} does not exist!", filePath);
                    return ret;
                }

                string jsonContent = Util.LoadJson(filePath);

                ret = JsonUtility.FromJson<OCSceneConfig>(jsonContent);
            }
            catch(Exception e)
            {
                Debug.LogError("batch mode:" + e);
            }

            return ret;
        }

        private static void ConfigGenerator(OCSceneConfig config)
        {

            Config.CellSize = config.CellSize;
            Config.ScreenHeight = config.ScreenHeight;
            Config.ScreenWidth = config.ScreenWidth;
            Config.MaxPlayAreaHeight = config.MaxPlayerHeight;
            Config.MinPlayAreaHeight = config.MinPlayerHeight;
            Config.mergeCell = config.MergeCell;
            Config.CellWeight = config.MergeCellWeight;
            Config.mergeObjectID = config.MergeObjectID;
            Config.mergeObjectDistance = config.MergeObjectDistance;
            Config.mergeObjectMaxSize = config.MergeObjectSize;
            Config.UseComputeShader = config.UseComputeShader;
            Config.UseVisibleCache = config.UseVisbileCache;         
            Config.ComputePerframe = config.ComputePerframe;
            Config.PerframeExecCount = config.PerframeExecCount;
            //Config.IsBatchMode = UnityEditorInternal.InternalEditorUtility.inBatchMode;

            Debug.LogFormat("batch mode {0} Use Compute Shader {1} Use Visible Cache {2}  ComputePerframe {3} PerframeExecCount {4} CellSize {5} MinHeight {6} MaxHeight {7} MergeObjectId {8} MergeCell {9}", 
                Config.IsBatchMode,
                Config.UseComputeShader, Config.UseVisibleCache,                
                Config.ComputePerframe, Config.PerframeExecCount,
                Config.CellSize, Config.MinPlayAreaHeight, Config.MaxPlayAreaHeight,
                Config.mergeObjectID, Config.mergeCell);
        }

        private static void PrintSystemInfo()
        {
            Debug.LogFormat("batch mode ProcessorCount {0}, Total Physics Memory {1} mb, Graphics Device Name {2}, Graphics Memory Size {3} mb, Graphics Shader Level {4}",
                SystemInfo.processorCount, SystemInfo.systemMemorySize, SystemInfo.graphicsDeviceName, SystemInfo.graphicsMemorySize, SystemInfo.graphicsShaderLevel);
        }

        private static void PrintArgs(int argNum)
        {
            for (int i = 1; i <= argNum; ++i)
            {
                Debug.LogFormat("batch mode Args {0}, Value {1}", i, System.Environment.GetCommandLineArgs()[i]);
            }
        }
        private static void ExitOnBatchMode()
        {
            if (UnityEditorInternal.InternalEditorUtility.inBatchMode)
            {
                Debug.LogFormat("batch mode Exit Editor Application On Batch Mode.");
                EditorApplication.Exit(0);
            }
        }

#endif
    }
}
