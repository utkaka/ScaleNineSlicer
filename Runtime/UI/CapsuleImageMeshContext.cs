using System;
using UnityEngine;

namespace Utkaka.ScaleNineSlicer.UI
{
    public readonly struct CapsuleImageMeshContext
    {
        public readonly Rect FullRect;
        public readonly Vector2 TileSize;
        public readonly Vector2Int TilesCount;
        public readonly Vector2 TopRightUVMultiplier;
        public readonly Vector2 SpriteSize;
        public readonly int VertexCount;
        public readonly float MultipliedPixelsPerUnit;
        public readonly bool CutTop;
        public readonly bool CutRight;

        public CapsuleImageMeshContext(CapsuleImage capsuleImage)
        {
            var sliced = capsuleImage.sliced;
            var tiled = capsuleImage.tiled;
            
            SpriteSize = capsuleImage.activeSprite.rect.size;
            var transformRect = capsuleImage.rectTransform.rect;
            FullRect = capsuleImage.GetPixelAdjustedRect();
            VertexCount = 4;
            TilesCount = Vector2Int.one;
            CutTop = false;
            CutRight = false;
            TopRightUVMultiplier = Vector2.one;
            
            if (capsuleImage.preserveAspect && !sliced && !tiled)
            {
                Utils.PreserveSpriteAspectRatio(ref FullRect, SpriteSize, capsuleImage.rectTransform.pivot);
            }
            TileSize = FullRect.size;

            MultipliedPixelsPerUnit = capsuleImage.multipliedPixelsPerUnit;

            if (tiled)
            {
                TileSize = capsuleImage.tileSize != Vector2.zero ? capsuleImage.tileSize : SpriteSize;
                transformRect.size = TileSize;
            }
            if (sliced)
            {
                var stepsCount = capsuleImage.sliceApproximationSteps;
                VertexCount = stepsCount * 2 + 3;
                if (capsuleImage.sliceType != CapsuleImage.Type.Circle)
                {
                    //VertexCount += 4 + (stepsCount & 1) * 2;
                    VertexCount += 4;
                }
            }

            if (tiled)
            {
                var canRepeatTiles = capsuleImage.activeSprite.texture.wrapMode == TextureWrapMode.Repeat;
                var tileSpacing = capsuleImage.tileSpacing;
                var canRepeatX = canRepeatTiles && tileSpacing.x == 0
                                                && Mathf.Abs(TileSize.x - SpriteSize.x) <= Mathf.Epsilon
                                                && VertexCount == 4;
                var canRepeatY = canRepeatTiles && tileSpacing.y == 0
                                                && Mathf.Abs(TileSize.y - SpriteSize.y) <= Mathf.Epsilon
                                                && VertexCount == 4;
                
                if (!canRepeatX)
                {
                    TileSize.x /= MultipliedPixelsPerUnit;
                    TilesCount.x = Mathf.CeilToInt(FullRect.size.x / TileSize.x);
                    CutRight = TilesCount.x * TileSize.x + (TilesCount.x - 1) * tileSpacing.x > FullRect.width;
                }
                else
                {
                    TopRightUVMultiplier.x = FullRect.width * MultipliedPixelsPerUnit / TileSize.x;
                    TileSize.x = FullRect.size.x;
                }

                if (!canRepeatY)
                {
                    TileSize.y /= MultipliedPixelsPerUnit;
                    TilesCount.y = Mathf.CeilToInt(FullRect.size.y / TileSize.y);
                    CutTop = TilesCount.y * TileSize.y + (TilesCount.y - 1) * tileSpacing.y > FullRect.height;
                }
                else
                {
                    TopRightUVMultiplier.y = FullRect.height * MultipliedPixelsPerUnit / TileSize.y;
                    TileSize.y = FullRect.size.y;
                }
            }
        }
    }
}