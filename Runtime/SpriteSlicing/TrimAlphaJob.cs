using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Utkaka.ScaleNineSlicer.SpriteSlicing {
	public struct TrimAlphaJob : IJobParallelFor {
		[ReadOnly, NativeDisableParallelForRestriction]
		private NativeArray<Color> _originalPixels;
		[WriteOnly]
		private NativeArray<Color> _trimmedPixels;

		private readonly int _opaqueAreaStartIndex;
		private readonly int _opaqueAreaWidth;
		private int _widthDiff;

		public TrimAlphaJob(NativeArray<Color> originalPixels, NativeArray<Color> trimmedPixels, int4 opaqueRect, int originalWidth) {
			_originalPixels = originalPixels;
			_trimmedPixels = trimmedPixels;
			_opaqueAreaStartIndex = originalWidth * opaqueRect.y + opaqueRect.x;
			_opaqueAreaWidth = opaqueRect.z;
			_widthDiff = originalWidth - _opaqueAreaWidth;
		}

		public void Execute(int index) {
			var y = (int)math.floor((float)index / _opaqueAreaWidth);
			var originalIndex = index + _opaqueAreaStartIndex + y * _widthDiff;
			_trimmedPixels[index] = _originalPixels[originalIndex];
		}
	}
}