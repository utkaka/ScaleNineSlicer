using System;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace Utkaka.ScaleNineSlicer.UI
{
    public abstract class ExtendedImage: MaskableGraphic,
        ILayoutElement,
        ICanvasRaycastFilter
    {
        public enum FillMethod
        {
            Horizontal = 0,
            Vertical = 1,
            Radial90 = 2,
            Radial180 = 3,
            Radial360 = 4,
            Custom = 5
        }
        
        [SerializeField]
        private float _pixelsPerUnitMultiplier = 1.0f;
        
        [SerializeField]
        private bool _fillCenter = true;

        [SerializeField]
        private bool _tiled;
        [SerializeField]
        private Vector2Int _tileSize;
        [SerializeField]
        private Vector2Int _tileSpacing;
        
        [SerializeField]
        private bool _filled;
        [SerializeField]
        private FillMethod _fillMethod;
        [SerializeField]
        private bool _fillClockwise = true;
        [SerializeField]
        private int _fillOrigin;
        [Range(0, 1)]
        [SerializeField]
        private float _fillAmount = 1.0f;
        [SerializeField]
        private SlicedImageCustomFilling _customFilling;
        
        private float _cachedReferencePixelsPerUnit = 100;
        private float _alphaHitTestMinimumThreshold;
        private bool _tracked;

        #region Base properties
        
        public abstract Sprite activeSprite { get; }
        
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
        
        #region Sliced properties

        public abstract bool sliced { get; set; }

        public bool fillCenter
        {
            get => _fillCenter;
            set
            {
                if (Utils.SetStruct(ref _fillCenter, value)) SetVerticesDirty();
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
        
        public FillMethod fillMethod
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
        
        public SlicedImageCustomFilling customFilling
        {
            get => _customFilling;
            set
            {
                if (Utils.SetClass(ref _customFilling, value) && filled) SetVerticesDirty();
            }
        }

        #endregion
        
        public float minWidth => 0.0f;
        public virtual float preferredWidth
        {
            get
            {
                if (activeSprite == null) return 0;
                if (sliced || _filled) return DataUtility.GetMinSize(activeSprite).x / pixelsPerUnit;
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
                if (sliced || _filled) return DataUtility.GetMinSize(activeSprite).y / pixelsPerUnit;
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

        protected ExtendedImage()
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
        
        public new Rect GetPixelAdjustedRect()
        {
            var localCanvas = canvas;
            if (!localCanvas || localCanvas.renderMode == RenderMode.WorldSpace || localCanvas.scaleFactor == 0.0f || !localCanvas.pixelPerfect)
                return rectTransform.rect;
            return RectTransformUtility.PixelAdjustRect(rectTransform, localCanvas);
        }

        protected int GetPolygonsCount()
        {
            if (fillMethod == FillMethod.Custom && customFilling != null)
                return customFilling.GetPolygonsCount(fillAmount);
            if (!filled || fillMethod != FillMethod.Radial360) return 1;
            return fillAmount <= 0.5f || Mathf.Abs(fillAmount - 1.0f) <= Mathf.Epsilon ? 1 : 2;
        }

        protected int GetPolygonCutLinesCount(int polygonIndex, bool cutTilesX, bool cutTilesY)
        {
            var baseCutLineCount = 0;
            if (cutTilesX) baseCutLineCount++;
            if (cutTilesY) baseCutLineCount++;
            if (fillMethod == FillMethod.Custom)
            {
                return customFilling == null
                    ? baseCutLineCount
                    : customFilling.GetPolygonCutLinesCount(polygonIndex, fillAmount) + baseCutLineCount;
            }
            if (!filled || Mathf.Abs(fillAmount - 1.0f) <= Mathf.Epsilon) return baseCutLineCount;
            if (fillMethod != FillMethod.Radial360 || polygonIndex == 0 && fillAmount >= 0.5f) return baseCutLineCount + 1;
            return baseCutLineCount + 2;
        }


        protected void FillPolygonCutLines(Span<CutLine> cutLines, Rect rect, int polygonIndex, bool cutTilesX, bool cutTilesY)
        {
            var lineIndex = 0;
            if (cutTilesX)
            {
                cutLines[lineIndex++] = new CutLine(rect.max, Vector2.left);
            }
            if (cutTilesY)
            {
                cutLines[lineIndex++] = new CutLine(rect.max, Vector2.down);
            }
            if (!filled || Mathf.Abs(fillAmount - 1.0f) <= Mathf.Epsilon) return;
            switch (fillMethod)
            {
                case FillMethod.Horizontal:
                    FillHorizontalCutLine(cutLines[lineIndex..], rect);
                    break;
                case FillMethod.Vertical:
                    FillVerticalCutLine(cutLines[lineIndex..], rect);
                    break;
                case FillMethod.Radial90:
                    FillRadial90CutLine(cutLines[lineIndex..], rect, fillOrigin, fillAmount, fillClockwise);
                    break;
                case FillMethod.Radial180:
                    FillRadial180CutLine(cutLines[lineIndex..], rect, fillOrigin, fillAmount, fillClockwise);
                    break;
                case FillMethod.Radial360:
                    FillRadial360CutLine(cutLines[lineIndex..], rect, polygonIndex, fillOrigin, fillAmount, fillClockwise);
                    break;
                case FillMethod.Custom:
                    customFilling?.FillPolygonCutLines(cutLines[lineIndex..], fillAmount, rect, polygonIndex);
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
            cutLines[0] = new CutLine(new Vector2(fillX, 0.0f), normal);
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
            cutLines[0] = new CutLine(new Vector2(0.0f, fillY), normal);
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
                cutLines[0] = polygonCutLine;
                return;
            }
            
            FillRadial180CutLine(cutLines, halfRect, halfSide, fill, clockwise);
            cutLines[1] = polygonCutLine;
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
            origin %= 4;
            var center = Vector2.zero;
            var corner = Vector2.zero;

            switch (origin)
            {
                case 0:
                    center = rect.position;
                    corner = new Vector2(rect.xMax, rect.yMax);
                    break;
                case 1:
                    center =  new Vector2(rect.xMin, rect.yMax);
                    corner = new Vector2(rect.xMax, rect.yMin);
                    break;
                case 2:
                    center = new Vector2(rect.xMax, rect.yMax);
                    corner = rect.position;
                    break;
                case 3:
                    center = new Vector2(rect.xMax, rect.yMin);
                    corner = new Vector2(rect.xMin, rect.yMax);
                    break;
            }

            var fill = (origin & 1) == 1 ? 1.0f - amount : amount;
            if (clockwise)
            {
                fill = 1.0f - fill;
            }
            var intersection = GetRadialIntersection(center, corner, fill);
            cutLines[0] = clockwise ? CutLine.FromLine(intersection, center) : CutLine.FromLine(center, intersection);
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
            else if (!(Mathf.Abs(canvas.referencePixelsPerUnit - _cachedReferencePixelsPerUnit) <= Mathf.Epsilon))
            {
                _cachedReferencePixelsPerUnit = canvas.referencePixelsPerUnit;
                if (sliced || _tiled)
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
            if (_tracked) ExtendedImageAtlasTracker.UnTrackImage(this);
        }
        
        protected void TrackSprite()
        {
            if (activeSprite == null || activeSprite.texture != null) return;
            ExtendedImageAtlasTracker.TrackImage(this);
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