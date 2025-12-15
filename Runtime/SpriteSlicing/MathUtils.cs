using Unity.Mathematics;
using UnityEngine;

namespace Utkaka.ScaleNineSlicer.SpriteSlicing {
	public static class MathUtils {
		public static int4 ToInt4(this Vector4 vector4) {
			return new int4((int)vector4.x, (int)vector4.y, (int)vector4.z, (int)vector4.w);
		}

		public static Vector4 ToVector4(this int4 int4) {
			return new Vector4(int4.x, int4.y, int4.z, int4.w);
		}
	}
}