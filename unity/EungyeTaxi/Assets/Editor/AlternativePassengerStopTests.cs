using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
public static class AlternativePassengerStopTests {
 static int checks;
 static void Check(bool ok,string reason){checks++;if(!ok)throw new Exception("ALTERNATIVE_PASSENGER_STOP: "+reason);}
 static MapData Load(){return ProjectMapStore.Load(ProjectMapBuild.Root).Compose();}
 static AlternativePassengerStopDefinition Definition(){return JsonUtility.FromJson<AlternativePassengerStopData>(Resources.Load<TextAsset>("eungye-passenger-alternatives").text).pairs[0];}
 static bool Review(PassengerStopCatalog catalog,MapData map,AlternativePassengerStopDefinition definition,out string reason){
  // Exercise the real admission implementation on a fresh composed map,
  // before its fictional asphalt/sidewalk overlays have been installed.
  var pendingCatalog=new PassengerStopCatalog();pendingCatalog.Entries.AddRange(catalog.Entries);
  typeof(PassengerStopCatalog).GetProperty("Stops").SetValue(pendingCatalog,catalog.Stops,null);
  var geometryType=typeof(PassengerStopCatalog).GetNestedType("Geometry",BindingFlags.NonPublic);
  var geometry=Activator.CreateInstance(geometryType,new object[]{map});
  var method=typeof(PassengerStopCatalog).GetMethod("ValidateAlternative",BindingFlags.NonPublic|BindingFlags.Instance);
  object[] arguments={map,new HdRoadGraph(map),geometry,definition,null,null};
  bool admitted=(bool)method.Invoke(pendingCatalog,arguments);reason=(string)arguments[5];return admitted;
 }
 public static void Run(){
  checks=0;string fingerprint=ProjectMapStore.Fingerprint(ProjectMapBuild.Root);var map=Load();int originalRoads=map.roads.Length;var catalog=PassengerStopCatalog.For(map);
  Check(catalog.LegacyCount==12&&map.stops.Length==12&&catalog.Stops.Length==24,"ordinary24 indices unchanged");
  var alternative=catalog.AlternativeFor("EG-GAME-02");
  Check(alternative!=null,"at least one validated physical bay+walking pair admitted; "+string.Join(" / ",catalog.AlternativeRejected.ToArray()));
  Check(catalog.Alternatives.Count()==1&&catalog.AlternativeRejected.Count==0,"one explicitly authored pair only");
  Check(alternative.WalkingMetres>49&&alternative.WalkingMetres<51&&alternative.WalkingMetres<=150,"bounded actual50m walking distance");
  Check(catalog.IndexOf(alternative.Id)<0,"alternatives never become new offers or change stop indices");
  Check(alternative.EvidenceStatus=="game-only-field-unverified","no real roadside legality claim");
  Check(catalog.AlternativeFor(null)==null&&catalog.AlternativeFor("S01")==null,"unreviewed originals have no invented alternative");
  Check(map.roads.Length==originalRoads+12+1,"separate visible asphalt pocket installed");
  Check(alternative.Stop.CanStop(alternative.Stop.Kerb,alternative.Stop.Direction,0)&&!alternative.Stop.CanStop(alternative.Stop.Kerb,-alternative.Stop.Direction,0),"actual directional bay interaction");
  Check(!ProjectMapScene.OnArea(map,alternative.Stop.Kerb,true),"alternative car footprint stays off raised sidewalk");
  var graph=new HdRoadGraph(map);foreach(var target in catalog.Stops){float distance;Check(PassengerStopCatalog.Reachable(graph,target,alternative.Stop,out distance),"directed inward route from ordinary stop");Check(PassengerStopCatalog.Reachable(graph,alternative.Stop,target,out distance),"directed outward route to ordinary stop");}
  float measured=0;for(int i=1;i<alternative.WalkingPath.Length;i++){
   var a=alternative.WalkingPath[i-1];var b=alternative.WalkingPath[i];var direction=(b-a).normalized;measured+=Vector3.Distance(a,b);int count=Mathf.CeilToInt(Vector3.Distance(a,b)/.5f);
   for(int n=0;n<=count;n++)foreach(float side in new[]{-.7f,0,.7f})Check(ProjectMapScene.OnArea(map,Vector3.Lerp(a,b,n/(float)count)+TrafficSystem.Right(direction)*side,true),"continuous installed1.5m pedestrian corridor");
  }
  Check(Mathf.Abs(measured-alternative.WalkingMetres)<.01f,"walking estimate measures actual polyline");
  int roads=map.roads.Length,areas=map.areas.Length;PassengerStopCatalog.Apply(map);Check(map.roads.Length==roads&&map.areas.Length==areas&&ReferenceEquals(alternative,catalog.AlternativeFor("EG-GAME-02")),"repeat access does not duplicate bay or reroll identity");
  string reason;Check(Review(catalog,Load(),Definition(),out reason),"same strict reviewer admits unmodified canonical composition: "+reason);
  var invalid=Definition();invalid.bay.evidenceStatus="real-approved";Check(!Review(catalog,Load(),invalid,out reason),"reject invented real legality evidence");
  invalid=Definition();invalid.walkingPath[1].x+=4;Check(!Review(catalog,Load(),invalid,out reason),"reject walking endpoint mismatch");
  var blocked=Load();blocked.buildings=blocked.buildings.Concat(new[]{new MapBuilding{id="alternative-bay-blocker",height=4,points=ProjectMapScene.Rectangle(alternative.Stop.Kerb,alternative.Stop.Direction,4,4)}}).ToArray();Check(!Review(catalog,blocked,Definition(),out reason)&&reason.Contains("overlap"),"new bay polygon obstacle fails closed: "+reason);
  var middle=(alternative.WalkingPath[0]+alternative.WalkingPath.Last())*.5f;
  blocked=Load();blocked.buildings=blocked.buildings.Concat(new[]{new MapBuilding{id="alternative-walking-blocker",height=4,points=ProjectMapScene.Rectangle(middle,alternative.Stop.Direction,2,2)}}).ToArray();Check(!Review(catalog,blocked,Definition(),out reason)&&reason.Contains("walking"),"blocked pedestrian corridor fails closed: "+reason);
  var crossing=Load();var cross=TrafficSystem.Right(alternative.Stop.Direction);crossing.roads=crossing.roads.Concat(new[]{new MapRoad{id="alternative-narrow-asphalt",width=.2f,points=new[]{new MapPoint{x=middle.x-cross.x*3,z=middle.z-cross.z*3},new MapPoint{x=middle.x+cross.x*3,z=middle.z+cross.z*3}}}}).ToArray();Check(!Review(catalog,crossing,Definition(),out reason)&&reason.Contains("asphalt"),"exact polygon check rejects even narrow asphalt crossing: "+reason);
  var disconnected=Load();int edge=Array.FindIndex(disconnected.hdEdges,e=>e.id==Definition().bay.laneId);disconnected.hdEdges[edge].next=new int[0];Check(!Review(catalog,disconnected,Definition(),out reason)&&reason.Contains("disconnected"),"removed directed exit fails closed: "+reason);
  var disabled=Load();disabled.hdEdges[edge].disabled=true;Check(!Review(catalog,disabled,Definition(),out reason)&&reason.Contains("disabled"),"disabled approach fails closed: "+reason);
  Check(ProjectMapStore.Fingerprint(ProjectMapBuild.Root)==fingerprint,"canonical project file/hash untouched");
  PassengerStopCatalogTests.Run();
  Debug.Log("ALTERNATIVE_PASSENGER_STOP_OK checks="+checks+" pair="+alternative.OriginalStopId+"->"+alternative.Id+" walk_m="+alternative.WalkingMetres.ToString("F3")+" ordinary="+catalog.Stops.Length);
 }
}
