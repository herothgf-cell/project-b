import sys
import unittest
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'tools'))
from map_geometry import clip_segment, triangulate, densify

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

    def test_densify_preserves_endpoints(self):
        points=densify([(0,0),(31,0)])
        self.assertEqual(points[0],(0,0))
        self.assertEqual(points[-1],(31,0))
        self.assertEqual(len(points),4)

if __name__=='__main__':unittest.main()
