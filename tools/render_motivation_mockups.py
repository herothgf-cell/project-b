"""Render motivation plan mockups (docs/content-design 28-34) in the current game UI tone.

Usage: python -I tools/render_motivation_mockups.py [name ...]
Outputs docs/content-design/images/motivation-*.png.

Each mockup overlays new elements on a real game screenshot (docs/ui/..., artifacts/...)
using the CasualUI tokens (unity/EungyeTaxi/Assets/Scripts/CasualUI.cs) and the game font
Noto Sans KR. New elements are outlined with a dashed orange frame so reviewers can tell
them apart from the shipped UI. Wireframe for planning, not final art.
"""
import json
import os
import sys

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "docs", "content-design", "images")
MAP = os.path.join(ROOT, "config", "maps", "project", "eungye.project.json")
FONTS = os.path.join(ROOT, "unity", "EungyeTaxi", "Assets", "Resources", "Fonts")
SHOT = {
    "drive": "docs/ui/2026-11-10-vehicle-purchase/04-day3-driving-new-car.png",
    "cockpit": "docs/ui/2026-11-07-taxi-exterior-cockpit/implemented-taxi-cockpit-1280.png",
    "prep": "docs/content-design/images/source-single-preparation.png",
    "summary": "docs/ui/2026-11-06-full-ui/07-single-day-summary.png",
    "room": "docs/ui/2026-11-12-room-items/03-second-day-room.png",
    "garage": "docs/ui/2026-11-10-vehicle-purchase/02-garage-quote-day2.png",
    "roombase": "unity/EungyeTaxi/Assets/Resources/personal-room-base.png",
}


def rgb(r, g, b):
    return (int(r * 255), int(g * 255), int(b * 255))


# CasualUI.cs tokens
INK = rgb(.075, .15, .29)
MUTED = rgb(.30, .39, .51)
CANVAS = rgb(.88, .94, .99)
CARD = (255, 255, 255)
SOFT = rgb(.93, .97, 1)
TRACK = rgb(.80, .86, .93)
BLUE = rgb(.025, .34, .78)
GREEN = rgb(.015, .43, .23)
DANGER = rgb(.76, .12, .17)
AMBER = rgb(.62, .34, .025)
BORDER = rgb(.77, .85, .94)
WARM = rgb(1, .97, .85)
NEW = (240, 120, 20)
WHITE = (255, 255, 255)


def font(size, bold=True):
    name = "NotoSansKR-Bold.ttf" if bold else "NotoSansKR-Medium.ttf"
    return ImageFont.truetype(os.path.join(FONTS, name), size)


def shot(key, size=None):
    img = Image.open(os.path.join(ROOT, SHOT[key])).convert("RGB")
    return img.resize(size, Image.LANCZOS) if size else img


def panel(d, r, fill=CARD, radius=12):
    x0, y0, x1, y1 = r
    d.rounded_rectangle((x0, y0 + 4, x1, y1 + 4), radius, fill=(205, 214, 226))
    d.rounded_rectangle((x0 - 1, y0 - 1, x1 + 1, y1 + 1), radius, fill=BORDER)
    d.rounded_rectangle(r, radius, fill=fill)


def header(d, r, title, color=BLUE, size=22):
    d.rounded_rectangle(r, 10, fill=color)
    cy = (r[1] + r[3]) / 2
    d.ellipse((r[0] + 14, cy - 11, r[0] + 36, cy + 11), outline=WHITE, width=3)
    d.text((r[0] + 50, cy), title, font=font(size), fill=WHITE, anchor="lm")


def button(d, r, text, primary=False, size=18, color=None):
    d.rounded_rectangle(r, 10, fill=color or (GREEN if primary else SOFT))
    d.text(((r[0] + r[2]) / 2, (r[1] + r[3]) / 2), text, font=font(size), fill=WHITE if primary or color else INK, anchor="mm")


def bar(d, r, frac, color=GREEN):
    d.rounded_rectangle(r, (r[3] - r[1]) // 2, fill=TRACK)
    if frac > 0:
        d.rounded_rectangle((r[0], r[1], r[0] + (r[2] - r[0]) * frac, r[3]), (r[3] - r[1]) // 2, fill=color)


def mark_new(d, r, label="신규", pad=6, at=None):
    x0, y0, x1, y1 = r[0] - pad, r[1] - pad, r[2] + pad, r[3] + pad
    dash = 10
    for x in range(int(x0), int(x1), dash * 2):
        d.line((x, y0, min(x + dash, x1), y0), fill=NEW, width=3)
        d.line((x, y1, min(x + dash, x1), y1), fill=NEW, width=3)
    for y in range(int(y0), int(y1), dash * 2):
        d.line((x0, y, x0, min(y + dash, y1)), fill=NEW, width=3)
        d.line((x1, y, x1, min(y + dash, y1)), fill=NEW, width=3)
    w = d.textlength(label, font=font(14)) + 14
    lx, top = at if at else (x0, y0 - 24 if y0 >= 24 else y0)
    d.rounded_rectangle((lx, top, lx + w, top + 24), 6, fill=NEW)
    d.text((lx + 7, top + 12), label, font=font(14), fill=WHITE, anchor="lm")


def frame(img, title, notes):
    """Put a screenshot into a captioned sheet in the game's canvas colour."""
    w, h = img.size
    sheet = Image.new("RGB", (w + 48, h + 150), CANVAS)
    d = ImageDraw.Draw(sheet)
    d.text((24, 18), title, font=font(28), fill=INK)
    d.text((24, 58), "주황 점선 = 새로 추가할 요소 · 나머지는 현재 게임 화면", font=font(16, False), fill=MUTED)
    sheet.paste(img, (24, 96))
    d.rounded_rectangle((23, 95, 24 + w, 96 + h), 4, outline=BORDER, width=2)
    d.text((24, h + 112), notes, font=font(16, False), fill=MUTED)
    return sheet


def save(img, name):
    path = os.path.join(OUT, name)
    img.save(path, optimize=True)
    print(path)


def wrap_text(d, xy, text, f, fill, width, gap=6):
    x, y = xy
    line = ""
    for ch in text:
        if d.textlength(line + ch, font=f) > width:
            d.text((x, y), line, font=f, fill=fill)
            y += f.size + gap
            line = ch
        else:
            line += ch
    if line:
        d.text((x, y), line, font=f, fill=fill)
    return y + f.size + gap


# ---------------------------------------------------------------- 32 cabin controls (cockpit)
def cabin():
    img = shot("cockpit")
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((1030, 568, 1246, 626), 8, fill=CARD)
    wrap_text(d, (1040, 572), "좀 덥네요… 에어컨 켜 주실 수 있어요?", font(15), INK, 190, 2)
    r = (650, 610, 880, 700)
    panel(d, r)
    d.text((664, 618), "미터기 · J", font=font(13), fill=MUTED)
    d.text((664, 636), "6,300원", font=font(24), fill=GREEN)
    d.rounded_rectangle((664, 674, 740, 694), 9, fill=GREEN)
    d.text((702, 684), "주행 중", font=font(12), fill=WHITE, anchor="mm")
    d.text((770, 618), "빈차등 · K", font=font(13), fill=MUTED)
    for i, (t, on) in enumerate((("빈차", False), ("예약", True), ("휴무", False))):
        x = 770 + i * 34
        d.rounded_rectangle((x, 640, x + 30, 660), 6, fill=BLUE if on else SOFT)
        d.text((x + 15, 650), t, font=font(11), fill=WHITE if on else MUTED, anchor="mm")
    d.text((770, 668), "에어컨 · L", font=font(13), fill=MUTED)
    d.rounded_rectangle((838, 670, 872, 692), 6, fill=BLUE)
    d.text((855, 681), "시원", font=font(11), fill=WHITE, anchor="mm")
    mark_new(d, r, "신규 · 차내 조작")
    rs = (548, 152, 732, 178)
    d.rounded_rectangle(rs, 8, fill=DANGER)
    d.text(((rs[0] + rs[2]) / 2, 165), "지붕 표시등 · 예약", font=font(14), fill=WHITE, anchor="mm")
    mark_new(d, rs, "신규")
    save(frame(img, "차내 장치 조작 3종 — 운전석 시점 시안",
               "속도 계기 오른쪽에 미터기·빈차등·에어컨을 묶고, 룸미러 아래에 지붕 표시등 상태를 띄운다. 단축키 J·K·L(현재 미사용)."),
         "motivation-cabin-controls.png")


# ---------------------------------------------------------------- 28/29 preparation: goal + condition
def goal():
    img = shot("prep")
    d = ImageDraw.Draw(img)
    c = (40, 156, 380, 470)
    panel(d, c)
    header(d, (46, 162, 374, 202), "오늘의 은계 · 4일차", color=BLUE, size=20)
    d.rounded_rectangle((60, 216, 360, 300), 10, fill=SOFT)
    d.text((76, 226), "비 오는 날", font=font(24), fill=BLUE)
    d.text((76, 264), "오후부터 비 소식", font=font(16, False), fill=INK)
    d.text((60, 314), "전 지역 손님이 늘어요", font=font(16, False), fill=INK)
    d.text((60, 340), "탑승할 때 차가 조금 더러워져요", font=font(16, False), fill=INK)
    d.text((60, 384), "아버지 수첩 · \"비 오는 날엔", font=font(14, False), fill=MUTED)
    d.text((60, 404), "우산 싸맬 봉투를 챙겨.\"", font=font(14, False), fill=MUTED)
    mark_new(d, c, "신규 · 하루 조건 카드(29)")
    g = (900, 156, 1240, 470)
    panel(d, g)
    header(d, (906, 162, 1234, 202), "내 목표", color=GREEN, size=20)
    d.text((920, 216), "아버지 차를, 내 차로", font=font(20), fill=INK)
    d.text((920, 248), "하이브리드 세단", font=font(16, False), fill=MUTED)
    bar(d, (920, 284, 1220, 304), .52, GREEN)
    d.text((920, 312), "13,600 / 26,000원 · 52%", font=font(16), fill=INK)
    d.text((920, 340), "오늘 평균이면 약 3일", font=font(16, False), fill=MUTED)
    d.rounded_rectangle((920, 384, 1220, 450), 10, fill=WARM)
    d.text((934, 392), "숙소 벽 메모", font=font(14), fill=AMBER)
    d.text((934, 418), "언제든 숙소에서 바꿀 수 있어요", font=font(14, False), fill=INK)
    mark_new(d, g, "신규 · 목표 카드(28)")
    save(frame(img, "영업 준비 — 하루 조건 카드 · 목표 카드 시안",
               "기존 '영업 준비' 패널은 그대로 두고 좌우에 카드 2장. 조건 카드는 단서만, 정답·예상 수입은 보여주지 않는다."),
         "motivation-goal-card.png")


def settlement():
    img = shot("summary", (1280, 720))
    d = ImageDraw.Draw(img)
    d.rectangle((410, 398, 870, 512), fill=CARD)
    d.text((418, 400), "목표 · 아버지 차를, 내 차로", font=font(13), fill=MUTED)
    bar(d, (418, 422, 760, 436), .68, GREEN)
    d.rounded_rectangle((418, 422, 418 + 342 * .52, 436), 7, fill=(120, 180, 140))
    d.text((770, 429), "52 → 68%", font=font(14), fill=GREEN, anchor="lm")
    d.text((418, 442), "오늘의 기록 · 최장 운송 3.2km · 무위반 연속 2일", font=font(12, False), fill=INK)
    d.text((418, 462), "숙소 월세  -1,200원", font=font(12, False), fill=INK)
    d.rounded_rectangle((416, 484, 633, 508), 8, fill=SOFT)
    d.text((525, 496), "숙소", font=font(13), fill=INK, anchor="mm")
    mark_new(d, (412, 398, 866, 510), "신규 · 목표 게이지(28) · 월세 · '내 공간'→'숙소'(34)", at=(880, 440))
    save(frame(img, "영업 마감 — 목표 게이지 · 월세 · 숙소 버튼 시안",
               "기존 정산 카드 안에 어제→오늘 진척, 오늘의 기록 1줄, 숙소 월세 한 줄, '숙소' 버튼을 넣는다. 금액은 초안."),
         "motivation-settlement-goal.png")


# ---------------------------------------------------------------- 29 condition cards set
def condition():
    w, h = 1280, 560
    img = Image.new("RGB", (w, h), CANVAS)
    d = ImageDraw.Draw(img)
    cards = [("평일", "평범한 하루", MUTED, "기본 수요"), ("비 오는 날", "오후부터 비", BLUE, "전 지역 손님 늘어남, 탑승 때 차가 쉽게 더러워짐"),
             ("상가 행사", "저녁 상가 행사", AMBER, "저녁 상가 출발 손님 많음"), ("금요일", "회식 많은 밤", rgb(.45, .20, .55), "밤 상가→주거 손님 많음"),
             ("도로 공사", "일부 구간 통제", DANGER, "지도에 통제 구간 표시")]
    for i, (t, cue, col, eff) in enumerate(cards):
        x = 24 + i * 248
        panel(d, (x, 24, x + 232, 520))
        header(d, (x + 6, 30, x + 226, 72), t, color=col, size=20)
        d.text((x + 18, 88), cue, font=font(20), fill=INK)
        m = (x + 18, 136, x + 214, 300)
        d.rounded_rectangle(m, 10, fill=SOFT)
        for gx in range(3):
            for gy in range(3):
                d.rounded_rectangle((m[0] + 10 + gx * 62, m[1] + 10 + gy * 50, m[0] + 64 + gx * 62, m[1] + 52 + gy * 50), 6, fill=WHITE)
        hot = {1: [(0, 0, 3, 3)], 2: [(2, 1, 3, 2)], 3: [(2, 1, 3, 3)], 4: [(1, 0, 2, 3)]}.get(i, [])
        for (a, b, c2, e) in hot:
            d.rounded_rectangle((m[0] + 10 + a * 62, m[1] + 10 + b * 50, m[0] + 2 + c2 * 62, m[1] + 2 + e * 50), 6, fill=col)
        wrap_text(d, (x + 18, 318), eff, font(16, False), INK, 196)
        d.text((x + 18, 470), "준비 화면 · 정산 회고", font=font(13, False), fill=MUTED)
    mark_new(d, (24, 24, 1256, 524), "신규 · 하루 조건 카드(29)", at=(24, 528))
    save(frame(img, "하루 조건 카드 5종 — 게임 카드 스타일 시안",
               "흰 카드 + 색 헤더(CasualUI). 칸 색칠은 손님이 몰리는 지역 단서. 4일 주기 안에서 같은 카드 반복 없음."),
         "motivation-day-condition-card.png")


# ---------------------------------------------------------------- 33/30 room-layout screens
def notebook_shell(title, active_tab):
    img = shot("room")
    d = ImageDraw.Draw(img)
    d.rectangle((50, 100, 1115, 900), fill=CANVAS)
    d.rounded_rectangle((69, 19, 1105, 87), 12, fill=BLUE)
    d.ellipse((92, 38, 122, 68), outline=WHITE, width=4)
    d.text((136, 53), title, font=font(28), fill=WHITE, anchor="lm")
    for i, t in enumerate(("숙소", "차고", "수첩 · 보는 중")):
        x = 640 + i * 155
        button(d, (x, 28, x + 145, 78), t, primary=(i == 2), size=18)
    d.rectangle((1145, 30, 1520, 880), fill=CARD)
    d.rounded_rectangle((1144, 31, 1519, 91), 12, fill=BLUE)
    d.text((1210, 61), "수첩 탭", font=font(26), fill=WHITE, anchor="lm")
    for i, t in enumerate(("아버지 · 1/5", "이야기 · 11/30", "손님 · 9/30", "동네 · ★ 4", "기록 · 3/12")):
        y = 120 + i * 74
        button(d, (1156, y, 1506, y + 60), t, primary=(i == active_tab), size=20)
    button(d, (1156, 744, 1506, 794), "숙소로 돌아가기", size=20)
    button(d, (1156, 810, 1506, 858), "다음 날 준비", primary=True, size=22)
    return img, d


def notebook():
    img, d = notebook_shell("기사 수첩 · 아버지", 0)
    nb = (80, 110, 1100, 700)
    d.rounded_rectangle(nb, 14, fill=(250, 244, 226), outline=(214, 190, 140), width=3)
    d.line((590, 126, 590, 684), fill=(214, 190, 140), width=3)
    d.text((112, 136), "p.1  아침 장사", font=font(26), fill=(110, 70, 30))
    for i, t in enumerate(("아침엔 아파트 단지 앞이 제일 바쁘다.", "수인로는 8시 반 넘으면 막힌다.", "첫 손님한테는 먼저 인사.")):
        y = 200 + i * 64
        d.line((112, y + 44, 560, y + 44), fill=(222, 204, 166), width=2)
        d.text((116, y), t, font=font(23, False), fill=(70, 55, 40))
    d.text((116, 410), "— 1994. 3. 은계", font=font(18, False), fill=(130, 105, 80))
    d.rounded_rectangle((112, 470, 560, 670), 12, fill=CARD, outline=BORDER, width=2)
    d.text((132, 488), "도움이 되는 점", font=font(18), fill=GREEN)
    d.text((132, 524), "아침 주거 지역 손님 힌트", font=font(18, False), fill=INK)
    d.text((132, 560), "열림 · 1일차 첫 운송 완료", font=font(16, False), fill=MUTED)
    d.text((620, 136), "아직 열리지 않은 페이지", font=font(22), fill=(110, 70, 30))
    for i, (a, b) in enumerate((("p.2 비 오는 날", "동네 ★ 3곳"), ("p.3 금요일 밤", "첫 금요일"), ("p.4 다시 타고 싶은 차", "무위반 하루"), ("p.5 다음 사람에게", "목표 50%"))):
        y = 190 + i * 118
        d.rounded_rectangle((620, y, 1068, y + 100), 12, fill=(242, 234, 214), outline=(222, 204, 166), width=2)
        d.text((644, y + 16), a, font=font(20), fill=(130, 105, 80))
        d.text((644, y + 58), "열림 조건 · " + b, font=font(16, False), fill=MUTED)
    d.text((82, 740), "아버지가 30년 몰던 차의 글러브박스에서 나온 수첩.", font=font(22, False), fill=INK)
    d.text((82, 790), "뒤쪽 탭에 내 이야기·손님·동네·기록이 이어진다.", font=font(18, False), fill=MUTED)
    mark_new(d, (70, 20, 1520, 880), "신규 · 수첩 화면(30·33) — 방/차고와 같은 배치")
    save(frame(img.resize((1280, 720), Image.LANCZOS), "기사 수첩 — 아버지 탭 시안",
               "방·차고 화면과 같은 틀(파란 헤더 + 탭 + 오른쪽 패널). 수첩 종이만 따뜻한 색으로 구분한다."),
         "motivation-father-notebook.png")


def logbook():
    img, d = notebook_shell("기사 수첩 · 이야기", 1)
    stories = ["첫 면접", "아이 열 나는 보호자", "이사 첫날", "야간 근무 끝", "막차 놓친 학생", "결혼식 하객", "화분 선물", "회식 후 귀가", "여행 출발", "속상한 하루"]
    seen = [(1, 1, 0), (1, 0, 0), (0, 0, 0), (1, 0, 1), (0, 0, 0), (0, 0, 0), (1, 0, 0), (1, 1, 1), (0, 0, 0), (1, 0, 0)]
    for i, s_ in enumerate(stories):
        cx, cy = 70 + (i % 5) * 208, 112 + (i // 5) * 300
        panel(d, (cx, cy, cx + 196, cy + 284))
        d.text((cx + 14, cy + 12), s_, font=font(18), fill=INK)
        for k, (lbl, col) in enumerate((("좋은 결말", GREEN), ("보통 결말", MUTED), ("아쉬운 결말", DANGER))):
            y = cy + 64 + k * 66
            on = seen[i][k]
            d.rounded_rectangle((cx + 14, y, cx + 182, y + 52), 10, fill=col if on else SOFT)
            d.text((cx + 98, y + 26), lbl if on else "?", font=font(16), fill=WHITE if on else TRACK, anchor="mm")
    d.text((82, 730), "결말 30칸 중 11칸", font=font(24), fill=INK)
    d.text((82, 776), "같은 손님도 내 운전에 따라 결말이 달라진다.", font=font(18, False), fill=MUTED)
    mark_new(d, (70, 20, 1520, 880), "신규 · 수첩 이야기 탭(30)")
    save(frame(img.resize((1280, 720), Image.LANCZOS), "기사 수첩 — 이야기 탭 시안",
               "이야기 10편 × 결말 3종. 본 결말만 색으로 채워진다. 탭 구성은 아버지 · 이야기 · 손님 · 동네 · 기록."),
         "motivation-logbook.png")


def wall_memo():
    img = shot("room")
    d = ImageDraw.Draw(img)
    d.rectangle((130, 24, 740, 84), fill=BLUE)
    d.text((136, 53), "영업을 마치고, 숙소", font=font(28), fill=WHITE, anchor="lm")
    mark_new(d, (130, 30, 420, 76), "'내 공간' → '숙소'")
    memos = [("아버지 차를, 내 차로", (255, 236, 150), -4, (250, 150)), ("이 집을 내 집답게", (200, 236, 220), 3, (480, 128)), ("일단, 오늘을 버티자", (226, 232, 246), -2, (710, 152))]
    for t, col, rot, (x, y) in memos:
        m = Image.new("RGBA", (214, 76), col + (255,))
        md = ImageDraw.Draw(m)
        md.text((107, 38), t, font=font(19), fill=INK, anchor="mm")
        m = m.rotate(rot, expand=True, resample=Image.BICUBIC, fillcolor=(0, 0, 0, 0))
        img.paste(m, (x, y), m)
    mark_new(d, (246, 124, 932, 240), "신규 · 벽 메모(33)")
    d.rectangle((600, 392, 640, 432), fill=(236, 228, 210), outline=(140, 110, 80), width=4)
    d.ellipse((612, 398, 628, 414), fill=(150, 130, 110))
    d.rectangle((609, 414, 631, 430), fill=(150, 130, 110))
    mark_new(d, (598, 390, 642, 434), "아버지 사진")
    d.rounded_rectangle((1144, 31, 1519, 91), 12, fill=BLUE)
    d.text((1210, 61), "목표 고르기", font=font(26), fill=WHITE, anchor="lm")
    d.rectangle((1146, 100, 1516, 688), fill=CARD)
    d.text((1156, 108), "무엇을 위해 달릴까?", font=font(28), fill=INK)
    d.text((1156, 156), "벽에 붙일 메모 한 장을 고르세요.", font=font(17, False), fill=MUTED)
    d.text((1156, 182), "언제든 바꿀 수 있어요.", font=font(17, False), fill=MUTED)
    for i, (t, sub) in enumerate((("아버지 차를, 내 차로", "꾸미기 · 새 차 저축"), ("이 집을 내 집답게", "소품 · 위층 넓은 호실"), ("일단, 오늘을 버티자", "목표 없이 · 3일차에 다시"))):
        y = 230 + i * 120
        d.rounded_rectangle((1156, y, 1506, y + 100), 12, fill=(226, 244, 232) if i == 0 else SOFT, outline=GREEN if i == 0 else BORDER, width=3 if i == 0 else 1)
        d.text((1176, y + 16), t, font=font(21), fill=INK)
        d.text((1176, y + 58), sub, font=font(16, False), fill=MUTED)
    d.text((1156, 600), "숙소 월세 -1,200원", font=font(16, False), fill=INK)
    d.text((1156, 628), "오늘 순수익 +5,460원", font=font(16, False), fill=INK)
    mark_new(d, (1146, 31, 1516, 660), "신규 · 첫날 밤 목표 선택(28·33)", at=(1146, 664))
    d.rectangle((60, 720, 1100, 860), fill=CANVAS)
    d.text((82, 745), "월세 빼고 이만큼. 내일도 나가야겠다.", font=font(24, False), fill=INK)
    d.text((82, 800), "개인 지출 0원 · 영업 순수익 5,460원 · 숙소 월세 1,200원", font=font(17, False), fill=MUTED)
    save(frame(img.resize((1280, 720), Image.LANCZOS), "첫날 밤 숙소 — 벽 메모 · 아버지 사진 시안",
               "현재 방 화면에 메모 3장과 책상 위 사진 액자를 더하고, 오른쪽 패널에서 목표를 고른다. 하단 문장은 첫 정산 독백."),
         "motivation-wall-memo.png")


# ---------------------------------------------------------------- 33 father car keep (garage)
def father_car():
    base = shot("garage")
    d = ImageDraw.Draw(base)
    d.rectangle((84, 116, 400, 162), fill=CARD)
    d.text((86, 138), "아버지의 택시 · 내 차", font=font(28), fill=INK, anchor="lm")
    ov = Image.new("RGBA", base.size, (19, 38, 74, 110))
    img = Image.alpha_composite(base.convert("RGBA"), ov).convert("RGB")
    d = ImageDraw.Draw(img)
    mark_new(d, (84, 116, 400, 162), "이름 변경")
    m = (420, 230, 1180, 690)
    panel(d, m)
    header(d, (428, 238, 1172, 292), "아버지의 택시는 숙소 주차장에 둘게요", color=BLUE, size=24)
    car = shot("garage").crop((100, 175, 375, 305)).resize((220, 104), Image.LANCZOS)
    img.paste(car, (452, 318))
    d.text((700, 314), "판매하지 않고 보관합니다", font=font(22), fill=INK)
    d.text((700, 354), "보상 판매가 없어 새 차 값을 모두 냅니다", font=font(17, False), fill=MUTED)
    d.text((700, 382), "하이브리드 세단  26,000원", font=font(18), fill=INK)
    d.text((700, 410), "남는 현금  2,000원", font=font(18), fill=INK)
    d.rounded_rectangle((452, 452, 1148, 560), 10, fill=WARM)
    d.text((472, 462), "보관한 차는", font=font(16), fill=AMBER)
    d.text((472, 490), "· 숙소 주차장에 늘 서 있어요", font=font(17, False), fill=INK)
    d.text((472, 518), "· 차고에서 다시 고르면 다음 날부터 그 차로 영업", font=font(17, False), fill=INK)
    button(d, (452, 590, 790, 650), "이 차로 바꾸기", primary=True, size=22)
    button(d, (810, 590, 1148, 650), "다시 생각하기", size=20)
    mark_new(d, m, "신규 · 교체 확인 창(33)")
    save(frame(img.resize((1280, 720), Image.LANCZOS), "차고 — 아버지 차 보관 확인 시안",
               "사용자 결정: 아버지 차는 팔지 않고 숙소 주차장에 보관. 시작 차 보상 판매(-10,000원)는 적용하지 않는다."),
         "motivation-father-car-keep.png")


# ---------------------------------------------------------------- 33 intro
def intro():
    W, H = 400, 225
    sheet = Image.new("RGB", (1280, 470), CANVAS)
    d = ImageDraw.Draw(sheet)
    d.text((24, 18), "첫 실행 3컷 인트로 — 게임 화면 톤 시안", font=font(28), fill=INK)
    d.text((24, 58), "약 26초 · 건너뛰기 가능 · 숙소 원룸과 운전석 화면으로 구성(최종 일러스트는 방 그림 톤)", font=font(16, False), fill=MUTED)
    base = Image.open(os.path.join(ROOT, SHOT["roombase"])).convert("RGB").resize((W, H), Image.LANCZOS)
    c1 = ImageEnhance.Brightness(base).enhance(.55)
    c1d = ImageDraw.Draw(c1)
    c1d.rectangle((150, 140, 250, 200), fill=(196, 160, 110), outline=(150, 120, 80), width=2)
    c1d.rectangle((176, 150, 224, 176), fill=(245, 245, 245))
    c1d.text((200, 163), "사원증", font=font(11), fill=BLUE, anchor="mm")
    car = shot("garage").crop((100, 175, 375, 305))
    c2 = Image.new("RGB", (W, H), (233, 196, 150))
    c2d = ImageDraw.Draw(c2)
    c2d.rectangle((0, 150, W, H), fill=(120, 124, 132))
    c2d.rectangle((0, 40, 120, 150), fill=(214, 200, 182))
    c2d.text((60, 60), "기사 숙소", font=font(14), fill=INK, anchor="mm")
    c2.paste(car.resize((240, 114), Image.LANCZOS), (130, 70))
    c2d.rectangle((316, 40, 372, 104), fill=(236, 228, 210), outline=(140, 110, 80), width=4)
    c2d.ellipse((334, 50, 354, 70), fill=(150, 130, 110))
    c2d.rectangle((330, 70, 358, 98), fill=(150, 130, 110))
    c3 = shot("cockpit").crop((180, 120, 1100, 638)).resize((W, H), Image.LANCZOS).filter(ImageFilter.GaussianBlur(.6))
    c3d = ImageDraw.Draw(c3)
    c3d.rounded_rectangle((300, 192, 386, 216), 6, fill=DANGER)
    c3d.text((343, 204), "빈차 · K", font=font(12), fill=WHITE, anchor="mm")
    caps = [("1 · 8초", "회사가 문을 닫았다.", c1), ("2 · 10초", "아버지가 남긴 건, 이 차 하나.", c2), ("3 · 8초", "오늘부터, 내 핸들로.", c3)]
    for i, (n, cap, im) in enumerate(caps):
        x = 24 + i * 420
        cell = Image.new("RGB", (W, H + 56), (12, 16, 24))
        cell.paste(im, (0, 0))
        ImageDraw.Draw(cell).text((W / 2, H + 28), cap, font=font(20), fill=WHITE, anchor="mm")
        sheet.paste(cell, (x, 100))
        d.rounded_rectangle((x + 10, 110, x + 84, 136), 8, fill=BLUE)
        d.text((x + 47, 123), n, font=font(13), fill=WHITE, anchor="mm")
    button(d, (1104, 400, 1252, 444), "건너뛰기 · Esc", size=16)
    d.text((24, 400), "아버지는 얼굴 없이 사진 액자로만. 컷3 이후 숙소 주차 칸 운전석에서 빈차등(K) → 첫 손님 미터기(J).", font=font(16, False), fill=MUTED)
    save(sheet, "motivation-intro-storyboard.png")


# ---------------------------------------------------------------- 31 day/night + radio
def daynight():
    base = shot("drive", (640, 360))
    light = base.convert("L").point(lambda v: 255 if v > 225 else 0).filter(ImageFilter.MaxFilter(9)).filter(ImageFilter.MinFilter(3))
    zones = Image.new("L", base.size, 0)
    zd = ImageDraw.Draw(zones)
    for z in ((0, 0, 168, 196), (535, 0, 640, 250), (0, 360, 226, 448), (272, 404, 414, 452), (528, 380, 640, 452)):
        zd.rectangle(z, fill=255)
    from PIL import ImageChops
    hud_mask = ImageChops.multiply(light, zones)
    grades = [("아침 08–10", (1.05, .95, .82), 1.0), ("낮 10–17", (1, 1, 1), 1.0), ("노을 17–19", (1.1, .78, .62), .82), ("밤 19–", (.55, .62, 1.0), .45)]
    sheet = Image.new("RGB", (1328, 900), CANVAS)
    d = ImageDraw.Draw(sheet)
    d.text((24, 18), "낮밤 4단계 · 라디오 자막 — 실제 운전 화면 색 보정 시안", font=font(28), fill=INK)
    d.text((24, 58), "조명 색·밝기만 바꾸고 HUD는 그대로. 노을·밤에는 가로등·건물 창·전조등을 켠다(점은 위치 표시).", font=font(16, False), fill=MUTED)
    for i, (t, mul, bright) in enumerate(grades):
        r_, g_, b_ = base.split()
        r_ = r_.point(lambda v, m=mul[0]: min(255, int(v * m)))
        g_ = g_.point(lambda v, m=mul[1]: min(255, int(v * m)))
        b_ = b_.point(lambda v, m=mul[2]: min(255, int(v * m)))
        im = ImageEnhance.Brightness(Image.merge("RGB", (r_, g_, b_))).enhance(bright)
        im.paste(base, (0, 0), hud_mask)
        id_ = ImageDraw.Draw(im)
        if bright < .9:
            for lx, ly in ((270, 140), (420, 150), (520, 118)):
                id_.ellipse((lx - 6, ly - 6, lx + 6, ly + 6), fill=(255, 226, 140))
            for wx in range(190, 300, 14):
                for wy in range(40, 120, 18):
                    if (wx + wy) % 3:
                        id_.rectangle((wx, wy, wx + 6, wy + 8), fill=(255, 214, 120))
        x, y = 24 + (i % 2) * 652, 100 + (i // 2) * 392
        sheet.paste(im, (x, y))
        d.rounded_rectangle((x + 180, y + 8, x + 330, y + 40), 8, fill=BLUE)
        d.text((x + 255, y + 24), t, font=font(16), fill=WHITE, anchor="mm")
        if i == 0:
            rr = (x + 150, y + 256, x + 600, y + 294)
            d.rounded_rectangle(rr, 10, fill=CARD, outline=BORDER)
            d.rounded_rectangle((rr[0] + 8, rr[1] + 6, rr[0] + 96, rr[3] - 6), 8, fill=AMBER)
            d.text((rr[0] + 52, (rr[1] + rr[3]) / 2), "은계 FM", font=font(14), fill=WHITE, anchor="mm")
            d.text((rr[0] + 106, (rr[1] + rr[3]) / 2), "은계 아침입니다. 오후부터 비 소식이 있습니다.", font=font(15, False), fill=INK, anchor="lm")
            mark_new(d, rr, "신규 · 라디오 자막")
    save(sheet, "motivation-day-night-radio.png")


# ---------------------------------------------------------------- 34 map
def home_map():
    data = json.load(open(MAP, encoding="utf-8"))
    b = data["baseline"]
    W, D = b["width"], b["depth"]
    s = 0.6
    ox, oy = 24, 100
    mw, mh = int(W * s), int(D * s)
    img = Image.new("RGB", (mw + 430, mh + 140), CANVAS)
    d = ImageDraw.Draw(img)
    d.text((24, 18), "기사 숙소 거점 위치 — 게임 지도 톤 시안", font=font(28), fill=INK)
    d.text((24, 58), "은계 정식 지도 r47 · 북쪽 위 · 기존 '영업 시작·마감 주차장' 자리를 숙소로 지정(지도 파일 변경 없음)", font=font(16, False), fill=MUTED)
    d.rounded_rectangle((ox - 2, oy - 2, ox + mw + 2, oy + mh + 2), 12, fill=BORDER)
    d.rounded_rectangle((ox, oy, ox + mw, oy + mh), 12, fill=(240, 244, 236))

    def P(x, z):
        return (ox + x * s, oy + (D - z) * s)

    for a in b["areas"]:
        pts = [P(p["x"], p["z"]) for p in a["points"]]
        if len(pts) > 2:
            d.polygon(pts, fill=(176, 214, 236))
    for bl in b["buildings"]:
        pts = [P(p["x"], p["z"]) for p in bl["points"]]
        if len(pts) > 2:
            d.polygon(pts, fill=(212, 222, 232))
    for r in b["roads"]:
        pts = [P(p["x"], p["z"]) for p in r["points"]]
        if len(pts) > 1:
            d.line(pts, fill=WHITE, width=max(2, int(r.get("width", 6) * s)), joint="curve")
    for i, st in enumerate(b["stops"]):
        x, y = P(st["x"], st["z"])
        if i:
            d.ellipse((x - 6, y - 6, x + 6, y + 6), fill=BLUE, outline=WHITE, width=2)
            d.text((x + 9, y), "S%02d" % (i + 1), font=font(13), fill=BLUE, anchor="lm")
    services = {}
    for o in data["document"]["scene"]["objects"]:
        if o.get("kind") in ("parking", "restaurant", "rest"):
            services[o["kind"]] = (o["serviceX"], o["serviceZ"])
    for kind, label in (("restaurant", "기사 식당"), ("rest", "휴식 구역 · 짧은 휴식만")):
        x, y = P(*services[kind])
        d.rounded_rectangle((x - 11, y - 11, x + 11, y + 11), 6, fill=GREEN, outline=WHITE, width=2)
        w = d.textlength(label, font=font(14)) + 16
        d.rounded_rectangle((x + 16, y - 14, x + 16 + w, y + 14), 8, fill=CARD, outline=BORDER)
        d.text((x + 24, y), label, font=font(14), fill=GREEN, anchor="lm")
    hx, hy = P(*services["parking"])
    d.ellipse((hx - 40, hy - 40, hx + 40, hy + 40), fill=(255, 236, 214), outline=NEW, width=3)
    d.polygon([(hx, hy - 22), (hx - 20, hy - 3), (hx + 20, hy - 3)], fill=NEW)
    d.rectangle((hx - 14, hy - 3, hx + 14, hy + 17), fill=NEW)
    d.rectangle((hx - 4, hy + 5, hx + 4, hy + 17), fill=WHITE)
    lab = (hx + 48, hy - 36, hx + 300, hy + 24)
    panel(d, lab)
    d.text((lab[0] + 14, lab[1] + 6), "기사 숙소", font=font(20), fill=NEW)
    d.text((lab[0] + 14, lab[1] + 36), "수인로 뒤 원룸 · 1층 주차 칸", font=font(13, False), fill=MUTED)
    d.text((ox + mw - 40, oy + 14), "N ↑", font=font(18), fill=INK)
    d.line((ox + mw - 150, oy + mh - 24, ox + mw - 150 + 100 * s, oy + mh - 24), fill=INK, width=3)
    d.text((ox + mw - 150, oy + mh - 50), "100 m", font=font(14), fill=INK)
    lx = ox + mw + 24
    panel(d, (lx, oy, lx + 380, oy + 600))
    header(d, (lx + 6, oy + 6, lx + 374, oy + 50), "생활거점 · F7", size=20)
    rows = [(NEW, "숙소", "하루 시작 · 퇴근 · 방 · 차고 · 수첩"), (GREEN, "기사 식당", "식사"), (GREEN, "휴식 구역", "짧은 휴식 (퇴근 불가로 변경)"), (BLUE, "S01–S12", "기본 승하차 · S01이 숙소 앞")]
    y = oy + 70
    for col, a, bb in rows:
        d.rounded_rectangle((lx + 18, y + 6, lx + 40, y + 28), 6, fill=col)
        d.text((lx + 54, y), a, font=font(18), fill=INK)
        d.text((lx + 54, y + 30), bb, font=font(14, False), fill=MUTED)
        y += 72
    d.text((lx + 18, y + 6), "하루 동선", font=font(18), fill=INK)
    for t in ("숙소 창밖 · 하루 조건 확인", "숙소 주차 칸 · 빈차등 켜기", "영업", "종료 후 귀갓길 손님", "숙소 주차 칸 정차 → F 퇴근"):
        y += 32
        d.text((lx + 18, y + 10), "· " + t, font=font(15, False), fill=INK)
    save(img, "motivation-home-base-map.png")


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    only = set(sys.argv[1:])
    for fn in (home_map, intro, notebook, logbook, wall_memo, father_car, cabin, goal, settlement, condition, daynight):
        if not only or fn.__name__ in only:
            fn()
