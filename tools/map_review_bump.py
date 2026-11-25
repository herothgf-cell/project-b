"""Register a new reviewed Eungye map revision after a map edit (2026-11 map review batches).

Adds reviewedR<N>Hash to BusinessProgress (save migration), widens JunctionRouteFeedbackTests to r25-r<N>
and refreshes the project map's size/SHA256 in config/maps/local-input-manifest.json. The hash is read
from the saved project file itself, so it cannot be cut short (r57 once was).
Usage: python tools/map_review_bump.py <revision> "<one-line comment>"
"""
import hashlib
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MAP = ROOT / 'config/maps/project/eungye.project.json'
BP = ROOT / 'unity/EungyeTaxi/Assets/Scripts/BusinessProgress.cs'
TESTS = ROOT / 'unity/EungyeTaxi/Assets/Editor/JunctionRouteFeedbackTests.cs'
MANIFEST = ROOT / 'config/maps/local-input-manifest.json'


def main(rev, comment):
    raw = MAP.read_bytes(); pack = json.loads(raw.decode('utf-8-sig'))
    h = pack['contentHash']; assert re.fullmatch(r'[0-9a-f]{64}', h), h
    assert pack['document']['revision'] == rev, ('map revision', pack['document']['revision'])
    prev = rev - 1
    s = BP.read_bytes().decode('utf-8')
    if f'reviewedR{rev}Hash=' in s:
        s = re.sub(rf'reviewedR{rev}Hash="[0-9a-f]*"', f'reviewedR{rev}Hash="{h}"', s)
    else:
        a = re.search(rf'  const string reviewedR{prev}Hash="[0-9a-f]{{64}}";\n', s).group(0)
        s = s.replace(a, a + f'  // r{rev} {comment}\n  const string reviewedR{rev}Hash="{h}";\n')
        for o, n in [(f'revision<={prev};', f'revision<={rev};'), (f'to>{prev})return;', f'to>{rev})return;'),
                     (f'string reviewedHash=to=={prev}?', f'string reviewedHash=to=={rev}?reviewedR{rev}Hash:to=={prev}?')]:
            assert s.count(o) == 1, o; s = s.replace(o, n)
    BP.write_bytes(s.encode('utf-8'))
    t = TESTS.read_bytes().decode('utf-8')
    t = re.sub(r'Range\(25,\d+\)', f'Range(25,{rev - 24})', t); t = re.sub(r'r25–r\d+', f'r25–r{rev}', t)
    TESTS.write_bytes(t.encode('utf-8'))
    m = MANIFEST.read_bytes().decode('utf-8')
    m = re.sub(r'("path": "config/maps/project/eungye.project.json",\s*"bytes": )\d+(,\s*"sha256": ")[0-9a-f]{64}',
               lambda g: g.group(1) + str(len(raw)) + g.group(2) + hashlib.sha256(raw).hexdigest(), m)
    MANIFEST.write_bytes(m.encode('utf-8'))
    print(f'r{rev} {h} bytes {len(raw)}')


if __name__ == '__main__':
    main(int(sys.argv[1]), sys.argv[2])
