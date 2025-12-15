using System;
using UnityEngine;
using Utkaka.ScaleNineSlicer.UI;

[CreateAssetMenu(menuName = "ScaleNineSlicer/Samples/DiamondImageFilling")]
public class DiamondImageFilling : SlicedImageCustomFilling
{
    public override int GetPolygonsCount(float fillAmount)
    {
        return Mathf.Approximately(fillAmount, 0.0f) ? 0 : 1;
    }

    public override int GetPolygonCutLinesCount(int polygonIndex, float fillAmount) => 4;

    public override void FillPolygonCutLines(Span<CutLine> cutLines, float fillAmount, Rect rect, int polygonIndex)
    {
        var cutLineIndex = 0;
        
        cutLines[cutLineIndex++] =
            CutLine.FromLine(new Vector2(Mathf.Lerp(rect.xMax, rect.xMin, fillAmount), rect.yMin),
                new Vector2(rect.xMin, Mathf.Lerp(rect.yMax, rect.yMin, fillAmount)));
        
        cutLines[cutLineIndex++] =
            CutLine.FromLine(new Vector2(rect.xMax, Mathf.Lerp(rect.yMax, rect.yMin, fillAmount)),
                new Vector2(Mathf.Lerp(rect.xMin, rect.xMax, fillAmount), rect.yMin));
        
        cutLines[cutLineIndex++] =
            CutLine.FromLine(new Vector2(rect.xMin, Mathf.Lerp(rect.yMin, rect.yMax, fillAmount)),
                new Vector2(Mathf.Lerp(rect.xMax, rect.xMin, fillAmount), rect.yMax));
        
        cutLines[cutLineIndex] =
            CutLine.FromLine(new Vector2(Mathf.Lerp(rect.xMin, rect.xMax, fillAmount), rect.yMax),
                new Vector2(rect.xMax, Mathf.Lerp(rect.yMin, rect.yMax, fillAmount)));
    }
}