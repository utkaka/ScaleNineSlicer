using UnityEngine.UI;

namespace Utkaka.ScaleNineSlicer.UI
{
    public interface IMeshStatistics
    {
        int VertexCount { get; }
        int TrianglesCount { get; }
    }
    
    public class ImageWithMeshStatistics: Image, IMeshStatistics
    {
        public int VertexCount { get; private set; }
        public int TrianglesCount { get; private set; }
        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            base.OnPopulateMesh(toFill);
            VertexCount = toFill.currentVertCount;
            TrianglesCount = toFill.currentIndexCount;
        }
    }
    
    public class SlicedImageWithMeshStatistics: SlicedImage, IMeshStatistics
    {
        public int VertexCount { get; private set; }
        public int TrianglesCount { get; private set; }
        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            base.OnPopulateMesh(toFill);
            VertexCount = toFill.currentVertCount;
            TrianglesCount = toFill.currentIndexCount;
        }
    }
}