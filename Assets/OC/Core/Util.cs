#if UNITY_EDITOR

using ArtPlugins;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;


namespace OC
{
    public class Util
    {
        public static bool Progress(string strTitle, string strMessage, float fT)
        {
            return EditorUtility.DisplayCancelableProgressBar(strTitle, strMessage, fT);
        }
        public static bool IsLodMesh(MeshRenderer mesh)
        {
            bool ret = false;

            Transform parent = mesh.transform;
            while (parent != null)
            {
                LODGroup[] groups = parent.GetComponentsInChildren<LODGroup>();
                if (groups != null)
                {
                    for (int i = 0; i < groups.Length; i++)
                    {
                        var group = groups[i];
                        var lods = group.GetLODs();
                        for (int k = 0; k < lods.Length; k++)
                        {
                            var lod = lods[k];
                            for (int j = 0; j < lod.renderers.Length; j++)
                            {
                                MeshRenderer renderer = lod.renderers[j] as MeshRenderer;
                                if (mesh == renderer)
                                {
                                    ret = true;
                                    return ret;
                                }

                            }
                        }
                    }
                }
                parent = parent.parent;
            }
            return ret;
        }

        public static bool IsMultiTag(MeshRenderer mesh)
        {
            bool ret = false;
            Transform parent = mesh.transform;
            while (parent != null)
            {
                var tags = parent.GetComponentsInChildren<MultiTagBase>();
                if (tags != null)
                {
                    for (int i = 0; i < tags.Length; i++)
                    {
                        var tag = tags[i];
                        var renderers = tag.transform.GetComponentsInChildren<MeshRenderer>();
                        foreach (var render in renderers)
                        {
                            if (render == mesh)
                            {
                                ret = true;
                                return ret;
                            }
                        }
                    }
                }
                parent = parent.parent;
            }
            return ret;
        }

        public static bool IsValidOCRenderer(MeshRenderer renderer)
        {
            bool ret = true;

            if (renderer == null || !renderer.gameObject.activeSelf || !renderer.enabled || renderer.sharedMaterials.Length <= 0)
            {
                ret = false;
                return ret;
            }

            MeshFilter mf = renderer.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
            {
                ret = false;
                return ret;
            }

            ret = !ContainAnyTransparentOrCutOff(renderer);

            return ret;
        }

        public static bool TryAdd(MeshRenderer render, List<MeshRenderer> renderers)
        {
            bool ret = true;
            var com = render.GetComponent<GameObjectID>();
            if (com == null)
            {
                Debug.LogErrorFormat("GameObjectID com is null for renderer {0}!", render.name);
                return false;
            }

            if (com.GUID == GameObjectID.resetID)
            {
                renderers.Add(render);
                com.Flag();
            }
            else
            {
                ret = false;
            }

            return ret;
        }

        public static bool ContainAnyTransparentOrCutOff(Renderer mr)
        {
            bool ret = false;
            for (int j = 0; j < mr.sharedMaterials.Length; j++)
            {
                var mat = mr.sharedMaterials[j];
                if (mat == null)
                    continue;

                if (mat.shader.renderQueue >= (int)UnityEngine.Rendering.RenderQueue.AlphaTest)
                {
                    ret = true;
                    break;
                }
                if (mat.HasProperty("_Mode"))
                {
                    float mode = mat.GetFloat("_Mode");
                    if (mode == 0)
                    {
                        //不透明
                        ret = false;
                    }
                    else
                    {
                        ret = true;
                        break;
                    }
                }
                //if( mat.shader.name == "Nature/SpeedTree3" 
                //|| mat.shader.name == "Nature/SpeedTree"
                //|| mat.shader.name == "Nature/SpeedTree2"
                //)
                if (mat.HasProperty("_Cutoff") && mat.HasProperty("_Mode") == false)
                {
                    ret = true;
                    break;
                }
            }
            return ret;
        }



        /*public static bool ContainAnyOpaque(MeshRenderer mr)
        {
            bool ret = false;
            for (int j = 0; j < mr.sharedMaterials.Length; j++)
            {
                var mat = mr.sharedMaterials[j];

                if (mat.shader.renderQueue < (int)UnityEngine.Rendering.RenderQueue.Transparent)
                {
                    float mode = 0;
                    if (mat.HasProperty("_Mode"))
                    {
                        mode = mat.GetFloat("_Mode");
                        if (mode == 0)
                        {
                            //不透明
                            ret = true;
                            break;
                        }
                        else
                        {
                            ret = false;
                        }
                    }
                }
            }
            return ret;
        }*/

        public static bool IsSceneOpened(string sceneName)
        {
            return SceneManager.GetSceneByName(sceneName).isLoaded;
        }

#if UNITY_EDITOR
        public static void ClearScenes()
        {
            if (!IsSceneOpened(String.Empty))
            {
                var emptyScene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

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


        public static string LoadJson(string path)
        {
            string ret = null;


            if (!File.Exists(path))
            {
                Debug.LogErrorFormat("oc scenes config file {0} does not exist!", path);
                return ret;
            }

            ret = File.ReadAllText(path);
            return ret;
        }

        public static void CopyTo(string sourceFilePath, string destFilePath)
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

        public static string GetObjectPath(Transform trans)
        {
            var path = trans.name;
            while (trans.parent != null)
            {
                trans = trans.parent;
                path = string.Format("{0}/{1}", trans.name, path);
            }

            return path;
        }
#endif


        public static Byte[] ConvertBitArray(BitArray bit_data)
        {
            Byte[] m_data = new Byte[bit_data.Length / 8];

            for (int i = 0, y = 0; i < bit_data.Length / 8; i++)
            {
                m_data[i] = 0;

                if (bit_data[y])
                    m_data[i] |= (byte)(1);

                if (bit_data[y + 1])
                    m_data[i] |= (byte)(1 << 1);

                if (bit_data[y + 2])
                    m_data[i] |= (byte)(1 << 2);

                if (bit_data[y + 3])
                    m_data[i] |= (byte)(1 << 3);

                if (bit_data[y + 4])
                    m_data[i] |= (byte)(1 << 4);

                if (bit_data[y + 5])
                    m_data[i] |= (byte)(1 << 5);

                if (bit_data[y + 6])
                    m_data[i] |= (byte)(1 << 6);

                if (bit_data[y + 7])
                    m_data[i] |= (byte)(1 << 7);

                y += 8;
            }
            return m_data;
        }
    }

}

#endif

