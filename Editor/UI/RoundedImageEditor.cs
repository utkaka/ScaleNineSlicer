using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using Utkaka.ScaleNineSlicer.UI;

namespace Utkaka.ScaleNineSlicer.Editor.UI
{
    [CustomEditor(typeof(RoundedImage), true)]
    [CanEditMultipleObjects]
    public class RoundedImageEditor : ExtendedImageEditor
    {
        private SerializedProperty _spriteProperty;
        private SerializedProperty _sliceTypeProperty;
        private SerializedProperty _sliceApproximationStepsProperty;
        private SerializedProperty _bottomLeftCornerProperty;
        private SerializedProperty _topLeftCornerProperty;
        private SerializedProperty _topRightCornerProperty;
        private SerializedProperty _bottomRightCornerProperty;
        
        private AnimBool _showApproximationSteps;
        private AnimBool _showCornerOptions;
        
        public override void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            var asset = (target as RoundedImage)?.roundedSprite;
            if (asset?.Sprite == null) return;

            var texture = asset.Sprite.texture;
            if (texture == null) return;
            SlicedSpriteDrawUtility.DrawCapsuleSprite(asset.Sprite, rect, Color.white,
                asset.OuterRadius, asset.OuterCenter, asset.InnerRadius, asset.InnerCenter);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            
            _spriteProperty = serializedObject.FindProperty("_roundedSprite");
            _sliceTypeProperty = serializedObject.FindProperty("_sliceType");
            _sliceApproximationStepsProperty = serializedObject.FindProperty("_sliceApproximationSteps");
            _bottomLeftCornerProperty = serializedObject.FindProperty("_bottomLeftCorner");
            _topLeftCornerProperty = serializedObject.FindProperty("_topLeftCorner");
            _topRightCornerProperty = serializedObject.FindProperty("_topRightCorner");
            _bottomRightCornerProperty = serializedObject.FindProperty("_bottomRightCorner");
            
            _showCornerOptions = new AnimBool(_sliceTypeProperty.enumValueIndex == (int)RoundedImage.Type.RoundedRect);
            _showCornerOptions.valueChanged.AddListener(Repaint);
            
            _showApproximationSteps = new AnimBool(!_showCornerOptions.target);
            _showApproximationSteps.valueChanged.AddListener(Repaint);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _showCornerOptions.valueChanged.RemoveListener(Repaint);
            _showApproximationSteps.valueChanged.RemoveListener(Repaint);
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
            EditorGUILayout.PropertyField(_spriteProperty);
            if (!EditorGUI.EndChangeCheck()) return;
            (serializedObject.targetObject as ExtendedImage)?.DisableSpriteOptimizations();
        }

        protected void SlicedGUI()
        {
            EditorGUILayout.PropertyField(_sliceTypeProperty);
            _showCornerOptions.target = _sliceTypeProperty.enumValueIndex == (int)RoundedImage.Type.RoundedRect &&
                                        !_sliceTypeProperty.hasMultipleDifferentValues;
            _showApproximationSteps.target = !_showCornerOptions.target;
            if (EditorGUILayout.BeginFadeGroup(_showCornerOptions.faded))
            {
                EditorGUILayout.BeginHorizontal();
                CornerGUI(_topLeftCornerProperty);
                //EditorGUILayout.Space(40);
                CornerGUI(_topRightCornerProperty);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                CornerGUI(_bottomLeftCornerProperty);
                //EditorGUILayout.Space(40);
                CornerGUI(_bottomRightCornerProperty);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndFadeGroup();
            if (EditorGUILayout.BeginFadeGroup(_showApproximationSteps.faded))
            {
                EditorGUILayout.PropertyField(_sliceApproximationStepsProperty);
            }
            EditorGUILayout.EndFadeGroup();
        }

        protected void CornerGUI(SerializedProperty cornerProperty)
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(cornerProperty.displayName);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(cornerProperty.FindPropertyRelative("Radius"));
            EditorGUILayout.PropertyField(cornerProperty.FindPropertyRelative("ApproximationSteps"));
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }
        
        protected void SimpleGUI()
        {
            DrawDisablableProperty(!IsTiled,
                PixelsPerUnitMultiplierProperty, "Does not apply to non tiled images.");
        }
    }
}