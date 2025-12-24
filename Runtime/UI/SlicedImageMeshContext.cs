using System;
using UnityEngine;

namespace Utkaka.ScaleNineSlicer.UI
{
    public readonly struct SlicedImageMeshContext
    {
        public readonly Rect FullRect;
        
        public readonly Vector2Int VertexCountPerTile;
        public readonly Vector4 Borders;
        public readonly Vector2 InnerTileSize;
        public readonly Vector2Int InnerTilesCount;
        
        public readonly Vector2 TileSize;
        public readonly Vector2Int TilesCount;
        public readonly Vector2 TopRightUVMultiplier;
        public readonly float MultipliedPixelsPerUnit;
        public readonly bool CutTop;
        public readonly bool CutRight;

        public SlicedImageMeshContext(SlicedImage slicedImage)
        {
            var sliced = slicedImage.sliced;
            var tiled = slicedImage.tiled;
            
            var spriteSize = slicedImage.activeSprite.rect.size;
            var transformRect = slicedImage.rectTransform.rect;
            FullRect = slicedImage.GetPixelAdjustedRect();
            VertexCountPerTile = new Vector2Int(2, 2);
            Borders = Vector4.zero;
            InnerTileSize = Vector2.zero;
            InnerTilesCount = Vector2Int.zero;
            TilesCount = Vector2Int.one;
            CutTop = false;
            CutRight = false;
            TopRightUVMultiplier = Vector2.one;
            
            if (slicedImage.preserveAspect && !sliced && !tiled)
            {
                Utils.PreserveSpriteAspectRatio(ref FullRect, spriteSize, slicedImage.rectTransform.pivot);
            }
            TileSize = FullRect.size;

            sliced &= slicedImage.hasBorder;
            
            MultipliedPixelsPerUnit = slicedImage.multipliedPixelsPerUnit;
            var tileMultipliedPixelsPerUnit = 1.0f;

            if (tiled)
            {
                TileSize = slicedImage.tileSize != Vector2.zero ? slicedImage.tileSize : spriteSize;
                transformRect.size = TileSize;
                tileMultipliedPixelsPerUnit = MultipliedPixelsPerUnit;
            }
            if (sliced)
            {
                var canSimplifyMesh = slicedImage.fillCenter && Mathf.Abs(MultipliedPixelsPerUnit - 1.0f) <= Mathf.Epsilon;
                var spriteBorder = slicedImage.activeSprite.border;

                var adjustedRect = new Rect(FullRect.position, TileSize / tileMultipliedPixelsPerUnit);
                Borders = Utils.GetAdjustedBorders(spriteBorder / MultipliedPixelsPerUnit, 
                    transformRect, adjustedRect);
                
                var position2 = new Vector2(Borders.x, Borders.y);
                var position3 = new Vector2(adjustedRect.width - Borders.z, adjustedRect.height - Borders.w);
                InnerTileSize = position3 - position2;
                var scaledPartSize = InnerTileSize;
                
                if (slicedImage.tileScaledSlices)
                {
                    var baseTileSize = new Vector2(spriteSize.x - spriteBorder.x - spriteBorder.z,
                        spriteSize.y - spriteBorder.y - spriteBorder.w);
                    var tileSize = slicedImage.slicedTileSize == Vector2Int.zero ? 
                        baseTileSize : slicedImage.slicedTileSize;
                    tileSize.x = Math.Max(1.0f, tileSize.x);
                    tileSize.y = Math.Max(1.0f, tileSize.y);
                    tileSize /= MultipliedPixelsPerUnit;
                    InnerTileSize = tileSize;
                    if (!(Mathf.Abs(adjustedRect.width - spriteSize.x) <= Mathf.Epsilon) || !(Mathf.Abs(tileSize.x - baseTileSize.x) <= Mathf.Epsilon) || !canSimplifyMesh)
                    {
                        InnerTilesCount.x = Mathf.CeilToInt(scaledPartSize.x / tileSize.x);
                        VertexCountPerTile.x = InnerTilesCount.x * 2 + 2;
                        if (!(scaledPartSize.x % tileSize.x <= Mathf.Epsilon))
                        {
                            VertexCountPerTile.x += 1;
                        }
                    }

                    if (!(Mathf.Abs(adjustedRect.height - spriteSize.y) <= Mathf.Epsilon) || !(Mathf.Abs(tileSize.y - baseTileSize.y) <= Mathf.Epsilon) || !canSimplifyMesh)
                    {
                        InnerTilesCount.y = Mathf.CeilToInt(scaledPartSize.y / tileSize.y);
                        VertexCountPerTile.y = InnerTilesCount.y * 2 + 2;
                        if (!(scaledPartSize.y % tileSize.y <= Mathf.Epsilon))
                        {
                            VertexCountPerTile.y += 1;
                        }
                    }
                }
                else
                {
                    if (!(Mathf.Abs(adjustedRect.width - spriteSize.x) <= Mathf.Epsilon) || !canSimplifyMesh)
                    {
                        VertexCountPerTile.x = 4;
                        InnerTilesCount.x = 1;
                    }

                    if (!(Mathf.Abs(adjustedRect.height - spriteSize.y) <= Mathf.Epsilon) || !canSimplifyMesh)
                    {
                        VertexCountPerTile.y = 4;
                        InnerTilesCount.y = 1;
                    }
                }
            }

            if (tiled)
            {
                var canRepeatTiles = slicedImage.activeSprite.texture.wrapMode == TextureWrapMode.Repeat;
                var tileSpacing = slicedImage.tileSpacing;
                var canRepeatX = canRepeatTiles && tileSpacing.x ==  0 
                                                && Mathf.Abs(TileSize.x - spriteSize.x) <= Mathf.Epsilon
                                                && VertexCountPerTile.x == 2;
                var canRepeatY = canRepeatTiles && tileSpacing.y == 0
                                                && Mathf.Abs(TileSize.y - spriteSize.y) <= Mathf.Epsilon
                                                && VertexCountPerTile.y == 2;
                
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