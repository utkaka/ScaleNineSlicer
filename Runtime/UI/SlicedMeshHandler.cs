using System;
using UnityEngine;
using UnityEngine.UI;

namespace Utkaka.ScaleNineSlicer.UI
{
    public static class SlicedMeshHandler
    {
        public static void PrepareMesh(Span<CutInputVertex> inputVertices, Span<CutLine> cutLines, 
            VertexHelper vertexHelper, Color32 color, int width, int height, bool skipCenter, ref int meshVertexCount)
        {
            if (width == 0 || height == 0) return;

            if (cutLines.Length == 0)
            {
                for (var i = 0; i < width - 1; i++)
                {
                    for (var j = 0; j < height - 1; j++)
                    {
                        if (skipCenter && i > 0 && i < width - 2 && j > 0 && j < height - 2) continue;
                        ProcessMeshQuad(inputVertices, vertexHelper, color, height, i, j, ref meshVertexCount);
                    }
                }
                return;
            }
            
            var edgeIntersectionsCount = ((width - 1) * height + width * (height - 1)) * cutLines.Length;
            var edgeIntersectionsHeapArray = Utils.GetFromPoolIfNeeded<CutVertex>(edgeIntersectionsCount);
            if (edgeIntersectionsHeapArray != null)
            {
                Array.Clear(edgeIntersectionsHeapArray, 0, edgeIntersectionsCount);
            }
            var edgeIntersections = edgeIntersectionsHeapArray == null ? stackalloc CutVertex[edgeIntersectionsCount] : edgeIntersectionsHeapArray.AsSpan();
            for (var i = 0; i < width - 1; i++)
            {
                for (var j = 0; j < height - 1; j++)
                {
                    if (skipCenter && i > 0 && i < width - 2 && j > 0 && j < height - 2) continue;
                    ProcessMeshQuad(inputVertices, edgeIntersections, cutLines, vertexHelper, color, width, height, i, j, ref meshVertexCount);
                }
            }
            Utils.ReturnToPool(edgeIntersections.Length, edgeIntersectionsHeapArray);
        }

        private static void ProcessMeshQuad(Span<CutInputVertex> inputVertices,
            VertexHelper vertexHelper, Color32 color, int height, int x, int y, ref int meshVertexCount)
        {
            var bottomLeftIndex = height * x + y;

            var vertex1 = AddInputVertex(inputVertices, vertexHelper, bottomLeftIndex, color, ref meshVertexCount);
            var vertex2 = AddInputVertex(inputVertices, vertexHelper, bottomLeftIndex + 1, color, ref meshVertexCount);
            var vertex3 = AddInputVertex(inputVertices, vertexHelper, bottomLeftIndex + height + 1, color, ref meshVertexCount);
            var vertex4 = AddInputVertex(inputVertices, vertexHelper, bottomLeftIndex + height, color, ref meshVertexCount);

            if (!(Math.Abs(vertex1.Position.x - vertex2.Position.x) <= Mathf.Epsilon
                  && Math.Abs(vertex1.Position.x - vertex3.Position.x) <= Mathf.Epsilon) &&
                !(Math.Abs(vertex1.Position.y - vertex2.Position.y) <= Mathf.Epsilon
                  && Math.Abs(vertex1.Position.y - vertex3.Position.y) <= Mathf.Epsilon))
            {
                vertexHelper.AddTriangle(vertex1.VertexIndex - 1, vertex2.VertexIndex - 1, vertex3.VertexIndex - 1);
            }
            
            if (!(Math.Abs(vertex1.Position.x - vertex4.Position.x) <= Mathf.Epsilon
                  && Math.Abs(vertex1.Position.x - vertex3.Position.x) <= Mathf.Epsilon) &&
                !(Math.Abs(vertex1.Position.y - vertex4.Position.y) <= Mathf.Epsilon
                  && Math.Abs(vertex1.Position.y - vertex3.Position.y) <= Mathf.Epsilon))
            {
                vertexHelper.AddTriangle(vertex1.VertexIndex - 1, vertex3.VertexIndex - 1, vertex4.VertexIndex - 1);
            }

        }

        private static CutInputVertex AddInputVertex(Span<CutInputVertex> inputVertices,
            VertexHelper vertexHelper, int index, Color32 color, ref int meshVertexCount)
        {
            var vertex = inputVertices[index];
            if (vertex.VertexIndex > 0) return vertex;
            vertexHelper.AddVert(new Vector3(vertex.Position.x, vertex.Position.y), color, new Vector4(vertex.UV.x, vertex.UV.y));
            meshVertexCount++;
            vertex.VertexIndex = meshVertexCount;
            inputVertices[index] = vertex;
            return vertex;
        }

        private static void ProcessMeshQuad(Span<CutInputVertex> inputVertices, Span<CutVertex> edgeIntersections, Span<CutLine> cutLines, 
            VertexHelper vertexHelper, Color32 color, int width, int height, int x, int y, ref int meshVertexCount)
        {
            var linesCount = cutLines.Length;
            var edgesCount = (width - 1) * height + width * (height - 1);

            var bufferSize = 4 + linesCount;

            var vertexBuffer1HeapArray = Utils.GetFromPoolIfNeeded<CutVertex>(bufferSize);
            var vertexBuffer1 = vertexBuffer1HeapArray == null ? stackalloc CutVertex[bufferSize] : vertexBuffer1HeapArray.AsSpan();

            var vertexBuffer2HeapArray = Utils.GetFromPoolIfNeeded<CutVertex>(bufferSize);
            var vertexBuffer2 = vertexBuffer2HeapArray == null ? stackalloc CutVertex[bufferSize] : vertexBuffer2HeapArray.AsSpan();

            var bottomLeftIndex = height * x + y;
            var doubleHeight = 2 * height - 1;
            var doubleHeightX = doubleHeight * x;

            var edge1 = doubleHeightX + y;
            var edge2 = doubleHeightX + y + height;
            var edge3 = doubleHeightX + doubleHeight + y;
            var edge4 = doubleHeightX + y + height - 1;
            
            vertexBuffer1[0] = CreateCutVertex(inputVertices, bottomLeftIndex, edge4, edge1);
            vertexBuffer1[1] = CreateCutVertex(inputVertices, bottomLeftIndex + 1, edge2, edge1);
            vertexBuffer1[2] = CreateCutVertex(inputVertices, bottomLeftIndex + height + 1, edge2, edge3);
            vertexBuffer1[3] = CreateCutVertex(inputVertices, bottomLeftIndex + height, edge4, edge3);

            var vertexCount = 4;
            

            for (var i = 0; i < linesCount; i++)
            {
                var line = cutLines[i];
                vertexCount = CutPolygon(vertexBuffer1, vertexCount, vertexBuffer2, edgeIntersections, edgesCount, line, i);
                //swap buffers
                var temp = vertexBuffer1;
                vertexBuffer1 = vertexBuffer2;
                vertexBuffer2 = temp;
            }

            if (vertexCount < 3) return;

            for (var i = 0; i < vertexCount; i++)
            {
                var vertex = vertexBuffer1[i];
                if (vertex.VertIndex > 0) continue;
                meshVertexCount++;
                vertex.VertIndex = meshVertexCount;
                vertexHelper.AddVert(new Vector3(vertex.Position.x, vertex.Position.y), color, new Vector4(vertex.UV.x, vertex.UV.y));
                vertexBuffer1[i] = vertex;
                if (vertex.IntersectionIndex < 0)
                {
                    var vertexIndex = ~vertex.IntersectionIndex;
                    var tempVertex = inputVertices[vertexIndex];
                    tempVertex.VertexIndex = vertex.VertIndex;
                    inputVertices[vertexIndex] = tempVertex;
                } else if (vertex.IntersectionIndex > 0)
                {
                    var vertexIndex = vertex.IntersectionIndex - 1;
                    var tempVertex = edgeIntersections[vertexIndex];
                    tempVertex.VertIndex = vertex.VertIndex;
                    edgeIntersections[vertexIndex] = tempVertex;
                }
            }
            
            var point0 = vertexBuffer1[0];
            var point0Index = point0.VertIndex - 1;
            
            for (var i = 1; i + 1 < vertexCount; i++)
            {
                var point1 = vertexBuffer1[i];
                var point2 = vertexBuffer1[i + 1];
                if (Math.Abs(point0.Position.x - point1.Position.x) <= Mathf.Epsilon 
                    && Math.Abs(point0.Position.x - point2.Position.x) <= Mathf.Epsilon) continue;
                if (Math.Abs(point0.Position.y - point1.Position.y) <= Mathf.Epsilon
                    && Math.Abs(point0.Position.y - point2.Position.y) <= Mathf.Epsilon) continue;
                vertexHelper.AddTriangle(point0Index, point1.VertIndex - 1, point2.VertIndex - 1);
            }
            
            Utils.ReturnToPool(vertexBuffer1.Length, vertexBuffer1HeapArray);
            Utils.ReturnToPool(vertexBuffer2.Length, vertexBuffer2HeapArray);
        }

        private static CutVertex CreateCutVertex(Span<CutInputVertex> inputVertices, int vertexIndex, int edgeX, int edgeY)
        {
            var inputVertex = inputVertices[vertexIndex];
            return new CutVertex
            {
                Position = inputVertex.Position,
                UV = inputVertex.UV,
                IntersectionIndex = ~vertexIndex,
                EdgeX = edgeX,
                EdgeY = edgeY,
                VertIndex = inputVertex.VertexIndex,
            };
        }
        
        private static int CutPolygon(Span<CutVertex> inputBuffer, int inputVertexCount, Span<CutVertex> outputBuffer,
            Span<CutVertex> edgeIntersections, int edgesCount, CutLine cutLine, int lineIndex)
        {
            if (inputVertexCount < 3) return 0;
            var vertexCount = 0;
            var point1 = inputBuffer[inputVertexCount - 1];

            var dot1 = Vector2.Dot(cutLine.Normal, point1.Position - cutLine.Start);
            var point1IsIn = dot1 >= 0f;

            for (var i = 0; i < inputVertexCount; i++)
            {
                var point2 = inputBuffer[i];
                var dot2 = Vector2.Dot(cutLine.Normal, point2.Position - cutLine.Start);
                var point2IsIn = dot2 >= 0f;

                if (point1IsIn && point2IsIn)
                {
                    outputBuffer[vertexCount++] = point2;
                }
                else if (point1IsIn)
                {
                    var intersection = GetIntersection(
                        point1, point2, dot1, dot2, edgeIntersections, edgesCount, lineIndex
                    );
                    if (!intersection.Position.Equals(point1.Position))
                    {
                        outputBuffer[vertexCount++] = intersection;   
                    }
                }
                else if (point2IsIn)
                {
                    var intersection = GetIntersection(
                        point1, point2, dot1, dot2, edgeIntersections, edgesCount, lineIndex
                    );
                    outputBuffer[vertexCount++] = intersection;
                    if (!intersection.Position.Equals(point2.Position))
                    {
                        outputBuffer[vertexCount++] = point2;   
                    }
                }

                point1 = point2;
                dot1 = dot2;
                point1IsIn = point2IsIn;
            }

            return vertexCount;
        }
        
        private static CutVertex GetIntersection(in CutVertex point1, in CutVertex point2, float dot1, float dot2,
            Span<CutVertex> edgeIntersections, int edgesCount, int lineIndex)
        {
            var isEdgeX = point1.EdgeX == point2.EdgeX && point1.EdgeX != -1;
            if (isEdgeX || point1.EdgeY == point2.EdgeY && point1.EdgeY != -1)
            {
                var edgeIndex = isEdgeX ? point1.EdgeX : point1.EdgeY;
                var intersectionIndex = edgesCount * lineIndex + edgeIndex;
                if (edgeIntersections[intersectionIndex].IntersectionIndex > 0)
                {
                    return edgeIntersections[intersectionIndex];
                }

                var newPoint = GetNewIntersectionPoint(point1, point2, dot1, dot2);
                if (isEdgeX)
                {
                    newPoint.EdgeX = point1.EdgeX;
                    newPoint.EdgeY = -1;
                }
                else
                {
                    newPoint.EdgeX = -1;
                    newPoint.EdgeY = point1.EdgeY;
                }
                newPoint.IntersectionIndex = intersectionIndex + 1;
                return newPoint;
            }
            return GetNewIntersectionPoint(point1, point2, dot1, dot2);
        }

        private static CutVertex GetNewIntersectionPoint(in CutVertex point1, in CutVertex point2, float dot1, float dot2)
        {
            var denom = dot1 - dot2;
            var t = 0.0f;
            if (Mathf.Abs(denom) > 0.0f)
            {
                t = Mathf.Clamp01(dot1 / denom);
            }
            var pos = Vector2.LerpUnclamped(point1.Position, point2.Position, t);
            var uv = Vector2.LerpUnclamped(point1.UV, point2.UV, t);

            return new CutVertex
            {
                Position = pos,
                UV = uv,
                EdgeX = -1,
                EdgeY = -1
            };
        }
    }
}