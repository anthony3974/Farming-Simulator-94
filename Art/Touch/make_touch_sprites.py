"""Draws the touch control sprites into Assets/Images/Touch (run: python3 Art/Touch/make_touch_sprites.py).

RoundedBorder.png / RoundedFill.png: white rounded squares for the rounded button shape (9-sliced in Unity,
tinted by the Image colour, so the yellow border and brown fill colours stay on the buttons). 128 px, drawn
at 2x: the Image uses Pixels Per Unit Multiplier 2, so the corner radius is 24 (border) and 19 (fill, 5 px in).
SteeringWheel.png: the steering wheel in the UI colours (yellow, brown, gold), with a gold mark at the top
so you can see it turn. 512 px, drawn at 4x and scaled down for smooth edges.
"""
import math, os
from PIL import Image, ImageDraw

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Images", "Touch")
os.makedirs(OUT, exist_ok=True)

YELLOW = (255, 204, 0, 255)
BROWN = (60, 45, 20, 255)
GOLD = (255, 201, 51, 255)
DARK = (40, 30, 12, 255)


def rounded(name, size, radius):
    s = 4
    img = Image.new("RGBA", (size * s, size * s), (255, 255, 255, 0))
    ImageDraw.Draw(img).rounded_rectangle([0, 0, size * s - 1, size * s - 1], radius=radius * s, fill=(255, 255, 255, 255))
    img.resize((size, size), Image.LANCZOS).save(os.path.join(OUT, name))


def wheel(name, size):
    s = 4
    n = size * s
    c = n / 2
    img = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)  # ImageDraw writes pixels as given, so a transparent fill cuts a hole

    def circle(r, fill):
        d.ellipse([c - r, c - r, c + r, c + r], fill=fill)

    def spoke(ang, half_width, start, end, fill):
        a = math.radians(ang)
        dx, dy = math.cos(a), -math.sin(a)
        px, py = -dy, dx
        x1, y1, x2, y2 = c + dx * start, c + dy * start, c + dx * end, c + dy * end
        d.polygon([(x1 + px * half_width, y1 + py * half_width), (x2 + px * half_width, y2 + py * half_width),
                   (x2 - px * half_width, y2 - py * half_width), (x1 - px * half_width, y1 - py * half_width)], fill=fill)

    R = n / 2 - 2 * s
    edge = 10 * s
    inner = R - 56 * s  # inside edge of the rim
    hub = 60 * s
    # rim: yellow edge, brown grip, yellow edge, then the open middle
    circle(R, YELLOW)
    circle(R - edge, BROWN)
    circle(inner, YELLOW)
    circle(inner - edge, (0, 0, 0, 0))
    # three spokes (left, right, down) from the hub into the rim's inner edge
    for ang in (180, 0, 270):  # 0 = right, 270 = down
        spoke(ang, 28 * s, 0, inner - edge / 2, YELLOW)
    for ang in (180, 0, 270):
        spoke(ang, 28 * s - edge, 0, inner - edge / 2, BROWN)
    # hub
    circle(hub, YELLOW)
    circle(hub - edge, BROWN)
    circle(hub - 32 * s, DARK)
    # gold mark at the top of the rim, so the turn shows
    mw, mh = 18 * s, 40 * s
    d.rounded_rectangle([c - mw, c - R + 3 * s, c + mw, c - R + 3 * s + mh], radius=8 * s, fill=GOLD)
    img.resize((size, size), Image.LANCZOS).save(os.path.join(OUT, name))


rounded("RoundedBorder.png", 128, 48)
rounded("RoundedFill.png", 128, 38)
wheel("SteeringWheel.png", 512)
print("written to", os.path.abspath(OUT))
