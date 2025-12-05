using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Utkaka.ScaleNineSlicer.SpriteSlicing {
	public struct TrimSliceCenterJob : IJobParallelFor {
		[ReadOnly, DeallocateOnJobCompletion, NativeDisableParallelForRestriction]
		private NativeArray<Color> _originalPixels;
		[WriteOnly]
		private NativeArray<Color> _trimmedPixels;

		private readonly int _trimmedWidth;
		private readonly int4 _centerPart;
		private readonly int _innerRectWidth;
		private readonly int _innerHorizontalArea;

		public TrimSliceCenterJob(NativeArray<Color> originalPixels, NativeArray<Color> trimmedPixels,
			int originalWidth, int4 centerPart, int2 centerPartSize) {
			_originalPixels = originalPixels;
			_trimmedPixels = trimmedPixels;
			_centerPart = centerPart;
			_innerRectWidth = centerPartSize.x;
			_trimmedWidth = originalWidth - centerPartSize.x;
			_centerPart.z -= centerPartSize.x;
			_centerPart.w -= centerPartSize.y;
			_innerHorizontalArea = centerPartSize.y * originalWidth;
		}

		public void Execute(int index) {
			var originalIndex = index;
			var x = index % _trimmedWidth;
			var y = (int)math.floor((float)index / _trimmedWidth);
			originalIndex += y * _innerRectWidth;
			if (x >= _centerPart.z) originalIndex += _innerRectWidth;
			if (y >= _centerPart.w) {
				originalIndex += _innerHorizontalArea;
			}
			_trimmedPixels[index] = _originalPixels[originalIndex];
		}
	}
}