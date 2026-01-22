using System;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace Utkaka.ScaleNineSlicer.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("UI/RoundedImage")]
    public class RoundedImage : ExtendedImage
    {
        public enum Type
        {
            Horizontal = 0,
            Vertical = 1,
            Circle = 2,
            RoundedRect = 3
        }

        [SerializeField]
        private RoundedSprite _roundedSprite;

        [SerializeField]
        private Type _sliceType;
        [SerializeField]
        [Range(0, 30)]
        private int _sliceApproximationSteps = 10;
        [SerializeField]
        private RoundedImageCornerSettings _bottomLeftCorner;
        [SerializeField]
        private RoundedImageCornerSettings _topLeftCorner;
        [SerializeField]
        private RoundedImageCornerSettings _topRightCorner;
        [SerializeField]
        private RoundedImageCornerSettings _bottomRightCorner;
        
        #region Base properties
        public RoundedSprite roundedSprite
        {
            get => _roundedSprite;
            set
            {
                if (_roundedSprite != null)
                {
                    if (_roundedSprite == value) return;
                    var oldRectSize = _roundedSprite?.Sprite != null ? _roundedSprite.Sprite.rect.size : Vector2.zero;
                    var newRectSize = value?.Sprite != null ? value.Sprite.rect.size : Vector2.zero;
                    m_SkipLayoutUpdate = oldRectSize.Equals(value ? newRectSize : Vector2.zero);
                    m_SkipMaterialUpdate = _roundedSprite?.Sprite.texture == (value ? value.Sprite?.texture : null);
                    _roundedSprite = value;
                    SetAllDirty();
                    TrackSprite();
                }
                else if (value != null)
                {
                    var oldRectSize = _roundedSprite?.Sprite != null ? _roundedSprite.Sprite.rect.size : Vector2.zero;
                    m_SkipLayoutUpdate = oldRectSize == Vector2.zero;
                    m_SkipMaterialUpdate = value.Sprite?.texture == null;
                    _roundedSprite = value;

                    SetAllDirty();
                    TrackSprite();
                }
            }
        }
        #endregion
        
        #region Sliced properties
        public override bool sliced
        {
            get => true;
            set { }
        }
        public int sliceApproximationSteps
        {
            get => _sliceApproximationSteps;
            set
            {
                if (Utils.SetStruct(ref _sliceApproximationSteps, Mathf.Max(0, value))) SetVerticesDirty();
            }
        }
        
        public Type sliceType
        {
            get => _sliceType;
            set
            {
                if (Utils.SetStruct(ref _sliceType, value)) SetVerticesDirty();
            }
        }
        
        public RoundedImageCornerSettings bottomLeftCorner
        {
            get => _bottomLeftCorner;
            set
            {
                value.ApproximationSteps = Mathf.Max(0, value.ApproximationSteps);
                value.Radius = Mathf.Max(0, value.Radius);
                if (Utils.SetStruct(ref _bottomLeftCorner, value)) SetVerticesDirty();
            }
        }
        
        public RoundedImageCornerSettings topLeftCorner
        {
            get => _topLeftCorner;
            set
            {
                value.ApproximationSteps = Mathf.Max(0, value.ApproximationSteps);
                value.Radius = Mathf.Max(0, value.Radius);
                if (Utils.SetStruct(ref _topLeftCorner, value)) SetVerticesDirty();
            }
        }
        
        public RoundedImageCornerSettings topRightCorner
        {
            get => _topRightCorner;
            set
            {
                value.ApproximationSteps = Mathf.Max(0, value.ApproximationSteps);
                value.Radius = Mathf.Max(0, value.Radius);
                if (Utils.SetStruct(ref _topRightCorner, value)) SetVerticesDirty();
            }
        }
        
        public RoundedImageCornerSettings bottomRightCorner
        {
            get => _bottomRightCorner;
            set
            {
                value.ApproximationSteps = Mathf.Max(0, value.ApproximationSteps);
                value.Radius = Mathf.Max(0, value.Radius);
                if (Utils.SetStruct(ref _bottomRightCorner, value)) SetVerticesDirty();
            }
        }
        

        #endregion
        
        public override Sprite activeSprite => roundedSprite?.Sprite;
        
        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            if (activeSprite == null)
            {
                base.OnPopulateMesh(toFill);
                return;
            }
            toFill.Clear();

            var context = new RoundedImageMeshContext(this);
            var vertexCount = context.OuterVertexCount * 2 + 1;
            
            var heapArray = Utils.GetFromPoolIfNeeded<CutInputVertex>(vertexCount);
            var vertices = heapArray == null ? stackalloc CutInputVertex[vertexCount] : heapArray.AsSpan(0, vertexCount);
            
            FillBaseVertices(vertices, in context);

            var polygonsCount = GetPolygonsCount();
            var meshVertexCount = 0;
            for (var polygonIndex = 0; polygonIndex < polygonsCount; polygonIndex++)
            {
                PreparePolygon(polygonIndex, in context, vertices, toFill, ref meshVertexCount);
            }
            
            Utils.ReturnToPool(vertices.Length, heapArray);
        }

        private void PreparePolygon(int polygonIndex, in RoundedImageMeshContext context, Span<CutInputVertex> vertices, 
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

                    SlicedMeshHandler.PrepareCapsuleMesh(tileVertices, cuts, toFill, Color.white, 3, context.OuterVertexCount, !fillCenter,
                        ref meshVertexCount);
                }
            }
            
            Utils.ReturnToPool(cuts.Length, cutsHeapArray);
            Utils.ReturnToPool(tileVertices.Length, tileVerticesHeapArray);
        }
        
        private void FillBaseVertices(Span<CutInputVertex> vertices, in RoundedImageMeshContext context)
        {
            var localActiveSprite = activeSprite;
            var adjustedPixelsPerUnit = context.MultipliedPixelsPerUnit;
            var outerUV = DataUtility.GetOuterUV(localActiveSprite);
            var padding = DataUtility.GetPadding(localActiveSprite);
            var position = context.FullRect.position;
            
            var capsuleRect = new Rect(Vector2.zero, context.CapsuleSize);
            capsuleRect.center = position + context.TileSize / 2.0f;
            
            var uv1 = new Vector2(outerUV.x, outerUV.y) - new Vector2(padding.x, padding.y) / context.SpriteSize;
            var uv4 = new Vector2(outerUV.z, outerUV.w) + new Vector2(padding.z, padding.w) / context.SpriteSize;
            
            var indexCount = 0;
            var innerCenterOnSprite = roundedSprite.InnerCenter;
            var outerCenterOnSprite = roundedSprite.OuterCenter;
            
            //Bottom left quarter
            FillQuarter(vertices, ref indexCount, context.BottomLeftQuarter, uv1, uv4, context.SpriteSize, -Mathf.PI * 0.5f, context.OuterVertexCount, position);
            
            //Top left quarter
            FillQuarter(vertices, ref indexCount, context.TopLeftQuarter, uv1, uv4, context.SpriteSize, -Mathf.PI, context.OuterVertexCount, position);
            
            //Top right quarter
            FillQuarter(vertices, ref indexCount, context.TopRightQuarter, uv1, uv4, context.SpriteSize, Mathf.PI * 0.5f, context.OuterVertexCount, position);
            
            //Bottom right quarter
            FillQuarter(vertices, ref indexCount, context.BottomRightQuarter, uv1, uv4, context.SpriteSize, 0.0f, context.OuterVertexCount, position);
            
            //Close circle
            vertices[context.OuterVertexCount - 1] = vertices[0];
            vertices[context.OuterVertexCount * 2 - 1] = vertices[context.OuterVertexCount];
            
            //Center point
            vertices[^1] = VertexFromAngle(Vector2.zero, 0.0f, innerCenterOnSprite, 0.0f,
                capsuleRect.center + (innerCenterOnSprite - outerCenterOnSprite) * context.ScaleRatio, context.SpriteSize, uv1, uv4);
        }

        private void FillQuarter(Span<CutInputVertex> vertices, ref int indexCount, RoundedImageQuarterData quarterData,
            Vector2 uv1, Vector2 uv4, Vector2 spriteSize, float startAngle, int outerVertexCount, Vector2 position)
        {
            var outerRadiusOnSprite = roundedSprite.OuterRadius;
            var innerRadiusOnSprite = roundedSprite.InnerRadius;
            var outerCenterOnSprite = roundedSprite.OuterCenter;
            var innerCenterOnSprite = roundedSprite.InnerCenter;

            var stepAngle = Mathf.PI / (2 * quarterData.ApproximationSteps);
            for (var i = 0; i < quarterData.OuterVertexCount; i++)
            {
                var angle = -i * stepAngle + startAngle;
                var sin = Mathf.Sin(angle);
                var cos = Mathf.Cos(angle);
                var circlePosition = new Vector2(cos, sin);
                vertices[indexCount + outerVertexCount] = VertexFromAngle(circlePosition, innerRadiusOnSprite,
                    innerCenterOnSprite,
                    quarterData.InnerRadius,
                    quarterData.InnerCenter + position, spriteSize, uv1, uv4);

                vertices[indexCount++] = VertexFromAngle(circlePosition, outerRadiusOnSprite, outerCenterOnSprite,
                    quarterData.OuterRadius,
                    quarterData.OuterCenter + position, spriteSize, uv1, uv4);
            }
        }

        private CutInputVertex VertexFromAngle(Vector2 circlePosition, float spriteRadius, Vector2 spriteOffset, float rectRadius,
            Vector2 rectOffset, Vector2 spriteSize, Vector2 uv1, Vector2 uv2)
        {
            var pointOnSprite = circlePosition * spriteRadius + spriteOffset + spriteSize * 0.5f;
            var pointRatio = pointOnSprite / spriteSize;
            var uv = pointRatio * (uv2 - uv1) + uv1;

            var pointOnRect = circlePosition * rectRadius + rectOffset;

            return new CutInputVertex
            {
                Position = pointOnRect,
                UV = uv
            };
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            _sliceApproximationSteps = Mathf.Max(0, _sliceApproximationSteps);
            _bottomLeftCorner.ApproximationSteps = Mathf.Max(0, _bottomLeftCorner.ApproximationSteps);
            _bottomLeftCorner.Radius = Mathf.Max(0, _bottomLeftCorner.Radius);
            _topLeftCorner.ApproximationSteps = Mathf.Max(0, _topLeftCorner.ApproximationSteps);
            _topLeftCorner.Radius = Mathf.Max(0, _topLeftCorner.Radius);
            _topRightCorner.ApproximationSteps = Mathf.Max(0, _topRightCorner.ApproximationSteps);
            _topRightCorner.Radius = Mathf.Max(0, _topRightCorner.Radius);
            _bottomRightCorner.ApproximationSteps = Mathf.Max(0, _bottomRightCorner.ApproximationSteps);
            _bottomRightCorner.Radius = Mathf.Max(0, _bottomRightCorner.Radius);
        }
#endif
    }
}