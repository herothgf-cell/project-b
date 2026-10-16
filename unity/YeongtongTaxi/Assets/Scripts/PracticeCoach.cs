using UnityEngine;

// Uses the production law observer. This view adds no fines or success points.
public class PracticeCoach : MonoBehaviour {
 public DriveSession session;
 public bool Active {get;private set;}
 public bool ResultOpen {get;private set;}
 public PracticeFeedback Report {get;private set;}
 public LawEvent AttemptEvent {get;private set;}
 public int Variant {get;private set;}
 public bool Finished {get;private set;}
 public string ProgressText {get;private set;}
 TrafficJunction junction;Vector3 direction,right;int seen;bool stopSeen,priorPaused,automaticShown;float priorTime;
 GameObject markers;Vector2 scroll;
 public void BeginAttempt(RightTurnSituation situation){
  Active=PracticeSession.Enabled;ResultOpen=false;Report=null;AttemptEvent=null;stopSeen=false;Finished=false;automaticShown=false;scroll=Vector2.zero;ProgressText=null;
  if(!Active)return;
  junction=situation.junction;Variant=situation.variant;direction=session.traffic.AxisDirection(junction,0);right=TrafficSystem.Right(direction);seen=session.law.ledger.Events.Length;
  if(markers)Destroy(markers);markers=new GameObject("연습 판단 위치");
  Mark("① 정지선",junction.center-direction*junction.StopDistance(0)+right*(junction.halfWidth[0]*.5f),right,junction.halfWidth[0]-.5f,new Color(1,.75f,.15f));
  Mark("② 보행 확인",junction.center+right*junction.CrossingNearEdge(0),direction,junction.halfWidth[1]*2,new Color(.15f,.85f,1));
 }
 void Mark(string name,Vector3 center,Vector3 axis,float length,Color color){
  var obj=new GameObject(name);obj.transform.SetParent(markers.transform);var line=obj.AddComponent<LineRenderer>();line.material=WorldBuilder.Mat(name,color);line.startColor=line.endColor=color;line.startWidth=line.endWidth=.18f;line.positionCount=2;line.SetPosition(0,center-axis*length*.5f+Vector3.up*.15f);line.SetPosition(1,center+axis*length*.5f+Vector3.up*.15f);
  var label=new GameObject(name+" label");label.transform.SetParent(markers.transform);label.transform.position=center+Vector3.up*2.1f;var text=label.AddComponent<TextMesh>();text.text=name;text.fontSize=40;text.characterSize=.09f;text.anchor=TextAnchor.MiddleCenter;text.color=color;KoreanText.Apply(text);label.AddComponent<FaceCamera>();
 }
 void Update(){
  if(!Active||junction==null||ResultOpen||session.PracticeMenuOpen||session.law.HistoryOpen)return;
  var front=session.taxi.transform.position+session.taxi.transform.forward*2;float along=Vector3.Dot(front-junction.center,direction),side=Vector3.Dot(front-junction.center,right),toLine=-along-junction.StopDistance(0);
  stopSeen=session.law.StopRecorded(junction);
  var events=session.law.ledger.Events;for(int i=seen;i<events.Length;i++)if(AttemptEvent==null&&events[i].junction==junction.name&&(events[i].family=="right"||events[i].family=="signal"))AttemptEvent=events[i];seen=events.Length;
  var observation=junction.rightTurnObservation;
  string stop=stopSeen?"정지 인식: 확인됨":"정지 인식: 아직 없음";
  if(AttemptEvent!=null)ProgressText="③ 판정 기록됨 · 회전을 마무리하세요\n오른쪽 도로로 빠져나가면 결과를 표시합니다.";
  else if(toLine>3)ProgressText="① 정지선까지 "+toLine.ToString("0")+"m · 감속하며 접근\n"+stop;
  else if(observation!=null&&observation.dedicated&&observation.right!=SignalColor.Green)ProgressText="① 우회전 전용 적색 · 정지선 앞 대기\n"+stop;
  else if(observation!=null&&observation.pedestrian!=PedestrianState.None)ProgressText="② 보행자 "+(observation.pedestrian==PedestrianState.Intending?"횡단 의도 있음":"횡단 중")+" · 대기\n"+stop;
  else if(Variant==1&&!stopSeen)ProgressText="① 적색 신호 · 완전 정지 필요\n차량 앞부분이 노란 안내선을 넘기 전에 멈추세요.";
  else ProgressText="③ 보행자·주변 확인 후 서행 우회전\n"+stop+" · 아직 통과 판정 전";
  bool cleared=AttemptEvent!=null&&(AttemptEvent.family=="right"?side>junction.CrossingNearEdge(0)+12&&Vector3.Dot(session.taxi.transform.forward,right)>.6f:along>junction.StopDistance(0)+12||side< -junction.CrossingNearEdge(0)-12);
  if(cleared&&!automaticShown){Finished=true;automaticShown=true;ShowResult();}
 }
 public PracticeFeedback SnapshotFeedback(){var result=PracticeFeedback.Build(AttemptEvent);if(AttemptEvent!=null&&!Finished){result.outcome="회전 미완료 · "+(AttemptEvent.Deduction>0?"위반 기록 있음":"통과 조건 기록됨");result.next="오른쪽 도로까지 회전을 마무리한 뒤 최종 결과를 확인하세요.\n"+result.next;}return result;}
 public bool ShowResult(){if(!Active||ResultOpen||session.GetComponent<PracticeSession>().PickerOpen||session.law.HistoryOpen)return false;Report=SnapshotFeedback();ResultOpen=true;priorPaused=session.paused;priorTime=Time.timeScale;session.paused=true;Time.timeScale=0;return true;}
 public void CloseResult(){if(!ResultOpen)return;ResultOpen=false;session.paused=priorPaused;Time.timeScale=priorTime;}
 void OnGUI(){
  if(!Active||session.GetComponent<PracticeSession>().PickerOpen||session.law.HistoryOpen)return;
  var text=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=16,wordWrap=true};var button=new GUIStyle(GUI.skin.button){font=KoreanText.Font,fontSize=15,wordWrap=true};GUI.depth=-170;
  if(!ResultOpen){
   if(session.paused)return;
   float y=Screen.height-185;Box(new Rect(16,y,430,165));GUI.Label(new Rect(30,y+10,402,30),RightPracticeState.Title(Variant)+" · "+(Mathf.Abs(session.taxi.Speed)*3.6f).ToString("0")+"km/h",text);GUI.Label(new Rect(30,y+48,402,60),ProgressText??"① 정지선으로 접근하세요",text);
   if(GUI.Button(new Rect(30,y+115,402,35),"현재 판정 보기 · 미통과는 미완료",button))ShowResult();return;
  }
  float w=Mathf.Min(780,Screen.width-32),h=Mathf.Min(560,Screen.height-32),x=(Screen.width-w)/2,y0=(Screen.height-h)/2;Box(new Rect(x,y0,w,h));
  GUI.Label(new Rect(x+22,y0+16,w-44,28),RightPracticeState.Title(Variant)+" · 이번 시도 결과",text);
  GUI.Label(new Rect(x+22,y0+52,w-44,55),Report.outcome,new GUIStyle(text){fontSize=21,fontStyle=FontStyle.Bold});
  var details=Report.Details();float contentHeight=Mathf.Max(290,text.CalcHeight(new GUIContent(details),w-75));scroll=GUI.BeginScrollView(new Rect(x+22,y0+115,w-44,h-225),scroll,new Rect(0,0,w-65,contentHeight));GUI.Label(new Rect(0,0,w-75,contentHeight),details,text);GUI.EndScrollView();
  GUI.Label(new Rect(x+22,y0+h-103,w-44,37),"이번 시도의 신호·정지·보행 통과 조건 평가입니다. 시선 확인·전체 운전 능력은 평가하지 않습니다.",new GUIStyle(text){fontSize=13});
  float bw=(w-64)/3;if(GUI.Button(new Rect(x+22,y0+h-55,bw,38),"같은 조건 다시 연습",button))session.GetComponent<PracticeSession>().RetryRight();
  if(GUI.Button(new Rect(x+32+bw,y0+h-55,bw,38),"다른 조건 선택",button))session.GetComponent<PracticeSession>().OpenPicker();
  if(GUI.Button(new Rect(x+42+bw*2,y0+h-55,bw,38),"닫고 계속 / F8 상세",button))CloseResult();
 }
 static void Box(Rect rect){GUI.color=new Color(.035f,.075f,.10f,.98f);GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=Color.white;}
}
