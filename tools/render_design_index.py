"""docs/content-design/INDEX.md -> docs/content-design/index.html

기획서 목록(INDEX.md)을 브라우저에서 보기 좋은 HTML 한 장으로 만든다.
INDEX.md를 고친 뒤 다시 실행한다:  python tools/render_design_index.py
"""
import html
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "docs", "content-design", "INDEX.md")
OUT = os.path.join(ROOT, "docs", "content-design", "index.html")

CSS = """
:root{--g:#F2B705;--ink:#1d2320;--sub:#5d6862;--bg:#f6f7f5;--card:#fff;--line:#dfe4e0;--th:#f7f1de;--code:#f0f2f0;
--ok:#1f8f4e;--src:#2f6fd6;--prog:#7a4fd0;--wait:#d98a00;--hold:#8a8f8c;--fix:#d2453a}
@media (prefers-color-scheme:dark){:root:not([data-theme="light"]){--ink:#e6ebe7;--sub:#9aa69f;--bg:#141816;--card:#1c211e;--line:#2e3632;--th:#2a2619;--code:#232a26}}
:root[data-theme="dark"]{--ink:#e6ebe7;--sub:#9aa69f;--bg:#141816;--card:#1c211e;--line:#2e3632;--th:#2a2619;--code:#232a26}
*{box-sizing:border-box}
body{margin:0;background:var(--bg);color:var(--ink);font:16px/1.7 "Pretendard","Apple SD Gothic Neo","Malgun Gothic","Noto Sans KR",sans-serif;word-break:keep-all}
.wrap{max-width:1040px;margin:0 auto;padding:28px 16px 80px}
header.doc{background:var(--card);border:1px solid var(--line);border-left:10px solid var(--g);border-radius:10px;padding:20px 24px;margin-bottom:24px}
header.doc h1{font-size:28px;margin:4px 0 8px;line-height:1.3}
header.doc p{margin:6px 0;color:var(--sub)}
h2{font-size:21px;margin:40px 0 10px;padding-bottom:6px;border-bottom:2px solid var(--g)}
h3{font-size:17px;margin:28px 0 6px}
h4{font-size:15px;margin:20px 0 4px;color:var(--sub)}
p,ul,ol{margin:8px 0}ul,ol{padding-left:22px}li{margin:3px 0}
a{color:#b07f00}@media (prefers-color-scheme:dark){a{color:#f2c94c}}
.tw{overflow-x:auto;margin:10px 0}
table{border-collapse:collapse;width:100%;background:var(--card);font-size:14.5px}
th,td{border:1px solid var(--line);padding:7px 10px;text-align:left;vertical-align:top}
th{background:var(--th);white-space:nowrap}
code{background:var(--code);border-radius:4px;padding:1px 5px;font-family:Consolas,Menlo,monospace;font-size:13.5px}
.st{display:inline-block;border-radius:999px;padding:0 9px;margin:1px 2px 1px 0;font-size:12.5px;font-weight:700;white-space:nowrap;color:#fff}
.st.ok{background:var(--ok)}.st.src{background:var(--src)}.st.prog{background:var(--prog)}
.st.wait{background:var(--wait)}.st.hold{background:var(--hold)}.st.fix{background:var(--fix)}
.legend{display:flex;flex-wrap:wrap;gap:6px;margin:10px 0}
.filter{display:flex;flex-wrap:wrap;gap:6px;margin:12px 0 0}
.filter button{border:1px solid var(--line);background:var(--card);color:var(--ink);border-radius:999px;padding:3px 12px;font:inherit;font-size:13.5px;cursor:pointer}
.filter button.on{border-color:var(--g);background:var(--g);color:#1d2320;font-weight:700}
nav.toc{background:var(--card);border:1px solid var(--line);border-radius:10px;padding:10px 16px;font-size:14.5px}
nav.toc a{text-decoration:none}
"""

JS = """
const btns=[...document.querySelectorAll('.filter button')];
btns.forEach(b=>b.addEventListener('click',()=>{
  btns.forEach(x=>x.classList.toggle('on',x===b));
  const k=b.dataset.k;
  document.querySelectorAll('tr[data-st]').forEach(tr=>{
    tr.style.display=(k==='all'||tr.dataset.st.split(' ').includes(k))?'':'none';
  });
}));
"""

# 상태 문구 -> 배지 종류. 위에서부터 먼저 맞는 것을 쓴다.
BADGES = [
    ("구현·배포", "ok", "구현·배포"),
    ("구현·소스", "src", "구현·소스"),
    ("진행 중", "prog", "진행 중"),
    ("미결", "wait", "미결"),
    ("결정 대기", "wait", "결정 대기"),
    ("승인 전", "wait", "승인 전"),
    ("대기", "wait", "대기"),
    ("보류", "hold", "보류"),
    ("제외", "hold", "제외"),
    ("보관", "hold", "보관"),
    ("개선 필요", "fix", "개선 필요"),
    ("미확인", "fix", "미확인"),
    ("실패", "fix", "실패"),
]


def slug(text):
    t = re.sub(r"<[^>]+>", "", text).strip().lower()
    t = re.sub(r"[^\w\- ]", "", t)
    return t.replace(" ", "-")


def inline(text):
    out = html.escape(text, quote=False)
    out = re.sub(r"`([^`]+)`", r"<code>\1</code>", out)
    out = re.sub(r"\*\*([^*]+)\*\*", r"<b>\1</b>", out)

    def link(m):
        label, href = m.group(1), m.group(2)
        # 같은 문서 안의 기획서 목록 링크는 HTML 쪽으로 돌린다
        if href == "INDEX.md":
            href = "index.html"
        return '<a href="%s">%s</a>' % (html.escape(href, quote=True), label)

    out = re.sub(r"\[([^\]]+)\]\(([^)\s]+)\)", link, out)
    return out


def status_kinds(cell):
    kinds = []
    for key, kind, _ in BADGES:
        if key in cell and kind not in kinds:
            kinds.append(kind)
    return kinds


def badge_cell(cell):
    kinds = status_kinds(cell)
    if not kinds:
        return inline(cell), kinds
    pills = []
    for key, kind, label in BADGES:
        if key in cell and kind in kinds and not any(kind == k for k, _ in pills):
            pills.append((kind, label))
    head = "".join('<span class="st %s">%s</span>' % (k, html.escape(l)) for k, l in pills)
    return head + "<br>" + inline(cell), kinds


def split_row(line):
    return [c.strip() for c in line.strip().strip("|").split("|")]


def render_table(rows):
    header = split_row(rows[0])
    body = [split_row(r) for r in rows[2:]]
    st_col = next((i for i, h in enumerate(header) if h in ("상태", "처리")), None)
    out = ['<div class="tw"><table>', "<tr>" + "".join("<th>%s</th>" % inline(h) for h in header) + "</tr>"]
    for cells in body:
        tds, kinds = [], []
        for i, c in enumerate(cells):
            if i == st_col:
                content, kinds = badge_cell(c)
            else:
                content = inline(c)
            tds.append("<td>%s</td>" % content)
        attr = ' data-st="%s"' % " ".join(kinds) if st_col is not None else ""
        out.append("<tr%s>%s</tr>" % (attr, "".join(tds)))
    out.append("</table></div>")
    return "\n".join(out)


def main():
    lines = open(SRC, encoding="utf-8").read().splitlines()
    title = lines[0].lstrip("# ").strip()
    body, toc, i = [], [], 1
    header_paras = []
    # 첫 h2 전까지는 머리말 상자에 넣는다
    while i < len(lines) and not lines[i].startswith("## "):
        if lines[i].strip():
            header_paras.append("<p>%s</p>" % inline(lines[i].strip()))
        i += 1
    para = []

    def flush():
        if para:
            body.append("<p>%s</p>" % inline(" ".join(para)))
            para.clear()

    while i < len(lines):
        line = lines[i]
        if line.startswith("|"):
            flush()
            rows = []
            while i < len(lines) and lines[i].startswith("|"):
                rows.append(lines[i])
                i += 1
            body.append(render_table(rows))
            continue
        m = re.match(r"^(#{2,4}) (.+)$", line)
        if m:
            flush()
            level, text = len(m.group(1)), m.group(2)
            sid = slug(text)
            body.append('<h%d id="%s">%s</h%d>' % (level, sid, inline(text), level))
            if level <= 3:
                toc.append((level, sid, re.sub(r"\s*—.*$", "", text)))
            if text == "상태 표기":
                legend = "".join('<span class="st %s">%s</span>' % (k, l) for k, l in [
                    ("ok", "구현·배포"), ("src", "구현·소스"), ("prog", "진행 중"),
                    ("wait", "미결"), ("hold", "보류·제외"), ("fix", "개선 필요")])
                body.append('<div class="legend">%s</div>' % legend)
            i += 1
            continue
        if line.startswith("- "):
            flush()
            items = []
            while i < len(lines) and lines[i].startswith("- "):
                items.append("<li>%s</li>" % inline(lines[i][2:].strip()))
                i += 1
            body.append("<ul>%s</ul>" % "".join(items))
            continue
        if line.strip():
            para.append(line.strip())
        else:
            flush()
        i += 1
    flush()

    toc_html = " · ".join(
        ('<a href="#%s">%s</a>' % (sid, html.escape(t))) if lvl == 2 else
        ('<a href="#%s" style="color:var(--sub)">%s</a>' % (sid, html.escape(t)))
        for lvl, sid, t in toc if lvl == 2 or re.match(r"^[A-I]\.", t))
    filters = "".join('<button data-k="%s"%s>%s</button>' % (k, ' class="on"' if k == "all" else "", l) for k, l in [
        ("all", "전체"), ("ok", "구현·배포"), ("prog", "진행 중"), ("wait", "미결"),
        ("fix", "개선 필요"), ("hold", "보류·제외")])

    page = """<!DOCTYPE html>
<html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>기획서 목록과 현황</title><style>%s</style></head>
<body><div class="wrap">
<header class="doc"><h1>%s</h1>%s
<div class="filter">%s</div></header>
<nav class="toc">%s</nav>
%s
<p style="margin-top:40px;color:var(--sub);font-size:13px">이 페이지는 <code>INDEX.md</code>에서 <code>python tools/render_design_index.py</code>로 생성한다. 내용은 INDEX.md를 고친다.</p>
</div><script>%s</script></body></html>
""" % (CSS, html.escape(title), "".join(header_paras), filters, toc_html, "\n".join(body), JS)
    open(OUT, "w", encoding="utf-8", newline="\n").write(page)
    print("wrote", os.path.relpath(OUT, ROOT))


if __name__ == "__main__":
    sys.exit(main())
