using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ScaleNineSlicer.Editor {
	public class TiledImageElement : VisualElement {
		private ushort[] _indices;
		private Vertex[] _vertices;

		public new class UxmlTraits : VisualElement.UxmlTraits{ }
		
		public new class UxmlFactory : UxmlFactory<TiledImageElement, UxmlTraits> { }
		
		public TiledImageElement() {
			_indices = new ushort[] {0, 2, 1, 0, 3, 2};
			_vertices = new Vertex[4];
			generateVisualContent += DrawMeshes;
		}

		private void DrawMeshes(MeshGenerationContext context) {
			var backgroundImage = resolvedStyle.backgroundImage;
			Texture texture = null;
			if (backgroundImage.sprite != null) {
				texture = backgroundImage.sprite.texture;
			} else if (backgroundImage.texture != null) {
				texture = backgroundImage.texture;
			} else if (backgroundImage.renderTexture != null) {
				texture = backgroundImage.renderTexture;
			}
			if (texture == null) return;
			var tint = resolvedStyle.unityBackgroundImageTintColor;
			var rect = contentRect;
			var tilingX = rect.width / texture.width;
			var tilingY = rect.height / texture.height;

			_vertices[0] = new Vertex {position = new Vector3(0.0f, 0.0f), uv = new Vector2(0, 0), tint = tint};
			_vertices[1] = new Vertex {position = new Vector3(0.0f, rect.height), uv = new Vector2(0, tilingY), tint = tint};
			_vertices[2] = new Vertex {position = new Vector3(rect.width, rect.height), uv = new Vector2(tilingX, tilingY), tint = tint};
			_vertices[3] = new Vertex { position = new Vector3(rect.width, 0.0f), uv = new Vector2(tilingX,0), tint = tint};

			var contextType = typeof(MeshGenerationContext);
			var methodInfo = contextType.GetMethod("Allocate", BindingFlags.Instance | BindingFlags.NonPublic);
			var trackMeshWriteData = (MeshWriteData)methodInfo?.Invoke(context, new object[]{4, 6, texture, null, 2});
			if (trackMeshWriteData != null) {
				trackMeshWriteData.SetAllVertices(_vertices);
				trackMeshWriteData.SetAllIndices(_indices);	
			}
		}
	}
}