using System;
using System.Linq;
using UnityEngine;

// Editor check for the explicit NGII paint suppression list.
public static class ReviewedMarkSuppressionTests {
 static int checks;
 static void Check(bool value,string text){checks++;if(!value)throw new InvalidOperationException("REVIEWED_MARK_SUPPRESSION_FAIL "+text);}
 // Suppression plus the existing marking/corridor/lane presentation regressions.
 public static void RunWithRegressions(){Run();ImportedMarkingRendererTests.Run();RoadviewCorridorTests.Run();LaneFollowupBuild.TraceCheck();RoadPresentationRevisionChecks.Navigation();RoadPresentationRevisionChecks.CompiledUturn();RoadPresentationRevisionChecks.AuthorizedUturnVisibility();Debug.Log("REVIEWED_MARK_SUPPRESSION_REGRESSIONS_OK");}
 public static void Run(){
  checks=0;var map=ProjectMapStore.Load(ProjectMapBuild.Root).Compose();
  Check(ReviewedMarkSuppression.J7973North.Distinct().Count()==ReviewedMarkSuppression.J7973North.Length,"ids are unique");
  foreach(var id in ReviewedMarkSuppression.J7973North){
   var mark=Array.Find(map.hdMarks,m=>m.id==id);Check(mark!=null,"source mark still exists "+id);
   Check(mark.kind!="525"&&mark.kind!="530"||id=="B2218G610884","junction guides and main stop line stay painted "+id);
   // Every hidden mark lies inside the J7973 north stem, never in the junction box or another road.
   Check(mark.points.All(p=>p.x>=780&&p.x<=818&&p.z>=392&&p.z<=413),"mark lies in J7973 north stem "+id);
   Check(ReviewedMarkSuppression.Hides(map,mark),"eungye map hides "+id);
  }
  // Replacement paint remains: the driven lanes cover the stem from the stop line northward.
  foreach(var id in new[]{"local:8017:3985:8089:6162","local:8074:3981:8146:6160","local:8131:3977:8203:6159"}){
   var lane=Array.Find(map.hdEdges,e=>e.id==id);Check(lane!=null&&!lane.disabled,"northbound driven lane present "+id);
  }
  Check(Array.Find(map.hdMarks,m=>m.id=="B2218G610880")!=null&&!ReviewedMarkSuppression.Hides(map,Array.Find(map.hdMarks,m=>m.id=="B2218G610880")),"main stop line is not in the list");
  var other=new MapData{mapId="pvp-city-a"};Check(!ReviewedMarkSuppression.Hides(other,map.hdMarks[0]),"other maps unaffected");
  Debug.Log("REVIEWED_MARK_SUPPRESSION_TESTS_OK checks="+checks);
 }
}
