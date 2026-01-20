using System;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace Utkaka.ScaleNineSlicer.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("UI/CapsuleImage")]
    public class CapsuleImage : ExtendedImage
    {
        public enum Type
        {
            Horizontal = 0,
            Vertical = 1,
            Circle = 2,
            RoundedRect = 3
        }
        
        [SerializeField]
        private CapsuleSprite _capsuleSprite;

        [SerializeField]
        private int _sliceApproximationSteps;
        [SerializeField]
        private Type _sliceType;
        [SerializeField]
        public Vector4 _cornerRadius;
        
        #region Base properties
        public CapsuleSprite capsuleSprite
        {
            get => _capsuleSprite;
            set
            {
                if (_capsuleSprite != null)
                {
                    if (_capsuleSprite == value) return;
                    var oldRectSize = _capsuleSprite?.Sprite != null ? _capsuleSprite.Sprite.rect.size : Vector2.zero;
                    var newRectSize = value?.Sprite != null ? value.Sprite.rect.size : Vector2.zero;
                    m_SkipLayoutUpdate = oldRectSize.Equals(value ? newRectSize : Vector2.zero);
                    m_SkipMaterialUpdate = _capsuleSprite?.Sprite.texture == (value ? value.Sprite?.texture : null);
                    _capsuleSprite = value;
                    SetAllDirty();
                    TrackSprite();
                }
                else if (value != null)
                {
                    var oldRectSize = _capsuleSprite?.Sprite != null ? _capsuleSprite.Sprite.rect.size : Vector2.zero;
                    m_SkipLayoutUpdate = oldRectSize == Vector2.zero;
                    m_SkipMaterialUpdate = value.Sprite?.texture == null;
                    _capsuleSprite = value;

                    SetAllDirty();
                    TrackSprite();
                }
            }
        }
        #endregion
        
        #region Sliced properties
        public int sliceApproximationSteps
        {
            get => _sliceApproximationSteps;
            set
            {
                if (Utils.SetStruct(ref _sliceApproximationSteps, value)) SetVerticesDirty();
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

        #endregion
        
        public override Sprite activeSprite => _capsuleSprite?.Sprite;
        
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


            var context = new CapsuleImageMeshContext(this);
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

        private void PreparePolygon(int polygonIndex, in CapsuleImageMeshContext context, Span<CutInputVertex> vertices, 
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
        
        private void FillBaseVertices(Span<CutInputVertex> vertices, in CapsuleImageMeshContext context)
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
            var innerCenterOnSprite = capsuleSprite.InnerCenter;
            var outerCenterOnSprite = capsuleSprite.OuterCenter;
            
            //Bottom left quarter
            FillQuarter(vertices, ref indexCount, uv1, uv4, capsuleRect.min, capsuleRect.center, context.CornerRadius.x,
                context.CloseVerticalQuarter, context.OuterVertexCount, context.SpriteSize, -Mathf.PI * 0.5f);
            
            //Top left quarter
            FillQuarter(vertices, ref indexCount, uv1, uv4, new Vector2(capsuleRect.xMin, capsuleRect.yMax), capsuleRect.center,
                context.CornerRadius.y, context.CloseHorizontalQuarter, context.OuterVertexCount, context.SpriteSize,
                -Mathf.PI);
            
            //Top right quarter
            FillQuarter(vertices, ref indexCount, uv1, uv4, capsuleRect.max, capsuleRect.center, context.CornerRadius.z,
                context.CloseVerticalQuarter, context.OuterVertexCount, context.SpriteSize, Mathf.PI * 0.5f);
            
            //Bottom right quarter
            FillQuarter(vertices, ref indexCount, uv1, uv4, new Vector2(capsuleRect.xMax, capsuleRect.yMin), capsuleRect.center,
                context.CornerRadius.w, context.CloseHorizontalQuarter, context.OuterVertexCount, context.SpriteSize,
                0.0f);
            
            //Close circle
            vertices[context.OuterVertexCount - 1] = vertices[0];
            vertices[context.OuterVertexCount * 2 - 1] = vertices[context.OuterVertexCount];
            
            //Center point
            vertices[^1] = VertexFromAngle(Vector2.zero, 0.0f, innerCenterOnSprite, 0.0f,
                capsuleRect.center + innerCenterOnSprite - outerCenterOnSprite, context.SpriteSize, uv1, uv4);
        }

        private void FillQuarter(Span<CutInputVertex> vertices, ref int indexCount, Vector2 uv1, Vector2 uv4,
            Vector2 corner, Vector2 center, float outerRadiusOnRect, bool addEndPoints, int outerVertexCount,
            Vector2 spriteSize, float startAngle)
        {
            var outerRadiusOnSprite = capsuleSprite.OuterRadius;
            var innerRadiusOnSprite = capsuleSprite.InnerRadius;
            var outerCenterOnSprite = capsuleSprite.OuterCenter;
            var innerCenterOnSprite = capsuleSprite.InnerCenter;
            var radiusDiff = outerRadiusOnSprite - innerRadiusOnSprite;
            
            outerRadiusOnRect = Mathf.Max(0f, outerRadiusOnRect);
            var innerRadiusOnRect = Mathf.Max(0f, outerRadiusOnRect - radiusDiff);
            
            var toCenter = center - corner;
            toCenter = new Vector2(Mathf.Sign(toCenter.x), Mathf.Sign(toCenter.y));
            var outerCenterOnRect = corner + toCenter * outerRadiusOnRect;
            var innerCenterOnRect = outerCenterOnRect;

            if (toCenter.x < Mathf.Epsilon)
            {
                innerCenterOnRect.x = Mathf.Min(corner.x - radiusDiff, outerCenterOnRect.x);
            }
            else
            {
                innerCenterOnRect.x = Mathf.Max(corner.x + radiusDiff, outerCenterOnRect.x);
            }
            
            if (toCenter.y < Mathf.Epsilon)
            {
                innerCenterOnRect.y = Mathf.Min(corner.y - radiusDiff, outerCenterOnRect.y);
            }
            else
            {
                innerCenterOnRect.y = Mathf.Max(corner.y + radiusDiff, outerCenterOnRect.y);
            }

            innerCenterOnRect += innerCenterOnSprite - outerCenterOnSprite;
            
            var baseStepCount = sliceApproximationSteps + 1;
            var stepAngle = Mathf.PI / (2 * baseStepCount);
            var stepCount = addEndPoints ? baseStepCount + 1 : baseStepCount;
            
            Debug.Log(innerCenterOnRect);

            for (var i = 0; i < stepCount; i++)
            {
                var angle = -i * stepAngle + startAngle;
                var sin = Mathf.Sin(angle);
                var cos = Mathf.Cos(angle);
                var circlePosition = new Vector2(cos, sin);
                vertices[indexCount + outerVertexCount] = VertexFromAngle(circlePosition, innerRadiusOnSprite, innerCenterOnSprite,
                    innerRadiusOnRect,
                    innerCenterOnRect, spriteSize, uv1, uv4);
                
                vertices[indexCount++] = VertexFromAngle(circlePosition, outerRadiusOnSprite, outerCenterOnSprite, outerRadiusOnRect,
                    outerCenterOnRect, spriteSize, uv1, uv4);
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
    }
}