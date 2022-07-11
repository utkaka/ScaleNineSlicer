using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace ScaleNineSlicer {
	public struct DetectBorderJob : IJobFor {
		private const float DiffConstant = 256.0f / 255.0f;
		private const float MaxDifference = 3.16f;

		public float Tolerance;
		public int Width;
		[ReadOnly, DeallocateOnJobCompletion]
		public NativeArray<Color> Pixels;
		[WriteOnly]
		public NativeArray<bool> HorizontalBorders;
		[WriteOnly]
		public NativeArray<bool> VerticalBorders;
		public NativeArray<int4> AlphaRect;
		public int PixelsCountMinusOne;
		
		public void Execute(int index) {
			var x = index % Width;
			var y = (int)math.floor((float)index / Width);
			if (x > 0 && ColorDifference(Pixels[index], Pixels[index - 1]) > MaxDifference * Tolerance) {
				VerticalBorders[x] = true;
			}
			if (y > 0 && ColorDifference(Pixels[index], Pixels[(y - 1) * Width + x]) > MaxDifference * Tolerance) {
				HorizontalBorders[y] = true;
			}
			AlphaRect[0] = DetectOpaqueRectJob.CheckIndex(Pixels, index, AlphaRect[0], Width, PixelsCountMinusOne);
		}

		private float ColorDifference(Color color1, Color color2) {
			var r = 0.5f * (color1.r + color2.r);
			var deltaR = color1.r - color2.r;
			var deltaG = color1.g - color2.g;
			var deltaB = color1.b - color2.b;
			var deltaA = color1.a - color2.a;
			var delta = math.sqrt((2.0f + r / DiffConstant) * deltaR * deltaR + 4.0f * deltaG * deltaG +
			                      (2.0f + (1.0f - r) / DiffConstant) * deltaB * deltaB + deltaA * deltaA);
			return delta;
		}
	}
}