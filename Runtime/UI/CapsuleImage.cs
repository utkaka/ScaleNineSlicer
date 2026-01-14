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
            Circle = 2
        }
        
        [SerializeField]
        private CapsuleSprite _capsuleSprite;

        [SerializeField]
        private int _sliceApproximationSteps;
        [SerializeField]
        private Type _sliceType;
        
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
            
            var heapArray = Utils.GetFromPoolIfNeeded<CutInputVertex>(context.VertexCount);
            var vertices = heapArray == null ? stackalloc CutInputVertex[context.VertexCount] : heapArray.AsSpan();
            
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

                    SlicedMeshHandler.PrepareCapsuleMesh(tileVertices, cuts, toFill, Color.white, 3, sliceApproximationSteps + 1 + 2, !fillCenter,
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
            var padding = DataUtility.GetPadding(localActiveSprite) / adjustedPixelsPerUnit;
            var position = context.FullRect.position;

            var baseRect = Rect.MinMaxRect(padding.x, padding.y, context.TileSize.x - padding.z,
                context.TileSize.y - padding.w);
            baseRect.position += position;
            
            var uv1 = new Vector2(outerUV.x, outerUV.y);
            var uv4 = new Vector2(outerUV.z * context.TopRightUVMultiplier.x, outerUV.w * context.TopRightUVMultiplier.y);
            
            var indexCount = 0;

            float rectRatio;
            switch (sliceType)
            {
                case Type.Horizontal:
                    rectRatio = context.TileSize.y / context.SpriteSize.y;
                    break;
                case Type.Vertical:
                    rectRatio = context.TileSize.x / context.SpriteSize.x;
                    break;
                case Type.Circle:
                    rectRatio = Math.Min(context.TileSize.x / context.SpriteSize.x, context.TileSize.y / context.SpriteSize.y);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var outerRadiusOnSprite = capsuleSprite.OuterRadius;
            var innerRadiusOnSprite = capsuleSprite.InnerRadius;
            var outerRadiusOnRect =  rectRatio * outerRadiusOnSprite;
            var innerRadiusOnRect = outerRadiusOnRect - (outerRadiusOnSprite - innerRadiusOnSprite);
            var innerCenterOnSprite = capsuleSprite.Center;
            var innerCenterOnRect = innerCenterOnSprite;
            
            //Circle
            var step = Mathf.PI * 2 / sliceApproximationSteps;
            
            switch (sliceType)
            {
                case Type.Horizontal:
                    
                    for (var i = 0; i <= sliceApproximationSteps / 2; i++)
                    {
                        var a = - i * step - Mathf.PI * 0.5f;
                        vertices[indexCount + sliceApproximationSteps + 1 + 2] = VertexFromAngle(a, innerRadiusOnSprite, innerCenterOnSprite, innerRadiusOnRect,
                            innerCenterOnRect, context.SpriteSize, baseRect, uv1, uv4, true);
                        vertices[indexCount++] = VertexFromAngle(a, outerRadiusOnSprite, Vector2.zero, outerRadiusOnRect,
                            Vector2.zero, context.SpriteSize, baseRect, uv1, uv4, false);
                    }

                    var a1 = - Mathf.PI * 0.5f - Mathf.PI;
                    /*if ((sliceApproximationSteps | 1) == 1)
                    {
                        vertices[indexCount + sliceApproximationSteps + 1 + 2] = VertexFromAngle(a1, innerRadiusOnSprite, innerCenterOnSprite, innerRadiusOnRect,
                            innerCenterOnRect, context.SpriteSize, baseRect, uv1, uv4, true);
                        vertices[indexCount++] = VertexFromAngle(a1, outerRadiusOnSprite, Vector2.zero, outerRadiusOnRect,
                            Vector2.zero, context.SpriteSize, baseRect, uv1, uv4, false);
                    }*/

                    baseRect.position += new Vector2(baseRect.width - outerRadiusOnRect, 0.0f);
                    
                    vertices[indexCount + sliceApproximationSteps + 1 + 2] = VertexFromAngle(a1, innerRadiusOnSprite, innerCenterOnSprite, innerRadiusOnRect,
                        innerCenterOnRect, context.SpriteSize, baseRect, uv1, uv4, true);
                    vertices[indexCount++] = VertexFromAngle(a1, outerRadiusOnSprite, Vector2.zero, outerRadiusOnRect,
                        Vector2.zero, context.SpriteSize, baseRect, uv1, uv4, false);
                    
                    for (var i = sliceApproximationSteps / 2 + 1; i <= sliceApproximationSteps; i++)
                    {
                        var a = - i * step - Mathf.PI * 0.5f;
                        vertices[indexCount + sliceApproximationSteps + 1 + 2] = VertexFromAngle(a, innerRadiusOnSprite, innerCenterOnSprite, innerRadiusOnRect,
                            innerCenterOnRect, context.SpriteSize, baseRect, uv1, uv4, true);
                        vertices[indexCount++] = VertexFromAngle(a, outerRadiusOnSprite, Vector2.zero, outerRadiusOnRect,
                            Vector2.zero, context.SpriteSize, baseRect, uv1, uv4, false);
                    }
                    
                    baseRect.position -= new Vector2(baseRect.width - outerRadiusOnRect, 0.0f);
                    
                    vertices[indexCount + sliceApproximationSteps + 1 + 2] = VertexFromAngle(- Mathf.PI * 0.5f, innerRadiusOnSprite, innerCenterOnSprite, innerRadiusOnRect,
                        innerCenterOnRect, context.SpriteSize, baseRect, uv1, uv4, true);
                    vertices[indexCount++] = VertexFromAngle(- Mathf.PI * 0.5f, outerRadiusOnSprite, Vector2.zero, outerRadiusOnRect,
                        Vector2.zero, context.SpriteSize, baseRect, uv1, uv4, false);
                    
                    break;
                case Type.Vertical:
                    rectRatio = context.TileSize.x / context.SpriteSize.x;
                    break;
                case Type.Circle:
                    for (var i = 0; i < sliceApproximationSteps + 1; i++)
                    {
                        var a = - i * step - Mathf.PI * 0.5f;
                        vertices[indexCount + sliceApproximationSteps + 2] = VertexFromAngle(a, innerRadiusOnSprite, innerCenterOnSprite, innerRadiusOnRect,
                            innerCenterOnRect, context.SpriteSize, baseRect, uv1, uv4, true);
                        vertices[indexCount++] = VertexFromAngle(a, outerRadiusOnSprite, Vector2.zero, outerRadiusOnRect,
                            Vector2.zero, context.SpriteSize, baseRect, uv1, uv4, false);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            Debug.Log(indexCount);
            
            vertices[vertices.Length - 1] = VertexFromAngle(0.0f, 0.0f, innerCenterOnSprite, 0.0f,
                innerCenterOnRect, context.SpriteSize, baseRect, uv1, uv4, true);
        }

        private CutInputVertex VertexFromAngle(float angle, float spriteRadius, Vector2 spriteOffset, float rectRadius, Vector2 rectOffset, Vector2 spriteSize,
            Rect rect, Vector2 uv1, Vector2 uv2, bool inner)
        {
            var sin = Mathf.Sin(angle);
            var cos = Mathf.Cos(angle);
            var circlePosition = new Vector2(cos, sin);

            var pointOnSprite = circlePosition * spriteRadius + spriteOffset + spriteSize * 0.5f;
            var pointRatio = pointOnSprite / spriteSize;
            var uv = pointRatio * (uv2 - uv1) + uv1;
            
            var pointOnRect = circlePosition * rectRadius + rectOffset + rect.center;

            return new CutInputVertex
            {
                Position = pointOnRect,
                UV = uv,
                Inner = inner
            };
        }
    }
}