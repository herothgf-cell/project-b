"""Draws the speed camera map icon (2026-11-26, user pick: design A, CCTV shape).

Output: unity/EungyeTaxi/Assets/Resources/UI/speed-camera-cctv.png (128 x 128, transparent).
A yellow round badge with a dark rim and a black CCTV camera: an angled bullet body with a sun shield,
a lens looking down-left, and a bracket arm to a wall plate on the right.
Run: python tools/art/draw_speed_camera_icon.py
"""
import math
import os
from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, 'unity', 'EungyeTaxi', 'Assets', 'Resources', 'UI', 'speed-camera-cctv.png')
S = 512
YELLOW = (255, 204, 26, 255)
INK = (26, 28, 33, 255)
LENS = (200, 214, 230, 255)


def rotated(points, cx, cy, deg):
    a = math.radians(deg)
    c, s = math.cos(a), math.sin(a)
    return [(cx + (x - cx) * c - (y - cy) * s, cy + (x - cx) * s + (y - cy) * c) for x, y in points]


def main():
    badge = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    bd = ImageDraw.Draw(badge)
    rim = 22
    bd.ellipse([0, 0, S - 1, S - 1], fill=INK)
    bd.ellipse([rim, rim, S - 1 - rim, S - 1 - rim], fill=YELLOW)
    # The camera is drawn on its own layer and enlarged so it still reads at 22 px on the minimap.
    im = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    cx, cy, tilt = 236, 236, -24  # body centre; negative tilt points the lens down-left
    # Wall plate and bracket (drawn first, behind the body).
    d.rounded_rectangle([352, 170, 384, 300], radius=10, fill=INK)
    arm = rotated([(300, 222), (368, 222), (368, 248), (300, 248)], 334, 235, 0)
    d.polygon(arm, fill=INK)
    d.polygon(rotated([(282, 230), (318, 230), (318, 262), (282, 262)], cx, cy, tilt), fill=INK)
    # Bullet body with a sun shield that overhangs the lens end.
    body = rotated([(120, 196), (316, 196), (316, 270), (120, 270)], cx, cy, tilt)
    d.polygon(body, fill=INK)
    shield = rotated([(96, 176), (326, 176), (326, 198), (96, 198)], cx, cy, tilt)
    d.polygon(shield, fill=INK)
    # Lens on the front face.
    lx, ly = rotated([(128, 233)], cx, cy, tilt)[0]
    d.ellipse([lx - 30, ly - 30, lx + 30, ly + 30], fill=INK)
    d.ellipse([lx - 20, ly - 20, lx + 20, ly + 20], fill=LENS)
    d.ellipse([lx - 9, ly - 9, lx + 9, ly + 9], fill=INK)
    # A small status light on the body.
    sx, sy = rotated([(282, 216)], cx, cy, tilt)[0]
    d.ellipse([sx - 9, sy - 9, sx + 9, sy + 9], fill=(230, 57, 50, 255))
    grow = 1.22
    big = im.resize((int(S * grow), int(S * grow)), Image.LANCZOS)
    off = (int(S * grow) - S) // 2
    im = big.crop((off - 10, off, off - 10 + S, off + S))
    badge.alpha_composite(im)
    im = badge.resize((128, 128), Image.LANCZOS)
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    im.save(OUT)
    print('wrote', OUT)


if __name__ == '__main__':
    main()
