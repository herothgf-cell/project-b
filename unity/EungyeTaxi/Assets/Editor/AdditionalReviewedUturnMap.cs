using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
// Reproducible, explicitly surveyed additions. Source data and the original baseline stay unchanged.
public static class AdditionalReviewedUturnMap {
 public static readonly string[] Incoming={"local:5897:14274:7063:14681:part:2","local:8017:3985:8089:6162:part:2","A2218G611877:0"};
 public static readonly string[] Outgoing={"local:7023:14796:5857:14389","local:7975:6165:7904:3992","A222BC004187:0"};
 public static readonly string[] Names={"J7254-SW","J8066-S","J7973-E"};
 public static readonly string[] Junctions={"J7254_14780","J8066_6364","J7973_3744"};
 public static readonly string[] Views={"https://map.naver.com/p/entry/coordinates/3zalzU,2AH30z?c=16,0,0,0,adh&p=h1v2mf8APsK03Fozf__QSw,70,5,60,Float","https://map.naver.com/p/entry/coordinates/3zao1h,2AGJhI?c=15,0,0,0,adh&p=VzXWr0yZ-kjmidpHNnS74w,30,6,20,Float","https://map.naver.com/p/entry/coordinates/3zanLi,2AGD8X?c=15,0,0,0,adh&p=vXxTsGf5QJ9PZl94AFingQ,-64,8,25,Float"};
 [MenuItem("Taxi/Project Map/Apply Additional Reviewed U-turns")]
 public static void Apply(){
  var root=ProjectMapBuild.Root;var token=ProjectMapStore.Fingerprint(root);var package=ProjectMapStore.Load(root);var document=package.document.Copy();
  for(int i=0;i<Incoming.Length;i++){
   string movementId="join:reviewed:"+Names[i]+":uturn",signId=movementId+":condition";
   document.lanes=document.lanes.Where(l=>l.id!=movementId).ToArray();
   foreach(var existing in document.lanes)existing.next=existing.next.Where(n=>n!=movementId).ToArray();
   document.facilities=document.facilities.Where(f=>!f.id.StartsWith(movementId+":" )).ToArray();
   var from=document.lanes.First(l=>l.id==Incoming[i]);var target=document.lanes.First(l=>l.id==Outgoing[i]);
   from.ruleId=Junctions[i];
   var created=RoadMapAuthoringOperations.Connect(document,from.id,target.id,UturnMovement.Turn);var lane=document.lanes.First(l=>l.id==created);
   lane.id=movementId;foreach(var l in document.lanes)l.next=l.next.Select(n=>n==created?movementId:n).ToArray();
   var evidence=new RoadMapEvidence{state="observed",url=Views[i],month="2026-05",checkedAt="2026-10-26",appearanceConfirmed=true,ruleConfirmed=true,note="유턴 표지: 좌회전시 / 보행신호시 직접 확인. 촬영 당시 조건; 곡선·출구 차로·신호 주기는 게임 추정치."};
   lane.evidence=evidence;lane.status="provisional";
   var a=from.points.Last().V();var heading=(a-from.points[from.points.Length-2].V()).normalized;var rule=package.baseline.junctionRules.First(r=>r.id==from.ruleId);var axisDir=new Vector3(rule.dx,0,rule.dz).normalized;int axis=Mathf.Abs(Vector3.Dot(heading,axisDir))>.7f?0:1;int sign=Vector3.Dot(heading,axis==0?axisDir:TrafficSystem.Right(axisDir))>=0?1:-1;
   var position=a-heading*7-TrafficSystem.Right(heading)*2.3f;
   // Match stop-plane heading to the surveyed approach, before the curve begins.
   // These offsets and carriageway widths are game geometry, not measured street dimensions.
   float width=new[]{18.3f,17.1f,9.9f}[i];var stop=a-heading*3+TrafficSystem.Right(heading)*(width/3);
   document.facilities=document.facilities.Concat(new[]{new RoadMapFacility{id=movementId+":stop",kind="stopLine",laneId=from.id,junctionId=from.ruleId,axis=axis,sign=sign,x=stop.x,z=stop.z,yaw=Quaternion.LookRotation(heading).eulerAngles.y,width=width,length=.4f,evidence=new RoadMapEvidence{state="game",note="진입 차로 방향과 유턴 곡선 이전에 맞춘 게임 정지선. 실측 위치 아님."}}}).ToArray();
   document.facilities=document.facilities.Concat(new[]{new RoadMapFacility{id=signId,kind="sign",type=UturnMovement.LeftOrPedestrianGreen,laneId=from.id,junctionId=from.ruleId,rule=true,axis=axis,sign=sign,x=position.x,z=position.z,yaw=Quaternion.LookRotation(heading).eulerAngles.y,height=3,text="유턴 · 좌회전 / 보행 신호 시",evidence=evidence}}).ToArray();
  }
  document.revision++;var updated=ProjectMapPackage.Create(package.baseline,document);ProjectMapStore.Save(root,updated,token);ProjectMapBuild.Prepare();Validate();AdditionalUturnObserverTests.Run();
 }
 public static void Validate(){
  var package=ProjectMapStore.Load(ProjectMapBuild.Root);var map=package.Compose();var graph=new HdRoadGraph(map);
  for(int i=0;i<Incoming.Length;i++){
   var movement=package.document.lanes.First(l=>l.id=="join:reviewed:"+Names[i]+":uturn");string exitId=movement.next.Single();
   string incomingId=package.document.lanes.Single(l=>!l.disabled&&l.next.Contains(movement.id)).id;
   int a=Array.FindIndex(map.hdEdges,l=>l.id==incomingId),b=Array.FindIndex(map.hdEdges,l=>l.id==exitId);
   var route=graph.Route(a,Mathf.Max(0,graph.paths[a].length-10),b,Mathf.Min(6,graph.paths[b].length));
   if(route==null||!route.edges.Any(e=>map.hdEdges[e].id=="join:reviewed:"+Names[i]+":uturn"))throw new Exception("Reviewed U-turn route missing "+Names[i]);
   var from=package.document.lanes.First(l=>l.id==incomingId);var target=package.document.lanes.First(l=>l.id==exitId);UturnMovement.Geometry(from,target);
   if(UturnMovement.Condition(package.document,from.id)==null)throw new Exception("Reviewed U-turn condition missing");
  }
  Debug.Log("ADDITIONAL_UTURN_MAP_OK movements=3 evidence=2026-05 checked=2026-10-26 hashes=valid");
 }
}
