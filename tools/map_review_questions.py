"""Make the 'confirm before fixing' sheet for review answers that disagree at the same spot.

Each question shows every disputed issue on the same zoomed crop of the game top view and of the
Kakao aerial, plus a locator on the zone map, the issue text, the current decision and the reviewer's
note. Output (Git-ignored, contains Kakao imagery):
  artifacts/map-review-20261124/questions/index.html and img/*.jpg
Usage: PYTHONPATH=artifacts/py314-deps python tools/map_review_questions.py <feedback.json>
"""
import base64
import html
import io
import json
import math
import re
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, str(Path(__file__).resolve().parent))
from kakao_aerial_compare import aerial, W_M, H_M  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT / 'artifacts/map-review-20261124'
OUT = BASE / 'questions'
CROP_W, CROP_H = 90.0, 50.6  # metres shown per panel (16:9)
PX = (1120, 630)
DEC = {'approve': ('승인', (30, 160, 60)), 'reject': ('반려', (220, 30, 30)), 'review': ('다시 검토', (230, 140, 0)), 'hold': ('보류', (120, 120, 120)), '': ('미검토', (90, 90, 200))}

Q = [
    dict(id='Q1', title='노란 테두리 회색 사각형 4곳: 모두 과속방지턱인가?',
         refs=['J029-2', 'J031-1', 'J034-1', 'J039-3'],
         why='네 카드가 서로 3~11 m 떨어진 같은 지점을 가리킵니다. 029·031은 "과속 방지턱임"으로 반려(게임 그대로), 034는 메모에 "방지턱"이라고 쓰셨지만 판정은 다시 검토, 039는 승인(황색 횡단보도로 바뀜)입니다.',
         options=['네 곳 모두 방지턱으로 둔다(039·034도 반려)', '039만 횡단보도로 바꾼다', '로드뷰로 확인하고 정한다']),
    dict(id='Q2', title='B4 지점: 횡단보도를 넣을까?',
         refs=['J041-1', 'J045-3'],
         why='약 4 m 떨어진 같은 지점입니다. 041-1은 다시 검토("1번이 어디인지 모르겠음"), 045-3은 반려("횡단보도 아님, 방지턱")입니다. 041의 1번이 바로 이 지점입니다.',
         options=['둘 다 반려(방지턱, 게임 그대로)', '횡단보도 추가', '로드뷰로 확인하고 정한다']),
    dict(id='Q3', title='주도로의 노란 표시: 과속 주의 표시인가, 횡단보도인가?',
         refs=['J045-2', 'J047-3'],
         why='약 11 m 거리입니다. 045-2는 반려("횡단보도가 아니고 과속 주의 구간 표시"), 047-3은 승인(황색 횡단보도와 고원식 횡단보도 추가)입니다.',
         options=['둘 다 과속 주의 표시(횡단보도 추가 안 함)', '047-3만 횡단보도 추가', '로드뷰로 확인하고 정한다'],
         extra='게임에 "과속 주의 구간" 노면 표시를 새로 그려 넣을지도 적어 주세요.'),
    dict(id='Q4', title='골목 입구의 적색 고원식 횡단보도',
         refs=['J066-4', 'J069-1'],
         why='같은 골목 입구(약 10 m)에 적색 고원식 횡단보도를 넣는 항목인데 066-4는 승인, 069-1은 반려입니다.',
         options=['넣는다', '넣지 않는다', '로드뷰로 확인하고 정한다']),
    dict(id='Q5', title='아래쪽 노란 표시: 횡단보도인가 방지턱인가?',
         refs=['J066-5', 'J069-2'],
         why='066-5는 승인(노란 횡단보도를 약 8 m 남쪽으로 옮김), 069-2는 다시 검토이고 메모는 "노란 횡단보도 아니고 과속 방지턱"입니다.',
         options=['방지턱: 옮기지 않고 그대로 둔다', '횡단보도: 8 m 남쪽으로 옮긴다', '로드뷰로 확인하고 정한다']),
    dict(id='Q6', title='아파트 입구 처리 규칙',
         refs=['J024-4', 'J025-4', 'J044-4', 'J075-2', 'J075-3'],
         why='모두 아파트 입구·단지 안인데 판정이 다릅니다. 024-4는 승인, 025-4·044-4·075-2·075-3은 반려입니다. 025 메모는 "아파트 입구임, 진입로처럼 하는 건 OK"입니다.',
         options=['공통 규칙: 중앙선 없는 짧은 진입로로만 정리, 넓히거나 횡단보도·연장 안 함', '카드별 판정 그대로', '기타(아래에 적기)']),
    dict(id='Q7', title='반려한 064-1 안의 흰 횡단보도',
         refs=['J064-1'],
         why='064-1은 "노란색은 방지턱"이라는 메모와 함께 반려하셨습니다. 그런데 이 항목에는 노란 표시 말고도 교차로 바로 동쪽의 흰색 횡단보도 추가가 들어 있습니다.',
         options=['흰 횡단보도만 넣는다', '넣지 않는다(전부 반려)', '로드뷰로 확인하고 정한다']),
    dict(id='Q8', title='022-1: "빨간 왼쪽 박스면 승인"',
         refs=['J022-1'],
         why='맞습니다. 1번은 왼쪽(서쪽) 접근로 폭을 약 35 m에서 27 m로 줄이는 항목입니다(아래 표시). 승인으로 처리할 예정입니다.',
         options=['승인', '다시 검토']),
    dict(id='Q9', title='"3번" 한 단어 메모의 뜻',
         refs=['J004-3', 'J007-3'],
         why='J004 메모 "3번"과 J007 메모 "3번, 차선별 도로 너비가 인게임 상이한 부분 체크"입니다. 3번은 004에서 남동쪽 차로 적색 포장, 007에서 북서쪽 진입 차로의 지그재그 서행선과 적색 포장입니다.',
         options=['로드뷰로 확인하고 고친다', '특히 신경 써서 그대로 고친다', '고치지 않는다']),
    dict(id='Q10', title='"4군데" = 모서리 교통섬 4개?',
         refs=['J008-4', 'J020-4'],
         why='J008 메모 "이 교차로의 교통성이 4군데 있음", J020 메모 "이 교차로에는 4군데 교차로가 있음"입니다. 네 모서리의 우회전 분리 교통섬 4개로 이해했습니다(표시 위치).',
         options=['맞다: 교통섬 4개를 넣는다', '다른 뜻(아래에 적기)']),
]


def font(n):
    return ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf', n)


def world_box(item, cells):
    xs, ys = [], []
    for c in cells:
        m = re.match(r'([A-H])([1-5])', c.strip().upper())
        if m:
            i, j = 'ABCDEFGH'.index(m.group(1)), int(m.group(2)) - 1
            xs += [i / 8, (i + 1) / 8]; ys += [j / 5, (j + 1) / 5]
    x0 = item['x'] + (min(xs) - .5) * W_M; x1 = item['x'] + (max(xs) - .5) * W_M
    z1 = item['z'] - (min(ys) - .5) * H_M; z0 = item['z'] - (max(ys) - .5) * H_M
    return x0, z0, x1, z1


def capture_crop(item, cx, cz):
    """Crop the game top capture of `item` to CROP_W x CROP_H around (cx, cz); shift inside the frame."""
    path = next((BASE / 'game').glob(f"{int(item['index']):03d}-*-top.jpg"))
    im = Image.open(path).convert('RGB')
    sx, sz = 1600 / W_M, 900 / H_M
    hw, hh = CROP_W / 2, CROP_H / 2
    cx = min(max(cx, item['x'] - W_M / 2 + hw), item['x'] + W_M / 2 - hw)
    cz = min(max(cz, item['z'] - H_M / 2 + hh), item['z'] + H_M / 2 - hh)
    u0, v0 = 800 + (cx - hw - item['x']) * sx, 450 - (cz + hh - item['z']) * sz
    return im.crop((round(u0), round(v0), round(u0 + CROP_W * sx), round(v0 + CROP_H * sz))).resize(PX, Image.LANCZOS), cx, cz


def draw_marks(im, cx, cz, marks):
    d = ImageDraw.Draw(im)
    sx, sz = PX[0] / CROP_W, PX[1] / CROP_H
    f = font(22)
    for label, colour, (x0, z0, x1, z1) in marks:
        u0, v0 = PX[0] / 2 + (x0 - cx) * sx, PX[1] / 2 - (z1 - cz) * sz
        u1, v1 = PX[0] / 2 + (x1 - cx) * sx, PX[1] / 2 - (z0 - cz) * sz
        d.rectangle([u0, v0, u1, v1], outline=(255, 255, 255), width=7)
        d.rectangle([u0, v0, u1, v1], outline=colour, width=4)
        tx, ty = max(4, min(u0 + 4, PX[0] - 220)), max(4, min(v0 + 4, PX[1] - 34))
        d.text((tx, ty), label, font=f, fill=colour, stroke_width=3, stroke_fill=(255, 255, 255))
    d.line([(PX[0] - 130, PX[1] - 24), (PX[0] - 130 + 10 * sx, PX[1] - 24)], fill=(255, 230, 0), width=5)
    d.text((PX[0] - 130, PX[1] - 56), '10 m · 북쪽 위', font=font(18), fill=(255, 255, 255), stroke_width=3, stroke_fill=(0, 0, 0))


def locator(points):
    im = Image.open(BASE / 'overview/zones.png').convert('RGB')
    d = ImageDraw.Draw(im)
    for x, z in points:
        X, Y = x * .8, 1400 - z * .8
        d.ellipse([X - 26, Y - 26, X + 26, Y + 26], outline=(255, 0, 160), width=8)
    mx = sum(p[0] for p in points) / len(points) * .8; my = 1400 - sum(p[1] for p in points) / len(points) * .8
    w, h = 760, 430
    l = min(max(mx - w / 2, 0), im.width - w); t = min(max(my - h / 2, 0), im.height - h)
    return im.crop((round(l), round(t), round(l + w), round(t + h)))


def jpg(im, q=84):
    b = io.BytesIO(); im.save(b, 'JPEG', quality=q); return b.getvalue()


def main(feedback_path):
    items = {i['id']: i for i in json.loads((BASE / 'sheet/items.json').read_text(encoding='utf-8'))}
    fb = {i['id']: i for i in json.loads(Path(feedback_path).read_text(encoding='utf-8'))['items']}
    (OUT / 'img').mkdir(parents=True, exist_ok=True)
    data = []
    for q in Q:
        refs = []
        for r in q['refs']:
            iid, n = r.split('-'); item = items[iid]; issue = item['issues'][int(n) - 1]
            dec = fb[iid]['issues'].get(n) or fb[iid]['status'] or ''
            box = world_box(item, issue['cells'])
            refs.append(dict(ref=r, item=item, issue=issue, dec=dec, note=fb[iid]['note'], box=box,
                             centre=((box[0] + box[2]) / 2, (box[1] + box[3]) / 2)))
        # group refs into panels (within 45 m of the group's first ref)
        panels = []
        for r in refs:
            for p in panels:
                if math.dist(p[0]['centre'], r['centre']) < 45:
                    p.append(r); break
            else:
                panels.append([r])
        out_panels = []
        for k, p in enumerate(panels):
            cx = sum(r['centre'][0] for r in p) / len(p); cz = sum(r['centre'][1] for r in p) / len(p)
            host = min((r['item'] for r in p), key=lambda it: math.dist((it['x'], it['z']), (cx, cz)))
            game, cx, cz = capture_crop(host, cx, cz)
            air, _ = aerial(cx, cz, CROP_W, CROP_H, PX)
            marks = [(f"{r['ref']} {DEC[r['dec']][0]}", DEC[r['dec']][1], r['box']) for r in p]
            draw_marks(game, cx, cz, marks); draw_marks(air, cx, cz, marks)
            name = f"{q['id']}-{k + 1}"
            (OUT / f'img/{name}-game.jpg').write_bytes(jpg(game)); (OUT / f'img/{name}-air.jpg').write_bytes(jpg(air))
            out_panels.append(dict(name=name, junctions=sorted({r['item']['junction'] for r in p}), roads=sorted({x for r in p for x in r['item'].get('roads', [])}),
                                   links=host['links'], x=round(cx), z=round(cz)))
        loc = locator([r['centre'] for r in refs]); (OUT / f"img/{q['id']}-map.jpg").write_bytes(jpg(loc))
        data.append(dict(id=q['id'], title=q['title'], why=q['why'], options=q['options'], extra=q.get('extra', ''), panels=out_panels,
                         refs=[dict(ref=r['ref'], junction=r['item']['junction'], zone=r['item']['zone'], dec=DEC[r['dec']][0], before=r['issue']['before'], after=r['issue']['after'], note=r['note']) for r in refs]))
    write_html(data)
    print(OUT / 'index.html')


def write_html(data):
    css = '''
:root{--bg:#f6f7f9;--fg:#1c2330;--muted:#5d6675;--card:#fff;--line:#d9dee6;--accent:#2457d6}
@media (prefers-color-scheme:dark){:root{--bg:#14171c;--fg:#e8ebf0;--muted:#9aa3b2;--card:#1d2128;--line:#323844;--accent:#7aa2ff}}
*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--fg);font:15px/1.55 "Malgun Gothic",system-ui,sans-serif}
header{position:sticky;top:0;z-index:5;background:var(--card);border-bottom:1px solid var(--line);padding:10px 16px;display:flex;gap:12px;align-items:center;flex-wrap:wrap}
header h1{font-size:18px;margin:0 auto 0 0}button{font:inherit;padding:7px 14px;border-radius:8px;border:1px solid var(--line);background:var(--card);color:var(--fg);cursor:pointer}
button.primary{background:var(--accent);color:#fff;border-color:var(--accent)}main{max-width:1240px;margin:0 auto;padding:16px}
.intro{color:var(--muted);margin:0 0 16px}article{background:var(--card);border:1px solid var(--line);border-radius:12px;padding:16px;margin:0 0 22px;scroll-margin-top:70px}
article h2{margin:0 0 6px;font-size:19px}.why{margin:0 0 12px}.loc{display:grid;grid-template-columns:320px 1fr;gap:14px;align-items:start;margin-bottom:10px}
.loc img{width:100%;border-radius:8px;border:1px solid var(--line)}.pair{display:grid;grid-template-columns:1fr 1fr;gap:10px;margin:8px 0}
.pair figure{margin:0}.pair img{width:100%;border-radius:8px;border:1px solid var(--line);cursor:zoom-in}figcaption{font-size:13px;color:var(--muted)}
table{border-collapse:collapse;width:100%;font-size:14px;margin:8px 0}td,th{border:1px solid var(--line);padding:6px 8px;vertical-align:top;text-align:left}
th{background:var(--bg);white-space:nowrap}.dec{font-weight:700;white-space:nowrap}.answer{border-top:1px dashed var(--line);margin-top:10px;padding-top:10px}
.answer label{display:block;margin:3px 0;cursor:pointer}textarea{width:100%;min-height:60px;font:inherit;padding:8px;border-radius:8px;border:1px solid var(--line);background:var(--bg);color:var(--fg)}
.links a{margin-right:12px;color:var(--accent)}.done{color:#1e9a46;font-weight:700}
#zoom{position:fixed;inset:0;background:rgba(0,0,0,.85);display:none;align-items:center;justify-content:center;z-index:9}#zoom img{max-width:96vw;max-height:94vh}
@media (max-width:760px){.loc,.pair{grid-template-columns:1fr}}
'''
    js = '''
const KEY='eungye-map-review-questions-20261125';let state={};try{state=JSON.parse(localStorage.getItem(KEY)||'{}')}catch(e){}
function save(){try{localStorage.setItem(KEY,JSON.stringify(state))}catch(e){}count()}
function count(){const n=DATA.filter(q=>state[q.id]&&(state[q.id].choice||state[q.id].text)).length;document.getElementById('cnt').textContent=`답한 질문 ${n} / ${DATA.length}`;DATA.forEach(q=>{const b=document.getElementById('st-'+q.id);if(b)b.textContent=state[q.id]&&(state[q.id].choice||state[q.id].text)?'답함':''})}
document.querySelectorAll('input[type=radio]').forEach(r=>{const s=state[r.name];if(s&&s.choice===r.value)r.checked=true;r.onchange=()=>{(state[r.name]=state[r.name]||{}).choice=r.value;save()}});
document.querySelectorAll('textarea').forEach(t=>{const s=state[t.dataset.q];if(s&&s.text)t.value=s.text;t.oninput=()=>{(state[t.dataset.q]=state[t.dataset.q]||{}).text=t.value;save()}});
document.getElementById('export').onclick=()=>{const out={exportedAt:new Date().toISOString(),sheet:'map-review-questions-20261125',answers:DATA.map(q=>({id:q.id,title:q.title,choice:(state[q.id]||{}).choice||'',text:(state[q.id]||{}).text||''}))};const a=document.createElement('a');a.href=URL.createObjectURL(new Blob([JSON.stringify(out,null,1)],{type:'application/json'}));a.download='map-review-answers-'+new Date().toISOString().slice(0,10)+'.json';a.click()};
document.getElementById('copy').onclick=()=>{const t=DATA.map(q=>`${q.id} ${(state[q.id]||{}).choice||'-'}${(state[q.id]||{}).text?' / '+(state[q.id]||{}).text:''}`).join('\\n');navigator.clipboard.writeText(t).then(()=>alert('답을 복사했습니다. 대화창에 붙여넣으세요.'),()=>prompt('복사해서 붙여넣으세요',t))};
const z=document.getElementById('zoom');document.querySelectorAll('.pair img').forEach(i=>i.onclick=()=>{z.querySelector('img').src=i.src;z.style.display='flex'});z.onclick=()=>z.style.display='none';
count();
'''
    e = html.escape
    src = lambda n: 'data:image/jpeg;base64,' + base64.b64encode((OUT / 'img' / n).read_bytes()).decode()
    parts = []
    for q in data:
        rows = ''.join(f"<tr><th>{e(r['ref'])}<br><small>{e(r['junction'])} · {e(r['zone'])}구역</small></th><td class=dec>{e(r['dec'])}</td><td>게임: {e(r['before'])}<br>시안: {e(r['after'])}</td><td>{e(r['note']) or '-'}</td></tr>" for r in q['refs'])
        panels = ''
        for p in q['panels']:
            panels += (f"<div class=pair><figure><img src='{src(p['name'] + '-game.jpg')}' alt='게임 화면'><figcaption>게임(지금) · {e(', '.join(p['junctions']))} · 도로 {e(', '.join(p['roads'])) or '-'} · 좌표 x {p['x']}, z {p['z']}</figcaption></figure>"
                       f"<figure><img src='{src(p['name'] + '-air.jpg')}' alt='항공사진'><figcaption>실제 항공사진(카카오) · <span class=links><a href='{e(p['links']['roadview'])}' target=_blank>로드뷰 열기</a><a href='{e(p['links']['skyview'])}' target=_blank>카카오 지도</a></span></figcaption></figure></div>")
        opts = ''.join(f"<label><input type=radio name='{q['id']}' value='{e(o)}'> {e(o)}</label>" for o in q['options'])
        parts.append(f"<article id='{q['id']}'><h2>{q['id']}. {e(q['title'])} <span class=done id='st-{q['id']}'></span></h2>"
                     f"<div class=loc><img src='{src(q['id'] + '-map.jpg')}' alt='위치'><div><p class=why>{e(q['why'])}</p><p class=intro>왼쪽 지도: 분홍 원이 위치입니다. 아래 사진의 상자 색은 지금 판정입니다(초록 승인 · 빨강 반려 · 주황 다시 검토).</p></div></div>"
                     f"{panels}<table><tr><th>항목</th><th>지금 판정</th><th>내용</th><th>남기신 메모</th></tr>{rows}</table>"
                     f"<div class=answer><b>답</b>{opts}{('<p class=intro>' + e(q['extra']) + '</p>') if q['extra'] else ''}<textarea data-q='{q['id']}' placeholder='추가 의견(선택)'></textarea></div></article>")
    nav = ' '.join(f"<a href='#{q['id']}'>{q['id']}</a>" for q in data)
    doc = (f"<!doctype html><html lang=ko><head><meta charset=utf-8><meta name=viewport content='width=device-width,initial-scale=1'><title>지도 검토 확인 질문</title><style>{css}</style></head><body>"
           f"<header><h1>지도 검토 확인 질문 10개</h1><span id=cnt></span><button id=copy>답 복사</button><button id=export class=primary>답 내보내기(JSON)</button></header><main>"
           f"<p class=intro>검토 결과에서 같은 위치인데 판정이 엇갈리거나, 판정과 메모가 맞지 않는 곳입니다. 질문마다 위치 지도, 게임 화면, 실제 항공사진을 같은 축척(가로 90 m, 북쪽 위)으로 놓았습니다. 사진을 누르면 크게 보입니다. 답은 브라우저에 자동 저장됩니다. 다 고르면 <b>답 복사</b>를 눌러 대화창에 붙여넣거나 <b>답 내보내기</b> 파일을 올려 주세요.</p><p class=links>{nav}</p>"
           + ''.join(parts) + f"</main><div id=zoom><img alt=''></div><script>const DATA={json.dumps([dict(id=q['id'], title=q['title']) for q in data], ensure_ascii=False)};{js}</script></body></html>")
    (OUT / 'index.html').write_text(doc, encoding='utf-8')


if __name__ == '__main__':
    main(sys.argv[1])
