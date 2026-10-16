import json
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'tools'))
from import_ngii_map import ImportFailure, inventory, build_map, main, inside


def layer(name, body, fields=''):
    return f'<LAYER_START>\n$LAYER_NAME\n"{name}"\n$END\n<HEADER>\n{fields}\n<END>\n<DATA>\n{body}\n<END>\n<LAYER_END>\n'


def package(folder, tile, x=0, datum='GRS80', duplicate=False):
    stem = folder / tile
    boundary = f'$RECORD 1\nLINESTRING\n5\n{x} 0\n{x+100} 0\n{x+100} 100\n{x} 100\n{x} 0'
    road = f'$RECORD 7\nLINESTRING\n2\n{x} 50\n{x+100} 50'
    if duplicate:
        road += '\n' + road
    stem.with_suffix('.ngi').write_text(layer('H0010000', boundary) + layer('A0020000', road), encoding='cp949')
    fields = '\n'.join(f'ATTRIB("{f}",STRING,256,0,TRUE)' for f in ['ID', '도로폭', '차로수', '일방통행', '도로명'])
    stem.with_suffix('.nda').write_text(layer('H0010000', '$RECORD 1\n1', 'ATTRIB("ID",NUMERIC,9,0,TRUE)') + layer('A0020000', '$RECORD 7\n7, 12, 2, "양방향", "시험로"', fields), encoding='cp949')
    stem.with_suffix('.xml').write_text(f'<메타><도엽번호>{tile}</도엽번호><좌표계구분코드>{datum}</좌표계구분코드><제작년도>2025</제작년도><축척코드>1:1,000</축척코드></메타>', encoding='utf8')


def config(*tiles, provisional=False):
    crs = {'status': 'provisional' if provisional else 'verified', 'definition': 'TEST:metre-grid', 'units': 'metre', 'evidence': 'Synthetic fixture with shared Cartesian coordinates'}
    return {'id': 'test', 'requiredTiles': [{'tile': t, 'crs': crs} for t in tiles], 'reviewedOverrides': []}


class ImportTests(unittest.TestCase):
    def setUp(self):
        temp_root = ROOT / 'data/inspection/test-temp'
        temp_root.mkdir(parents=True, exist_ok=True)
        self.temp = tempfile.TemporaryDirectory(dir=temp_root)
        self.addCleanup(self.temp.cleanup)
        self.folder = Path(self.temp.name)

    def test_adjacent_merge_keeps_duplicate_record_ids_and_shared_origin(self):
        package(self.folder, '100', 0)
        package(self.folder, '101', 100)
        result, report = build_map(config('100', '101'), self.folder)
        self.assertEqual([r['id'] for r in result['roads']], ['100:A0020000:7:0', '101:A0020000:7:0'])
        self.assertEqual(result['width'], 200)
        self.assertEqual(result['roads'][0]['points'][-1], result['roads'][1]['points'][0])
        self.assertEqual(len(result['sources']), 2)
        self.assertEqual(report['status'], 'passed')
        self.assertTrue(all(r['oneWay'] == 0 for r in result['roads']))

    def test_missing_tile_fails_with_tile_number(self):
        package(self.folder, '100')
        with self.assertRaisesRegex(ImportFailure, '101'):
            build_map(config('100', '101'), self.folder)

    def test_unknown_crs_cannot_merge(self):
        package(self.folder, '100')
        package(self.folder, '101', 100)
        with self.assertRaisesRegex(ImportFailure, 'CRS'):
            build_map(config('100', '101', provisional=True), self.folder)

    def test_mismatched_crs_cannot_merge(self):
        package(self.folder, '100')
        package(self.folder, '101', 100)
        cfg = config('100', '101')
        cfg['requiredTiles'][1]['crs'] = dict(cfg['requiredTiles'][1]['crs'], definition='TEST:different-grid')
        with self.assertRaisesRegex(ImportFailure, 'CRS'):
            build_map(cfg, self.folder)

    def test_metadata_datum_mismatch_fails(self):
        package(self.folder, '100')
        package(self.folder, '101', 100, datum='BESSEL')
        with self.assertRaisesRegex(ImportFailure, 'datum'):
            build_map(config('100', '101'), self.folder)

    def test_duplicate_within_layer_fails(self):
        package(self.folder, '100', duplicate=True)
        with self.assertRaisesRegex(ImportFailure, 'duplicate'):
            build_map(config('100'), self.folder)

    def test_incomplete_package_reported(self):
        package(self.folder, '100')
        (self.folder / '100.nda').unlink()
        found = inventory(self.folder)
        self.assertTrue(found[0]['errors'])
        with self.assertRaisesRegex(ImportFailure, 'nda'):
            build_map(config('100'), self.folder)

    def test_duplicate_package_requires_explicit_selection(self):
        package(self.folder, '100')
        second = self.folder / 'second'
        second.mkdir()
        package(second, '100')
        with self.assertRaisesRegex(ImportFailure, 'ambiguous'):
            build_map(config('100'), self.folder)
        cfg = config('100')
        cfg['requiredTiles'][0]['package'] = 'second/100.ngi'
        result, _ = build_map(cfg, self.folder)
        self.assertEqual(len(result['roads']), 1)

    def test_unreviewed_override_does_not_change_direction(self):
        package(self.folder, '100')
        cfg = config('100')
        cfg['reviewedOverrides'] = [{'feature': '100:A0020000:7', 'status': 'unverified', 'oneWay': 1, 'evidence': ''}]
        result, report = build_map(cfg, self.folder)
        self.assertEqual(result['roads'][0]['oneWay'], 0)
        self.assertEqual(len(report['ignoredOverrides']), 1)

    def test_verified_override_requires_evidence_and_matching_feature(self):
        package(self.folder, '100')
        cfg = config('100')
        cfg['reviewedOverrides'] = [{'feature': '100:A0020000:7', 'status': 'verified', 'oneWay': -1, 'evidence': 'Reviewed synthetic direction'}]
        result, _ = build_map(cfg, self.folder)
        self.assertEqual(result['roads'][0]['oneWay'], -1)
        cfg['reviewedOverrides'][0]['evidence'] = ''
        with self.assertRaisesRegex(ImportFailure, 'evidence'):
            build_map(cfg, self.folder)

    def test_malformed_coordinate_fails(self):
        package(self.folder, '100')
        path = self.folder / '100.ngi'
        path.write_text(path.read_text(encoding='cp949').replace('100 50', 'nan 50'), encoding='cp949')
        with self.assertRaisesRegex(ImportFailure, 'finite'):
            build_map(config('100'), self.folder)

    def test_disconnected_sheets_are_not_combined(self):
        package(self.folder, '100')
        package(self.folder, '101', 5000)
        with self.assertRaisesRegex(ImportFailure, 'not adjacent'):
            build_map(config('100', '101'), self.folder)

    def test_failed_cli_does_not_replace_existing_map(self):
        package(self.folder, '100')
        cfg = config('100', '101')
        output = self.folder / 'map.json'
        report = self.folder / 'report.json'
        cfg['output'] = str(output)
        cfg_path = self.folder / 'config.json'
        cfg_path.write_text(json.dumps(cfg), encoding='utf8')
        output.write_text('previous verified map', encoding='utf8')
        self.assertEqual(main(['build', '--config', str(cfg_path), '--raw', str(self.folder), '--report', str(report)]), 2)
        self.assertEqual(output.read_text(encoding='utf8'), 'previous verified map')
        self.assertEqual(json.loads(report.read_text())['status'], 'failed')

    def test_landmark_requires_review_and_can_use_source_building_location(self):
        package(self.folder, '100')
        cfg = config('100')
        cfg['landmarks'] = [{'name': 'Reviewed test point', 'sourceX': 25, 'sourceY': 75, 'kind': 'test', 'status': 'verified', 'evidence': 'Synthetic survey'}]
        result, _ = build_map(cfg, self.folder)
        self.assertEqual(result['landmarks'][0]['x'], 25)
        cfg['landmarks'][0]['status'] = 'unverified'
        result, report = build_map(cfg, self.folder)
        self.assertEqual(result['landmarks'], [])
        self.assertEqual(len(report['ignoredLandmarks']), 1)

    def test_unknown_direction_is_not_guessed(self):
        package(self.folder, '100')
        path = self.folder / '100.nda'
        path.write_text(path.read_text(encoding='cp949').replace('양방향', '일방통행'), encoding='cp949')
        with self.assertRaisesRegex(ImportFailure, 'unreviewed direction'):
            build_map(config('100'), self.folder)

    def test_real_package_parses_directly_without_inspection_cache(self):
        raw = ROOT / 'data/raw/ngii'
        if not list(raw.glob('*.ngi')):
            self.skipTest('User-provided raw NGII package absent')
        result, report = build_map(config('377092296', provisional=True), raw)
        self.assertEqual(report['sourceFeatureCount'], 1942)
        self.assertGreater(len(result['roads']), 100)
        self.assertGreater(len(result['buildings']), 100)
        self.assertEqual(result['coordinateStatus'], 'provisional')
        self.assertGreaterEqual(len(result['stops']), 6)
        self.assertTrue(all(r['sourceLanes'] in [1, 2, 4, 5] for r in result['roads']))
        self.assertEqual(len({r['id'] for r in result['roads']}), len(result['roads']))
        for stop in result['stops']:
            offset = stop['width'] * .5 + .85
            x, z = stop['x'] + stop['dz'] * offset, stop['z'] - stop['dx'] * offset
            self.assertFalse(any(inside(x, z, b['points']) for b in result['buildings']))


if __name__ == '__main__':
    unittest.main()
