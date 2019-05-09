using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OC
{
    public class Window
    {
        public Dictionary<Index, Tile> tileMap = new Dictionary<Index, Tile>();

        private World _owner;
        public World Owner
        {
            get { return _owner; }
        }
        private Vector2 _position;
        public Vector2 Position
        {
            get { return _position; }
            set { _position = value; }
        }

        private int _radius;
        public int Radius
        {
            get { return _radius; }
            set { _radius = value; }
        }

        protected Index _currentIndex;
        public Index CurrentIndex
        {
            get { return _currentIndex; }
            set
            {
                _currentIndex = value;
            }
        }

        public Window(World owner, int radius)
        {
            _owner = owner;
            _radius = radius;
        }

        public void InitPosition(Vector2 pos)
        {
            if (_owner.IsValidPosition(pos))
            {
                _position = pos;
                _currentIndex = _owner.ComputerIndex(pos);
                Update(pos, false);
            }
        }

        public bool Init(Index index)
        {
//            Clean();
            if (_owner.IsValidIndex(index))
            {
                _currentIndex = index;

                Vector2 ret;
                bool bSuc = _owner.ComputerIndexPosition(index, out ret);
                if (bSuc)
                {
                    _position = ret;
                    return Update(_position, false);
                }
            }

            return false;
        }
     

        public bool IsContains(Index index)
        {
            return tileMap.ContainsKey(index);
        }

        public void RemoveTile(Index index)
        {
            if (tileMap.ContainsKey(index))
                tileMap.Remove(index);
        }

        public void Clean()
        {
            foreach (var pair in tileMap)
            {
                Tile tile = pair.Value;              
                _owner.UnloadTile(tile);
            }
            tileMap.Clear();
        }

        public bool Update(Index newIndex)
        {
            var initSuccess = true;
            if (_owner != null)
            {
               // _position = _owner.Com;
                //Index newIndex = _owner.ComputerIndex(position);
                if ( newIndex.Equals(_currentIndex))
                {
                    return true;
                }
                else
                {
                    _currentIndex = newIndex;
                }

                Dictionary<Index, Tile> oldTileMap = new Dictionary<Index, Tile>();
                foreach (var tile in tileMap)
                {
                    oldTileMap.Add(tile.Key, tile.Value);
                }


                UpdateTileMap();

                //unload tiles
                foreach (var pair in oldTileMap)
                {
                    Tile tile = pair.Value;
                    if (tile != null)
                    {
                        if (IsContains(tile.TileIndex) == false)
                        {
                            _owner.UnloadTile(tile);
                        }
                    }
                }

                //new window
                foreach (KeyValuePair<Index, Tile> pair in tileMap)
                {
                    Tile tile = pair.Value;
                    if (tile != null)
                    {
                        _owner.OpenScene(tile);
                        tile.GeneraterRenderableObjectID();
                    }
                }

             
            }

            return initSuccess;
        }

        public bool Update(Vector2 position, bool checkMovment = true)
        {
            var initSuccess = true;
            if (_owner != null)
            {
                _position = position;
                Index newIndex = _owner.ComputerIndex(position);
                if (checkMovment && newIndex.Equals(_currentIndex))
                {
                    return true;
                }
                else
                {
                    _currentIndex = newIndex;
                }

                Dictionary<Index, Tile> oldTileMap = new Dictionary<Index, Tile>();
                foreach (var tile in tileMap)
                {
                    oldTileMap.Add(tile.Key, tile.Value);
                }
               

                UpdateTileMap();

                //unload tiles
                foreach (var pair in oldTileMap)
                {
                    Tile tile = pair.Value;
                    if (tile != null)
                    {
                        if (IsContains(tile.TileIndex) == false)
                        {                          
                            _owner.UnloadTile(tile);
                        }
                    }
                }

                //new window
                foreach (KeyValuePair<Index, Tile> pair in tileMap)
                {
                    Tile tile = pair.Value;
                    if (tile != null)
                    {
                        _owner.OpenScene(tile);
                        tile.GeneraterRenderableObjectID();
                    }
                }
            }

            return initSuccess;
        }

        protected void UpdateTileMap()
        {
            if (_owner != null)
            {
                tileMap.Clear();
                int xFirstCorner = _currentIndex.PreX(_radius);
                int yFirstCorner = _currentIndex.PreY(_radius);

                int sizeY = _radius * 2 + 1;
                int sizeX = _radius * 2 + 1;

                Index workIndex = _currentIndex;
             
                workIndex.x = xFirstCorner;
                workIndex.y = yFirstCorner;

                while (sizeY-- > 0)
                {
                    int workSizeX = sizeX;
                    while (workSizeX-- > 0)
                    {
                        Tile tile = _owner.GetOrCreateTile(workIndex);
                        if (tile != null)
                        {                                                
                            tileMap[workIndex] = tile;
                        }

                        workIndex.x = workIndex.NextX();
                    }

                    workIndex.x = xFirstCorner;
                    workIndex.y = workIndex.NextY();
                }
            }
        }

        public bool Bake(bool bFrame, string tempPath)
        {
            foreach (var pair in tileMap)
            {
                Tile tile = pair.Value;
                if (tile.TileIndex.Equals(_currentIndex))
                {
                    return tile.Bake(bFrame, tempPath);                   
                }
            }

            return true;
        }

        public void CopyOCDataTo(string temporaryContainer)
        {
            foreach (var pair in tileMap)
            {
                SingleScene scene = pair.Value as SingleScene;
                if (scene.TileIndex.Equals(_currentIndex))
                {
                    try
                    {
                        var dataFilePath = scene.GetOCDataFilePath();
                        if (File.Exists(dataFilePath))
                        {
                            if (!Directory.Exists(temporaryContainer))
                            {
                                Debug.LogWarningFormat("Can not find temporay directory for container {0}, create it", temporaryContainer);
                                Directory.CreateDirectory(temporaryContainer);
                            }

                            var paths = dataFilePath.Split('/');
                            var fileName = paths[paths.Length - 1];
                            var filePath = Path.Combine(temporaryContainer, fileName);
                            if (File.Exists(filePath))
                                File.Delete(filePath);

                            File.Copy(dataFilePath, filePath);

                        }
                        else
                        {
                            Debug.LogErrorFormat("Can not find oc file{0}", dataFilePath);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        throw;
                    }
                    
                    break;
                }
            }
        }


    }

}
