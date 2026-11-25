"""기획 문서(md) -> 같은 폴더의 html

docs/content-design/의 기획서(00~09, README, INDEX, 마인드맵 목차)와 pvp/ 문서를
브라우저에서 읽기 좋은 HTML로 만든다. 원본은 md다. md를 고친 뒤 다시 실행한다.

    python tools/render_design_docs.py            # 작업 폴더의 md로 생성
    python tools/render_design_docs.py --rev HEAD # 커밋된 md로 생성(다른 작업의 미커밋 변경 제외)
    python tools/render_design_docs.py --rev HEAD --force  # 미커밋 html도 덮어씀

외부 패키지 없이 표준 라이브러리만 쓴다.
"""
import html
import os
import re
import subprocess
import sys
import urllib.parse

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CD = "docs/content-design"


def targets():
    names = sorted(f for f in os.listdir(os.path.join(ROOT, CD)) if f.endswith(".md"))
    out = [CD + "/" + f for f in names]
    pvp = sorted(f for f in os.listdir(os.path.join(ROOT, CD, "pvp")) if f.endswith(".md"))
    out += [CD + "/pvp/" + f for f in pvp]
    return out


def place_targets(index_text):
    """INDEX의 "기획 위치" 칸이 가리키는 md 중 위 대상 밖의 문서(작업 이력의 상세 기획, 보관 기획 등)."""
    out = []
    for line in index_text.replace("\r\n", "\n").split("\n"):
        if not line.strip().startswith("|"):
            continue
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        if len(cells) < 2:
            continue
        for h in re.findall(r"\]\(([^)#\s]+\.md)", cells[1]):
            t = os.path.normpath(os.path.join(CD, urllib.parse.unquote(h))).replace(os.sep, "/")
            if t not in out and os.path.exists(os.path.join(ROOT, t)):
                out.append(t)
    return out


def out_name(md_path):
    d, b = os.path.split(md_path)
    if b == "INDEX.md":
        b = "index.md"
    return d + "/" + b[:-3] + ".html"


# ---------------------------------------------------------------- inline

ALLOWED_TAG = re.compile(r"</?(?:br|a|b|i|em|strong|sup|sub|span|kbd|small|u|s|mark|code)\b[^>]*>", re.I)


def slug(text):
    t = re.sub(r"!\[[^\]]*\]\([^)]*\)", "", text)
    t = re.sub(r"\[([^\]]*)\]\([^)]*\)", r"\1", t)
    t = re.sub(r"<[^>]+>", "", t).replace("`", "").replace("*", "")
    t = t.strip().lower()
    t = re.sub(r"[^\w\- ]", "", t)
    return t.replace(" ", "-")


class Ctx:
    def __init__(self, src, generated):
        self.src = src
        self.base = os.path.dirname(src)
        self.generated = generated

    def href(self, href):
        if re.match(r"^[a-z]+:", href) or href.startswith("#"):
            return href
        path, sep, frag = href.partition("#")
        if path.endswith(".md"):
            tgt = os.path.normpath(os.path.join(self.base, urllib.parse.unquote(path))).replace(os.sep, "/")
            if tgt in self.generated:
                path = path[:-3] + ".html"
                if os.path.basename(tgt) == "INDEX.md":
                    path = path[: -len("INDEX.html")] + "index.html"
        return path + sep + frag


def inline(text, ctx):
    stash = []

    def keep(s):
        stash.append(s)
        return "\x00%d\x00" % (len(stash) - 1)

    text = re.sub(r"`([^`]+)`", lambda m: keep("<code>%s</code>" % html.escape(m.group(1), quote=False)), text)
    text = ALLOWED_TAG.sub(lambda m: keep(m.group(0)), text)

    def img(m):
        alt, src = m.group(1), m.group(2).split(" ")[0]
        return keep('<img src="%s" alt="%s" loading="lazy">' % (html.escape(ctx.href(src), quote=True), html.escape(alt, quote=True)))

    text = re.sub(r"!\[([^\]]*)\]\(([^)]+)\)", img, text)

    def link(m):
        label, href = m.group(1), m.group(2).strip()
        if href.startswith("<") and href.endswith(">"):
            href = href[1:-1]
        href = href.split(' "')[0]
        return keep('<a href="%s">' % html.escape(ctx.href(href), quote=True)) + label + keep("</a>")

    # 안쪽 링크부터 바꾼다. 링크 안의 링크는 GitHub처럼 바깥 괄호를 글자로 남긴다.
    text = re.sub(r"\[([^\[\]]+)\]\(([^)\s]+(?: \"[^\"]*\")?)\)", link, text)
    text = re.sub(r"<(https?://[^>\s]+)>", lambda m: keep('<a href="%s">%s</a>' % (m.group(1), m.group(1))), text)
    text = html.escape(text, quote=False)
    text = re.sub(r"\*\*(.+?)\*\*", r"<b>\1</b>", text)
    text = re.sub(r"__(.+?)__", r"<b>\1</b>", text)
    text = re.sub(r"(?<![\w*])\*(?!\s)(.+?)(?<!\s)\*(?![\w*])", r"<i>\1</i>", text)
    text = re.sub(r"~~(.+?)~~", r"<s>\1</s>", text)
    while "\x00" in text:
        text = re.sub(r"\x00(\d+)\x00", lambda m: stash[int(m.group(1))], text)
    return text


# ---------------------------------------------------------------- blocks

LIST_RE = re.compile(r"^(\s*)([-*+]|\d+[.)])\s+(.*)$")
HR_RE = re.compile(r"^\s{0,3}([-*_])(\s*\1){2,}\s*$")


def is_table_start(lines, i):
    return (lines[i].lstrip().startswith("|") and i + 1 < len(lines)
            and re.match(r"^\s*\|?\s*:?-{2,}", lines[i + 1]) is not None)


def split_row(line):
    s = line.strip()
    if s.startswith("|"):
        s = s[1:]
    if s.endswith("|"):
        s = s[:-1]
    cells, cur, code = [], "", False
    for ch in s:
        if ch == "`":
            code = not code
        if ch == "|" and not code and not cur.endswith("\\"):
            cells.append(cur.strip())
            cur = ""
        else:
            cur += ch
    cells.append(cur.strip())
    return [c.replace("\\|", "|") for c in cells]


class Renderer:
    def __init__(self, ctx, hooks=None):
        self.ctx = ctx
        self.ids = {}
        self.toc = []
        self.hooks = hooks or {}

    def uniq(self, s):
        n = self.ids.get(s, 0)
        self.ids[s] = n + 1
        return s if n == 0 else "%s-%d" % (s, n)

    def table(self, rows):
        header = split_row(rows[0])
        aligns = []
        for c in split_row(rows[1]):
            c = c.strip()
            aligns.append("right" if c.endswith(":") and not c.startswith(":") else
                          "center" if c.startswith(":") and c.endswith(":") else "")
        hook = self.hooks.get("row")
        out = ['<div class="tw"><table><thead><tr>']
        for i, h in enumerate(header):
            out.append("<th>%s</th>" % inline(h, self.ctx))
        out.append("</tr></thead><tbody>")
        for r in rows[2:]:
            cells = split_row(r)
            attr, rendered = "", []
            for i, c in enumerate(cells):
                content = inline(c, self.ctx)
                if hook:
                    content, extra = hook(header, i, c, content)
                    attr = attr or extra
                al = aligns[i] if i < len(aligns) and aligns[i] else ""
                rendered.append('<td%s>%s</td>' % (' style="text-align:%s"' % al if al else "", content))
            out.append("<tr%s>%s</tr>" % (attr, "".join(rendered)))
        out.append("</tbody></table></div>")
        return "\n".join(out)

    def list_block(self, lines):
        # lines: 목록 한 덩어리. 들여쓰기로 중첩을 만든다.
        items = []  # (indent, ordered, text_lines)
        for ln in lines:
            m = LIST_RE.match(ln)
            if m:
                items.append([len(m.group(1).replace("\t", "    ")), m.group(2)[0].isdigit(), [m.group(3)]])
            elif items:
                items[-1][2].append(ln.strip())
        return self._nest(items, 0, len(items))[0]

    def _nest(self, items, i, end):
        if i >= end:
            return "", i
        base = items[i][0]
        ordered = items[i][1]
        tag = "ol" if ordered else "ul"
        out = ["<%s>" % tag]
        while i < end and items[i][0] >= base:
            if items[i][0] > base:
                sub, i = self._nest(items, i, end)
                out[-1] = out[-1][:-5] + sub + "</li>"
                continue
            text = " ".join(t for t in items[i][2] if t)
            task = re.match(r"^\[( |x|X)\]\s+(.*)$", text)
            if task:
                text = ('<input type="checkbox" disabled%s> ' % (" checked" if task.group(1) != " " else "")) + inline(task.group(2), self.ctx)
            else:
                text = inline(text, self.ctx)
            out.append("<li>%s</li>" % text)
            i += 1
        out.append("</%s>" % tag)
        return "".join(out), i

    def render(self, lines):
        out, para = [], []
        i, n = 0, len(lines)

        def flush():
            if para:
                parts = []
                for k, p in enumerate(para):
                    hard = p.endswith("  ") or p.endswith("\\")
                    p = p.rstrip().rstrip("\\")
                    parts.append(inline(p.strip(), self.ctx) + ("<br>" if hard and k < len(para) - 1 else ""))
                out.append("<p>%s</p>" % " ".join(parts))
                para.clear()

        while i < n:
            line = lines[i]
            s = line.strip()
            if not s:
                flush()
                i += 1
                continue
            if s.startswith("```") or s.startswith("~~~"):
                flush()
                fence = s[:3]
                lang = s[3:].strip()
                code = []
                i += 1
                while i < n and not lines[i].strip().startswith(fence):
                    code.append(lines[i])
                    i += 1
                i += 1
                out.append('<pre><code%s>%s</code></pre>' % (' class="lang-%s"' % html.escape(lang) if lang else "",
                                                             html.escape("\n".join(code), quote=False)))
                continue
            if s.startswith("<!--"):
                flush()
                while i < n and "-->" not in lines[i]:
                    i += 1
                i += 1
                continue
            if re.match(r"^<(a|div|details|summary|/details|/div|table|img|p|br|hr|sup|span)\b", s, re.I) and not para:
                out.append(s)
                i += 1
                continue
            m = re.match(r"^(#{1,6})\s+(.*?)\s*#*\s*$", line)
            if m:
                flush()
                level, text = len(m.group(1)), m.group(2)
                hid = self.uniq(slug(text))
                out.append('<h%d id="%s">%s</h%d>' % (level, hid, inline(text, self.ctx), level))
                if level in (2, 3):
                    self.toc.append((level, hid, re.sub(r"<[^>]+>", "", inline(text, self.ctx))))
                if "heading" in self.hooks:
                    out.append(self.hooks["heading"](text))
                i += 1
                continue
            if HR_RE.match(line) and not para:
                out.append("<hr>")
                i += 1
                continue
            if is_table_start(lines, i):
                flush()
                rows = []
                while i < n and lines[i].strip().startswith("|"):
                    rows.append(lines[i])
                    i += 1
                out.append(self.table(rows))
                continue
            if s.startswith(">"):
                flush()
                quote = []
                while i < n and lines[i].strip().startswith(">"):
                    quote.append(re.sub(r"^\s*>\s?", "", lines[i]))
                    i += 1
                out.append("<blockquote>%s</blockquote>" % Renderer(self.ctx).render(quote))
                continue
            if LIST_RE.match(line) and (not para or not line.startswith(" ")):
                flush()
                block = []
                while i < n:
                    ln = lines[i]
                    if not ln.strip():
                        # 빈 줄 뒤에 같은 목록이 이어지면 함께 묶는다
                        if i + 1 < n and (LIST_RE.match(lines[i + 1]) or lines[i + 1].startswith("  ")):
                            i += 1
                            continue
                        break
                    if LIST_RE.match(ln) or ln.startswith("  ") or ln.startswith("\t"):
                        block.append(ln)
                        i += 1
                    else:
                        break
                out.append(self.list_block(block))
                continue
            para.append(line)
            i += 1
        flush()
        return "\n".join(out)


# ---------------------------------------------------------------- page

CSS = """
:root{--accent:#F2B705;--ink:#1d2320;--sub:#5d6862;--bg:#f6f7f5;--card:#fff;--line:#dfe4e0;--th:#f7f1de;--code:#f0f2f0;--link:#9a6c00;
--ok:#1f8f4e;--src:#2f6fd6;--prog:#7a4fd0;--wait:#d98a00;--hold:#8a8f8c;--fix:#d2453a}
@media (prefers-color-scheme:dark){:root:not([data-theme="light"]){--ink:#e6ebe7;--sub:#9aa69f;--bg:#141816;--card:#1c211e;--line:#2e3632;--th:#2a2619;--code:#232a26;--link:#f2c94c}}
:root[data-theme="dark"]{--ink:#e6ebe7;--sub:#9aa69f;--bg:#141816;--card:#1c211e;--line:#2e3632;--th:#2a2619;--code:#232a26;--link:#f2c94c}
*{box-sizing:border-box}
body{margin:0;background:var(--bg);color:var(--ink);font:16px/1.75 "Pretendard","Apple SD Gothic Neo","Malgun Gothic","Noto Sans KR",sans-serif;word-break:keep-all;overflow-wrap:anywhere}
.wrap{max-width:1040px;margin:0 auto;padding:24px 16px 80px}
.crumb{font-size:13.5px;color:var(--sub);margin-bottom:10px}
header.doc{background:var(--card);border:1px solid var(--line);border-left:10px solid var(--accent);border-radius:10px;padding:18px 22px;margin-bottom:20px}
header.doc h1{font-size:27px;margin:2px 0 6px;line-height:1.35}
header.doc p{margin:6px 0;color:var(--sub)}
h1{font-size:26px}h2{font-size:21px;margin:40px 0 10px;padding-bottom:6px;border-bottom:2px solid var(--accent)}
h3{font-size:17.5px;margin:28px 0 6px}h4,h5,h6{font-size:15.5px;margin:20px 0 4px;color:var(--sub)}
p,ul,ol{margin:8px 0}ul,ol{padding-left:24px}li{margin:3px 0}
a{color:var(--link)}
img{max-width:100%;height:auto;border:1px solid var(--line);border-radius:6px;background:#fff}
.tw{overflow-x:auto;margin:12px 0}
table{border-collapse:collapse;width:100%;background:var(--card);font-size:14.5px}
th,td{border:1px solid var(--line);padding:7px 10px;text-align:left;vertical-align:top}
th{background:var(--th)}
code{background:var(--code);border-radius:4px;padding:1px 5px;font-family:Consolas,Menlo,monospace;font-size:13.5px}
pre{background:var(--code);border-radius:8px;padding:12px 14px;overflow-x:auto;line-height:1.5}pre code{padding:0;background:none}
blockquote{margin:12px 0;padding:6px 14px;border-left:5px solid var(--accent);background:var(--card);color:var(--sub);border-radius:0 6px 6px 0}
hr{border:none;border-top:1px solid var(--line);margin:28px 0}
details.toc{background:var(--card);border:1px solid var(--line);border-radius:10px;padding:8px 16px;font-size:14.5px}
details.toc summary{cursor:pointer;font-weight:700}
details.toc ol{margin:6px 0}details.toc li.l3{margin-left:18px;list-style:circle;color:var(--sub)}
details.toc a{text-decoration:none}
nav.foot{display:flex;justify-content:space-between;gap:12px;margin-top:48px;padding-top:16px;border-top:1px solid var(--line);font-size:14px;flex-wrap:wrap}
.st{display:inline-block;border-radius:999px;padding:0 9px;margin:1px 2px 1px 0;font-size:12.5px;font-weight:700;white-space:nowrap;color:#fff}
.st.ok{background:var(--ok)}.st.src{background:var(--src)}.st.prog{background:var(--prog)}.st.wait{background:var(--wait)}.st.hold{background:var(--hold)}.st.fix{background:var(--fix)}
.legend{display:flex;flex-wrap:wrap;gap:6px;margin:10px 0}
.filter{display:flex;flex-wrap:wrap;gap:6px;margin:12px 0 0}
.filter button{border:1px solid var(--line);background:var(--card);color:var(--ink);border-radius:999px;padding:3px 12px;font:inherit;font-size:13.5px;cursor:pointer}
.filter button.on{border-color:var(--accent);background:var(--accent);color:#1d2320;font-weight:700}
.gen{margin-top:28px;color:var(--sub);font-size:12.5px}
.fixnote{margin:8px 0 14px;padding:8px 14px;border:1px solid var(--line);border-left:5px solid var(--fix);border-radius:0 8px 8px 0;background:var(--card);font-size:14.5px}
.fixnote ul{margin:4px 0}.fixnote .more{font-size:13px;color:var(--sub)}
h1 .st,h2 .st,h3 .st,h4 .st,h5 .st,h6 .st{vertical-align:middle;margin-left:6px}
header.doc .fixsum{color:var(--ink);font-size:14.5px}
details.toc .fx{color:var(--fix);font-size:12px;font-weight:700;margin-left:4px}
"""

FILTER_JS = """
const btns=[...document.querySelectorAll('.filter button')];
btns.forEach(b=>b.addEventListener('click',()=>{
  btns.forEach(x=>x.classList.toggle('on',x===b));
  const k=b.dataset.k;
  document.querySelectorAll('tr[data-st]').forEach(tr=>{
    tr.style.display=(k==='all'||tr.dataset.st.split(' ').includes(k))?'':'none';
  });
}));
"""

# INDEX의 상태 칸 배지. 위에서부터 먼저 맞는 것을 쓴다.
BADGES = [
    ("구현·배포", "ok", "구현·배포"), ("구현·소스", "src", "구현·소스"), ("진행 중", "prog", "진행 중"),
    ("미결", "wait", "미결"), ("결정 대기", "wait", "결정 대기"), ("승인 전", "wait", "승인 전"),
    ("대기", "wait", "대기"), ("보류", "hold", "보류"), ("제외", "hold", "제외"), ("보관", "hold", "보관"),
    ("개선 필요", "fix", "개선 필요"), ("미확인", "fix", "미확인"), ("실패", "fix", "실패"),
]


def index_row_hook(header, i, raw, rendered):
    if i >= len(header) or header[i] not in ("상태", "처리"):
        return rendered, ""
    kinds, pills = [], []
    for key, kind, label in BADGES:
        if key in raw and kind not in kinds:
            kinds.append(kind)
            pills.append('<span class="st %s">%s</span>' % (kind, label))
    if not kinds:
        return rendered, ' data-st=""'
    return "".join(pills) + "<br>" + rendered, ' data-st="%s"' % " ".join(kinds)


def index_heading_hook(text):
    if text.strip() != "상태 표기":
        return ""
    return '<div class="legend">%s</div>' % "".join('<span class="st %s">%s</span>' % kv for kv in [
        ("ok", "구현·배포"), ("src", "구현·소스"), ("prog", "진행 중"), ("wait", "미결"), ("hold", "보류·제외"), ("fix", "개선 필요")])


# ---------------------------------------------------------------- 개선 필요 표시
# INDEX의 "항목 | 기획 위치 | 상태 | 근거" 표에서 상태가 개선 필요(INDEX 필터의 fix와 같은 기준)인 줄을
# 기획 위치 링크가 가리키는 문서의 장 제목 아래에 표시한다. 기준은 INDEX 하나다.
MD_LINK = re.compile(r"\[((?:[^\[\]]|\[[^\]]*\])*)\]\(([^)\s]+)\)")


def is_fix(raw):
    return any(key in raw for key, kind, _ in BADGES if kind == "fix")


def rebase_links(md, from_dir, to_dir):
    def sub(m):
        href = m.group(2)
        if re.match(r"^[a-z]+:", href) or href.startswith("#"):
            return m.group(0)
        path, sep, frag = href.partition("#")
        tgt = os.path.normpath(os.path.join(from_dir, urllib.parse.unquote(path)))
        new = os.path.relpath(tgt, to_dir).replace(os.sep, "/")
        if path.endswith("/") and not new.endswith("/"):
            new += "/"
        return "[%s](%s%s%s)" % (m.group(1), urllib.parse.quote(new), sep, frag)
    return MD_LINK.sub(sub, md)


def collect_fix_notes(index_text, all_paths):
    notes = {}
    lines = index_text.replace("\r\n", "\n").split("\n")
    header = None
    for k, line in enumerate(lines):
        if not line.strip().startswith("|"):
            header = None
            continue
        if header is None:
            if is_table_start(lines, k):
                header = [c.strip() for c in split_row(line)]
            continue
        if re.match(r"^\s*\|[\s:|-]+\|\s*$", line):
            continue
        if len(header) < 4 or header[1] != "기획 위치":
            continue
        cells = [c.strip() for c in split_row(line)]
        if len(cells) < 4 or not is_fix(cells[2]):
            continue
        seen = set()
        for m in MD_LINK.finditer(cells[1]):
            path, _, frag = m.group(2).partition("#")
            tgt = os.path.normpath(os.path.join(CD, urllib.parse.unquote(path))).replace(os.sep, "/")
            if tgt not in all_paths or tgt == CD + "/INDEX.md" or (tgt, frag) in seen:
                continue
            seen.add((tgt, frag))
            notes.setdefault(tgt, []).append({"frag": urllib.parse.unquote(frag), "item": cells[0],
                                              "status": cells[2], "evidence": cells[3]})
    return notes


def note_li(n, path, ctx):
    d = os.path.dirname(path)
    return "<li><b>%s</b> — %s<br><span class=\"more\">근거·남은 것: %s</span></li>" % (
        inline(rebase_links(n["item"], CD, d), ctx), inline(rebase_links(n["status"], CD, d), ctx),
        inline(rebase_links(n["evidence"], CD, d), ctx))


def apply_fix_notes(body, notes, path, ctx):
    """본문에 표시를 넣고 (본문, 표시한 제목 id 목록, 머리말 요약)을 돌려준다."""
    if not notes:
        return body, set(), ""
    head_re = re.compile(r'<h([1-6]) id="([^"]*)">')
    by_head, order, loose = {}, [], []
    for n in notes:
        hm = None
        if n["frag"]:
            m = re.search(r'id="%s"' % re.escape(n["frag"]), body)
            if m:
                hm = head_re.search(body, body.rfind("<", 0, m.start()))
        if not hm:
            loose.append(n)
            continue
        hid = hm.group(2)
        if hid not in by_head:
            by_head[hid] = []
            order.append(hid)
        by_head[hid].append(n)
    index_href = rel(path, out_name(CD + "/INDEX.md"))
    titles = {}
    for hid in order:
        hm = re.search(r'<h([1-6]) id="%s">' % re.escape(hid), body)
        close = body.find("</h%s>" % hm.group(1), hm.end())
        titles[hid] = re.sub(r"<[^>]+>", "", body[hm.end():close]).strip()
        box = '<div class="fixnote"><span class="st fix">개선 필요</span> %d건 · <a href="%s">기획서 목록에서 보기</a><ul>%s</ul></div>' % (
            len(by_head[hid]), index_href, "".join(note_li(n, path, ctx) for n in by_head[hid]))
        body = body[:close] + '<span class="st fix">개선 필요</span>' + body[close:close + 5] + "\n" + box + body[close + 5:]
    if loose:
        body = '<div class="fixnote"><span class="st fix">개선 필요</span> 문서 전체 %d건 · <a href="%s">기획서 목록에서 보기</a><ul>%s</ul></div>\n' % (
            len(loose), index_href, "".join(note_li(n, path, ctx) for n in loose)) + body
    links = ['<a href="#%s">%s</a>' % (hid, html.escape(titles[hid])) for hid in order]
    if loose:
        links.insert(0, "문서 전체 %d건" % len(loose))
    summary = '<p class="fixsum"><span class="st fix">개선 필요 %d건</span> %s <span class="more">(<a href="%s">기획서 목록</a> 기준)</span></p>' % (
        len(notes), " · ".join(links), index_href)
    return body, set(order), summary


def neighbours(path, all_paths):
    same = [p for p in all_paths if os.path.dirname(p) == os.path.dirname(path)
            and re.match(r"^\d\d-", os.path.basename(p))]
    if path not in same:
        return None, None
    k = same.index(path)
    return (same[k - 1] if k > 0 else None), (same[k + 1] if k + 1 < len(same) else None)


def title_of(md_text, path):
    m = re.search(r"^#\s+(.+)$", md_text, re.M)
    return re.sub(r"[*`]", "", m.group(1)).strip() if m else os.path.basename(path)[:-3]


def rel(frm, to):
    return urllib.parse.quote(os.path.relpath(to, os.path.dirname(frm)).replace(os.sep, "/"))


def page(path, text, all_paths, texts, fix_notes=None):
    ctx = Ctx(path, set(all_paths))
    is_index = os.path.basename(path) == "INDEX.md"
    hooks = {"row": index_row_hook, "heading": index_heading_hook} if is_index else {}
    lines = text.replace("\r\n", "\n").split("\n")
    title = title_of(text, path)
    # 첫 h1과 그 뒤 첫 문단은 머리말 상자로 뺀다
    h1 = next((k for k, l in enumerate(lines) if re.match(r"^#\s+", l)), None)
    lead_html = ""
    if h1 is not None:
        k = h1 + 1
        while k < len(lines) and not lines[k].strip():
            k += 1
        lead = []
        while k < len(lines) and lines[k].strip() and not re.match(r"^(#|\||<|```|\s*[-*+]\s|\s*\d+[.)]\s)", lines[k]):
            lead.append(lines[k])
            k += 1
        lead_html = Renderer(ctx).render(lead) if lead else ""
        lines = lines[:h1] + lines[k:]
    r = Renderer(ctx, hooks)
    r.ids[slug(title)] = 1  # h1 자리 id 예약(GitHub와 같은 번호 매김)
    body = r.render(lines)
    body, fixed, fixsum = apply_fix_notes(body, (fix_notes or {}).get(path, []), path, ctx)

    idx = CD + "/INDEX.md"
    readme = CD + "/README.md"
    crumbs = ['<a href="%s">기획서 목록</a>' % rel(path, out_name(idx)),
              '<a href="%s">기획 README</a>' % rel(path, out_name(readme))]
    if "/pvp/" in path and not path.endswith("pvp/README.md"):
        crumbs.append('<a href="%s">PvP 목록</a>' % rel(path, out_name(CD + "/pvp/README.md")))
    crumbs.append('<a href="%s">원본 md</a>' % urllib.parse.quote(os.path.basename(path)))

    toc = ""
    if len(r.toc) >= 3 and not is_index:
        toc = '<details class="toc"%s><summary>목차 (%d)</summary><ol>%s</ol></details>' % (
            " open" if len(r.toc) <= 14 else "", len([t for t in r.toc if t[0] == 2]) or len(r.toc),
            "".join('<li class="l%d"><a href="#%s">%s</a>%s</li>' % (lv, hid, html.escape(t), '<span class="fx">● 개선 필요</span>' if hid in fixed else "")
                    for lv, hid, t in r.toc))
    filt = ""
    if is_index:
        filt = '<div class="filter">%s</div>' % "".join(
            '<button data-k="%s"%s>%s</button>' % (k, ' class="on"' if k == "all" else "", l) for k, l in [
                ("all", "전체"), ("ok", "구현·배포"), ("prog", "진행 중"), ("wait", "미결"), ("fix", "개선 필요"), ("hold", "보류·제외")])
        toc = '<details class="toc" open><summary>바로 가기</summary><ol>%s</ol></details>' % "".join(
            '<li class="l%d"><a href="#%s">%s</a></li>' % (lv, hid, html.escape(t)) for lv, hid, t in r.toc
            if lv == 2 or re.match(r"^[A-I]\.", t))

    prev_p, next_p = neighbours(path, all_paths)
    foot = ""
    if prev_p or next_p:
        def lab(p):
            return html.escape(title_of(texts[p], p))
        foot = '<nav class="foot"><span>%s</span><span>%s</span></nav>' % (
            ('← <a href="%s">%s</a>' % (rel(path, out_name(prev_p)), lab(prev_p))) if prev_p else "",
            ('<a href="%s">%s</a> →' % (rel(path, out_name(next_p)), lab(next_p))) if next_p else "")

    return """<!DOCTYPE html>
<html lang="ko"><head><meta charset="utf-8"><meta http-equiv="Cache-Control" content="no-cache"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>%s</title><style>%s</style></head>
<body><div class="wrap">
<div class="crumb">%s</div>
<header class="doc"><h1>%s</h1>%s%s%s</header>
%s
%s
%s
<p class="gen">이 페이지는 <code>%s</code>에서 <code>python tools/render_design_docs.py</code>로 만든다. 내용은 md를 고친다.</p>
</div>%s</body></html>
""" % (html.escape(title), CSS, " · ".join(crumbs), inline(title, ctx), lead_html, fixsum, filt, toc, body, foot,
       html.escape(path), "<script>%s</script>" % FILTER_JS if is_index else "")


def read(path, rev):
    if rev:
        r = subprocess.run(["git", "-C", ROOT, "show", "%s:%s" % (rev, path)], capture_output=True)
        if r.returncode != 0:
            return None
        return r.stdout.decode("utf-8")
    full = os.path.join(ROOT, path)
    return open(full, encoding="utf-8").read() if os.path.exists(full) else None


def main(argv):
    rev = argv[argv.index("--rev") + 1] if "--rev" in argv else None
    all_paths = targets()
    texts = {p: read(p, rev) for p in all_paths}
    if texts.get(CD + "/INDEX.md"):
        extra = [p for p in place_targets(texts[CD + "/INDEX.md"]) if p not in texts]
        all_paths += extra
        texts.update({p: read(p, rev) for p in extra})
    all_paths = [p for p in all_paths if texts[p] is not None]
    dirty = set()
    if rev and "--force" not in argv:
        # 다른 작업이 고쳐 둔(커밋된 파일의 미커밋 변경) html은 덮어쓰지 않는다. 새 파일(??)은 쓴다.
        st = subprocess.run(["git", "-C", ROOT, "-c", "core.quotepath=off", "status", "--porcelain", "--"] + sorted({os.path.dirname(p) for p in all_paths}),
                            capture_output=True).stdout.decode("utf-8").splitlines()
        dirty = {l[3:] for l in st if l[:2] != "??" and l[:2].strip() and l[3:].endswith(".html")}
    idx = CD + "/INDEX.md"
    fix_notes = collect_fix_notes(texts[idx], set(all_paths)) if texts.get(idx) else {}
    written, skipped = 0, []
    for p in all_paths:
        if out_name(p) in dirty:
            skipped.append(out_name(p))
            continue
        out = os.path.join(ROOT, out_name(p))
        open(out, "w", encoding="utf-8", newline="\n").write(page(p, texts[p], all_paths, texts, fix_notes))
        written += 1
    print("wrote %d html files%s" % (written, " from " + rev if rev else ""))
    for p in sorted(fix_notes):
        print("개선 필요 %s: %d" % (p, len(fix_notes[p])))
    for s in skipped:
        print("skipped (uncommitted changes):", s)


if __name__ == "__main__":
    main(sys.argv[1:])
