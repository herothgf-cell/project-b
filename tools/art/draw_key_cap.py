"""Draws the world-space key cap sprite for road-side action labels (play feedback round 7, 2026-11-26).

Output: unity/EungyeTaxi/Assets/Resources/UI/key-cap-f.png (128 x 128, transparent).
Same look as the HUD key caps (KeyHints.Cap): a light face, a dark outline with a thicker bottom edge,
and a bold dark letter.
Run: python tools/art/draw_key_cap.py
"""
import os
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, 'unity', 'EungyeTaxi', 'Assets', 'Resources', 'UI', 'key-cap-f.png')
FONT = os.path.join(ROOT, 'unity', 'EungyeTaxi', 'Assets', 'Resources', 'Fonts', 'NotoSansKR-Bold.ttf')
S = 512
EDGE = (40, 48, 62, 255)
FACE = (246, 248, 252, 255)
INK = (24, 30, 40, 255)


def main():
    im = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    pad = 18
    d.rounded_rectangle([pad, pad, S - pad, S - pad], radius=70, fill=EDGE)
    d.rounded_rectangle([pad + 22, pad + 22, S - pad - 22, S - pad - 64], radius=52, fill=FACE)
    font = ImageFont.truetype(FONT, 300)
    d.text((S / 2, (pad + 22 + S - pad - 64) / 2 - 8), 'F', font=font, fill=INK, anchor='mm')
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    im.resize((128, 128), Image.LANCZOS).save(OUT)
    print('wrote', OUT)


if __name__ == '__main__':
    main()
