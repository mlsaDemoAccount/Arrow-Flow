using UnityEngine;
using UnityEngine.UI;

namespace ArrowFlow
{
    // Fixed slots and a shared contour keep full/empty hearts aligned on every platform.
    // Drawing the icons avoids relying on the UI font's Unicode heart coverage.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HeartDisplay : MaskableGraphic
    {
        [SerializeField, Range(0, 3)] int lives = 3;
        public Image[] slots;
        const int Segments = 64;
        const float Size = 28, Gap = 10;

        public int Lives
        {
            get { return lives; }
            set
            {
                int next = Mathf.Clamp(value, 0, 3);
                if (lives == next) return;
                lives = next; RefreshSlots();
                SetVerticesDirty();
            }
        }

        public void RefreshSlots()
        {
            if (slots == null) return;
            for (int i = 0; i < slots.Length; i++) if (slots[i] != null)
                slots[i].color = i < lives ? Color.white : new Color(.35f, .45f, .55f, .4f);
            SetVerticesDirty();
        }
        protected override void OnEnable() { base.OnEnable(); RefreshSlots(); }

        static Vector2 Contour(int segment)
        {
            float t = segment * (2 * Mathf.PI / Segments);
            float sine = Mathf.Sin(t);
            float x = 16 * sine * sine * sine;
            float y = 13 * Mathf.Cos(t) - 5 * Mathf.Cos(2 * t)
                - 2 * Mathf.Cos(3 * t) - Mathf.Cos(4 * t);
            return new Vector2(x / 32, (y + 2.5f) / 29);
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (slots != null && slots.Length == 3) return;
            Rect rect = rectTransform.rect;
            float scale = Mathf.Min(1, Mathf.Min(rect.width / (3 * Size + 2 * Gap), rect.height / Size));
            for (int slot = 0; slot < 3; slot++)
            {
                Vector2 center = rect.center + Vector2.right * ((slot - 1) * (Size + Gap) * scale);
                bool filled = slot < lives;
                Color tint = color;
                if (!filled) tint.a *= .55f;
                int start = mesh.currentVertCount;
                // Pair each contour point with a center (full) or inner contour (empty).
                for (int i = 0; i < Segments; i++)
                {
                    Vector2 edge = Contour(i) * (Size * scale);
                    mesh.AddVert(center + edge, tint, Vector2.zero);
                    mesh.AddVert(center + (filled ? Vector2.zero : edge * .76f), tint, Vector2.zero);
                }
                for (int i = 0; i < Segments; i++)
                {
                    int outer = start + i * 2;
                    int next = start + ((i + 1) % Segments) * 2;
                    mesh.AddTriangle(outer, next, outer + 1);
                    if (!filled) mesh.AddTriangle(next, next + 1, outer + 1);
                }
            }
        }
    }
}

