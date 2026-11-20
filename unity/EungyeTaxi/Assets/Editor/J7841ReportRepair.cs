using System;
using System.Linq;
using UnityEngine;

public static class J7841ReportRepair {
 public const string Approach="local:8228:888:8024:892", Upstream="local:8674:881:8401:884";
 public static void Diagnose(){
  var map=ProjectMapStore.Load(ProjectMapBuild.Root).Compose();
  Action<string,Vector3,Vector3,float,float,bool> mark=(name,p,d,w,l,y)=>{if(p.x>765&&p.x<784&&p.z>48&&p.z<77)Debug.Log("J7841_PAINT "+name+" p="+p.ToString("F3")+" d="+d+" w="+w+" l="+l+" yellow="+y);};
  RoadPaintGeometry.Paint(map,null,mark);UturnRoadMarkings.Paint(map,mark);
  foreach(var m in map.hdMarks.Where(m=>m.points.Any(p=>p.x>765&&p.x<784&&p.z>48&&p.z<77)))Debug.Log("J7841_SOURCE "+JsonUtility.ToJson(m));
  foreach(var e in map.hdEdges.Where(e=>!e.disabled&&e.points.Any(p=>p.x>765&&p.x<784&&p.z>48&&p.z<77)))Debug.Log("J7841_LANE "+e.id+" source="+e.sourceId);
  foreach(var a in LaneDirectionPaint.Layout(map).Where(a=>a.centre.x>800&&a.centre.x<875&&a.centre.z>80&&a.centre.z<100))Debug.Log("J7841_ARROW "+map.hdEdges[a.edgeIndex].id+" mask="+a.turns+" p="+a.centre);
 }
 public static void Verify(){
  var map=ProjectMapStore.Load(ProjectMapBuild.Root).Compose();var e=map.hdEdges.Single(x=>x.id==Approach);
  if(!e.next.Select(n=>map.hdEdges[n].turn).OrderBy(t=>t).SequenceEqual(new[]{0,1}))throw new Exception("J7841 east approach must allow straight/right only");
  var arrows=LaneDirectionPaint.Layout(map);
  if(!arrows.Any(a=>map.hdEdges[a.edgeIndex].id==Approach&&a.turns==6))throw new Exception("J7841 straight/right arrow missing");
  if(arrows.Any(a=>map.hdEdges[a.edgeIndex].id==Upstream))throw new Exception("J7841 upstream arrow must be hidden");
  Debug.Log("J7841_REPORT_OK");
 }
 public static void VerifyMedian(){
  var map=ProjectMapStore.Load(ProjectMapBuild.Root).Compose();
  var zone=UturnRoadMarkings.Build(map).Single(z=>z.joinId=="join:reviewed:G04:NB:uturn");
  var incoming=map.hdEdges.Single(e=>e.id=="reviewed:G04:NB:left").points.Last().V();
  var opposing=map.hdEdges.Single(e=>e.id=="local:7798:686:7778:21").points.First().V();
  var expected=(incoming+opposing)*.5f;
  if(Mathf.Abs(Vector3.Dot(zone.start-expected,zone.across))>.15f)throw new Exception("J7841 white U-turn line is inside the opposing lane instead of the median");
  Debug.Log("J7841_MEDIAN_OK "+zone.start);
 }
 public static void DiagnoseAndVerify(){Diagnose();Verify();}
 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);var before=ProjectMapStore.Load(root);
  if(before.document.revision!=45||before.contentHash!="be92b289b8c5243d653df405a689f5e032c34c6cda073b345b0aa2e0be314d26")throw new Exception("Expected reviewed r45");
  var d=before.document.Copy();var lane=d.lanes.Single(l=>l.id==Approach);string left="join:"+Approach+":-1";
  var evidence=new RoadMapEvidence{state="user",month="2026-11",checkedAt="2026-11-17",appearanceConfirmed=true,ruleConfirmed=true,url="user://2026-11-17/j7841-road-report",note="User annotated game overview: east approach straight/right only; upstream glyph removed, navigation retained. White line inside opposing lane removed by correcting the U-turn median alignment."};
  lane.next=lane.next.Where(id=>id!=left).ToArray();lane.evidence=evidence;
  d.lanes.Single(l=>l.id==left).disabled=true;
  d.facilities=d.facilities.Append(new RoadMapFacility{id="reviewed:r46:J7841:upstream-no-arrow",kind="lanePresentation",type=LaneDirectionPaint.NoArrowsType,laneId=Upstream,junctionId=d.lanes.Single(l=>l.id==Upstream).ruleId,x=850.09f,z=88.21f,rule=false,evidence=evidence}).ToArray();
  d.revision=46;d.projectRevision="";d.savedAt=DateTime.UtcNow.ToString("o");var next=ProjectMapPackage.Create(before.baseline,d);
  ProjectMapStore.Save(root,next,token);ProjectMapBuild.Prepare();Verify();Debug.Log("J7841_APPLIED hash="+next.contentHash);
 }
 public static void Routes(){
  var package=ProjectMapStore.Load(ProjectMapBuild.Root);var map=package.Compose();EungyeConnectivityRepair.Apply(map,package.baseline);var graph=new HdRoadGraph(map);var stops=PassengerStopCatalog.For(map).Stops;int count=0;
  if(stops.Length!=39)throw new Exception("Passenger stops changed");
  foreach(var a in stops)foreach(var b in stops)if(a!=b){if(graph.Route(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt)==null)throw new Exception("Lost route "+a.name+" to "+b.name);count++;}
  var upstream=map.hdEdges.Single(e=>e.id==Upstream);if(!upstream.next.Select(i=>map.hdEdges[i].turn).OrderBy(t=>t).SequenceEqual(new[]{0,1}))throw new Exception("Hidden arrow changed upstream permissions");
  if(map.hdEdges.Any(e=>!e.disabled&&e.next.Any(i=>map.hdEdges[i].id=="join:"+Approach+":-1")))throw new Exception("Forbidden left connector remains reachable");
  Debug.Log("J7841_ROUTES_OK stops="+stops.Length+" pairs="+count);
 }
 public static void BuildVerified(){
  Verify();VerifyMedian();Routes();ImportedMarkingRendererTests.Run();JunctionSixRepair.UturnApproaches();JunctionRouteFeedbackTests.VerifyMigration();
  Build();
 }
 public static void MigrationAndBuild(){JunctionRouteFeedbackTests.VerifyMigration();Build();}
 static void Build(){
  string scene="Assets/Scenes/FirstDrive.unity";var bytes=System.IO.File.ReadAllBytes(scene);
  try{PrototypeBuild.BuildHybrid();}finally{System.IO.File.WriteAllBytes(scene,bytes);}
 }
}
