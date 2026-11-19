"""Render motivation plan mockups (docs/content-design 28-34).

Usage: python -I tools/render_motivation_mockups.py
Outputs PNGs into docs/content-design/images/motivation-*.png.
Mockups are wireframes for planning, not final art.
"""
import json
import math
import os
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "docs", "content-design", "images")
MAP = os.path.join(ROOT, "config", "maps", "project", "eungye.project.json")
FONT = "C:/Windows/Fonts/malgun.ttf"
FONT_B = "C:/Windows/Fonts/malgunbd.ttf"

INK = (28, 40, 56)
SUB = (92, 106, 122)
BG = (247, 249, 251)
PANEL = (255, 255, 255)
LINE = (214, 222, 230)
ACCENT = (232, 120, 30)
TEAL = (20, 120, 110)
BLUE = (40, 70, 130)
YELLOW = (250, 190, 20)


def font(size, bold=False):
    return ImageFont.truetype(FONT_B if bold else FONT, size)


def canvas(w, h, title, subtitle):
    img = Image.new("RGB", (w, h), BG)
    d = ImageDraw.Draw(img)
    d.text((32, 22), title, font=font(30, True), fill=INK)
    d.text((32, 64), subtitle, font=font(17), fill=SUB)
    return img, d


def box(d, xy, fill=PANEL, outline=LINE, r=14, width=2):
    d.rounded_rectangle(xy, r, fill=fill, outline=outline, width=width)


def save(img, name):
    path = os.path.join(OUT, name)
    img.save(path, optimize=True)
    print(path)


# ---------------------------------------------------------------- 34 map
def home_map():
    data = json.load(open(MAP, encoding="utf-8"))
    b = data["baseline"]
    W, D = b["width"], b["depth"]
    s = 0.62
    ox, oy = 32, 100
    mw, mh = int(W * s), int(D * s)
    img, d = canvas(mw + 420, mh + 150, "기사 숙소 거점 위치 (시안)",
                    "은계 프로젝트 지도 r47 · 북쪽 위 · 기존 '영업 시작·마감 주차장' 자리를 숙소 거점으로 지정")
    d.rectangle((ox, oy, ox + mw, oy + mh), fill=(236, 242, 241), outline=LINE)

    def P(x, z):
        return (ox + x * s, oy + (D - z) * s)

    for a in b["areas"]:
        pts = [P(p["x"], p["z"]) for p in a["points"]]
        if len(pts) > 2:
            d.polygon(pts, fill=(184, 218, 230))
    for bl in b["buildings"]:
        pts = [P(p["x"], p["z"]) for p in bl["points"]]
        if len(pts) > 2:
            d.polygon(pts, fill=(222, 228, 232))
    for r in b["roads"]:
        pts = [P(p["x"], p["z"]) for p in r["points"]]
        if len(pts) > 1:
            d.line(pts, fill=(255, 255, 255), width=max(2, int(r.get("width", 6) * s)), joint="curve")

    for i, st in enumerate(b["stops"]):
        x, y = P(st["x"], st["z"])
        d.ellipse((x - 5, y - 5, x + 5, y + 5), fill=BLUE)
        if i:
            d.text((x + 8, y - 10), "S%02d" % (i + 1), font=font(13, True), fill=BLUE)

    services = {}
    for o in data["document"]["scene"]["objects"]:
        if o.get("kind") in ("parking", "restaurant", "rest"):
            services[o["kind"]] = (o["serviceX"], o["serviceZ"], o["name"])
    for kind, label, col in (("restaurant", "기사 식당", TEAL), ("rest", "동부 휴식 구역", TEAL)):
        if kind in services:
            x, y = P(services[kind][0], services[kind][1])
            d.rounded_rectangle((x - 9, y - 9, x + 9, y + 9), 4, fill=col)
            d.text((x + 13, y - 12), label, font=font(15, True), fill=col)

    hx, hy = P(services["parking"][0], services["parking"][1])
    d.ellipse((hx - 46, hy - 46, hx + 46, hy + 46), outline=ACCENT, width=3)
    d.polygon([(hx, hy - 22), (hx - 20, hy - 4), (hx + 20, hy - 4)], fill=ACCENT)
    d.rectangle((hx - 14, hy - 4, hx + 14, hy + 16), fill=ACCENT)
    d.rectangle((hx - 4, hy + 4, hx + 4, hy + 16), fill=(255, 255, 255))
    d.rounded_rectangle((hx + 52, hy - 34, hx + 290, hy + 20), 8, fill=PANEL, outline=ACCENT, width=2)
    d.text((hx + 62, hy - 30), "기사 숙소 (거점)", font=font(18, True), fill=ACCENT)
    d.text((hx + 62, hy - 6), "수인로 뒤 원룸 · 1층 전용 주차 칸", font=font(13), fill=SUB)

    d.line((ox + mw - 140, oy + mh - 24, ox + mw - 140 + 100 * s, oy + mh - 24), fill=INK, width=3)
    d.text((ox + mw - 140, oy + mh - 50), "100 m", font=font(14), fill=INK)
    d.text((ox + mw - 40, oy + 12), "N ↑", font=font(18, True), fill=INK)

    lx = ox + mw + 30
    d.text((lx, oy), "범례", font=font(22, True), fill=INK)
    rows = [(ACCENT, "기사 숙소 거점", "하루 시작·퇴근·방·차고"),
            (TEAL, "식당 · 휴식 구역", "식사·짧은 휴식 (퇴근 불가로 변경)"),
            (BLUE, "S01–S12 기본 승하차", "S01이 숙소 바로 앞")]
    y = oy + 44
    for col, a, bb in rows:
        d.rounded_rectangle((lx, y + 4, lx + 18, y + 22), 4, fill=col)
        d.text((lx + 30, y), a, font=font(17, True), fill=INK)
        d.text((lx + 30, y + 26), bb, font=font(14), fill=SUB)
        y += 70
    y += 10
    d.text((lx, y), "거점에서 하는 일", font=font(20, True), fill=INK)
    for t in ["아침: 창밖 보고 하루 조건 확인", "출발: 주차 칸에서 빈차등 켜기", "퇴근: 숙소 주차 칸에 정차 → F",
              "밤: 방 꾸미기 · 목표 메모 · 수첩", "차고: 아버지 택시 관리·교체"]:
        y += 34
        d.text((lx, y), "· " + t, font=font(15), fill=INK)
    save(img, "motivation-home-base-map.png")


# ---------------------------------------------------------------- 33 intro
def intro():
    img, d = canvas(1260, 520, "첫 실행 3컷 인트로 (시안)", "약 26초 · 건너뛰기 가능 · 컷3이 끝나면 바로 운전석에서 빈차등 켜기")
    caps = [("1 · 8초", "회사가 문을 닫았다."), ("2 · 10초", "아버지가 남긴 건, 이 차 하나."), ("3 · 8초", "오늘부터, 내 핸들로.")]
    for i, (n, c) in enumerate(caps):
        x = 32 + i * 410
        box(d, (x, 110, x + 390, 400), fill=(40, 48, 62), outline=(40, 48, 62))
        if i == 0:
            d.rectangle((x + 40, 270, x + 350, 290), fill=(120, 96, 70))
            d.rectangle((x + 140, 200, x + 260, 270), fill=(196, 160, 110))
            d.line((x + 140, 200, x + 200, 180, x + 260, 200), fill=(170, 136, 90), width=4)
            d.rectangle((x + 180, 215, x + 220, 245), fill=(240, 240, 240))
            d.text((x + 186, 220), "ID", font=font(14, True), fill=BLUE)
            d.rectangle((x + 60, 150, x + 120, 268), fill=(70, 80, 96))
        elif i == 1:
            d.rounded_rectangle((x + 60, 220, x + 330, 300), 18, fill=YELLOW)
            d.rounded_rectangle((x + 110, 180, x + 280, 230), 16, fill=(230, 170, 10))
            d.rectangle((x + 175, 166, x + 215, 182), fill=(250, 250, 250))
            d.text((x + 180, 164), "개인", font=font(12, True), fill=INK)
            for cx in (x + 110, x + 280):
                d.ellipse((cx - 22, 280, cx + 22, 324), fill=(30, 30, 30))
            d.rectangle((x + 300, 130, x + 360, 200), fill=(236, 230, 214), outline=(150, 120, 80), width=4)
            d.ellipse((x + 318, 142, x + 342, 166), fill=(150, 130, 110))
            d.rectangle((x + 314, 168, x + 346, 194), fill=(150, 130, 110))
            d.text((x + 292, 206), "아버지 사진", font=font(12), fill=(220, 220, 220))
        else:
            d.polygon([(x + 195, 170), (x + 20, 330), (x + 370, 330)], fill=(120, 128, 140))
            d.line((x + 195, 175, x + 195, 330), fill=(255, 255, 255), width=3)
            d.rectangle((x + 10, 330, x + 380, 392), fill=(28, 30, 36))
            d.arc((x + 130, 300, x + 260, 420), 200, 340, fill=(90, 90, 96), width=10)
            d.rounded_rectangle((x + 290, 340, x + 370, 372), 6, fill=(220, 40, 40))
            d.text((x + 306, 343), "빈차", font=font(16, True), fill=(255, 255, 255))
            d.rectangle((x + 10, 112, x + 380, 160), fill=(250, 200, 140))
            d.text((x + 14, 118), n, font=font(15, True), fill=INK)
        d.text((x + 14, 118), n, font=font(15, True), fill=(230, 230, 230))
        d.text((x + 10, 414), c, font=font(22, True), fill=INK)
    d.text((32, 470), "그림은 배치 시안. 실제 컷은 방 일러스트와 같은 톤의 한 장 그림 3장. 아버지 얼굴은 사진 액자로만 보인다.", font=font(15), fill=SUB)
    save(img, "motivation-intro-storyboard.png")


def notebook():
    img, d = canvas(1200, 640, "아버지의 운행 수첩 (시안)", "글러브박스에서 발견 · 기사 수첩(30)의 첫 장 · 페이지는 플레이로 하나씩 열림")
    box(d, (60, 110, 1140, 600), fill=(250, 244, 228), outline=(180, 150, 100), width=3)
    d.line((600, 120, 600, 590), fill=(200, 176, 130), width=3)
    d.text((100, 135), "p.1  아침 장사", font=font(22, True), fill=(110, 70, 30))
    lines = ["아침엔 아파트 단지 앞이 제일 바쁘다.", "수인로는 8시 반 넘으면 막힌다.", "첫 손님한테는 먼저 인사."]
    for i, t in enumerate(lines):
        y = 190 + i * 56
        d.line((100, y + 36, 560, y + 36), fill=(214, 196, 160), width=1)
        d.text((104, y), t, font=font(21), fill=(60, 50, 40))
    d.text((104, 380), "— 1994. 3. 은계", font=font(17), fill=(120, 100, 80))
    d.rounded_rectangle((104, 440, 560, 560), 10, fill=(255, 252, 240), outline=(200, 176, 130))
    d.text((120, 452), "게임 효과", font=font(16, True), fill=TEAL)
    d.text((120, 482), "아침 주거 지역 수요 힌트 표시", font=font(16), fill=INK)
    d.text((120, 510), "해금: 1일차 첫 운송 완료", font=font(16), fill=SUB)
    d.text((640, 135), "잠긴 페이지", font=font(22, True), fill=(110, 70, 30))
    locks = [("p.2 비 오는 날", "동네 ★ 3곳"), ("p.3 금요일 밤", "첫 금요일 카드"), ("p.4 다시 타고 싶은 차", "무위반 하루"),
             ("p.5 다음 사람에게", "목표 50%")]
    for i, (a, bb) in enumerate(locks):
        y = 190 + i * 92
        d.rounded_rectangle((640, y, 1100, y + 74), 10, fill=(242, 234, 214), outline=(214, 196, 160))
        d.text((664, y + 10), "[잠김] " + a, font=font(19, True), fill=(120, 100, 80))
        d.text((664, y + 42), "열림 조건: " + bb, font=font(15), fill=SUB)
    save(img, "motivation-father-notebook.png")


# ---------------------------------------------------------------- 32 cabin
def cabin():
    img, d = canvas(1200, 640, "차내 장치 조작 3종 (시안)", "운전 화면 하단 · 단축키 3개 · 처음 3일은 자동 보조 켬")
    d.rectangle((32, 100, 1168, 420), fill=(150, 160, 172))
    d.polygon([(600, 150), (200, 420), (1000, 420)], fill=(118, 124, 134))
    d.line((600, 155, 600, 420), fill=(255, 255, 255), width=4)
    d.rectangle((32, 420, 1168, 610), fill=(30, 32, 38))
    # meter
    box(d, (80, 450, 420, 590), fill=(16, 18, 22), outline=(80, 200, 120), width=3)
    d.text((100, 460), "미터기  [J]", font=font(16, True), fill=(80, 200, 120))
    d.text((100, 492), "주행 중", font=font(18), fill=(200, 230, 210))
    d.text((210, 486), "₩ 6,300", font=font(40, True), fill=(120, 255, 160))
    d.text((100, 552), "탑승 후 켜기 · 하차 시 끄기 → 정산", font=font(14), fill=(160, 180, 170))
    # roof sign
    box(d, (450, 450, 760, 590), fill=(16, 18, 22), outline=(220, 60, 60), width=3)
    d.text((470, 460), "빈차등  [K]", font=font(16, True), fill=(240, 120, 120))
    for i, (t, col, on) in enumerate((("빈차", (220, 40, 40), True), ("예약", (60, 120, 220), False), ("휴무", (90, 90, 96), False))):
        x = 470 + i * 94
        d.rounded_rectangle((x, 500, x + 82, 540), 8, fill=col if on else (50, 52, 58), outline=col, width=2)
        d.text((x + 20, 506), t, font=font(18, True), fill=(255, 255, 255) if on else (150, 150, 150))
    d.text((470, 552), "휴무 = 호출 안 받음(휴식·시설 이동)", font=font(14), fill=(180, 170, 170))
    # aircon
    box(d, (790, 450, 1120, 590), fill=(16, 18, 22), outline=(80, 160, 230), width=3)
    d.text((810, 460), "에어컨  [L]", font=font(16, True), fill=(120, 190, 250))
    for i, t in enumerate(("따뜻", "끔", "시원")):
        x = 810 + i * 100
        on = t == "시원"
        d.ellipse((x + 10, 494, x + 62, 546), fill=(40, 110, 180) if on else (50, 52, 58), outline=(80, 160, 230), width=2)
        d.text((x + 18, 506), t, font=font(16, True), fill=(255, 255, 255) if on else (150, 150, 150))
    d.text((810, 552), "온도 민감 승객 판정과 연결", font=font(14), fill=(160, 180, 200))
    # passenger bubble
    box(d, (680, 130, 1120, 210), fill=PANEL, outline=LINE)
    d.text((700, 142), "손님", font=font(15, True), fill=TEAL)
    d.text((700, 166), "좀 덥네요… 에어컨 켜 주실 수 있어요?", font=font(19), fill=INK)
    save(img, "motivation-cabin-controls.png")


# ---------------------------------------------------------------- 28 goal
def goal():
    img, d = canvas(1260, 600, "목표 카드 · 정산 게이지 · 벽 메모 (시안)", "계속 영업 모드: 준비 화면 / 정산 화면 / 첫날 밤 숙소")
    box(d, (32, 110, 420, 560))
    d.text((52, 124), "준비 화면 · 목표 카드", font=font(17, True), fill=SUB)
    d.text((52, 170), "매일 타고 싶은 차", font=font(24, True), fill=INK)
    d.text((52, 206), "하이브리드 세단", font=font(18), fill=SUB)
    d.rounded_rectangle((52, 250, 400, 278), 10, fill=(236, 240, 244))
    d.rounded_rectangle((52, 250, 52 + 348 * .52, 278), 10, fill=ACCENT)
    d.text((52, 290), "₩13,600 / ₩26,000 · 52%", font=font(17, True), fill=INK)
    d.text((52, 322), "오늘 평균이면 약 3일", font=font(17), fill=SUB)
    d.rounded_rectangle((52, 380, 400, 540), 10, fill=(252, 246, 236), outline=(240, 210, 170))
    d.text((68, 392), "4일차 · 비 오는 날", font=font(16, True), fill=ACCENT)
    d.text((68, 422), "오후부터 비. 전 지역 손님↑", font=font(16), fill=INK)
    d.text((68, 450), "탑승 청결 -2", font=font(16), fill=SUB)

    box(d, (440, 110, 840, 560))
    d.text((460, 124), "정산 화면 · 목표 진척", font=font(17, True), fill=SUB)
    d.text((460, 170), "오늘 순수익  +₩4,100", font=font(24, True), fill=TEAL)
    for i, (lbl, v) in enumerate((("어제", .52), ("오늘", .68))):
        y = 230 + i * 60
        d.text((460, y), lbl, font=font(16), fill=SUB)
        d.rounded_rectangle((520, y, 820, y + 26), 10, fill=(236, 240, 244))
        d.rounded_rectangle((520, y, 520 + 300 * v, y + 26), 10, fill=ACCENT if i else (240, 190, 140))
        d.text((520 + 300 * v - 46, y + 30), "%d%%" % (v * 100), font=font(14, True), fill=INK)
    d.text((460, 370), "오늘의 기록", font=font(17, True), fill=INK)
    for i, t in enumerate(("최장 운송 3.2km", "무위반 연속 2일", "이야기 '첫 면접' · 좋은 결말")):
        d.text((460, 402 + i * 32), "· " + t, font=font(16), fill=INK)

    box(d, (860, 110, 1228, 560), fill=(238, 232, 222))
    d.text((880, 124), "첫날 밤 · 숙소 벽 메모", font=font(17, True), fill=SUB)
    memos = [("매일 타고 싶은 차", (255, 236, 150), -4), ("이 집을 내 집답게", (200, 236, 220), 3), ("아직 모르겠다", (230, 230, 240), -2)]
    for i, (t, col, rot) in enumerate(memos):
        m = Image.new("RGBA", (300, 96), col + (255,))
        md = ImageDraw.Draw(m)
        md.text((20, 30), t, font=font(21, True), fill=INK)
        m = m.rotate(rot, expand=True, fillcolor=(0, 0, 0, 0))
        img.paste(m, (890, 170 + i * 120), m)
    d.text((880, 528), "하나를 벽에 붙임 · 언제든 교체", font=font(15), fill=SUB)
    save(img, "motivation-goal-card.png")


# ---------------------------------------------------------------- 29 day condition
def condition():
    img, d = canvas(1260, 560, "하루 조건 카드 (시안)", "준비 화면에 1장 · 정답을 알려주지 않고 단서만")
    cards = [("평일", "평범한 하루", (120, 130, 140), "기본 수요"), ("비 오는 날", "오후부터 비", (60, 120, 200), "전 지역 ×1.3 · 청결 -2"),
             ("상가 행사", "저녁 상가 행사", (200, 110, 40), "저녁 상업 출발 ×2"), ("금요일", "회식 많은 밤", (150, 60, 150), "저녁 상업→주거 ×2"),
             ("도로 공사", "일부 구간 통제", (210, 150, 20), "지정 구간 우회")]
    for i, (t, cue, col, eff) in enumerate(cards):
        x = 32 + i * 244
        box(d, (x, 110, x + 228, 470), outline=col, width=3)
        d.rounded_rectangle((x, 110, x + 228, 190), 14, fill=col)
        d.rectangle((x, 160, x + 228, 190), fill=col)
        d.text((x + 18, 128), t, font=font(26, True), fill=(255, 255, 255))
        d.text((x + 18, 210), cue, font=font(19, True), fill=INK)
        mx, my = x + 24, 260
        d.rounded_rectangle((mx, my, mx + 180, my + 130), 8, fill=(236, 242, 241))
        hot = {1: [(0, 0, 3, 3)], 2: [(2, 1, 3, 2)], 3: [(2, 1, 3, 2)], 4: [(1, 1, 2, 3)]}.get(i, [])
        for gx in range(3):
            for gy in range(3):
                d.rectangle((mx + 10 + gx * 56, my + 10 + gy * 38, mx + 60 + gx * 56, my + 42 + gy * 38), outline=(255, 255, 255), width=3)
        for (a, b_, c, e) in hot:
            d.rectangle((mx + 10 + a * 56, my + 10 + b_ * 38, mx + 4 + c * 56, my + 4 + e * 38), fill=col + (0,) if False else col)
        d.text((x + 18, 404), eff, font=font(15), fill=SUB)
    d.text((32, 500), "색칠 = 수요가 몰리는 지역(시안). 공사 카드는 지도에 통제 구간 표시. 4일 주기 안에 같은 카드 반복 없음.", font=font(15), fill=SUB)
    save(img, "motivation-day-condition-card.png")


# ---------------------------------------------------------------- 30 logbook
def logbook():
    img, d = canvas(1200, 640, "기사 수첩 (시안)", "아버지의 페이지 뒤로 내 기록이 이어짐 · 탭 5개")
    box(d, (32, 100, 1168, 610))
    tabs = ["아버지", "이야기", "손님", "동네", "기록"]
    for i, t in enumerate(tabs):
        x = 52 + i * 140
        on = t == "이야기"
        d.rounded_rectangle((x, 116, x + 128, 156), 10, fill=ACCENT if on else (236, 240, 244))
        d.text((x + 30, 122), t, font=font(18, True), fill=(255, 255, 255) if on else SUB)
    stories = ["첫 면접", "아이 열 나는 보호자", "이사 첫날", "야간 근무 끝", "막차 놓친 학생", "결혼식 하객", "화분 선물", "회식 후 귀가", "여행 출발", "속상한 하루"]
    for i, s_ in enumerate(stories):
        col, row = i % 5, i // 5
        x, y = 52 + col * 222, 180 + row * 200
        box(d, (x, y, x + 206, y + 180))
        d.text((x + 14, y + 12), s_, font=font(17, True), fill=INK)
        seen = [(True, True, False), (True, False, False), (False, False, False), (True, False, True), (False, False, False),
                (False, False, False), (True, False, False), (True, True, True), (False, False, False), (True, False, False)][i]
        for k, (lbl, c) in enumerate((("좋은", TEAL), ("보통", (150, 150, 150)), ("아쉬운", (190, 80, 70)))):
            yy = y + 56 + k * 38
            d.rounded_rectangle((x + 14, yy, x + 192, yy + 30), 8, fill=c if seen[k] else (240, 242, 245))
            d.text((x + 24, yy + 3), lbl + " 결말" if seen[k] else "?", font=font(15, True), fill=(255, 255, 255) if seen[k] else (170, 170, 170))
    d.text((52, 584), "결말 30칸 중 11칸 · 같은 이야기를 다른 운전으로 다시 볼 이유", font=font(15), fill=SUB)
    save(img, "motivation-logbook.png")


# ---------------------------------------------------------------- 31 day/night
def daynight():
    img, d = canvas(1260, 560, "낮밤 4단계 · 라디오 자막 (시안)", "영업 08:00–20:00 · 조명만으로 시간 흐름 표현")
    stages = [("아침 08–10", (255, 214, 170), (250, 240, 220), False), ("낮 10–17", (170, 210, 245), (240, 246, 250), False),
              ("노을 17–19", (250, 150, 90), (120, 90, 110), True), ("밤 19–", (30, 40, 80), (40, 44, 60), True)]
    for i, (t, sky, ground, lights) in enumerate(stages):
        x = 32 + i * 304
        for yy in range(110, 300):
            k = (yy - 110) / 190
            c = tuple(int(sky[j] * (1 - k * .3)) for j in range(3))
            d.line((x, yy, x + 290, yy), fill=c)
        d.rectangle((x, 300, x + 290, 400), fill=ground)
        for bx in range(5):
            bxl = x + 12 + bx * 56
            d.rectangle((bxl, 200 + (bx % 2) * 30, bxl + 44, 300), fill=(90, 96, 110) if lights else (200, 206, 214))
            if lights:
                for wy in range(214 + (bx % 2) * 30, 290, 18):
                    d.rectangle((bxl + 8, wy, bxl + 16, wy + 8), fill=(255, 220, 120))
                    d.rectangle((bxl + 26, wy, bxl + 34, wy + 8), fill=(255, 220, 120) if wy % 36 else (70, 74, 90))
        d.polygon([(x + 145, 300), (x + 40, 400), (x + 250, 400)], fill=(110, 116, 126) if not lights else (50, 54, 66))
        if lights:
            for lx in (x + 60, x + 230):
                d.line((lx, 330, lx, 390), fill=(160, 160, 160), width=3)
                d.ellipse((lx - 18, 380, lx + 18, 400), fill=(255, 230, 150))
        d.text((x + 6, 410), t, font=font(20, True), fill=INK)
    box(d, (132, 460, 1128, 520), fill=(20, 24, 32), outline=(20, 24, 32))
    d.text((152, 468), "은계 FM", font=font(16, True), fill=(250, 200, 120))
    d.text((290, 472), "은계 아침입니다. 오늘은 오후부터 비 소식이 있습니다.", font=font(20), fill=(240, 240, 240))
    save(img, "motivation-day-night-radio.png")


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    only = set(sys.argv[1:])
    for fn in (home_map, intro, notebook, cabin, goal, condition, logbook, daynight):
        if not only or fn.__name__ in only:
            fn()
