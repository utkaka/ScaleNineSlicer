using System;
using UnityEngine;

namespace Utkaka.ScaleNineSlicer.UI
{
    public abstract class SlicedImageCustomFilling : ScriptableObject
    {
        public abstract int GetPolygonsCount(float fillAmount);
        public abstract int GetPolygonCutLinesCount(int polygonIndex, float fillAmount);
        public abstract void FillPolygonCutLines(Span<CutLine> cutLines, float fillAmount, Rect rect, int polygonIndex);
    }
}