using System;
using UnityEngine;
using UnityEngine.UI;

namespace Utkaka.ScaleNineSlicer.UI
{
    public static class CapsuleMeshHandler
    {
        public static void PrepareMesh(Span<CapsuleVertex> inputVertices, Span<CutLine> cutLines, 
            VertexHelper vertexHelper)
        {
            ProcessVertexChain(inputVertices, cutLines, vertexHelper);
        }

        private static void ProcessVertexChain(Span<CapsuleVertex> inputVertices, Span<CutLine> cutLines, VertexHelper vertexHelper)
        {
            var vertexCount = inputVertices.Length;
            var linesCount = cutLines.Length;
            var bufferSize = vertexCount * (linesCount + 1);
            var chainCount = (bufferSize + 2) / 3;

            var vertexBuffer1HeapArray = Utils.GetFromPoolIfNeeded<CapsuleVertex>(bufferSize);
            var vertexBuffer1 = vertexBuffer1HeapArray == null ? stackalloc CapsuleVertex[bufferSize] : vertexBuffer1HeapArray.AsSpan();
            
            var chainBuffer1HeapArray = Utils.GetFromPoolIfNeeded<ValueTuple<int, int>>(chainCount);
            var chainBuffer1 = chainBuffer1HeapArray == null ? stackalloc ValueTuple<int, int>[chainCount] : chainBuffer1HeapArray.AsSpan();

            var vertexBuffer2HeapArray = Utils.GetFromPoolIfNeeded<CapsuleVertex>(bufferSize);
            var vertexBuffer2 = vertexBuffer2HeapArray == null ? stackalloc CapsuleVertex[bufferSize] : vertexBuffer2HeapArray.AsSpan();
            
            var chainBuffer2HeapArray = Utils.GetFromPoolIfNeeded<ValueTuple<int, int>>(chainCount);
            var chainBuffer2 = chainBuffer2HeapArray == null ? stackalloc ValueTuple<int, int>[chainCount] : chainBuffer2HeapArray.AsSpan();

            inputVertices.CopyTo(vertexBuffer1);
            chainBuffer1[0] = new ValueTuple<int, int>(0, inputVertices.Length);

            for (var i = 0; i < linesCount; i++)
            {
                var line = cutLines[i];
                CutPolygons(vertexBuffer1, chainBuffer1, vertexBuffer2, chainBuffer2, line);
                //swap buffers
                var temp = vertexBuffer1;
                vertexBuffer1 = vertexBuffer2;
                vertexBuffer2 = temp;
                
                var temp2 = chainBuffer1;
                chainBuffer1 = chainBuffer2;
                chainBuffer2 = temp2;
            }

            //if (vertexCount < 3) return;

            var vertexOffset = 0;

            for (var i = 0; i < chainBuffer1.Length; i++)
            {
                var chain =  chainBuffer1[i];
                if (chain.Item2 == 0) continue;
                //EarClipping(vertexBuffer1.Slice(chain.Item1, chain.Item2), vertexHelper, vertexOffset);
                QuadTriangulation(vertexBuffer1.Slice(chain.Item1, chain.Item2), vertexHelper, vertexOffset);
                for (var j = chain.Item1; j < chain.Item1 + chain.Item2; j++)
                {
                    var vertex = vertexBuffer1[j]; ;
                    vertexHelper.AddVert(new Vector3(vertex.Position.x, vertex.Position.y), GetColorByIndexGolden(i),
                        new Vector4(vertex.UV.x, vertex.UV.y));
                    vertexBuffer1[i] = vertex;    
                }

                vertexOffset = vertexHelper.currentVertCount;
            }
            
            Utils.ReturnToPool(vertexBuffer1.Length, vertexBuffer1HeapArray);
            Utils.ReturnToPool(vertexBuffer2.Length, vertexBuffer2HeapArray);
            
            Utils.ReturnToPool(chainBuffer1.Length, chainBuffer1HeapArray);
            Utils.ReturnToPool(chainBuffer2.Length, chainBuffer2HeapArray);
        }

        private static void QuadTriangulation(Span<CapsuleVertex> inputBuffer, VertexHelper vertexHelper, int vertexOffset)
        {
            for (var i = 0; i < inputBuffer.Length - 1; i++)
            {
                vertexHelper.AddTriangle(vertexOffset + i, vertexOffset + i + 1, vertexOffset + inputBuffer.Length - i - 1);
                Debug.Log($"Triangle: {i}, {i + 1}, {inputBuffer.Length - i - 1}");
            }
        }
        
        //0 1 -1
        //1 -2 -1

        private static void EarClipping(Span<CapsuleVertex> inputBuffer, VertexHelper vertexHelper, int vertexOffset)
        {
            var n = inputBuffer.Length;
            if (n < 3) return;
            
            var prevHeapArray = Utils.GetFromPoolIfNeeded<int>(n);
            var prevBuffer = prevHeapArray == null ? stackalloc int[n] : prevHeapArray.AsSpan();
            
            var nextHeapArray = Utils.GetFromPoolIfNeeded<int>(n);
            var nextBuffer = prevHeapArray == null ? stackalloc int[n] : nextHeapArray.AsSpan();
            
            var aliveHeapArray = Utils.GetFromPoolIfNeeded<bool>(n);
            var aliveBuffer = prevHeapArray == null ? stackalloc bool[n] : aliveHeapArray.AsSpan();
            
            var earHeapArray = Utils.GetFromPoolIfNeeded<bool>(n);
            var earBuffer = prevHeapArray == null ? stackalloc bool[n] : earHeapArray.AsSpan();

            var signedArea2 = 0.0f;
            
            for (var i = 0; i < n; i++)
            {
                var j = (i + 1) % n;
                prevBuffer[j] = i;
                nextBuffer[i] = j;
                aliveBuffer[i] = true;
                signedArea2 += inputBuffer[i].Position.x * inputBuffer[j].Position.y -
                               inputBuffer[j].Position.x * inputBuffer[i].Position.y;
            }
            
            var clockwise = signedArea2 < 0.0f;
            
            for (var i = 0; i < n; i++)
            {
                earBuffer[i] = IsEar(i, inputBuffer, prevBuffer, nextBuffer, aliveBuffer, clockwise);
            }

            var m = n;
            while (m > 3)
            {
                var alive = GetNextAlive(0, inputBuffer, aliveBuffer, earBuffer);

                if (alive < 0) throw new Exception("Alive not found");
                
                var prev = prevBuffer[alive];
                var next = nextBuffer[alive];
                
                vertexHelper.AddTriangle(vertexOffset + prev, vertexOffset + alive, vertexOffset + next);
                aliveBuffer[alive] = false;
                nextBuffer[prev] = next;
                prevBuffer[next] = prev;
                m--;
                
                earBuffer[prev] = IsEar(prev, inputBuffer, prevBuffer, nextBuffer, aliveBuffer, clockwise);
                earBuffer[next] = IsEar(next, inputBuffer, prevBuffer, nextBuffer, aliveBuffer, clockwise);
            }
            
            var a = GetNextAlive(0, inputBuffer, aliveBuffer, earBuffer);
            var b = GetNextAlive(a + 1, inputBuffer, aliveBuffer, earBuffer);
            var c = GetNextAlive(b + 1, inputBuffer, aliveBuffer, earBuffer);
            if (a > -1 && b > -1 && c > -1)
            {
                vertexHelper.AddTriangle(vertexOffset + a, vertexOffset + b, vertexOffset + c);   
            }
            
            Utils.ReturnToPool(prevBuffer.Length, prevHeapArray);
            Utils.ReturnToPool(nextBuffer.Length, nextHeapArray);
            Utils.ReturnToPool(aliveBuffer.Length, aliveHeapArray);
            Utils.ReturnToPool(earBuffer.Length, earHeapArray);
        }

        private static int GetNextAlive(int startIndex, Span<CapsuleVertex> inputBuffer, Span<bool> aliveBuffer, Span<bool> earBuffer)
        {
            for (var i = startIndex; i < inputBuffer.Length; i++)
            {
                if (!aliveBuffer[i] || !earBuffer[i]) continue;
                return i;
            }

            return -1;
        }

        private static bool IsConvex(Vector2 a, Vector2 b, Vector2 c, bool clockwise)
        {
            var sign = Cross(b - a, c - a);
            return clockwise ? sign < -Mathf.Epsilon : sign > Mathf.Epsilon;
        }


        private static bool IsPointInTriangle(Vector2 a, Vector2 b, Vector2 c, Vector2 x, bool clockwise)
        {
            var s1 = Cross(b - a, x - a);
            var s2 = Cross(c - b, x - b);
            var s3 = Cross(a - c, x - c);
            if (clockwise) return s1 <= Mathf.Epsilon && s2 <= Mathf.Epsilon && s3 <= Mathf.Epsilon;
            return s1 >= -Mathf.Epsilon && s2 >= -Mathf.Epsilon && s3 >= -Mathf.Epsilon;
        }

        private static bool IsEar(int index, Span<CapsuleVertex> inputBuffer, Span<int> prevBuffer, Span<int> nextBuffer, Span<bool> aliveBuffer, bool clockwise)
        {
            if (!aliveBuffer[index]) return false;
            var prev = prevBuffer[index];
            var next = nextBuffer[index];
            var a = inputBuffer[prev].Position;
            var b = inputBuffer[index].Position;
            var c = inputBuffer[next].Position;
            if (!IsConvex(a, b, c, clockwise)) return false;

            for (var i = 0; i < inputBuffer.Length; i++)
            {
                if (i == index || i == prev || i == next) continue;
                if (IsPointInTriangle(a, b, c, inputBuffer[i].Position, clockwise))
                    return false;
            }

            return true;
        }
            
        private static float Cross(this Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        private static Color GetColorByIndexGolden(int i)
        {
            return Color.white;
            const float golden = 0.61803398875f;
            var h = Mathf.Repeat(i * golden, 1f);
            var s = 0.9f;
            var v = 0.95f;
            return Color.HSVToRGB(h, s, v);
        }
        
        private static void CutPolygons(Span<CapsuleVertex> inputBuffer, Span<ValueTuple<int, int>> inputChains,
            Span<CapsuleVertex> outputBuffer, Span<ValueTuple<int, int>> outputChains, CutLine cutLine)
        {
            var vertexCount = 0;
            var chainCount = 0;
            for (var i = 0; i < outputChains.Length; i++)
            {
                outputChains[i] = new ValueTuple<int, int>(0, 0);
            }
            
            if (inputChains[0].Item2 == 0) return;

            for (var i = 0; i < inputChains.Length; i++)
            {
                var chainStartLength = inputChains[i];
                if (chainStartLength.Item2 == 0) break;
                CutPolygon(inputBuffer.Slice(chainStartLength.Item1, chainStartLength.Item2), outputBuffer, outputChains, cutLine, ref vertexCount, ref chainCount);
            }
        }
        
        private struct Crossing
        {
            public float T;
            public int Chain;
            public bool IsEntry;
        }

        private static bool AlmostEqual(Vector2 a, Vector2 b) => (a - b).sqrMagnitude <= Mathf.Epsilon * Mathf.Epsilon;

        private static float ProjectT(Vector2 p, Vector2 dir) => Vector2.Dot(p, dir);
        
        private static void SortCrossingsByT(Span<Crossing> a, int count)
        {
            for (var i = 1; i < count; i++)
            {
                var key = a[i];
                var j = i - 1;
                while (j >= 0 && a[j].T > key.T)
                {
                    a[j + 1] = a[j];
                    j--;
                }
                a[j + 1] = key;
            }
        }

        private static int AppendVertexNoDup(Span<CapsuleVertex> verts, int vCount, CapsuleVertex v)
        {
            if (vCount > 0 && AlmostEqual(verts[vCount - 1].Position, v.Position)) return vCount;
            verts[vCount++] = v;
            return vCount;
        }

        private static void CutPolygon(Span<CapsuleVertex> inputBuffer, Span<CapsuleVertex> outputBuffer,
            Span<ValueTuple<int, int>> outputChains, CutLine cutLine, ref int vertexCount, ref int chainCount)
        {
            var n = inputBuffer.Length;
            if (n < 3) return;

            var dotsHeapArray = Utils.GetFromPoolIfNeeded<float>(n);
            var dots = dotsHeapArray == null ? stackalloc float[n] : dotsHeapArray.AsSpan();

            var allOut = true;
            var outsideIndex = -1;

            for (var i = 0; i < n; i++)
            {
                var dot = Vector2.Dot(cutLine.Normal, inputBuffer[i].Position - cutLine.Start);
                dots[i] = dot;

                if (dot >= 0.0f) allOut = false;
                else outsideIndex = i;
            }

            // No points inside
            if (allOut)
            {
                Utils.ReturnToPool(dots.Length, dotsHeapArray);
                return;
            }
            
            // All points inside
            if (outsideIndex == -1)
            {
                var start = vertexCount;
                inputBuffer.CopyTo(outputBuffer[vertexCount..]);
                vertexCount += n;
                outputChains[chainCount++] = (start, n);
                Utils.ReturnToPool(dots.Length, dotsHeapArray);
                return;
            }
            
            var maxTempVerts = n * 2;

            var tempVertsHeap = Utils.GetFromPoolIfNeeded<CapsuleVertex>(maxTempVerts);
            var tempVerts = tempVertsHeap == null
                ? stackalloc CapsuleVertex[maxTempVerts]
                : tempVertsHeap.AsSpan();

            var tempChainsHeap = Utils.GetFromPoolIfNeeded<ValueTuple<int, int>>(n / 2 + 1);
            var tempChains = tempChainsHeap == null
                ? stackalloc ValueTuple<int, int>[n]
                : tempChainsHeap.AsSpan();

            var crossingsHeap = Utils.GetFromPoolIfNeeded<Crossing>(n);
            var crossings = crossingsHeap == null
                ? stackalloc Crossing[n]
                : crossingsHeap.AsSpan();

            var alongCutLine = new Vector2(-cutLine.Normal.y, cutLine.Normal.x);

            var tempVertexCount = 0;
            var tempChainCount = 0;
            var crossCount = 0;

            var currentChain = -1;
            var chainStart = 0;

            var p1 = inputBuffer[outsideIndex];
            var d1 = dots[outsideIndex];
            var in1 = d1 >= 0f;

            for (var i = 1; i <= n; i++)
            {
                var j = (outsideIndex + i) % n;
                var p2 = inputBuffer[j];
                var d2 = dots[j];
                var in2 = d2 >= 0f;

                if (in1 && in2)
                {
                    tempVertexCount = AppendVertexNoDup(tempVerts, tempVertexCount, p2);
                }
                else if (in1 && !in2)
                {
                    var inter = GetNewIntersectionPoint(p1, p2, d1, d2);
                    tempVertexCount = AppendVertexNoDup(tempVerts, tempVertexCount, inter);
                    
                    if (currentChain >= 0)
                    {
                        var pos = inter.Position;
                        crossings[crossCount++] = new Crossing
                        {
                            T = ProjectT(pos, alongCutLine),
                            Chain = currentChain,
                            IsEntry = false
                        };
                        tempChains[currentChain] = (chainStart, tempVertexCount - chainStart);
                        currentChain = -1;
                        tempChainCount++;
                    }
                }
                else if (!in1 && in2)
                {
                    var inter = GetNewIntersectionPoint(p1, p2, d1, d2);
                    currentChain = tempChainCount;
                    chainStart = tempVertexCount;

                    tempVertexCount = AppendVertexNoDup(tempVerts, tempVertexCount, inter);
                    tempVertexCount = AppendVertexNoDup(tempVerts, tempVertexCount, p2);

                    var pos = inter.Position;
                    crossings[crossCount++] = new Crossing
                    {
                        T = ProjectT(pos, alongCutLine),
                        Chain = currentChain,
                        IsEntry = true
                    };
                }

                p1 = p2;
                d1 = d2;
                in1 = in2;
            }
            
            if (tempChainCount == 0)
            {
                Utils.ReturnToPool(dots.Length, dotsHeapArray);
                Utils.ReturnToPool(tempVerts.Length, tempVertsHeap);
                Utils.ReturnToPool(tempChains.Length, tempChainsHeap);
                Utils.ReturnToPool(crossings.Length, crossingsHeap);
                return;
            }
            
            
            /*if ((crossCount & 1) != 0)
            {
            // ??
            }*/

            SortCrossingsByT(crossings, crossCount);

            var nextChainHeap = Utils.GetFromPoolIfNeeded<int>(tempChainCount);
            var nextChain = nextChainHeap == null ? stackalloc int[tempChainCount] : nextChainHeap.AsSpan();
            
            var exitNodeOfChainHeap = Utils.GetFromPoolIfNeeded<int>(tempChainCount);
            var exitNodeOfChain = exitNodeOfChainHeap == null ? stackalloc int[tempChainCount] : exitNodeOfChainHeap.AsSpan();

            for (var i = 0; i < tempChainCount; i++)
            {
                nextChain[i] = -1;
                exitNodeOfChain[i] = -1;
            }

            var entryOfNodeHeap = Utils.GetFromPoolIfNeeded<int>(crossCount);
            var entryOfNode = entryOfNodeHeap == null ? stackalloc int[crossCount] : entryOfNodeHeap.AsSpan();
            for (var i = 0; i < crossCount; i++)
            {
                entryOfNode[i] = -1;
            }

            for (var i = 0; i < crossCount; i++)
            {
                var c = crossings[i];
                if (c.IsEntry) entryOfNode[i] = c.Chain;
                else exitNodeOfChain[c.Chain] = i;
            }

            for (var i = 0; i < tempChainCount; i++)
            {
                var exitNode = exitNodeOfChain[i];
                if (exitNode < 0) continue;

                var partner = exitNode ^ 1;
                var next = entryOfNode[partner];

                nextChain[i] = next;
            }

            var visitedHeap = Utils.GetFromPoolIfNeeded<byte>(tempChainCount);
            var visited = visitedHeap == null ? stackalloc byte[tempChainCount] : visitedHeap.AsSpan();
            for (var i = 0; i < tempChainCount; i++) visited[i] = 0;

            for (var startChain = 0; startChain < tempChainCount; startChain++)
            {
                if (visited[startChain] != 0) continue;

                var polyStart = vertexCount;
                var current = startChain;

                while (current >= 0 && visited[current] == 0)
                {
                    visited[current] = 1;

                    var (s, len) = tempChains[current];
                    var src = tempVerts.Slice(s, len);

                    if (len > 0)
                    {
                        if (vertexCount > polyStart && AlmostEqual(outputBuffer[vertexCount - 1].Position, src[0].Position))
                        {
                            if (len > 1)
                            {
                                src[1..].CopyTo(outputBuffer[vertexCount..]);
                                vertexCount += (len - 1);
                            }
                        }
                        else
                        {
                            src.CopyTo(outputBuffer[vertexCount..]);
                            vertexCount += len;
                        }
                    }

                    var next = nextChain[current];
                    if (next == startChain)
                    {
                        break;
                    }
                    current = next;
                }

                var polyLen = vertexCount - polyStart;
                if (polyLen >= 3)
                    outputChains[chainCount++] = (polyStart, polyLen);
                else
                    vertexCount = polyStart;
            }

            Utils.ReturnToPool(dots.Length, dotsHeapArray);
            Utils.ReturnToPool(tempVerts.Length, tempVertsHeap);
            Utils.ReturnToPool(tempChains.Length, tempChainsHeap);
            Utils.ReturnToPool(crossings.Length, crossingsHeap);
            Utils.ReturnToPool(nextChain.Length, nextChainHeap);
            Utils.ReturnToPool(entryOfNode.Length, entryOfNodeHeap);
            Utils.ReturnToPool(exitNodeOfChain.Length, exitNodeOfChainHeap);
            Utils.ReturnToPool(visited.Length, visitedHeap);
        }

        private static CapsuleVertex GetNewIntersectionPoint(in CapsuleVertex point1, in CapsuleVertex point2, float dot1, float dot2)
        {
            var denom = dot1 - dot2;
            var t = 0.0f;
            if (Mathf.Abs(denom) > 0.0f)
            {
                t = Mathf.Clamp01(dot1 / denom);
            }
            var pos = Vector2.LerpUnclamped(point1.Position, point2.Position, t);
            var uv = Vector2.LerpUnclamped(point1.UV, point2.UV, t);

            return new CapsuleVertex
            {
                Position = pos,
                UV = uv
            };
        }
    }
}