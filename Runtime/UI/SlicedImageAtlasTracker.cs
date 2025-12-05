using System.Collections.Generic;
using UnityEngine.U2D;

namespace Utkaka.ScaleNineSlicer.UI
{
    public static class SlicedImageAtlasTracker
    {
        // To track textureless images, which will be rebuild if sprite atlas manager registered a Sprite Atlas that will give this image new texture
        private static readonly List<SlicedImage> TrackedTexturelessImages = new();
        private static bool _initialized;

        private static void RebuildImage(SpriteAtlas spriteAtlas)
        {
            for (var i = TrackedTexturelessImages.Count - 1; i >= 0; i--)
            {
                var slicedImage = TrackedTexturelessImages[i];
                if (null == slicedImage.activeSprite || !spriteAtlas.CanBindTo(slicedImage.activeSprite)) continue;
                slicedImage.SetAllDirty();
                TrackedTexturelessImages.RemoveAt(i);
            }
        }

        public static void TrackImage(SlicedImage g)
        {
            if (!_initialized)
            {
                SpriteAtlasManager.atlasRegistered += RebuildImage;
                _initialized = true;
            }

            TrackedTexturelessImages.Add(g);
        }

        public static void UnTrackImage(SlicedImage g)
        {
            TrackedTexturelessImages.Remove(g);
        }
    }
}