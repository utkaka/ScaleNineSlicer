using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Utkaka.ScaleNineSlicer.SpriteSlicing;

namespace Utkaka.ScaleNineSlicer.Editor.SpriteSlicing
{
    public static class SpriteNineSlicerContextMenu
    {
        [MenuItem("Assets/Sprite Nine Slicer/Autodetect Borders", true)]
        public static bool CheckSelectionAutodetectBorders() => CheckSelection();

        [MenuItem("Assets/Sprite Nine Slicer/Trim Alpha", true)]
        public static bool CheckSelectionTrimAlpha() => CheckSelection();

        [MenuItem("Assets/Sprite Nine Slicer/Trim Slice Center", true)]
        public static bool CheckSelectionTrimSliceCenter() => CheckSelection();

        [MenuItem("Assets/Sprite Nine Slicer/Autodetect Borders And Trim All", true)]
        public static bool CheckSelectionAutodetectBordersAndTrimAll() => CheckSelection();

        [MenuItem("Assets/Sprite Nine Slicer/Autodetect Borders")]
        public static void AutodetectBorders()
        {
            ProcessSprites(true, false, false);
        }

        [MenuItem("Assets/Sprite Nine Slicer/Trim Alpha")]
        public static void TrimAlpha()
        {
            ProcessSprites(false, true, false);
        }

        [MenuItem("Assets/Sprite Nine Slicer/Trim Slice Center")]
        public static void TrimSliceCenter()
        {
            ProcessSprites(false, false, true);
        }

        [MenuItem("Assets/Sprite Nine Slicer/Autodetect Borders And Trim All")]
        public static void AutodetectBordersAndTrimAll()
        {
            ProcessSprites(true, true, true);
        }

        private static void ProcessSprites(bool autodetectBorders, bool trimAlpha, bool trimCenter)
        {
            var sprites = GetSelectedSprites();
            foreach (var sprite in sprites)
            {
                var imagePath = AssetDatabase.GetAssetPath(sprite);
                var importer = (TextureImporter)AssetImporter.GetAtPath(imagePath);
                var singleSprite = importer.spriteImportMode == SpriteImportMode.Single;

                var spriteInfo = new SpriteInfo(
                    imagePath,
                    sprite.texture.width,
                    sprite.texture.height,
                    sprite.border.ToInt4(),
                    sprite.texture.filterMode
                );

                if (autodetectBorders)
                {
                    spriteInfo.AutoDetectBorder(0.0f);
                }

                if (trimAlpha && singleSprite)
                {
                    spriteInfo.TrimAlpha();
                }
                if (trimCenter && singleSprite)
                {
                    spriteInfo.TrimCenter();
                }

                if (singleSprite && (trimAlpha || trimCenter))
                {
                    spriteInfo.WriteTextureToFile(AssetDatabase.GetAssetPath(sprite.texture));
                }

                importer.spriteBorder = spriteInfo.Border.ToVector4();
                importer.SaveAndReimport();
            }
        }

        public static List<Sprite> GetSelectedSprites()
        {
            var result = new List<Sprite>();

            var selectedObjects = Selection.objects;
            foreach (var selectedObject in selectedObjects)
            {
                var assetPath = AssetDatabase.GetAssetPath(selectedObject);
                if (AssetDatabase.IsValidFolder(assetPath))
                {
                    var spritesInsideFolder = AssetDatabase.FindAssets(
                        "t:Sprite",
                        new[] { assetPath }
                    );
                    foreach (var sprite in spritesInsideFolder)
                    {
                        result.AddRange(
                            AssetDatabase
                                .LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(sprite))
                                .OfType<Sprite>()
                        );
                    }
                }
                else
                {
                    result.AddRange(AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>());
                }
            }

            return result;
        }

        private static bool CheckSelection() =>
            Selection.objects != null && Selection.objects.Length > 0;
    }
}

