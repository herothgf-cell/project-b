import sys
import unittest
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'tools'))
from export_drive_map import clip_segment, triangulate, export_map

class MapTests(unittest.TestCase):
    def test_segment_crossing_box_with_both_ends_outside(self):
        self.assertEqual(clip_segment((-5,5),(15,5),(0,0,10,10)),((0.,5.),(10.,5.)))

    def test_outside_segment_is_discarded(self):
        self.assertIsNone(clip_segment((-5,-1),(15,-1),(0,0,10,10)))

    def test_concave_roof_preserves_area(self):
        p=[(0,0),(4,0),(4,1),(1,1),(1,4),(0,4)]
        t=triangulate(p)
        self.assertEqual(len(t),12)
        area=0
        for i in range(0,len(t),3):
            a,b,c=[p[t[i+j]] for j in range(3)]
            area+=abs((b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0]))/2
        self.assertAlmostEqual(area,7)

    def test_real_map_has_targets_and_bounded_geometry(self):
        m=export_map()
        self.assertGreater(len(m['roads']),10)
        self.assertGreater(len(m['buildings']),10)
        self.assertEqual([t['name'] for t in m['targets']],['T1','T2','T3'])
        self.assertGreater(m['width']*m['depth'],240000)
        self.assertTrue(any(r['sourceLanes']==4 for r in m['roads']))
        self.assertGreaterEqual(len(m['stops']),6)
        for road in m['roads']:
            self.assertGreater(road['width'],0)
            for p in road['points']:
                self.assertTrue(-.001<=p['x']<=m['width']+.001 and -.001<=p['z']<=m['depth']+.001)
        for b in m['buildings']:
            self.assertGreater(len(b['triangles']),0)
            self.assertTrue(all(0<=i<len(b['points']) for i in b['triangles']))

    def test_passenger_positions_clear_buildings(self):
        m=export_map()
        def inside(x,z,poly):
            hit=False
            for a,b in zip(poly,poly[1:]+poly[:1]):
                if (a['z']>z)!=(b['z']>z) and x<(b['x']-a['x'])*(z-a['z'])/(b['z']-a['z'])+a['x']:hit=not hit
            return hit
        for stop in m['stops']:
            offset=stop['width']*.5+.85
            x=stop['x']+stop['dz']*offset;z=stop['z']-stop['dx']*offset
            self.assertFalse(any(inside(x,z,b['points']) for b in m['buildings']),stop['name'])

    def test_source_lane_counts_are_preserved(self):
        import json
        source=json.loads((Path(__file__).resolve().parents[1]/'data/inspection/ngii/native-features.json').read_text(encoding='utf8'))
        by_id={f['id']:int(f['attributes']['차로수']) for f in source if f['layer']=='A0020000'}
        for road in export_map()['roads']:
            self.assertEqual(road['sourceLanes'],by_id[road['id']])

if __name__=='__main__':unittest.main()
