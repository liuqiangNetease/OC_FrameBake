
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace OC
{
    internal class RenderableObjectSet : IEnumerable, IEnumerable<RenderableObj>
    {
        private SingleScene _owner;

        private HashSet<RenderableObj> _renderableObjSet = new HashSet<RenderableObj>();
        private Dictionary<int, RenderableObj> _idObjDict = new Dictionary<int, RenderableObj>();
        private Dictionary<MeshRenderer, RenderableObj> _meshObjDict = new Dictionary<MeshRenderer, RenderableObj>();

        public RenderableObjectSet(SingleScene owner)
        {
            _owner = owner;
        }

        public int Count
        {
            get { return _idObjDict.Count; }
        }

        public void Add(int guid, MeshRenderer renderer)
        {
            RenderableObj obj = null;
            if (_idObjDict.TryGetValue(guid, out obj))
            {
                obj.AddMeshRenderer(renderer);
            }
            else
            {
                obj = new RenderableObj(_owner);
                obj.SetID(guid);
                obj.AddMeshRenderer(renderer);
                _idObjDict[guid] = obj;
                _renderableObjSet.Add(obj);
            }

            _meshObjDict[renderer] = obj;
        }

        internal void Add(List<MeshRenderer> renderers)
        {
            var renderableObj = new RenderableObj(_owner);
            for (int i = 0; i < renderers.Count; i++)
            {
                var renderer = renderers[i];
                renderableObj.AddMeshRenderer(renderer);
                _meshObjDict[renderer] = renderableObj;
            }
            _renderableObjSet.Add(renderableObj);
        }

        internal void Add(MeshRenderer renderer)
        {
            var renderableObj = new RenderableObj(_owner);
            renderableObj.AddMeshRenderer(renderer);
            _meshObjDict[renderer] = renderableObj;
            _renderableObjSet.Add(renderableObj);
        }

        public bool RecalcRenderableObjectGuid(out ushort maxId, Func<string, string, float, bool> progress = null)
        {
            _idObjDict.Clear();
            maxId = 0;
            var objCount = _renderableObjSet.Count;
            var cancelled = false;

            Debug.LogFormat("Generate Game Object Id for scene {0}", _owner.Name);
            foreach (var obj in _renderableObjSet)
            {
                if (!Config.IsBatchMode && progress != null)
                {
                    cancelled = progress(string.Format("场景{0}生成ObjectId", _owner.Name), "设置Object Id", ((float)maxId) / objCount);
                    if (cancelled)
                        break;
                }

                obj.SetID(maxId);
                _idObjDict[maxId] = obj;
                maxId++;
            }

#if UNITY_EDITOR
            if (!Config.IsBatchMode && progress != null)
            {
                EditorUtility.ClearProgressBar();
            }
#endif

            return !cancelled;
        }

        public void RemoveEmptyRenerableObject()
        {
            var removeList = new List<RenderableObj>();
            foreach (var obj in _renderableObjSet)
            {
                if (obj.Count == 0)
                {
                    removeList.Add(obj);
                }
            }

            foreach (var obj in removeList)
            {
                _renderableObjSet.Remove(obj);
                _idObjDict.Remove(obj.GUID);
            }
        }

        public void Clear()
        {
            _renderableObjSet.Clear();
            _idObjDict.Clear();
            _meshObjDict.Clear();
        }

        public RenderableObj GetByGuid(ushort guid)
        {
            RenderableObj obj = null;
            _idObjDict.TryGetValue(guid, out obj);
            return obj;
        }

        public RenderableObj GetByMeshRenderer(MeshRenderer renderer)
        {
            RenderableObj obj = null;
            _meshObjDict.TryGetValue(renderer, out obj);
            return obj;
        }

        public void Remove(MeshRenderer renderer)
        {
            RenderableObj obj = null;
            if (_meshObjDict.TryGetValue(renderer, out obj))
            {
                _meshObjDict.Remove(renderer);
                obj.RemoveMeshRenderer(renderer);
            }
        }


        public IEnumerator<RenderableObj> GetEnumerator()
        {
            return _renderableObjSet.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _renderableObjSet.GetEnumerator();
        }
    }
}
