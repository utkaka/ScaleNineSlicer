using System;
using UnityEngine;
using UnityEngine.UI;

namespace Utkaka.ScaleNineSlicer.UI
{
    public static class SlicedMeshHandler
    {
        public static void PrepareMesh(Span<CutInputVertex> inputVertices, ReadOnlySpan<CutLine> cutLines, 
            VertexHelper vertexHelper, Color32 color, int width, int height, bool skipCenter)
        {
            //Debug.Log($"PrepareMesh {width}x{height}");
            if (width == 0 || height == 0) return;
            Span<CutVertex> edgeIntersections = stackalloc CutVertex[((width - 1) * height + width * (height - 1)) * cutLines.Length];
            for (var i = 0; i < width - 1; i++)
            {
                for (var j = 0; j < height - 1; j++)
                {
                    //zDebug.Log("Process Quad");
                    if (skipCenter && i > 0 && i < width - 2 && j > 0 && j < height - 2) continue;
                    ProcessMeshQuad(inputVertices, edgeIntersections, cutLines, vertexHelper, color, width, height, i, j);
                }
            }
        }

        private static void ProcessMeshQuad(Span<CutInputVertex> inputVertices, Span<CutVertex> edgeIntersections, ReadOnlySpan<CutLine> cutLines, 
            VertexHelper vertexHelper, Color32 color, int width, int height, int x, int y)
        {
            var linesCount = cutLines.Length;
            var edgesCount = (width - 1) * height + width * (height - 1);
            
            Span<CutVertex> vertexBuffer1 = stackalloc CutVertex[4 + linesCount];
            Span<CutVertex> vertexBuffer2 = stackalloc CutVertex[4 + linesCount];
            var bottomLeftIndex = height * x + y;

            var edge1 = (2 * height - 1) * x + y;
            var edge2 = (2 * height - 1) * x + y + height;
            var edge3 = (2 * height - 1) * (x + 1) + y;
            var edge4 = (2 * height - 1) * x + y + height - 1;
            
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
                vertex.VertIndex = vertexHelper.currentVertCount + 1;
                vertexHelper.AddVert(vertex.Position, color, vertex.UV);
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
            
            var point0 = vertexBuffer1[0].VertIndex - 1;
            for (var i = 1; i + 1 < vertexCount; i++)
            {
                if (Mathf.Approximately(vertexBuffer1[0].Position.x, vertexBuffer1[i].Position.x) 
                    && Mathf.Approximately(vertexBuffer1[0].Position.x, vertexBuffer1[i + 1].Position.x)) continue;
                if (Mathf.Approximately(vertexBuffer1[0].Position.y, vertexBuffer1[i].Position.y) 
                    && Mathf.Approximately(vertexBuffer1[0].Position.y, vertexBuffer1[i + 1].Position.y)) continue;
                vertexHelper.AddTriangle(point0, vertexBuffer1[i].VertIndex - 1, vertexBuffer1[i + 1].VertIndex - 1);
            }
        }

        private static CutVertex CreateCutVertex(ReadOnlySpan<CutInputVertex> inputVertices, int vertexIndex, int edgeX, int edgeY)
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

            var point1IsIn = Vector2.Dot(cutLine.Normal, point1.Position - cutLine.Start) >= 0f;

            for (var i = 0; i < inputVertexCount; i++)
            {
                var point2 = inputBuffer[i];
                var point2IsIn = Vector2.Dot(cutLine.Normal, point2.Position - cutLine.Start) >= 0f;

                if (point1IsIn && point2IsIn)
                {
                    outputBuffer[vertexCount++] = point2;
                }
                else if (point1IsIn)
                {
                    var intersection = GetIntersection(
                        point1, point2, edgeIntersections, edgesCount, cutLine, lineIndex
                    );
                    if (!Vector2.Equals(intersection.Position, point1.Position))
                    {
                        outputBuffer[vertexCount++] = intersection;   
                    }
                }
                else if (point2IsIn)
                {
                    var intersection = GetIntersection(
                        point1, point2, edgeIntersections, edgesCount, cutLine, lineIndex
                    );
                    outputBuffer[vertexCount++] = intersection;
                    if (!Vector2.Equals(intersection.Position, point2.Position))
                    {
                        outputBuffer[vertexCount++] = point2;   
                    }
                }

                point1 = point2;
                point1IsIn = point2IsIn;
            }

            return vertexCount;
        }
        
        private static CutVertex GetIntersection(in CutVertex point1, in CutVertex point2,
            Span<CutVertex> edgeIntersections, int edgesCount, CutLine cutLine, int lineIndex)
        {
            var isEdgeX = point1.EdgeX == point2.EdgeX;
            if (isEdgeX || point1.EdgeY == point2.EdgeY)
            {
                var edgeIndex = isEdgeX ? point1.EdgeX : point1.EdgeY;
                var intersectionIndex = edgesCount * lineIndex + edgeIndex;
                if (edgeIntersections[intersectionIndex].VertIndex > 0)
                {
                    return edgeIntersections[intersectionIndex];
                }

                var newPoint = GetNewIntersectionPoint(point1, point2, cutLine);
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
                edgeIntersections[intersectionIndex] = newPoint;
                return newPoint;
            }
            return GetNewIntersectionPoint(point1, point2, cutLine);
        }

        private static CutVertex GetNewIntersectionPoint(in CutVertex point1, in CutVertex point2, CutLine cutLine)
        {
            var dot1 = Vector3.Dot(cutLine.Normal, point1.Position - cutLine.Start);
            var dot2 = Vector3.Dot(cutLine.Normal, point2.Position - cutLine.Start);
            var denom = dot1 - dot2;
            var t = Mathf.Clamp01(Mathf.Approximately(denom, 0f) ? 0f : dot1 / denom);
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