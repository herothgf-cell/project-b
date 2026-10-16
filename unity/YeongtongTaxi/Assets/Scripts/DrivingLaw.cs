using System.Collections.Generic;
using UnityEngine;

// Independent observer: never calls DrivingManners or changes a fare.
public class DrivingLaw : MonoBehaviour {
 public DriveSession session;
 public LawLedger ledger=new LawLedger();
 public PrototypeIds ids=new PrototypeIds(System.Guid.NewGuid().ToString("N"));
 public PrototypeSettings settings=new PrototypeSettings();
 public string tripId="";
 public bool HistoryOpen {get{return showHistory;}}
 Vector3 previous; bool initialized,showHistory; Vector2 scroll;
 readonly Dictionary<string,bool> stops=new Dictionary<string,bool>();
 class Entry {public TrafficJunction junction;public Vector3 direction;public int axis,sign;public bool stopped,pedestrianYield;public RoadObservation observed;public string id;}
 Entry entry;
 public int Day {get{return session.business.ledger.Day;}}
 public void ResetObservation(){initialized=false;entry=null;stops.Clear();}
 void FixedUpdate(){
  if(!session||session.paused||session.business.ShowingSummary)return;
  var taxi=session.taxi;var front=taxi.transform.position+taxi.transform.forward*2;
  if(!initialized||Vector3.Distance(previous,front)>10){previous=front;initialized=true;entry=null;stops.Clear();return;}
  if(entry!=null){
   var q=front-entry.junction.center;float along=Vector3.Dot(q,entry.direction),side=Vector3.Dot(q,TrafficSystem.Right(entry.direction));
   float edge=entry.junction.CrossingNearEdge(entry.axis);
   var current=session.traffic.Observe(entry.junction,entry.axis,entry.sign,entry.direction,taxi.gameObject);if(taxi.body.velocity.magnitude<.05f&&side<edge&&current.known&&current.pedestrian!=PedestrianState.None&&current.pedestrian!=PedestrianState.Unknown)entry.pedestrianYield=true;
   bool right=side>edge,left=side< -edge,through=along>entry.junction.StopDistance(entry.axis)+3;
   if(right||left||through){
    LawEvent e;
    if(right){var now=session.traffic.Observe(entry.junction,entry.axis,entry.sign,entry.direction,taxi.gameObject);now.forward=entry.observed.forward;now.dedicated=entry.observed.dedicated;now.right=entry.observed.right;e=LawEvaluator.RightTurn(entry.id,Day,now,entry.stopped,settings,entry.pedestrianYield||(entry.stopped&&entry.observed.pedestrian!=PedestrianState.None&&entry.observed.pedestrian!=PedestrianState.Unknown));}
    else {e=new LawEvent{id=entry.id,day=Day,family="signal",observation=entry.observed.Copy(),action=left?"좌회전":"직진",reason="진입 시점 차량 신호"};e.Add("L01",left?LawVerdict.Deferred:entry.observed.forward==SignalColor.Red?LawVerdict.Violation:entry.observed.forward==SignalColor.Green?LawVerdict.Compliant:LawVerdict.Deferred,settings.redStraightPenalty);}
    var situation=session.GetComponent<RightTurnSituation>();if(right&&situation&&situation.Active&&situation.junction==entry.junction)e.scenario="R"+situation.variant;
    e.junction=entry.junction.name;e.tripId=tripId;e.occupied=session.jobs.trip.State==TripState.OnTrip;e.assisted=session.assisted.Active;e.speed=taxi.body.velocity.magnitude;e.seconds=(float)session.business.ledger.Elapsed;
    ledger.Record(e);entry=null;
   }
  }
  if(entry==null)foreach(var j in session.traffic.junctions)for(int axis=0;axis<2;axis++)foreach(int sign in new[]{-1,1}){
   if(!j.signalized&&!j.scenarioSignal)continue;
   var dir=session.traffic.AxisDirection(j,axis)*sign;var stop=j.center-dir*j.StopDistance(axis);
   float before=Vector3.Dot(previous-stop,dir),after=Vector3.Dot(front-stop,dir),lane=Vector3.Dot(front-j.center,TrafficSystem.Right(dir));string key=j.name+":"+axis+":"+sign;
   if(after< -8||after>8)stops.Remove(key);
   if(lane<=.15f||lane>=j.halfWidth[axis])continue;
   if(after>=-3&&after<=TrafficJunction.StopLineEntryTolerance&&taxi.body.velocity.magnitude<.05f)stops[key]=true;
   if(before<TrafficJunction.StopLineEntryTolerance&&after>=TrafficJunction.StopLineEntryTolerance){entry=new Entry{junction=j,direction=dir,axis=axis,sign=sign,stopped=stops.ContainsKey(key),observed=session.traffic.Observe(j,axis,sign,dir,taxi.gameObject),id=ids.Next("event")};stops.Remove(key);break;}
  }
  previous=front;
 }
 void Update(){if(!session.business.preparing&&!session.services.open&&!session.business.taxPrompt&&Input.GetKeyDown(KeyCode.F8)){showHistory=!showHistory;if(!session.business.ShowingSummary){session.paused=showHistory;Time.timeScale=showHistory?0:1;}}}
 void OnGUI(){
  var style=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=15,wordWrap=true};var button=new GUIStyle(GUI.skin.button){font=KoreanText.Font,fontSize=15};
  if(!showHistory){GUI.Label(new Rect(420,70,700,24),"F8 법규 기록 · 오늘 "+ledger.Score(Day)+"점 (승객 매너와 별도)",style);return;}
  GUI.depth=-100;var box=new Rect(40,40,Mathf.Min(820,Screen.width-80),Screen.height-80);GUI.Box(box,"");
  GUI.Label(new Rect(60,55,box.width-40,35),"독립 법규 기록 · "+Day+"일차 "+ledger.Score(Day)+"점 · F8 닫기",style);
  if(GUI.Button(new Rect(box.xMax-100,52,80,28),"닫기",button)){showHistory=false;if(!session.business.ShowingSummary){session.paused=false;Time.timeScale=1;}}
  GUI.Label(new Rect(60,90,box.width-40,50),"출구 진입: "+ledger.Summary("exit","L06")+"\n우회전 정지: "+ledger.Summary("right","L02"),style);
  var all=ledger.Events;scroll=GUI.BeginScrollView(new Rect(60,150,box.width-40,box.height-135),scroll,new Rect(0,0,box.width-65,Mathf.Max(80,all.Length*110)));
  if(all.Length==0)GUI.Label(new Rect(0,0,650,50),"아직 관찰 기록이 없습니다. 빈차·승객 탑승 모두 기록합니다.",style);
  for(int i=0;i<all.Length;i++){var e=all[all.Length-1-i];string rules="";foreach(var r in e.rules)rules+=r.rule+" "+Label(r.verdict)+"  ";GUI.Label(new Rect(0,i*110,box.width-70,105),e.day+"일 · "+e.junction+" · "+e.action+" · 감점 "+e.Deduction+"\n"+rules+"\n"+e.reason,style);if((e.practice!=null||!string.IsNullOrEmpty(e.scenario)&&e.scenario.StartsWith("R"))&&GUI.Button(new Rect(box.width-255,i*110+75,170,27),"같은 조건 연습 창",button)){PracticeSession.Launch(e);}}
  GUI.EndScrollView();
 }
 static string Label(LawVerdict v){return v==LawVerdict.Compliant?"준수":v==LawVerdict.Violation?"위반":v==LawVerdict.Deferred?"보류":"평가 기회 없음";}
}
