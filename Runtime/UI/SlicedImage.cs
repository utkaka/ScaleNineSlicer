using System;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace Utkaka.ScaleNineSlicer.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("UI/SlicedImage")]
    public class SlicedImage : ExtendedImage
    {
        [SerializeField]
        private Sprite _sprite;
        [SerializeField]
        private bool _preserveAspect;
        [SerializeField]
        private bool _useSpriteMesh;
        
        [SerializeField]
        private bool _sliced;
        [SerializeField]
        private bool _tileScaledSlices;
        [SerializeField]
        private Vector2Int _slicedTileSize;
        
        private Sprite _overrideSprite;
        
        #region Base properties
        public Sprite sprite
        {
            get => _sprite;
            set
            {
                if (_sprite != null)
                {
                    if (_sprite == value) return;
                    m_SkipLayoutUpdate = _sprite.rect.size.Equals(value ? value.rect.size : Vector2.zero);
                    m_SkipMaterialUpdate = _sprite.texture == (value ? value.texture : null);
                    _sprite = value;
                    SetAllDirty();
                    TrackSprite();
                }
                else if (value != null)
                {
                    m_SkipLayoutUpdate = value.rect.size == Vector2.zero;
                    m_SkipMaterialUpdate = value.texture == null;
                    _sprite = value;

                    SetAllDirty();
                    TrackSprite();
                }
            }
        }
        
        public Sprite overrideSprite
        {
            get => activeSprite;
            set
            {
                if (!Utils.SetClass(ref _overrideSprite, value)) return;
                SetAllDirty();
                TrackSprite();
            }
        }
        
        public override Sprite activeSprite => _overrideSprite != null ? _overrideSprite : sprite;
        
        #endregion
        
        #region Simple properties
        public bool preserveAspect
        {
            get { return _preserveAspect; }
            set
            {
                if (Utils.SetStruct(ref _preserveAspect, value)) SetVerticesDirty();
            }
        }
        
        public bool useSpriteMesh
        {
            get => _useSpriteMesh;
            set
            {
                if (Utils.SetStruct(ref _useSpriteMesh, value)) SetVerticesDirty();
            }
        }

        #endregion
        
        #region Sliced properties
        public override bool sliced
        {
            get => _sliced;
            set { if (Utils.SetStruct(ref _sliced, value)) SetVerticesDirty(); }
        }
        public bool tileScaledSlices
        {
            get => _tileScaledSlices;
            set
            {
                if (Utils.SetStruct(ref _tileScaledSlices, value)) SetVerticesDirty();
            }
        }
        
        public Vector2Int slicedTileSize
        {
            get => _slicedTileSize;
            set
            {
                if (Utils.SetStruct(ref _slicedTileSize, value)) SetVerticesDirty();
            }
        }

        #endregion
        
        public bool hasBorder
        {
            get
            {
                if (activeSprite == null) return false;
                var v = activeSprite.border;
                return v.sqrMagnitude > 0f;
            }
        }
        
        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            if (activeSprite == null)
            {
                base.OnPopulateMesh(toFill);
                return;
            }
            toFill.Clear();
            
            if (useSpriteMesh && !sliced && !tiled && !filled)
            {
                var spriteMeshRect = GetPixelAdjustedRect();
                if (preserveAspect)
                {
                    Utils.PreserveSpriteAspectRatio(ref spriteMeshRect, activeSprite.rect.size, rectTransform.pivot);
                }
                SpriteMeshHandler.PrepareMesh(toFill, activeSprite, color, spriteMeshRect, rectTransform.pivot);
                return;
            }


            var context = new SlicedImageMeshContext(this);
            var totalVertexCount = context.VertexCountPerTile.x * context.VertexCountPerTile.y;
            if (totalVertexCount > 65000)
            {
                Debug.LogError($"Too many vertices per tile: {totalVertexCount}");
                return;
            }

            var heapArray = Utils.GetFromPoolIfNeeded<CutInputVertex>(totalVertexCount);
            var vertices = heapArray == null ? stackalloc CutInputVertex[totalVertexCount] : heapArray.AsSpan();
            
            FillBaseVertices(vertices, in context);

            var polygonsCount = GetPolygonsCount();
            var meshVertexCount = 0;
            for (var polygonIndex = 0; polygonIndex < polygonsCount; polygonIndex++)
            {
                PreparePolygon(polygonIndex, in context, vertices, toFill, ref meshVertexCount);
            }
            
            Utils.ReturnToPool(vertices.Length, heapArray);
        }

        private void PreparePolygon(int polygonIndex, in SlicedImageMeshContext context, Span<CutInputVertex> vertices, 
            VertexHelper toFill, ref int meshVertexCount)
        {
            var rectTransformComponent = rectTransform;
            var parentCanvas = canvas;
            var cutLinesCount = GetPolygonCutLinesCount(polygonIndex, context.CutRight, context.CutTop);
            
            var cutsHeapArray = Utils.GetFromPoolIfNeeded<CutLine>(cutLinesCount);
            var cuts = cutsHeapArray == null ? stackalloc CutLine[cutLinesCount] : cutsHeapArray.AsSpan();
            
            FillPolygonCutLines(cuts, context.FullRect, polygonIndex, context.CutRight, context.CutTop);
            
            var tileVerticesHeapArray = Utils.GetFromPoolIfNeeded<CutInputVertex>(vertices.Length);
            var tileVertices = cutsHeapArray == null ? stackalloc CutInputVertex[vertices.Length] : tileVerticesHeapArray.AsSpan();
            
            for (var i = 0; i < context.TilesCount.x; i++)
            {
                for (var j = 0; j < context.TilesCount.y; j++)
                {
                    var tileShift = new Vector2((context.TileSize.x + tileSpacing.x) * i, (context.TileSize.y + tileSpacing.y) * j);
                    for (var v = 0; v < vertices.Length; v++)
                    {
                        var vertex = vertices[v];
                        vertex.Position = RectTransformUtility.PixelAdjustPoint(vertex.Position + tileShift, rectTransformComponent, parentCanvas);
                        tileVertices[v] = vertex;
                    }
                    SlicedMeshHandler.PrepareMesh(tileVertices, cuts, toFill, color, context.VertexCountPerTile.x, context.VertexCountPerTile.y, !fillCenter, ref meshVertexCount);
                }
            }
            
            Utils.ReturnToPool(cuts.Length, cutsHeapArray);
            Utils.ReturnToPool(tileVertices.Length, tileVerticesHeapArray);
        }
        
        private void FillBaseVertices(Span<CutInputVertex> vertices, in SlicedImageMeshContext context)
        {
            var localActiveSprite = activeSprite;
            var adjustedPixelsPerUnit = context.MultipliedPixelsPerUnit;
            var outerUV = DataUtility.GetOuterUV(localActiveSprite);
            var innerUV = DataUtility.GetInnerUV(localActiveSprite);
            var padding = DataUtility.GetPadding(localActiveSprite) / adjustedPixelsPerUnit;
            var position = context.FullRect.position;
            
            var position1 = new Vector2(padding.x, padding.y) + position;
            var position2 = new Vector2(context.Borders.x, context.Borders.y) + position;
            var position3 = new Vector2(context.TileSize.x - context.Borders.z,
                context.TileSize.y - context.Borders.w) + position;
            var position4 = new Vector2(context.TileSize.x - padding.z, context.TileSize.y - padding.w) +
                            position;
            
            var uv1 = new Vector2(outerUV.x, outerUV.y);
            var uv2 = new Vector2(innerUV.x, innerUV.y);
            var uv3 = new Vector2(innerUV.z, innerUV.w);
            var uv4 = new Vector2(outerUV.z * context.TopRightUVMultiplier.x, outerUV.w * context.TopRightUVMultiplier.y);

            var row1PositionUV = new Vector2(position1.y, uv1.y);
            var row2PositionUV = new Vector2(position2.y, uv2.y);
            var row3PositionUV = new Vector2(position3.y, uv3.y);
            var row4PositionUV = new Vector2(position4.y, uv4.y);

            var indexCount = 0;
            

            FillVertexColumn(vertices, ref indexCount, context.InnerTilesCount.y,
                context.InnerTileSize.y, new Vector2(position1.x, uv1.x),
                row1PositionUV, row2PositionUV, row3PositionUV, row4PositionUV);
            
            for (var i = 0; i < context.InnerTilesCount.x; i++)
            {
                var positionX = position2.x + i * context.InnerTileSize.x;
                FillVertexColumn(vertices, ref indexCount, context.InnerTilesCount.y,
                    context.InnerTileSize.y, new Vector2(positionX, uv2.x),
                    row1PositionUV, row2PositionUV, row3PositionUV, row4PositionUV);
                var columnX = positionX + context.InnerTileSize.x;
                if (tileScaledSlices && columnX > position3.x)
                {
                    columnX = position3.x;
                    var columnU = Mathf.Lerp(uv2.x, uv3.x, (position3.x - positionX) / context.InnerTileSize.x);
                    FillVertexColumn(vertices, ref indexCount, context.InnerTilesCount.y,
                        context.InnerTileSize.y, new Vector2(columnX, columnU),
                        row1PositionUV, row2PositionUV, row3PositionUV, row4PositionUV);
                }
                FillVertexColumn(vertices, ref indexCount, context.InnerTilesCount.y,
                    context.InnerTileSize.y, new Vector2(columnX, uv3.x),
                    row1PositionUV, row2PositionUV, row3PositionUV, row4PositionUV);
            }
            
            FillVertexColumn(vertices, ref indexCount, context.InnerTilesCount.y,
                context.InnerTileSize.y, new Vector2(position4.x, uv4.x),
                row1PositionUV, row2PositionUV, row3PositionUV, row4PositionUV);
        }

        private void FillVertexColumn(Span<CutInputVertex> vertices, ref int currentIndex, int innerCellsCount,
            float tileHeight, Vector2 columnPositionUV,
            Vector2 row1PositionUV, Vector2 row2PositionUV, Vector2 row3PositionUV, Vector2 row4PositionUV)
        {
            vertices[currentIndex++] = new CutInputVertex
            {
                Position = new Vector2(columnPositionUV.x, row1PositionUV.x),
                UV = new Vector2(columnPositionUV.y, row1PositionUV.y)
            };
            
            for (var i = 0; i < innerCellsCount; i++)
            {
                var positionY = row2PositionUV.x + i * tileHeight;
                vertices[currentIndex++] = new CutInputVertex
                {
                    Position = new Vector2(columnPositionUV.x, positionY),
                    UV = new Vector2(columnPositionUV.y, row2PositionUV.y)
                };
                var rowY = positionY + tileHeight;
                if (tileScaledSlices && rowY > row3PositionUV.x)
                {
                    rowY = row3PositionUV.x;
                    var rowU = Mathf.Lerp(row2PositionUV.y, row3PositionUV.y, (row3PositionUV.x - positionY) / tileHeight);
                    vertices[currentIndex++] = new CutInputVertex
                    {
                        Position = new Vector2(columnPositionUV.x, rowY),
                        UV = new Vector2(columnPositionUV.y, rowU)
                    };
                }
                vertices[currentIndex++] = new CutInputVertex
                {
                    Position = new Vector2(columnPositionUV.x, rowY),
                    UV = new Vector2(columnPositionUV.y, row3PositionUV.y)
                };
            }
            
            vertices[currentIndex++] = new CutInputVertex
            {
                Position = new Vector2(columnPositionUV.x, row4PositionUV.x),
                UV = new Vector2(columnPositionUV.y, row4PositionUV.y)
            };
        }
    }
}