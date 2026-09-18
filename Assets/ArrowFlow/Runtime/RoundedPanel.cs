using UnityEngine;
using UnityEngine.UI;
namespace ArrowFlow {
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoundedPanel : MaskableGraphic
    {
        public float radius = 24;
        protected override void OnPopulateMesh(VertexHelper v)
        {
            v.Clear();
            Rect r = rectTransform.rect;
            float rad = Mathf.Min(radius, Mathf.Min(r.width, r.height) * .5f);
            v.AddVert(r.center, color, Vector2.zero);
            int n = 0;
            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 center = new Vector2(corner < 2 ? r.xMax - rad : r.xMin + rad, corner == 0 || corner == 3 ? r.yMin + rad : r.yMax - rad);
                for (int i = 0; i <= 8; i++)
                {
                    float a = (-90 + corner * 90 + i * 90f / 8) * Mathf.Deg2Rad;
                    v.AddVert(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rad, color, Vector2.zero);
                    n++;
                    if (n > 1)
                        v.AddTriangle(0, n - 1, n);
                }
            }

            v.AddTriangle(0, n, 1);
        }
    }

}
