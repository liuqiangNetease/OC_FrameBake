#if UNITY_EDITOR
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

        public static void SaveDiffFile(string sceneName, string tempPath)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            var diffFilePath = scene.path.Replace(".unity", Config.OCPatchFileSuffix);

            using (BinaryWriter w = new BinaryWriter(File.Open(diffFilePath, FileMode.Create)))
            {
                Writer writer = new Writer(w);

                var rootObjects = scene.GetRootGameObjects();
                foreach (var root in rootObjects)
                {
                    var compList = root.GetComponentsInChildren<GameObjectID>();

                    writer.Write(compList.Length);

                    foreach (var comp in compList)
                    {
                        //guid
                        writer.Write(comp.GUID);

                        //transform
                        var trans = comp.transform;
                        var transPath = Util.GetObjectPath(trans);
                        writer.Write(transPath);                       
                        writer.Write(trans.position);
                        writer.Write(trans.rotation);
                        writer.Write(trans.lossyScale);
                    
                        //mesh bounds
                        var meshFilter = comp.GetComponent<MeshFilter>();                    
                        if (meshFilter != null && meshFilter.sharedMesh != null)
                        {
                            writer.Write( meshFilter.sharedMesh.bounds);
                        }
                    }
                }
            }
            //copy to tempPath
            var destFilePath = Path.Combine(tempPath, String.Format("{0}{1}", sceneName, Config.OCPatchFileSuffix));
            Util.CopyTo(diffFilePath, destFilePath);
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

            using (BinaryReader reader = new BinaryReader(File.Open(diffFilePath, FileMode.Open)))
            {
                Reader r = new Reader(reader);

                var count = r.ReadInt();

                for(int i=0; i< count; i++)
                {
                    //guid
                    var guid = r.ReadInt();
                    //transform
                    var transPath = r.ReadString();
                    var pos = r.ReadVector3();
                    var rot = r.ReadQuaternion();
                    var scale = r.ReadVector3();
                    //mesh bounds
                    var bounds = r.ReadBounds();

                    var go = GameObject.Find(transPath);
                    if (go == null)
                    {
                        Debug.LogErrorFormat("Can not find gameobject of path {0}", transPath);
                        success = false;
                        break;
                    }

                    var position = go.transform.position;
                    var rotation = go.transform.rotation;
                    var lossScale = go.transform.lossyScale;

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

                    var meshBounds = mesh.bounds;

                    //
                    if (!IsApproximatelySame(pos, position))
                    {
                        success = false;
                        break;
                    }

                    if (!IsApproximatelySame(rot, rotation))
                    {
                        success = false;
                        break;
                    }

                    if (!IsApproximatelySame(scale, lossScale))
                    {
                        success = false;
                        break;
                    }

                    if (!IsApproximatelySame(bounds.center, meshBounds.center))
                    {
                        success = false;
                        break;
                    }

                    if (!IsApproximatelySame(bounds.extents, meshBounds.extents))
                    {
                        success = false;
                        break;
                    }

                    var idComp = go.GetComponent<GameObjectID>();
                    if (idComp == null)
                    {
                        go.AddComponent<GameObjectID>();
                    }
                    idComp.GUID = guid;

                    if (isStreamScene)
                    {
                        SetMultiTagRenerId(go.transform, guid);
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

                    EditorUtility.SetDirty(tag);
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
       
    }
}
#endif
