using UnityEngine;
using UnityEngine.UI;

namespace Utkaka.ScaleNineSlicer.UI
{
    public class SpriteMeshHandler
    {
        public static void PrepareMesh(VertexHelper vertexHelper, Sprite sprite, Color32 color, Rect adjustedRect, Vector2 rectPivot)
        {
            var spriteSize = new Vector2(sprite.rect.width, sprite.rect.height);

            // Covert sprite pivot into normalized space.
            var spritePivot = sprite.pivot / spriteSize;
            /*Rect r = GetPixelAdjustedRect();
            if (lPreserveAspect & spriteSize.sqrMagnitude > 0.0f)
            {
                PreserveSpriteAspectRatio(ref r, spriteSize);
            }*/

            var drawingSize = new Vector2(adjustedRect.width, adjustedRect.height);
            var spriteBoundSize = sprite.bounds.size;

            // Calculate the drawing offset based on the difference between the two pivots.
            var drawOffset = (rectPivot - spritePivot) * drawingSize;

            vertexHelper.Clear();

            var vertices = sprite.vertices;
            var uvs = sprite.uv;
            for (var i = 0; i < vertices.Length; ++i)
            {
                vertexHelper.AddVert(new Vector3((vertices[i].x / spriteBoundSize.x) * drawingSize.x - drawOffset.x, (vertices[i].y / spriteBoundSize.y) * drawingSize.y - drawOffset.y), color, new Vector2(uvs[i].x, uvs[i].y));
            }

            var triangles = sprite.triangles;
            for (var i = 0; i < triangles.Length; i += 3)
            {
                vertexHelper.AddTriangle(triangles[i + 0], triangles[i + 1], triangles[i + 2]);
            }
        }
    }
}