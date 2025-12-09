using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace Utkaka.ScaleNineSlicer.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("UI/SlicedImage")]
    public class SlicedImage : MaskableGraphic,
        ILayoutElement,
        ICanvasRaycastFilter
    {
        [SerializeField]
        private Sprite _sprite;
        
        [SerializeField]
        private bool _preserveAspect;
        [SerializeField]
        private bool _useSpriteMesh;
        [SerializeField]
        private float _pixelsPerUnitMultiplier = 1.0f;

        [SerializeField]
        private bool _sliced;
        [SerializeField]
        private bool _fillCenter = true;
        [SerializeField]
        private bool _tileScaledSlices;
        [SerializeField]
        private Vector2Int _slicedTileSize;
        
        [SerializeField]
        private bool _tiled;
        [SerializeField]
        private Vector2Int _tileSize;
        [SerializeField]
        private Vector2Int _tileSpacing;
        
        [SerializeField]
        private bool _filled;
        [SerializeField]
        private Image.FillMethod _fillMethod;
        [SerializeField]
        private bool _fillClockwise = true;
        [SerializeField]
        private int _fillOrigin;
        [Range(0, 1)]
        [SerializeField]
        private float _fillAmount = 1.0f;
        
        private float _cachedReferencePixelsPerUnit = 100;
        private float _alphaHitTestMinimumThreshold;
        private Sprite _overrideSprite;
        private bool _tracked;

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
        
        public Sprite activeSprite => _overrideSprite != null ? _overrideSprite : sprite;
        
        public float pixelsPerUnitMultiplier
        {
            get => _pixelsPerUnitMultiplier;
            set
            {
                _pixelsPerUnitMultiplier = Mathf.Max(0.01f, value);
                SetVerticesDirty();
            }
        }

        public float pixelsPerUnit
        {
            get
            {
                float spritePixelsPerUnit = 100;
                if (activeSprite) spritePixelsPerUnit = activeSprite.pixelsPerUnit;
                if (canvas) _cachedReferencePixelsPerUnit = canvas.referencePixelsPerUnit;
                return spritePixelsPerUnit / _cachedReferencePixelsPerUnit;
            }
        }
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
        public bool sliced
        {
            get => _sliced;
            set { if (Utils.SetStruct(ref _sliced, value)) SetVerticesDirty(); }
        }
        
        public bool fillCenter
        {
            get => _fillCenter;
            set
            {
                if (Utils.SetStruct(ref _fillCenter, value)) SetVerticesDirty();
            }
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

        #region Tiled properties
        public bool tiled
        {
            get => _tiled;
            set { if (Utils.SetStruct(ref _tiled, value)) SetVerticesDirty(); }
        }
        
        public Vector2Int tileSize
        {
            get => _tileSize;
            set { if (Utils.SetStruct(ref _tileSize, value)) SetVerticesDirty(); }
        }
        
        public Vector2Int tileSpacing
        {
            get => _tileSpacing;
            set { if (Utils.SetStruct(ref _tileSpacing, value)) SetVerticesDirty(); }
        }

        #endregion

        #region Filled properties

        public bool filled
        {
            get => _filled;
            set { if (Utils.SetStruct(ref _filled, value)) SetVerticesDirty(); }
        }
        
        public int fillOrigin
        {
            get => _fillOrigin;
            set { if (Utils.SetStruct(ref _fillOrigin, value)) SetVerticesDirty(); }
        }
        
        public Image.FillMethod fillMethod
        {
            get => _fillMethod;
            set
            {
                if (Utils.SetStruct(ref _fillMethod, value))
                {
                    SetVerticesDirty();
                    _fillOrigin = 0;
                }
            }
        }

        public float fillAmount
        {
            get => _fillAmount;
            set
            {
                if (Utils.SetStruct(ref _fillAmount, Mathf.Clamp01(value))) SetVerticesDirty();
            }
        }
        
        public bool fillClockwise
        {
            get => _fillClockwise;
            set
            {
                if (Utils.SetStruct(ref _fillClockwise, value)) SetVerticesDirty();
            }
        }

        #endregion
        
        public float minWidth => 0.0f;
        public virtual float preferredWidth
        {
            get
            {
                if (activeSprite == null) return 0;
                if (_sliced || _filled) return DataUtility.GetMinSize(activeSprite).x / pixelsPerUnit;
                return activeSprite.rect.size.x / pixelsPerUnit;
            }
        }
        public float flexibleWidth => -1.0f;
        public float minHeight => 0.0f;
        public virtual float preferredHeight
        {
            get
            {
                if (activeSprite == null) return 0;
                if (_sliced || _filled) return DataUtility.GetMinSize(activeSprite).y / pixelsPerUnit;
                return activeSprite.rect.size.y / pixelsPerUnit;
            }
        }
        public float flexibleHeight => -1.0f;
        public int layoutPriority => 0;
        
        public float alphaHitTestMinimumThreshold
        {
            get => _alphaHitTestMinimumThreshold;
            set => _alphaHitTestMinimumThreshold = value;
        }

        public bool hasBorder
        {
            get
            {
                if (activeSprite == null) return false;
                var v = activeSprite.border;
                return v.sqrMagnitude > 0f;
            }
        }

        public float multipliedPixelsPerUnit => pixelsPerUnit * _pixelsPerUnitMultiplier;

        public override Texture mainTexture
        {
            get
            {
                if (activeSprite != null) return activeSprite.texture;
                if (material != null && material.mainTexture != null)
                {
                    return material.mainTexture;
                }
                return s_WhiteTexture;

            }
        }
        
        public override Material material
        {
            get
            {
                if (m_Material != null)
                    return m_Material;
#if UNITY_EDITOR
                if (Application.isPlaying && activeSprite && activeSprite.associatedAlphaSplitTexture != null)
                    return Image.defaultETC1GraphicMaterial;
#else
                if (activeSprite && activeSprite.associatedAlphaSplitTexture != null)
                    return Image.defaultETC1GraphicMaterial;
#endif
                return defaultMaterial;
            }
            set { base.material = value; }
        }

        protected SlicedImage()
        {
            useLegacyMeshGeneration = false;
        }
        
        public override void SetNativeSize()
        {
            if (activeSprite == null) return;
            var w = activeSprite.rect.width / pixelsPerUnit;
            var h = activeSprite.rect.height / pixelsPerUnit;
            rectTransform.anchorMax = rectTransform.anchorMin;
            rectTransform.sizeDelta = new Vector2(w, h);
            SetAllDirty();
        }
        
        public void DisableSpriteOptimizations()
        {
            m_SkipLayoutUpdate = false;
            m_SkipMaterialUpdate = false;
        }

        public virtual void CalculateLayoutInputHorizontal() { }

        public virtual void CalculateLayoutInputVertical() { }
        
        public virtual bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            //TODO: Make proper point to texture mapping with alpha testing
            return alphaHitTestMinimumThreshold <= 1.0f;
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
            /*if (totalVertexCount > 65000)
            {
                Debug.LogError($"Too many vertices per tile {totalVertexCount}");
                return;
            }*/

            var heapArray = Utils.GetFromPoolIfNeeded<CutInputVertex>(totalVertexCount);
            var vertices = heapArray == null ? stackalloc CutInputVertex[totalVertexCount] : heapArray.AsSpan();
            
            FillBaseVertices(vertices, context);

            var polygonsCount = GetPolygonsCount();
            for (var polygonIndex = 0; polygonIndex < polygonsCount; polygonIndex++)
            {
                PreparePolygon(polygonIndex, context, vertices, toFill);
            }

            if (heapArray != null)
            {
                System.Buffers.ArrayPool<CutInputVertex>.Shared.Return(heapArray);
            }
        }

        private void PreparePolygon(int polygonIndex, SlicedImageMeshContext context, Span<CutInputVertex> vertices, 
            VertexHelper toFill)
        {
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
                        vertex.Position = RectTransformUtility.PixelAdjustPoint(vertex.Position + tileShift, rectTransform, canvas);
                        tileVertices[v] = vertex;
                    }
                    SlicedMeshHandler.PrepareMesh(tileVertices, cuts, toFill, color, context.VertexCountPerTile.x, context.VertexCountPerTile.y, !fillCenter);
                }
            }

            if (cutsHeapArray != null)
            {
                System.Buffers.ArrayPool<CutLine>.Shared.Return(cutsHeapArray);
            }
            if (tileVerticesHeapArray != null)
            {
                System.Buffers.ArrayPool<CutInputVertex>.Shared.Return(tileVerticesHeapArray);
            }
        }

        protected virtual int GetPolygonsCount()
        {
            if (!filled || fillMethod != Image.FillMethod.Radial360) return 1;
            return fillAmount <= 0.5f || Mathf.Approximately(fillAmount, 1.0f) ? 1 : 2;
        }

        protected virtual int GetPolygonCutLinesCount(int polygonIndex, bool cutTilesX, bool cutTilesY)
        {
            var baseCutLineCount = 0;
            if (cutTilesX) baseCutLineCount++;
            if (cutTilesY) baseCutLineCount++;
            if (!filled || Mathf.Approximately(fillAmount, 1.0f)) return baseCutLineCount;
            if (fillMethod != Image.FillMethod.Radial360 || polygonIndex == 0 && fillAmount >= 0.5f) return baseCutLineCount + 1;
            return baseCutLineCount + 2;
        }


        protected virtual void FillPolygonCutLines(Span<CutLine> cutLines, Rect rect, int polygonIndex, bool cutTilesX, bool cutTilesY)
        {
            var cutLineIndex = 0;
            if (cutTilesX)
            {
                cutLines[cutLineIndex++] = CutLine.FromLine(new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMax, rect.yMin));
            }
            if (cutTilesY)
            {
                cutLines[cutLineIndex] = CutLine.FromLine(new Vector2(rect.xMin, rect.yMax), new Vector2(rect.xMax, rect.yMax));
            }
            if (!filled || Mathf.Approximately(fillAmount, 1.0f)) return;
            switch (fillMethod)
            {
                case Image.FillMethod.Horizontal:
                    FillHorizontalCutLine(cutLines, rect);
                    break;
                case Image.FillMethod.Vertical:
                    FillVerticalCutLine(cutLines, rect);
                    break;
                case Image.FillMethod.Radial90:
                    FillRadial90CutLine(cutLines, rect, fillOrigin, fillAmount, fillClockwise);
                    break;
                case Image.FillMethod.Radial180:
                    FillRadial180CutLine(cutLines, rect, fillOrigin, fillAmount, fillClockwise);
                    break;
                case Image.FillMethod.Radial360:
                    FillRadial360CutLine(cutLines, rect, polygonIndex, fillOrigin, fillAmount, fillClockwise);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void FillHorizontalCutLine(Span<CutLine> cutLines, Rect rect)
        {
            var fillX = rect.width * fillAmount;
            var normal = Vector2.left;
            if ((fillOrigin & 1) == 1)
            {
                fillX = rect.width - fillX;
                normal = Vector2.right;
            }
            fillX += rect.x;
            cutLines[^1] = new CutLine(new Vector2(fillX, 0.0f), normal);
        }
        
        private void FillVerticalCutLine(Span<CutLine> cutLines, Rect rect)
        {
            var fillY = rect.height * fillAmount;
            var normal = Vector2.down;
            if ((fillOrigin & 1) == 1)
            {
                fillY = rect.height - fillY;
                normal = Vector2.up;
            }
            fillY += rect.y;
            cutLines[^1] = new CutLine(new Vector2(0.0f, fillY), normal);
        }

        private void FillRadial360CutLine(Span<CutLine> cutLines, Rect rect, int polygonIndex, int side, float amount, bool clockwise)
        {
            if (polygonIndex > 0 && amount <= 0.5f) return;
            var isFirstHalf = polygonIndex == 0;
            
            var halfRectSize = rect.size * 0.5f;
            var fill = amount * 2.0f - (isFirstHalf ? 0.0f : 1.0f);
            Vector2 halfShift;
            CutLine polygonCutLine;
            Rect halfRect;
            int halfSide;
            
            if ((side & 1) == 0)
            {
                halfShift = new Vector2(halfRectSize.x, 0.0f);
                halfRectSize.y = rect.size.y;
                if (side is 0 && (isFirstHalf && !clockwise || !isFirstHalf && clockwise) || 
                    side is 2 && (!isFirstHalf && !clockwise || isFirstHalf && clockwise))
                {
                    halfRect = new Rect(rect.position + halfShift, halfRectSize);
                    halfSide = 1;
                    polygonCutLine = new CutLine(rect.center, Vector2.right);
                }
                else
                {
                    halfRect = new Rect(rect.position, halfRectSize);
                    halfSide = 3;
                    polygonCutLine = new CutLine(rect.center, Vector2.left);
                }
            }
            else
            {
                halfShift = new Vector2(0.0f, halfRectSize.y);
                halfRectSize.x = rect.size.x;
                if (side is 1 && (isFirstHalf && !clockwise || !isFirstHalf && clockwise) || 
                    side is 3 && (!isFirstHalf && !clockwise || isFirstHalf && clockwise))
                {
                    halfRect = new Rect(rect.position + halfShift, halfRectSize);
                    halfSide = 0;
                    polygonCutLine = new CutLine(rect.center, Vector2.up);
                }
                else
                {
                    halfRect = new Rect(rect.position, halfRectSize);
                    halfSide = 2;
                    polygonCutLine = new CutLine(rect.center, Vector2.down);
                }
            }

            if (isFirstHalf && amount >= 0.5f)
            {
                cutLines[^1] = polygonCutLine;
                return;
            }
            
            FillRadial180CutLine(cutLines, halfRect, halfSide, fill, clockwise);
            cutLines[^2] = polygonCutLine;
        }

        private static void FillRadial180CutLine(Span<CutLine> cutLines, Rect rect, int side, float amount, bool clockwise)
        {
            var isFirstHalf = amount <= 0.5f;
            var halfRectSize = rect.size * 0.5f;
            var fill = amount * 2.0f - (isFirstHalf ? 0.0f : 1.0f);
            Vector2 halfShift;
            if ((side & 1) == 0)
            {
                halfShift = new Vector2(halfRectSize.x, 0.0f);
                halfRectSize.y = rect.size.y;
            }
            else
            {
                halfShift = new Vector2(0.0f, halfRectSize.y);
                halfRectSize.x = rect.size.x;
            }
            Rect halfRect;
            int corner;
            if (side is 0 or 3 && (isFirstHalf && !clockwise || !isFirstHalf && clockwise) || 
                side is 2 or 1 && (!isFirstHalf && !clockwise || isFirstHalf && clockwise))
            {
                halfRect = new Rect(rect.position + halfShift, halfRectSize);
                corner = side switch
                {
                    0 => 0,
                    1 => 0,
                    2 => 1,
                    3 => 3,
                    _ => 0
                };
            }
            else
            {
                halfRect = new Rect(rect.position, halfRectSize);
                corner = side switch
                {
                    0 => 3,
                    1 => 1,
                    2 => 2,
                    3 => 2,
                    _ => 0
                };
            }
            FillRadial90CutLine(cutLines, halfRect, corner, fill, clockwise);
        }
        
        private static void FillRadial90CutLine(Span<CutLine> cutLines, Rect rect, int origin, float amount, bool clockwise)
        {
            
            var vertices = new float2x4
            {
                [0] = rect.position,
                [1] = new Vector2(rect.xMin, rect.yMax),
                [2] = new Vector2(rect.xMax, rect.yMax),
                [3] = new Vector2(rect.xMax, rect.yMin)
            };

            var center = vertices[origin];
            var corner = vertices[(origin + 2) % 4];
            var fill = (origin & 1) == 1 ? 1.0f - amount : amount;
            if (clockwise)
            {
                fill = 1.0f - fill;
            }
            var intersection = GetRadialIntersection(center, corner, fill);
            cutLines[^1] = clockwise ? CutLine.FromLine(intersection, center) : CutLine.FromLine(center, intersection);
        }
        
        private static Vector2 GetRadialIntersection(Vector2 center, Vector2 corner, float fill)
        {
            var angle = fill * 0.5f * Mathf.PI;
            var cos = Mathf.Cos(angle);
            var sin = Mathf.Sin(angle);

            var result = Vector2.zero;

            if (cos > sin)
            {
                result.x = corner.x;
                result.y = Mathf.Lerp(center.y, corner.y, sin / cos);
            }
            else if (sin > cos)
            {
                result.x = Mathf.Lerp(center.x, corner.x, cos / sin);
                result.y = corner.y;
            }
            else
            {
                result.x = corner.x;
                result.y = corner.y;
            }

            return result;
        }

        private void PopulateBaseVertices(Rect originalRect, Rect adjustedRect, out float2x4 positions, out float2x4 uv, out int verticalVertexCount, out int horizontalVertexCount)
        {
            var adjustedPixelsPerUnit = multipliedPixelsPerUnit;
            var outerUV = DataUtility.GetOuterUV(activeSprite);
            var innerUV = DataUtility.GetInnerUV(activeSprite);
            var padding = DataUtility.GetPadding(activeSprite) / adjustedPixelsPerUnit;
            var position = (float2)adjustedRect.position;
            var adjustedBorders = Utils.GetAdjustedBorders(activeSprite.border / adjustedPixelsPerUnit, originalRect, adjustedRect);
            
            positions = float2x4.zero;
            positions[0] = new float2(padding.x, padding.y) + position;
            positions[3] = new float2(adjustedRect.width - padding.z, adjustedRect.height - padding.w) + position;
            positions[1] = new float2(adjustedBorders.x, adjustedBorders.y) + position;
            positions[2] = new float2(adjustedRect.width - adjustedBorders.z, adjustedRect.height - adjustedBorders.w) + position;

            uv = float2x4.zero;
            uv[0] = new float2(outerUV.x, outerUV.y);
            uv[1] = new float2(innerUV.x, innerUV.y);
            uv[2] = new float2(innerUV.z, innerUV.w);
            uv[3] = new float2(outerUV.z, outerUV.w);
            
            var canSimplifyMesh = fillCenter && Mathf.Approximately(adjustedPixelsPerUnit, 1.0f);

            if (sliced && hasBorder)
            {   
                verticalVertexCount = Mathf.Approximately(adjustedRect.height, activeSprite.rect.height) && canSimplifyMesh ? 2 : 4;
                horizontalVertexCount = Mathf.Approximately(adjustedRect.width, activeSprite.rect.width) && canSimplifyMesh ? 2 : 4;
            }
            else
            {
                verticalVertexCount = 2;
                horizontalVertexCount = 2;
            }
        }
        
        private void FillBaseVertices(Span<CutInputVertex> vertices, SlicedImageMeshContext context)
        {
            var adjustedPixelsPerUnit = multipliedPixelsPerUnit;
            var outerUV = DataUtility.GetOuterUV(activeSprite);
            var innerUV = DataUtility.GetInnerUV(activeSprite);
            var padding = DataUtility.GetPadding(activeSprite) / adjustedPixelsPerUnit;
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

        private void FillBaseVertices(Span<CutInputVertex> vertices, float2x4 positions, float2x4 uv,
            int verticalVertexCount, int horizontalVertexCount)
        {
            var index = 0;
            for (var x = 0; x < 4; x++)
            {
                for (var y = 0; y < 4; y++)
                {
                    var vertex = new CutInputVertex
                    {
                        Position = new Vector2(positions[x].x, positions[y].y),
                        UV = new Vector2(uv[x].x, uv[y].y)
                    };
                    vertices[index++] = vertex;
                    if (verticalVertexCount == 2) y += 2;
                }
                if (horizontalVertexCount == 2) x += 2;
            }
        }
        
        protected override void UpdateMaterial()
        {
            base.UpdateMaterial();
            if (activeSprite == null)
            {
                canvasRenderer.SetAlphaTexture(null);
                return;
            }

            var alphaTex = activeSprite.associatedAlphaSplitTexture;
            if (alphaTex != null)
            {
                canvasRenderer.SetAlphaTexture(alphaTex);
            }
        }
        
        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            if (canvas == null)
            {
                _cachedReferencePixelsPerUnit = 100;
            }
            else if (!Mathf.Approximately(canvas.referencePixelsPerUnit, _cachedReferencePixelsPerUnit))
            {
                _cachedReferencePixelsPerUnit = canvas.referencePixelsPerUnit;
                if (_sliced || _tiled)
                {
                    SetVerticesDirty();
                    SetLayoutDirty();
                }
            }
        }
        
        protected override void OnDidApplyAnimationProperties()
        {
            SetMaterialDirty();
            SetVerticesDirty();
            SetRaycastDirty();
        }
        
        protected override void OnEnable()
        {
            base.OnEnable();
            TrackSprite();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_tracked) SlicedImageAtlasTracker.UnTrackImage(this);
        }
        
        private void TrackSprite()
        {
            if (activeSprite == null || activeSprite.texture != null) return;
            SlicedImageAtlasTracker.TrackImage(this);
            _tracked = true;
        }
        
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            _pixelsPerUnitMultiplier = Mathf.Max(0.01f, _pixelsPerUnitMultiplier);
        }
#endif
    }
}