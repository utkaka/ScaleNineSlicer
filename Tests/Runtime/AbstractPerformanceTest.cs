using System.Collections;
using NUnit.Framework;
using Unity.PerformanceTesting;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UI;

namespace Utkaka.ScaleNineSlicer.Tests.Runtime
{
    public class AbstractPerformanceTest : AbstractImageTest
    {
        protected static readonly int[] SpritesCount = {100, 1000, 10000};
        protected static readonly bool[] ChangeGridSize = {false, true};
        
        private GridLayoutGroup _grid;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _grid = ImageContainer.gameObject.AddComponent<GridLayoutGroup>();
            _grid.padding = new RectOffset(0, 0, 0, 0);
            _grid.spacing = Vector2.zero;
            _grid.constraint = GridLayoutGroup.Constraint.Flexible;
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }

        protected void AdjustGridSize(int totalCount)
        {
            var sqrt = Mathf.Sqrt(totalCount);
            var cellWidth = Screen.width / (float)sqrt;
            var cellHeight = Screen.height / (float)sqrt;
            _grid.cellSize = new Vector2(cellWidth, cellHeight);
        }
        
        protected IEnumerator CollectStatistics(bool changeGridSize)
        {
            using var totalUsedMem = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");
            using var gcAlloc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            using var batchesCount = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            
            var endOfFrame = new WaitForEndOfFrame();

            for (var i = 0; i < 60; i++)
            {
                yield return endOfFrame;
            }
            
            for (var i = 0; i < 60; i++)
            {
                if (changeGridSize)
                {
                    _grid.cellSize += new Vector2(0, 0.01f);
                }
                Measure.Scope("PlayerLoop");
                Measure.Custom(
                    new SampleGroup("Memory", SampleUnit.Megabyte), BytesToMB(totalUsedMem.LastValue));
                Measure.Custom(
                    new SampleGroup("GC Allocations", SampleUnit.Kilobyte), BytesToKB(gcAlloc.LastValue));
                yield return endOfFrame;
            }
            Measure.Custom(
                new SampleGroup("Rander batches", SampleUnit.Undefined), batchesCount.LastValue);
        }
        
        private static double BytesToKB(double bytes) => bytes / (1024.0 );
        private static double BytesToMB(double bytes) => BytesToKB(bytes) / (1024.0 );
    }
}