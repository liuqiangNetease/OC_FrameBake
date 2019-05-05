using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace OC
{
    //    class IndexComparer : IEqualityComparer<Index>
    //    {
    //        public bool Equals(Index x, Index y)
    //        {
    //            return x.Equals(y);
    //        }
    //
    //        public int GetHashCode(Index obj)
    //        {
    //            return obj.GetHashCode();
    //        }
    //    }

    [Serializable]
    public struct Index: IEquatable<Index>
    {
        public static Index InValidIndex = new Index(-10, -10);

        public Index(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public int x;
        public int y;

//        public bool IsValid()
//        {
//            return (x >= 0) && ( y >= 0);
//        }



        public int PreX(int count = 1)
        {
            return x - count;
        }

        public int PreY(int count = 1)
        {
            return y - count;
        }

        public int NextX(int count = 1)
        {
            return x + count;
        }

        public int NextY(int count = 1)
        {
            return y + count;
        }

        public override bool Equals(System.Object obj)
        {
            bool ret = false;
            if (obj is Index)
            {
                Index temp = (Index)obj;
                ret = Equals( temp);
            }
            return ret;
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ (y.GetHashCode() << 2);
        }

        public bool Equals(Index other)
        {
            return (x == other.x && y == other.y );
        }
        public static Index operator +(Index a, Index b)
        {
            Index ret;
            ret.x = a.x + b.x;
            ret.y = a.y + b.y;
            return ret;
        }

        public override string ToString()
        {
            return String.Format("[{0},{1}]", x, y);
        }
    }

}

