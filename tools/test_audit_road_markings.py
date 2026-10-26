import unittest

from audit_road_markings import audit


class AuditRoadMarkingsTests(unittest.TestCase):
    def test_reverse_duplicate_and_parallel_safety_boundaries_are_distinct(self):
        marks = [
            {"id": "a", "type": "111", "kind": "501", "points": [{"x": 0, "z": 2}, {"x": 10, "z": 2}]},
            {"id": "a-copy", "type": "111", "kind": "501", "points": [{"x": 10, "z": 2}, {"x": 0, "z": 2}]},
            {"id": "b", "type": "111", "kind": "501", "points": [{"x": 0, "z": -2}, {"x": 10, "z": -2}]},
        ]
        report = audit({"roads": [], "hdEdges": [], "hdMarks": marks})
        self.assertEqual(report["exactDuplicatePairs"], [["a", "a-copy"]])
        self.assertEqual(report["markRoles"]["parallel_501_candidate"], 2)
        self.assertEqual(report["markRoles"].get("central_safety_boundary", 0), 0)
        self.assertEqual(report["markRoles"].get("road_edge", 0), 0)

    def test_catalog_evidence_promotes_only_named_pair_to_confirmed_strip(self):
        marks = [
            {"id": "north", "type": "111", "kind": "501", "points": [{"x": 0, "z": 2}, {"x": 10, "z": 2}]},
            {"id": "south", "type": "111", "kind": "501", "points": [{"x": 0, "z": -2}, {"x": 10, "z": -2}]},
            {"id": "edge", "type": "111", "kind": "505", "points": [{"x": 0, "z": 8}, {"x": 10, "z": 8}]},
        ]
        catalog = {"facilities": [{"kind": "paired-median", "sourceUrl": "https://example.test/panorama", "northMarkIds": ["north"], "southMarkIds": ["south"]}]}
        report = audit({"roads": [], "hdEdges": [], "hdMarks": marks}, catalog)
        self.assertEqual(report["markRoles"]["central_safety_boundary"], 2)
        self.assertEqual(report["markRoles"]["road_edge"], 1)

    def test_generated_centre_sample_reports_near_imported_line_without_folding_roles(self):
        def lane(name, z, reverse=False):
            points = [{"x": 10, "z": z}, {"x": 0, "z": z}] if reverse else [{"x": 0, "z": z}, {"x": 10, "z": z}]
            return {"id": name, "sourceId": "road", "status": "provisional", "number": 1 if not reverse else 2, "turn": 0, "change": 0, "points": points}
        marks = [{"id": "imported", "type": "111", "kind": "501", "points": [{"x": 0, "z": 0}, {"x": 10, "z": 0}]}]
        report = audit({"roads": [], "hdEdges": [lane("out", 2), lane("back", -2, True)], "hdMarks": marks})
        self.assertEqual(report["generatedVsImported"]["coincidentSamples"], 1)
        self.assertEqual(report["markRoles"]["centerline"], 1)


if __name__ == "__main__":
    unittest.main()
