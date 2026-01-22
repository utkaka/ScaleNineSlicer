using UnityEngine;

namespace Utkaka.ScaleNineSlicer.UI
{
    public readonly struct RoundedImageQuarterData
    {
        public readonly Vector2 OuterCenter;
        public readonly Vector2 InnerCenter;
        public readonly float OuterRadius;
        public readonly float InnerRadius;
        public readonly int ApproximationSteps;
        public readonly int OuterVertexCount;

        public RoundedImageQuarterData(Vector2 center, Vector2 corner, Vector2 innerCenterOffset, RoundedImageCornerSettings cornerSettings,
            float radiusDiff, float scaleRatio, bool closeQuarter)
        {
            OuterRadius = Mathf.Max(0f, cornerSettings.Radius) * scaleRatio;
            InnerRadius = Mathf.Max(0f, OuterRadius - radiusDiff);

            var toCenter = center - corner;
            toCenter = new Vector2(Mathf.Sign(toCenter.x), Mathf.Sign(toCenter.y));
            OuterCenter = corner + toCenter * OuterRadius;
            InnerCenter = OuterCenter;

            if (toCenter.x < Mathf.Epsilon)
            {
                InnerCenter.x = Mathf.Min(corner.x - radiusDiff, OuterCenter.x);
            }
            else
            {
                InnerCenter.x = Mathf.Max(corner.x + radiusDiff, OuterCenter.x);
            }

            if (toCenter.y < Mathf.Epsilon)
            {
                InnerCenter.y = Mathf.Min(corner.y - radiusDiff, OuterCenter.y);
            }
            else
            {
                InnerCenter.y = Mathf.Max(corner.y + radiusDiff, OuterCenter.y);
            }

            InnerCenter += innerCenterOffset * scaleRatio;

            ApproximationSteps = Mathf.Max(0, cornerSettings.ApproximationSteps) + 1;
            OuterVertexCount = closeQuarter ? ApproximationSteps + 1 : ApproximationSteps;
        }
    }
}