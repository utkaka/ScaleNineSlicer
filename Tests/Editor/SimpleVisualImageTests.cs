using NUnit.Framework;
using UnityEngine;

namespace Utkaka.ScaleNineSlicer.Tests.Editor
{
    public class SimpleVisualImageTests : AbstractVisualImageTest
    {
        [Test]
        public void SimpleImage(
            [ValueSource(nameof(WrapMode))] TextureWrapMode wrapMode,
            [ValueSource(nameof(MeshType))] SpriteMeshType spriteMeshType,
            [ValueSource(nameof(Border))] Vector4 border,
            [ValueSource(nameof(ImageSize))] Vector2Int size,
            [ValueSource(nameof(FillCenter))] bool fillCenter,
            [ValueSource(nameof(PixelsPerUnitMultiplier))] int pixelsPerUnitMultiplier)
        {
            var sprite = CreateSprite(wrapMode, spriteMeshType, border);
            var image = CreateImage(sprite, false, false, fillCenter, pixelsPerUnitMultiplier, size);
            var slicedImage = CreateSlicedImage(sprite, false, false, fillCenter, pixelsPerUnitMultiplier, size);
            
            CompareLessMeshStatistics(image, slicedImage);
            CompareImages(size, image, slicedImage);
            DestroyImages(sprite, image, slicedImage);
        }

        [Test]
        public new void UseSpriteMesh(
            [ValueSource(nameof(WrapMode))] TextureWrapMode wrapMode,
            [ValueSource(nameof(MeshType))] SpriteMeshType spriteMeshType,
            [ValueSource(nameof(Border))] Vector4 border,
            [ValueSource(nameof(ImageSize))] Vector2Int size,
            [ValueSource(nameof(FillCenter))] bool fillCenter,
            [ValueSource(nameof(PixelsPerUnitMultiplier))] int pixelsPerUnitMultiplier)
        {
            var sprite = CreateSprite(wrapMode, spriteMeshType, border);
            var image = CreateImage(sprite, false, true, fillCenter, pixelsPerUnitMultiplier, size);
            var slicedImage = CreateSlicedImage(sprite, false, true, fillCenter, pixelsPerUnitMultiplier, size);
            
            CompareExactMeshStatistics(image, slicedImage);
            CompareImages(size, image, slicedImage);
            DestroyImages(sprite, image, slicedImage);
        }
        
        [Test]
        public new void PreserveAspect(
            [ValueSource(nameof(WrapMode))] TextureWrapMode wrapMode,
            [ValueSource(nameof(MeshType))] SpriteMeshType spriteMeshType,
            [ValueSource(nameof(Border))] Vector4 border,
            [ValueSource(nameof(ImageSize))] Vector2Int size,
            [ValueSource(nameof(FillCenter))] bool fillCenter,
            [ValueSource(nameof(PixelsPerUnitMultiplier))] int pixelsPerUnitMultiplier)
        {
            var sprite = CreateSprite(wrapMode, spriteMeshType, border);
            var image = CreateImage(sprite, true, false, fillCenter, pixelsPerUnitMultiplier, size);
            var slicedImage = CreateSlicedImage(sprite, true, false, fillCenter, pixelsPerUnitMultiplier, size);
            
            CompareLessMeshStatistics(image, slicedImage);
            CompareImages(size, image, slicedImage);
            DestroyImages(sprite, image, slicedImage);
        }
        
        [Test]
        public void UseSpriteMeshWithPreserveAspect(
            [ValueSource(nameof(WrapMode))] TextureWrapMode wrapMode,
            [ValueSource(nameof(MeshType))] SpriteMeshType spriteMeshType,
            [ValueSource(nameof(Border))] Vector4 border,
            [ValueSource(nameof(ImageSize))] Vector2Int size,
            [ValueSource(nameof(FillCenter))] bool fillCenter,
            [ValueSource(nameof(PixelsPerUnitMultiplier))] int pixelsPerUnitMultiplier)
        {
            var sprite = CreateSprite(wrapMode, spriteMeshType, border);
            var image = CreateImage(sprite, true, true, fillCenter, pixelsPerUnitMultiplier, size);
            var slicedImage = CreateSlicedImage(sprite, true, true, fillCenter, pixelsPerUnitMultiplier, size);
            
            CompareExactMeshStatistics(image, slicedImage);
            CompareImages(size, image, slicedImage);
            DestroyImages(sprite, image, slicedImage);
        }
    }
}