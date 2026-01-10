using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace Utkaka.ScaleNineSlicer.UI
{
    public class VertexHelperVisualizer : MonoBehaviour
    {
        [SerializeField]
        private Transform _verticesContainer;
        [SerializeField]
        private Transform _linesContainer;
        private void OnDrawGizmos()
        {
            var vertices = _verticesContainer.GetComponentsInChildren<Transform>().Where(t => t != _verticesContainer)
                .Select((t) => new CapsuleVertex { Position = t.position }).ToArray();

            var lineTransforms = _linesContainer.GetComponentsInChildren<Transform>()
                .Where(t => t != _linesContainer).ToList();
            
            var lines = new CutLine[lineTransforms.Count / 2];
            var lineIndex = 0;
            for (var i = 0; i < lineTransforms.Count; i +=2 )
            {
                var mid = (lineTransforms[i].position + lineTransforms[i + 1].position) * 0.5f;
                var line = CutLine.FromLine(lineTransforms[i].position, lineTransforms[i + 1].position);
                Handles.DrawLine(lineTransforms[i].position, lineTransforms[i + 1].position);
                Handles.DrawLine(mid, mid + (Vector3)line.Normal.normalized);
                lines[lineIndex++] = line;
            }

            var helper = new VertexHelper();
            
            
            
            CapsuleMeshHandler.PrepareMesh(vertices.AsSpan(), lines.AsSpan(), helper);
            
            var mesh = new Mesh();
            helper.FillMesh(mesh);

            for (var i = 0; i < mesh.vertexCount; i++)
            {
                var v = mesh.vertices[i];
                Handles.color = mesh.colors[i];
                Handles.DrawSolidDisc(v, Vector3.forward, 0.25f);
            }
            
            for (var i = 0; i < mesh.GetIndexCount(0); i += 3)
            {
                var v1 = mesh.vertices[mesh.GetIndices(0)[i]];
                var v2 = mesh.vertices[mesh.GetIndices(0)[i + 1]];
                var v3 = mesh.vertices[mesh.GetIndices(0)[i + 2]];
                
                Handles.color = mesh.colors[mesh.GetIndices(0)[i]];
                
                Handles.DrawLine(v1, v2, 0.25f);
                Handles.DrawLine(v2, v3, 0.25f);
                Handles.DrawLine(v3, v1, 0.25f);
            }
                
            helper.Dispose();
        }
    }
}