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
    
    internal class OCMapConfig
    {
        public string MapName;
        public bool IsStreamScene;
        public string SceneAssetPath;
        public string SceneNamePattern;
        public string TemporaryContainer;
        public int TileDimension;
        public bool UseComputeShader;
        public bool UseVisbileCache;
        public bool ComputePerframe;
        public int PerframeExecCount;

        public HashSet<Index> Tiles;

        public override string ToString()
        {
            var str = String.Format(
                "MapName {0}, Stream {1}, AssetPath {2}, SceneNamePattern {3} TempContainer {4} TileDim {5} ComputeShader {6}",
                MapName, IsStreamScene, SceneAssetPath, SceneNamePattern, TemporaryContainer, TileDimension, UseComputeShader);


            if (Tiles == null)
            {
                str += " No Tiles";
            }
            else
            {
                str += " Tiles: ";
                foreach (var tile in Tiles)
                {
                    str += String.Format("[{0}, {1}]", tile.x, tile.y);
                }
            }

            return str;
        }

        public HashSet<Index> GetBakeTiles()
        {
            var tiles = Tiles;
            if (tiles == null)
            {
                //bake all tiles if there is no any tile specified to bake
                tiles = new HashSet<Index>();
                var dimension = TileDimension;
                for (int x = 0; x < dimension; ++x)
                {
                    for (int y = 0; y < dimension; ++y)
                    {
                        tiles.Add(new Index(x, y));
                    }
                }
            }

            return tiles;
        }

        public string GetSceneNameOf(int x, int y)
        {
            if (IsStreamScene)
            {
                //return MultiScene.GetSceneNameOfPattern(SceneNamePattern, x, y);
            }

            return SceneNamePattern;
        }

        public string GetOCDataFilePath()
        {
            return System.IO.Path.Combine(TemporaryContainer, GetOCDataFileName());
        }

        public string GetOCDataFileName()
        {
            string fileName;
            if (IsStreamScene)
            {
                fileName = MultiScene.GetOCDataFileName(SceneNamePattern);
            }
            else
            {
                fileName = SingleScene.GetOCDataFileName(SceneNamePattern);
            }

            return fileName;
        }
    }

    public partial class OCGenerator
    {

#if UNITY_EDITOR
        public static void GenerateOCGenMapConfigFile()
        {
            var mapName = System.Environment.GetCommandLineArgs()[1];
            var bakeForTile = bool.Parse(System.Environment.GetCommandLineArgs()[2]);
            var processorNum = int.Parse(System.Environment.GetCommandLineArgs()[3]);
            PrintArgs(2);

            GenerateOCGenMapConfigFile(mapName, bakeForTile, processorNum);
        }

        public static void InitOCGeneration()
        {
            PrintSystemInfo();

            var mapName = System.Environment.GetCommandLineArgs()[1];
            var tileX = int.Parse(System.Environment.GetCommandLineArgs()[2]);
            var tileY = int.Parse(System.Environment.GetCommandLineArgs()[3]);
            if (!OpenAllScenes(mapName, tileX, tileY))
                return;

            ClearLightmappingData(mapName, tileX, tileY);
            GenerateAllSceneRenderableObjectID();
        }

        public static void GenerateOCData()
        {
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

        public static void GenerateOCDataForTile()
        {
            var projectPath = System.Environment.GetCommandLineArgs()[1];
            var index = int.Parse(System.Environment.GetCommandLineArgs()[2]);
            var processorNum = int.Parse(System.Environment.GetCommandLineArgs()[3]);
            var x = int.Parse(System.Environment.GetCommandLineArgs()[4]);
            var y = int.Parse(System.Environment.GetCommandLineArgs()[5]);
            PrintArgs(5);

            //SetTestData();

            var config = LoadOCMapConfig(projectPath, 0);
            if (config == null)
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
                //var contextIter = StreamTileSceneContextGenerator(config, x, y, index, processorNum);
                //var contextManager = new OCBakeContextManager(contextIter);
                //contextManager.Bake();
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

        public static void MergeOCDataForStreamScene()
        {
            var projectPath = System.Environment.GetCommandLineArgs()[1];
            PrintArgs(1);

            var config = LoadOCMapConfig(projectPath, 0);
            if (config == null)
            {
                Debug.LogErrorFormat("Can not get oc map config for stream scene in oc data mergence, path {0} index {1}", projectPath, 0);
                return;
            }

            if (config.IsStreamScene)
                MergeStreamSceneOCData(config.SceneAssetPath, config.SceneNamePattern, config.TemporaryContainer, config.TileDimension);
        }

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

        private static void ExitOnBatchMode()
        {
            if (UnityEditorInternal.InternalEditorUtility.inBatchMode)
            {
                Debug.LogFormat("Exit Editor Application On Batch Mode.");
                EditorApplication.Exit(0);
            }
        }





        private static void GenerateOCGenMapConfigFile(string mapName, bool bakeForTile, int processorNum)
        {
            var config = GetMapConfig(mapName);
            if (config == null)
            {
                Debug.LogErrorFormat("Can not found oc map config item for map {0}", mapName);
                return;
            }

            GenerateOCGenMapConfigFile(config, bakeForTile, processorNum);
        }

        private static OCMapConfig GetMapConfig(string mapName)
        {
            var filePath = "Assets/Assets/template/OCGenMapConfig.xml";
            if (!File.Exists(filePath))
            {
                var otherFilePath = "Assets/Assets/CoreRes/template/OCGenMapConfig.xml";
                if (!File.Exists(otherFilePath))
                {
                    Debug.LogErrorFormat("Can not found config file: \"OCGenMapConfig.xml\" from path {0} or {1}", filePath, otherFilePath);
                    return null;
                }

                filePath = otherFilePath;
            }


            var config = new OCMapConfig();
            config.UseComputeShader = true; //use compute shader in default
            config.MapName = mapName;
            var doc = new XmlDocument();
            doc.Load(filePath);

            var root = doc.DocumentElement;
            var mapNodes = root.SelectNodes("/root/Map");

            foreach (XmlNode node in mapNodes)
            {
                var nameNode = node.SelectSingleNode("MapName");
                var name = nameNode.InnerText;
                if (mapName.Equals(name))
                {
                    ParseOCMapConfig(node, config);
                    return config;
                }
            }

            return null;
        }

        private static void GenerateOCGenMapConfigFile(OCMapConfig config, bool bakeForTile, int processorNum)
        {
            DeleteOCMapConfigFile(".\\Assets");
            var bakeTiles = config.GetBakeTiles();
            CreateOCGenMapConfigFiles(config, bakeTiles, bakeForTile, processorNum);
        }

        private static void DeleteOCMapConfigFile(string path)
        {
            //delete origin oc generation files
            var configFiles = Directory.GetFiles(path, "OCGenMapConfig_*.xml");
            foreach (var file in configFiles)
            {
                File.Delete(file);
            }
        }

        private static void CreateOCGenMapConfigFiles(OCMapConfig config, HashSet<Index> bakeTiles, bool bakeForTile, int processorNum)
        {
            var tileCount = bakeTiles.Count;
            processorNum = processorNum > tileCount ? tileCount : processorNum;
            var perCountArray = new int[processorNum];
            for (int i = 0; i < tileCount; ++i)
            {
                perCountArray[i % processorNum] += 1;
            }

            var bakeTileList = bakeTiles.ToList();
            var startTile = 0;
            for (int index = 0; index < processorNum; ++index)
            {
                Debug.LogFormat("Baking Tile Count for Processor {0} is {1}", index, perCountArray[index]);
                if (bakeForTile)
                {
                    CreateOCGenMapConfigFile(".\\Assets", index, config, bakeTileList, 0, bakeTileList.Count);
                }
                else
                {
                    CreateOCGenMapConfigFile(".\\Assets", index, config, bakeTileList, startTile, perCountArray[index]);
                    startTile += perCountArray[index];
                }
               
            }
        }

        private static void CreateOCGenMapConfigFile(string path, int index, OCMapConfig config, List<Index> bakeTiles, int startTile, int tileCount)
        {
            var fileName = String.Format("OCGenMapConfig_{0}.xml", index);
            var filePath = Path.Combine(path, fileName);
            
            var writer = new XmlTextWriter(filePath, Encoding.UTF8);
            writer.WriteStartDocument();
            writer.WriteStartElement("root");
            WriteXmlNode(writer, "MapName", config.MapName);
            WriteXmlNode(writer, "StreamScene", config.IsStreamScene);
            WriteXmlNode(writer, "UseComputeShader", config.UseComputeShader);
            WriteXmlNode(writer, "UseVisbileCache", config.UseVisbileCache);
            WriteXmlNode(writer, "ComputePerframe", config.ComputePerframe);
            WriteXmlNode(writer, "PerframeExecCount", config.PerframeExecCount);
            WriteXmlNode(writer, "SceneAssetPath", config.SceneAssetPath);
            WriteXmlNode(writer, "SceneNamePattern", config.SceneNamePattern);
            WriteXmlNode(writer, "TemporaryContainer", config.TemporaryContainer);
            WriteXmlNode(writer, "TileDimension", config.TileDimension);
            WriteXmlTilesNode(writer, bakeTiles, startTile, tileCount);
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Close();
        }

        private static void ParseOCMapConfig(XmlNode node, OCMapConfig config)
        {
            config.IsStreamScene = ParseXmlNode<bool>(node, "StreamScene");
            config.UseComputeShader = ParseXmlNode(node, "UseComputeShader", true);
            config.UseVisbileCache = ParseXmlNode(node, "UseVisbileCache", true);
            config.ComputePerframe = ParseXmlNode(node, "ComputePerframe", false);
            config.PerframeExecCount = ParseXmlNode(node, "PerframeExecCount", 3000);
            config.SceneAssetPath = ParseXmlNode<string>(node, "SceneAssetPath");
            config.SceneNamePattern = ParseXmlNode<string>(node, "SceneNamePattern");
            config.TemporaryContainer = ParseXmlNode<string>(node, "TemporaryContainer");
            config.TileDimension = ParseXmlNode(node, "TileDimension", 1);
            config.Tiles = ParseTiles(node);
        }

        private static T ParseXmlNode<T>(XmlNode parent, string nodeName, T defaultValue = default(T))
        {
            var node = parent.SelectSingleNode(nodeName);
            if (node == null)
                return defaultValue;

            var converter =
                TypeDescriptor.GetConverter(typeof(T));

            return (T) converter.ConvertFromString(node.InnerText);
        }

        private static HashSet<Index> ParseTiles(XmlNode parent)
        {
            var tilesNode = parent.SelectSingleNode("Tiles");
            if (tilesNode == null)
                return null;

            var tileNodeList = tilesNode.SelectNodes("Tile");
            if (tileNodeList.Count == 0)
                return null;

            var tileIndices = new HashSet<Index>();
            foreach (XmlNode tileNode in tileNodeList)
            {
                var x = int.Parse(tileNode.Attributes["X"].Value);
                var y = int.Parse(tileNode.Attributes["Y"].Value);

                tileIndices.Add(new Index(x, y));
            }

            return tileIndices;
        }

        private static void WriteXmlNode<T>(XmlTextWriter writer, string nodeName, T value)
        {
            writer.WriteElementString(nodeName, String.Format("{0}", value));
        }

        private static void WriteXmlTilesNode(XmlTextWriter writer, List<Index> tiles, int startTile, int tileCount)
        {
            writer.WriteStartElement("Tiles");
            for (int i = startTile; i < startTile + tileCount; ++i)
            {
                var index = tiles[i];
                writer.WriteStartElement("Tile");
                writer.WriteAttributeString("X", index.x.ToString());
                writer.WriteAttributeString("Y", index.y.ToString());
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        private static OCMapConfig LoadOCMapConfig(string projectAssetPath, int index)
        {
            var filePath = Path.Combine(projectAssetPath, String.Format("OCGenMapConfig_{0}.xml", index));
            
            if (!File.Exists(filePath))
            {
                Debug.LogErrorFormat("oc gen map config file {0} does not exist, path {1} index {2}", filePath, projectAssetPath, index);
                return null;
            }

            var doc = new XmlDocument();
            doc.Load(filePath);
            var root = doc.DocumentElement;
            
            var config = new OCMapConfig();
            config.MapName = ParseXmlNode<string>(root, "MapName");
            ParseOCMapConfig(root, config);
            return config;
        }

        private static void SetTestData()
        {
            Config.CustomVolume = true;
            Config.CustomVolumeCenter = new Vector3(100, 100, 100);
            Config.CustomVolumeSize = new Vector3(10, 10, 10);
        }

        public static void ClearLightmappingData(string mapName, int tileX, int tileY)
        {
            Debug.Log("Clear Lighting Data Asset ...");
            ClearLightmappingData();
            Debug.Log("Clear Lighting Data Asset Successfully!");
        }

        private static bool OpenAllScenes(string mapName, int tileX, int tileY)
        {
            //close existed scenes 
            ClearScenes();

            //open new scenes
            var config = GetMapConfig(mapName);
            if (config == null)
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

                        sceneNames.Add(String.Format("{0}/{1}.unity", config.SceneAssetPath,
                            String.Format(config.SceneNamePattern, x, y)));
                    }
                }
            }
            else
            {
                sceneNames.Add(String.Format("{0}/{1}.unity", config.SceneAssetPath, config.SceneNamePattern));
            }

            foreach (var sceneName in sceneNames)
            {
                if (!IsSceneOpened(sceneName))
                {
                    Debug.LogFormat("Open Scene {0}...", sceneName);
                    EditorSceneManager.OpenScene(sceneName, OpenSceneMode.Additive);
                }
            }
            return true;
        }

        public static void ClearLightmappingData()
        {
            Lightmapping.ClearLightingDataAsset();
            Lightmapping.Clear();
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

                var singleScene = new SingleScene(scene.path, scene.name);
                singleScene.GeneraterRenderableObjectID();
                singleScene.Save();
            }

        }

        private static void GenerateOCDataForFixedScene(OCMapConfig config)
        {
            ConfigGenerator(config);
            if (!IsSceneOpened(config.SceneNamePattern))
            {
                Debug.LogFormat("Open Scene {0}", config.SceneNamePattern);
                EditorSceneManager.OpenScene(String.Format("{0}/{1}.unity", config.SceneAssetPath, config.SceneNamePattern));
            }


            var scene = new SingleScene(config.SceneAssetPath, config.SceneNamePattern);
            // var contextManager = new OCBakeContextManager(scene,
            //     () =>
            //     {
            //         scene.CopyOCDataTo(config.TemporaryContainer);
            //         GenerateSceneOCDiffPatch(config.SceneNamePattern, config.TemporaryContainer);
            //     });

            // contextManager.Bake();
        }

        private static readonly string OCPatchFileSuffix = "_oc_patch.txt";
        private static void GenerateSceneOCDiffPatch(string sceneName, string temporaryContainer)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            var diffFilePath = scene.path.Replace(".unity", OCPatchFileSuffix);
            SaveSceneOCDiffPatch(scene, diffFilePath);
            //copy to temporary container
            var destFilePath = Path.Combine(temporaryContainer, String.Format("{0}{1}", sceneName, OCPatchFileSuffix));
            CopyTo(diffFilePath, destFilePath);
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
                            GetObjectPath(trans), comp.GUID,
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

        private static void CopyTo(string sourceFilePath, string destFilePath)
        {
            Debug.LogFormat("Copy file from {0} to {1}", sourceFilePath, destFilePath);

            if (!File.Exists(sourceFilePath))
            {
                Debug.LogErrorFormat("The file does not exist {0}", sourceFilePath);
                throw new Exception(String.Format("Source File {0} does not exist, can not copy to {1}", sourceFilePath, destFilePath));
            }

            if (File.Exists(destFilePath))
            {
                File.Delete(destFilePath);
            }

            File.Copy(sourceFilePath, destFilePath);
        }

        private static string GetObjectPath(Transform trans)
        {
            var path = trans.name;
            while (trans.parent != null)
            {
                trans = trans.parent;
                path = string.Format("{0}/{1}", trans.name, path);
            }

            return path;
        }

        private static void GenerateOCDataForStreamScene(OCMapConfig config)
        {
            var tiles = config.Tiles;
            if (tiles != null)
            {
                // var contextIter = StreamFullSceneContextGenerator(config);
                // var contextManager = new OCBakeContextManager(contextIter);
                // contextManager.Bake();
            }
            else
            {
                Debug.LogErrorFormat("Can not get bake tiles for map {0}", config.MapName);
                ExitOnBatchMode();
            }
        }

        // private static IEnumerator<OCBakeContext> StreamFullSceneContextGenerator(OCMapConfig config)
        // {
        //     var tiles = config.Tiles;
        //     foreach (var tile in tiles)
        //     {
        //         Debug.LogFormat("Generate Stream Scene OC Data {0} {1}", tile.X, tile.Y);
        //         var contextIter = StreamOCBakeContextGenerator(config, tile.X, tile.Y);

        //         while (contextIter.MoveNext())
        //         {
        //             yield return contextIter.Current;
        //         }
        //     }
        // }

        // private static IEnumerator<OCBakeContext> StreamTileSceneContextGenerator(OCMapConfig config, int x, int y, int index, int processorNum)
        // {
        //     var startx = x <= 0 ? x : x - 1;
        //     var endx = x >= config.TileDimension - 1 ? x : x + 1;
        //     var starty = y <= 0 ? y : y - 1;
        //     var endy = y >= config.TileDimension - 1 ? y : y + 1;

        //     var bakedTiles = config.GetBakeTiles();
        //     int count = 0;
        //     for (int xi = startx; xi <= endx; ++xi)
        //     {
        //         for (int yi = starty; yi <= endy; ++yi)
        //         {
        //             if (bakedTiles.Contains(new TileIndex(xi, yi)))
        //             {
        //                 count++;
        //             }
        //         }
        //     }

        //     Debug.LogFormat("Total Bake Tile Count {0} Processor num {1}", count, processorNum);

        //     var perCountArray = new int[processorNum];
        //     for (int i = 0; i < count; ++i)
        //     {
        //         perCountArray[i % processorNum] += 1;
        //     }

        //     var startTile = 0;
        //     var endTile = 0;
        //     for (int i = 0; i < index; ++i)
        //     {
        //         startTile += perCountArray[i];
        //     }
        //     endTile = startTile + perCountArray[index];

        //     var curTile = 0;
        //     for (int xi = startx; xi <= endx; ++xi)
        //     {
        //         for (int yi = starty; yi <= endy; ++yi)
        //         {
        //             var needToBake = bakedTiles.Contains(new TileIndex(xi, yi));
        //             if (needToBake)
        //             {
        //                 if (curTile >= startTile && curTile < endTile)
        //                 {
        //                     var contextIter = StreamOCBakeContextGenerator(config, xi, yi);
        //                     while (contextIter.MoveNext())
        //                     {
        //                         yield return contextIter.Current;
        //                     }
        //                 }

        //                 curTile++;
        //             }
        //         }
        //     }
        // }

        // private static IEnumerator<OCBakeContext> StreamOCBakeContextGenerator(OCMapConfig config, int x, int y)
        // {
        //     ConfigGenerator(config);
        //     var path = config.SceneAssetPath;
        //     var sceneNamePattern = config.SceneNamePattern;
        //     var tileDimension = config.TileDimension;
        //     var temporaryContainer = config.TemporaryContainer;

        //     OpenStreamScene(path, sceneNamePattern, x, y, tileDimension);
        //     var multiScene = new MultiScene(path, sceneNamePattern, tileDimension, 1000);
        //     Debug.LogFormat("Bake Stream Scene Tile {0} {1} ...", x, y);
        //     var contextIter = multiScene.GetBakeContexts(x, y, temporaryContainer);

        //     while (contextIter.MoveNext())
        //     {
        //         var context = contextIter.Current;
        //         if (context.TileIndex.Equals(new Index(x, y)))
        //         {
        //             context.OnSuccessCallback += () =>
        //             {
        //                 var sceneName = multiScene.GetSceneNameOf(x, y);
        //                 if (!IsSceneOpened(sceneName))
        //                 {
        //                     throw new Exception(String.Format("Baked Scene {0} is not loaded", sceneName));
        //                 }
        //                 GenerateSceneOCDiffPatch(sceneName, temporaryContainer);

        //                 Debug.LogFormat("Bake Stream Scene Tile {0} {1} Successfully", x, y);
        //             };
        //         }

        //         yield return context;
        //     }

        // }

        private static void OpenStreamScene(string path, string sceneNamePattern, int x, int y, int tileDimension)
        {
            //close previous scene
            ClearScenes();

            bool additive = false;
            int startx = x >= 1 ? x - 1 : 0;
            int endx = (x < 0 || x >= tileDimension - 1) ? tileDimension - 1 : x + 1;
            int starty = y >= 1 ? y - 1 : 0;
            int endy = (y < 0 || y >= tileDimension - 1) ? tileDimension - 1 : y + 1;

            for (int xi = startx; xi <= endx; ++xi)
            {
                for (int yi = starty; yi <= endy; ++yi)
                {
                    var sceneName = String.Format(sceneNamePattern, xi, yi);
                    if (!IsSceneOpened(sceneName))
                    {
                        Debug.LogFormat("Open Scene {0}", sceneName);
                        EditorSceneManager.OpenScene(String.Format("{0}/{1}.unity", path, sceneName), additive ? OpenSceneMode.Additive : OpenSceneMode.Single);
                    }
                    additive = true;
                }
            }
        }

        private static void ClearScenes()
        {
            if (!IsSceneOpened(String.Empty))
            {
                var emptyScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

                var roots = emptyScene.GetRootGameObjects();
                foreach (var root in roots)
                {
                    GameObject.DestroyImmediate(root);
                }
            }

            while (SceneManager.sceneCount > 1)
            {
                for (int i = 0; i < SceneManager.sceneCount; ++i)
                {
                    var openedScene = SceneManager.GetSceneAt(i);
                    if (openedScene.name.Equals(String.Empty))
                    {
                        continue;
                    }

                    Debug.LogFormat("Cloese Scene {0}", openedScene.name);
                    EditorSceneManager.CloseScene(openedScene, true);
                    break;
                }
            }

            Debug.LogFormat("Remove unrelated scene left scene count {0}", SceneManager.sceneCount);
        }

        private static void InitScene(string sceneName, bool isStreamScene)
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

        public static void MergeStreamSceneOCData(string path, string sceneNamePattern, string temporaryContainer, int tileDimension)
        {
            var scene = new MultiScene(path, sceneNamePattern, tileDimension, 1000);
            scene.MergeOCData(temporaryContainer);
            scene.CopyOCDataTo(temporaryContainer);
        }

        private static bool ApplyOCDiffPatch(string mapName)
        {
            var config = GetMapConfig(mapName);
            if (config == null)
            {
                return false;
            }

            var temporaryContainer = config.TemporaryContainer;
            var success = false;
            if (config.IsStreamScene)
            {
                var tiles = config.GetBakeTiles();
                foreach (var tile in tiles)
                {
                    var sceneName = config.GetSceneNameOf(tile.x, tile.y);
                    var diffFilePath = Path.Combine(temporaryContainer, String.Format("{0}{1}", sceneName, OCPatchFileSuffix));
                    success = ApplyOCDiffPatch(config.SceneAssetPath, sceneName, diffFilePath, true);
                    if (!success)
                        break;
                }
            }
            else
            {
                var diffFilePath = Path.Combine(temporaryContainer, String.Format("{0}{1}", config.SceneNamePattern, OCPatchFileSuffix));
                success = ApplyOCDiffPatch(config.SceneAssetPath, config.SceneNamePattern, diffFilePath, false);
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

            ClearScenes();

            Debug.LogFormat("Apply OC Diff Patch For Scene {0}...", sceneName);
            var scenePath = String.Format("{0}/{1}.unity", path, sceneName);
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            InitScene(sceneName, isStreamScene);

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
            var config = GetMapConfig(mapName);
            if (config != null)
            {
                var ocDataFilePath = config.GetOCDataFilePath();
                var destDirectory = Path.Combine(projectPath, config.SceneAssetPath);
                var destFilePath = Path.Combine(destDirectory, config.GetOCDataFileName());

                if (File.Exists(destFilePath))
                {
                    File.Delete(destFilePath);    
                }

                if (File.Exists(ocDataFilePath))
                {
                    File.Copy(ocDataFilePath, destFilePath);

                    var assetFilePath = Path.Combine(config.SceneAssetPath, config.GetOCDataFileName());
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

        private static bool IsSceneOpened(string sceneName)
        {
            return SceneManager.GetSceneByName(sceneName).isLoaded;
        }

        private static void ConfigGenerator(OCMapConfig config)
        {
            ComputeShader computeShader = null;
            var useComputeShader = config.UseComputeShader;
            if (useComputeShader)
            {
                computeShader = GetOCVisComputeShader();

                if (computeShader == null)
                {
                    useComputeShader = false;
                }
            }

            Config.UseComputeShader = useComputeShader;
            Config.UseVisibleCache = config.UseVisbileCache;
            Config.SavePerCell = true;
            Config.ClearOnSave = true;
            Config.ComputePerframe = config.ComputePerframe;
            Config.PerframeExecCount = config.PerframeExecCount;
            Config.IsBatchMode = UnityEditorInternal.InternalEditorUtility.inBatchMode;

            Debug.LogFormat("OC Configuration: Batch Mode {0} Use Compute Shader {1} Use Visible Cache {2} SavePerCell {3} ClearOnSave {4} ComputePerframe {5} PerframeExecCount {6} CellSize {7} MinHeight {8} MaxHeight {9} MergeObjectId {10} MergeCell {11} Clear Light Probes {12}", 
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
            Debug.LogFormat("ProcessorCount {0}, Total Physics Memory {1} mb, Graphics Device Name {2}, Graphics Memory Size {3} mb, Graphics Shader Level {4}",
                SystemInfo.processorCount, SystemInfo.systemMemorySize, SystemInfo.graphicsDeviceName, SystemInfo.graphicsMemorySize, SystemInfo.graphicsShaderLevel);
        }

        private static void PrintArgs(int argNum)
        {
            for (int i = 1; i <= argNum; ++i)
            {
                Debug.LogFormat("Args {0}, Value {1}", i, System.Environment.GetCommandLineArgs()[i]);
            }
        }

#endif
    }
}
