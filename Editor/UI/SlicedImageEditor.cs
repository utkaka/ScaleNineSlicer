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
    public class ImageEditor : ExtendedImageEditor
    {
        private GUIContent _spriteContent;
        
        private SerializedProperty _spriteProperty;
        private SerializedProperty _preserveAspectProperty;
        private SerializedProperty _useSpriteMeshProperty;
        
        private SerializedProperty _slicedProperty;
        private SerializedProperty _fillCenterProperty;
        private SerializedProperty _tileScaledSlicesProperty;
        private SerializedProperty _slicedTileSizeProperty;
        
        private AnimBool _showSlicedOptions;
        
        public override void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            var image = target as SlicedImage;
            if (image == null) return;
            var sprite = image.sprite;
            if (sprite == null) return;

            SlicedSpriteDrawUtility.DrawSprite(sprite, rect, image.canvasRenderer.GetColor());
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            
            _spriteContent = EditorGUIUtility.TrTextContent("Source Image");
            _spriteProperty = serializedObject.FindProperty("_sprite");
            
            _preserveAspectProperty = serializedObject.FindProperty("_preserveAspect");
            _useSpriteMeshProperty = serializedObject.FindProperty("_useSpriteMesh");
            
            _slicedProperty = serializedObject.FindProperty("_sliced");
            _fillCenterProperty = serializedObject.FindProperty("_fillCenter");
            _tileScaledSlicesProperty = serializedObject.FindProperty("_tileScaledSlices");
            _slicedTileSizeProperty = serializedObject.FindProperty("_slicedTileSize");
            _showSlicedOptions = new AnimBool(_slicedProperty.boolValue);
            _showSlicedOptions.valueChanged.AddListener(Repaint);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _showSlicedOptions.valueChanged.RemoveListener(Repaint);
        }

        protected override void DrawInspectorGUI()
        {
            SpriteGUI();
            AppearanceControlsGUI();
            RaycastControlsGUI();
            MaskableControlsGUI();
            SimpleGUI();
            SlicedGUI();
            TiledGUI();
            FilledGUI();
            
            NativeSizeButtonGUI();
        }
        
        protected void SpriteGUI()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_spriteProperty, _spriteContent);
            if (!EditorGUI.EndChangeCheck()) return;
            (serializedObject.targetObject as ExtendedImage)?.DisableSpriteOptimizations();
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
                DrawDisablableProperty(!_tileScaledSlicesProperty.boolValue, 
                    _slicedTileSizeProperty, "You need to enable tiling of scaled slices.");
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.EndFadeGroup();
        }
        
        protected void SimpleGUI()
        {
            DrawDisablableProperty(IsFilled || IsTiled || _slicedProperty.boolValue,
                _useSpriteMeshProperty, "Does not apply to sliced, tiled, or filled images.");
            DrawDisablableProperty(IsTiled || _slicedProperty.boolValue,
                _preserveAspectProperty, "Does not apply to sliced or tiled images.");
            DrawDisablableProperty(!IsTiled && !_slicedProperty.boolValue,
                PixelsPerUnitMultiplierProperty, "Does not apply to non sliced or non tiled images.");
        }
    }
}