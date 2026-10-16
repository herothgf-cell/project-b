import sys, unittest
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'tools'))
import import_hdmap as hd

class HdMapTests(unittest.TestCase):
    def test_only_matching_node_and_position_connect(self):
        a={'id':'a','from':'1','to':'2','points':[(0,0),(10,0)]}
        b={'id':'b','from':'2','to':'3','points':[(10,0),(20,0)]}
        c={'id':'c','from':'other','to':'4','points':[(10,0),(20,0)]}
        d={'id':'d','from':'2','to':'5','points':[(100,0),(110,0)]}
        self.assertEqual(hd.successors([a,b,c,d])[0],[1])
    def test_right_turn_terminal_is_valid_not_fabricated(self):
        edges=[{'id':'in','from':'a','to':'b','points':[(0,0),(10,0)]},
               {'id':'right','from':'b','to':'end','points':[(10,0),(12,-2),(12,-10)]}]
        self.assertEqual(hd.successors(edges),[[1],[]])
    def test_only_white_single_dashed_allows_change(self):
        self.assertTrue(hd.change_mark({'type':'212'}))
        for code in ['211','111','213','999','']:
            self.assertFalse(hd.change_mark({'type':code}))
    def test_slice_keeps_original_curve_and_endpoints(self):
        p=[(0,0),(10,0),(10,10)]
        self.assertEqual(hd.slice_line(p,5,15),[(5.,0.),(10,0),(10.,5.)])
    def test_no_reverse_connection(self):
        e=[{'id':'a','from':'1','to':'2','points':[(0,0),(10,0)]},
           {'id':'b','from':'2','to':'1','points':[(10,0),(0,0)]}]
        self.assertEqual(hd.successors(e),[[],[]])

if __name__=='__main__':unittest.main()
