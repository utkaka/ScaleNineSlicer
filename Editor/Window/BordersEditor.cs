using System;
using Unity.Mathematics;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ScaleNineSlicer.Editor {
	public class BordersEditor {
        private readonly VisualElement _root;
        private readonly VisualElement _imageArea;
        private readonly VisualElement _image;
        private UQueryBuilder<VisualElement> _guideBarQuery;
        
        private readonly VisualElement _guideLeft;
        private readonly VisualElement _guideRight;
        private readonly VisualElement _guideTop;
        private readonly VisualElement _guideBottom;
        
        private readonly IntegerField _borderLeft;
        private readonly IntegerField _borderRight;
        private readonly IntegerField _borderTop;
        private readonly IntegerField _borderBottom;

        private readonly Slider _toleranceSlider;

        private VisualElement _selectedGuide;

        private SpriteInfo _spriteInfo;
        private int2 _imageSize;
        private float _scale;

        public BordersEditor(VisualElement root) {
            _root = root;
            var borderWindow = _root.Q<VisualElement>(className:"overlay-window");
            _borderLeft = borderWindow.Q<IntegerField>(name:"Left");
            _borderRight = borderWindow.Q<IntegerField>(name:"Right");
            _borderTop = borderWindow.Q<IntegerField>(name:"Top");
            _borderBottom = borderWindow.Q<IntegerField>(name:"Bottom");
            borderWindow.Query<IntegerField>().ForEach((e) => {
                e.RegisterCallback<ChangeEvent<int>>(OnBorderValueChange);
            });
            borderWindow.Q<Button>(name: "Detect").RegisterCallback<ClickEvent>(OnDetectBorderClick);
            _toleranceSlider = borderWindow.Q<Slider>(className: "tolerance-slider");
            _root.Q<VisualElement>(name:"unity-content-viewport").Add(borderWindow);
            
            _imageArea = _root.Q<VisualElement>(name:"Area");
            _image = _root.Q<VisualElement>(name:"Image");
            _guideBarQuery = _root.Query(className:"guide-bar");
            var guides = _root.Q<VisualElement>(name:"Guides");
            _guideLeft = guides.Q<VisualElement>(name:"Left");
            _guideRight = guides.Q<VisualElement>(name:"Right");
            _guideTop = guides.Q<VisualElement>(name:"Top");
            _guideBottom = guides.Q<VisualElement>(name:"Bottom");
            RegisterCallbacks();
        }

        public void SetSpriteInfo(SpriteInfo spriteInfo) {
            _spriteInfo = spriteInfo;
            _root.style.display = DisplayStyle.Flex;
            var backgroundImage = _image.style.backgroundImage;
            backgroundImage.value = new Background{sprite = _spriteInfo.CreateSprite(FilterMode.Point, int4.zero)};
            _image.style.backgroundImage = backgroundImage;
            _scale = 1.0f;
            SetScale(1.0f);
            UpdateBorders();
        }

        private void RegisterCallbacks() {
            if (_selectedGuide == null) {
                _guideBarQuery.ForEach((element) => {
                    element.RegisterCallback<PointerDownEvent>(OnGuidePointerDown);
                    element.RegisterCallback<PointerEnterEvent>(OnGuidePointerEnter);
                    element.RegisterCallback<PointerOutEvent>(OnGuidePointerOut);
                });
                _root.RegisterCallback<WheelEvent>(WheelEvent, TrickleDown.TrickleDown);
                _imageArea.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
                _root.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            } else {
                _guideBarQuery.ForEach((element) => {
                    element.UnregisterCallback<PointerDownEvent>(OnGuidePointerDown);
                    element.UnregisterCallback<PointerEnterEvent>(OnGuidePointerEnter);
                    element.UnregisterCallback<PointerOutEvent>(OnGuidePointerOut);
                });
                _root.UnregisterCallback<WheelEvent>(WheelEvent, TrickleDown.TrickleDown);
                _imageArea.RegisterCallback<PointerMoveEvent>(OnPointerMove);
                _root.RegisterCallback<MouseUpEvent>(OnMouseUp);
            }
        }
        
        private void OnBorderValueChange(ChangeEvent<int> evt) {
            SetBorderSideValue(((VisualElement) evt.target).name, evt.newValue);
        }

        private void WheelEvent(WheelEvent evt) {
            if (!evt.actionKey) return;
            SetScale(_scale + evt.delta.y / -5.0f);
            evt.StopImmediatePropagation();
        }
		
		private void OnPointerMove(PointerMoveEvent evt) {
            var pixelPosition = evt.localPosition / _scale;
            pixelPosition = new Vector3(Mathf.Round(pixelPosition.x), Mathf.Round(pixelPosition.y)) * _scale;
            var value = 0; 
            switch (_selectedGuide.name) {
                case "Left":
                    value = (int)pixelPosition.x;
                    break;
                case "Bottom":
                    value = _imageSize.y - (int)pixelPosition.y;
                    break;
                case "Right":
                    value = _imageSize.x - (int)pixelPosition.x;
                    break;
                case "Top":
                    value = (int)pixelPosition.y;
                    break;
            }
            SetBorderSideValue(_selectedGuide.name, Mathf.RoundToInt(value /_scale));
        }
        
        private void OnMouseUp(MouseUpEvent evt) {
            _selectedGuide.RemoveFromClassList("active");
            _selectedGuide = null;
            RegisterCallbacks();
        }

        private void OnGuidePointerDown(PointerDownEvent evt) {
            if (evt.currentTarget is not VisualElement guide) return;
            _selectedGuide = guide.parent;
            RegisterCallbacks();
        }

        private void OnGuidePointerEnter(PointerEnterEvent evt) {
            if (evt.currentTarget is not VisualElement guide) return;
            guide.parent.AddToClassList("active");
        }
        
        private void OnGuidePointerOut(PointerOutEvent evt) {
            if (evt.currentTarget is not VisualElement guide) return;
            guide.parent.RemoveFromClassList("active");
        }
        
        private void OnDetectBorderClick(ClickEvent evt) {
            _spriteInfo.AutoDetectBorder(_toleranceSlider.value);
            PlaceGuides();
            UpdateBorders();
        }

        private void SetBorderSideValue(string side, int value) {
            var border = _spriteInfo.Border;
            switch (side) {
                case "Left":
                    border.x = Mathf.Max(0,Mathf.Min(value, _spriteInfo.Width - border.z - 1));
                    break;
                case "Bottom":
                    border.y = Mathf.Max(0,Mathf.Min(value, _spriteInfo.Height - border.w - 1));
                    break;
                case "Right":
                    border.z = Mathf.Max(0,Mathf.Min(value, _spriteInfo.Width - border.x - 1));
                    break;
                case "Top":
                    border.w = Mathf.Max(0,Mathf.Min(value, _spriteInfo.Height - border.y - 1));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, null);
            }
            _spriteInfo.Border = border;
            PlaceGuides();
            UpdateBorders();
        }

        private void SetScale(float value) {
            value = Mathf.Max(1.0f, value);
            _scale = value;
            _imageSize = new int2(Mathf.RoundToInt(_spriteInfo.Width * _scale), Mathf.RoundToInt(_spriteInfo.Height * _scale));
            _imageArea.style.width = _imageSize.x;
            _imageArea.style.height = _imageSize.y;
            PlaceGuides();
        }

        private void PlaceGuides() {
            _guideLeft.style.left = _spriteInfo.Border.x * _scale;
            _guideBottom.style.bottom = _spriteInfo.Border.y * _scale;
            _guideRight.style.right = _spriteInfo.Border.z * _scale;
            _guideTop.style.top = _spriteInfo.Border.w * _scale;
        }

        private void UpdateBorders() {
            _borderLeft.SetValueWithoutNotify(_spriteInfo.Border.x);
            _borderBottom.SetValueWithoutNotify(_spriteInfo.Border.y);
            _borderRight.SetValueWithoutNotify(_spriteInfo.Border.z);
            _borderTop.SetValueWithoutNotify(_spriteInfo.Border.w);
        }
    }
}