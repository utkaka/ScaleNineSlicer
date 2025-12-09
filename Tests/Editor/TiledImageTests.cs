using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Utkaka.ScaleNineSlicer.Tests.Editor
{
    public class TiledImageTests : AbstractImageTest
    {
        private static readonly Vector4[] OverrideBorder = { Vector4.zero, new(15f, 15f, 15f, 20f) };
        
        [Test]
        public void TileRepeated(
           [ValueSource(nameof(MeshType))] SpriteMeshType spriteMeshType,
           [ValueSource(nameof(OverrideBorder))] Vector4 border,
           [ValueSource(nameof(ImageSize))] Vector2Int size,
           [ValueSource(nameof(PreserveAspect))] bool preserveAspect,
           [ValueSource(nameof(UseSpriteMesh))] bool useSpriteMesh,
           [ValueSource(nameof(FillCenter))] bool fillCenter,
           [ValueSource(nameof(PixelsPerUnitMultiplier))] int pixelsPerUnitMultiplier)
        {
            if (border == Vector4.zero && !fillCenter)
            {
                Assert.Ignore("Ignore sprite without with fillCenter == false borders as UI.Image doesn't handle it correctly");
            }
            var sprite = CreateSprite(TextureWrapMode.Repeat, spriteMeshType, border);
            var image = CreateImage(sprite, preserveAspect, useSpriteMesh, fillCenter, pixelsPerUnitMultiplier, size);
            image.type = Image.Type.Tiled;
            var slicedImage = CreateSlicedImage(sprite, preserveAspect, useSpriteMesh, fillCenter, pixelsPerUnitMultiplier, size);
            if (border != Vector4.zero)
            {
                slicedImage.sliced = true;
                slicedImage.tileScaledSlices = true;
            }
            else
            {
                slicedImage.tiled = true;
                slicedImage.tileSize = new Vector2Int((int)sprite.rect.size.x, (int)sprite.rect.size.y);
            }

            CompareLessMeshStatistics(image, slicedImage);
            CompareImages(size, image, slicedImage);
            DestroyImages(sprite, image, slicedImage);
        }
        
        [Test]
        public void TileClamped(
            [ValueSource(nameof(MeshType))] SpriteMeshType spriteMeshType,
            [ValueSource(nameof(Border))] Vector4 border,
            [ValueSource(nameof(ImageSize))] Vector2Int size,
            [ValueSource(nameof(PreserveAspect))] bool preserveAspect,
            [ValueSource(nameof(UseSpriteMesh))] bool useSpriteMesh,
            [ValueSource(nameof(FillCenter))] bool fillCenter,
            [ValueSource(nameof(PixelsPerUnitMultiplier))] int pixelsPerUnitMultiplier)
        {
            if (border == Vector4.zero && !fillCenter)
            {
                Assert.Ignore("Ignore sprite without with fillCenter == false borders as UI.Image doesn't handle it correctly");
            }
            if (pixelsPerUnitMultiplier != 1)
            {
                //Assert.Ignore("For some reason some of SlicedImage tiles are slightly shifted compared to UI.Image.");
            }
            var sprite = CreateSprite(TextureWrapMode.Clamp, spriteMeshType, border);
            var image = CreateImage(sprite, preserveAspect, useSpriteMesh, fillCenter, pixelsPerUnitMultiplier, size);
            image.type = Image.Type.Tiled;
            var slicedImage = CreateSlicedImage(sprite, preserveAspect, useSpriteMesh, fillCenter, pixelsPerUnitMultiplier, size);
            if (border != Vector4.zero)
            {
                slicedImage.sliced = true;
                slicedImage.tileScaledSlices = true;
            }
            else
            {
                slicedImage.tiled = true;
                slicedImage.tileSize = new Vector2Int((int)sprite.rect.size.x, (int)sprite.rect.size.y);
            }

            CompareLessMeshStatistics(image, slicedImage);
            CompareImages(size, image, slicedImage);
            DestroyImages(sprite, image, slicedImage);
            
            
        }
    }
}