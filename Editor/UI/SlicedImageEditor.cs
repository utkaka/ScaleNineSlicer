using System.Linq;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;
using Utkaka.ScaleNineSlicer.UI;

namespace Utkaka.ScaleNineSlicer.Editor.UI
{
    [CustomEditor(typeof(SlicedImage), true)]
    [CanEditMultipleObjects]
    public class ImageEditor : GraphicEditor
    {
        private GUIContent _spriteContent;
        private GUIContent _spriteTypeContent;
        private GUIContent _clockwiseContent;
        
        private SerializedProperty _spriteProperty;
        private SerializedProperty _preserveAspectProperty;
        private SerializedProperty _useSpriteMeshProperty;
        private SerializedProperty _pixelsPerUnitMultiplierProperty;
        
        private SerializedProperty _slicedProperty;
        private SerializedProperty _fillCenterProperty;
        private SerializedProperty _tileScaledSlicesProperty;
        private SerializedProperty _slicedTileSizeProperty;
        
        private SerializedProperty _tiledProperty;
        private SerializedProperty _tileSizeProperty;
        private SerializedProperty _tileSpacingProperty;
        
        private SerializedProperty _filledProperty;
        private SerializedProperty _fillMethodProperty;
        private SerializedProperty _fillClockwiseProperty;
        private SerializedProperty _fillOriginProperty;
        private SerializedProperty _fillAmountProperty;
        private SerializedProperty _customFillingProperty;
        
        private AnimBool _showSlicedOptions;
        private AnimBool _showTiledOptions;
        private AnimBool _showFillOptions;
        
        private bool _isDriven;

        private class Styles
        {
            public static GUIContent text = EditorGUIUtility.TrTextContent("Fill Origin");
            public static GUIContent[] OriginHorizontalStyle =
            {
                EditorGUIUtility.TrTextContent("Left"),
                EditorGUIUtility.TrTextContent("Right")
            };

            public static GUIContent[] OriginVerticalStyle =
            {
                EditorGUIUtility.TrTextContent("Bottom"),
                EditorGUIUtility.TrTextContent("Top")
            };

            public static GUIContent[] Origin90Style =
            {
                EditorGUIUtility.TrTextContent("BottomLeft"),
                EditorGUIUtility.TrTextContent("TopLeft"),
                EditorGUIUtility.TrTextContent("TopRight"),
                EditorGUIUtility.TrTextContent("BottomRight")
            };

            public static GUIContent[] Origin180Style =
            {
                EditorGUIUtility.TrTextContent("Bottom"),
                EditorGUIUtility.TrTextContent("Left"),
                EditorGUIUtility.TrTextContent("Top"),
                EditorGUIUtility.TrTextContent("Right")
            };

            public static GUIContent[] Origin360Style =
            {
                EditorGUIUtility.TrTextContent("Bottom"),
                EditorGUIUtility.TrTextContent("Right"),
                EditorGUIUtility.TrTextContent("Top"),
                EditorGUIUtility.TrTextContent("Left")
            };
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            _spriteContent = EditorGUIUtility.TrTextContent("Source Image");
            _spriteProperty = serializedObject.FindProperty("_sprite");
            _preserveAspectProperty = serializedObject.FindProperty("_preserveAspect");
            _useSpriteMeshProperty = serializedObject.FindProperty("_useSpriteMesh");
            _pixelsPerUnitMultiplierProperty = serializedObject.FindProperty("_pixelsPerUnitMultiplier");
            
            _slicedProperty = serializedObject.FindProperty("_sliced");
            _fillCenterProperty = serializedObject.FindProperty("_fillCenter");
            _tileScaledSlicesProperty = serializedObject.FindProperty("_tileScaledSlices");
            _slicedTileSizeProperty = serializedObject.FindProperty("_slicedTileSize");
            _showSlicedOptions = new AnimBool(_slicedProperty.boolValue);
            _showSlicedOptions.valueChanged.AddListener(Repaint);
            
            _tiledProperty = serializedObject.FindProperty("_tiled");
            _tileSizeProperty = serializedObject.FindProperty("_tileSize");
            _tileSpacingProperty = serializedObject.FindProperty("_tileSpacing");
            _showTiledOptions = new AnimBool(_tiledProperty.boolValue);
            _showTiledOptions.valueChanged.AddListener(Repaint);
            
            _filledProperty = serializedObject.FindProperty("_filled");
            _fillMethodProperty = serializedObject.FindProperty("_fillMethod");
            _fillClockwiseProperty = serializedObject.FindProperty("_fillClockwise");
            _fillOriginProperty = serializedObject.FindProperty("_fillOrigin");
            _fillAmountProperty = serializedObject.FindProperty("_fillAmount");
            _customFillingProperty = serializedObject.FindProperty("_customFilling");
            _showFillOptions = new AnimBool(_filledProperty.boolValue);
            _showFillOptions.valueChanged.AddListener(Repaint);
            
            _clockwiseContent      = EditorGUIUtility.TrTextContent("Clockwise");
            
            _isDriven = false;
            
            SetShowNativeSize(true);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _showSlicedOptions.valueChanged.RemoveListener(Repaint);
            _showTiledOptions.valueChanged.RemoveListener(Repaint);
            _showFillOptions.valueChanged.RemoveListener(Repaint);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var image = target as SlicedImage;
            var rect = image.GetComponent<RectTransform>();
            _isDriven = (rect.drivenByObject as Slider)?.fillRect == rect;

            SpriteGUI();
            AppearanceControlsGUI();
            RaycastControlsGUI();
            MaskableControlsGUI();
            SimpleGUI();
            SlicedGUI();
            TiledGUI();
            FilledGUI();
            
            NativeSizeButtonGUI();
            
            serializedObject.ApplyModifiedProperties();
        }

        void SetShowNativeSize(bool instant)
        {
            bool showNativeSize = true;
            base.SetShowNativeSize(showNativeSize, instant);
        }
        
        private void SpriteGUI()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_spriteProperty, _spriteContent);
            if (!EditorGUI.EndChangeCheck()) return;
            (serializedObject.targetObject as SlicedImage)?.DisableSpriteOptimizations();
        }

        protected void SimpleGUI()
        {
            DrawDisablableProperty(_filledProperty.boolValue || _tiledProperty.boolValue || _slicedProperty.boolValue, 
                _useSpriteMeshProperty, "Does not apply to sliced, tiled, or filled images.");
            DrawDisablableProperty(_tiledProperty.boolValue || _slicedProperty.boolValue, 
                _preserveAspectProperty, "Does not apply to sliced or tiled images.");
            DrawDisablableProperty(!_tiledProperty.boolValue && !_slicedProperty.boolValue, 
                _pixelsPerUnitMultiplierProperty, "Does not apply to non sliced or non tiled images.");
        }

        protected void SlicedGUI()
        {
            EditorGUILayout.PropertyField(_slicedProperty);
            _showSlicedOptions.target = _slicedProperty.boolValue && !_slicedProperty.hasMultipleDifferentValues;
            EditorGUI.indentLevel++;
            if (EditorGUILayout.BeginFadeGroup(_showSlicedOptions.faded))
            {
                EditorGUILayout.PropertyField(_fillCenterProperty);
                EditorGUILayout.PropertyField(_tileScaledSlicesProperty);
                EditorGUILayout.PropertyField(_slicedTileSizeProperty);
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.EndFadeGroup();
        }
        
        protected void TiledGUI()
        {
            EditorGUILayout.PropertyField(_tiledProperty);
            _showTiledOptions.target = _tiledProperty.boolValue && !_tiledProperty.hasMultipleDifferentValues;
            EditorGUI.indentLevel++;
            if (EditorGUILayout.BeginFadeGroup(_showTiledOptions.faded))
            {
                EditorGUILayout.PropertyField(_tileSizeProperty);
                EditorGUILayout.PropertyField(_tileSpacingProperty);
                if (targets.Length == 1)
                {
                    //GUILayout.Button("Reset");
                }
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.EndFadeGroup();
        }
        
        protected void FilledGUI()
        {
            EditorGUILayout.PropertyField(_filledProperty);
            _showFillOptions.target = _filledProperty.boolValue && !_filledProperty.hasMultipleDifferentValues;
            EditorGUI.indentLevel++;
            if (EditorGUILayout.BeginFadeGroup(_showFillOptions.faded))
            {
                EditorGUILayout.PropertyField(_fillMethodProperty);
                if ((SlicedImage.FillMethod)_fillMethodProperty.enumValueIndex == SlicedImage.FillMethod.Custom)
                {
                    EditorGUILayout.PropertyField(_customFillingProperty);
                }
                else
                {
                    var shapeRect = EditorGUILayout.GetControlRect(true);
                    switch ((SlicedImage.FillMethod)_fillMethodProperty.enumValueIndex)
                    {
                        case SlicedImage.FillMethod.Horizontal:
                            Popup(shapeRect, _fillOriginProperty, Styles.OriginHorizontalStyle, Styles.text);
                            break;
                        case SlicedImage.FillMethod.Vertical:
                            Popup(shapeRect, _fillOriginProperty, Styles.OriginVerticalStyle, Styles.text);
                            break;
                        case SlicedImage.FillMethod.Radial90:
                            Popup(shapeRect, _fillOriginProperty, Styles.Origin90Style, Styles.text);
                            break;
                        case SlicedImage.FillMethod.Radial180:
                            Popup(shapeRect, _fillOriginProperty, Styles.Origin180Style, Styles.text);
                            break;
                        case SlicedImage.FillMethod.Radial360:
                            Popup(shapeRect, _fillOriginProperty, Styles.Origin360Style, Styles.text);
                            break;
                        case SlicedImage.FillMethod.Custom:
                            break;
                    }
                    if ((SlicedImage.FillMethod)_fillMethodProperty.enumValueIndex > SlicedImage.FillMethod.Vertical)
                    {
                        EditorGUILayout.PropertyField(_fillClockwiseProperty, _clockwiseContent);
                    }   
                }
                if (_isDriven)
                    EditorGUILayout.HelpBox("The Fill amount property is driven by Slider.", MessageType.None);
                using (new EditorGUI.DisabledScope(_isDriven))
                {
                    EditorGUILayout.PropertyField(_fillAmountProperty);
                }
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.EndFadeGroup();
        }

        private void DrawDisablableProperty(bool disabled, SerializedProperty property, string disabledDescription)
        {
            EditorGUI.BeginDisabledGroup(disabled);
            if (disabled)
            {
                EditorGUILayout.PropertyField(property, new GUIContent(property.displayName, disabledDescription));   
            }
            else
            {
                EditorGUILayout.PropertyField(property);
            }
            EditorGUI.EndDisabledGroup();
        }

        public override bool HasPreviewGUI() => true;

        public override void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            var image = target as SlicedImage;
            if (image == null) return;
            var sprite = image.sprite;
            if (sprite == null) return;

            SlicedSpriteDrawUtility.DrawSprite(sprite, rect, image.canvasRenderer.GetColor());
        }

        public override string GetInfoString()
        {
            var image = target as SlicedImage;
            if (image == null) return "";
            var sprite = image.sprite;
            if (sprite == null) return "";
            
            var x = (sprite != null) ? Mathf.RoundToInt(sprite.rect.width) : 0;
            var y = (sprite != null) ? Mathf.RoundToInt(sprite.rect.height) : 0;
            return $"Image Size: {x}x{y}";
        }
        
        private static void Popup(
            Rect position,
            SerializedProperty property,
            GUIContent[] displayedOptions,
            GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);
            var intValue = property.intValue % displayedOptions.Length;
            EditorGUI.BeginChangeCheck();
            var num = EditorGUI.Popup(position, label, property.hasMultipleDifferentValues ? -1 : intValue, displayedOptions);
            if (EditorGUI.EndChangeCheck())
                property.intValue = num;
            EditorGUI.EndProperty();
        }
    }
}