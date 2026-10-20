using System;
using UnityEngine;

// Fictional operation on an existing Eungye lane connector. Never edits surveyed map/source records.
public sealed class ConfusionPractice : MonoBehaviour {
 public DriveSession session;
 public bool LiveMode {get;private set;}
 public ConfusionPracticeState State {get;private set;}
 public TrafficJunction Junction {get;private set;}
 public HdRoadGraph.RoutePath Route {get;private set;}
 public int Axis {get;private set;}public int Sign {get;private set;}
 public Vector3 Direction {get;private set;}
 public bool Active {get{return State!=null;}}
 public string Progress {get;private set;}
 public LawEvent AttemptEvent {get;private set;}
 public bool Owns(TrafficJunction j){return Active&&j==Junction;}
 public bool LampOn {get{return !Active||!State.Flashing||Time.unscaledTime%1<.5f;}}
 public float StopAlong {get{return -Junction.StopDistance(Axis);}}
 TrafficCar fixture;GameObject marks;LeftTurnMode previousMode;Vector3 previous;bool initialized,stopSeen,shown;float connectorEnd;
 public bool StartAttempt(int variant){
  if(!PracticeSession.Enabled||!ConfusionPracticeState.Contains(variant))return false;
  // Select a legal graph connection with room before and after it; navigation and H share this route.
  var hd=session.jobs.nav.hd;int incoming=-1,outgoing=-1,connector=-1;TrafficJunction selected=null;float score=float.MaxValue;
  foreach(var j in session.traffic.junctions){
   if(!j.signalized)continue;
   for(int c=0;c<hd.edges.Length;c++){
    var edge=hd.edges[c];if(edge.disabled||edge.ruleId!=j.name||edge.turn!=(variant>=12?-1:0)||edge.change!=0)continue;
    int after=-1;foreach(int n in edge.next)if(!hd.edges[n].disabled&&hd.edges[n].change==0&&hd.paths[n].length>30){after=n;break;}if(after<0)continue;
    for(int b=0;b<hd.edges.Length;b++){
     if(hd.edges[b].disabled||hd.edges[b].change!=0||hd.edges[b].number!=1||hd.paths[b].length<65||Array.IndexOf(hd.edges[b].next,c)<0)continue;
     float d=Vector3.Distance(j.center,new Vector3(575,0,257));if(d>=score)continue;
     var r=hd.Route(b,Mathf.Max(0,hd.paths[b].length-75),after,25);if(r==null||!r.edges.Contains(c)||r.edges.Count!=3)continue;
     score=d;selected=j;incoming=b;outgoing=after;connector=c;
    }
   }
  }
  if(selected==null){GetComponent<PracticeSession>().notice="연습용 차로 연결을 준비하지 못했습니다. 다른 조건을 선택하세요.";return false;}
  LiveMode=false;return Setup(variant,selected,incoming,outgoing);
 }
 public bool CanStartInService(ServiceLawZone zone){
  if(zone.variant!=10&&zone.variant!=13)return true;
  var hd=session.jobs.nav.hd;var route=hd.Route(zone.incoming,Mathf.Max(0,hd.paths[zone.incoming].length-75),zone.outgoing,25);if(route==null)return false;
  Vector3 a,b;
  if(zone.variant==10){a=route.path.Position(route.ends[1]+1.5f);b=route.path.Position(route.path.length);}
  else{var dir=hd.paths[zone.incoming].Tangent(hd.paths[zone.incoming].length-3);int axis=Mathf.Abs(Vector3.Dot(dir,session.traffic.AxisDirection(zone.junction,0)))>.7f?0:1;dir=session.traffic.AxisDirection(zone.junction,axis)*session.traffic.Sign(dir,axis,zone.junction);var right=TrafficSystem.Right(dir);a=zone.junction.center+dir*6-right*1.75f;b=zone.junction.center-dir*65-right*1.75f;}
  var heading=(b-a).normalized;float length=Vector3.Distance(a,b);
  for(float at=0;at<=length+3;at+=2)if(Physics.OverlapBox(Vector3.Lerp(a,b,Mathf.Clamp01(at/length))+Vector3.up*.85f,new Vector3(1.05f,.6f,2.5f),Quaternion.LookRotation(heading),(1<<8)|(1<<9),QueryTriggerInteraction.Ignore).Length>0)return false;
  return true;
 }
 public bool StartInService(ServiceLawZone zone){LiveMode=true;return Setup(zone.variant,zone.junction,zone.incoming,zone.outgoing);}
 bool Setup(int variant,TrafficJunction selected,int incoming,int outgoing){
  var hd=session.jobs.nav.hd;
  if(!LiveMode){GetComponent<PracticeCoach>().CloseResult();GetComponent<RightTurnSituation>().Stop();GetComponent<ExitQueueSituation>().Stop();}Stop();
  Junction=selected;previousMode=Junction.leftTurnMode;Junction.leftTurnMode=variant>=12?LeftTurnMode.Permissive:previousMode;
  if(!LiveMode)session.manners.enabled=false;State=new ConfusionPracticeState(variant);AttemptEvent=null;initialized=stopSeen=shown=false;
  float start=Mathf.Max(0,hd.paths[incoming].length-75);Route=hd.Route(incoming,start,outgoing,25);connectorEnd=Route.ends[1];
  Direction=hd.paths[incoming].Tangent(hd.paths[incoming].length-3);Axis=Mathf.Abs(Vector3.Dot(Direction,session.traffic.AxisDirection(Junction,0)))>.7f?0:1;Sign=session.traffic.Sign(Direction,Axis,Junction);Direction=session.traffic.AxisDirection(Junction,Axis)*Sign;
  if(!LiveMode){session.taxi.SetSpawn(Route.path.Position(0)+Vector3.up*.65f,Quaternion.LookRotation(Route.path.Tangent(0)));session.taxi.ResetTaxi();session.jobs.nav.Clear();session.jobs.nav.SetHdDestination(outgoing,25);session.law.ResetObservation();session.manners.Suppress(1);}
  marks=new GameObject("은계 창작 법규 교육 시설");var right=TrafficSystem.Right(Direction);
  float stopError;float stopAt=Route.path.Project(Junction.center+Direction*StopAlong,out stopError);
  Mark("① 정지선 · 가상 교육",Route.path.Position(stopAt),right,3.3f,Color.yellow);
  Mark(variant>=12?"② 대향차 양보 후 좌회전":"② 출구 공간 확인 후 직진",Route.path.Position(connectorEnd+10),TrafficSystem.Right(Route.path.Tangent(connectorEnd+10)),3.3f,Color.cyan);
  Label("가상 교육 구간 · 실제 규제 아님\n"+ConfusionPracticeState.Title(variant),Junction.center-Direction*45+right*(Junction.halfWidth[Axis]+2)+Vector3.up*4,Color.yellow);
  if(variant>=12)Label("← 비보호\n녹색에서 대향차 양보",Junction.center-Direction*Junction.StopDistance(Axis)+right*(Junction.halfWidth[Axis]+2)+Vector3.up*4,Color.white);
  if(variant==10||variant==13){var route=new TrafficRoute();
   if(variant==10)route.SetPoints(new[]{Route.path.Position(connectorEnd+1.5f),Route.path.Position(Route.path.length)});
   else route.SetPoints(new[]{Junction.center+Direction*6-right*1.75f,Junction.center-Direction*65-right*1.75f});
   if(LiveMode){fixture=new GameObject("운송 중 상황 차량 · 창작").AddComponent<TrafficCar>();fixture.Initialize(session.traffic,route,0,1);fixture.respawn=false;}else{fixture=session.traffic.cars[0];fixture.Continue(route);fixture.Place(0);}fixture.enabled=false;
  }
  ObserveTraffic();ApplySignal();if(!LiveMode)GetComponent<PracticeCoach>().BeginConfusion();return true;
 }
 void Mark(string name,Vector3 p,Vector3 axis,float width,Color c){var o=new GameObject(name);o.transform.SetParent(marks.transform);var line=o.AddComponent<LineRenderer>();line.material=WorldBuilder.Mat(name,c);line.startWidth=line.endWidth=.2f;line.positionCount=2;line.SetPosition(0,p-axis*width/2+Vector3.up*.13f);line.SetPosition(1,p+axis*width/2+Vector3.up*.13f);Label(name,p+Vector3.up*2,c);}
 void Label(string value,Vector3 p,Color c){var o=new GameObject(value);o.transform.SetParent(marks.transform);o.transform.position=p;var t=o.AddComponent<TextMesh>();t.text=value;t.fontSize=40;t.characterSize=.095f;t.anchor=TextAnchor.MiddleCenter;t.color=c;KoreanText.Apply(t);o.AddComponent<FaceCamera>();}
 public void Stop(){if(fixture){fixture.Recycle();if(LiveMode)Destroy(fixture.gameObject);fixture=null;}if(Junction!=null){Junction.scenarioSignal=false;Junction.scenarioFlashing=false;Junction.scenarioOpposingGreen=false;Junction.leftTurnMode=previousMode;}if(marks)Destroy(marks);State=null;Junction=null;Route=null;AttemptEvent=null;}
 void ObserveTraffic(){
  bool blocked=false,oncoming=false;if(fixture&&fixture.Visible){if(State.Variant==10){var p=Route.path.Position(connectorEnd);var heading=Route.path.Tangent(connectorEnd+1);float room=Vector3.Dot(fixture.transform.position-p,heading)-2.1f;blocked=room<6;}else oncoming=Vector3.Dot(fixture.transform.position-Junction.center,Direction)>=-Junction.StopDistance(Axis)-4;}if(LiveMode)foreach(var car in session.traffic.cars){if(!car.Visible)continue;var q=car.transform.position-Junction.center;
   if(State.Variant==10){var p=Route.path.Position(connectorEnd);var heading=Route.path.Tangent(connectorEnd+1);float room=Vector3.Dot(car.transform.position-p,heading)-2.1f;if(room>=-4&&room<6&&Mathf.Abs(Vector3.Dot(car.transform.position-p,TrafficSystem.Right(heading)))<1.9f)blocked=true;}
   if(State.Left&&Vector3.Dot(car.transform.forward,Direction)<-.7f&&car.CurrentTurn>=0&&Vector3.Dot(q,Direction)>=-Junction.StopDistance(Axis)-4&&Vector3.Dot(q,Direction)<Mathf.Max(20,car.speed*3+8)&&Mathf.Abs(Vector3.Dot(q,TrafficSystem.Right(Direction)))<Junction.halfWidth[Axis])oncoming=true;
  }State.ObserveTraffic(blocked,oncoming);
 }
 void ApplySignal(){Junction.scenarioSignal=true;Junction.scenarioOpposingGreen=State.Left&&State.Signal==SignalColor.Green;Junction.scenarioFlashing=State.Flashing;Junction.scenarioLampOn=LampOn;Junction.scenarioForward=State.Signal;Junction.observationAxis=Axis;Junction.observationSign=Sign;}
 void Update(){if(!Active)return;ApplySignal();}
 public PracticeFeedback Feedback(){var f=State.Feedback();if(LiveMode){f.outcome=f.outcome.Replace("연습","운행");f.next=f.next.Replace("이 연습","이 구역");}return f;}
 public void RecordObservation(){
  if(!Active||AttemptEvent!=null)return;AttemptEvent=State.Event(session.law.ids.Next("confusion"),session.law.Day);AttemptEvent.junction=Junction.name;AttemptEvent.speed=session.taxi.body.velocity.magnitude;AttemptEvent.assisted=session.assisted.Active;AttemptEvent.tripId=session.law.tripId;AttemptEvent.occupied=LiveMode||session.jobs.trip.State==TripState.OnTrip;AttemptEvent.seconds=(float)session.business.ledger.Elapsed;if(LiveMode)AttemptEvent.reason+="\n"+Feedback().Details();session.law.ledger.Record(AttemptEvent);
 }
 void FixedUpdate(){
  if(!Active||session.paused||session.PracticeMenuOpen||State.Finished)return;
  var front=session.taxi.transform.position+session.taxi.transform.forward*2;var right=TrafficSystem.Right(Direction);float along=Vector3.Dot(front-Junction.center,Direction),side=Vector3.Dot(front-Junction.center,right),toLine=StopAlong-along;
  bool atLine=toLine>=-TrafficJunction.StopLineEntryTolerance&&toLine<=3&&side>.15f&&side<Junction.halfWidth[Axis]&&Vector3.Dot(session.taxi.transform.forward,Direction)>.7f;
  bool stopped=atLine&&session.taxi.body.velocity.magnitude<.05f;
  if(stopped)stopSeen=true;
  State.Advance(Time.fixedDeltaTime,atLine,stopped);ApplySignal();
  if(fixture&&LiveMode){fixture.enabled=fixture.Visible&&State.Started&&(State.Variant!=10||State.Elapsed>=4);}
  else if(fixture){float d=State.Variant==10?Mathf.Max(0,State.Elapsed-4)*18:State.Elapsed*(6+Junction.StopDistance(Axis)+4)/12;fixture.Place(Mathf.Min(fixture.route.length,d));if(d>=fixture.route.length)fixture.Recycle();}
  ObserveTraffic();
  if(initialized&&Vector3.Distance(front,previous)<10){
   float before=Vector3.Dot(previous-Junction.center,Direction);
   if(!State.Entered&&before<StopAlong+TrafficJunction.StopLineEntryTolerance&&along>=StopAlong+TrafficJunction.StopLineEntryTolerance&&side>.15f&&side<Junction.halfWidth[Axis]&&Vector3.Dot(session.taxi.transform.forward,Direction)>.7f)State.Enter(stopSeen);
   if(State.Entered&&State.Left&&side<-.5f)State.Conflict();
   if(State.Entered){float error;float progress=Route.path.Project(front,out error);bool correct=progress>connectorEnd+12&&error<3.5f&&Vector3.Dot(session.taxi.transform.forward,Route.path.Tangent(progress))>.7f;
    bool leftArea=Mathf.Abs(along)>Junction.StopDistance(Axis)+18||Mathf.Abs(side)>Junction.halfWidth[Axis]+18;
    if(correct||leftArea&&(error>5||Vector3.Dot(session.taxi.transform.forward,Route.path.Tangent(progress))<.3f))State.Complete(correct);
   }
  }
  previous=front;initialized=true;
  Progress="가상 교육 · "+(State.Flashing?(State.Variant==8?"적색 점멸":"황색 점멸"):State.Signal==SignalColor.Red?"적색":"녹색")+" · 정지선 "+Mathf.Max(0,toLine).ToString("0")+"m\n"+(State.Blocked?"출구 막힘 · 정지선 밖 대기":State.Oncoming?"대향차 있음 · 양보":State.Entered?"② 안내 도로까지 진행":"① 신호·주변·출구 확인");
  if(State.Finished&&!shown){shown=true;RecordObservation();if(!LiveMode)GetComponent<PracticeCoach>().ShowResult();}
 }
}
