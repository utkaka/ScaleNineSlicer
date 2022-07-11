using System;
using System.IO;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace ScaleNineSlicer {
	public class SpriteInfo {
		public event Action BorderChange;
		
		private Color[] _pixels;
		private int _width;
		private int _height;
		private int4 _border;
		private readonly FilterMode _filterMode;
		public int Width => _width;

		public int Height => _height;

		public Color[] Pixels => _pixels;

		public int4 Border {
			get => _border;
			set {
				if (_border.x >= _width - _border.z) {
					throw new ArgumentException("Border's left has to be less than (width - right)");
				}
				if (_border.y >= _height - _border.w) {
					throw new ArgumentException("Border's bottom has to be less than (height - top)");
				}
				_border = value;
				BorderChange?.Invoke();
			}
		}

		public SpriteInfo(string imagePath, int width, int height, int4 border, FilterMode filterMode) {
			var imageBytes = File.ReadAllBytes(imagePath);
			var tempTexture = new Texture2D(width, height,
				TextureFormat.RGBA32, false);
			tempTexture.LoadImage(imageBytes);
			_pixels = tempTexture.GetPixels();
			_width = width;
			_height = height;
			_filterMode = filterMode;
			Border = border;
		}

		public SpriteInfo(Color[] pixels, int width, int height, int4 border, FilterMode filterMode) {
			_pixels = pixels;
			_width = width;
			_height = height;
			_filterMode = filterMode;
			Border = border;
		}
		
		public SpriteInfo(SpriteInfo spriteInfo) {
			_pixels = spriteInfo._pixels;
			_width = spriteInfo._width;
			_height = spriteInfo._height;
			_filterMode = spriteInfo._filterMode;
			Border = spriteInfo._border;
		}

		public void WriteTextureToFile(string path) {
			var texture = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
			texture.SetPixels(_pixels);
			texture.Apply();
			byte[] bytes;
			var extension = Path.GetExtension(path).ToLower();
			switch (extension) {
				case "jpeg":
				case "jpg":
					bytes = texture.EncodeToJPG();
					break;
				case "exr":
					bytes = texture.EncodeToEXR();
					break;
				case "tga":
					bytes = texture.EncodeToTGA();
					break;
				default:
					bytes = texture.EncodeToPNG();
					break;
			}
			File.WriteAllBytes(path, bytes);
		}

		public Sprite CreateSprite() {
			return CreateSprite(_filterMode, _border);
		}
		
		public Sprite CreateSprite(FilterMode filterMode, int4 border) {
			var texture = new Texture2D(_width, _height);
			texture.filterMode = filterMode;
			texture.SetPixels(_pixels);
			texture.Apply();
			return Sprite.Create(texture, new Rect(0.0f, 0.0f, _width, _height), new Vector2(0.5f, 0.5f),
				100.0f, 1, SpriteMeshType.FullRect, border.ToVector4());
		}
		
		public void AutoDetectBorder(float tolerance) {
			var opaqueRectArray = new NativeArray<int4>(new[] {new int4(-1, -1, -1, -1)}, Allocator.TempJob);
			var pixelsNativeArray = new NativeArray<Color>(_pixels, Allocator.TempJob);
			var horizontalBorders = new NativeArray<bool>(_height, Allocator.TempJob);
			var verticalBorders = new NativeArray<bool>(_width, Allocator.TempJob);
			var detectAllBordersJob = new DetectBorderJob() {
				Pixels = pixelsNativeArray, HorizontalBorders = horizontalBorders,
				VerticalBorders = verticalBorders, Width = _width, Tolerance = tolerance,
				AlphaRect = opaqueRectArray, PixelsCountMinusOne = _pixels.Length - 1
			};

			 detectAllBordersJob.Schedule(_width * _height, default).Complete();

			 var opaqueRect = opaqueRectArray[0];
			 opaqueRectArray.Dispose();

			 var verticalBorderNativeArray = new NativeArray<Border>(
				new[] {new Border{CurrentBorder = new int2(opaqueRect.x, opaqueRect.x + 1)}},
				Allocator.TempJob);
			var selectVerticalBordersJob = new SelectBordersJob() {
				PreferablePosition = _width / 2.0f, AllBorders =  verticalBorders,
				Border = verticalBorderNativeArray, AlphaBounds = new int2(opaqueRect.x, opaqueRect.z)
			};
			var horizontalBorderNativeArray = new NativeArray<Border>(
				new[] {new Border{CurrentBorder = new int2(opaqueRect.y, opaqueRect.y + 1)}},
				Allocator.TempJob);
			var selectHorizontalBordersJob = new SelectBordersJob() {
				PreferablePosition = _height / 2.0f, AllBorders =  horizontalBorders,
				Border = horizontalBorderNativeArray, AlphaBounds = new int2(opaqueRect.y, opaqueRect.w)
			};

			var verticalHandle = selectVerticalBordersJob.Schedule(_width, default);
			var horizontalHandle = selectHorizontalBordersJob.Schedule(_height, default);
			
			verticalHandle.Complete();
			horizontalHandle.Complete();

			var selectedHorizontalBorder = horizontalBorderNativeArray[0].SelectedBorder;
			var selectedVerticalBorder = verticalBorderNativeArray[0].SelectedBorder;

			verticalBorderNativeArray.Dispose();
			horizontalBorderNativeArray.Dispose();
			
			Border = new int4(selectedVerticalBorder.x, selectedHorizontalBorder.x,
				_width - selectedVerticalBorder.y, _height - selectedHorizontalBorder.y);
		}
		
		public void TrimAlpha() {
			var opaqueRectNativeArray = new NativeArray<int4>(1, Allocator.TempJob);
			opaqueRectNativeArray[0] = new int4(-1, -1, -1, -1);
			var pixelsNativeArray = new NativeArray<Color>(_pixels, Allocator.TempJob);
			var detectOpaqueRectJob = new DetectOpaqueRectJob(pixelsNativeArray, opaqueRectNativeArray, _width);
			detectOpaqueRectJob.Schedule(_pixels.Length,default).Complete();
			var opaqueRect = opaqueRectNativeArray[0];
			opaqueRect.z = opaqueRect.z - opaqueRect.x + 1;
			opaqueRect.w = opaqueRect.w - opaqueRect.y + 1;

			var trimmedPixelsCount = opaqueRect.z * opaqueRect.w;
			var trimmedPixels = new NativeArray<Color>(trimmedPixelsCount, Allocator.TempJob);
			var trimJob = new TrimAlphaJob(pixelsNativeArray, trimmedPixels, opaqueRect, _width);
			var sliceCount = trimmedPixelsCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount;
			trimJob.Schedule(trimmedPixelsCount, sliceCount).Complete();
			
			_border.x -= opaqueRect.x;
			_border.y -= opaqueRect.y;
			_border.z -= _width - opaqueRect.z - opaqueRect.x;
			_border.w -= _height - opaqueRect.w - opaqueRect.y;

			_border.x = Mathf.Max(0, _border.x);
			_border.y = Mathf.Max(0, _border.y);
			_border.z = Mathf.Max(0, _border.z);
			_border.w = Mathf.Max(0, _border.w);
			
			_width = opaqueRect.z;
			_height = opaqueRect.w;
			
			_pixels = trimmedPixels.ToArray();
			
			pixelsNativeArray.Dispose();
			opaqueRectNativeArray.Dispose();
			trimmedPixels.Dispose();
		}
		
		public void TrimCenter() {
			if (_border.Equals(int4.zero)) return;
			var originalPixels = new NativeArray<Color>(_pixels, Allocator.TempJob);
			var originalPixelsCount = _pixels.Length;
			var centerPart = new int4(Mathf.Max(0, _border.x + 1), Mathf.Max(0, _border.y + 1),
				Mathf.Min(_width, _width - _border.z), Mathf.Min(_height, _height - _border.w));
			var centerPartSize = new int2(centerPart.z - centerPart.x, centerPart.w - centerPart.y);
			var trimmedPixelsCount = originalPixelsCount - centerPartSize.x * _height -
				centerPartSize.y * _width + centerPartSize.x * centerPartSize.y;
			var trimmedPixels =
				new NativeArray<Color>(trimmedPixelsCount, Allocator.TempJob);
			var sliceCount = trimmedPixelsCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount;

			var trimSliceCenterJob = new TrimSliceCenterJob(originalPixels, trimmedPixels, _width, centerPart, centerPartSize);
			trimSliceCenterJob.Schedule(trimmedPixelsCount, sliceCount).Complete();

			_width -= centerPartSize.x;
			_height -= centerPartSize.y;
			_pixels = trimmedPixels.ToArray();
			trimmedPixels.Dispose();
		}

		public SpriteInfo GetExtendedSpriteInfo(int extendedWidth, int extendedHeight) {
			var originalPixels = new NativeArray<Color>(_pixels, Allocator.TempJob);
			var extendedPixelsCount = extendedWidth * extendedHeight;
			var extendedPixels =
				new NativeArray<Color>(extendedPixelsCount, Allocator.TempJob);
			var sliceCount = extendedPixelsCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount;
			var extendSliceCenterJob = new ExtendSliceCenterJob(originalPixels, extendedPixels,
				new int2(_width, _height), new int2(extendedWidth, extendedHeight),
				_border);
			extendSliceCenterJob.Schedule(extendedPixelsCount, sliceCount).Complete();

			var result = new SpriteInfo(extendedPixels.ToArray(), extendedWidth, extendedHeight, _border, _filterMode);

			extendedPixels.Dispose();
			
			return result;
		}
	}
}