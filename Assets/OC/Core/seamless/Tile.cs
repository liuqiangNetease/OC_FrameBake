﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OC
{
    public enum TileState
    {
        Loading,
        Unloading,
        Loaded,
        Unloaded
    }

    public class Tile
    {
        private World _owner;
        public World Owner
        {
            get { return _owner; }
        }

        private TileState _state;
        public TileState State
        {
            get { return _state; }
            set { _state = value; }
        }

        private Index _index;

        public Index TileIndex
        {
            get { return _index; }
            set { _index = value; }
        }

        private int _tileDimension;

        private int _blockIndex;
        private byte[] _data;

        public List<Window> windowList = new List<Window>();

        public Tile(World owner) :
            this(null, owner)
        {
            
        }

        public Tile(byte[] data, World owner):
            this(Index.InValidIndex, 1000, data, owner)
        {
            
        }

        public Tile(Index index, int tileDimension, byte[] data, World owner)
        {
            _index = index;
            _tileDimension = tileDimension;

            _blockIndex = _index.x * tileDimension + index.y;
            _data = data;

            _owner = owner;
            _state = TileState.Unloaded;
        }

        ~Tile()
        {
            if(_owner != null)
                _owner.RemoveTile(_index);

            for (int i = 0; i < windowList.Count; i++)
            {
                windowList[i].RemoveTile(_index);
            }
            windowList.Clear();
        }
        public void AddWindow(Window window)
        {
            if(windowList.Contains(window) == false)
                windowList.Add(window);
        }

        public void RemoveWindow(Window window)
        {
            if (windowList.Contains(window))
                windowList.Remove(window);
        }

        public virtual void Open()
        {
            
        }

        public virtual bool InitOnOpen()
        {
            return true;
        }

        public virtual void Close()
        {
            
        }

        public void DoLoad()
        {
            if ( _state == TileState.Unloaded)
            {
                _state = TileState.Loading;

                Load(_data, _blockIndex);

                _state = TileState.Loaded;
            }
        }

        public void DoUnload()
        {
            if (_state == TileState.Loaded)
            {
                if ( windowList.Count <= 0 )
                {
                    _state = TileState.Unloading;

                   Unload();

                    _state = TileState.Unloaded;
                }
            }
        }


        public virtual void Load(byte[] ocData, int blockIndex = 0)
        {
            
        }

        public virtual void Unload()
        {
            
        }

        public virtual void OnGameObjectLoad(GameObject go, int renderId)
        {
            
        }

        public virtual void OnGameObjectUnload(GameObject go, int renderId)
        {
            
        }

        public virtual bool Bake(bool bFrame)
        {
            return true;
        }

        public virtual string GetOCDataFilePath()
        {
            return null;
        }

    }
}