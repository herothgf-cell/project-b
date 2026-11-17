using System;
using System.Linq;
using UnityEngine;

public static class JunctionSixRepair {
 const string Id="reviewed:Y05:44:1:-1:zebra";
 public static void Verify(){
  var map=ProjectMapStore.Load(ProjectMapBuild.Root).Compose();var f=GeneratedCrosswalkGeometry.Effective(map,map.editorDocument.facilities.Single(x=>x.id==Id));Vector3 a,b;
  if(!CrosswalkLanding.Try(map,f,out a,out b))throw new Exception("J6_CROSSWALK ends do not land on sidewalks");
  Debug.Log("J6_CROSSWALK_OK centre="+f.Position+" width="+f.width+" landings="+a+" / "+b);
 }
 public static void Apply(){
  var root=ProjectMapBuild.Root;var token=ProjectMapStore.Fingerprint(root);var package=ProjectMapStore.Load(root);
  if(package.contentHash!="4bc4690b4e3fd618571b4ed82a53e5bb1f49e5c3339a7a6af32fd5a403cb901f")throw new Exception("Expected reviewed r44");
  var map=package.Compose();var d=package.document.Copy();var crossing=d.facilities.Single(f=>f.id==Id);var origin=crossing.Position;var direction=Quaternion.Euler(0,crossing.yaw,0)*Vector3.forward;
  bool found=false;float shift=0;Vector3 a,b;
  for(float distance=0;distance<=12;distance+=.5f){var center=origin-direction*distance;float half=crossing.width*.5f;
   if(!GeneratedCrosswalkGeometry.Fit(map,ref center,direction,ref half))continue;
   var trial=JsonUtility.FromJson<RoadMapFacility>(JsonUtility.ToJson(crossing));trial.x=center.x;trial.z=center.z;trial.width=half*2;
   if(!CrosswalkLanding.Try(map,trial,out a,out b))continue;crossing.x=center.x;crossing.z=center.z;crossing.width=half*2;shift=distance;found=true;break;
  }
  if(!found)throw new Exception("No supported crossing within 12m upstream");
  var stop=d.facilities.Single(f=>f.id=="reviewed:Y05:44:1:-1:stop");stop.x-=direction.x*shift;stop.z-=direction.z*shift;
  d.revision=45;d.projectRevision="";d.savedAt=DateTime.UtcNow.ToString("o");var next=ProjectMapPackage.Create(package.baseline,d);
  ProjectMapStore.Save(root,next,token);ProjectMapBuild.Prepare();Verify();Debug.Log("J6_REPAIR_APPLIED shift="+shift+" hash="+next.contentHash);
 }
 public static void Walker(){
  var root=new GameObject("walker phase regression");try{
   var system=root.AddComponent<TrafficSystem>();system.manualClock=true;
   var walker=root.AddComponent<RoadMapWalker>();walker.system=system;walker.hasLandings=true;walker.landingA=Vector3.zero;walker.landingB=Vector3.right*35;
   walker.crossing=new RoadMapFacility{id="crossing",kind="crosswalk",axis=0,sign=1,width=33};
   walker.program=new RoadMapSignalProgram{groups=new[]{new RoadMapSignalGroup{id="walk",pedestrian=true,crosswalkId="crossing",axis=0,sign=1}},stages=new[]{new RoadMapStage{green=25,yellow=0,clearance=5,groups=new[]{"walk"}}}};
   for(int i=0;i<250;i++){system.manualTime=i*.1f;walker.Advance(.1f);}
   if(walker.Crossing||Vector3.Distance(walker.transform.position,walker.landingB)>.01f)throw new Exception("Walker did not finish 35m within green");
   system.manualTime=26;walker.Advance(.1f);if(walker.Crossing)throw new Exception("Walker moved on red");
   for(int i=0;i<250;i++){system.manualTime=30+i*.1f;walker.Advance(.1f);}
   if(walker.Crossing||Vector3.Distance(walker.transform.position,walker.landingA)>.01f)throw new Exception("Walker did not return without teleporting");
   Debug.Log("J6_WALKER_OK green-completion red-wait alternate-side");
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
 public static void UturnApproaches(){
  var map=ProjectMapStore.Load(ProjectMapBuild.Root).Compose();int checkedZones=0;
  foreach(var zone in UturnRoadMarkings.Build(map)){
   if(zone.joinId=="join:reviewed:J8066-S:uturn")continue;
   var lane=map.hdEdges.Single(e=>e.id==zone.incomingId);var end=lane.points.Last().V();
   if(Vector3.Dot(zone.start-end,zone.forward)>-10)throw new Exception("UTURN_APPROACH yellow remains alongside permitted approach "+zone.joinId);
   checkedZones++;
  }Debug.Log("UTURN_APPROACH_OK zones="+checkedZones);
 }
 public static void FinalBuild(){UturnApproaches();ImportedMarkingRendererTests.Run();PrototypeBuild.BuildHybrid();}
 public static void BuildVerified(){Verify();Walker();RoundaboutFlowTests.Run();RoundaboutFlowTests.Motion();ImportedMarkingRendererTests.Run();BankApproachRegression.Run();ConfirmedCorridorsR43.Verify();JunctionRouteFeedbackTests.VerifyMigration();PrototypeBuild.BuildHybrid();}
}
