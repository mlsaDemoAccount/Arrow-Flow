using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowFlow
{
    // A round, editable outline for the reference's cartoon lettering.
    [AddComponentMenu("UI/Arrow Attack/Text Stroke")]
    public sealed class TextStroke : BaseMeshEffect
    {
        [Range(0, 8)] public float width = 3;
        public Color strokeColor = Color.black;
        public override void ModifyMesh(VertexHelper mesh)
        {
            if (!IsActive() || width <= 0) return;
            var source = new List<UIVertex>(); mesh.GetUIVertexStream(source);
            var result = new List<UIVertex>(source.Count * 13);
            for (int direction = 0; direction < 12; direction++)
            {
                float angle = direction * Mathf.PI / 6;
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * width;
                foreach (var original in source)
                {
                    var v = original; v.position += offset;
                    var tint = strokeColor; tint.a *= original.color.a / 255f; v.color = tint;
                    result.Add(v);
                }
            }
            result.AddRange(source); mesh.Clear(); mesh.AddUIVertexTriangleStream(result);
        }
    }
}
