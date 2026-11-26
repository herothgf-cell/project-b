using System;
using System.Linq;
using UnityEngine;

public static class JunctionRouteFeedbackTests {
 public static void VerifyMigration(){
  MapCatalog.LockedRegion="eungye";var map=MapCatalog.Load();
  int target=Enumerable.Range(25,44).SingleOrDefault(revision=>map.editorRevision=="은계 프로젝트 지도 · r"+revision);
  if(target==0)throw new Exception("Migration regression requires a reviewed r25–r68 default map");
  var method=typeof(BusinessProgress).GetMethod("UpgradeEungyeApproachCheckpoint",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
  var migrate=typeof(BusinessProgress).GetMethod("ApplyReviewedEungyeCheckpointMigration",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
  int checks=0;
  var recentJunctions=new[]{"J5692_14220","J6478_4105","J6481_3341","J5076_2580","J5750_2576","J6480_2573","J6199_11987","J3516_12881","J7801_9629","J7841_867","J1919_877","J4396_13458","J2564_12237","J5852_16963","reviewed:r47:P51"};
  var previousJunctions=new[]{"J7204_2569","J5074_877","J2145_15970","J7876_1656","J8346_1658","J10587_15883","J1882_11791","J2225_12001","J8921_10835","J7355_12821","J7292_13794","J7254_14780","J8066_6364","J7917_2565","J7973_3744","J1927_5181","J5692_14220","unrelated-junction"};
  foreach(int revision in Enumerable.Range(14,target-14))foreach(string junction in previousJunctions.Concat(recentJunctions)){
   var saved=MigrationFixture(revision,junction);var expected=JsonUtility.FromJson<LiveCampaignCheckpoint>(JsonUtility.ToJson(saved));
   expected.mapRevision=map.editorRevision;if(revision<25||target>=27&&revision<31||target>=33){expected.jobs.navigationCaptured=false;expected.jobs.navAvoidEdge=expected.jobs.navObservedEdge=expected.jobs.navPassedObservedEdge=-1;}
   bool clear=target>=27&&revision<27&&(junction=="J7917_2565"||junction=="J7204_2569")||revision<25&&junction=="J2145_15970"||revision<24&&junction=="J5074_877"||revision<23&&(junction=="J7876_1656"||junction=="J8346_1658")||revision<22&&(junction=="J7254_14780"||junction=="J10587_15883")||revision<21&&(junction=="J1882_11791"||junction=="J2225_12001")||revision<20&&(new[]{"J8921_10835","J7355_12821","J7292_13794","J7254_14780","J8066_6364","J7917_2565"}.Contains(junction)||junction=="J7973_3744"&&revision!=16||junction=="J1927_5181"&&revision==14);
   if(target>=28&&revision<28&&junction=="J7254_14780")clear=true;
   if(target>=29&&revision<29&&junction=="J5692_14220")clear=true;
   if(target>=30&&revision<30&&(junction=="J6478_4105"||junction=="J6481_3341"))clear=true;
   if(target>=31&&revision<31&&(junction=="J5076_2580"||junction=="J5750_2576"||junction=="J6480_2573"||junction=="J7204_2569"))clear=true;
   if(target>=33&&revision<33&&(junction=="J6481_3341"||junction=="J5750_2576"||junction=="J7204_2569"))clear=true;
   if(target>=34&&revision<34&&junction=="J6199_11987")clear=true;
   if(target>=35&&revision<35&&(junction=="J3516_12881"||junction=="J7801_9629"))clear=true;
   if(target>=36&&revision<36&&junction=="J7841_867")clear=true;
   if(target>=37&&revision<37&&junction=="J1919_877")clear=true;
   if(target>=38&&revision<38&&(junction=="J1927_5181"||junction=="J7841_867"))clear=true;
   if(target>=39&&revision<39&&(junction=="J6481_3341"||junction=="J3516_12881"))clear=true;
   if(target>=40&&revision<40&&junction=="J4396_13458")clear=true;
   if(target>=41&&revision<41&&(junction=="J4396_13458"||junction=="J5692_14220"))clear=true;
   if(target>=42&&revision<42&&(junction=="J2564_12237"||junction=="J7254_14780"||junction=="J8921_10835"||junction=="J7841_867"||junction=="J5076_2580"||junction=="J8066_6364")||
    target>=43&&revision<43&&(junction=="J1927_5181"||junction=="J8066_6364"||junction=="J7841_867"||junction=="J7876_1656"||junction=="J5852_16963"||junction=="J7254_14780")||target>=44&&revision<44&&junction=="J1927_5181"||target>=45&&revision<45&&junction=="J5692_14220"||target>=46&&revision<46&&junction=="J7841_867"||
    target>=47&&revision<47&&(junction=="J7841_867"||junction=="J7876_1656"||junction=="J6481_3341"||junction=="J6480_2573"||junction=="J7973_3744")||target>=51&&revision<target&&junction=="reviewed:r47:P51"){
    clear=true;expected.jobs.manners.crossing=null;
    expected.jobs.manners.lawObservation.entry=null;expected.jobs.manners.lawObservation.entryPresent=false;
   }
   if(clear){expected.lawObservation.entry=null;expected.lawObservation.entryPresent=false;}
   // Exercise the version/junction matrix without serializing the entire map for each fixture.
   // Separate cases below run the exact-default gate used by the actual game checkpoint.
   migrate.Invoke(null,new object[]{saved,revision,target});
   if(JsonUtility.ToJson(saved)!=JsonUtility.ToJson(expected))throw new Exception("reviewed migration changed protected metadata or missed invalidation from r"+revision+" at "+junction);
   checks++;
  }
  var originalDocument=map.editorDocument;
  try{
   foreach(bool wrongHash in new[]{true,false}){
    map.editorDocument=originalDocument.Copy();if(wrongHash)map.editorDocument.projectRevision="unreviewed-hash";else map.editorDocument.name+=" user edit";
    var saved=MigrationFixture(target-1,"J2145_15970");string before=JsonUtility.ToJson(saved);method.Invoke(null,new object[]{saved,map});
    if(JsonUtility.ToJson(saved)!=before)throw new Exception("migration accepted "+(wrongHash?"wrong content hash":"user-edited map"));checks++;
   }
  }finally{map.editorDocument=originalDocument;}
  foreach(int revision in new[]{13,target}){var saved=MigrationFixture(revision,"J2225_12001");string before=JsonUtility.ToJson(saved);method.Invoke(null,new object[]{saved,map});if(JsonUtility.ToJson(saved)!=before)throw new Exception("migration accepted unsupported/same revision "+revision);checks++;}
  foreach(int revision in new[]{14,target-1}){
   var saved=MigrationFixture(revision,"J7841_867");var expected=JsonUtility.FromJson<LiveCampaignCheckpoint>(JsonUtility.ToJson(saved));
   migrate.Invoke(null,new object[]{expected,revision,target});method.Invoke(null,new object[]{saved,map});
   if(JsonUtility.ToJson(saved)!=JsonUtility.ToJson(expected))throw new Exception("exact default did not execute migration from r"+revision);
   checks++;
  }
  if(target>=42){
   // r48 repaints Suin-ro only; the latest approach change (J7841) is checked from r46, P51 (r51) from r50.
   var legacy=MigrationFixture(target>=51?target-1:target>=48?46:target-1,target>=51?"reviewed:r47:P51":target>=46?"J7841_867":target>=45?"J5692_14220":target>=44?"J1927_5181":target>=43?"J5852_16963":"J2564_12237");legacy.lawObservation=null;legacy.lawObservationPresent=false;legacy.operationVersion=0;
   method.Invoke(null,new object[]{legacy,map});
   if(legacy.jobs.manners.crossing!=null||legacy.jobs.manners.lawObservation.entryPresent||legacy.jobs.manners.lawObservation.entry!=null||
    legacy.jobs.manners.score!=87||legacy.jobs.manners.events!=3)throw new Exception("migration did not preserve the score while clearing legacy crossing state");
   checks++;
  }
  Debug.Log("JUNCTION_MIGRATION_OK target=r"+target+" versions="+(target-14)+" checks="+checks+" metadata=preserved strict-default-hash=required");
 }
 static LiveCampaignCheckpoint MigrationFixture(int revision,string junction){return new LiveCampaignCheckpoint{
  mapRevision="은계 프로젝트 지도 · r"+revision,tripId="migration-trip",position=new Vector3(800,0,1200),rotation=Quaternion.Euler(0,17,0),preparing=revision%2==1,preferFastest=revision%2==0,hasDestination=true,destination=new Vector3(450,0,640),realSeconds=123.5,
  jobs=new PassengerJobsSnapshot{navigationCaptured=true,navFastest=true,navAvoidEdge=2000,navObservedEdge=2001,navPassedObservedEdge=2002,balance=12345,completed=2,failed=1,generation=7,offerId="stable-offer",message="preserve",presenceCaptured=true,hasCurrent=true,changeApplied=true,deadlineNotice=true,waitingCaptured=true,roadsideWaiting=new[]{true,false},stopIds=new[]{"pickup-stable","drop-stable"},current=new JobOffer{pickup=2,drop=7,fare=6400,requiredScore=82,pickupSeconds=45,seconds=120,distance=850,waitSeconds=9,scenarioId="stable-scenario",pickupStopId="pickup-stable",dropStopId="drop-stable",mapRevision="original-offer-metadata",catalogRevision="stable-catalog",candidateSeed="stable-seed"},
   manners=new DrivingMannersSnapshot{score=87,events=3,crossing=new MannerCrossingSnapshot{junction=junction,axis=1,sign=1,entered=true,left=true},lawObservation=new DrivingLawSnapshot{observationVersion=1,entryPresent=true,entry=new LawEntrySnapshot{junction=junction,id="manner-entry-id",axis=1,sign=1,entered=true}}}},
  operationVersion=1,signalsPresent=true,signals=new TaxiSignalsSnapshot{direction=1,manualHazards=true,passengerStop=true},lawObservationPresent=true,lawObservation=new DrivingLawSnapshot{observationVersion=1,entryPresent=true,wrongSeconds=2,wrongDistance=4,clearSeconds=3,wrongLatched=true,entry=new LawEntrySnapshot{junction=junction,id="entry-id",axis=1,sign=1,entered=true,stopped=true,necessaryBefore=2}}
 };}
 public static void Run(){Verify();VerifyDestination();J7917LaneMapEdit.RunChecks();PlaytestBalanceTests.Run();Debug.Log("JUNCTION_FEEDBACK_REGRESSION_OK");}
 public static void Verify(){Verify(ProjectMapStore.Load(ProjectMapBuild.Root));}
 static void Verify(ProjectMapPackage package){
  var composed=package.Compose();EungyeConnectivityRepair.Apply(composed,package.baseline);var g=new HdRoadGraph(composed);
  foreach(var pair in new[]{new[]{"reviewed:J7917:middle:local:7848:3507:7812:2752:part:1","join:local:7924:3503:7887:2749:-1"},new[]{"reviewed:J7917:middle:local:8039:2741:8076:3496:part:1","A2218G611104:0"}}){
   int start=Array.FindIndex(g.edges,e=>e.id==pair[0]),turn=Array.FindIndex(g.edges,e=>e.id==pair[1]);var r=g.Route(start,1,turn,g.paths[turn].length);
   Debug.Log("JUNCTION_FEEDBACK_ROUTE "+pair[0]+" distance="+(r==null?-1:r.path.length));
   if(r==null||r.path.length>150)throw new Exception("Adjacent middle lane cannot prepare for local left turn: "+pair[0]);
  }
  Debug.Log("JUNCTION_FEEDBACK_OK");
 }
 public static void VerifyDestination(){
  var p=ProjectMapStore.Load(ProjectMapBuild.Root);var map=p.Compose();EungyeConnectivityRepair.Apply(map,p.baseline);var g=new HdRoadGraph(map);
  var goal=map.stops.First(s=>s.name.Contains("국도42")&&s.name.EndsWith("1"));
  foreach(var pose in new[]{new Vector3(791,0,423),new Vector3(797,0,470)}){
   float at;int start=g.Nearest(pose,Vector3.back,out at);var r=g.Route(start,at,goal.laneEdge,goal.laneAt);
   Debug.Log("JUNCTION_DESTINATION_ROUTE pose="+pose+" distance="+(r==null?-1:r.path.length));
   if(r==null||r.path.length>1150||!r.edges.Any(i=>g.edges[i].id=="A2218G611104:0"))throw new Exception("Westbound stop still bypasses J7973 northbound left");
   if(Vector3.Distance(r.path.Position(r.path.length),g.paths[goal.laneEdge].Position(goal.laneAt))>.01f)throw new Exception("Destination changed");
  }
  Debug.Log("JUNCTION_DESTINATION_OK");
 }
 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);var p=ProjectMapStore.Load(root);
  if(p.document.revision!=17)throw new Exception("Expected r17");
  p.document=PlaytestLaneChangeRepair.ApplyJunctionFeedback(p);p.document.revision=18;p.document.savedAt=DateTime.UtcNow.ToString("o");p.document.projectRevision="";
  p.contentHash=ProjectMapPackage.Digest(JsonUtility.ToJson(p.document));Verify(p);ProjectMapStore.Save(root,p,token);ProjectMapBuild.Prepare();Debug.Log("JUNCTION_FEEDBACK_SAVED "+p.contentHash);
 }
 public static void Diagnose(){
  var package=ProjectMapStore.Load(ProjectMapBuild.Root);var map=package.Compose();EungyeConnectivityRepair.Apply(map,package.baseline);var g=new HdRoadGraph(map);
  var goal=map.stops.First(s=>s.name.Contains("국도42")&&s.name.EndsWith("1"));
  Debug.Log("JUNCTION_GOAL "+g.edges[goal.laneEdge].id+" at="+goal.laneAt);
  foreach(string id in new[]{"A2218G611104:0","A2218G611447:0","local:7963:2745:8000:3499","local:8039:2741:8076:3496"}){int i=Array.FindIndex(g.edges,e=>e.id==id);var r=g.Route(i,0,goal.laneEdge,goal.laneAt);Debug.Log("JUNCTION_GOAL_FROM "+id+" length="+(r==null?-1:r.path.length)+" chain="+(r==null?"none":string.Join(" | ",r.edges.Select(e=>g.edges[e].id))));}
  foreach(float z in new[]{470f,440f,423f})foreach(float x in new[]{785f,791f,797f,800f}){
   float at;int start=g.Nearest(new Vector3(x,0,z),Vector3.back,out at);if(start<0)continue;
   var r=g.Route(start,at,goal.laneEdge,goal.laneAt);
   Debug.Log("JUNCTION_ROUTE pose="+x+","+z+" start="+g.edges[start].id+" distance="+(r==null?-1:r.path.length)+" turns="+(r==null?"none":string.Join(" | ",r.edges.Where(i=>g.edges[i].turn!=0).Select(i=>g.edges[i].id))));
  }
  foreach(var e in g.edges.Where(e=>!e.disabled&&e.turn==-1)){
   var a=e.points[0].V();if(Vector3.Distance(a,new Vector3(797.33f,0,374.4f))<65)Debug.Log("JUNCTION_LEFT "+e.id+" start="+a+" end="+e.points.Last().V());
  }
 }
}
