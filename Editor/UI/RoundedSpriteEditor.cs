using UnityEditor;
using UnityEngine;
using Utkaka.ScaleNineSlicer.UI;

namespace Utkaka.ScaleNineSlicer.Editor.UI
{
    [CustomEditor(typeof(RoundedSprite))]
    public class RoundedSpriteEditor : UnityEditor.Editor
    {
        public override bool HasPreviewGUI()
        {
            return (target as RoundedSprite)?.Sprite != null;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            var asset = target as RoundedSprite;
            if (asset?.Sprite == null) return;

            var texture = asset.Sprite.texture;
            if (texture == null) return;
            SlicedSpriteDrawUtility.DrawCapsuleSprite(asset.Sprite, r, Color.white,
                asset.OuterRadius, asset.OuterCenter, asset.InnerRadius, asset.InnerCenter);
        }
    }
}