using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OC
{
    public class Writer
    {
        private BinaryWriter writer;

        public Writer(BinaryWriter writer)
        {
            this.writer = writer;
        }

        public void Write(string str)
        {
            writer.Write(str);
        }

        public void Write(float v)
        {
            writer.Write(v);
        }

        public void Write(int v)
        {
            writer.Write(v);
        }

        public void Write(Vector3 v)
        {
            writer.Write(v.x);
            writer.Write(v.y);
            writer.Write(v.z);
        }

        public void Write(Quaternion q)
        {
            writer.Write(q.x);
            writer.Write(q.y);
            writer.Write(q.z);
            writer.Write(q.w);
        }

        public void Write(Bounds v)
        {
            Write(v.center);
            Write(v.size);
        }

        public void Write(byte v)
        {
            writer.Write(v);
        }

        public void Write(byte[] v)
        {
            writer.Write(v);
        }
    }

}