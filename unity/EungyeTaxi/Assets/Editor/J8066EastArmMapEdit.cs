using System;
using System.Linq;
using UnityEngine;

// East arm of J8066: the surveyed westbound approach has two lanes, not
// two lanes in both directions. Distances and widths below are game geometry.
public static class J8066EastArmMapEdit {
 public const string West="local:9032:10757:8270:6402";
 public const string East="local:8265:6305:9056:10851";
 public const string Pocket="reviewed:J8066:west-left";
 public const string ApartmentPocket="reviewed:J8066:apartment-service-left";
 const string Left="join:"+West+":-1";
 static MapPoint Point(Vector3 p){return new MapPoint{x=p.x,z=p.z};}
 static AssistedRoadNetwork.Path Path(RoadMapLane l){return new AssistedRoadNetwork.Path(l.points.Select(p=>p.V()).ToArray());}
 static RoadMapEvidence Evidence(){return new RoadMapEvidence{state="observed",appearanceConfirmed=true,checkedAt="2026-11-13",month="2026-05",url="https://map.naver.com/p?c=15.00,0,0,0,adh&p=FI0tmS2ImG-qd356ibc6TQ,267,-10,80,Float",note="J8066 east arm: westbound inner left-only, outer straight/right; opposite eastbound one lane at junction. Further east changes to two eastbound lanes at apartment access, which is not represented in current road graph. Pocket 90m, taper20m and width3.3m are provisional game measurements."};}
 static RoadMapLane SplitAt(RoadMapDocument d,RoadMapLane lane,float distance){float at=0;for(int i=1;i<lane.points.Length-1;i++){at+=Vector3.Distance(lane.points[i-1].V(),lane.points[i].V());if(at>=distance){string id=RoadMapAuthoringOperations.SplitLane(d,lane.id,i);return d.lanes.Single(l=>l.id==id);}}throw new InvalidOperationException("Split outside approach");}
 static void ApartmentAccess(RoadMapDocument d){
  var head=d.lanes.Single(l=>l.id==East);var middle=SplitAt(d,head,100);SplitAt(d,middle,55);var path=Path(middle);
  var evidence=new RoadMapEvidence{state="observed",appearanceConfirmed=true,checkedAt="2026-11-13",month="2026-05",url="https://map.naver.com/p?c=15.00,0,0,0,adh&p=5WjiH29cxCHZWKAgq6_S5g,90,-10,80,Float",note="Eastbound inner left-only to north-side gated apartment access, outer straight; westbound one lane. Access station155m and pocket start100m are provisional map alignment, not surveyed distances. Private branch terminates at road boundary; no public through connection or passenger stop."};
  var lane=JsonUtility.FromJson<RoadMapLane>(JsonUtility.ToJson(middle));lane.id=ApartmentPocket;lane.sourceOffset=0;lane.toNode=ApartmentPocket+":end";lane.number=1;lane.width=3.3f;lane.ruleId="reviewed:J8066:private-access";lane.status="provisional";lane.evidence=evidence;
  float walked=0;lane.points=middle.points.Select((p,i)=>{if(i>0)walked+=Vector3.Distance(middle.points[i-1].V(),p.V());return Point(p.V()-TrafficSystem.Right(path.Tangent(walked))*3.3f*Mathf.SmoothStep(0,1,walked/20));}).ToArray();
  middle.number=2;middle.width=3.3f;middle.evidence=evidence;head.next=head.next.Concat(new[]{lane.id}).ToArray();
  var start=lane.points.Last().V();var forward=path.Tangent(path.length);var left=-TrafficSystem.Right(forward);var end=start+forward*7+left*5;var points=new MapPoint[33];
  for(int i=0;i<points.Length;i++){float t=i/32f,u=1-t;points[i]=Point(u*u*u*start+3*u*u*t*(start+forward*5)+3*u*t*t*(end-left*5)+t*t*t*end);}
  var landing=new RoadMapLane{id=ApartmentPocket+":private-boundary",sourceId=lane.sourceId,fromNode=ApartmentPocket+":landing",toNode=ApartmentPocket+":terminal",ruleId=lane.ruleId,number=1,width=3.3f,status="private-access",points=new[]{Point(end),Point(end+left*3)},heights=new float[2],next=new string[0],evidence=evidence};
  var turn=new RoadMapLane{id="join:"+ApartmentPocket,sourceId=lane.sourceId,fromNode=lane.toNode,toNode=landing.fromNode,ruleId=lane.ruleId,number=1,turn=-1,width=3.3f,status="provisional",points=points,heights=new float[points.Length],next=new[]{landing.id},evidence=evidence};lane.next=new[]{turn.id};
  d.lanes=d.lanes.Concat(new[]{lane,turn,landing}).ToArray();
 }
 public static void Apply(){
  var root=ProjectMapBuild.Root;var token=ProjectMapStore.Fingerprint(root);var p=ProjectMapStore.Load(root);var d=p.document;
  if(d.lanes.Any(l=>l.id==Pocket))throw new InvalidOperationException("J8066 west pocket already authored");
  var west=d.lanes.Single(l=>l.id==West);var original=Path(west);float walked=0;int cut=0;
  for(int i=1;i<west.points.Length;i++){walked+=Vector3.Distance(west.points[i-1].V(),west.points[i].V());if(walked>=original.length-90){cut=i;break;}}
  if(cut<=0)throw new InvalidOperationException("Approach is too short");
  string tailId=RoadMapAuthoringOperations.SplitLane(d,West,cut);var outer=d.lanes.Single(l=>l.id==tailId);var path=Path(outer);
  var pocket=JsonUtility.FromJson<RoadMapLane>(JsonUtility.ToJson(outer));pocket.id=Pocket;pocket.toNode=Pocket+":end";pocket.sourceOffset=0;pocket.number=1;pocket.width=3.3f;pocket.status="provisional";pocket.evidence=Evidence();pocket.next=new[]{Left};
  float distance=0;pocket.points=outer.points.Select((v,i)=>{if(i>0)distance+=Vector3.Distance(outer.points[i-1].V(),v.V());return Point(v.V()-TrafficSystem.Right(path.Tangent(distance))*3.3f*Mathf.SmoothStep(0,1,distance/20));}).ToArray();
  outer.number=2;outer.width=3.3f;outer.evidence=Evidence();outer.next=outer.next.Where(id=>id!=Left).ToArray();west.next=west.next.Concat(new[]{Pocket}).ToArray();
  var turn=d.lanes.Single(l=>l.id==Left);var turnPath=Path(turn);var shift=pocket.points.Last().V()-turn.points[0].V();distance=0;
  turn.points=turn.points.Select((v,i)=>{if(i>0)distance+=Vector3.Distance(turnPath.points[i-1],turnPath.points[i]);return Point(v.V()+shift*(1-Mathf.SmoothStep(0,1,distance/turnPath.length)));}).ToArray();turn.fromNode=pocket.toNode;turn.number=1;turn.evidence=Evidence();
  d.lanes=d.lanes.Concat(new[]{pocket}).ToArray();
  ApartmentAccess(d);
  var stop=d.facilities.Single(f=>f.junctionId=="J8066_6364"&&f.kind=="stopLine"&&f.axis==0&&f.sign==1);var offset=-TrafficSystem.Right(path.Tangent(path.length))*1.65f;stop.x+=offset.x;stop.z+=offset.z;stop.width=6.9f;stop.evidence=Evidence();
  var errors=RoadMapValidator.Validate(p.baseline,d).Where(e=>e.error).ToArray();if(errors.Length>0)throw new InvalidOperationException(string.Join(" / ",errors.Take(6).Select(e=>e.ToString())));
  var map=RoadMapComposer.Compose(p.baseline,d);foreach(string id in new[]{Pocket,ApartmentPocket}){var edge=map.hdEdges.Single(e=>e.id==id);if(!EungyeConnectivityRepair.FootprintClear(map,edge))throw new InvalidOperationException("J8066 pocket footprint leaves roadway: "+id);}
  d.revision++;d.savedAt=DateTime.UtcNow.ToString("o");d.projectRevision="";p.contentHash=ProjectMapPackage.Digest(JsonUtility.ToJson(d));ProjectMapStore.Save(root,p,token);ProjectMapBuild.Prepare();
  Debug.Log("J8066_EAST_ARM_MAP_OK "+p.contentHash+" westPocket="+path.length+" / east apartment branch terminates at private boundary; station estimates provisional");
 }
}
