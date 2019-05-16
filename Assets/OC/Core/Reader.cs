using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OC
{

    public class Reader
    {
        private BinaryReader reader;

        public Reader(BinaryReader r)
        {
            reader = r;
        }

        public string ReadString()
        {
            return reader.ReadString();
        }

        public float ReadFloat()
        {
            return reader.ReadSingle();
        }

        public int ReadInt()
        {
            return reader.ReadInt32();
        }

        public Vector3 ReadVector3()
        {
            Vector3 res;
            res.x = reader.ReadSingle();
            res.y = reader.ReadSingle();
            res.z = reader.ReadSingle();
            return res;
        }

        public Quaternion ReadQuaternion()
        {
            Quaternion ret;
            ret.x = reader.ReadSingle();
            ret.y = reader.ReadSingle();
            ret.z = reader.ReadSingle();
            ret.w = reader.ReadSingle();
            return ret;
        }

        public Bounds ReadBounds()
        {
            Bounds res = new Bounds();
            res.center = ReadVector3();
            res.size = ReadVector3();
            return res;
        }

        public byte ReadByte()
        {
            return reader.ReadByte();
        }

        public byte[] ReadBytes(int count)
        {
            return reader.ReadBytes(count);
        }
    }
}