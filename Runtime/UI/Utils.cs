using System.Collections.Generic;
using UnityEngine;

namespace Utkaka.ScaleNineSlicer.UI
{
    public static class Utils
    {
        public static bool SetColor(ref Color currentValue, Color newValue)
        {
            if (
                Mathf.Approximately(currentValue.r, newValue.r)
                && Mathf.Approximately(currentValue.g, newValue.g)
                && Mathf.Approximately(currentValue.b, newValue.b)
                && Mathf.Approximately(currentValue.a, newValue.a)
            )
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetStruct<T>(ref T currentValue, T newValue)
            where T : struct
        {
            if (EqualityComparer<T>.Default.Equals(currentValue, newValue))
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetClass<T>(ref T currentValue, T newValue)
            where T : class
        {
            if (
                (currentValue == null && newValue == null)
                || (currentValue != null && currentValue.Equals(newValue))
            )
                return false;

            currentValue = newValue;
            return true;
        }
        
        public static Vector4 GetAdjustedBorders(Vector4 border, Rect originalRect, Rect adjustedRect)
        {
            for (var axis = 0; axis <= 1; axis++)
            {
                float borderScaleRatio;

                if (originalRect.size[axis] != 0)
                {
                    borderScaleRatio = adjustedRect.size[axis] / originalRect.size[axis];
                    border[axis] *= borderScaleRatio;
                    border[axis + 2] *= borderScaleRatio;
                }

                var combinedBorders = border[axis] + border[axis + 2];
                if (!(adjustedRect.size[axis] < combinedBorders) || combinedBorders == 0) continue;
                borderScaleRatio = adjustedRect.size[axis] / combinedBorders;
                border[axis] *= borderScaleRatio;
                border[axis + 2] *= borderScaleRatio;
            }
            return border;
        }
        
        public static void PreserveSpriteAspectRatio(ref Rect rect, Vector2 spriteSize, Vector2 pivot)
        {
            var spriteRatio = spriteSize.x / spriteSize.y;
            var rectRatio = rect.width / rect.height;

            if (spriteRatio > rectRatio)
            {
                var oldHeight = rect.height;
                rect.height = rect.width * (1.0f / spriteRatio);
                rect.y += (oldHeight - rect.height) * pivot.y;
            }
            else
            {
                var oldWidth = rect.width;
                rect.width = rect.height * spriteRatio;
                rect.x += (oldWidth - rect.width) * pivot.x;
            }
        }
    }
}