import importlib.util
from pathlib import Path
import unittest
spec=importlib.util.spec_from_file_location("supplement",Path(__file__).resolve().parents[1]/"tools/build_eungye_supplement.py")
m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
class SupplementTests(unittest.TestCase):
 def test_unknown_exit_is_not_forbidden(self):
  self.assertFalse(m.forbidden_turn('only_right_turn',None))
 def test_unmarked_and_missing_crossings_are_not_painted(self):
  self.assertFalse(m.paint_crossing({'crossing':'unmarked'}));self.assertFalse(m.paint_crossing({}))
  self.assertTrue(m.paint_crossing({'crossing':'marked'}))
 def test_aligned_road_matches(self):
  self.assertEqual(m.match_road([(0,0),(30,0)],[{'id':1,'points':[(0,1),(30,1)],'tags':{}}])['id'],1)
 def test_parallel_ambiguity_is_deferred(self):
  self.assertIsNone(m.match_road([(0,0),(30,0)],[{'id':1,'points':[(0,1),(30,1)],'tags':{}},{'id':2,'points':[(0,-1),(30,-1)],'tags':{}}]))
 def test_crossing_is_not_road_match(self):
  self.assertIsNone(m.match_road([(0,0),(30,0)],[{'id':1,'points':[(15,-20),(15,20)],'tags':{}}]))
 def test_oneway_follows_way_vertex_order(self):
  way={'points':[(0,0),(30,0)],'tags':{'oneway':'yes'}}
  self.assertTrue(m.reverse_to_oneway([(30,1),(0,1)],way));self.assertFalse(m.reverse_to_oneway([(0,1),(30,1)],way))
  way['tags']['oneway']='-1';self.assertTrue(m.reverse_to_oneway([(0,1),(30,1)],way))
 def test_only_rule_blocks_other_destinations(self):
  self.assertTrue(m.forbidden_turn('only_right_turn',False));self.assertFalse(m.forbidden_turn('only_right_turn',True))
  self.assertTrue(m.forbidden_turn('no_left_turn',True));self.assertFalse(m.forbidden_turn('no_left_turn',False))
if __name__=='__main__':unittest.main()
