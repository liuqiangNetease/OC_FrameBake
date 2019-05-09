using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


namespace OC
{
    public abstract class World
    {
#if UNITY_EDITOR
        public BoundsOctree<MeshRenderer> treeMesh;
#endif

        public Dictionary<Index, Tile> tileMap = new Dictionary<Index, Tile>();

        private int _left, _right, _bottom, _top;

        private int _tilesX, _tilesY;

        public int TilesX
        {
            get { return _tilesX; }
        }

        public int TilesY
        {
            get { return _tilesY; }
        }

        private int _tileWidth, _tileHeight;


        public abstract int TileDimension { get; }

        public World()
        {
        }

        public void Save()
        {
//            using (BinaryWriter w = new BinaryWriter(File.Open(OC.Config.savePath + Name + ".oc", FileMode.Create)))
//            {
//                OCDataWrite writer = new OCDataWrite(w);
//
//                writer.Write(_tilesX );
//                writer.Write(_tilesY);
//                writer.Write(_tileWidth);
//                writer.Write(_tileHeight);
//                writer.Write(_left);
//                writer.Write(_bottom);
//
//            }

        }

        public void SetWorldLimits(int left, int right, int bottom, int top, int tilesX, int tilesY)
        {
            _left = left;
            _right = right;
            _bottom = bottom;
            _top = top;
            _tilesX = tilesX;
            _tilesY = tilesY;

            _tileWidth = (right - left) / tilesX;
            _tileHeight = (top - bottom) / tilesY;
        }

        public bool IsValidPosition(Vector2 position)
        {
            return (position.x <= _right) && (position.x >= _left) && (position.y <= _top) && (position.y >= _bottom);
        }

        public bool ComputerIndexPosition(Index index, out Vector2 ret)
        {
            ret = Vector2.zero;
            bool suc = false;
            if (IsValidIndex(index))
            {
                suc = true;
                ret.x = index.x * _tileWidth + _left;
                ret.y = index.y * _tileHeight + _bottom;
            }
            return suc;
        }

        public Index ComputerIndex(Vector2 position)
        {
            Index ret = Index.InValidIndex;
            if (IsValidPosition(position))
            {
                float indexX =  (position.x - _left) / _tileWidth;
                float indexY = (position.y - _bottom) / _tileHeight;
                ret.x = Mathf.FloorToInt(indexX);
                ret.y = Mathf.FloorToInt(indexY);
            }
            return ret;
        }

        public bool IsValidIndex(Index index)
        {
            return (index.x >=0) && (index.y >=0) && (index.x < _tilesX) && (index.y < _tilesY);
        }

        virtual public Tile BuildTile(Index index)
        {
            return null;
        }

        public Tile GetOrCreateTile(Index index)
        {
            Tile ret = null;
            if (IsValidIndex(index))
            {
                if (tileMap.TryGetValue(index, out ret) == false)
                {
                    ret = BuildTile(index);
                    if (ret != null)
                    {
                        tileMap.Add(index, ret);
                    }
                }
            }

            return ret;
        }

        public void RemoveTile(Index index)
        {
            if (tileMap.ContainsKey(index))
                tileMap.Remove(index);
        }

        virtual public void DestroyTile(Index index)
        {
            Tile tile = null;
            if(tileMap.TryGetValue(index, out tile))
                tileMap.Remove(index);

            if (tile != null)
            {
                tile.DoUnload();
                tile = null;
            }
        }

        public Tile ExistTile(Index index)
        {
            Tile ret = null;
            tileMap.TryGetValue(index, out ret);
            return ret;
        }

        public void Clean()
        {
            foreach (var tile in tileMap)
            {
                Tile temp = tile.Value;
                temp.DoUnload();
                temp = null;
            }
            tileMap.Clear();
        }

        public void OpenScene(Tile tile)
        {
            tile.Open();
            tileMap[tile.TileIndex] = tile;
        }

        public void UnloadTile(Tile tile)
        {
            tile.Close();
            DestroyTile(tile.TileIndex);
        }
    }
}

