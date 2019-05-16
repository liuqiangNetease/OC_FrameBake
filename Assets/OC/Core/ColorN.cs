using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
namespace OC
{
    public class ColorN
    {
        private int _alpha = 254;
        public int Alpha
        {
            get { return _alpha; }
        }

        private int _offset = 50;
        public int Offset
        {
            get { return _offset; }
            set { _offset = value; }
        }
        public int N
        {
            get { return 256 - _offset; }
        }

        int[] a = new int[3];

        public Color32 IntToColorN(int n)
        {
            Color32 ret = new Color32(0, 0, 0, (byte)Alpha);

            int temp = n;
            if (temp == 0)
            {
                ret.r = (byte)_offset; ret.g = (byte)_offset; ret.b = (byte)_offset;
                return ret;
            }

            int k = 0;
            while (temp > 0)
            {
                a[k++] = temp % (256 - _offset);
                temp /= 256 - _offset;
            }

            ret.r = (byte)(a[2] + _offset);
            ret.g = (byte)(a[1] + _offset);
            ret.b = (byte)(a[0] + _offset);

            return ret;
        }

        public int ColorNToInt(Color32 color)
        {
            int ret = 0;
            ret = (color.r - _offset) * N * N + (color.g - _offset) * N + (color.b - _offset);
            return ret;
        }



    }
}
#endif

