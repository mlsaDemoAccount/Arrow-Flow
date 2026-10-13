"""Build Arrow Attack's app icon from the supplied logo.

Run from the project root:  python Assets/ArrowFlow/Art/Icon/Source~/build_icon.py
Reads Assets/ArrowFlow/Art/Reference/Logo.png and writes Assets/ArrowFlow/Art/Icon/AppIcon.png
(1024x1024, no transparency, as the App Store requires). Needs Pillow and numpy.
Unity scales this one image to every Android, iOS and Windows icon size (Editor/Branding.cs).
"""
import os
import numpy as np
from PIL import Image

SIZE = 1024
LOGO = os.path.join("Assets", "ArrowFlow", "Art", "Reference", "Logo.png")
OUT = os.path.join("Assets", "ArrowFlow", "Art", "Icon", "AppIcon.png")
LOGO_WIDTH = 900            # pixels across the icon; keeps the logo inside a circular mask
LOGO_CENTER = (512, 520)    # slightly below the middle, like the title screen
CENTER_COLOR = np.array([176, 240, 255], float)
EDGE_COLOR = np.array([30, 153, 236], float)
RAYS = 16


def background():
    y, x = np.mgrid[0:SIZE, 0:SIZE].astype(float)
    dx, dy = x - SIZE / 2 + 0.5, y - SIZE / 2 + 0.5
    d = np.hypot(dx, dy) / (SIZE / 2 * np.sqrt(2))
    rgb = CENTER_COLOR + (EDGE_COLOR - CENTER_COLOR) * d[..., None]
    # Soft light rays, as on the game's title screen, fading in away from the middle.
    angle = np.arctan2(dy, dx) * RAYS / (2 * np.pi)
    wave = np.cos(2 * np.pi * (angle - np.floor(angle) - 0.5))
    ray = np.clip((wave - 0.35) / 0.3, 0, 1) * np.clip(d / 0.25, 0, 1)
    rgb += (255 - rgb) * 0.05 * ray[..., None]
    return rgb


def logo():
    im = Image.open(LOGO).convert("RGBA")
    a = np.asarray(im).astype(float)
    # The supplied PNG carries a faint haze (alpha below 10) over its whole rectangle,
    # which would show as a box on the icon; drop it and keep the real artwork.
    a[..., 3] = np.where(a[..., 3] < 10, 0, a[..., 3])
    im = Image.fromarray(a.astype(np.uint8), "RGBA")
    height = round(im.height * LOGO_WIDTH / im.width)
    return im.resize((LOGO_WIDTH, height), Image.LANCZOS)


def build():
    canvas = Image.fromarray(np.clip(np.round(background()), 0, 255).astype(np.uint8), "RGB").convert("RGBA")
    art = logo()
    left = LOGO_CENTER[0] - art.width // 2
    top = LOGO_CENTER[1] - art.height // 2
    canvas.alpha_composite(art, (left, top))
    icon = canvas.convert("RGB")
    assert icon.size == (SIZE, SIZE)
    icon.save(OUT, optimize=True)
    print(f"{OUT}: {SIZE}x{SIZE}, logo {art.width}x{art.height} at ({left}, {top})")


if __name__ == "__main__":
    build()
