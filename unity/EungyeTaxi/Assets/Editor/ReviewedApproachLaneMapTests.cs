using System;
using System.Linq;
using UnityEngine;

public static class ReviewedApproachLaneMapTests {
 static int checks;
 static void Check(bool ok,string reason){checks++;if(!ok)throw new Exception("REVIEWED_APPROACH_LANE_MAP_TEST: "+reason);}
 public static void Verify(ProjectMapPackage source,ProjectMapPackage candidate){
  Check(candidate.baselineHash==source.baselineHash,"source baseline hash remains exact");
  Check(ReviewedApproachLaneMap.ProtectedContent(source.document)==ReviewedApproachLaneMap.ProtectedContent(candidate.document),"all source lanes, routing, signals, scene and unrelated facilities remain exact");
  var map=candidate.Compose();var selected=map.editorDocument.facilities.Where(f=>f.id.StartsWith(ReviewedApproachLaneMap.Prefix)).ToArray();
  Check(selected.Length==32&&selected.Select(f=>f.laneId).Distinct().Count()==32,"32 distinct native approach pieces are explicitly selected");
  Check(selected.All(f=>f.kind=="lanePresentation"&&!f.rule&&f.evidence.appearanceConfirmed&&!f.evidence.ruleConfirmed&&f.evidence.month=="2026-05"&&f.evidence.checkedAt=="2026-11-09"),"appearance evidence does not create U-turn permissions or conditions");
  Check(ReviewedNativeLanePresentation.ValidateAssignments(map,map.editorDocument).Count==0,"all selected native intervals and approaches validate");
  var arrows=LaneDirectionPaint.Layout(map);var presentation=new ReviewedNativeLanePresentation(map);var clearances=RoadJunctionGeometry.PaintClearances(map);
  for(int approach=0;approach<ReviewedApproachLaneMap.Lanes.Length;approach++){
   var ids=ReviewedApproachLaneMap.Lanes[approach];
   Check(arrows.Count(a=>ids.Contains(map.hdEdges[a.edgeIndex].id))==4,"one full-role glyph station per physical lane on approach "+approach);
   for(int lane=0;lane<4;lane++){
    var matches=arrows.Where(a=>map.hdEdges[a.edgeIndex].id==ids[lane]).ToArray();
    Check(matches.Length==1&&matches[0].turns==ReviewedApproachLaneMap.Masks[approach][lane],"actual active graph role mask "+ids[lane]);
    Check(LaneDirectionPaint.Clear(matches[0],clearances),"glyph footprint stays outside stop/crosswalk geometry "+ids[lane]);
    if(lane==0){var glyphs=LaneDirectionPaint.Glyphs(matches[0]);Check(glyphs.Count==2&&glyphs[0].turns==LaneDirectionPaint.Left&&glyphs[1].turns==LaneDirectionPaint.Uturn&&Mathf.Abs(Vector3.Distance(glyphs[0].centre,glyphs[1].centre)-LaneDirectionPaint.GlyphSpacing)<.01f,"left and U-turn are separate longitudinal glyphs "+ids[lane]);}
   }
   foreach(string id in ids){
    var edge=map.hdEdges.Single(e=>e.id==id);var path=new AssistedRoadNetwork.Path(edge.points.Select(p=>p.V()).ToArray());var f=selected.Single(s=>s.laneId==id);
    Check(f.startAt==0&&Mathf.Abs(f.endAt-path.length)<.01f,"full existing native piece is selected without geometry extension "+id);
    Check(presentation.SuppressesDivider(path.Position(path.length*.5f),path.Tangent(path.length*.5f)),"selected interval replaces stray synthetic divider "+id);
   }
  }
  Check(!arrows.Any(a=>selected.Any(f=>f.laneId==map.hdEdges[a.edgeIndex].id)&&!ReviewedApproachLaneMap.Lanes.Any(ids=>ids.Take(4).Contains(map.hdEdges[a.edgeIndex].id))),"terminal coverage never paints a second or narrower role glyph");
 }
 public static void Run(){
  checks=0;var source=ProjectMapStore.Load(ProjectMapBuild.Root);string original=source.document.ToJson();var candidate=ReviewedApproachLaneMap.Draft(source);Verify(source,candidate);
  Check(source.document.ToJson()==original,"draft preparation never mutates or saves its source package");
  var again=ReviewedApproachLaneMap.Draft(candidate);Verify(candidate,again);
  string a=JsonUtility.ToJson(new RoadMapDocument{id="",facilities=candidate.document.facilities.Where(f=>f.id.StartsWith(ReviewedApproachLaneMap.Prefix)).ToArray()});
  string b=JsonUtility.ToJson(new RoadMapDocument{id="",facilities=again.document.facilities.Where(f=>f.id.StartsWith(ReviewedApproachLaneMap.Prefix)).ToArray()});
  Check(a==b,"repeating authoring produces identical assignments without duplicates");
  Debug.Log("REVIEWED_APPROACH_LANE_MAP_TEST_OK checks="+checks);
 }
}
