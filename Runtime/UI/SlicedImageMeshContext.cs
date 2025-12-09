using System;
using UnityEngine;

namespace Utkaka.ScaleNineSlicer.UI
{
    public struct SlicedImageMeshContext
    {
        public Rect FullRect;
        
        public Vector2Int VertexCountPerTile;
        public Vector4 Borders;
        public Vector2 InnerTileSize;
        public Vector2Int InnerTilesCount;
        
        public bool CutTop;
        public bool CutRight;
        public Vector2 TileSize;
        public Vector2Int TilesCount;
        public Vector2 TopRightUVMultiplier;

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
            
            var multipliedPixelsPerUnit = slicedImage.multipliedPixelsPerUnit;
            var tileMultipliedPixelsPerUnit = 1.0f;

            if (tiled)
            {
                TileSize = slicedImage.tileSize != Vector2.zero ? slicedImage.tileSize : spriteSize;
                transformRect.size = TileSize;
                tileMultipliedPixelsPerUnit = multipliedPixelsPerUnit;
            }
            if (sliced)
            {
                var canSimplifyMesh = slicedImage.fillCenter && Mathf.Approximately(multipliedPixelsPerUnit, 1.0f);
                var spriteBorder = slicedImage.activeSprite.border;

                var adjustedRect = new Rect(FullRect.position, TileSize / tileMultipliedPixelsPerUnit);
                Borders = Utils.GetAdjustedBorders(spriteBorder / multipliedPixelsPerUnit, 
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
                    tileSize /= multipliedPixelsPerUnit;
                    InnerTileSize = tileSize;
                    if (!Mathf.Approximately(adjustedRect.width, spriteSize.x) || !canSimplifyMesh)
                    {
                        InnerTilesCount.x = Mathf.CeilToInt(scaledPartSize.x / tileSize.x);
                        VertexCountPerTile.x = InnerTilesCount.x * 2 + 2;
                        if (!Mathf.Approximately(scaledPartSize.x % tileSize.x, 0.0f))
                        {
                            VertexCountPerTile.x += 1;
                        }
                    }

                    if (!Mathf.Approximately(adjustedRect.height, spriteSize.y) || !canSimplifyMesh)
                    {
                        InnerTilesCount.y = Mathf.CeilToInt(scaledPartSize.y / tileSize.y);
                        VertexCountPerTile.y = InnerTilesCount.y * 2 + 2;
                        if (!Mathf.Approximately(scaledPartSize.y % tileSize.y, 0.0f))
                        {
                            VertexCountPerTile.y += 1;
                        }
                    }
                }
                else
                {
                    if (!Mathf.Approximately(adjustedRect.width, spriteSize.x) || !canSimplifyMesh)
                    {
                        VertexCountPerTile.x = 4;
                        InnerTilesCount.x = 1;
                    }

                    if (!Mathf.Approximately(adjustedRect.height, spriteSize.y) || !canSimplifyMesh)
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
                                                && Mathf.Approximately(TileSize.x, spriteSize.x)
                                                && VertexCountPerTile.x == 2;
                var canRepeatY = canRepeatTiles && tileSpacing.y == 0
                                                && Mathf.Approximately(TileSize.y, spriteSize.y)
                                                && VertexCountPerTile.y == 2;
                
                if (!canRepeatX)
                {
                    TileSize.x /= multipliedPixelsPerUnit;
                    TilesCount.x = Mathf.CeilToInt(FullRect.size.x / TileSize.x);
                    CutRight = TilesCount.x * TileSize.x > FullRect.width;
                }
                else
                {
                    TopRightUVMultiplier.x = FullRect.width * multipliedPixelsPerUnit / TileSize.x;
                    TileSize.x = FullRect.size.x;
                }

                if (!canRepeatY)
                {
                    TileSize.y /= multipliedPixelsPerUnit;
                    TilesCount.y = Mathf.CeilToInt(FullRect.size.y / TileSize.y);
                    CutTop = TilesCount.y * TileSize.y > FullRect.height;
                }
                else
                {
                    TopRightUVMultiplier.y = FullRect.height * multipliedPixelsPerUnit / TileSize.y;
                    TileSize.y = FullRect.size.y;
                }
            }
        }
    }
}