﻿
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace OC
{
    public class RenderableObjectListEquality : IEqualityComparer<RenderableObj>
    {
        public bool Equals(RenderableObj x, RenderableObj y)
        {
            return (x.GUID == y.GUID) && (x.Owner == y.Owner);
        }

        public int GetHashCode(RenderableObj obj)
        {
            return (obj == null) ? 0 : obj.GUID;
        }
    }


    public class RenderableObj : IComparable<RenderableObj>
    {
        private SingleScene _owner;

        public SingleScene Owner
        {
            get { return _owner; }
        }
        public RenderableObj(SingleScene owner)
        {
            _owner = owner;
            _visible = true;
        }
        private List<MeshRenderer> _rendererList = new List<MeshRenderer>();

        bool _visible;
        public bool IsVisible
        {
            get { return _visible; }
            set { _visible = value; }
        }


        private int guid;

        public int GUID
        {
            get { return guid; }
            set { guid = value; }
        }

        public void SetID(int guid)
        {
            GUID = guid;
            for (int i = 0; i < _rendererList.Count; i++)
            {
                var com = _rendererList[i].gameObject.GetComponent<GameObjectID>();
                if (com == null)
                    com = _rendererList[i].gameObject.AddComponent<GameObjectID>();
                com.GUID = guid;

            }          
        }

        public void Add(RenderableObj obj)
        {
            _rendererList.AddRange(obj._rendererList);
        }

        public void AddMeshRenderer(MeshRenderer mesh)
        {
            _rendererList.Add(mesh);
        }

        public void RemoveMeshRenderer(MeshRenderer mesh)
        {
            _rendererList.Remove(mesh);
        }

        public bool HasMeshRenderer(MeshRenderer mesh)
        {
            return _rendererList.Contains(mesh);
        }

        public int Count
        {
            get { return _rendererList.Count; }
        }

        public MeshRenderer this[int index]
        {
            get { return _rendererList[index]; }
        }

        public void Clear()
        {
            _rendererList.Clear();
        }


        public void SetVisible(bool bVis)
        {
            if (_visible == bVis)
                return;

            _visible = bVis;
            
            foreach (var meshRenderer in _rendererList)
            {
                meshRenderer.enabled = bVis;              
            }
        }

        public int CompareTo(RenderableObj other)
        {
            int ret = 0;

            if (guid < other.guid)
                ret = -1;
            else if (guid > other.guid)
                ret = 1;

            return ret;

        }
    }
}

