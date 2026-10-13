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
        [Tooltip("Color of an arrow leaving after a correct tap. A wrongly tapped arrow turns Error red, bumps into the arrow in its way, slides back and blinks.")]
        public Color success = Color.white;
        [Tooltip("Arrow line thickness as a fraction of the cell size.")]
        [Range(.08f, .3f)] public float lineThickness = .16f;
        [Tooltip("Thinnest and thickest arrow line, in UI units (the thickest grows when zoomed in).")]
        public float minLineThickness = 6f, maxLineThickness = 8f;
        float zoom = 1, departure = -1, departureTime, wrongStart, wrongOut;
        int flying = -1, wrongBlocker = -1, wrongGap;
        // Wrong-tap animation: back in its place WrongBack seconds after the hit, then WrongBlinks red flashes over
        // WrongBlink seconds. The arrow it hits is knocked aside for KnockTime seconds.
        const float WrongBack = .24f, WrongBlink = .6f, KnockTime = .2f;
        const int WrongBlinks = 3;
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
            puzzle = value; removed = cleared; hint = wrong = flying = wrongBlocker = -1;
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
            if (wrong == id) wrong = wrongBlocker = -1;
            flying = id;
            departureTime = Time.unscaledTime;
            departure = 0;
            hint = -1;
            guide = false;
            SetVerticesDirty();
        }

        // A wrong tap: the arrow turns Error red, slides forward until it hits the arrow in its way, knocks it,
        // slides back to its place and blinks. Other arrows can still be tapped meanwhile.
        public void Error(int id)
        {
            wrong = id;
            wrongStart = Time.unscaledTime;
            wrongBlocker = Blocker(id, out wrongGap);
            // Farther blockers take a little longer to reach.
            wrongOut = Mathf.Clamp(.1f + .03f * wrongGap, .1f, .25f);
            SetVerticesDirty();
        }

        // Seconds from a wrong tap until the arrow is back in its place, before it blinks.
        public float WrongReturnSeconds
        {
            get
            {
                return wrong >= 0 ? wrongOut + WrongBack : 0;
            }
        }

        // The first arrow on this arrow's way out, and how many free cells lie before it.
        int Blocker(int id, out int gap)
        {
            gap = 0;
            var a = puzzle == null ? null : puzzle.arrows.Find(v => v.id == id);
            if (a == null || a.cells.Count < 2)
                return -1;
            var owners = new Dictionary<Cell, int>();
            foreach (var other in puzzle.arrows)
                if (other.id != id && !removed.Contains(other.id))
                    foreach (var c in other.cells)
                        owners[c] = other.id;
            for (Cell c = a.Head + a.Direction; puzzle.Inside(c); c += a.Direction, gap++)
            {
                int owner;
                if (owners.TryGetValue(c, out owner))
                    return owner;
            }
            return -1;
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

            if (wrong >= 0)
            {
                if (Time.unscaledTime - wrongStart >= wrongOut + WrongBack + WrongBlink)
                    wrong = wrongBlocker = -1;
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
            // Generous touch area: the nearest arrow within about half a cell.
            float nearest = Mathf.Max(Thickness, Mathf.Min(Step * .45f, 40f));
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

        float Thickness
        {
            get
            {
                float thinnest = Mathf.Max(1, minLineThickness);
                return Mathf.Clamp(Step * lineThickness, thinnest, Mathf.Max(thinnest, maxLineThickness * zoom));
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (puzzle == null)
                return;
            float step = Step;
            float thick = Thickness;
            int cellCount = 0;
            foreach (var arrow in puzzle.arrows) cellCount += arrow.cells.Count;
            // Reserve room for the board and hint glow under Unity UI's 65K vertex limit.
            int guideDashes = Mathf.Clamp((60000 - cellCount * 16 - puzzle.arrows.Count * 8)
                / Mathf.Max(1, puzzle.arrows.Count * 4), 1, 12);
            if (guide)
                foreach (var a in puzzle.arrows)
                    if (!removed.Contains(a.id))
                        Ray(vh, a, new Color(accent.r, accent.g, accent.b, .19f), Mathf.Max(1.6f, thick * .3f), guideDashes);
            var bumping = wrong >= 0 ? puzzle.arrows.Find(v => v.id == wrong) : null;
            float bumpShift = 0, knock = 0;
            bool bumpRed = true;
            if (bumping != null)
                BumpState(step, thick, out bumpShift, out bumpRed, out knock);
            foreach (var a in puzzle.arrows)
            {
                if (removed.Contains(a.id) && a.id != flying)
                    continue;
                // The wrongly tapped arrow is drawn last, over the arrows it slides past.
                if (a == bumping && a.id != flying)
                    continue;
                Color c = a.id == hint ? accent : ink;
                var points = new List<Vector2>();
                foreach (Cell cell in a.cells)
                    points.Add(Pos(cell));
                if (a.id == flying)
                {
                    c = success;
                    float shift = departure * (a.cells.Count + Mathf.Max(puzzle.width, puzzle.height) + 3) * step;
                    points = Slide(points, new Vector2(a.Direction.x, a.Direction.y), shift);
                }
                else if (a.id == wrongBlocker && bumping != null && knock > 0)
                {
                    // Knocked aside by the hit.
                    Vector2 push = new Vector2(bumping.Direction.x, bumping.Direction.y) * knock;
                    for (int i = 0; i < points.Count; i++)
                        points[i] += push;
                }

                if (a.id == hint)
                    foreach (var p in points)
                        Disk(vh, p, thick * 1.6f, new Color(accent.r, accent.g, accent.b, .16f));
                DrawArrow(vh, a, points, c, step, thick);
            }

            if (bumping != null && bumping.id != flying && !removed.Contains(bumping.id))
            {
                var points = new List<Vector2>();
                foreach (Cell cell in bumping.cells)
                    points.Add(Pos(cell));
                if (bumpShift > 0)
                    points = Slide(points, new Vector2(bumping.Direction.x, bumping.Direction.y), bumpShift);
                DrawArrow(vh, bumping, points, bumpRed ? error : ink, step, thick);
            }
        }

        // The wrong-tap animation at this moment: how far the arrow has slid toward the arrow in its way (speeding
        // up on the way there, easing on the way back), whether it shows red (while moving, then three blinks) and
        // how far the arrow it hit is knocked aside.
        void BumpState(float step, float thick, out float shift, out bool red, out float knock)
        {
            float t = Time.unscaledTime - wrongStart;
            // Close enough for the arrow's tip to touch the line it hits.
            float reach = Mathf.Max(step * .3f, (wrongGap + 1) * step - thick * 1.05f);
            shift = knock = 0;
            red = true;
            if (t < wrongOut)
            {
                float p = t / wrongOut;
                shift = reach * p * p;
            }
            else if (t < wrongOut + WrongBack)
            {
                float q = 1 - (t - wrongOut) / WrongBack;
                shift = reach * q * q;
            }
            else
                red = (int)((t - wrongOut - WrongBack) / (WrongBlink / (WrongBlinks * 2))) % 2 == 1;
            float k = (t - wrongOut) / KnockTime;
            if (wrongBlocker >= 0 && k > 0 && k < 1)
                knock = Mathf.Sin(k * Mathf.PI) * step * .14f;
        }

        void DrawArrow(VertexHelper vh, Arrow a, List<Vector2> points, Color c, float step, float thick)
        {
            for (int i = 1; i < points.Count; i++)
                Line(vh, points[i - 1], points[i], thick, c);
            for (int i = 0; i < points.Count - 1; i++)
                Disk(vh, points[i], thick * .5f, c);
            // Solid arrowhead in proportion to the line, never wider than its cell allows.
            Vector2 head = points[points.Count - 1], dir = new Vector2(a.Direction.x, a.Direction.y), side = new Vector2(-dir.y, dir.x);
            Vector2 tip = head + dir * thick * .55f, back = head - dir * Mathf.Min(thick * 3.4f, step * .42f);
            float halfWidth = Mathf.Min(thick * 2.1f, step * .3f);
            Triangle(vh, tip, back + side * halfWidth, back - side * halfWidth, c);
        }

        static void Triangle(VertexHelper v, Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            int k = v.currentVertCount;
            v.AddVert(a, color, Vector2.zero);
            v.AddVert(b, color, Vector2.zero);
            v.AddVert(c, color, Vector2.zero);
            v.AddTriangle(k, k + 1, k + 2);
        }

        void Ray(VertexHelper vh, Arrow a, Color c, float width, int maxDashes = 12)
        {
            Vector2 start = Pos(a.Head), d = new Vector2(a.Direction.x, a.Direction.y);
            int cells = a.Direction.x > 0 ? puzzle.width - a.Head.x : a.Direction.x < 0 ? a.Head.x + 1
                : a.Direction.y > 0 ? puzzle.height - a.Head.y : a.Head.y + 1;
            float distance = Step * cells;
            int dashes = Mathf.Min(maxDashes, Mathf.Max(1, Mathf.CeilToInt(cells / .32f)));
            float spacing = distance / dashes;
            for (int i = 0; i < dashes; i++)
            {
                float t = spacing * (i + .25f);
                Line(vh, start + d * t, start + d * (t + spacing * .44f), width, c);
            }
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
