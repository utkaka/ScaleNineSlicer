using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace ScaleNineSlicer {
	public struct DetectOpaqueRectJob : IJobFor {
		[ReadOnly]
		private NativeArray<Color> _pixels;
		private NativeArray<int4> _opaqueRect;
		private int _width;
		private int _pixelsCountMinusOne;

		public DetectOpaqueRectJob(NativeArray<Color> pixels, NativeArray<int4> opaqueRect, int width) {
			_pixels = pixels;
			_opaqueRect = opaqueRect;
			_width = width;
			_pixelsCountMinusOne = _pixels.Length - 1;
		}

		public void Execute(int index) {
			_opaqueRect[0] = CheckIndex(_pixels, index, _opaqueRect[0], _width, _pixelsCountMinusOne);
		}

		public static int4 CheckIndex(NativeArray<Color> pixels, int index, int4 rect, int width, int pixelsCountMinusOne) {
			if (pixels[index].a > 0.0f) {
				var x = index % width;
				var y = (int)math.floor((float)index / width);
				if (rect.x == -1 || x < rect.x) {
					rect.x = x;
				}
				if (rect.y == -1 || y < rect.y) {
					rect.y = y;
				}	
			}
			var reversedIndex = pixelsCountMinusOne - index;
			if (pixels[reversedIndex].a > 0.0f) {
				var x = reversedIndex % width;
				var y = (int)math.floor((float)reversedIndex / width);
				if (rect.z == -1 || x > rect.z) {
					rect.z = x;
				}

				if (rect.w == -1 || y > rect.w) {
					rect.w = y;
				}	
			}
			return rect;
		}
	}
}