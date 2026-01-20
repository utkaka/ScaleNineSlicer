using System;
using UnityEngine;

namespace Utkaka.ScaleNineSlicer.UI
{
    public readonly struct CapsuleImageMeshContext
    {
        public readonly Rect FullRect;
        public readonly Vector4 CornerRadius;
        public readonly Vector2 TileSize;
        public readonly Vector2Int TilesCount;
        public readonly Vector2 SpriteSize;
        public readonly Vector2 CapsuleSize;
        public readonly int OuterVertexCount;
        public readonly float MultipliedPixelsPerUnit;
        public readonly bool CloseVerticalQuarter;
        public readonly bool CloseHorizontalQuarter;
        public readonly bool CutTop;
        public readonly bool CutRight;

        public CapsuleImageMeshContext(CapsuleImage capsuleImage)
        {
            var sliced = capsuleImage.sliced;
            var tiled = capsuleImage.tiled;
            
            SpriteSize = capsuleImage.activeSprite.rect.size;
            FullRect = capsuleImage.GetPixelAdjustedRect();
            TilesCount = Vector2Int.one;
            CutTop = false;
            CutRight = false;
            
            if (capsuleImage.preserveAspect && !sliced && !tiled)
            {
                Utils.PreserveSpriteAspectRatio(ref FullRect, SpriteSize, capsuleImage.rectTransform.pivot);
            }
            TileSize = FullRect.size;

            MultipliedPixelsPerUnit = capsuleImage.multipliedPixelsPerUnit;

            if (tiled)
            {
                var tileSpacing = capsuleImage.tileSpacing;
                TileSize = capsuleImage.tileSize != Vector2.zero ? capsuleImage.tileSize : SpriteSize;
                TileSize /= MultipliedPixelsPerUnit;
                
                TilesCount.x = Mathf.CeilToInt(FullRect.size.x / TileSize.x);
                CutRight = TilesCount.x * TileSize.x + (TilesCount.x - 1) * tileSpacing.x > FullRect.width;
                TilesCount.y = Mathf.CeilToInt(FullRect.size.y / TileSize.y);
                CutTop = TilesCount.y * TileSize.y + (TilesCount.y - 1) * tileSpacing.y > FullRect.height;
            }
            
            OuterVertexCount = 4 * (capsuleImage.sliceApproximationSteps + 1) + 1;
            CapsuleSize = TileSize;
            switch (capsuleImage.sliceType)
            {
                case CapsuleImage.Type.Horizontal:
                    float horizontalRadius;
                    if (TileSize.x >= TileSize.y)
                    {
                        horizontalRadius = TileSize.y / 2.0f;
                    }
                    else
                    {
                        horizontalRadius = TileSize.x / 2.0f;
                        CapsuleSize  = new Vector2(TileSize.x, TileSize.x);
                    }
                    CornerRadius = new Vector4(horizontalRadius, horizontalRadius, horizontalRadius, horizontalRadius);
                    CloseVerticalQuarter = false;
                    CloseHorizontalQuarter = Mathf.Abs(TileSize.x - SpriteSize.x) > Mathf.Epsilon;
                    break;
                case CapsuleImage.Type.Vertical:
                    float verticalRadius;
                    if (TileSize.y >= TileSize.x)
                    {
                        verticalRadius = TileSize.x / 2.0f;
                    }
                    else
                    {
                        verticalRadius = TileSize.y / 2.0f;
                        CapsuleSize  = new Vector2(TileSize.y, TileSize.y);
                    }
                    CornerRadius = new Vector4(verticalRadius, verticalRadius, verticalRadius, verticalRadius);
                    CloseVerticalQuarter = Mathf.Abs(TileSize.y - SpriteSize.y) > Mathf.Epsilon;
                    CloseHorizontalQuarter = false;
                    break;
                case CapsuleImage.Type.Circle:
                    var circleRadius = Mathf.Min(TileSize.x, TileSize.y) / 2.0f;
                    CornerRadius = new Vector4(circleRadius, circleRadius, circleRadius, circleRadius);
                    CapsuleSize  = new Vector2(circleRadius, circleRadius) * 2.0f;
                    CloseVerticalQuarter = false;
                    CloseHorizontalQuarter = false;
                    break;
                case CapsuleImage.Type.RoundedRect:
                    CornerRadius = capsuleImage._cornerRadius;
                    CloseVerticalQuarter = true;
                    CloseHorizontalQuarter = true;
                    var ratioV = Mathf.Min(TileSize.y / (CornerRadius.x + CornerRadius.y), TileSize.y / (CornerRadius.z + CornerRadius.w));
                    var ratioH = Mathf.Min(TileSize.x / (CornerRadius.y + CornerRadius.z), TileSize.x / (CornerRadius.x + CornerRadius.w));
                    var ratio = Mathf.Min(ratioV, ratioH);
                    ratio = Mathf.Min(1.0f, ratio);
                    CornerRadius *= ratio;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (CloseVerticalQuarter)
            {
                OuterVertexCount += 2;
            }

            if (CloseHorizontalQuarter)
            {
                OuterVertexCount += 2;
            }
        }
    }
}