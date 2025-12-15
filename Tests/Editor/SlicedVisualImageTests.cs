using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Utkaka.ScaleNineSlicer.Tests.Editor
{
    public class SlicedVisualImageTests : AbstractVisualImageTest
    {
         [Test]
         public void SlicedImage(
            [ValueSource(nameof(WrapMode))] TextureWrapMode wrapMode,
            [ValueSource(nameof(MeshType))] SpriteMeshType spriteMeshType,
            [ValueSource(nameof(Border))] Vector4 border,
            [ValueSource(nameof(ImageSize))] Vector2Int size,
            [ValueSource(nameof(PreserveAspect))] bool preserveAspect,
            [ValueSource(nameof(UseSpriteMesh))] bool useSpriteMesh,
            [ValueSource(nameof(PixelsPerUnitMultiplier))] int pixelsPerUnitMultiplier)
         {
            var sprite = CreateSprite(wrapMode, spriteMeshType, border);
            var image = CreateImage(sprite, preserveAspect, useSpriteMesh, true, pixelsPerUnitMultiplier, size);
            image.type = Image.Type.Sliced;
            var slicedImage = CreateSlicedImage(sprite, preserveAspect, useSpriteMesh, true, pixelsPerUnitMultiplier, size);
            slicedImage.sliced = true;
            
            CompareLessMeshStatistics(image, slicedImage);
            CompareImages(size, image, slicedImage);
            DestroyImages(sprite, image, slicedImage);
         }

         [Test]
         public void SkipCenter(
            [ValueSource(nameof(WrapMode))] TextureWrapMode wrapMode,
            [ValueSource(nameof(MeshType))] SpriteMeshType spriteMeshType,
            [ValueSource(nameof(Border))] Vector4 border,
            [ValueSource(nameof(ImageSize))] Vector2Int size,
            [ValueSource(nameof(PreserveAspect))] bool preserveAspect,
            [ValueSource(nameof(UseSpriteMesh))] bool useSpriteMesh,
            [ValueSource(nameof(PixelsPerUnitMultiplier))] int pixelsPerUnitMultiplier)
         {
            var sprite = CreateSprite(wrapMode, spriteMeshType, border);
            var image = CreateImage(sprite, false, true, false, pixelsPerUnitMultiplier, size);
            image.type = Image.Type.Sliced;
            var slicedImage = CreateSlicedImage(sprite, false, true, false, pixelsPerUnitMultiplier, size);
            slicedImage.sliced = true;
            
            CompareLessMeshStatistics(image, slicedImage);
            CompareImages(size, image, slicedImage);
            DestroyImages(sprite, image, slicedImage);
         }
    }
}