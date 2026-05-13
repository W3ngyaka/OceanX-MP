using System.Collections.Generic;
using UnityEngine;

namespace OceanX
{
    /// <summary>
    /// Custom comparer for the bounds size.
    /// </summary>
    public class BoundsComparer : IComparer<Bounds>
    {
        /// <inheritdoc/>
        public int Compare(Bounds x, Bounds y)
        {
            if (x.center == y.center && x.size == y.size)
            {
                return 0;
            }

            float xSizeX = Mathf.Abs(x.size.x);
            float xSizeY = Mathf.Abs(x.size.y);
            float xSizeZ = Mathf.Abs(x.size.z);
            float totalSizeX = xSizeX + xSizeY + xSizeZ;

            float ySizeX = Mathf.Abs(y.size.x);
            float ySizeY = Mathf.Abs(y.size.y);
            float ySizeZ = Mathf.Abs(y.size.z);
            float totalSizeY = ySizeX + ySizeY + ySizeZ;

            return Mathf.Approximately(totalSizeX, totalSizeY) ? 0 : totalSizeX > totalSizeY ? 1 : -1;
        }
    }
}