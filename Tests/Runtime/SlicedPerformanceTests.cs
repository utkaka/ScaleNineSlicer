using System.Collections;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Utkaka.ScaleNineSlicer.Tests.Runtime
{
    public class SlicedPerformanceTests : AbstractPerformanceTest
    {
        private static readonly Vector4 Border = new(33f, 37f, 33f, 37f);

        [UnityTest, Performance]
        public IEnumerator ImagePerformanceTest(
            [ValueSource(nameof(SpritesCount))] int spritesCount,
            [ValueSource(nameof(ChangeGridSize))] bool changeGridSize)
        {
            AdjustGridSize(spritesCount);
            var sprite = CreateSprite(TextureWrapMode.Clamp, SpriteMeshType.FullRect, Border);
            for (var i = 0; i < spritesCount; i++)
            {
                var image = CreateImage(sprite, false, false, true, 100, Vector2Int.one);
                image.type = Image.Type.Sliced;
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
            var sprite = CreateSprite(TextureWrapMode.Clamp, SpriteMeshType.FullRect, Border);
            for (var i = 0; i < spritesCount; i++)
            {
                var slicedImage = CreateSlicedImage(sprite, false, false, true, 100, Vector2Int.one);
                slicedImage.sliced = true;
            }

            yield return CollectStatistics(changeGridSize);

            Object.DestroyImmediate(sprite.texture);
            Object.DestroyImmediate(sprite);
        }
    }
}