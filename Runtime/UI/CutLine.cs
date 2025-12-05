using UnityEngine;

namespace Utkaka.ScaleNineSlicer.UI
{
    public struct CutLine
    {
        public readonly Vector2 Start;
        public readonly Vector2 Normal;

        public static CutLine FromLine(Vector2 start, Vector2 end)
        {
            var lineVector = end - start;
            return new CutLine(start, new Vector2(lineVector.y, -lineVector.x));
        }
        
        public CutLine(Vector2 start, Vector2 normal)
        {
            Start = start;
            Normal = normal;
        }
    }
}