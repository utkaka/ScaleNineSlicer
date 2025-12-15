using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Utkaka.ScaleNineSlicer.SpriteSlicing {
	public struct Border {
		public int2 SelectedBorder;
		public int2 CurrentBorder;
	}
	
	public struct SelectBordersJob : IJobFor {
		public float PreferablePosition;
		public int2 AlphaBounds;
		[ReadOnly, DeallocateOnJobCompletion]
		public NativeArray<bool> AllBorders;
		public NativeArray<Border> Border;

		public void Execute(int index) {
			var border = Border[0];
			if (index < AlphaBounds.x || index > AlphaBounds.y) return;
			var selectedBorder = border.SelectedBorder;
			var currentBorder = border.CurrentBorder;
			currentBorder = new int2(AllBorders[index] ? index : currentBorder.x, index + 1);
			if (selectedBorder.x == currentBorder.x) {
				selectedBorder = currentBorder;
			} else {
				var selectedBorderLength = selectedBorder.y - selectedBorder.x;
				var currentBorderLength = currentBorder.y - currentBorder.x;
				if (currentBorderLength > selectedBorderLength ||
				    currentBorderLength == selectedBorderLength &&
				    math.abs(currentBorder.x + currentBorderLength / 2.0f - PreferablePosition) <
				    math.abs(selectedBorder.x + selectedBorderLength / 2.0f - PreferablePosition)) {
					selectedBorder = currentBorder;
				}
			}
			Border[0] = new Border() { SelectedBorder = selectedBorder, CurrentBorder = currentBorder};
		}
	}
}