import struct
import json
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'tools'))
from ngii_shapefile import read_dbf, read_shapes, archive_metadata
from import_ngii_map import build_map, components, ImportFailure


def shape_bytes(kind=3, points=((0., 0.), (10., 10.))):
    body = struct.pack('<i4d2i', kind, 0, 0, 10, 10, 1, len(points)) + struct.pack('<i', 0)
    body += b''.join(struct.pack('<2d', *p) for p in points)
    header = struct.pack('>7i', 9994, 0, 0, 0, 0, 0, (108 + len(body)) // 2)
    header += struct.pack('<2i8d', 1000, kind, 0, 0, 10, 10, 0, 0, 0, 0)
    return header + struct.pack('>2i', 1, len(body) // 2) + body


class ShapefileTests(unittest.TestCase):
    def test_shape_records_validate_and_preserve_coordinates(self):
        self.assertEqual(read_shapes(shape_bytes())[0], ('LINESTRING', [[(0., 0.), (10., 10.)]]))

    def test_truncated_shape_fails(self):
        with self.assertRaisesRegex(ValueError, 'length'):
            read_shapes(shape_bytes()[:-1])

    def test_nonfinite_shape_fails(self):
        with self.assertRaisesRegex(ValueError, 'finite'):
            read_shapes(shape_bytes(points=((0., 0.), (float('nan'), 1.))))

    def test_dbf_euckr_header_and_values(self):
        head = bytearray(32)
        head[0] = 3
        struct.pack_into('<IHH', head, 4, 1, 65, 11)
        field = bytearray(32)
        field[:len('명칭'.encode('euc-kr'))] = '명칭'.encode('euc-kr')
        field[11], field[16] = ord('C'), 10
        data = bytes(head + field) + b'\r ' + '은계'.encode('euc-kr').ljust(10, b' ') + b'\x1a'
        self.assertEqual(read_dbf(data, 'euc-kr'), [{'명칭': '은계'}])

    def test_real_eungye_archives_have_matching_explicit_projection(self):
        files = list((ROOT / 'data/raw/ngii/eungye').glob('*.zip'))
        if len(files) < 2:
            self.skipTest('User-provided Eungye archives absent')
        metadata = [archive_metadata(p) for p in sorted(files)]
        self.assertEqual({m['tile'] for m in metadata}, {'37612022', '37612023'})
        self.assertEqual(len({m['wkt'] for m in metadata}), 1)
        self.assertIn('Korea_2000_Central_Belt_2010', metadata[0]['wkt'])

    def test_real_eungye_export_has_connected_stops_source_junctions_and_lake(self):
        raw = ROOT / 'data/raw/ngii/eungye'
        if len(list(raw.glob('*.zip'))) < 2:
            self.skipTest('User-provided Eungye archives absent')
        cfg = json.loads((ROOT / 'config/maps/eungye.json').read_text(encoding='utf8'))
        result, report = build_map(cfg, raw)
        self.assertEqual(result['coordinateStatus'], 'verified')
        self.assertEqual({s['tile'] for s in result['sources']}, {'37612022', '37612023'})
        self.assertEqual((result['width'], result['depth']), (2100, 1750))
        self.assertGreater(len(result['roads']), 500)
        self.assertGreater(len(result['buildings']), 3000)
        self.assertEqual(len({r['id'] for r in result['roads']}), len(result['roads']))
        self.assertTrue(all(r['oneWay'] == 0 and r['width'] >= 6 for r in result['roads']))
        self.assertTrue(any('malformed' in w for w in report['warnings']))
        self.assertTrue(any('one-way' in r['reason'] for r in report['skippedRoads']))
        graph, groups = components(result['roads'])
        key = lambda p: (round(p['x'], 3), round(p['z'], 3))
        for target in result['targets']:
            self.assertGreaterEqual(len(graph[key(target)]), 4)
        self.assertEqual(len(result['stops']), 8)
        # All exported edges are bidirectional. Verify all 56 ordered routes,
        # not just geometrical proximity to a road or an arbitrary component.
        for start in result['stops']:
            reached = {key(start)}
            pending = [key(start)]
            while pending:
                for node in graph[pending.pop()]:
                    if node not in reached:
                        reached.add(node)
                        pending.append(node)
            self.assertTrue(all(key(stop) in reached for stop in result['stops']))
            self.assertTrue(all(key(target) in reached for target in result['targets']))
        lake = result['areas'][0]
        self.assertEqual(lake['name'], '소래저수지')
        self.assertEqual(lake['kind'], 'water')
        p = lake['points']
        triangle_area = 0
        for i in range(0, len(lake['triangles']), 3):
            a, b, c = [p[v] for v in lake['triangles'][i:i + 3]]
            triangle_area += abs((b['x'] - a['x']) * (c['z'] - a['z']) - (b['z'] - a['z']) * (c['x'] - a['x'])) / 2
        polygon_area = abs(sum(a['x'] * b['z'] - b['x'] * a['z'] for a, b in zip(p, p[1:] + p[:1]))) / 2
        self.assertAlmostEqual(triangle_area, polygon_area, places=6)
        # The DBF rounded survey area differs from the supplied millimetre
        # polygon vertices by 0.058m²; test preservation against the geometry.
        self.assertAlmostEqual(triangle_area, 72192.29, delta=.1)

    def test_shp_import_rejects_configured_crs_disagreement(self):
        raw = ROOT / 'data/raw/ngii/eungye'
        if len(list(raw.glob('*.zip'))) < 2:
            self.skipTest('User-provided Eungye archives absent')
        cfg = json.loads((ROOT / 'config/maps/eungye.json').read_text(encoding='utf8'))
        for item in cfg['requiredTiles']:
            item['crs']['definition'] = 'EPSG:5179'
        with self.assertRaisesRegex(ImportFailure, 'disagrees'):
            build_map(cfg, raw)


if __name__ == '__main__':
    unittest.main()
