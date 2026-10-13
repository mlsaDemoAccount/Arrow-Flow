using UnityEngine;
using UnityEngine.UI;

namespace ArrowFlow
{
    // Three reward stars drawn as one graphic, so no font or sprite coverage is needed.
    // Each time the dialog opens they pop in one after another, the middle one larger and raised.
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("UI/Arrow Attack/Star Display")]
    public sealed class StarDisplay : MaskableGraphic
    {
        [SerializeField, Range(0, 3)] int earned = 3;
        [Tooltip("Seconds between one star popping in and the next.")]
        public float popDelay = .22f;
        [Tooltip("Seconds each star takes to pop in.")]
        public float popSeconds = .32f;
        public Color fill = new Color32(255, 204, 0, 255);
        public Color shine = new Color32(255, 238, 140, 255);
        public Color outline = new Color32(58, 32, 0, 255);
        [Tooltip("Gray used for a star that was not earned, with its outline and highlight.")]
        public Color empty = new Color32(168, 172, 178, 255);
        public Color emptyShine = new Color32(214, 217, 221, 255);
        public Color emptyOutline = new Color32(62, 66, 72, 255);
        float shownAt = -1;

        public int Earned
        {
            get { return earned; }
            set { earned = Mathf.Clamp(value, 0, 3); Replay(); }
        }

        // Restart the pop-in animation.
        public void Replay()
        {
            shownAt = Time.unscaledTime;
            SetVerticesDirty();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Replay();
        }

        void Update()
        {
            if (Application.isPlaying && shownAt >= 0 && Time.unscaledTime - shownAt <= popDelay * 2 + popSeconds + .05f)
                SetVerticesDirty();
        }

        // Ease-out with a small overshoot: 0 before the star appears, 1 once it has settled.
        static float Pop(float t)
        {
            if (t <= 0) return 0;
            if (t >= 1) return 1;
            const float c1 = 1.70158f, c3 = c1 + 1;
            float u = t - 1;
            return 1 + c3 * u * u * u + c1 * u * u;
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect r = rectTransform.rect;
            float unit = Mathf.Min(r.width / 3.3f, r.height / 1.05f); // middle star's outer diameter
            for (int i = 0; i < 3; i++)
            {
                float t = Application.isPlaying ? (Time.unscaledTime - shownAt - i * popDelay) / Mathf.Max(.01f, popSeconds) : 1;
                float scale = Pop(t);
                if (scale <= 0) continue;
                bool middle = i == 1;
                float radius = unit * .5f * (middle ? 1f : .78f) * scale;
                Vector2 center = r.center + new Vector2((i - 1) * unit * 1.05f, middle ? unit * .06f : -unit * .08f);
                float tilt = (1 - i) * 14f; // side stars lean outward
                bool on = i < earned;
                Star(mesh, center, radius, tilt, (on ? outline : emptyOutline) * color);
                Star(mesh, center, radius * .8f, tilt, (on ? fill : empty) * color);
                Star(mesh, center + Rotate(new Vector2(-.12f, .14f), tilt) * radius, radius * .38f, tilt, (on ? shine : emptyShine) * color);
            }
        }

        static Vector2 Rotate(Vector2 v, float degrees)
        {
            float a = degrees * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        // A five-pointed star as a fan around its center.
        static void Star(VertexHelper mesh, Vector2 center, float radius, float tilt, Color tint)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(center, tint, Vector2.zero);
            for (int k = 0; k < 10; k++)
            {
                float length = k % 2 == 0 ? radius : radius * .48f;
                float angle = (90 + tilt + k * 36) * Mathf.Deg2Rad;
                mesh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * length, tint, Vector2.zero);
            }
            for (int k = 0; k < 10; k++)
                mesh.AddTriangle(start, start + 1 + k, start + 1 + (k + 1) % 10);
        }
    }
}
