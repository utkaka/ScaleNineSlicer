using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Utkaka.ScaleNineSlicer.UI;

namespace Utkaka.ScaleNineSlicer.Tests.Editor
{
    public class FilledVisualImageTests : AbstractVisualImageTest
    {
        private static readonly int[] AxisOrigins = { 0, 1 };
        private static readonly int[] RadialOrigins = { 0, 1, 2, 3 };
        private static readonly bool[] RadialOrientation = { false, true };

        private static readonly float[] FillAmounts =
        {
            0f,
            0.3f,
            0.5f,
            0.6f,
            1f
        };
    
        [Test]
        public void SimpleFill(
            [ValueSource(nameof(WrapMode))] TextureWrapMode wrapMode,
            [ValueSource(nameof(MeshType))] SpriteMeshType spriteMeshType,
            [ValueSource(nameof(Border))] Vector4 border,
            [ValueSource(nameof(ImageSize))] Vector2Int size,
            [ValueSource(nameof(PreserveAspect))] bool preserveAspect,
            [ValueSource(nameof(UseSpriteMesh))] bool useSpriteMesh,
            [ValueSource(nameof(FillCenter))] bool fillCenter,
            [ValueSource(nameof(PixelsPerUnitMultiplier))] int pixelsPerUnitMultiplier)
        {
            var sprite = CreateSprite(wrapMode, spriteMeshType, border);
            var image = CreateImage(sprite, preserveAspect, useSpriteMesh, fillCenter, pixelsPerUnitMultiplier, size);
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = 0;
            image.fillAmount = 0.3f;
            var slicedImage = CreateSlicedImage(sprite, preserveAspect, useSpriteMesh, fillCenter, pixelsPerUnitMultiplier, size);
            slicedImage.filled = true;
            slicedImage.fillMethod = SlicedImage.FillMethod.Horizontal;
            slicedImage.fillOrigin = 0;
            slicedImage.fillAmount = 0.3f;

            CompareLessMeshStatistics(image, slicedImage);
            CompareImages(size, image, slicedImage);
            DestroyImages(sprite, image, slicedImage);
        }
    
        [Test]
        public void HorizontalFill(
            [ValueSource(nameof(ImageSize))] Vector2Int size,
            [ValueSource(nameof(AxisOrigins))] int origin,
            [ValueSource(nameof(FillAmounts))] float fillAmount)
        {
            var sprite = CreateSprite(TextureWrapMode.Clamp, SpriteMeshType.Tight, Vector4.zero);
            var image = CreateImage(sprite, false, false, true, 1, size);
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = origin;
            image.fillAmount = fillAmount;
            var slicedImage = CreateSlicedImage(sprite, false, false, true, 1, size);
            slicedImage.filled = true;
            slicedImage.fillMethod = SlicedImage.FillMethod.Horizontal;
            slicedImage.fillOrigin = origin;
            slicedImage.fillAmount = fillAmount;

            CompareLessMeshStatistics(image, slicedImage);
            CompareImages(size, image, slicedImage);
            DestroyImages(sprite, image, slicedImage);
        }
    
        [Test]
        public void VerticalFill(
            [ValueSource(nameof(ImageSize))] Vector2Int size,
            [ValueSource(nameof(AxisOrigins))] int origin,
            [ValueSource(nameof(FillAmounts))] float fillAmount)
        {
            var sprite = CreateSprite(TextureWrapMode.Clamp, SpriteMeshType.Tight, Vector4.zero);
            var image = CreateImage(sprite, false, false, true, 1, size);
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Vertical;
            image.fillOrigin = origin;
            image.fillAmount = fillAmount;
            var slicedImage = CreateSlicedImage(sprite, false, false, true, 1, size);
            slicedImage.filled = true;
            slicedImage.fillMethod = SlicedImage.FillMethod.Vertical;
            slicedImage.fillOrigin = origin;
            slicedImage.fillAmount = fillAmount;

            CompareLessMeshStatistics(image, slicedImage);
            CompareImages(size, image, slicedImage);
            DestroyImages(sprite, image, slicedImage);
        }
    
        [Test]
        public void Radial90Fill(
            [ValueSource(nameof(ImageSize))] Vector2Int size,
            [ValueSource(nameof(RadialOrigins))] int origin,
            [ValueSource(nameof(RadialOrientation))] bool clockwise,
            [ValueSource(nameof(FillAmounts))] float fillAmount)
        {
            var sprite = CreateSprite(TextureWrapMode.Clamp, SpriteMeshType.Tight, Vector4.zero);
            var image = CreateImage(sprite, false, false, true, 1, size);
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial90;
            image.fillOrigin = origin;
            image.fillClockwise = clockwise;
            image.fillAmount = fillAmount;
            var slicedImage = CreateSlicedImage(sprite, false, false, true, 1, size);
            slicedImage.filled = true;
            slicedImage.fillMethod = SlicedImage.FillMethod.Radial90;
            slicedImage.fillOrigin = origin;
            slicedImage.fillClockwise = clockwise;
            slicedImage.fillAmount = fillAmount;

            CompareLessMeshStatistics(image, slicedImage);
            CompareImages(size, image, slicedImage);
            DestroyImages(sprite, image, slicedImage);
        }
    
        [Test]
        public void Radial180Fill(
            [ValueSource(nameof(ImageSize))] Vector2Int size,
            [ValueSource(nameof(RadialOrigins))] int origin,
            [ValueSource(nameof(RadialOrientation))] bool clockwise,
            [ValueSource(nameof(FillAmounts))] float fillAmount)
        {
            var sprite = CreateSprite(TextureWrapMode.Clamp, SpriteMeshType.Tight, Vector4.zero);
            var image = CreateImage(sprite, false, false, true, 1, size);
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial180;
            image.fillOrigin = origin;
            image.fillClockwise = clockwise;
            image.fillAmount = fillAmount;
            var slicedImage = CreateSlicedImage(sprite, false, false, true, 1, size);
            slicedImage.filled = true;
            slicedImage.fillMethod = SlicedImage.FillMethod.Radial180;
            slicedImage.fillOrigin = origin;
            slicedImage.fillClockwise = clockwise;
            slicedImage.fillAmount = fillAmount;

            CompareLessMeshStatistics(image, slicedImage);
            CompareImages(size, image, slicedImage);
            DestroyImages(sprite, image, slicedImage);
        }
    
        [Test]
        public void Radial360Fill(
            [ValueSource(nameof(ImageSize))] Vector2Int size,
            [ValueSource(nameof(RadialOrigins))] int origin,
            [ValueSource(nameof(RadialOrientation))] bool clockwise,
            [ValueSource(nameof(FillAmounts))] float fillAmount)
        {
            var sprite = CreateSprite(TextureWrapMode.Clamp, SpriteMeshType.Tight, Vector4.zero);
            var image = CreateImage(sprite, false, false, true, 1, size);
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = origin;
            image.fillClockwise = clockwise;
            image.fillAmount = fillAmount;
            var slicedImage = CreateSlicedImage(sprite, false, false, true, 1, size);
            slicedImage.filled = true;
            slicedImage.fillMethod = SlicedImage.FillMethod.Radial360;
            slicedImage.fillOrigin = origin;
            slicedImage.fillClockwise = clockwise;
            slicedImage.fillAmount = fillAmount;

            CompareLessMeshStatistics(image, slicedImage);
            CompareImages(size, image, slicedImage);
            DestroyImages(sprite, image, slicedImage);
        }
    }
}
