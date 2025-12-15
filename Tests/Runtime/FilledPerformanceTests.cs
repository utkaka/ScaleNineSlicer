using System.Collections;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Utkaka.ScaleNineSlicer.UI;

namespace Utkaka.ScaleNineSlicer.Tests.Runtime
{
    public class FilledPerformanceTests : AbstractPerformanceTest
    {
        [UnityTest, Performance]
        public IEnumerator ImagePerformanceTest(
            [ValueSource(nameof(SpritesCount))] int spritesCount,
            [ValueSource(nameof(ChangeGridSize))] bool changeGridSize)
        {
            AdjustGridSize(spritesCount);
            var sprite = CreateSprite(TextureWrapMode.Clamp, SpriteMeshType.FullRect, Vector4.zero);
            for (var i = 0; i < spritesCount; i++)
            {
                var image = CreateImage(sprite, false, false, true, 100, Vector2Int.one);
                image.type = Image.Type.Filled;
                image.gameObject.AddComponent<FillAmountChange>();
                image.fillMethod = Image.FillMethod.Radial360;
                image.fillAmount = 0.0f;
            }
            
            yield return CollectStatistics(changeGridSize);

            Object.DestroyImmediate(sprite.texture);
            Object.DestroyImmediate(sprite);
        }
        
        [UnityTest, Performance]
        public IEnumerator SlicedImagePerformanceTest(
            [ValueSource(nameof(SpritesCount))] int spritesCount,
            [ValueSource(nameof(ChangeGridSize))] bool changeGridSize)
        {
            AdjustGridSize(spritesCount);
            var sprite = CreateSprite(TextureWrapMode.Clamp, SpriteMeshType.FullRect, Vector4.zero);
            for (var i = 0; i < spritesCount; i++)
            {
                var slicedImage = CreateSlicedImage(sprite, false, false, true, 100, Vector2Int.one);
                slicedImage.gameObject.AddComponent<FillAmountChange>();
                slicedImage.filled = true;
                slicedImage.fillMethod = SlicedImage.FillMethod.Radial360;
                slicedImage.fillAmount = 0.0f;
            }

            yield return CollectStatistics(changeGridSize);

            Object.DestroyImmediate(sprite.texture);
            Object.DestroyImmediate(sprite);
        }
    }
}