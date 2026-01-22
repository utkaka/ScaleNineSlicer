using System;
using UnityEngine;

namespace Utkaka.ScaleNineSlicer.UI
{
    public readonly struct RoundedImageMeshContext
    {
        public readonly RoundedImageQuarterData BottomLeftQuarter;
        public readonly RoundedImageQuarterData TopLeftQuarter;
        public readonly RoundedImageQuarterData TopRightQuarter;
        public readonly RoundedImageQuarterData BottomRightQuarter;
        public readonly Rect FullRect;
        public readonly Vector2 TileSize;
        public readonly Vector2Int TilesCount;
        public readonly Vector2 SpriteSize;
        public readonly Vector2 CapsuleSize;
        public readonly int OuterVertexCount;
        public readonly float ScaleRatio;
        public readonly float MultipliedPixelsPerUnit;
        public readonly bool CutTop;
        public readonly bool CutRight;

        public RoundedImageMeshContext(RoundedImage roundedImage)
        {
            var tiled = roundedImage.tiled;
            
            SpriteSize = roundedImage.activeSprite.rect.size;
            FullRect = roundedImage.GetPixelAdjustedRect();
            TilesCount = Vector2Int.one;
            CutTop = false;
            CutRight = false;
            
            TileSize = FullRect.size;

            MultipliedPixelsPerUnit = roundedImage.multipliedPixelsPerUnit;

            if (tiled)
            {
                var tileSpacing = roundedImage.tileSpacing;
                TileSize = roundedImage.tileSize != Vector2.zero ? roundedImage.tileSize : SpriteSize;
                TileSize /= MultipliedPixelsPerUnit;
                
                TilesCount.x = Mathf.CeilToInt(FullRect.size.x / TileSize.x);
                CutRight = TilesCount.x * TileSize.x + (TilesCount.x - 1) * tileSpacing.x > FullRect.width;
                TilesCount.y = Mathf.CeilToInt(FullRect.size.y / TileSize.y);
                CutTop = TilesCount.y * TileSize.y + (TilesCount.y - 1) * tileSpacing.y > FullRect.height;
            }

            var halfTileSize = TileSize / 2.0f;
            var outerRadius = roundedImage.roundedSprite.OuterRadius;
            var innderRadius = roundedImage.roundedSprite.InnerRadius;
            var innerCenterOffset = roundedImage.roundedSprite.InnerCenter - roundedImage.roundedSprite.OuterCenter;
            var radiusDiff = outerRadius - innderRadius;
            CapsuleSize = TileSize;
            RoundedImageCornerSettings bottomLeftCorner;
            RoundedImageCornerSettings topLeftCorner;
            RoundedImageCornerSettings topRightCorner;
            RoundedImageCornerSettings bottomRightCorner;
            
            switch (roundedImage.sliceType)
            {
                case RoundedImage.Type.Horizontal:
                    float horizontalRadius;
                    if (TileSize.x >= TileSize.y)
                    {
                        horizontalRadius = halfTileSize.y;
                    }
                    else
                    {
                        horizontalRadius = halfTileSize.x;
                        CapsuleSize  = new Vector2(TileSize.x, TileSize.x);
                    }

                    bottomLeftCorner = topLeftCorner =
                        topRightCorner = bottomRightCorner = new RoundedImageCornerSettings {
                            Radius = horizontalRadius, 
                            ApproximationSteps = roundedImage.sliceApproximationSteps
                        };
                    break;
                case RoundedImage.Type.Vertical:
                    float verticalRadius;
                    if (TileSize.y >= TileSize.x)
                    {
                        verticalRadius = halfTileSize.x;
                    }
                    else
                    {
                        verticalRadius = halfTileSize.y;
                        CapsuleSize  = new Vector2(TileSize.y, TileSize.y);
                    }
                    
                    bottomLeftCorner = topLeftCorner =
                        topRightCorner = bottomRightCorner = new RoundedImageCornerSettings {
                            Radius = verticalRadius, 
                            ApproximationSteps = roundedImage.sliceApproximationSteps
                        };
                    break;
                case RoundedImage.Type.Circle:
                    var circleRadius = Mathf.Min(halfTileSize.x, halfTileSize.y);
                    bottomLeftCorner = topLeftCorner =
                        topRightCorner = bottomRightCorner = new RoundedImageCornerSettings {
                            Radius = circleRadius, 
                            ApproximationSteps = roundedImage.sliceApproximationSteps
                        };
                    CapsuleSize  = new Vector2(circleRadius, circleRadius) * 2.0f;
                    break;
                case RoundedImage.Type.RoundedRect:
                    bottomLeftCorner = roundedImage.bottomLeftCorner;
                    topLeftCorner = roundedImage.topLeftCorner;
                    topRightCorner = roundedImage.topRightCorner;
                    bottomRightCorner = roundedImage.bottomRightCorner;
                    var ratioV = Mathf.Min(TileSize.y / (bottomLeftCorner.Radius + topLeftCorner.Radius), TileSize.y / (topRightCorner.Radius + bottomRightCorner.Radius));
                    var ratioH = Mathf.Min(TileSize.x / (topLeftCorner.Radius + topRightCorner.Radius), TileSize.x / (bottomLeftCorner.Radius + bottomRightCorner.Radius));
                    var ratio = Mathf.Min(ratioV, ratioH);
                    ratio = Mathf.Min(1.0f, ratio);
                    bottomLeftCorner.Radius *= ratio;
                    topLeftCorner.Radius *= ratio;
                    topRightCorner.Radius *= ratio;
                    bottomRightCorner.Radius *= ratio;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            var capsuleRect = new Rect(Vector2.zero, CapsuleSize)
            {
                center = halfTileSize
            };
            
            ScaleRatio = Mathf.Min(CapsuleSize.y / (2 * radiusDiff), CapsuleSize.x / (2 * radiusDiff));
            ScaleRatio = Mathf.Min(1.0f, ScaleRatio);
            radiusDiff *= ScaleRatio;

            BottomLeftQuarter = new RoundedImageQuarterData(capsuleRect.center, capsuleRect.min, innerCenterOffset,
                bottomLeftCorner, radiusDiff, ScaleRatio,
                Math.Abs(bottomLeftCorner.Radius + topLeftCorner.Radius - CapsuleSize.y) > Mathf.Epsilon);
            
            TopLeftQuarter = new RoundedImageQuarterData(capsuleRect.center, new Vector2(capsuleRect.xMin, capsuleRect.yMax), innerCenterOffset,
                topLeftCorner, radiusDiff, ScaleRatio,
                Math.Abs(topLeftCorner.Radius + topRightCorner.Radius - CapsuleSize.x) > Mathf.Epsilon);
            
            TopRightQuarter = new RoundedImageQuarterData(capsuleRect.center, capsuleRect.max, innerCenterOffset,
                topRightCorner, radiusDiff, ScaleRatio,
                Math.Abs(topRightCorner.Radius + bottomRightCorner.Radius - CapsuleSize.y) > Mathf.Epsilon);
            
            BottomRightQuarter = new RoundedImageQuarterData(capsuleRect.center, new Vector2(capsuleRect.xMax, capsuleRect.yMin), innerCenterOffset,
                bottomRightCorner, radiusDiff, ScaleRatio,
                Math.Abs(bottomRightCorner.Radius + bottomLeftCorner.Radius - CapsuleSize.x) > Mathf.Epsilon);

            OuterVertexCount = BottomLeftQuarter.OuterVertexCount + TopLeftQuarter.OuterVertexCount +
                               TopRightQuarter.OuterVertexCount + BottomRightQuarter.OuterVertexCount + 1;
        }
        
        
    }
}