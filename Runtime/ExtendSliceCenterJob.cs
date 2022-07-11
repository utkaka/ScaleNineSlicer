using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace ScaleNineSlicer {
	public struct ExtendSliceCenterJob : IJobParallelFor{
		[ReadOnly, DeallocateOnJobCompletion, NativeDisableParallelForRestriction]
		private NativeArray<Color> _originalPixels;
		[WriteOnly]
		private NativeArray<Color> _extendedPixels;

		private readonly int _originalWidth;
		private readonly int _extendedWidth;
		private readonly int2 _extensionSize;
		private readonly int4 _extendedCenterPart;

		public ExtendSliceCenterJob(NativeArray<Color> originalPixels, NativeArray<Color> extendedPixels, int2 originalSize,
			int2 extendedSize, int4 originalBorders) {
			_originalPixels = originalPixels;
			_extendedPixels = extendedPixels;
			_originalWidth = originalSize.x;
			_extendedWidth = extendedSize.x;
			_extensionSize = extendedSize - originalSize;
			_extendedCenterPart = new int4(originalBorders.x, originalBorders.y,
				originalSize.x - originalBorders.z + _extensionSize.x,
				originalSize.y - originalBorders.w + _extensionSize.y);
		}

		public void Execute(int index) {
			var x = index % _extendedWidth;
			var y = (int)math.floor((float)index / _extendedWidth);
			
			var below = y < _extendedCenterPart.y;
			var above = y >= _extendedCenterPart.w;
			var left = x < _extendedCenterPart.x;
			var right = x >= _extendedCenterPart.z;
			
			if (!left && !right) {
				x = _extendedCenterPart.x;
			} else if (right) {
				x -= _extensionSize.x;
			}
			
			if (!below && !above) {
				y = _extendedCenterPart.y;
			} else if (above) {
				y -= _extensionSize.y;
			}
			
			var originalIndex = _originalWidth * y + x;
			_extendedPixels[index] = _originalPixels[originalIndex];
		}
	}
}