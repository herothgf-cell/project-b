using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[Serializable] public sealed class AlternativePassengerStopDefinition {
 public string originalStopId;public PassengerStopCandidate bay;public MapPoint[] walkingPath;
}
[Serializable] public sealed class AlternativePassengerStopData {
 public int schema;public string mapId,revision;public AlternativePassengerStopDefinition[] pairs;
}
public sealed class AlternativePassengerStop {
 public string Id,OriginalStopId,EvidenceStatus;public MapStop Stop;public Vector3[] WalkingPath;public float WalkingMetres;
}
public sealed partial class PassengerStopCatalog {
 readonly Dictionary<string,AlternativePassengerStop> alternatives=new Dictionary<string,AlternativePassengerStop>();
 readonly List<PassengerStopCandidate> alternativeAdmitted=new List<PassengerStopCandidate>();
 public readonly List<string> AlternativeRejected=new List<string>();
 public IEnumerable<AlternativePassengerStop> Alternatives {get{return alternatives.Values;}}
 public AlternativePassengerStop AlternativeFor(string originalStopId){AlternativePassengerStop result;return originalStopId!=null&&alternatives.TryGetValue(originalStopId,out result)?result:null;}
 void InstallAlternatives(MapData map,HdRoadGraph graph){
  var asset=Resources.Load<TextAsset>("eungye-passenger-alternatives");if(!asset)return;
  var data=JsonUtility.FromJson<AlternativePassengerStopData>(asset.text);
  if(data==null||data.schema!=1||data.mapId!=map.mapId||data.pairs==null){AlternativeRejected.Add("invalid alternative overlay");return;}
  var ids=new HashSet<string>();
  foreach(var definition in data.pairs){
   string id=definition==null||definition.bay==null?"missing":definition.bay.stopId;
   if(definition==null||definition.bay==null||string.IsNullOrEmpty(id)||!ids.Add(id)||IndexOf(id)>=0||alternatives.ContainsKey(definition.originalStopId??"")){AlternativeRejected.Add(id+": duplicate/missing alternative identity");continue;}
   AlternativePassengerStop alternative;string reason;
   if(!ValidateAlternative(map,graph,new Geometry(map),definition,out alternative,out reason)){AlternativeRejected.Add(id+": "+reason);continue;}
   // Commit only after both directed road access and the complete pedestrian
   // corridor pass. Failed overlays restore the exact previous map arrays.
   var priorRoads=map.roads;var priorAreas=map.areas;
   try{
    InstallBay(map,definition.bay);var areas=new List<MapArea>(map.areas);
    for(int i=1;i<alternative.WalkingPath.Length;i++){
     var from=alternative.WalkingPath[i-1];var to=alternative.WalkingPath[i];var direction=(to-from).normalized;
     areas.Add(Walk((from+to)*.5f,direction,1.5f,FlatDistance(from,to)+.1f,"game-alternative-walk:"+id));
    }
    map.areas=areas.ToArray();ProjectMapScene.InvalidateAreaIndex(map);
    string installedReason;
    if(!WalkingClear(map,new Geometry(map),alternative.WalkingPath,true,out installedReason))throw new InvalidOperationException(installedReason);
    alternatives.Add(alternative.OriginalStopId,alternative);alternativeAdmitted.Add(definition.bay);
   }catch(InvalidOperationException error){map.roads=priorRoads;map.areas=priorAreas;ProjectMapScene.InvalidateAreaIndex(map);AlternativeRejected.Add(id+": "+error.Message);}
  }
 }
 bool ValidateAlternative(MapData map,HdRoadGraph graph,Geometry geometry,AlternativePassengerStopDefinition definition,out AlternativePassengerStop alternative,out string reason){
  alternative=null;reason="";var original=Entries.FirstOrDefault(e=>e.StopId==definition.originalStopId);
  if(original==null||!original.Stop.hasPassengerAnchor){reason="original passenger sidewalk unavailable";return false;}
  MapStop stop;if(!Validate(map,graph,geometry,definition.bay,out stop,out reason))return false;
  float separation=FlatDistance(original.Stop.Kerb,stop.Kerb);
  if(separation<30||separation>130||Stops.Any(s=>FlatDistance(s.Kerb,stop.Kerb)<30)||alternatives.Values.Any(a=>FlatDistance(a.Stop.Kerb,stop.Kerb)<30)){reason="alternative physical bay separation";return false;}
  if(graph.Route(original.Stop.laneEdge,original.Stop.laneAt,stop.laneEdge,stop.laneAt)==null||graph.Route(stop.laneEdge,stop.laneAt,original.Stop.laneEdge,original.Stop.laneAt)==null){reason="alternative disconnected directed approach/exit";return false;}
  if(definition.walkingPath==null||definition.walkingPath.Length<2){reason="missing authored walking path";return false;}
  var walking=definition.walkingPath.Select(p=>p==null?new Vector3(float.NaN,0,0):new Vector3(p.x,.14f,p.z)).ToArray();
  if(FlatDistance(walking[0],original.Stop.Passenger)>.1f||FlatDistance(walking[walking.Length-1],stop.Passenger)>.1f){reason="walking endpoints mismatch";return false;}
  float metres=0;for(int i=1;i<walking.Length;i++)metres+=FlatDistance(walking[i-1],walking[i]);
  if(float.IsNaN(metres)||float.IsInfinity(metres)||metres<1||metres>150){reason="walking route outside 1–150m";return false;}
  if(!WalkingClear(map,geometry,walking,false,out reason))return false;
  alternative=new AlternativePassengerStop{Id=definition.bay.stopId,OriginalStopId=original.StopId,EvidenceStatus=definition.bay.evidenceStatus,Stop=stop,WalkingPath=walking,WalkingMetres=metres};return true;
 }
 static bool WalkingClear(MapData map,Geometry geometry,Vector3[] path,bool requireInstalled,out string reason){
  reason="";
  for(int i=1;i<path.Length;i++){
   var from=path[i-1];var to=path[i];float length=FlatDistance(from,to);if(length<.1f){reason="zero walking segment";return false;}
   var direction=(to-from).normalized;var right=TrafficSystem.Right(direction);
   var footprint=ProjectMapScene.Rectangle((from+to)*.5f,direction,1.5f,length+.05f);
   if(geometry.FootprintBlocked(footprint)){reason="walking polygon obstacle overlap";return false;}
   if(geometry.FootprintOnRoad(footprint,.05f)){reason="walking corridor crosses asphalt";return false;}
   int steps=Mathf.CeilToInt(length/.5f);
   for(int n=0;n<=steps;n++){
    var point=Vector3.Lerp(from,to,n/(float)steps);
    foreach(float offset in new[]{-.7f,0,.7f}){
     var sample=point+right*offset;
     if(float.IsNaN(sample.x)||float.IsNaN(sample.z)||sample.x<=3||sample.z<=3||sample.x>=map.width-3||sample.z>=map.depth-3||geometry.OnRoad(sample,.05f)||geometry.Blocked(sample)){reason="walking corridor blocked/bounds/asphalt";return false;}
     if(requireInstalled&&!ProjectMapScene.OnArea(map,sample,true)){reason="walking sidewalk discontinuity";return false;}
    }
    foreach(var junction in map.junctionRules??new HybridJunctionRule[0])if(FlatDistance(point,junction.V())<30){reason="walking junction clearance";return false;}
    foreach(var crossing in map.osmCrossings??new OsmCrossing[0])if(FlatDistance(point,new Vector3(crossing.x,0,crossing.z))<18){reason="walking crossing clearance";return false;}
    if(map.editorDocument!=null)foreach(var facility in map.editorDocument.facilities)if((facility.kind=="crosswalk"||facility.kind=="stopLine")&&FlatDistance(point,facility.Position)<18){reason="walking edited crossing/stop line";return false;}
   }
  }return true;
 }
}
