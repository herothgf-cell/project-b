"""Strict, dependency-free reader for NGII's 2D SHP/DBF ZIP layers.

Reads archive members in memory; never extracts files or modifies original data.
Only polyline/polygon layers needed by the driving map are supported.
"""
import hashlib
import math
import re
import struct
import xml.etree.ElementTree as ET
from pathlib import Path, PurePosixPath
from zipfile import ZipFile


def check(condition, message):
    if not condition:
        raise ValueError(message)


def read_dbf(data, encoding):
    check(len(data) >= 33, 'DBF header truncated')
    count, header_size, row_size = struct.unpack_from('<IHH', data, 4)
    check(header_size >= 33 and (header_size - 33) % 32 == 0 and len(data) >= header_size + count * row_size, 'DBF length mismatch')
    check(data[header_size - 1] == 13, 'DBF field terminator missing')
    fields = []
    for pos in range(32, header_size - 1, 32):
        descriptor = data[pos:pos + 32]
        name = descriptor[:11].split(b'\0')[0].decode(encoding)
        check(chr(descriptor[11]) in ('C', 'N', 'F', 'D', 'L'), f'Unsupported DBF field type {descriptor[11]}')
        fields.append((name, descriptor[16]))
    check(len({f[0] for f in fields}) == len(fields), 'Duplicate DBF fields')
    check(sum(size for _, size in fields) + 1 == row_size, 'DBF row length mismatch')
    records = []
    for index in range(count):
        row = data[header_size + index * row_size:header_size + (index + 1) * row_size]
        check(row[0] in (32, 42), 'Invalid DBF record marker')
        if row[0] == 42:
            records.append(None)
            continue
        offset, record = 1, {}
        for name, size in fields:
            record[name] = row[offset:offset + size].decode(encoding).strip()
            offset += size
        records.append(record)
    return records


def read_shapes(data, shx=None):
    check(len(data) >= 100 and struct.unpack_from('>i', data)[0] == 9994, 'Invalid SHP header')
    check(struct.unpack_from('>i', data, 24)[0] * 2 == len(data), 'SHP file length mismatch')
    check(struct.unpack_from('<i', data, 28)[0] == 1000, 'Unsupported SHP version')
    header_kind = struct.unpack_from('<i', data, 32)[0]
    check(header_kind in (3, 5), f'Unsupported SHP geometry type {header_kind}; only 2D line/polygon supported')
    if shx is not None:
        check(len(shx) >= 100 and (len(shx) - 100) % 8 == 0 and struct.unpack_from('>i', shx, 24)[0] * 2 == len(shx), 'SHX length mismatch')
    result, position = [], 100
    while position < len(data):
        check(position + 12 <= len(data), 'SHP record header truncated')
        number, words = struct.unpack_from('>2i', data, position)
        check(number == len(result) + 1 and words >= 2 and position + 8 + words * 2 <= len(data), 'SHP record number/length invalid')
        if shx is not None:
            index = 100 + len(result) * 8
            check(index + 8 <= len(shx) and struct.unpack_from('>2i', shx, index) == (position // 2, words), 'SHP/SHX record index mismatch')
        record = data[position + 8:position + 8 + words * 2]
        position += 8 + words * 2
        kind = struct.unpack_from('<i', record)[0]
        if kind == 0:
            result.append((None, []))
            continue
        check(kind == header_kind and len(record) >= 44, 'SHP record geometry mismatch')
        parts_count, points_count = struct.unpack_from('<2i', record, 36)
        check(parts_count > 0 and points_count > 0 and len(record) == 44 + parts_count * 4 + points_count * 16, 'SHP geometry length mismatch')
        starts = list(struct.unpack_from(f'<{parts_count}i', record, 44))
        check(starts[0] == 0 and starts == sorted(set(starts)) and starts[-1] < points_count, 'SHP part offsets invalid')
        points = list(struct.iter_unpack('<2d', record[44 + parts_count * 4:]))
        check(all(math.isfinite(v) for p in points for v in p), 'Expected finite SHP coordinates')
        parts = [points[a:b] for a, b in zip(starts, starts[1:] + [points_count])]
        check(all(len(part) >= (2 if kind == 3 else 4) for part in parts), 'SHP ring/line too short')
        if kind == 5:
            check(all(part[0] == part[-1] for part in parts), 'SHP polygon ring is not closed')
        result.append(('LINESTRING' if kind == 3 else 'POLYGON', parts))
    if shx is not None:
        check(len(shx) == 100 + 8 * len(result), 'SHX extra records')
    return result


def open_archive(path):
    archive = ZipFile(path)
    members = archive.infolist()
    names = [m.filename for m in members]
    try:
        check(len(names) == len(set(names)), 'ZIP contains duplicate member names')
        check(len(names) <= 5000 and sum(m.file_size for m in members) <= 512 * 1024 * 1024, 'ZIP exceeds importer resource budget')
        check(all(not PurePosixPath(n).is_absolute() and '..' not in PurePosixPath(n).parts and '\\' not in n and ':' not in n for n in names), 'Unsafe ZIP member path')
        check(all(not (m.flag_bits & 1) for m in members), 'Encrypted ZIP unsupported')
        return archive
    except Exception:
        archive.close()
        raise


def layer_records(archive, basename):
    lookup = {PurePosixPath(n).name: n for n in archive.namelist() if not n.endswith('/')}
    check(len(lookup) == len([n for n in archive.namelist() if not n.endswith('/')]), 'Ambiguous layer basenames in ZIP')
    required = [basename + suffix for suffix in ('.shp', '.shx', '.dbf', '.cpg', '.prj')]
    check(all(name in lookup for name in required), f'Missing SHP companion for {basename}')
    encoding = archive.read(lookup[basename + '.cpg']).decode('ascii').strip().lower()
    check(encoding in ('euc-kr', 'cp949', 'utf-8', 'utf8', '949'), f'Unsupported DBF codepage {encoding}')
    if encoding == '949':
        encoding = 'cp949'
    attrs = read_dbf(archive.read(lookup[basename + '.dbf']), encoding)
    shapes = read_shapes(archive.read(lookup[basename + '.shp']), archive.read(lookup[basename + '.shx']))
    check(len(attrs) == len(shapes), f'{basename}: SHP/DBF record count mismatch')
    return list(zip(attrs, shapes))


def archive_metadata(path):
    with open_archive(path) as archive:
        prj_names = [n for n in archive.namelist() if n.lower().endswith('.prj')]
        wkts = {archive.read(n).decode('utf8').strip() for n in prj_names}
        check(len(wkts) == 1, 'SHP layer CRS definitions differ or PRJ missing')
        wkt = next(iter(wkts))
        check(re.search(r'UNIT\["Meter",\s*1\]\]$', wkt) is not None, 'SHP projection must explicitly use metres')
        boundary = layer_records(archive, 'N3A_H0010000')
        tiles = {attrs.get('도엽코드') for attrs, _ in boundary if attrs}
        check(len(tiles) == 1 and None not in tiles, 'SHP boundary lacks one unique tile code')
        points = [p for attrs, (_, parts) in boundary for ring in parts for p in ring]
        bounds = [min(p[0] for p in points), min(p[1] for p in points), max(p[0] for p in points), max(p[1] for p in points)]
        return {'tile': next(iter(tiles)), 'wkt': wkt, 'bounds': bounds,
                'layerCount': len([n for n in archive.namelist() if n.endswith('.shp')]),
                'archiveMembers': [{'name': i.filename, 'bytes': i.file_size, 'crc32': f'{i.CRC:08x}'} for i in archive.infolist()]}


def xml_metadata(path):
    """Recover only bounded, unique metadata fields; report malformed ISO XML.

    The supplied official ISO XML has mismatched tags. No repair is written and
    import requires an explicit review acknowledgement; PRJ/boundary also checked.
    """
    text = Path(path).read_text(encoding='utf8')
    warning = None
    try:
        ET.fromstring(text)
    except ET.ParseError as exc:
        warning = f'Original ISO metadata XML is malformed ({exc}); narrow text fields recovered and checked against SHP/PRJ'

    def unique(pattern, label):
        matches = set(re.findall(pattern, text, re.S))
        check(len(matches) == 1, f'ISO XML must contain one unambiguous {label}')
        return next(iter(matches)).strip()

    tile = unique(r'도엽번호\s*:\s*(\d+)', 'tile ID')
    year = unique(r'제작년도\s*:\s*(\d{4})', 'production year')
    scale = unique(r'<gmd:denominator>\s*<gco:Integer>(\d+)</gco:Integer>\s*</gmd:denominator>', 'scale denominator')
    crs = unique(r'<gmd:code>\s*<gco:CharacterString>(EPSG:\d+)</gco:CharacterString>\s*</gmd:code>', 'CRS code')
    return {'도엽번호': tile, '제작년도': year, '축척코드': '1:' + scale, '좌표계구분코드': crs}, warning


def parse_archive(path, tile):
    features = []
    with open_archive(path) as archive:
        basenames = ['N3A_H0010000', 'N3L_A0020000', 'N3A_B0010000']
        if any(PurePosixPath(n).name == 'N3A_E0052114.shp' for n in archive.namelist()):
            basenames.append('N3A_E0052114')
        for basename in basenames:
            layer = basename.split('_', 1)[1]
            ufids = set()
            for index, (attrs, (kind, parts)) in enumerate(layer_records(archive, basename), 1):
                if attrs is None or kind is None:
                    continue
                ufid = attrs.get('UFID', '')
                check(ufid and ufid not in ufids, f'{tile}/{layer}: empty or duplicate UFID')
                ufids.add(ufid)
                check(kind != 'LINESTRING' or len(parts) == 1, f'{tile}/{layer}/{index}: multipart road requires explicit review')
                if layer == 'A0020000':
                    attrs['도로명'] = attrs.get('명칭', '')
                features.append({'id': f'{tile}:{layer}:{index}', 'record': str(index), 'tile': tile,
                                 'layer': layer, 'kind': kind, 'parts': parts, 'attributes': attrs})
    return features
