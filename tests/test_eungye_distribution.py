import hashlib
import json
import unittest
import zipfile
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
class EungyeDistributionTests(unittest.TestCase):
    def test_project_and_import_paths_are_eungye_only(self):
        manifest=json.loads((ROOT/'config/maps/local-input-manifest.json').read_text(encoding='utf-8-sig'))
        self.assertEqual([Path(v['path']).name for v in manifest['inputs']],['eungye-map.json','route42-map.json'])
        self.assertTrue((ROOT/'unity/EungyeTaxi/ProjectSettings/ProjectVersion.txt').is_file())
        self.assertFalse((ROOT/'unity/EungyeTaxi/Assets/Resources/drive-map.json').exists())
        for item in manifest['inputs']:
            self.assertTrue(item['path'].startswith('unity/EungyeTaxi/'))
    def test_zip_payload_and_sidecar_match_actual_bytes(self):
        path=ROOT/'distribution/maps/Eungye-map-inputs-2026-10-20.zip'
        self.assertEqual(hashlib.sha256(path.read_bytes()).hexdigest(),path.with_suffix('.zip.sha256').read_text().split()[0])
        manifest=json.loads((ROOT/'config/maps/local-input-manifest.json').read_text(encoding='utf-8-sig'))
        with zipfile.ZipFile(path) as archive:
            self.assertEqual(set(archive.namelist()),{v['path'] for v in manifest['inputs']}|{'_handoff/local-input-manifest.json','_handoff/map-transfer-guide.md'})
            self.assertEqual(json.loads(archive.read('_handoff/local-input-manifest.json')),manifest)
            for item in manifest['inputs']:
                payload=archive.read(item['path'])
                self.assertEqual(len(payload),item['bytes'])
                self.assertEqual(hashlib.sha256(payload).hexdigest(),item['sha256'])
    def test_canonical_is_eungye_with_editable_scene(self):
        path=ROOT/'config/maps/project/eungye.project.json'
        package=json.loads(path.read_text(encoding='utf-8-sig'))
        self.assertEqual(package['baseline']['mapId'],'eungye')
        self.assertGreater(len(package['document']['scene']['objects']),100)
        self.assertEqual(len(package['contentHash']),64)

if __name__=='__main__':unittest.main()
