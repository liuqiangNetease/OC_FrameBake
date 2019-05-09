
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

#if UNITY_EDITOR
        //jenkins interface 产生配置文件
        public static void GenerateOCGenMapConfigFile()
        {
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
                var bakeTiles = config.GetBakeIndices();
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
            PrintSystemInfo();

            /*var mapName = System.Environment.GetCommandLineArgs()[1];
            var tileX = int.Parse(System.Environment.GetCommandLineArgs()[2]);
            var tileY = int.Parse(System.Environment.GetCommandLineArgs()[3]);
            if (!OpenAllScenes(mapName, tileX, tileY))
                return;*/

            //ClearLightmappingData();
            //GenerateAllSceneRenderableObjectID();
        }

        /*private static bool OpenAllScenes(string mapName, int tileX, int tileY)
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

                        sceneNames.Add(String.Format("{0}/{1}.unity", config.GetSceneAssetPath,
                            String.Format(config.SceneNamePattern, x, y)));
                    }
                }
            }
            else
            {
                sceneNames.Add(String.Format("{0}/{1}.unity", config.GetSceneAssetPath, config.SceneNamePattern));
            }

            foreach (var sceneName in sceneNames)
            {
                if (!Util.IsSceneOpened(sceneName))
                {
                    Debug.LogFormat("Open Scene {0}...", sceneName);
                    EditorSceneManager.OpenScene(sceneName, OpenSceneMode.Additive);
                }
            }
            return true;
        }*/
        public static void ClearLightmappingData()
        {
            Debug.Log("Clear Lighting Data Asset ...");
            Lightmapping.ClearLightingDataAsset();
            Lightmapping.Clear();
            Debug.Log("Clear Lighting Data Asset Successfully!");
        }

        /*private static void GenerateAllSceneRenderableObjectID()
        {
            var sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name.Equals(String.Empty))
                {
                    continue;
                }

                var singleScene = new SingleScene(scene.path, scene.name, Index.InValidIndex);
                singleScene.GeneraterRenderableObjectID();
                singleScene.Save();
            }
        }*/

        //------------------------------
        //jenkins interface computer pvs data for json config
        public static void GenerateOCData()
        {
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


        //------------------------------------
        #region DiffPatch
        
        public static void ApplyOCData()
        {
            var mapName = System.Environment.GetCommandLineArgs()[1];
            var projectPath = System.Environment.GetCommandLineArgs()[2];
            PrintArgs(2);

            ApplyOCData(mapName, projectPath);
        }

        public static void ApplyOCData(string mapName, string projectPath)
        {
            if (ApplyOCDiffPatch(mapName))
            {
                CopyOCData(mapName, projectPath);
            }
            else
            {
                Debug.LogWarningFormat("There is something error to apply oc data on map {0}", mapName);
            }
        }
        
        public static void GenerateSceneOCDiffPatch(string sceneName, string temporaryContainer)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            var diffFilePath = scene.path.Replace(".unity", Config.OCPatchFileSuffix);
            SaveSceneOCDiffPatch(scene, diffFilePath);
            //copy to temporary container
            var destFilePath = Path.Combine(temporaryContainer, String.Format("{0}{1}", sceneName, Config.OCPatchFileSuffix));
            Util.CopyTo(diffFilePath, destFilePath);
        }

        private static void SaveSceneOCDiffPatch(Scene scene, string diffFilePath)
        {
            using (var diffFile = File.Open(diffFilePath, FileMode.Create))
            {
                var rootObjects = scene.GetRootGameObjects();
                foreach (var root in rootObjects)
                {
                    var compList = root.GetComponentsInChildren<GameObjectID>();
                    foreach (var comp in compList)
                    {
                        var trans = comp.transform;
                        var position = trans.position;
                        var rotation = trans.rotation;
                        var scale = trans.lossyScale;
                        var meshFilter = comp.GetComponent<MeshFilter>();
                        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
                        if (meshFilter != null && meshFilter.sharedMesh != null)
                        {
                            bounds = meshFilter.sharedMesh.bounds;
                        }

                        var str = String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17}\n",
                            Util.GetObjectPath(trans), comp.GUID,
                            position.x, position.y, position.z, //2~4
                            rotation.x, rotation.y, rotation.z, rotation.w,//5~8
                            scale.x, scale.y, scale.z,//9~11
                            bounds.center.x, bounds.center.y, bounds.center.z,//12~14
                            bounds.extents.x, bounds.extents.y, bounds.extents.z);//15~17
                        var bytes = Encoding.UTF8.GetBytes(str);
                        diffFile.Write(bytes, 0, bytes.Length);
                    }
                }
            }
            
        }
        private static void ResetSceneMultiTag(string sceneName, bool isStreamScene)
        {
            if (isStreamScene)
            {
                var scene = SceneManager.GetSceneByName(sceneName);
                var roots = scene.GetRootGameObjects();
                foreach (var root in roots)
                {
                    var tags = root.GetComponentsInChildren<MultiTagBase>();
                    foreach (var tag in tags)
                    {
                        tag.renderId = MultiTagBase.InvalidRenderId;
#if UNITY_EDITOR
                        EditorUtility.SetDirty(tag);
#endif
                    }
                }
            }
        }
        private static bool ApplyOCDiffPatch(string mapName)
        {
            var config = GetSceneConfig(mapName);
            if (string.IsNullOrEmpty(config.MapName))
            {
                return false;
            }

            var temporaryContainer = config.TemporaryContainer;
            var success = false;
            if (config.IsStreamScene)
            {
                var tiles = config.GetBakeIndices();
                foreach (var tile in tiles)
                {
                    var sceneName = config.GetSceneNameOf(tile.x, tile.y);
                    var diffFilePath = Path.Combine(temporaryContainer, String.Format("{0}{1}", sceneName, Config.OCPatchFileSuffix));
                    success = ApplyOCDiffPatch(config.GetSceneAssetPath(), sceneName, diffFilePath, true);
                    if (!success)
                        break;
                }
            }
            else
            {
                var diffFilePath = Path.Combine(temporaryContainer, String.Format("{0}{1}", config.SceneNamePattern, Config.OCPatchFileSuffix));
                success = ApplyOCDiffPatch(config.GetSceneAssetPath(), config.SceneNamePattern, diffFilePath, false);
            }
            
            return success;
        }

        private static bool ApplyOCDiffPatch(string path, string sceneName, string diffFilePath, bool isStreamScene)
        {
            if (!File.Exists(diffFilePath))
            {
                Debug.LogErrorFormat("Diff OC Patch File {0} does not exist!", diffFilePath);
                return false;
            }

            Util.ClearScenes();

            Debug.LogFormat("Apply OC Diff Patch For Scene {0}...", sceneName);
            var scenePath = String.Format("{0}/{1}.unity", path, sceneName);
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            ResetSceneMultiTag(sceneName, isStreamScene);

            var success = true;
            using (var file = new StreamReader(diffFilePath))
            {
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    var data = line.Split(',');
                    var objectPath = data[0];
                    var id = ushort.Parse(data[1]);
                    var p = GetVector3From(data, 2);
                    var r = GetQuaternionFrom(data, 5);
                    var s = GetVector3From(data, 9);
                    var bc = GetVector3From(data, 12);
                    var be = GetVector3From(data, 15);

                    var go = GameObject.Find(objectPath);
                    if (go == null)
                    {
                        Debug.LogErrorFormat("Can not find gameobject of path {0}", objectPath);
                        success = false;
                        break;
                    }

                    var meshFilter = go.GetComponent<MeshFilter>();
                    if (meshFilter == null)
                    {
                        success = false;
                        break;
                    }

                    var mesh = meshFilter.sharedMesh;
                    if (mesh == null)
                    {
                        success = false;
                        break;
                    }

                    var bounds = mesh.bounds;

                    var transform = go.transform;
                    var position = transform.position;
                    var rotation = transform.rotation;
                    var scale = transform.lossyScale;

                    if (!IsApproximatelySame(p, position))
                    {
                        success = false;
                        break;
                    }

                    if (!IsApproximatelySame(r, rotation))
                    {
                        success = false;
                        break;
                    }

                    if (!IsApproximatelySame(s, scale))
                    {
                        success = false;
                        break;
                    }

                    if (!IsApproximatelySame(bounds.center, bc))
                    {
                        success = false;
                        break;
                    }

                    if (!IsApproximatelySame(bounds.extents, be))
                    {
                        success = false;
                        break;
                    }

                    var idComp = go.GetComponent<GameObjectID>();
                    if (idComp == null)
                    {
                        go.AddComponent<GameObjectID>();
                    }
                    idComp.GUID = id;

                    if (isStreamScene)
                    {
                        SetMultiTagRenerId(go.transform, id);
                    }
                }
            }

            if (success)
            {
                var scene = SceneManager.GetSceneByName(sceneName);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            else
            {
                Debug.LogWarningFormat("Apply OC Diff Patch Failed scene {0} diff patch file {1}", sceneName, diffFilePath);
            }

            Debug.LogFormat("Scene {0} OC Diff Patch Applied, Success {1}", sceneName, success);
            return success;
        }

        private static void SetMultiTagRenerId(Transform transform, int renderId)
        {
            var parent = transform;
            while (parent != null)
            {
                var tag = parent.GetComponent<MultiTagBase>();
                if (tag != null)
                {
                    if (tag.renderId != renderId && tag.renderId != MultiTagBase.InvalidRenderId)
                    {
                        Debug.LogWarningFormat("The tag id is inconsistent among children of gameobject {0}, previous id {1} current id {2}",
                            tag.name, tag.renderId, renderId);    
                    }

                    tag.renderId = renderId;

#if UNITY_EDITOR
                    EditorUtility.SetDirty(tag);
#endif
                    break;
                }

                parent = parent.parent;
            }
        }

        private static void CopyOCData(string mapName, string projectPath)
        {
            var config = GetSceneConfig(mapName);
            if (string.IsNullOrEmpty(config.MapName) == false)
            {
                var ocDataFilePath = config.GetOCDataFilePath();
                var destDirectory = Path.Combine(projectPath, config.GetSceneAssetPath());
                var destFilePath = Path.Combine(destDirectory, config.GetOCDataFileName());

                if (File.Exists(destFilePath))
                {
                    File.Delete(destFilePath);    
                }

                if (File.Exists(ocDataFilePath))
                {
                    File.Copy(ocDataFilePath, destFilePath);

                    var assetFilePath = Path.Combine(config.GetSceneAssetPath(), config.GetOCDataFileName());
                    AssetDatabase.ImportAsset(assetFilePath);
                    var importer = AssetImporter.GetAtPath(assetFilePath);
                    importer.SetAssetBundleNameAndVariant("OC", null);
                }
                else
                {
                    Debug.LogErrorFormat("Can not found oc data file {0}", ocDataFilePath);
                }
            }
        }

        private static Vector3 GetVector3From(string[] data, int start)
        {
            var x = float.Parse(data[start]);
            var y = float.Parse(data[start + 1]);
            var z = float.Parse(data[start + 2]);

            return new Vector3(x, y, z);
        }

        private static Quaternion GetQuaternionFrom(string[] data, int start)
        {
            var x = float.Parse(data[start]);
            var y = float.Parse(data[start + 1]);
            var z = float.Parse(data[start + 2]);
            var w = float.Parse(data[start + 4]);

            return new Quaternion(x, y, z, w);
        }

        private static bool IsApproximatelySame(Quaternion q0, Quaternion q1)
        {
            return IsApproximatelySame(q0.x, q1.x) &&
                   IsApproximatelySame(q0.y, q1.y) &&
                   IsApproximatelySame(q0.z, q1.z) &&
                   IsApproximatelySame(q0.w, q1.w);
        }

        private static bool IsApproximatelySame(Vector3 v0, Vector3 v1)
        {
            return IsApproximatelySame(v0.x, v1.x) &&
                   IsApproximatelySame(v0.y, v1.y) &&
                   IsApproximatelySame(v0.z, v1.z);
        }

        private static bool IsApproximatelySame(float f0, float f1)
        {
            return Math.Abs(f0 - f1) <= 1e-4f;
        }
        #endregion

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
                            Debug.LogErrorFormat("Can not found config file: \"OCScenesConfig.json\" from path {0} or {1}", filePath, otherFilePath);
                            return ret;
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
            Config.SavePerCell = true;
            Config.ClearOnSave = true;
            Config.ComputePerframe = config.ComputePerframe;
            Config.PerframeExecCount = config.PerframeExecCount;
            Config.IsBatchMode = UnityEditorInternal.InternalEditorUtility.inBatchMode;

            Debug.LogFormat("batch mode {0} Use Compute Shader {1} Use Visible Cache {2} SavePerCell {3} ClearOnSave {4} ComputePerframe {5} PerframeExecCount {6} CellSize {7} MinHeight {8} MaxHeight {9} MergeObjectId {10} MergeCell {11} Clear Light Probes {12}", 
                Config.IsBatchMode,
                Config.UseComputeShader, Config.UseVisibleCache, 
                Config.SavePerCell, Config.ClearOnSave,
                Config.ComputePerframe, Config.PerframeExecCount,
                Config.CellSize, Config.MinPlayAreaHeight, Config.MaxPlayAreaHeight,
                Config.mergeObjectID, Config.mergeCell,
                Config.ClearLightProbes);
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
