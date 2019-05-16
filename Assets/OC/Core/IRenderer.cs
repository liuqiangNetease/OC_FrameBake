using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OC
{
    public interface IRenderer
    {
        void Prepare();
        HashSet<MeshRenderer> Do(List<MeshRenderer> filterMeshRenderers = null);
        void Finish();
    }
}

