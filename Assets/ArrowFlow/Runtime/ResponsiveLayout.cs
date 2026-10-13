using System;

namespace ArrowFlow
{
    // Rules for fitting the 720x1280 reference layout to any screen shape: tall phones, tablets, foldables and
    // resized windows. Pure math with no UnityEngine types, so FitDesign (in the game) and the offline layout
    // checks run exactly the same code. SCENE_UI_GUIDE.md ("Responsive layout") explains the rules.
    public static class ResponsiveLayout
    {
        public const float ReferenceWidth = 720, ReferenceHeight = 1280;
        // Backgrounds reach this far past the screen edges, so rounded corners and edge seams never show.
        public const float CoverMargin = 32;

        // How a direct child of a screen or dialog keeps its place when the screen is taller or wider than 720x1280.
        public enum Role
        {
            Top,        // keeps its distance from the top edge
            Middle,     // stays centred between the top and bottom groups
            Bottom,     // keeps its distance from the bottom edge
            Stretch,    // grows with the free space (the game board)
            EdgeLeft,   // side frame: hugs the left edge and grows with the height
            EdgeRight,  // side frame: hugs the right edge and grows with the height
            BandTop,    // top frame: hugs the top edge and grows with the width
            BandBottom, // bottom frame: hugs the bottom edge and grows with the width
            Cover,      // background: covers the whole screen, including notches and rounded corners
            Centre      // oversized art that already covers any screen: stays centred
        }

        // A rectangle in its parent's space, measured from the parent's bottom-left corner.
        public struct Box
        {
            public float xMin, yMin, xMax, yMax;
            public Box(float xMin, float yMin, float xMax, float yMax)
            {
                this.xMin = xMin; this.yMin = yMin; this.xMax = xMax; this.yMax = yMax;
            }
            public float Width { get { return xMax - xMin; } }
            public float Height { get { return yMax - yMin; } }
        }

        // RectTransform anchors (fractions of the parent) and offsets from them (design units).
        public struct Anchors
        {
            public float minX, minY, maxX, maxY, offsetMinX, offsetMinY, offsetMaxX, offsetMaxY;
        }

        // Elements that grow with the free space. A child that exactly fills one grows with it.
        public static readonly string[] StretchIds = { "Game/Viewport" };

        // Elements wholly above TopBand keep to the top and elements wholly below BottomBand keep to the bottom
        // (reference units from the top edge). Everything between moves together as the middle group, so rows
        // of buttons and grids are never pulled apart.
        public static float TopBand(string screen) { return screen == "Screen/Levels" ? 200 : 310; }
        public static float BottomBand(string screen) { return screen == "Screen/Home" ? 740 : 1016; }

        // Design area, in design units, for a safe area of the given aspect (width / height). Phones keep the
        // 720 width and taller phones get extra height; wider screens keep at least the 1280 height. On wider
        // screens the UI may also shrink by up to tabletRoom, which gives the game board more room on tablets.
        // The area is never smaller than 720x1280, so every screen fits without overlaps on any device.
        public static void DesignSize(float aspect, float tabletRoom, out float width, out float height)
        {
            const float phone = ReferenceWidth / ReferenceHeight;
            if (!(aspect > 0) || float.IsInfinity(aspect)) aspect = phone;
            if (aspect <= phone)
            {
                width = ReferenceWidth;
                height = ReferenceWidth / aspect;
                return;
            }
            float room = Math.Max(1f, Math.Min(tabletRoom, (float)Math.Sqrt(aspect / phone)));
            height = ReferenceHeight * room;
            width = height * aspect;
        }

        // Role of a direct child of a screen (or dialog) whose reference size is width x height.
        public static Role Classify(string screen, string id, bool dialog, float width, float height, Box box)
        {
            if (dialog) return Role.Middle;
            bool spanW = box.xMin <= 1 && box.xMax >= width - 1, spanH = box.yMin <= 1 && box.yMax >= height - 1;
            if (spanW && spanH)
                return box.Width <= width * 1.5f && box.Height <= height * 1.5f ? Role.Cover : Role.Centre;
            if (spanH) return box.xMin + box.xMax < width ? Role.EdgeLeft : Role.EdgeRight;
            float top = height - box.yMax, bottom = height - box.yMin; // edge distances from the top
            if (spanW)
                return bottom <= TopBand(screen) ? Role.BandTop : top >= BottomBand(screen) ? Role.BandBottom : Role.Middle;
            if (Array.IndexOf(StretchIds, id) >= 0) return Role.Stretch;
            if (bottom <= TopBand(screen)) return Role.Top;
            if (top >= BottomBand(screen)) return Role.Bottom;
            return Role.Middle;
        }

        // Anchors for a role that keep the box exactly where it is at the reference size.
        public static Anchors Anchor(Role role, Box box, float width, float height)
        {
            float minX = .5f, maxX = .5f, minY = .5f, maxY = .5f;
            switch (role)
            {
                case Role.Top: minY = maxY = 1; break;
                case Role.Bottom: minY = maxY = 0; break;
                case Role.Stretch: minX = minY = 0; maxX = maxY = 1; break;
                case Role.EdgeLeft: minX = maxX = 0; minY = 0; maxY = 1; break;
                case Role.EdgeRight: minX = maxX = 1; minY = 0; maxY = 1; break;
                case Role.BandTop: minX = 0; maxX = 1; minY = maxY = 1; break;
                case Role.BandBottom: minX = 0; maxX = 1; minY = maxY = 0; break;
            }
            return new Anchors
            {
                minX = minX, minY = minY, maxX = maxX, maxY = maxY,
                offsetMinX = box.xMin - width * minX, offsetMinY = box.yMin - height * minY,
                offsetMaxX = box.xMax - width * maxX, offsetMaxY = box.yMax - height * maxY
            };
        }

        // Where anchored content ends up inside a parent of the given size.
        public static Box Resolve(Anchors a, float width, float height)
        {
            return new Box(width * a.minX + a.offsetMinX, height * a.minY + a.offsetMinY,
                width * a.maxX + a.offsetMaxX, height * a.maxY + a.offsetMaxY);
        }

        // True when the box covers its whole parent.
        public static bool Fills(Box box, float width, float height)
        {
            return Math.Abs(box.xMin) < .5f && Math.Abs(box.yMin) < .5f
                && Math.Abs(box.xMax - width) < .5f && Math.Abs(box.yMax - height) < .5f;
        }

        // The smallest box with the given aspect (0: any shape) that covers the target plus a margin, centred on it.
        public static Box Envelope(Box target, float aspect, float margin)
        {
            float w = target.Width + 2 * margin, h = target.Height + 2 * margin;
            if (aspect > 0 && w > 0 && h > 0)
            {
                if (w / h > aspect) h = w / aspect;
                else w = h * aspect;
            }
            float cx = (target.xMin + target.xMax) * .5f, cy = (target.yMin + target.yMax) * .5f;
            return new Box(cx - w * .5f, cy - h * .5f, cx + w * .5f, cy + h * .5f);
        }
    }
}
