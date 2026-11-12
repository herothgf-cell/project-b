"""내 공간(방 꾸미기) 탁상 조명·러그 오버레이 생성.

배경 `personal-room-base.png`(1672x941)와 같은 크기의 투명 PNG를 만들어
PersonalRoomHUD가 배경과 같은 사각형에 그대로 겹쳐 그린다(좌표 정렬 불필요).
배경의 따뜻한 저녁 조명·낮은 채도·부드러운 음영에 맞춰 절차적으로 그린다.

  python tools/generate_room_items.py            # Resources에 생성
  python tools/generate_room_items.py --preview out_dir   # 합성 미리보기도 저장

출력(unity/EungyeTaxi/Assets/Resources/):
  room-lamp-0-on/off.png  나무 받침 + 리넨 갓
  room-lamp-1-on/off.png  짙은 철제 갓
  room-lamp-glow.png      켜짐 시 벽·책상에 번지는 빛
  room-rug-0.png          회청색 단색 러그(커튼·침구 색)
  room-rug-1.png          테라코타 줄무늬 러그(술 장식)
"""
from __future__ import annotations

import argparse
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parent.parent
RES = ROOT / "unity/EungyeTaxi/Assets/Resources"
W, H = 1672, 941
SS = 4  # supersampling for hand-drawn shapes

# Floor perspective: back-wall vanishing point of the backdrop.
VP = np.array([840.0, 420.0])
# Rug near edge (closest to viewer) and far edge depth.
RUG_NEAR_Y, RUG_FAR_Y = 905.0, 752.0
RUG_NEAR_X = (528.0, 1150.0)

# Lamp anchor: desk top surface (y 505-518) left of the chair.
LAMP_X, LAMP_Y = 712.0, 514.0


def rgba(c, a=255):
    return (int(c[0]), int(c[1]), int(c[2]), int(a))


# ---------------------------------------------------------------- rug

def rug_corners():
    def at(y, x_near):
        t = (y - VP[1]) / (RUG_NEAR_Y - VP[1])
        return VP[0] + (x_near - VP[0]) * t
    nl, nr = RUG_NEAR_X
    return np.array([
        [at(RUG_FAR_Y, nl), RUG_FAR_Y],   # far-left  (u=0,v=0)
        [at(RUG_FAR_Y, nr), RUG_FAR_Y],   # far-right (u=1,v=0)
        [nr, RUG_NEAR_Y],                 # near-right(u=1,v=1)
        [nl, RUG_NEAR_Y],                 # near-left (u=0,v=1)
    ])


def homography(src, dst):
    a = []
    for (x, y), (u, v) in zip(src, dst):
        a.append([x, y, 1, 0, 0, 0, -u * x, -u * y, -u])
        a.append([0, 0, 0, x, y, 1, -v * x, -v * y, -v])
    _, _, vt = np.linalg.svd(np.array(a))
    return vt[-1].reshape(3, 3) / vt[-1][-1]


def rug_pattern(u, v, style, rng_noise):
    """Return RGB float array and alpha mask for rug texture coordinates (u along width, v depth)."""
    # Real rug about 2.0m x 1.4m: fringe only on the short (left/right) ends for style 1.
    inside = (u >= 0) & (u <= 1) & (v >= 0) & (v <= 1)
    du = np.minimum(u, 1 - u)
    dv = np.minimum(v, 1 - v)
    edge = np.minimum(du * 2.0, dv * 1.4)  # roughly metric distance to the nearest edge
    if style == 0:
        base = np.array([116, 124, 140], float)       # curtain/bedding blue-grey
        border = np.array([90, 96, 112], float)
        inner = np.array([150, 154, 164], float)
        col = np.where((edge < 0.09)[..., None], border, base)
        line = (np.abs(edge - 0.13) < 0.012)
        col = np.where(line[..., None], inner, col)
    else:
        base = np.array([176, 104, 72], float)        # terracotta
        cream = np.array([214, 184, 144], float)
        deep = np.array([140, 76, 52], float)
        col = np.broadcast_to(base, u.shape + (3,)).copy()
        stripes = (np.abs(((u * 6.0) % 1.0) - 0.5) < 0.045) & (edge > 0.1)
        col = np.where(stripes[..., None], cream, col)
        col = np.where(((edge < 0.07) & (edge > 0.035))[..., None], cream, col)
        col = np.where((edge <= 0.035)[..., None], deep, col)
    # Woven texture + soft lighting from the window/ceiling (brighter toward the back).
    weave = 1.0 + 0.035 * np.sin(u * 900) * np.sin(v * 650) + rng_noise * 0.05
    light = 0.94 + 0.10 * (1 - v) + 0.04 * (1 - np.abs(u - 0.5) * 2)
    col = col * (weave * light)[..., None]
    alpha = inside.astype(float)
    if style == 1:
        # Fringe beyond the left/right short ends.
        fr = ((u < 0) & (u > -0.035) | (u > 1) & (u < 1.035)) & (v > 0.02) & (v < 0.98)
        strands = (np.abs(((v * 60.0) % 1.0) - 0.5) < 0.28)
        fringe = fr & strands
        col = np.where(fringe[..., None], np.array([214, 190, 152], float), col)
        alpha = np.maximum(alpha, fringe.astype(float) * 0.95)
    return np.clip(col, 0, 255), alpha


def draw_rug(style):
    corners = rug_corners()
    hm = homography(corners, [(0, 0), (1, 0), (1, 1), (0, 1)])
    x0, y0 = np.floor(corners.min(0)).astype(int) - 40
    x1, y1 = np.ceil(corners.max(0)).astype(int) + 40
    y1 = min(y1, H - 6)
    s = 3
    ys, xs = np.mgrid[y0 * s:y1 * s, x0 * s:x1 * s].astype(float)
    px, py = (xs + 0.5) / s, (ys + 0.5) / s
    den = hm[2, 0] * px + hm[2, 1] * py + hm[2, 2]
    u = (hm[0, 0] * px + hm[0, 1] * py + hm[0, 2]) / den
    v = (hm[1, 0] * px + hm[1, 1] * py + hm[1, 2]) / den
    rng = np.random.default_rng(7 + style)
    noise = rng.standard_normal(u.shape)
    col, alpha = rug_pattern(u, v, style, noise)
    hh, ww = u.shape
    col = col.reshape(hh // s, s, ww // s, s, 3).mean((1, 3))
    alpha = alpha.reshape(hh // s, s, ww // s, s).mean((1, 3))
    layer = np.zeros((H, W, 4), float)
    # Contact shadow: blurred, slightly offset toward viewer.
    sh = Image.fromarray((alpha * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(5))
    sh_full = np.zeros((H, W), float)
    sh_full[y0 + 4:y1 + 4, x0:x1] = np.asarray(sh, float) / 255.0 * 0.35
    layer[..., 3] = sh_full
    layer[..., :3] = np.array([60, 32, 15], float)
    # Composite rug over its shadow.
    a = np.zeros((H, W), float)
    c = np.zeros((H, W, 3), float)
    a[y0:y1, x0:x1] = alpha
    c[y0:y1, x0:x1] = col
    out_a = a + layer[..., 3] * (1 - a)
    out_c = (c * a[..., None] + layer[..., :3] * layer[..., 3:4] * (1 - a[..., None])) / np.maximum(out_a, 1e-6)[..., None]
    img = np.dstack([np.clip(out_c, 0, 255), np.clip(out_a * 255, 0, 255)]).astype(np.uint8)
    return Image.fromarray(img, "RGBA")


# ---------------------------------------------------------------- lamp

def vgrad(draw_img, box, top, bottom, mask):
    """Paint a vertical gradient into box through mask (all in supersampled coords)."""
    x0, y0, x1, y1 = [int(v) for v in box]
    h = max(1, y1 - y0)
    grad = np.zeros((h, x1 - x0, 4), np.uint8)
    t = np.linspace(0, 1, h)[:, None]
    for i in range(4):
        grad[..., i] = (top[i] + (bottom[i] - top[i]) * t).astype(np.uint8)
    g = Image.fromarray(grad, "RGBA")
    draw_img.paste(g, (x0, y0), mask.crop((x0, y0, x1, y1)))


def hgrad_shade(img, mask, box, dark=0.75, light=1.12, peak=0.35):
    """Multiply a horizontal cylinder-like shading into img inside mask."""
    x0, y0, x1, y1 = [int(v) for v in box]
    arr = np.asarray(img, float)
    m = np.asarray(mask, float)[y0:y1, x0:x1] / 255.0
    xs = np.linspace(0, 1, x1 - x0)
    f = np.where(xs < peak, dark + (light - dark) * xs / peak, light - (light - dark) * (xs - peak) / (1 - peak))
    region = arr[y0:y1, x0:x1, :3]
    shaded = region * f[None, :, None]
    arr[y0:y1, x0:x1, :3] = region * (1 - m[..., None]) + shaded * m[..., None]
    return Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), "RGBA")


def lamp_layer(style, on):
    S = SS
    cx, by = LAMP_X * S, LAMP_Y * S
    img = Image.new("RGBA", (W * S, H * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # contact shadow on desk
    d.ellipse((cx - 34 * S, by - 4 * S, cx + 40 * S, by + 4 * S), fill=(70, 38, 18, 90))
    if style == 0:
        # Wooden base: low turned disc + slim wooden stem.
        base_c, stem_c = (150, 102, 62), (176, 124, 78)
        d.ellipse((cx - 25 * S, by - 9 * S, cx + 25 * S, by + 3 * S), fill=rgba((120, 80, 46)))
        d.ellipse((cx - 25 * S, by - 13 * S, cx + 25 * S, by - 1 * S), fill=rgba(base_c))
        d.rounded_rectangle((cx - 4 * S, by - 78 * S, cx + 4 * S, by - 8 * S), radius=3 * S, fill=rgba(stem_c))
        stem = Image.new("L", img.size, 0)
        ImageDraw.Draw(stem).rounded_rectangle((cx - 4 * S, by - 78 * S, cx + 4 * S, by - 8 * S), radius=3 * S, fill=255)
        img = hgrad_shade(img, stem, (cx - 4 * S, by - 78 * S, cx + 4 * S, by - 8 * S), 0.7, 1.15, 0.4)
        # Linen drum/cone shade.
        top_w, bot_w, top_y, bot_y = 30, 46, by - 122 * S, by - 70 * S
        poly = [(cx - top_w * S, top_y), (cx + top_w * S, top_y), (cx + bot_w * S, bot_y), (cx - bot_w * S, bot_y)]
        mask = Image.new("L", img.size, 0)
        ImageDraw.Draw(mask).polygon(poly, fill=255)
        lit = on
        top = rgba((250, 232, 196) if lit else (214, 202, 180))
        bot = rgba((255, 222, 160) if lit else (196, 182, 160))
        vgrad(img, (cx - bot_w * S, top_y, cx + bot_w * S, bot_y), top, bot, mask)
        img = hgrad_shade(img, mask, (cx - bot_w * S, top_y, cx + bot_w * S, bot_y), 0.82 if lit else 0.74, 1.06, 0.38)
        d = ImageDraw.Draw(img)
        d.line([(cx - top_w * S, top_y), (cx + top_w * S, top_y)], fill=rgba((176, 160, 136)), width=2 * S)
        d.line([(cx - bot_w * S, bot_y), (cx + bot_w * S, bot_y)], fill=rgba((182, 160, 126) if not lit else (240, 196, 128)), width=2 * S)
        if lit:  # light spilling below the shade
            d.ellipse((cx - 30 * S, bot_y - 3 * S, cx + 30 * S, bot_y + 5 * S), fill=(255, 236, 190, 230))
    else:
        # Dark slate task lamp: round weighted base, angled arm, dome shade facing the desk.
        metal = (62, 70, 78)
        d.ellipse((cx - 24 * S, by - 8 * S, cx + 24 * S, by + 3 * S), fill=rgba((40, 45, 52)))
        d.ellipse((cx - 24 * S, by - 12 * S, cx + 24 * S, by - 1 * S), fill=rgba(metal))
        d.ellipse((cx - 14 * S, by - 10 * S, cx - 2 * S, by - 6 * S), fill=(120, 128, 136, 140))
        j1 = (cx - 6 * S, by - 8 * S)
        j2 = (cx - 22 * S, by - 82 * S)
        j3 = (cx + 26 * S, by - 112 * S)
        for a_, b_ in ((j1, j2), (j2, j3)):
            d.line([a_, b_], fill=rgba((52, 58, 66)), width=6 * S)
            d.line([(a_[0] + 2 * S, a_[1]), (b_[0] + 2 * S, b_[1])], fill=rgba((98, 106, 114)), width=2 * S)
        for j in (j1, j2):
            d.ellipse((j[0] - 5 * S, j[1] - 5 * S, j[0] + 5 * S, j[1] + 5 * S), fill=rgba((44, 50, 58)))
        # Dome shade pointing down-right toward the desk.
        sx, sy = j3[0] + 14 * S, j3[1] + 10 * S
        shade = [(sx - 30 * S, sy - 6 * S), (sx + 4 * S, sy - 24 * S), (sx + 34 * S, sy + 18 * S), (sx - 6 * S, sy + 30 * S)]
        mask = Image.new("L", img.size, 0)
        ImageDraw.Draw(mask).polygon(shade, fill=255)
        vgrad(img, (sx - 30 * S, sy - 24 * S, sx + 34 * S, sy + 30 * S), rgba((84, 94, 104)), rgba((50, 56, 64)), mask)
        img = hgrad_shade(img, mask, (sx - 30 * S, sy - 24 * S, sx + 34 * S, sy + 30 * S), 0.8, 1.2, 0.3)
        d = ImageDraw.Draw(img)
        d.ellipse((j3[0] - 5 * S, j3[1] - 5 * S, j3[0] + 5 * S, j3[1] + 5 * S), fill=rgba((44, 50, 58)))
        mouth = [(sx + 34 * S, sy + 18 * S), (sx - 6 * S, sy + 30 * S)]
        d.line(mouth, fill=rgba((255, 230, 170) if on else (36, 40, 46)), width=5 * S)
    return img.resize((W, H), Image.LANCZOS)


def lamp_glow(style):
    """Warm light: wall halo behind the shade + pool on the desk; alpha-blended over the backdrop."""
    ys, xs = np.mgrid[0:H, 0:W].astype(float)
    if style == 0:
        cx, cy = LAMP_X, LAMP_Y - 92
        halo = np.exp(-(((xs - cx) / 150) ** 2 + ((ys - cy) / 105) ** 2))
        pool = np.exp(-(((xs - cx - 10) / 120) ** 2 + ((ys - LAMP_Y - 6) / 14) ** 2))
    else:
        cx, cy = LAMP_X + 50, LAMP_Y - 80
        halo = np.exp(-(((xs - cx) / 120) ** 2 + ((ys - cy) / 85) ** 2)) * 0.7
        pool = np.exp(-(((xs - LAMP_X - 70) / 110) ** 2 + ((ys - LAMP_Y - 4) / 12) ** 2)) * 1.2
    a = np.clip(halo * 0.32 + pool * 0.40, 0, 0.55)
    col = np.dstack([np.full_like(a, 255), np.full_like(a, 206), np.full_like(a, 120)])
    return Image.fromarray(np.dstack([col, a * 255]).astype(np.uint8), "RGBA")


# ---------------------------------------------------------------- main

def outputs():
    yield "room-rug-0.png", draw_rug(0)
    yield "room-rug-1.png", draw_rug(1)
    for st in (0, 1):
        yield f"room-lamp-{st}-on.png", lamp_layer(st, True)
        yield f"room-lamp-{st}-off.png", lamp_layer(st, False)
        yield f"room-lamp-{st}-glow.png", lamp_glow(st)


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--preview", type=Path)
    args = p.parse_args()
    made = {}
    for name, img in outputs():
        img.save(RES / name, optimize=True)
        made[name] = img
        print("wrote", name)
    if args.preview:
        args.preview.mkdir(parents=True, exist_ok=True)
        base = Image.open(RES / "personal-room-base.png").convert("RGBA")
        for st in (0, 1):
            for on in (True, False):
                comp = base.copy()
                comp.alpha_composite(made[f"room-rug-{st}.png"])
                if on:
                    comp.alpha_composite(made[f"room-lamp-{st}-glow.png"])
                comp.alpha_composite(made[f"room-lamp-{st}-{'on' if on else 'off'}.png"])
                comp.convert("RGB").save(args.preview / f"preview-style{st}-{'on' if on else 'off'}.png")


if __name__ == "__main__":
    main()
