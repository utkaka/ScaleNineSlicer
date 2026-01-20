using System;
using UnityEditor;
using UnityEngine;
using Utkaka.ScaleNineSlicer.UI;
using Object = UnityEngine.Object;

namespace Utkaka.ScaleNineSlicer.Editor.UI
{
    [CustomEditor(typeof(CapsuleSprite))]
    public class CapsuleSpriteEditor : UnityEditor.Editor
    {
        public override bool HasPreviewGUI()
        {
            return (target as CapsuleSprite)?.Sprite != null;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            var asset = target as CapsuleSprite;
            if (asset?.Sprite == null) return;

            var texture = asset.Sprite.texture;
            if (texture == null) return;
            SlicedSpriteDrawUtility.DrawCapsuleSprite(asset.Sprite, r, Color.white,
                asset.OuterRadius, asset.OuterCenter, asset.InnerRadius, asset.InnerCenter);
        }
    }
}