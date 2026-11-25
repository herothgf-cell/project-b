"""Side-by-side before/after crops (70 m square, north up) of -mapAudit overhead captures.

Captures are 1600x900 px covering W_M x H_M metres, centred on the junction.
Usage: python tools/map_review_before_after.py <before dir> <after dir> <out dir> <NNN-Junction> ...
Example name: 004-J2145_15970 (reads <dir>/004-J2145_15970-top.jpg). Game imagery only.
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

W_M = 2 * 75 * 0.5773502691896257 * 16 / 9  # camera 75 m up, vertical FOV 60, 16:9
CROP, PX = 70.0, 640
FONT = ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf', 24)


def crop(path):
    im = Image.open(path).convert('RGB'); h = CROP * im.width / W_M / 2
    return im.crop((int(im.width / 2 - h), int(im.height / 2 - h), int(im.width / 2 + h), int(im.height / 2 + h))).resize((PX, PX))


def main(before, after, out, names, labels=('전', '후')):
    out = Path(out); out.mkdir(parents=True, exist_ok=True)
    for name in names:
        sheet = Image.new('RGB', (PX * 2, PX + 40), 'white'); d = ImageDraw.Draw(sheet)
        for k, (label, folder) in enumerate(zip(labels, (before, after))):
            sheet.paste(crop(Path(folder) / f'{name}-top.jpg'), (PX * k, 40)); d.text((PX * k + 10, 6), f'{name}  {label}', font=FONT, fill='black')
        sheet.save(out / f'{name}.jpg', quality=85)


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4:])
