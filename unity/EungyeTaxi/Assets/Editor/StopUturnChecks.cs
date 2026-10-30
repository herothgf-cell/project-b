using System;
using System.Linq;
using UnityEngine;

public static class StopUturnChecks {
 static void Check(bool ok,string reason){if(!ok)throw new Exception("STOP_UTURN: "+reason);}
 public static void Map(){
  var p=ProjectMapStore.Load(ProjectMapBuild.Root);var map=p.Compose();var zones=ReviewedCrosswalkGeometry.Build(map);
  foreach(string name in new[]{"J8066-S","J7973-E"}){
   var join=p.document.lanes.Single(l=>l.id=="join:reviewed:"+name+":uturn");var from=p.document.lanes.Single(l=>!l.disabled&&l.next.Contains(join.id));var to=p.document.lanes.Single(l=>l.id==join.next[0]);
   Check(UturnMovement.Condition(p.document,from.id)!=null,name+" exact approach has condition");Check(UturnMovement.GeometryError(join.points.Select(q=>q.V()).ToArray(),(from.points.Last().V()-from.points[from.points.Length-2].V()).normalized,(to.points[1].V()-to.points[0].V()).normalized)==null,name+" curvature/end tangents");
   var crossing=zones.Where(z=>Vector3.Distance(z.junction,map.junctionRules.First(j=>j.id==join.ruleId).V())<.1f).ToArray();
   for(int i=1;i<join.points.Length;i++){var q=join.points[i].V();var f=(q-join.points[i-1].V()).normalized;foreach(float along in new[]{-2.3f,0,2.3f})foreach(float side in new[]{-1.05f,1.05f})Check(!crossing.Any(z=>z.InBand(q+f*along+TrafficSystem.Right(f)*side,.15f)),name+" complete vehicle stays outside crossing "+i);}
   Debug.Log("STOP_UTURN_CLEAR "+name+" start="+from.points.Last().V()+" end="+to.points[0].V());
  }
  Check(p.document.scene.objects.Count(o=>o.enabled&&o.id=="reviewed:2026-10-30:J7973-SE-island"&&o.kind=="sidewalk")==1,"single authored J7973 island");
  var island=p.document.scene.objects.Single(o=>o.id=="reviewed:2026-10-30:J7973-SE-island");ProjectMapScene.Triangulate(island.points);
  foreach(var l in p.document.lanes.Where(l=>!l.disabled)){
   var path=new AssistedRoadNetwork.Path(l.points.Select(q=>q.V()).ToArray());float err;path.Project(new Vector3(813,0,356),out err);if(err>20)continue;
   for(float at=0;at<path.length;at+=.5f){var q=path.Position(at);var f=path.Tangent(at);foreach(float along in new[]{-2.3f,2.3f})foreach(float side in new[]{-1.05f,1.05f})Check(!RoadMapRules.Polygon(island.points,q+f*along+TrafficSystem.Right(f)*side),"vehicle corridor avoids island "+l.id);}
  }
  foreach(string id in new[]{"J1927_5181","J7973_3744","J8066_6364"})foreach(var group in p.document.facilities.Where(f=>f.junctionId==id&&f.kind=="stopLine").GroupBy(f=>f.axis+":"+f.sign))Check(group.Count()==1,"one canonical stop per approach "+id+" "+group.Key);
  AdditionalReviewedUturnMap.Validate();Debug.Log("STOP_UTURN_MAP_CHECKS_OK");
 }
 public static void SourceStopInventory(){var map=ProjectMapStore.Load(ProjectMapBuild.Root).Compose();var hd=new HdRoadGraph(map);foreach(var c in PrototypeScenario.Build(hd,map.stops)){int k=c.route.edges.IndexOf(c.connector);var path=hd.paths[c.route.edges[k-1]];var q=path.Position(path.length);var f=path.Tangent(path.length);Debug.Log("SOURCE_STOP_COURSE "+c.id+" variant="+c.variant+" junction="+hd.edges[c.connector].ruleId+" laneEnd="+q+" heading="+f+" incoming="+hd.edges[c.route.edges[k-1]].id);}}
 public static void Build(){StopUturnMapEdits.CorrectMetadata();ProjectMapBuild.Prepare();Map();TrafficReservationLifecycleTests.Run();UpstreamUturnObserverTests.Run();AdditionalUturnObserverTests.Run();WorldFollowupChecks.Run();PrototypeBuild.BuildHybrid();}
 public static void ApplyAndBuild(){StopUturnMapEdits.Apply();Build();}
}
