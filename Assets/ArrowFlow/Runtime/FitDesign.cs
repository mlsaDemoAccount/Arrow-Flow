using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace ArrowFlow
{
    // Fits the 720x1280 design to the screen. While playing, after GameApp calls MakeResponsive, the design takes
    // the shape of the device instead of leaving empty bands: headers keep to the top, buttons to the bottom,
    // content stays centred, the game board grows into the free space, frames follow the screen edges and
    // backgrounds cover the whole screen, on any phone, tablet, foldable or window. Edit mode keeps the fixed
    // 720x1280 layout, scaled to fit, for editing. The rules are in ResponsiveLayout.cs.
    [ExecuteAlways, RequireComponent(typeof(RectTransform))]
    public sealed class FitDesign : MonoBehaviour
    {
        [Tooltip("While playing, let the screens take each device's shape. Off: the fixed 720x1280 layout scaled to fit, with empty bands.")]
        public bool responsive = true;
        [Tooltip("On screens wider than a 9:16 phone (tablets, foldables), the UI may shrink by up to this factor so the game board gets more room. 1 keeps phone-sized UI.")]
        [Range(1, 1.4f)] public float tabletRoom = 1.15f;

        struct Cover { public RectTransform rect; public float aspect; }
        readonly List<Cover> covers = new List<Cover>();
        readonly Vector3[] corners = new Vector3[4];
        RectTransform canvasRect;
        Vector2 lastAvailable, lastCanvas;
        bool applied;
        int settle;

        // True once the screens follow the device's shape.
        public bool Applied { get { return applied; } }

        // Called once by GameApp at startup, before anything remembers UI positions. Re-anchors every screen and
        // dialog so each element keeps exactly its authored place at 720x1280 and moves sensibly at other sizes.
        public void MakeResponsive(SceneUI ui)
        {
            if (applied || !responsive || !Application.isPlaying) return;
            var design = (RectTransform)transform;
            var ids = new Dictionary<Transform, string>();
            if (ui != null)
                foreach (var entry in ui.elements)
                    if (entry != null && entry.gameObject != null && !ids.ContainsKey(entry.gameObject.transform))
                        ids.Add(entry.gameObject.transform, entry.id);
            Vector2 reference = design.rect.size;
            var fillers = new List<RectTransform>();
            for (int i = 0; i < design.childCount; i++)
            {
                var root = design.GetChild(i) as RectTransform;
                if (root == null) continue;
                string screen = Id(ids, root);
                bool dialog = screen.StartsWith("Dialog/", StringComparison.Ordinal);
                Vector2 size = root.rect.size;
                for (int c = 0; c < root.childCount; c++)
                {
                    var child = root.GetChild(c) as RectTransform;
                    if (child == null) continue;
                    var box = BoxOf(child, size);
                    var role = ResponsiveLayout.Classify(screen, Id(ids, child), dialog, size.x, size.y, box);
                    if (role == ResponsiveLayout.Role.Cover) { AddCover(child, true); continue; }
                    // Content that exactly fills a growing element (the board in its viewport) grows with it.
                    Vector2 inner = child.rect.size;
                    fillers.Clear();
                    if (role == ResponsiveLayout.Role.Stretch)
                        for (int g = 0; g < child.childCount; g++)
                        {
                            var grand = child.GetChild(g) as RectTransform;
                            if (grand != null && ResponsiveLayout.Fills(BoxOf(grand, inner), inner.x, inner.y)) fillers.Add(grand);
                        }
                    Apply(child, ResponsiveLayout.Anchor(role, box, size.x, size.y));
                    foreach (var grand in fillers)
                        Apply(grand, ResponsiveLayout.Anchor(ResponsiveLayout.Role.Stretch, BoxOf(grand, inner), inner.x, inner.y));
                }
                // Screens fill the safe area; a dialog's dimmed backdrop covers the whole screen.
                if (dialog) AddCover(root, false);
                else Apply(root, ResponsiveLayout.Anchor(ResponsiveLayout.Role.Stretch, BoxOf(root, reference), reference.x, reference.y));
            }
            applied = true;
            lastAvailable = Vector2.zero;
            var parent = design.parent as RectTransform;
            if (parent != null) FitTo(parent.rect.size);
        }

        void LateUpdate()
        {
            var parent = transform.parent as RectTransform;
            if (parent != null) FitTo(parent.rect.size);
        }

        // Sizes the design for the space available in its parent (the safe area), in canvas units.
        public void FitTo(Vector2 available)
        {
            var design = (RectTransform)transform;
            if (available.x <= 0 || available.y <= 0) return;
            if (!applied)
            {
                // The fixed 720x1280 layout, scaled to fit (edit mode, or Responsive switched off).
                if (design.rect.width <= 0 || design.rect.height <= 0) return;
                float fit = Mathf.Min(available.x / design.rect.width, available.y / design.rect.height);
                var fixedScale = new Vector3(fit, fit, 1);
                if (design.localScale != fixedScale) design.localScale = fixedScale;
                return;
            }
            if (canvasRect == null)
            {
                var canvas = GetComponentInParent<Canvas>();
                if (canvas != null) canvasRect = canvas.rootCanvas.transform as RectTransform;
            }
            Vector2 canvasSize = canvasRect != null ? canvasRect.rect.size : Vector2.zero;
            // Refit when the screen, safe area or ad banner changes, then once more so every rect has settled.
            if (available != lastAvailable || canvasSize != lastCanvas) settle = 2;
            else if (settle <= 0) return;
            settle--;
            lastAvailable = available;
            lastCanvas = canvasSize;
            float width, height;
            ResponsiveLayout.DesignSize(available.x / available.y, tabletRoom, out width, out height);
            design.anchorMin = design.anchorMax = design.pivot = new Vector2(.5f, .5f);
            design.anchoredPosition = Vector2.zero;
            design.sizeDelta = new Vector2(width, height);
            float scale = available.y / height;
            design.localScale = new Vector3(scale, scale, 1);
            UpdateCovers();
        }

        // Backgrounds and dialog backdrops cover the whole screen, beyond the safe area, keeping the art's shape.
        void UpdateCovers()
        {
            if (canvasRect == null) return;
            canvasRect.GetWorldCorners(corners);
            foreach (var cover in covers)
            {
                if (cover.rect == null) continue;
                var parent = cover.rect.parent as RectTransform;
                if (parent == null) continue;
                Vector3 a = parent.InverseTransformPoint(corners[0]), b = parent.InverseTransformPoint(corners[2]);
                var target = new ResponsiveLayout.Box(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
                var box = ResponsiveLayout.Envelope(target, cover.aspect, ResponsiveLayout.CoverMargin);
                cover.rect.sizeDelta = new Vector2(box.Width, box.Height);
                cover.rect.anchoredPosition = new Vector2((box.xMin + box.xMax) * .5f, (box.yMin + box.yMax) * .5f) - parent.rect.center;
            }
        }

        void AddCover(RectTransform rect, bool keepArtShape)
        {
            float aspect = 0;
            var image = keepArtShape ? rect.GetComponent<Image>() : null;
            if (image != null && image.sprite != null && image.type == Image.Type.Simple && image.sprite.rect.height > 0)
                aspect = image.sprite.rect.width / image.sprite.rect.height;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            covers.Add(new Cover { rect = rect, aspect = aspect });
        }

        static string Id(Dictionary<Transform, string> ids, Transform t)
        {
            string id;
            return ids.TryGetValue(t, out id) ? id : t.name;
        }

        // A RectTransform's rectangle in its parent, measured from the parent's bottom-left corner.
        static ResponsiveLayout.Box BoxOf(RectTransform rect, Vector2 parent)
        {
            Vector2 min = Vector2.Scale(parent, rect.anchorMin) + rect.offsetMin, max = Vector2.Scale(parent, rect.anchorMax) + rect.offsetMax;
            return new ResponsiveLayout.Box(min.x, min.y, max.x, max.y);
        }

        static void Apply(RectTransform rect, ResponsiveLayout.Anchors a)
        {
            rect.anchorMin = new Vector2(a.minX, a.minY);
            rect.anchorMax = new Vector2(a.maxX, a.maxY);
            rect.offsetMin = new Vector2(a.offsetMinX, a.offsetMinY);
            rect.offsetMax = new Vector2(a.offsetMaxX, a.offsetMaxY);
        }
    }
}
