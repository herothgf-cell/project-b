using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public static class J7917LaneMapEdit {
 const string Junction="J7917_2565";
 const string NorthInner="local:7922:1836:7947:2382";
 const string NorthOuter="local:7998:1833:8023:2378";
 const string Pocket="reviewed:J7917:north-left";
 static RoadMapLane Copy(RoadMapLane lane){return JsonUtility.FromJson<RoadMapLane>(JsonUtility.ToJson(lane));}
 static MapPoint Point(Vector3 p){return new MapPoint{x=p.x,z=p.z};}
 static AssistedRoadNetwork.Path Path(RoadMapLane lane){return new AssistedRoadNetwork.Path(lane.points.Select(p=>p.V()).ToArray());}
 static RoadMapEvidence Evidence(){return new RoadMapEvidence{state="observed",appearanceConfirmed=true,month="2026-05",checkedAt="2026-11-12",url="https://map.naver.com/p/entry/coordinates/3zanLi,2AGD8X?c=16,0,0,0,adh&p=2Tjw8TJtiWoPKkQLcqed7g,75,-15,80,Float",note="은계로 J7917 남쪽: 남행 3차로, 북행 정지선 좌/직/직/우 4차로. 추가 중간 차로와 기존 출구 연결. 폭/테이퍼는 게임 치수."};}
 // Preserve every existing ID and index. Only append lanes/connectors so saved
 // stop, service and traffic references do not silently change their meaning.
 public static void Apply(){
  var root=ProjectMapBuild.Root;var token=ProjectMapStore.Fingerprint(root);var package=ProjectMapStore.Load(root);var doc=package.document;
  Require(doc.revision==16&&!doc.lanes.Any(l=>l.id==Pocket),"expected original r16");
  var original=doc.lanes.ToDictionary(l=>l.id);var added=new List<RoadMapLane>();var middle=new Dictionary<string,RoadMapLane>();
  string[][] pairs={
   new[]{NorthInner,NorthOuter},
   new[]{"local:7871:2385:7846:1840","local:7795:2388:7771:1843"},
   new[]{"local:7924:3503:7887:2749","local:7848:3507:7812:2752"},
   new[]{"local:7963:2745:8000:3499","local:8039:2741:8076:3496"}
  };
  foreach(var pair in pairs)foreach(string suffix in new[]{"",":part:1",":part:2"}){
   RoadMapLane inner,outer;if(!original.TryGetValue(pair[0]+suffix,out inner)||!original.TryGetValue(pair[1]+suffix,out outer))continue;
   var path=Path(inner);var other=Path(outer);var lane=Copy(outer);lane.id="reviewed:J7917:middle:"+outer.id;
   lane.fromNode=lane.id+":start";lane.toNode=lane.id+":end";lane.number=3;lane.width=3.5f;lane.evidence=Evidence();
   lane.points=Enumerable.Range(0,outer.points.Length).Select(i=>Point((other.Position(other.length*i/(outer.points.Length-1))+path.Position(path.length*i/(outer.points.Length-1)))*.5f)).ToArray();
   middle.Add(outer.id,lane);added.Add(lane);
  }
  // Adjacent pieces share exactly the same node and position.
  foreach(var pair in middle)foreach(var target in original[pair.Key].next)if(middle.ContainsKey(target)){
   var a=pair.Value;var b=middle[target];b.fromNode=a.toNode;b.points[0]=Point(a.points.Last().V());
  }
  foreach(var pair in middle){
   var lane=pair.Value;var next=new List<string>();
   foreach(string id in original[pair.Key].next){
    var target=original[id];if(target.disabled||target.change!=0||target.turn!=0)continue;
    if(middle.ContainsKey(id)){next.Add(middle[id].id);continue;}
    if(!id.StartsWith("join:"))continue; // Existing native portals belong to the outer lane.
    foreach(string exitId in target.next){
     RoadMapLane exit=middle.ContainsKey(exitId)?middle[exitId]:original[exitId];
     var join=ShiftJoin(target,"join:"+lane.id+":"+added.Count,lane.points.Last().V(),exit.points[0].V(),lane.toNode,exit.fromNode,exit.id);
     added.Add(join);next.Add(join.id);
    }
   }
   lane.next=next.ToArray();Require(lane.next.Length>0,"middle exit "+lane.id);
  }
  foreach(var pair in middle){
   var lane=pair.Value;
   foreach(var prior in original.Values.Where(l=>!l.disabled&&l.change==0&&l.next.Contains(pair.Key)&&!middle.ContainsKey(l.id)).ToArray()){
    if(!prior.id.StartsWith("join:"))continue;
    var join=ShiftJoin(prior,"join:reviewed:J7917:entry:"+added.Count,prior.points[0].V(),lane.points[0].V(),prior.fromNode,lane.fromNode,lane.id);
    added.Add(join);
    foreach(var entry in original.Values.Where(l=>!l.disabled&&l.next.Contains(prior.id)))entry.next=entry.next.Concat(new[]{join.id}).Distinct().ToArray();
   }
  }
  var north=original[NorthInner];var northPath=Path(north);var pocket=Copy(north);pocket.id=Pocket;pocket.number=4;pocket.width=3.3f;pocket.toNode=Pocket+":end";pocket.evidence=Evidence();
  pocket.points=north.points.Select((p,i)=>Point(p.V()-TrafficSystem.Right(northPath.Tangent(northPath.length*i/(north.points.Length-1)))*3.3f*Mathf.SmoothStep(0,1,northPath.length*i/(north.points.Length-1)/18f))).ToArray();
  var left=original["join:"+NorthInner+":-1"];
  var moved=ShiftJoin(left,left.id,pocket.points.Last().V(),left.points.Last().V(),pocket.toNode,left.toNode,left.next.Single());
  left.points=moved.points;left.fromNode=moved.fromNode;left.evidence=Evidence();
  north.next=north.next.Where(id=>id!=left.id).ToArray();pocket.next=new[]{left.id};
  foreach(var prior in original.Values.Where(l=>!l.disabled&&l.next.Contains(north.id)))prior.next=prior.next.Concat(new[]{Pocket}).ToArray();
  added.Add(pocket);
  // The outside approach now turns right only. Keep its existing straight
  // connector available to the new middle lane through the copied connector.
  original[NorthOuter].next=new[]{"join:"+NorthOuter+":1"};
  original["join:"+NorthOuter+":0"].disabled=true;
  doc.lanes=doc.lanes.Concat(added).ToArray();doc.revision++;doc.savedAt=DateTime.UtcNow.ToString("o");doc.projectRevision="";
  package.contentHash=ProjectMapPackage.Digest(JsonUtility.ToJson(doc));ProjectMapStore.Save(root,package,token);ProjectMapBuild.Prepare();
  Debug.Log("J7917_LANES_MAP_OK "+package.contentHash+" added="+added.Count);
 }
 static RoadMapLane ShiftJoin(RoadMapLane source,string id,Vector3 start,Vector3 end,string from,string to,string exit){
  var result=Copy(source);result.id=id;result.fromNode=from;result.toNode=to;result.next=new[]{exit};result.evidence=Evidence();
  var a=start-source.points[0].V();var b=end-source.points.Last().V();var path=Path(source);float at=0;
  result.points=source.points.Select((p,i)=>{if(i>0)at+=Vector3.Distance(source.points[i-1].V(),p.V());return Point(p.V()+Vector3.Lerp(a,b,Mathf.SmoothStep(0,1,at/path.length)));}).ToArray();return result;
 }
 static void Require(bool value,string message){if(!value)throw new InvalidOperationException("J7917_LANES: "+message);}
 public static void RunChecks(){Verify();LaneDirectionPaintTests.Run();J7973SouthboundMapEdit.Verify();PersistentGreenLeftTests.Run();Debug.Log("J7917_REGRESSION_SUITE_OK");}
 public static void Verify(){
  var package=ProjectMapStore.Load(ProjectMapBuild.Root);var map=package.Compose();EungyeConnectivityRepair.Apply(map,package.baseline);
  foreach(float z in new[]{212.52565f,225f,295f}){
   var crossing=map.hdEdges.Where(e=>!e.disabled&&e.change==0&&e.turn==0&&!e.id.StartsWith("join:")&&e.points.Min(p=>p.x)>765&&e.points.Max(p=>p.x)<820&&e.points.Min(p=>p.z)<z&&e.points.Max(p=>p.z)>z).ToArray();
   int north=crossing.Count(e=>e.points.Last().z>e.points[0].z),south=crossing.Length-north;
   Require(north==(z<240?4:3)&&south==3,"cross section z="+z+" expected N="+(z<240?4:3)+" S=3 actual N="+north+" S="+south);
  }
  var arrows=LaneDirectionPaint.Layout(map).Where(a=>a.centre.z>210&&a.centre.z<240&&a.forward.z>.95f&&map.hdEdges[a.edgeIndex].ruleId==Junction).OrderBy(a=>a.centre.x).ToArray();
  Require(arrows.Select(a=>a.turns).SequenceEqual(new[]{LaneDirectionPaint.Left,LaneDirectionPaint.Straight,LaneDirectionPaint.Straight,LaneDirectionPaint.Right}),"north arrows L/S/S/R: "+string.Join(",",arrows.Select(a=>a.turns)));
  var graph=new HdRoadGraph(map);
  foreach(var e in graph.edges.Where(e=>!e.disabled&&e.id.StartsWith("reviewed:J7917:"))){
   int index=Array.IndexOf(graph.edges,e);
   Require(graph.edges.Any(p=>!p.disabled&&p.next.Contains(index)),"incoming "+e.id);
   Require(e.next.Any(n=>!graph.edges[n].disabled),"outgoing "+e.id);
  }
  var stops=PassengerStopCatalog.For(map).Stops;int pairs=0;
  foreach(var a in stops)foreach(var b in stops)if(a!=b){Require(graph.Route(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt)!=null,"route "+a.name+" -> "+b.name);pairs++;}
  Debug.Log("J7917_LANES_CHECKS_OK pairs="+pairs);
 }
}
