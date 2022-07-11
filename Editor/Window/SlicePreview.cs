using Unity.Mathematics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ScaleNineSlicer.Editor {
	public class SlicePreview {
		private readonly VisualElement _root;
		private readonly VisualElement _sizeWindow;
		private readonly VisualElement _image;
		private readonly VisualElement _imageArea;
		private readonly IntegerField _widthInput;
		private readonly IntegerField _heightInput;
		private readonly Button _restoreButton;
		private readonly Button _exportButton;
		
		private int _originalWidth;
		private int _originalHeight;
		private SpriteInfo _spriteInfo;
		private int _extendedWidth;
		private int _extendedHeight;

		public SlicePreview(VisualElement root) {
			_root = root;
			_sizeWindow = _root.Q<VisualElement>(className:"overlay-window");
			_root.Q<VisualElement>(name:"unity-content-viewport").Add(_sizeWindow);
			_image = _root.Q<VisualElement>(name:"Image");
			_imageArea = _root.Q<VisualElement>(name:"Area");
			_widthInput = _root.Q<IntegerField>(name:"Width");
			_heightInput = _root.Q<IntegerField>(name:"Height");
			_restoreButton = _root.Q<Button>(name:"Restore");
			_exportButton = _root.Q<Button>(name:"Export");
			
			_restoreButton.RegisterCallback<ClickEvent>(OnRestore);
			_exportButton.RegisterCallback<ClickEvent>(OnExport);
			_widthInput.RegisterValueChangedCallback(OnSizeChange);
			_heightInput.RegisterValueChangedCallback(OnSizeChange);
		}

		public void SetSpriteInfo(SpriteInfo spriteInfo) {
			_spriteInfo = spriteInfo;
			var backgroundImage = _image.style.backgroundImage;
			backgroundImage.value = new Background{sprite = spriteInfo.CreateSprite()};
			_image.style.backgroundImage = backgroundImage;
			_originalWidth = spriteInfo.Width;
			_originalHeight = spriteInfo.Height;

			_imageArea.style.width = _originalWidth;
			_imageArea.style.height = _originalHeight;
			_widthInput.SetValueWithoutNotify(_originalWidth);
			_heightInput.SetValueWithoutNotify(_originalHeight);
		}

		private void OnRestore(ClickEvent evt) {
			_widthInput.SetValueWithoutNotify(_originalWidth);
			_heightInput.SetValueWithoutNotify(_originalHeight);
			OnSizeChange(null);
		}
		
		private void OnExport(ClickEvent evt) {
			var path = EditorUtility.SaveFilePanel("Save as PNG", "", "export.png", "png");
			if (string.IsNullOrEmpty(path)) return;

			var extendedSpriteInfo = _spriteInfo.GetExtendedSpriteInfo(_extendedWidth, _extendedHeight);
			extendedSpriteInfo.WriteTextureToFile(path);
		}

		private void OnSizeChange(ChangeEvent<int> evt) {
			_extendedWidth = math.max(_originalWidth, _widthInput.value);
			_extendedHeight = math.max(_originalHeight, _heightInput.value);

			_imageArea.style.width = _extendedWidth;
			_imageArea.style.height = _extendedHeight;
		}
	}
}