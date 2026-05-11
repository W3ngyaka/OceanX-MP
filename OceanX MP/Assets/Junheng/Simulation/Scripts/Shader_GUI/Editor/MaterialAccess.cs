using UnityEngine;

namespace OceanX
{
    internal static class MaterialAccess
    {
        internal static int ReadMaterialRawRenderQueue(Material mat)
        {
            return mat.renderQueue;
        }
    }
}
