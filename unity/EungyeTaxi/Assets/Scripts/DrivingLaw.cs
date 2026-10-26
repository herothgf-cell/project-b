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
 float speedingSeconds;bool speedRecorded;void CheckEditorSpeed(){float limit=session.traffic.EditorSpeedLimit(session.taxi.transform.position,session.taxi.transform.forward);if(limit<=0||session.taxi.body.velocity.magnitude*3.6f<=limit+3){speedingSeconds=0;speedRecorded=false;return;}speedingSeconds+=Time.fixedDeltaTime;if(speedingSeconds<2||speedRecorded)return;speedRecorded=true;var e=new LawEvent{recoveryPolicyVersion=1,id=ids.Next("editor-speed"),day=Day,family="사용자 제한속도",action="제한속도 초과",reason="게임 설정 "+limit+"km/h · 3km/h 여유 초과 2초",speed=session.taxi.body.velocity.magnitude,seconds=(float)session.business.ledger.Elapsed};e.Add("editor-speed",LawVerdict.Violation,5);ledger.Record(e);} class Entry {public bool edited,uturnSigned,uturnAllowed;public SignalColor left;public TrafficJunction junction;public Vector3 direction;public int axis,sign;public bool stopped,pedestrianYield;public RoadObservation observed;public string id;}
 Entry entry;
 public int Day {get{return session.business.ledger.Day;}}
 public bool StopRecorded(TrafficJunction junction){return entry!=null&&entry.junction==junction&&entry.stopped||stops.ContainsKey(junction.name+":0:1");}
 public void ResetObservation(){initialized=false;entry=null;stops.Clear();}
 void FixedUpdate(){
  if(!session||session.paused||session.business.ShowingSummary)return;
  CheckEditorSpeed();var taxi=session.taxi;var front=taxi.transform.position+taxi.transform.forward*2;
  if(!initialized||Vector3.Distance(previous,front)>10){previous=front;initialized=true;entry=null;stops.Clear();return;}
  if(entry!=null){
   var q=front-entry.junction.center;float along=Vector3.Dot(q,entry.direction),side=Vector3.Dot(q,TrafficSystem.Right(entry.direction));
   float edge=entry.junction.CrossingNearEdge(entry.axis);
   var current=session.traffic.Observe(entry.junction,entry.axis,entry.sign,entry.direction,taxi.gameObject);if(taxi.body.velocity.magnitude<.05f&&side<edge&&current.known&&current.pedestrian!=PedestrianState.None&&current.pedestrian!=PedestrianState.Unknown)entry.pedestrianYield=true;
   bool right=side>edge,left=side< -edge,through=along>entry.junction.StopDistance(entry.axis)+3;
   bool reversed=Vector3.Dot(taxi.transform.forward,entry.direction)<-.7f;bool uturn=reversed&&(left||along< -entry.junction.StopDistance(entry.axis,entry.sign)-3);bool holdLeft=entry.uturnSigned&&left&&!uturn&&side> -edge-5;
   if(right||uturn||left&&!holdLeft||through){
    LawEvent e;
    if(right){var now=session.traffic.Observe(entry.junction,entry.axis,entry.sign,entry.direction,taxi.gameObject);now.forward=entry.observed.forward;now.dedicated=entry.observed.dedicated;now.right=entry.observed.right;e=LawEvaluator.RightTurn(entry.id,Day,now,entry.stopped,settings,entry.pedestrianYield||(entry.stopped&&entry.observed.pedestrian!=PedestrianState.None&&entry.observed.pedestrian!=PedestrianState.Unknown));}
    else if(uturn){e=new LawEvent{recoveryPolicyVersion=1,id=entry.id,day=Day,family="signal",observation=entry.observed.Copy(),action="유턴",reason="진입 시점 유턴 보조표지·신호"};e.Add("L01",entry.uturnAllowed?LawVerdict.Compliant:LawVerdict.Violation,settings.redStraightPenalty);}
    else {e=new LawEvent{recoveryPolicyVersion=1,id=entry.id,day=Day,family="signal",observation=entry.observed.Copy(),action=left?"좌회전":"직진",reason="진입 시점 차량 신호"};e.Add("L01",left?(entry.edited?(entry.left==SignalColor.Red?LawVerdict.Violation:entry.left==SignalColor.Green?LawVerdict.Compliant:LawVerdict.Deferred):LawVerdict.Deferred):entry.observed.forward==SignalColor.Red?LawVerdict.Violation:entry.observed.forward==SignalColor.Green?LawVerdict.Compliant:LawVerdict.Deferred,settings.redStraightPenalty);}
    var situation=session.GetComponent<RightTurnSituation>();if(right&&situation&&situation.Active&&situation.junction==entry.junction)e.scenario="R"+situation.variant;
    e.junction=entry.junction.name;e.tripId=tripId;e.occupied=session.jobs.trip.State==TripState.OnTrip;e.assisted=session.assisted.Active;e.speed=taxi.body.velocity.magnitude;e.seconds=(float)session.business.ledger.Elapsed;
    ledger.Record(e);entry=null;
   }
  }
  if(entry==null)foreach(var j in session.traffic.junctions)for(int axis=0;axis<2;axis++)foreach(int sign in new[]{-1,1}){
   var training=session.GetComponent<ConfusionPractice>();var zones=session.GetComponent<ServiceLawZones>();if(training&&training.Owns(j)||zones&&zones.Owns(j))continue;
   if(!j.signalized&&!j.scenarioSignal)continue;
   var dir=session.traffic.AxisDirection(j,axis)*sign;if(Vector3.Dot(taxi.transform.forward,dir)<.7f)continue;var stop=session.traffic.StopPosition(j,axis,sign);var planeNormal=session.traffic.StopNormal(j,axis,sign);
   float before=Vector3.Dot(previous-stop,planeNormal),after=Vector3.Dot(front-stop,planeNormal),lane=Vector3.Dot(front-j.center,TrafficSystem.Right(dir));string key=j.name+":"+axis+":"+sign;
   if(after< -8||after>8)stops.Remove(key);
   if((lane<=.15f||lane>=j.halfWidth[axis])&&!session.traffic.DirectedApproach(j,axis,sign,taxi.transform.position,taxi.transform.forward))continue;
   if(after>=-3&&after<=TrafficJunction.StopLineEntryTolerance&&taxi.body.velocity.magnitude<.05f)stops[key]=true;
   if(before<TrafficJunction.StopLineEntryTolerance&&after>=TrafficJunction.StopLineEntryTolerance){string laneId=session.traffic.EditorLaneId(taxi.transform.position,taxi.transform.forward);var observed=session.traffic.Observe(j,axis,sign,dir,taxi.gameObject);bool uturnSigned=session.traffic.UturnCondition(laneId)!=null;entry=new Entry{edited=j.editorProgram!=null,left=j.editorProgram!=null?j.editorProgram.Movement(axis,sign,-1,session.traffic.Clock,laneId):SignalColor.Red,junction=j,direction=dir,axis=axis,sign=sign,stopped=stops.ContainsKey(key),observed=observed,uturnSigned=uturnSigned,uturnAllowed=uturnSigned&&observed.PedestrianClear&&session.traffic.UturnPermitted(j,axis,sign,laneId),id=ids.Next("event")};stops.Remove(key);break;}
  }
  previous=front;
 }
 bool passengerTab;
 public void OpenHistory(bool passengers=false){if(session.FullMapOpen||session.ChoiceOpen)return;passengerTab=passengers;if(session.PracticeMenuOpen||session.PracticeBusy||session.business.restartPrompt||session.services.open||session.business.pendingPayment>=0||session.business.preparing||session.business.taxPrompt)return;showHistory=true;session.paused=true;Time.timeScale=0;}
 public void CloseHistory(){showHistory=false;session.paused=false;Time.timeScale=session.business.ShowingSummary?0:1;}
 void Update(){if(!session.PracticeMenuOpen&&!session.PracticeBusy&&!session.business.restartPrompt&&session.business.pendingPayment<0&&!session.business.preparing&&!session.services.open&&!session.business.taxPrompt&&Input.GetKeyDown(KeyCode.F8)){if(showHistory)CloseHistory();else OpenHistory();}}
 static string PassengerHistoryText(PassengerResult r){bool modern=!string.IsNullOrEmpty(r.bonusReason);string settlement=modern?RideReceiptSummary.History(r)+"\n"+PassengerScoreView.Recorded(r).Baseline+"\n"+PassengerScoreView.Recorded(r).Breakdown+" · 초과 "+r.excessScore+"점\n"+r.bonusReason:"지급 "+r.paid.ToString("N0")+"원 · 만족도 "+r.score+" / 요구 "+r.required;return r.day+"일 · "+r.pickup+" → "+r.destination+"\n"+r.outcome+" · 약정 "+r.quoted.ToString("N0")+"원 · 운송 "+r.elapsed.ToString("0")+"초\n"+settlement+"\n"+r.request+"\n도착 "+r.arrivalElapsed.ToString("0")+"초 / 하차 "+r.disembarkElapsed.ToString("0")+"초 · "+(r.drunk?"취객 ":"")+(r.vomited?"구토 ":"")+(r.wakeDelayed?"기상 지연 ":"")+"청결 "+r.cleanlinessAfter+" · "+r.timeReason+"\n"+(r.hasDrivingScore?"하차 시 운전 점수 "+r.drivingScore+"점\n":"")+r.mannerReason;}
 void OnGUI(){
  var style=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=14,wordWrap=true};var button=new GUIStyle(GUI.skin.button){font=KoreanText.Font,fontSize=14};
  if(!showHistory){if(session.ShowHints&&!session.business.ShowingSummary&&!session.business.preparing&&!session.services.open)GUI.Label(new Rect(420,70,650,24),"F8 운전·운송 기록 · "+ledger.DaySummary(Day).Split('\n')[0],style);return;}
  GUI.depth=-120;var box=new Rect(40,35,Mathf.Min(900,Screen.width-80),Screen.height-70);GUI.color=new Color(.025f,.045f,.06f,1);GUI.DrawTexture(box,Texture2D.whiteTexture);GUI.color=Color.white;
  GUI.Label(new Rect(60,45,box.width-150,55),"운전 점수 "+ledger.TotalScore+"점 · "+Day+"일차 · "+ledger.DaySummary(Day),style);
  if(GUI.Button(new Rect(box.xMax-100,45,80,28),"닫기",button))CloseHistory();
  if(GUI.Button(new Rect(60,107,180,30),"법규 사건",button)){passengerTab=false;scroll=Vector2.zero;}
  if(GUI.Button(new Rect(250,107,180,30),"승객 운송 결과",button)){passengerTab=true;scroll=Vector2.zero;}
  GUI.Label(new Rect(60,143,box.width-40,55),passengerTab?"운전 점수는 승차 운임 보정, 승객 만족도는 감액·보너스에 반영됩니다.":"출구: "+ledger.Summary("exit","L06")+" / 우회전 정지: "+ledger.Summary("right","L02"),style);
  var all=ledger.Events;var rides=session.business.passengers;int count=passengerTab?rides.Count:all.Length;float height=passengerTab?215:235;
  var rowText=new string[count];var rowHeights=new float[count];var rowY=new float[count];float contentHeight=0;if(passengerTab)for(int i=0;i<count;i++){rowText[i]=PassengerHistoryText(rides[count-1-i]);rowY[i]=contentHeight;rowHeights[i]=Mathf.Max(100,style.CalcHeight(new GUIContent(rowText[i]),box.width-80)+14);contentHeight+=rowHeights[i];}
  scroll=GUI.BeginScrollView(new Rect(60,202,box.width-40,box.height-192),scroll,new Rect(0,0,box.width-65,Mathf.Max(80,passengerTab?contentHeight:count*height)));
  if(count==0)GUI.Label(new Rect(0,0,box.width-80,50),"아직 기록이 없습니다. 관찰되지 않은 상황은 준수로 간주하지 않습니다.",style);
  for(int i=0;i<count;i++){
   float y=passengerTab?rowY[i]:i*height;
   if(passengerTab){GUI.Label(new Rect(0,y,box.width-80,rowHeights[i]-8),rowText[i],style);continue;}
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
