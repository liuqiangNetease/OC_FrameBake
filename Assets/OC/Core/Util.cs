using ArtPlugins;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace OC
{
    public class Util
    {
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

            ret = !ContainAnyTransparent(renderer);

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

        public static bool ContainAnyTransparent(MeshRenderer mr)
        {
            bool bTransparent = false;
            for (int j = 0; j < mr.sharedMaterials.Length; j++)
            {
                var mat = mr.sharedMaterials[j];
                if (mat == null)
                    continue;

                if (mat.shader.renderQueue >= (int)UnityEngine.Rendering.RenderQueue.Transparent)
                {
                    bTransparent = true;
                    break;
                }
                if (mat.HasProperty("_Mode"))
                {
                    float mode = mat.GetFloat("_Mode");
                    if (mode == 0)
                    {
                        //不透明
                        bTransparent = false;
                    }
                    else
                    {
                        bTransparent = true;
                        break;
                    }
                }
                /*if(mat.HasProperty("_Cutoff"))
                {
                    //float cutOff = mat.GetFloat("_Cutoff");
                    bTransparent = true;
                    break;
                }*/
            }
            return bTransparent;
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

