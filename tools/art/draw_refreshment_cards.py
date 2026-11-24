"""Draw the snack-shop menu pictures (kimbap, americano) for the facility menu cards.

2026-11-24 play-test round 4: the diner had picture cards, the kimbap shop and the cafe only a text row.
The diner pictures are approved generated art; these two are drawn here in a matching flat-illustration
style (soft shadow, white plate/cup, transparent background) at 272x237 like pork/stew/soup.
Usage: python tools/art/draw_refreshment_cards.py
"""
import math, os, random
from PIL import Image, ImageDraw, ImageFilter

OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'unity', 'EungyeTaxi', 'Assets', 'Resources', 'UI', 'Facilities')
W, H, S = 272, 237, 4


def canvas():
    return Image.new('RGBA', (W * S, H * S), (0, 0, 0, 0))


def shadow(img, box, blur=18, alpha=70):
    layer = Image.new('RGBA', img.size, (0, 0, 0, 0))
    ImageDraw.Draw(layer).ellipse(box, fill=(40, 30, 20, alpha))
    img.alpha_composite(layer.filter(ImageFilter.GaussianBlur(blur * S / 4)))


def finish(img, name):
    img = img.resize((W, H), Image.LANCZOS)
    img.save(os.path.join(OUT, name))
    print('wrote', name)


def kimbap():
    random.seed(7)
    img = canvas()
    cx, cy = W * S // 2, int(H * S * .58)
    shadow(img, (cx - 470, cy - 150, cx + 470, cy + 230))
    d = ImageDraw.Draw(img)
    # plate: rim and inner well
    d.ellipse((cx - 480, cy - 250, cx + 480, cy + 200), fill=(236, 238, 240))
    d.ellipse((cx - 455, cy - 232, cx + 455, cy + 180), fill=(252, 252, 252))
    d.ellipse((cx - 380, cy - 185, cx + 380, cy + 135), fill=(246, 246, 247))
    fills = [(255, 214, 40), (238, 120, 40), (70, 150, 60), (140, 90, 50), (240, 150, 160)]
    # slices, back row first so the front row overlaps it
    rows = [(-150, [-300, -100, 100, 300]), (10, [-390, -195, 0, 195, 390])]
    for dy, xs in rows:
        for dx in xs:
            x, y = cx + dx, cy + dy
            r = 112
            # side of the roll (seaweed edge seen below the cut face)
            d.ellipse((x - r, y - r * .72 + 34, x + r, y + r * .72 + 34), fill=(22, 34, 22))
            d.ellipse((x - r, y - r * .72, x + r, y + r * .72), fill=(30, 46, 28))
            ri = r - 16
            d.ellipse((x - ri, y - ri * .72, x + ri, y + ri * .72), fill=(250, 250, 246))
            for _ in range(70):  # rice grains
                a = random.random() * math.tau; rr = random.random() * ri * .92
                gx, gy = x + math.cos(a) * rr, y + math.sin(a) * rr * .72
                d.ellipse((gx - 7, gy - 4, gx + 7, gy + 4), fill=(232, 232, 226))
            # fillings in the middle
            k = 0
            for fx, fy in [(-28, -14), (10, -20), (34, 8), (-6, 16), (-36, 14)]:
                c = fills[k % len(fills)]; k += 1
                d.rounded_rectangle((x + fx - 17, y + fy - 12, x + fx + 17, y + fy + 12), radius=6, fill=c)
            for _ in range(8):  # sesame
                a = random.random() * math.tau; rr = random.random() * ri * .85
                gx, gy = x + math.cos(a) * rr, y + math.sin(a) * rr * .72
                d.ellipse((gx - 5, gy - 3, gx + 5, gy + 3), fill=(244, 226, 170))
            d.arc((x - r, y - r * .72, x + r, y + r * .72), 200, 330, fill=(80, 110, 70), width=6)
    finish(img, 'kimbap.png')


def coffee():
    img = canvas()
    cx, cy = W * S // 2 - 30, int(H * S * .62)
    shadow(img, (cx - 400, cy + 40, cx + 420, cy + 250))
    d = ImageDraw.Draw(img)
    # saucer
    d.ellipse((cx - 400, cy + 10, cx + 400, cy + 230), fill=(230, 232, 236))
    d.ellipse((cx - 360, cy + 30, cx + 360, cy + 200), fill=(250, 250, 251))
    d.ellipse((cx - 230, cy + 70, cx + 230, cy + 165), fill=(236, 237, 240))
    # handle
    d.ellipse((cx + 190, cy - 230, cx + 370, cy - 30), fill=(244, 244, 246))
    d.ellipse((cx + 235, cy - 185, cx + 325, cy - 75), fill=(0, 0, 0, 0))
    # cup body (tapered) and rim
    body = [(cx - 260, cy - 330), (cx + 260, cy - 330), (cx + 205, cy + 110), (cx - 205, cy + 110)]
    d.polygon(body, fill=(252, 252, 253))
    d.ellipse((cx - 205, cy + 60, cx + 205, cy + 160), fill=(252, 252, 253))
    d.polygon([(cx + 140, cy - 330), (cx + 260, cy - 330), (cx + 205, cy + 110), (cx + 110, cy + 125)], fill=(232, 234, 238))
    d.ellipse((cx - 262, cy - 400, cx + 262, cy - 260), fill=(240, 241, 244))
    # coffee with crema ring and highlight
    d.ellipse((cx - 232, cy - 385, cx + 232, cy - 275), fill=(150, 96, 52))
    d.ellipse((cx - 200, cy - 372, cx + 200, cy - 288), fill=(74, 42, 22))
    d.ellipse((cx - 120, cy - 358, cx + 20, cy - 330), fill=(110, 66, 36))
    # cafe band on the cup
    d.polygon([(cx - 245, cy - 200), (cx + 245, cy - 200), (cx + 236, cy - 140), (cx - 236, cy - 140)], fill=(46, 140, 150))
    # steam
    st = Image.new('RGBA', img.size, (0, 0, 0, 0))
    sd = ImageDraw.Draw(st)
    for off in (-90, 20, 120):
        pts = [(cx + off + 40 * math.sin(t / 9), cy - 430 - t * 9) for t in range(0, 40)]
        sd.line(pts, fill=(255, 255, 255, 150), width=22, joint='curve')
    img.alpha_composite(st.filter(ImageFilter.GaussianBlur(6)))
    # spoon on the saucer
    d.rounded_rectangle((cx - 380, cy + 120, cx - 150, cy + 142), radius=10, fill=(190, 194, 200))
    d.ellipse((cx - 170, cy + 105, cx - 90, cy + 158), fill=(198, 202, 208))
    finish(img, 'coffee.png')


if __name__ == '__main__':
    kimbap()
    coffee()
