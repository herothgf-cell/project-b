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
 public bool StopRecorded(TrafficJunction junction){return entry!=null&&entry.junction==junction&&entry.stopped||stops.ContainsKey(junction.name+":0:1");}
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
   var training=session.GetComponent<ConfusionPractice>();if(training&&training.Owns(j))continue;
   if(!j.signalized&&!j.scenarioSignal)continue;
   var dir=session.traffic.AxisDirection(j,axis)*sign;if(Vector3.Dot(taxi.transform.forward,dir)<.7f)continue;var stop=j.center-dir*j.StopDistance(axis);
   float before=Vector3.Dot(previous-stop,dir),after=Vector3.Dot(front-stop,dir),lane=Vector3.Dot(front-j.center,TrafficSystem.Right(dir));string key=j.name+":"+axis+":"+sign;
   if(after< -8||after>8)stops.Remove(key);
   if(lane<=.15f||lane>=j.halfWidth[axis])continue;
   if(after>=-3&&after<=TrafficJunction.StopLineEntryTolerance&&taxi.body.velocity.magnitude<.05f)stops[key]=true;
   if(before<TrafficJunction.StopLineEntryTolerance&&after>=TrafficJunction.StopLineEntryTolerance){entry=new Entry{junction=j,direction=dir,axis=axis,sign=sign,stopped=stops.ContainsKey(key),observed=session.traffic.Observe(j,axis,sign,dir,taxi.gameObject),id=ids.Next("event")};stops.Remove(key);break;}
  }
  previous=front;
 }
 bool passengerTab;
 public void OpenHistory(bool passengers=false){passengerTab=passengers;if(session.PracticeMenuOpen||session.PracticeBusy||session.business.restartPrompt||session.services.open||session.business.pendingPayment>=0||session.business.preparing||session.business.taxPrompt)return;showHistory=true;session.paused=true;Time.timeScale=0;}
 public void CloseHistory(){showHistory=false;session.paused=false;Time.timeScale=session.business.ShowingSummary?0:1;}
 void Update(){if(!session.PracticeMenuOpen&&!session.PracticeBusy&&!session.business.restartPrompt&&session.business.pendingPayment<0&&!session.business.preparing&&!session.services.open&&!session.business.taxPrompt&&Input.GetKeyDown(KeyCode.F8)){if(showHistory)CloseHistory();else OpenHistory();}}
 void OnGUI(){
  var style=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=14,wordWrap=true};var button=new GUIStyle(GUI.skin.button){font=KoreanText.Font,fontSize=14};
  if(!showHistory){if(session.ShowHints&&!session.business.ShowingSummary&&!session.business.preparing&&!session.services.open)GUI.Label(new Rect(420,70,650,24),"F8 법규·운송 기록 · "+ledger.DaySummary(Day).Split('\n')[0],style);return;}
  GUI.depth=-120;var box=new Rect(40,35,Mathf.Min(900,Screen.width-80),Screen.height-70);GUI.color=new Color(.025f,.045f,.06f,1);GUI.DrawTexture(box,Texture2D.whiteTexture);GUI.color=Color.white;
  GUI.Label(new Rect(60,45,box.width-150,55),"독립 법규 · "+Day+"일차 · "+ledger.DaySummary(Day),style);
  if(GUI.Button(new Rect(box.xMax-100,45,80,28),"닫기",button))CloseHistory();
  if(GUI.Button(new Rect(60,107,180,30),"법규 사건",button)){passengerTab=false;scroll=Vector2.zero;}
  if(GUI.Button(new Rect(250,107,180,30),"승객 운송 결과",button)){passengerTab=true;scroll=Vector2.zero;}
  GUI.Label(new Rect(60,143,box.width-40,55),passengerTab?"매너·지급액은 법규 점수와 별도입니다. 마지막 정산까지 함께 저장됩니다.":"출구: "+ledger.Summary("exit","L06")+" / 우회전 정지: "+ledger.Summary("right","L02"),style);
  var all=ledger.Events;var rides=session.business.passengers;int count=passengerTab?rides.Count:all.Length;float height=passengerTab?155:235;
  scroll=GUI.BeginScrollView(new Rect(60,202,box.width-40,box.height-192),scroll,new Rect(0,0,box.width-65,Mathf.Max(80,count*height)));
  if(count==0)GUI.Label(new Rect(0,0,box.width-80,50),"아직 기록이 없습니다. 관찰되지 않은 상황은 준수로 간주하지 않습니다.",style);
  for(int i=0;i<count;i++){
   float y=i*height;
   if(passengerTab){var r=rides[count-1-i];GUI.Label(new Rect(0,y,box.width-80,height-8),r.day+"일 · "+r.pickup+" → "+r.destination+"\n"+r.outcome+" · 지급 "+r.paid.ToString("N0")+" / 약정 "+r.quoted.ToString("N0")+"원 · 운송 "+r.elapsed.ToString("0")+"초\n매너 "+r.score+" / 요구 "+r.required+" · "+r.request+"\n"+r.mannerReason,style);continue;}
   var e=all[all.Length-1-i];string rules="";foreach(var r in e.rules)rules+=r.rule+" "+Label(r.verdict)+"  ";
   string condition=e.observation==null?"신호 대기열·진입·상대 방해 관찰":("전방 "+SignalLabel(e.observation.forward)+" / 우회전 전용 "+(e.observation.dedicated?SignalLabel(e.observation.right):"없음")+" / 보행 "+PedestrianLabel(e.observation.pedestrian));
   string practice="";foreach(var r in session.business.practices)if(r.sourceId==e.id)practice="연습: "+r.action+" · "+r.EvaluationText();
   GUI.Label(new Rect(0,y,box.width-80,180),e.day+"일 · "+e.junction+" · "+e.action+" · 사건 감점 "+e.Deduction+"\n"+rules+"\n조건: "+condition+"\n"+e.reason+"\n"+(e.occupied?"승객 탑승":"빈차")+" · "+(e.assisted?"조향 보조 사용":"수동 조향")+" · 영업 "+e.seconds.ToString("0")+"초\n"+practice,style);
   bool replay=e.practice!=null||!string.IsNullOrEmpty(e.scenario)&&(e.scenario.StartsWith("R")||e.scenario.StartsWith("C"));var runner=session.GetComponent<PracticeSession>();
   GUI.enabled=replay&&session.business.ShowingSummary&&!runner.Busy;
   if(GUI.Button(new Rect(0,y+184,245,30),session.business.ShowingSummary?"같은 조건 연습 (별도 창)":"복습은 하루 결산에서 가능",button))runner.Begin(e);GUI.enabled=true;
   GUI.Label(new Rect(255,y+184,box.width-335,40),runner.notice,style);
  }
  GUI.EndScrollView();
 }
 static string SignalLabel(SignalColor c){return c==SignalColor.Red?"적색":c==SignalColor.Green?"녹색":"황색";}
 static string PedestrianLabel(PedestrianState p){return p==PedestrianState.None?"없음":p==PedestrianState.Intending?"횡단 의도":p==PedestrianState.Crossing?"횡단 중":"미확인";}
 static string Label(LawVerdict v){return v==LawVerdict.Compliant?"준수":v==LawVerdict.Violation?"위반":v==LawVerdict.Deferred?"보류":"평가 기회 없음";}
}
