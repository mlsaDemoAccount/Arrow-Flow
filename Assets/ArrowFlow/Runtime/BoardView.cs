using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ArrowFlow.Core;

namespace ArrowFlow
{
    // A single vector mesh keeps dense boards inexpensive on mobile; no external art is needed.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BoardView : MaskableGraphic, IPointerDownHandler, IPointerUpHandler, IDragHandler, IScrollHandler
    {
        public Puzzle puzzle;
        public HashSet<int> removed = new HashSet<int>();
        public Action<int> tapped;
        public bool guide;
        public int hint = -1, wrong = -1;
        public bool interactive = true;
        public Color ink = new Color32(34, 52, 60, 255), accent = new Color32(28, 125, 111, 255), error = new Color32(202, 79, 70, 255);
        float zoom = 1, departure = -1, departureTime, wrongUntil;
        int flying = -1;
        Vector2 pan, down, last;
        bool dragged, multi;
        float pinchDistance;
        Vector2 pinchMid;
        public float Zoom
        {
            get
            {
                return zoom;
            }
        }

        public void SetPuzzle(Puzzle value, HashSet<int> cleared)
        {
            puzzle = value; removed = cleared; hint = wrong = flying = -1;
            departure = -1; guide = false; interactive = true;
            multi = dragged = false; ResetView();
        }

        public void ResetView()
        {
            zoom = 1;
            pan = Vector2.zero;
            SetVerticesDirty();
        }

        public void Exit(int id)
        {
            flying = id;
            departureTime = Time.unscaledTime;
            departure = 0;
            hint = -1;
            guide = false;
            SetVerticesDirty();
        }

        public void Error(int id)
        {
            wrong = id;
            wrongUntil = Time.unscaledTime + 1.1f;
            SetVerticesDirty();
        }

        public void Refresh()
        {
            SetVerticesDirty();
        }

        public bool Animating
        {
            get
            {
                return flying >= 0;
            }
        }

        float Step
        {
            get
            {
                return puzzle == null ? 1 : Mathf.Min(rectTransform.rect.width / (puzzle.width + 1.8f), rectTransform.rect.height / (puzzle.height + 1.8f)) * zoom;
            }
        }

        Vector2 Pos(Cell c)
        {
            // Mesh vertices and pointer positions use local space. The UI factory uses a
            // top pivot, so local (0, 0) is the top edge, not the rectangle center.
            return rectTransform.rect.center + pan + new Vector2((c.x - (puzzle.width - 1) * .5f) * Step, (c.y - (puzzle.height - 1) * .5f) * Step);
        }

        void Update()
        {
            if (flying >= 0)
            {
                departure = (Time.unscaledTime - departureTime) / .45f;
                if (departure >= 1)
                {
                    flying = -1;
                    departure = -1;
                }

                SetVerticesDirty();
            }

            if (wrong >= 0 && Time.unscaledTime > wrongUntil)
            {
                wrong = -1;
                SetVerticesDirty();
            }

            if (!interactive)
                return;
            if (Input.touchCount >= 2)
            {
                Vector2 a = Input.GetTouch(0).position, b = Input.GetTouch(1).position;
                Vector2 mid = (a + b) * .5f;
                float distance = Vector2.Distance(a, b);
                if (!multi)
                {
                    multi = true;
                    pinchDistance = distance;
                    pinchMid = mid;
                }
                else
                {
                    if (pinchedInside())
                    {
                        float ratio = distance / Mathf.Max(1, pinchDistance);
                        zoom = Mathf.Clamp(zoom * ratio, 1, 3.5f);
                        pan += (mid - pinchMid) / Mathf.Max(.01f, rectTransform.lossyScale.x);
                        ClampPan();
                        SetVerticesDirty();
                    }

                    pinchDistance = distance;
                    pinchMid = mid;
                }

                dragged = true;
            }
            else if (Input.touchCount == 0)
                multi = false;
        }

        bool pinchedInside()
        {
            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, pinchMid, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera);
        }

        void ClampPan()
        {
            Vector2 limits = rectTransform.rect.size * (zoom - 1) * .5f;
            pan = new Vector2(Mathf.Clamp(pan.x, -limits.x, limits.x), Mathf.Clamp(pan.y, -limits.y, limits.y));
        }

        public void OnPointerDown(PointerEventData e)
        {
            down = last = e.position;
            dragged = false;
        }

        public void OnDrag(PointerEventData e)
        {
            if (!interactive || multi)
                return;
            if (Vector2.Distance(down, e.position) > 12)
                dragged = true;
            if (dragged && zoom > 1)
            {
                pan += (e.position - last) / Mathf.Max(.01f, rectTransform.lossyScale.x);
                ClampPan();
                SetVerticesDirty();
            }

            last = e.position;
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (!interactive || multi || dragged || Animating || puzzle == null || Vector2.Distance(down, e.position) > 12)
                return;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, e.position, e.pressEventCamera, out local);
            int best = -1;
            float nearest = Mathf.Min(Step * .38f, 22f);
            foreach (var a in puzzle.arrows)
                if (!removed.Contains(a.id))
                    for (int i = 1; i < a.cells.Count; i++)
                    {
                        Vector2 s = Pos(a.cells[i - 1]), t = Pos(a.cells[i]);
                        float d = Distance(local, s, t);
                        if (d < nearest)
                        {
                            nearest = d;
                            best = a.id;
                        }
                    }

            if (best >= 0 && tapped != null)
                tapped(best);
        }

        public void OnScroll(PointerEventData e)
        {
            if (!interactive)
                return;
            zoom = Mathf.Clamp(zoom + e.scrollDelta.y * .15f, 1, 3.5f);
            ClampPan();
            SetVerticesDirty();
        }

        static float Distance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 d = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, d) / d.sqrMagnitude);
            return Vector2.Distance(p, a + d * t);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (puzzle == null)
                return;
            float step = Step;
            float thick = Mathf.Clamp(step * .12f, 3, 8);
            if (guide)
                foreach (var a in puzzle.arrows)
                    if (!removed.Contains(a.id))
                        Ray(vh, a, new Color(accent.r, accent.g, accent.b, .19f), 1.6f);
            if (wrong >= 0)
            {
                var a = puzzle.arrows.Find(v => v.id == wrong);
                if (a != null)
                    Ray(vh, a, new Color(error.r, error.g, error.b, .65f), 2);
            }

            foreach (var a in puzzle.arrows)
            {
                if (removed.Contains(a.id) && a.id != flying)
                    continue;
                Color c = a.id == wrong ? error : a.id == hint ? accent : ink;
                if (a.id == flying)
                    c = accent;
                var points = new List<Vector2>();
                foreach (Cell cell in a.cells)
                    points.Add(Pos(cell));
                if (a.id == flying)
                {
                    float shift = departure * (a.cells.Count + Mathf.Max(puzzle.width, puzzle.height) + 3) * step;
                    points = Slide(points, new Vector2(a.Direction.x, a.Direction.y), shift);
                }

                if (a.id == hint)
                    foreach (var p in points)
                        Disk(vh, p, thick * 1.6f, new Color(accent.r, accent.g, accent.b, .16f));
                for (int i = 1; i < points.Count; i++)
                    Line(vh, points[i - 1], points[i], thick, c);
                for (int i = 0; i < points.Count - 1; i++)
                    Disk(vh, points[i], thick * .5f, c);
                Vector2 head = points[points.Count - 1], dir = new Vector2(a.Direction.x, a.Direction.y), side = new Vector2(-dir.y, dir.x);
                Line(vh, head, head - dir * step * .27f + side * step * .23f, thick, c);
                Line(vh, head, head - dir * step * .27f - side * step * .23f, thick, c);
                Disk(vh, head, thick * .5f, c);
            }
        }

        void Ray(VertexHelper vh, Arrow a, Color c, float width)
        {
            Vector2 start = Pos(a.Head), d = new Vector2(a.Direction.x, a.Direction.y);
            for (float t = Step * .5f; t < Step * (Mathf.Max(puzzle.width, puzzle.height) + 2); t += Step * .32f)
                Line(vh, start + d * t, start + d * (t + Step * .14f), width, c);
        }

        static List<Vector2> Slide(List<Vector2> p, Vector2 dir, float shift)
        {
            var r = new List<Vector2>();
            float total = 0;
            for (int i = 1; i < p.Count; i++)
                total += Vector2.Distance(p[i - 1], p[i]);
            r.Add(Sample(p, dir, shift));
            float arc = 0;
            for (int i = 1; i < p.Count; i++)
            {
                arc += Vector2.Distance(p[i - 1], p[i]);
                if (arc > shift && arc < shift + total)
                    r.Add(p[i]);
            }

            r.Add(Sample(p, dir, shift + total));
            return r;
        }

        static Vector2 Sample(List<Vector2> p, Vector2 dir, float distance)
        {
            for (int i = 1; i < p.Count; i++)
            {
                float len = Vector2.Distance(p[i - 1], p[i]);
                if (distance <= len)
                    return Vector2.Lerp(p[i - 1], p[i], distance / len);
                distance -= len;
            }

            return p[p.Count - 1] + dir * distance;
        }

        static void Line(VertexHelper v, Vector2 a, Vector2 b, float width, Color c)
        {
            Vector2 d = b - a;
            if (d.sqrMagnitude < .001f)
                return;
            Vector2 n = new Vector2(-d.y, d.x).normalized * width * .5f;
            int k = v.currentVertCount;
            v.AddVert(a - n, c, Vector2.zero);
            v.AddVert(a + n, c, Vector2.zero);
            v.AddVert(b + n, c, Vector2.zero);
            v.AddVert(b - n, c, Vector2.zero);
            v.AddTriangle(k, k + 1, k + 2);
            v.AddTriangle(k, k + 2, k + 3);
        }

        static void Disk(VertexHelper v, Vector2 center, float radius, Color c)
        {
            int k = v.currentVertCount;
            v.AddVert(center, c, Vector2.zero);
            for (int i = 0; i <= 10; i++)
            {
                float angle = i * Mathf.PI * .2f;
                v.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, c, Vector2.zero);
                if (i > 0)
                    v.AddTriangle(k, k + i, k + i + 1);
            }
        }
    }

}
