using System;
using UnityEngine;

namespace Utkaka.ScaleNineSlicer.UI
{
    [Serializable]
    public struct RoundedImageCornerSettings
    {
        public float Radius;
        [Range(0, 30)]
        public int ApproximationSteps;
    }
}