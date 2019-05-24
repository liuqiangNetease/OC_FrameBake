#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OC
{
    public interface IRenderer
    {
        void Prepare();
        HashSet<MeshRenderer> GetVisibleModels(List<MeshRenderer> renderers = null);
        void Finish();
    }
}

#endif
