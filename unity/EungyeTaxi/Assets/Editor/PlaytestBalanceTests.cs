using System;
using System.Reflection;
using UnityEngine;

public static class PlaytestBalanceTests {
 public static void RunFinalFindings(){Run();}
 static int failed;
 static void Check(bool ok,string why){Debug.Log((ok?"BALANCE_PASS ":"BALANCE_FAIL ")+why);if(!ok)failed++;}
 public static void Run(){
  failed=0;
  var red=new RoadObservation{known=true,trafficClear=true,forward=SignalColor.Red,pedestrian=PedestrianState.None};
  Check(LawEvaluator.RightTurn("stop",1,red,false).Deduction==5,"simple missed right stop costs five driving points");
  red.dedicated=true;red.right=SignalColor.Red;
  Check(LawEvaluator.RightTurn("dedicated",1,red,false).Deduction==20,"dedicated red remains severe");
  red.pedestrian=PedestrianState.Crossing;
  Check(LawEvaluator.RightTurn("ped",1,red,false).Deduction==25,"pedestrian danger remains severe and uses incident maximum");
  Check(OneDayOfferPolicy.PickupSeconds(0)==150&&OneDayOfferPolicy.PickupSeconds(1732)>=700,"pickup reduced thirty seconds while retaining measured long route margin");
  var intrusion=typeof(LawEvaluator).GetMethod("StopLine");var incident=typeof(LawLedger).GetMethod("RecordIncident");
  Check(intrusion!=null&&incident!=null,"stop-line scoring and same-incident escalation exist");
  if(intrusion!=null&&incident!=null){
   var green=new RoadObservation{known=true,forward=SignalColor.Green,pedestrian=PedestrianState.None};
   var clear=(LawEvent)intrusion.Invoke(null,new object[]{"green",1,green});Check(clear.Deduction==0,"ordinary green stop-line passage never penalized");
   var line=(LawEvent)intrusion.Invoke(null,new object[]{"one",1,new RoadObservation{known=true,forward=SignalColor.Red}});var ledger=new LawLedger();incident.Invoke(ledger,new object[]{line});
   Check(ledger.TotalScore==99,"isolated red stop-line intrusion costs one point");
   var revision=typeof(LawLedger).GetProperty("Revision");int before=revision==null?-1:(int)revision.GetValue(ledger,null);
   var full=new LawEvent{id="one",day=1};full.Add("L01",LawVerdict.Violation,20);incident.Invoke(ledger,new object[]{full});
   Check(ledger.Count==1&&ledger.TotalScore==80,"full red violation replaces same incident deduction without stacking");
   Check(revision!=null&&(int)revision.GetValue(ledger,null)>before,"same-count escalation invalidates journal and notification observers");
   var campaign=new LawLedger();campaign.Record(line);DrivingScoreJournal.Restore(new DrivingScoreSnapshot{campaign="restore-incident",sequence=1,events=new[]{full}},new PrototypeIds("restore-incident"),campaign);
   Check(campaign.Count==1&&campaign.TotalScore==80,"newer independent journal upgrades same-ID intrusion in older campaign checkpoint");
   DrivingScoreJournal.Restore(new DrivingScoreSnapshot{campaign="restore-incident",sequence=1,events=new[]{line}},new PrototypeIds("restore-incident"),campaign);Check(campaign.TotalScore==80,"stale journal cannot downgrade full violation in newer campaign checkpoint");
  }
  var transport=typeof(OneDayOfferPolicy).GetMethod("TransportSeconds");
  Check(transport!=null,"new transport deadline policy available");
  if(transport!=null){
   float hurry=(float)transport.Invoke(null,new object[]{100f,20f,2});
   float normal=(float)transport.Invoke(null,new object[]{100f,20f,1});
   float relaxed=(float)transport.Invoke(null,new object[]{100f,20f,0});
   Check(hurry==185&&normal==210&&relaxed==240,"same safe ETA keeps traffic margin and ordered passenger urgency");
  }
  var root=new GameObject("balance wrong-way regression");
  try{
   var law=root.AddComponent<DrivingLaw>();var observe=typeof(DrivingLaw).GetMethod("ObserveWrongWay");
   Check(observe!=null,"directed-road wrong-way observer exists");
   if(observe!=null){
    var s=root.AddComponent<DriveSession>();s.taxi=root.AddComponent<TaxiController>();s.taxi.body=root.AddComponent<Rigidbody>();s.jobs=root.AddComponent<PassengerJobs>();s.jobs.nav=root.AddComponent<RoadNavigation>();s.business=root.AddComponent<BusinessProgress>();s.business.ledger=new EconomyLedger(PrototypeSettings.ForThreeDays());s.manners=root.AddComponent<DrivingManners>();s.manners.session=s;s.law=law;law.session=s;
    s.jobs.nav.hd=new HdRoadGraph(new MapData{hdEdges=new[]{new HdEdge{id="road",next=new int[0],points=new[]{new MapPoint{x=0,z=0},new MapPoint{x=0,z=200}}}}});
    root.transform.position=new Vector3(0,0,100);root.transform.forward=Vector3.back;s.taxi.body.velocity=Vector3.back*4;
    for(int i=0;i<4;i++)observe.Invoke(law,new object[]{.5f});
    Check(law.ledger.TotalScore==90&&s.manners.ledger.Score==90,"persistent forward travel against actual lane costs driving and manner ten once");
    for(int i=0;i<8;i++)observe.Invoke(law,new object[]{.5f});
    Check(law.ledger.Count==1,"continuous wrong-way episode never repeats");
    root.transform.forward=Vector3.forward;s.taxi.body.velocity=Vector3.back*4;
    for(int i=0;i<8;i++)observe.Invoke(law,new object[]{.5f});
    Check(law.ledger.Count==1,"backing along road cannot create wrong-way event");
    var saved=JsonUtility.FromJson<DrivingLawSnapshot>(JsonUtility.ToJson(law.CaptureObservation()));law.RestoreObservation(saved);root.transform.forward=Vector3.back;
    for(int i=0;i<8;i++)observe.Invoke(law,new object[]{.5f});
    Check(law.ledger.Count==1,"restored ongoing wrong-way latch survives actual JsonUtility roundtrip");
    law.ResetObservation();law.ledger=new LawLedger();root.transform.forward=Vector3.forward;s.taxi.body.velocity=Vector3.forward*4;
    for(int i=0;i<4;i++)observe.Invoke(law,new object[]{.5f});
    root.transform.forward=Vector3.back;s.taxi.body.velocity=Vector3.back*4;observe.Invoke(law,new object[]{.5f});root.transform.forward=Vector3.forward;s.taxi.body.velocity=Vector3.forward*4;observe.Invoke(law,new object[]{.5f});
    Check(law.ledger.Count==0,"brief heading/velocity excursion is not persistent wrong-way driving");
    s.jobs.nav.hd=new HdRoadGraph(new MapData{hdEdges=new[]{new HdEdge{id="forward",next=new int[0],points=new[]{new MapPoint{x=0,z=0},new MapPoint{x=0,z=200}}},new HdEdge{id="opposite",next=new int[0],points=new[]{new MapPoint{x=3.5f,z=200},new MapPoint{x=3.5f,z=0}}}}});
    root.transform.position=new Vector3(0,0,100);root.transform.forward=Vector3.forward;s.taxi.body.velocity=Vector3.forward*4;for(int i=0;i<8;i++)observe.Invoke(law,new object[]{.5f});
    Check(law.ledger.Count==0,"legal lane remains legal beside a close opposite-direction parallel lane");
    s.traffic=root.AddComponent<TrafficSystem>();s.traffic.junctions.Add(new TrafficJunction{center=root.transform.position});root.transform.forward=Vector3.back;s.taxi.body.velocity=Vector3.back*4;for(int i=0;i<8;i++)observe.Invoke(law,new object[]{.5f});
    Check(law.ledger.Count==0,"junction turning area excluded from wrong-way classification");s.traffic.junctions.Clear();
    s.jobs.nav.hd=new HdRoadGraph(new MapData{hdEdges=new[]{new HdEdge{id="join:uturn",turn=-2,next=new int[0],points=new[]{new MapPoint{x=0,z=0},new MapPoint{x=0,z=200}}}}});for(int i=0;i<8;i++)observe.Invoke(law,new object[]{.5f});
    Check(law.ledger.Count==0,"authored U-turn connector never classified as wrong-way lane");
    var map=new MapData{hdEdges=new[]{new HdEdge{id="road",next=new int[0],points=new[]{new MapPoint{x=0,z=0},new MapPoint{x=0,z=200}}}}};s.jobs.nav.hd=new HdRoadGraph(map);s.world=root.AddComponent<WorldBuilder>();s.world.map=map;s.jobs.session=s;s.taxi.body.velocity=Vector3.zero;root.transform.forward=Vector3.forward;
    s.jobs.stops=new[]{new MapStop{laneEdge=0,laneAt=50,dx=0,dz=1},new MapStop{laneEdge=0,laneAt=150,dx=0,dz=1},new MapStop{laneEdge=0,laneAt=150,dx=0,dz=1}};s.jobs.current=new JobOffer{pickup=0,drop=1,timingPolicyVersion=1,urgency=2};s.jobs.trip.Accept(5000,300);s.jobs.trip.Board();float extra,remaining;int fare;
    Check(s.jobs.PreviewPassengerChange(2,false,out extra,out fare,out remaining)&&extra==0&&remaining==330,"equal-distance destination change grants only thirty-second stop margin without minimum deadline windfall");
   }
  }finally{UnityEngine.Object.DestroyImmediate(root);}
  RunSignals();
  if(failed>0)throw new Exception("Playtest balance regressions: "+failed);Debug.Log("PLAYTEST_BALANCE_OK");
 }
 static void RunSignals(){
  var root=new GameObject("actual stop-line observer regression");
  try{
   var s=root.AddComponent<DriveSession>();s.taxi=root.AddComponent<TaxiController>();s.taxi.body=root.AddComponent<Rigidbody>();s.jobs=root.AddComponent<PassengerJobs>();s.jobs.session=s;s.jobs.nav=root.AddComponent<RoadNavigation>();s.business=root.AddComponent<BusinessProgress>();s.business.ledger=new EconomyLedger(PrototypeSettings.ForThreeDays());s.manners=root.AddComponent<DrivingManners>();s.manners.session=s;s.law=root.AddComponent<DrivingLaw>();s.law.session=s;s.assisted=root.AddComponent<AssistedDriveController>();s.traffic=root.AddComponent<TrafficSystem>();
   typeof(TrafficSystem).GetField("roadMap",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(s.traffic,new MapData());s.traffic.mainDirection=Vector3.forward;
   var j=new TrafficJunction{name="line",direction=Vector3.forward,scenarioSignal=true,scenarioForward=SignalColor.Red,observationAxis=0,observationSign=1};s.traffic.junctions.Add(j);
   var tick=typeof(DrivingLaw).GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic);
   for(int green=0;green<3;green++){
    s.law.ledger=new LawLedger();s.law.ResetObservation();j.scenarioForward=green==1?SignalColor.Green:SignalColor.Red;
    root.transform.forward=Vector3.forward;root.transform.position=new Vector3(2,0,-17);s.taxi.body.velocity=Vector3.zero;tick.Invoke(s.law,null);
    root.transform.position=new Vector3(2,0,-15.5f);tick.Invoke(s.law,null);tick.Invoke(s.law,null);
    Check(s.law.ledger.TotalScore==(green==1?100:99),green==1?"actual green line crossing and stop has no encroachment penalty":"actual red line encroachment immediately costs one");
    if(green==0){var saved=JsonUtility.FromJson<DrivingMannersSnapshot>(JsonUtility.ToJson(s.manners.CaptureLive()));s.manners.RestoreLive(saved);tick.Invoke(s.law,null);Check(s.law.ledger.Count==1,"saved intrusion observation resumes same incident without duplicating one point");}
    if(green==2)j.scenarioForward=SignalColor.Green;
    for(float z=-7.5f;z<=16.5f;z+=8){root.transform.position=new Vector3(2,0,z);tick.Invoke(s.law,null);}
    Check(s.law.ledger.Count==1&&s.law.ledger.TotalScore==(green==1?100:green==2?99:80),green==1?"actual green through crossing never creates stop-line penalty":green==2?"intrusion then waiting for green before junction entry keeps only one-point penalty":"actual intrusion followed by full red crossing costs twenty total");
   }
   s.law.ledger=new LawLedger();s.law.ResetObservation();j.scenarioForward=SignalColor.Red;root.transform.forward=Vector3.forward;root.transform.position=new Vector3(2,0,-17);tick.Invoke(s.law,null);root.transform.position=new Vector3(2,0,-16.5f);tick.Invoke(s.law,null);root.transform.position=new Vector3(2,0,-15.5f);tick.Invoke(s.law,null);tick.Invoke(s.law,null);
   Check(s.law.ledger.TotalScore==99,"correct initial stop cannot exempt later red stop-line creep");root.transform.forward=Vector3.right;root.transform.position=new Vector3(5,0,-12);tick.Invoke(s.law,null);
   Check(s.law.ledger.TotalScore==99&&s.law.ledger.Events[0].rules.Exists(x=>x.rule=="L02"&&x.verdict==LawVerdict.Compliant),"creep intrusion preserves earlier right-turn stopping compliance without extra missed-stop penalty");
   s.manners.BeginRide();root.transform.forward=Vector3.forward;s.taxi.body.velocity=Vector3.forward*3;root.transform.position=new Vector3(2,0,-15.5f);s.manners.CheckSignal(new Vector3(2,0,-15),new Vector3(2,0,-13.5f));var pending=JsonUtility.FromJson<DrivingMannersSnapshot>(JsonUtility.ToJson(s.manners.CaptureLive()));s.manners.RestoreLive(pending);var prior=new Vector3(2,0,-13.5f);for(float z=-5.5f;z<=18.5f;z+=8){if(z>0)j.scenarioForward=SignalColor.Green;var front=new Vector3(2,0,z);root.transform.position=front-Vector3.forward*2;s.manners.CheckSignal(prior,front);prior=front;}
   Check(s.manners.ledger.Score==88,"saved pending actual red entry keeps manner penalty despite later green inside junction");
   s.manners.BeginRide();j.scenarioForward=SignalColor.Red;root.transform.forward=Vector3.forward;root.transform.position=new Vector3(2,0,-15.5f);s.taxi.body.velocity=Vector3.zero;s.manners.CheckSignal(new Vector3(2,0,-15),new Vector3(2,0,-13.5f));s.manners.CheckSignal(new Vector3(2,0,-13.5f),new Vector3(2,0,-13.5f));j.scenarioForward=SignalColor.Green;prior=new Vector3(2,0,-13.5f);for(float z=-5.5f;z<=18.5f;z+=8){var front=new Vector3(2,0,z);root.transform.position=front-Vector3.forward*2;s.manners.CheckSignal(prior,front);prior=front;}
   Check(s.manners.ledger.Score==100,"red line intrusion then green before actual junction entry never creates false manner red penalty");
   j.scenarioForward=SignalColor.Red;s.manners.BeginRide();root.transform.forward=Vector3.forward;s.taxi.body.velocity=Vector3.forward*3;root.transform.position=new Vector3(2,0,-15.5f);s.manners.CheckSignal(new Vector3(2,0,-15),new Vector3(2,0,-13.5f));root.transform.forward=Vector3.right;root.transform.position=new Vector3(8,0,-8);s.taxi.body.velocity=Vector3.right*3;s.manners.CheckSignal(new Vector3(8,0,-8),new Vector3(10,0,-8));Check(s.manners.ledger.Score==96,"actual clear red right turn missing stop costs four manner points");
   var notices=root.AddComponent<DriveNotifications>();notices.session=s;s.law.ledger=new LawLedger();notices.RefreshScores();s.law.ledger.RecordIncident(LawEvaluator.StopLine("notice",1,new RoadObservation{known=true,forward=SignalColor.Red}));notices.RefreshScores();var severe=new LawEvent{id="notice",day=1,action="직진"};severe.Add("L01",LawVerdict.Violation,20);s.law.ledger.RecordIncident(severe);notices.RefreshScores();Check(Array.Exists(notices.VisibleTexts,x=>x.Contains("-19")),"same-count escalation immediately notifies only the nineteen additional points");
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
}
