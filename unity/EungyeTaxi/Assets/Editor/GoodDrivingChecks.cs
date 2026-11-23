using System;
using System.IO;
using UnityEngine;

// 08 chapter 2: tip, clean-run streak and good-driving praise rules (Editor, no scene).
public static class GoodDrivingChecks {
 static int checks;
 static void Check(bool ok,string name){checks++;if(!ok)throw new Exception("GOOD_DRIVING "+name);}
 public static void Run(){
  checks=0;
  ContentTuning.ConfigureCatalog(File.ReadAllText(Path.GetFullPath("../../config/content/catalog.json")));
  ContentTuning.ValidateJson(File.ReadAllText(Path.GetFullPath("../../config/content/default-profile.json")));
  // Defaults from the catalog/profile.
  Check(GoodDrivingRules.SmoothMaxDecel==2.5f&&GoodDrivingRules.SmoothFinalDecel==1.2f&&GoodDrivingRules.PreciseDistance==.3f&&GoodDrivingRules.LaneMaxPerTrip==2,"draft thresholds");
  Check(GoodDrivingRules.Points(5,5,true,true)==6&&GoodDrivingRules.Points(1,0,false,false)==1&&GoodDrivingRules.Points(-3,1,false,true)==2,"points cap smooth/lane at 2 each");
  Check(GoodDrivingRules.Chance(0,0)==20&&GoodDrivingRules.Chance(3,0)==50&&GoodDrivingRules.Chance(3,2)==60&&GoodDrivingRules.Chance(6,9)==80,"chance: base 20, +10/point, +5/streak (cap 25), max 80");
  Check(GoodDrivingRules.Amount(0,false)==100&&GoodDrivingRules.Amount(3,false)==200&&GoodDrivingRules.Amount(5,true)==400&&GoodDrivingRules.Amount(6,true)==500,"amount 100-500");
  int chance;Check(GoodDrivingRules.Tip("trip-a",false,6,true,5,out chance)==0&&chance==0,"ineligible trip never tips");
  int hits=0;for(int i=0;i<2000;i++){int c;if(GoodDrivingRules.Tip("trip-"+i,true,3,false,0,out c)>0)hits++;}
  Check(hits>900&&hits<1100,"50% chance roughly holds over 2000 trips ("+hits+")");
  int first=GoodDrivingRules.Tip("same-trip",true,4,false,1,out chance),second=GoodDrivingRules.Tip("same-trip",true,4,false,1,out chance);
  Check(first==second,"tip decision is stable for a trip id (reload cannot reroll)");
  Check(GoodDrivingRules.FeedbackLevel(1)==2&&GoodDrivingRules.FeedbackLevel(2)==1&&GoodDrivingRules.FeedbackLevel(3)==0&&GoodDrivingRules.FeedbackLevel(9)==0,"praise fades within a trip");
  // Smooth stop from synthetic physics samples.
  Check(Stop(25,1.5f,.8f,false),"gentle stop from 25 km/h is smooth");
  Check(!Stop(25,3.2f,.8f,false),"3.2 m/s2 stop is not smooth");
  Check(!Stop(25,1.5f,2f,false),"lurch in the last 0.5 s is not smooth");
  Check(!Stop(25,1.5f,.8f,true),"harsh motion in the window is not smooth");
  Check(!Stop(10,1f,.5f,false),"creeping stop below 15 km/h is not judged");
  var t=new GoodDrivingTracker();Feed(t,25,1.5f,.8f,false,Vector3.zero);Check(t.TakeSmoothStop(10,Vector3.zero)&&!t.TakeSmoothStop(10,Vector3.zero),"a stop is awarded once");
  t=new GoodDrivingTracker();Feed(t,25,1.5f,.8f,false,Vector3.zero);Check(!t.TakeSmoothStop(10,new Vector3(3,0,0)),"moved away from the stop: no award");
  t=new GoodDrivingTracker();{float v=25/3.6f;for(float time=-6;time<=0.001f;time+=.02f){v=Mathf.Max(0,v-1.5f*.02f);t.Observe(time,time>-.01f?0:Mathf.Max(.6f,v),time>-.5f?-.8f:-1.5f,false,Vector3.zero);}}Check(t.TakeSmoothStop(0,Vector3.zero),"a stop at game time 0 (right after the preparation pause) still counts");
  // Lane change: 1.2 m/s sideways at 40 km/h, lane id switches after 1.6 s.
  Check(Lane(1,true,true)==1,"signalled right lane change is clean");
  Check(Lane(-1,true,true)==-1,"signalled left lane change is clean");
  Check(Lane(1,false,true)==0,"no indicator: not praised");
  Check(Lane(1,true,false)==0,"same-lane id split without sideways motion: not praised");
  // Account and PvP ledgers refuse a second tip for one trip.
  var account=new CampaignAccount(PrototypeSettings.ForThreeDays());
  Check(account.Tip("t1",300,1)&&!account.Tip("t1",300,1)&&account.Tips(1)==300&&!account.Tip("",300,1)&&!account.Tip("t2",0,1),"account tip entry once per trip");
  var pvp=new PvpSimulationLedger();Check(pvp.Settle("t1",2,5000)&&pvp.Settle("tip:t1",2,300)&&!pvp.Settle("tip:t1",2,300)&&pvp.Score(2)==5300,"PvP ledger tip once");
  // Snapshot round trip (save and LAN replication carry the same fields).
  var root=new GameObject("good driving snapshot");try{
   root.AddComponent<Rigidbody>();   var m=root.AddComponent<DrivingManners>();m.goodSmooth=2;m.goodLane=1;m.goodPrecise=1;m.goodSerial=7;m.goodKind=2;m.goodLevel=2;m.goodLine="line";m.cleanStreak=4;m.streakDay=2;m.tipSerial=3;m.tipAmount=400;m.tipLine="tip";
   var snap=JsonUtility.FromJson<DrivingMannersSnapshot>(JsonUtility.ToJson(m.CaptureLive()));
   var restoreRoot=new GameObject("restore");restoreRoot.AddComponent<Rigidbody>();var n=restoreRoot.AddComponent<DrivingManners>();try{n.RestoreLive(snap);
    Check(n.goodSmooth==2&&n.goodLane==1&&n.goodPrecise==1&&n.goodSerial==7&&n.goodKind==2&&n.goodLevel==2&&n.goodLine=="line"&&n.cleanStreak==4&&n.streakDay==2&&n.tipSerial==3&&n.tipAmount==400&&n.tipLine=="tip","manners snapshot keeps praise, streak and tip");
    n.FinishTrip(true,2);Check(n.cleanStreak==5,"clean trip extends the streak");n.FinishTrip(false,2);Check(n.cleanStreak==0,"broken streak returns to 0 only");
    n.cleanStreak=3;n.EnsureStreakDay(3);Check(n.cleanStreak==0&&n.streakDay==3,"streak lasts one working day");
   }finally{UnityEngine.Object.DestroyImmediate(n.gameObject);}
   var legacy=JsonUtility.FromJson<DrivingMannersSnapshot>("{\"score\":90}");Check(legacy.cleanStreak==0&&legacy.tipSerial==0&&legacy.goodLine==null,"older saves load with no praise state");
  }finally{UnityEngine.Object.DestroyImmediate(root);}
  Check(PvpNativeLan.Protocol=="native-city-lan-v25","LAN contract carries tip state");
  Debug.Log("GOOD_DRIVING_OK checks="+checks);
 }
 static void Feed(GoodDrivingTracker t,float kph,float decel,float finalDecel,bool harsh,Vector3 at){
  float speed=kph/3.6f;
  for(float time=4;time<=10.001f;time+=.02f){bool last=time>9.99f;float d=time>9.5f?finalDecel:decel;speed=Mathf.Max(0,speed-d*.02f);t.Observe(time,last?0:Mathf.Max(.6f,speed),-d,harsh&&time>8,at);}
 }
 static bool Stop(float kph,float decel,float finalDecel,bool harsh){var t=new GoodDrivingTracker();Feed(t,kph,decel,finalDecel,harsh,Vector3.zero);return t.TakeSmoothStop(10,Vector3.zero);}
 static int Lane(int side,bool indicator,bool sideways){
  var t=new GoodDrivingTracker();int result=0;var position=Vector3.zero;float speed=40/3.6f;
  t.ObserveIndicator(0,indicator?side:0);
  for(float time=0;time<=4;time+=.02f){
   position+=Vector3.forward*speed*.02f+(sideways&&time>=.5f&&time<2.5f?Vector3.right*side*1.2f*.02f:Vector3.zero);
   string lane=time<2.1f?"editor:segment:a":"editor:segment:b";
   t.Observe(time,speed,0,false,position);t.ObserveIndicator(time,indicator?side:0);
   if(t.LaneDue(time)){int r=t.ObserveLane(time,lane,position,Vector3.forward,speed);if(r!=0)result=r;}
  }
  return result;
 }
}
