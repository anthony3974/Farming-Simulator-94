#!/usr/bin/env python3
"""
Draws the Farming Simulator 94 app icon and renders every size the game needs.

Run from the project folder:  python3 Art/Icon/make_icon.py
Needs rsvg-convert (librsvg). Writes the SVGs next to this script and the PNGs to:
  Assets/Images/Icon/IconBackground.png   432 x 432  Android adaptive icon, back layer (sky, sun, field)
  Assets/Images/Icon/IconForeground.png   432 x 432  Android adaptive icon, front layer (the tractor, transparent around it)
  Assets/Images/Icon/Icon.png            1024 x 1024 Default icon and Android legacy icon (both layers, square)
  Assets/Images/Icon/IconRound.png       1024 x 1024 Android round icon (the same, cut to a circle)
  Art/Icon/PlayStoreIcon.png              512 x 512  for the Google Play listing (upload it in the Play Console)

Everything is drawn on Android's 108 x 108 adaptive-icon grid. A phone shows only the middle 72 x 72 and may
cut it to a circle, so the tractor stays inside the middle 66 x 66 circle (the "safe zone"). The square icons
show that middle 72 x 72. Colours are the tractor's materials (Assets/Tractor) and the game's UI gold.
"""
import os
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.dirname(os.path.dirname(HERE))
OUT = os.path.join(PROJECT, "Assets", "Images", "Icon")

PAINT = "#C71A14"      # TractorPaint
PAINT_DARK = "#8E120E"
DARK = "#212124"       # TractorDark
RUBBER = "#141414"     # TractorRubber
HUB = "#EBC733"        # TractorHub
GLASS = "#33526B"      # TractorGlass
LIGHT = "#FFF5CC"      # TractorLight
GOLD = "#FFC933"       # UI gold text

BACKGROUND = f"""
  <defs>
    <linearGradient id="sky" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#3E8FD6"/>
      <stop offset="1" stop-color="#A9D8F5"/>
    </linearGradient>
    <linearGradient id="soil" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#7A4E26"/>
      <stop offset="1" stop-color="#5A3818"/>
    </linearGradient>
  </defs>
  <rect width="108" height="108" fill="url(#sky)"/>
  <circle cx="73" cy="35" r="12.5" fill="#FFF3B8" opacity="0.45"/>
  <circle cx="73" cy="35" r="8.5" fill="{GOLD}"/>
  <path d="M0 64 Q 27 57 54 62 T 108 60 V 70 H 0 Z" fill="#5E9E3A"/>
  <rect y="67" width="108" height="41" fill="url(#soil)"/>
  <rect y="66" width="108" height="3" fill="#4C8A2C"/>
  {''.join(f'<rect y="{y}" width="108" height="{h}" fill="#4A2D12" opacity="0.55"/>' for y, h in ((73, 1.6), (80, 2.2), (89, 2.8), (100, 3.4)))}
"""

# side view, facing right; wheels stand on y = 78 before the scale below
TRACTOR = f"""
  <g transform="translate(54 53) scale(0.85) translate(-54.5 -53)">
    <!-- exhaust -->
    <rect x="69.5" y="37" width="3" height="14" rx="1" fill="{DARK}"/>
    <rect x="68.8" y="35.5" width="4.4" height="2.4" rx="1" fill="{DARK}"/>
    <!-- hood -->
    <path d="M46 49 H79 Q83 49 83 53 V62 H46 Z" fill="{PAINT}"/>
    <rect x="46" y="49" width="37" height="2.2" fill="#E2362F"/>
    <rect x="80.6" y="52" width="2.4" height="9" fill="{DARK}"/>
    <circle cx="81.2" cy="51.6" r="1.5" fill="{LIGHT}"/>
    <text x="62" y="59.6" font-family="DejaVu Sans, Liberation Sans, sans-serif" font-weight="bold"
          font-size="8.5" text-anchor="middle" fill="{GOLD}">94</text>
    <!-- frame under the hood, joins the wheels -->
    <rect x="40" y="61" width="36" height="4" fill="{DARK}"/>
    <!-- cab -->
    <rect x="30" y="32" width="20" height="30" rx="1.5" fill="{PAINT}"/>
    <rect x="33" y="35.5" width="14.5" height="15" rx="1" fill="{GLASS}"/>
    <path d="M34.5 37 L40 37 L35 49 L34.5 49 Z" fill="#FFFFFF" opacity="0.25"/>
    <rect x="27.5" y="28" width="25" height="4.5" rx="1.2" fill="{DARK}"/>
    <rect x="31" y="31.5" width="2" height="3" fill="{DARK}"/>
    <rect x="47" y="31.5" width="2" height="3" fill="{DARK}"/>
    <!-- rear wheel -->
    <circle cx="40" cy="64" r="14" fill="{RUBBER}"/>
    <circle cx="40" cy="64" r="14" fill="none" stroke="#2C2C2C" stroke-width="2.4" stroke-dasharray="3 2.5"/>
    <circle cx="40" cy="64" r="8.2" fill="{HUB}"/>
    <circle cx="40" cy="64" r="5.6" fill="none" stroke="#C9A51E" stroke-width="1"/>
    <circle cx="40" cy="64" r="2.6" fill="{DARK}"/>
    <!-- rear fender over the wheel -->
    <path d="M24.5 61 A15.8 15.8 0 0 1 55.5 61 L52.6 61 A12.9 12.9 0 0 0 27.4 61 Z" fill="{PAINT_DARK}"/>
    <!-- front wheel -->
    <circle cx="72" cy="68.5" r="9.5" fill="{RUBBER}"/>
    <circle cx="72" cy="68.5" r="9.5" fill="none" stroke="#2C2C2C" stroke-width="1.8" stroke-dasharray="2.4 2"/>
    <circle cx="72" cy="68.5" r="5.4" fill="{HUB}"/>
    <circle cx="72" cy="68.5" r="1.9" fill="{DARK}"/>
  </g>
"""

SHADOW = '<ellipse cx="54.5" cy="74.6" rx="27" ry="2.6" fill="#000000" opacity="0.25"/>'


def svg(body, view="0 0 108 108", clip_circle=False):
    clip = ""
    if clip_circle:
        x, y, w, _ = (float(v) for v in view.split())
        clip = f'<clipPath id="round"><circle cx="{x + w / 2}" cy="{y + w / 2}" r="{w / 2}"/></clipPath>'
        body = f'<g clip-path="url(#round)">{body}</g>'
    return f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="{view}"><defs>{clip}</defs>{body}</svg>\n'


def render(name, content, size, png_path):
    svg_path = os.path.join(HERE, name + ".svg")
    with open(svg_path, "w") as f:
        f.write(content)
    subprocess.run(["rsvg-convert", "-w", str(size), "-h", str(size), "-o", png_path, svg_path], check=True)
    print(f"{png_path}  ({size} x {size})")


def main():
    os.makedirs(OUT, exist_ok=True)
    visible = "18 18 72 72"  # what a phone shows of the 108 grid
    full = BACKGROUND + SHADOW + TRACTOR
    render("IconBackground", svg(BACKGROUND), 432, os.path.join(OUT, "IconBackground.png"))
    render("IconForeground", svg(SHADOW + TRACTOR), 432, os.path.join(OUT, "IconForeground.png"))
    render("Icon", svg(full, visible), 1024, os.path.join(OUT, "Icon.png"))
    render("IconRound", svg(full, visible, clip_circle=True), 1024, os.path.join(OUT, "IconRound.png"))
    render("PlayStoreIcon", svg(full, visible), 512, os.path.join(HERE, "PlayStoreIcon.png"))


if __name__ == "__main__":
    sys.exit(main())
