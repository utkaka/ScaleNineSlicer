using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Utkaka.ScaleNineSlicer.Tests.Runtime;
using Utkaka.ScaleNineSlicer.UI;

namespace Utkaka.ScaleNineSlicer.Tests.Editor
{
    public abstract class AbstractVisualImageTest : AbstractImageTest
    {
        protected static readonly TextureWrapMode[] WrapMode = { TextureWrapMode.Clamp, TextureWrapMode.Repeat };
        protected static readonly SpriteMeshType[] MeshType = { SpriteMeshType.FullRect, SpriteMeshType.Tight };
        protected static readonly Vector4[] Border = { Vector4.zero, new(33f, 37f, 33f, 37f) };
        protected static readonly Vector2Int[] ImageSize = { new(67, 75), new(100, 100) };
        protected static readonly bool[] PreserveAspect = { true, false };
        protected static readonly bool[] UseSpriteMesh = { true, false };
        protected static readonly bool[] FillCenter = { true, false };
        protected static readonly int[] PixelsPerUnitMultiplier = { 1, 2 };
        
        protected void DestroyImages(Sprite sprite, Image image, SlicedImage slicedImage)
        {
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(slicedImage);
            Object.DestroyImmediate(sprite.texture);
            Object.DestroyImmediate(sprite);
        }

        protected void LogMeshStatistics(Image image, SlicedImage slicedImage)
        {
            Canvas.ForceUpdateCanvases();
            if (image is IMeshStatistics imageStatistics)
            {
                Debug.Log($"[Image] Vertex count: {imageStatistics.VertexCount} Triangles count: {imageStatistics.TrianglesCount}");
            }
            if (slicedImage is IMeshStatistics slicedImageStatistics)
            {
                Debug.Log($"[SlicedImage] Vertex count: {slicedImageStatistics.VertexCount} Triangles count: {slicedImageStatistics.TrianglesCount}");
            }
        }

        protected void CompareExactMeshStatistics(Image image, SlicedImage slicedImage)
        {
            LogMeshStatistics(image, slicedImage);
            Assert.AreEqual(((IMeshStatistics)image).VertexCount, ((IMeshStatistics)slicedImage).VertexCount);
            Assert.AreEqual(((IMeshStatistics)image).TrianglesCount, ((IMeshStatistics)slicedImage).TrianglesCount);
        }
        
        protected void CompareLessMeshStatistics(Image image, SlicedImage slicedImage)
        {
            LogMeshStatistics(image, slicedImage);
            Assert.GreaterOrEqual(((IMeshStatistics)image).VertexCount, ((IMeshStatistics)slicedImage).VertexCount);
            Assert.GreaterOrEqual(((IMeshStatistics)image).TrianglesCount, ((IMeshStatistics)slicedImage).TrianglesCount);
        }
        
        protected void CompareImages(Vector2Int size, Image image, SlicedImage slicedImage)
        {
            size = new Vector2Int(size.x, size.y);
            slicedImage.gameObject.SetActive(false);
            var imageSnapshot = RenderImageSnapshot(size);
            
            image.gameObject.SetActive(false);
            slicedImage.gameObject.SetActive(true);
            var slicedImageSnapshot = RenderImageSnapshot(size);

            AssertTexturesEqual(imageSnapshot, slicedImageSnapshot);
            
            Object.DestroyImmediate(imageSnapshot);
            Object.DestroyImmediate(slicedImageSnapshot);
        }
        
        private Texture2D RenderImageSnapshot(Vector2Int size)
        {
            size += new Vector2Int(10, 10);
            var rt = new RenderTexture(size.x, size.y, 24, RenderTextureFormat.ARGB32);
            var prevActive = RenderTexture.active;
            var prevTarget = Camera.targetTexture;
            Camera.targetTexture = rt;
            RenderTexture.active = rt;

            Camera.Render();

            var snapshot = new Texture2D(size.x, size.y, TextureFormat.RGBA32, 0, true);
            snapshot.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
            snapshot.Apply();
            snapshot.hideFlags = HideFlags.HideAndDontSave;

            RenderTexture.active = prevActive;
            Camera.targetTexture = prevTarget;
            rt.Release();
            Object.DestroyImmediate(rt);
            return snapshot;
        }

        private static void AssertTexturesEqual(Texture2D imageSnapshot, Texture2D slicedImageSnapshot)
        {
            var testName = TestContext.CurrentContext.Test.Name;
            var imageSnapshotPixels = imageSnapshot.GetPixels32();
            var slicedImageSnapshotPixels = slicedImageSnapshot.GetPixels32();
            Assert.AreEqual(imageSnapshotPixels.Length, slicedImageSnapshotPixels.Length, "Pixel array length differs");
            
            var areEqual = !imageSnapshotPixels.Where((t, i) => !t.Equals(slicedImageSnapshotPixels[i])).Any();

            if (!areEqual)
            {
                SaveSnapshot(imageSnapshot, $"{testName} (Image)");
                SaveSnapshot(slicedImageSnapshot, $"{testName} (SlicedImage)");
            }
            
            Assert.IsTrue(areEqual);
        }

        private static void SaveSnapshot(Texture2D tex, string fileName)
        {
            var dir = Path.Combine(Application.dataPath, "../TestSnapshots");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, fileName + ".png");
            var bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
        }
    }
}