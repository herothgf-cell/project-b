using System;
using UnityEngine;

public static class ProtoExpansionTests {
 public static void RunP12(){
  Check(typeof(PassengerRequest).Assembly.GetType("OperationMetrics")!=null,"operation metrics must record empty travel separately");
  Check(PassengerDemandPolicy.Weight("residential",299.9)==3&&PassengerDemandPolicy.Weight("residential",300)==1&&PassengerDemandPolicy.Weight("office",300)==3&&PassengerDemandPolicy.Weight("commercial",900)==3&&PassengerDemandPolicy.Weight("commercial",99999)==3,"demand phase boundaries and overtime");
  Check(PassengerDemandPolicy.Region("unknown")=="neutral"&&PassengerDemandPolicy.Weight("neutral",0)==1,"unknown candidates neutral");
  int early=0,late=0;for(int i=0;i<2000;i++){if(PassengerDemandPolicy.Priority("draw"+i,"EG-GAME-03",0)<PassengerDemandPolicy.Priority("draw"+i,"EG-GAME-02",0))early++;if(PassengerDemandPolicy.Priority("draw"+i,"EG-GAME-03",900)<PassengerDemandPolicy.Priority("draw"+i,"EG-GAME-02",900))late++;}
  Check(early>1300&&late<700,"same candidates change relative selection, not fares");
  var m=new OperationMetrics();m.Observe(100,10,false,false);m.Observe(0,20,false,false);m.Observe(200,20,true,false);m.Observe(0,30,false,true);m.Observe(10000,.02,false,false);
  var v=m.Capture();Check(v.emptyMetres==100&&v.occupiedMetres==200&&v.emptyMovingSeconds==10&&v.emptyWaitingSeconds==20&&v.serviceSeconds==30,"pickup, occupancy, waiting, service and teleport boundaries");
  var copy=OperationMetrics.Restore(v);copy.Observe(10,1,false,false);Check(copy.Capture().emptyMetres==110&&v.emptyMetres==100,"restore is independent and does not replay distance");
  Check(OperationMetrics.Restore(null,120).Capture().startedAt==120,"legacy history starts at restore boundary");
  var e=new EconomyLedger(PrototypeSettings.ForThreeDays());e.AdvanceOperation(10,100,false);e.AdvanceService(10);Check(e.Elapsed==20&&e.Metrics.Capture().serviceSeconds==10,"service advances clock once");
  var resumed=EconomyLedger.RestoreCampaign(e.Snapshot(),e.Account.Snapshot());Check(resumed.Metrics.Capture().emptyMetres==100&&resumed.Elapsed==20,"active metrics restored with accounting boundary");
  e.RequestClose();Check(e.Settle()&&e.Last.metrics.emptyMetres==100,"daily settlement captures metrics");Check(e.NextDay()&&e.Metrics.Capture().emptyMetres==0&&e.TotalMetrics.emptyMetres==100,"day reset retains campaign total once");
  RunP11();
  Debug.Log("PROTO_P12_OK");
 }
 static void Check(bool condition,string message){if(!condition)throw new Exception("P11: "+message);}
 public static void RunP11(){
  Check(typeof(PassengerRequest).Assembly.GetType("PassengerRequestSchedule")!=null,
   "candidate-stable request scheduler must exist before multi-request trips can run");
  int[] counts=new int[3];bool boarding=false,middle=false;
  var kinds=(RequestKind[])Enum.GetValues(typeof(RequestKind));
  for(int n=0;n<3000;n++){
   var a=PassengerRequestSchedule.Create("candidate-"+n,kinds);
   var b=PassengerRequestSchedule.Create("candidate-"+n,kinds);
   var x=a.Capture();var y=b.Capture();counts[x.slots.Length]++;
   Check(JsonUtility.ToJson(x)==JsonUtility.ToJson(y),"same candidate retains draw");
   foreach(var slot in x.slots){Check(slot.kind!=RequestKind.LostItem,"lost item is post ride only");boarding|=slot.threshold==0;middle|=slot.threshold>=.25f&&slot.threshold<=.65f;}
   if(x.slots.Length==2)Check(PassengerRequestSchedule.Compatible(x.slots[0].kind,x.slots[1].kind),"conflicting promises excluded");
  }
  Check(counts[0]>400&&counts[0]<800&&counts[1]>1500&&counts[1]<2100&&counts[2]>400&&counts[2]<800,"0/1/2 distribution");
  Check(boarding&&middle,"boarding and midride variation");
  var saved=new PassengerRequestScheduleSnapshot{version=1,active=-1,slots=new[]{new PassengerRequestSlot{kind=RequestKind.Comfort,threshold=.25f},new PassengerRequestSlot{kind=RequestKind.Eta,threshold=.4f}}};
  var schedule=PassengerRequestSchedule.Restore(saved);
  Check(schedule.Tick(19,.4f,true,false)==-1,"twenty moving seconds required");
  Check(schedule.Tick(1,.4f,false,false)==-1,"moving cannot present");
  Check(schedule.Tick(0,.4f,true,false)==0,"first safe stop presents");
  schedule.Responded();Check(schedule.Tick(20,.6f,true,false)==-1,"same stop cannot present second");
  Check(schedule.Tick(0,.1f,false,false)==-1,"progress cannot regress after reroute");
  Check(schedule.Tick(0,.1f,true,false)==1,"distinct stop and retained progress");
  var restored=PassengerRequestSchedule.Restore(schedule.Capture());
  Check(restored.Capture().slots[1].presented,"restore does not redraw");
  schedule=PassengerRequestSchedule.Restore(saved);
  Check(schedule.Tick(25,.85f,true,false)==-1&&schedule.Capture().slots[0].closed,"late request neutral");
  Check(PassengerRequestSchedule.Compatible(RequestKind.Quiet,RequestKind.Comfort),"quiet and comfort coexist");
  Check(!PassengerRequestSchedule.Compatible(RequestKind.Quiet,RequestKind.Interview)&&!PassengerRequestSchedule.Compatible(RequestKind.ShortRoad,RequestKind.RouteAdvice)&&!PassengerRequestSchedule.Compatible(RequestKind.ViaStop,RequestKind.SafeStop),"explicit conflict cases");
  IndependentObservations();
  Debug.Log("PROTO_P11_OK");
 }
 static void IndependentObservations(){
  var root=new GameObject("P11 observations");
  try{
   var s=root.AddComponent<DriveSession>();s.business=root.AddComponent<BusinessProgress>();s.business.useV2=true;
   s.jobs=root.AddComponent<PassengerJobs>();s.jobs.session=s;s.jobs.nav=root.AddComponent<RoadNavigation>();s.jobs.trip.Accept(5000,300,90);s.jobs.trip.Board();
   s.taxi=root.AddComponent<TaxiController>();s.taxi.body=root.AddComponent<Rigidbody>();s.taxi.body.useGravity=false;
   s.traffic=root.AddComponent<TrafficSystem>();s.law=root.AddComponent<DrivingLaw>();s.law.tripId="fixture";
   s.manners=root.AddComponent<DrivingManners>();s.manners.session=s;
   s.requests=root.AddComponent<PassengerRequestUI>();s.requests.session=s;
   var a=new PassengerRequest();a.Begin("fixture",RequestKind.Comfort);a.Accept("comfort");
   var b=new PassengerRequest();b.Begin("fixture",RequestKind.Quiet);b.Accept("quiet");
   var state=new PassengerRequestUISnapshot{version=2,active=1,initialDistance=300,preferFast=true,schedule=new PassengerRequestScheduleSnapshot{active=1,slots=new[]{new PassengerRequestSlot{kind=RequestKind.Comfort,presented=true},new PassengerRequestSlot{kind=RequestKind.Quiet,presented=true,threshold=.4f}}},observations=new[]{new PassengerRequestObservationSnapshot{request=a.Snapshot(),preferFast=true,observedTrip="fixture"},new PassengerRequestObservationSnapshot{request=b.Snapshot(),preferFast=true,quietReply=true,observedTrip="fixture"}}};
   s.requests.RestoreLive(state);
   Check(s.manners.ComfortAccepted&&s.requests.SuppressFollowupDialogue,"first comfort and second quiet effects retained");
   s.requests.Finish();Check(s.requests.TotalSatisfactionDelta==10,"8+5 clamped once to10");
   Check(s.requests.Summary.Contains("comfort")&&s.requests.Summary.Contains("quiet"),"both result records retained");
   var snapshot=JsonUtility.FromJson<PassengerRequestUISnapshot>(JsonUtility.ToJson(s.requests.CaptureLive()));
   s.requests.RestoreLive(snapshot);s.requests.Finish();Check(s.requests.TotalSatisfactionDelta==10,"new JSON restore cannot settle twice");
   s.requests.RestoreLive(new PassengerRequestUISnapshot{request=b.Snapshot(),preferFast=true,observedTrip="fixture"});
   Check(s.requests.request.Kind==RequestKind.Quiet&&s.requests.CaptureLive().version==0,"legacy single promise preserved without draw");
   s.requests.BeginKind(RequestKind.Quiet);Check(s.requests.SafeToRespond(),"ordinary safe fixture stop");
   s.traffic.junctions.Add(new TrafficJunction{center=Vector3.zero,direction=Vector3.forward});
   Check(!s.requests.SafeToRespond(),"junction interior blocked");
   root.transform.position=new Vector3(2,0,-18);Check(s.requests.SafeToRespond(),"behind crossing signal queue not blanket rejected");
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
}
