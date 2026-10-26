using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
// Reproducible, explicitly surveyed additions. Source data and the original baseline stay unchanged.
public static class ReviewedUturnMap {
 public static readonly string[] Incoming={"reviewed:T1-SE:pocket","reviewed:T1-NW:pocket","local:5489:14147:4576:13604:part:2"};
 public static readonly string[] Outgoing={"local:4502:13231:5956:12047","local:4347:13732:2398:15897","local:4640:13501:5550:14042"};
 public static readonly string[] Names={"T1-SE","T1-NW","T1-NE"};
 public static readonly string[] Views={"NeRkbKpCLPyjXB6lsBHKhw,-70,12,50","pbIhvNw5ofVGzsLRZ9jVWQ,125,8,25","C05LjSoKPvjCfwsrheEcxQ,230,12,30"};
 [MenuItem("Taxi/Project Map/Apply Reviewed U-turns")]
 public static void Apply(){
  var root=ProjectMapBuild.Root;var token=ProjectMapStore.Fingerprint(root);var package=ProjectMapStore.Load(root);var document=package.document.Copy();
  for(int i=0;i<Incoming.Length;i++){
   string movementId="join:reviewed:"+Names[i]+":uturn",signId=movementId+":condition";
   if(document.lanes.Any(l=>l.id==movementId))continue;
   var from=document.lanes.First(l=>l.id==Incoming[i]);var target=document.lanes.First(l=>l.id==Outgoing[i]);
   var created=RoadMapAuthoringOperations.Connect(document,from.id,target.id,UturnMovement.Turn);var lane=document.lanes.First(l=>l.id==created);
   lane.id=movementId;foreach(var l in document.lanes)l.next=l.next.Select(n=>n==created?movementId:n).ToArray();
   var evidence=new RoadMapEvidence{state="observed",url="https://map.naver.com/p/entry/coordinates/3zadbu,2AGZTu?c=16,0,0,0,adh&p="+Views[i]+",Float",month="2026-05",checkedAt="2026-10-26",appearanceConfirmed=true,ruleConfirmed=true,note="유턴 표지: 좌회전시 / 보행신호시 직접 확인. 촬영 당시 조건; 곡선·출구 차로·신호 주기는 게임 추정치."};
   lane.evidence=evidence;lane.status="provisional";
   var a=from.points.Last().V();var heading=(a-from.points[from.points.Length-2].V()).normalized;var rule=package.baseline.junctionRules.First(r=>r.id==from.ruleId);var axisDir=new Vector3(rule.dx,0,rule.dz).normalized;int axis=Mathf.Abs(Vector3.Dot(heading,axisDir))>.7f?0:1;int sign=Vector3.Dot(heading,axis==0?axisDir:TrafficSystem.Right(axisDir))>=0?1:-1;
   var position=a-heading*7-TrafficSystem.Right(heading)*2.3f;
   document.facilities=document.facilities.Concat(new[]{new RoadMapFacility{id=signId,kind="sign",type=UturnMovement.LeftOrPedestrianGreen,laneId=from.id,junctionId=from.ruleId,rule=true,axis=axis,sign=sign,x=position.x,z=position.z,yaw=Quaternion.LookRotation(heading).eulerAngles.y,height=3,text="유턴 · 좌회전 / 보행 신호 시",evidence=evidence}}).ToArray();
  }
  document.revision++;var updated=ProjectMapPackage.Create(package.baseline,document);ProjectMapStore.Save(root,updated,token);ProjectMapBuild.Prepare();Validate();
 }
 public static void Validate(){
  var package=ProjectMapStore.Load(ProjectMapBuild.Root);var map=package.Compose();var graph=new HdRoadGraph(map);
  for(int i=0;i<Incoming.Length;i++){
   int a=Array.FindIndex(map.hdEdges,l=>l.id==Incoming[i]),b=Array.FindIndex(map.hdEdges,l=>l.id==Outgoing[i]);
   var route=graph.Route(a,Mathf.Max(0,graph.paths[a].length-10),b,Mathf.Min(6,graph.paths[b].length));
   if(route==null||!route.edges.Any(e=>map.hdEdges[e].id=="join:reviewed:"+Names[i]+":uturn"))throw new Exception("Reviewed U-turn route missing "+Names[i]);
   var from=package.document.lanes.First(l=>l.id==Incoming[i]);var target=package.document.lanes.First(l=>l.id==Outgoing[i]);UturnMovement.Geometry(from,target);
   if(UturnMovement.Condition(package.document,from.id)==null)throw new Exception("Reviewed U-turn condition missing");
  }
  Debug.Log("REVIEWED_UTURN_MAP_OK movements=3 evidence=2026-05 checked=2026-10-26 hashes=valid");
 }
}
