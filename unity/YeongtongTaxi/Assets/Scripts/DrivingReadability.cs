using UnityEngine;

// Presentation only: shares navigation and signal state with the simulation.
public class DrivingReadability : MonoBehaviour {
 public DriveSession session;public bool Overview,Facilities;
 public TrafficJunction SignalTarget {get;private set;}
 public int SignalAxis {get;private set;}public int SignalSign {get;private set;}
 Rect bounds;GUIStyle label;const float Size=280;
 public void SelectSignal(){
  SignalTarget=null;float best=125;var taxi=session.taxi;
  foreach(var j in session.traffic.junctions){
   if(!j.signalized&&!j.scenarioSignal)continue;
   int axis=Mathf.Abs(Vector3.Dot(taxi.transform.forward,session.traffic.AxisDirection(j,0)))>.7f?0:1;
   int sign=session.traffic.Sign(taxi.transform.forward,axis,j);var dir=session.traffic.AxisDirection(j,axis)*sign;
   var delta=j.center-taxi.transform.position;float along=Vector3.Dot(delta,dir),side=Mathf.Abs(Vector3.Dot(delta,TrafficSystem.Right(dir)));
   if(Vector3.Dot(taxi.transform.forward,dir)<.85f||along<2||along>best||side>j.halfWidth[axis]+1)continue;
   best=along;SignalTarget=j;SignalAxis=axis;SignalSign=sign;
  }
 }
 void Update(){if(!session.paused&&Input.GetKeyDown(KeyCode.M))Overview=!Overview;SelectSignal();}
 public Vector2 Project(Vector3 p){return new Vector2((p.x-bounds.x)/bounds.width*Size,(1-(p.z-bounds.y)/bounds.height)*Size);}
 public void SetBounds(){
  var map=session.world.map;var p=session.taxi.transform.position;
  float span=Overview?Mathf.Max(map.width,map.depth):650;
  bounds=new Rect(Overview?(map.width-span)/2:p.x-span/2,Overview?(map.depth-span)/2:p.z-span/2,span,span);
 }
 void OnGUI(){
  if(session.PracticeMenuOpen||session.business.preparing||session.business.ShowingSummary||session.services.open||session.law.HistoryOpen||session.business.restartPrompt||session.business.taxPrompt)return;
  if(label==null){label=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=14,wordWrap=true};label.normal.textColor=Color.white;}
  float x=Screen.width-Size-16;SetBounds();var jobs=session.jobs;var map=session.world.map;
  Panel(new Rect(x-5,10,Size+10,374));
  GUI.Label(new Rect(x+5,14,145,24),Overview?"은계 전체 · 북쪽 ↑":"내 주변 650m · 북쪽 ↑",label);
  if(GUI.Button(new Rect(x+155,14,125,24),Overview?"M 주변 확대":"M 전체 지도"))Overview=!Overview;
  GUI.BeginGroup(new Rect(x,42,Size,Size));
  var tile=GetComponent<DriveHUD>().tile;
  if(tile)GUI.DrawTextureWithTexCoords(new Rect(-bounds.x/bounds.width*Size,(1-(map.depth-bounds.y)/bounds.height)*Size,map.width/bounds.width*Size,map.depth/bounds.height*Size),tile,new Rect(0,0,1,1));
  if(Facilities&&map.hybrid)foreach(var j in map.junctionRules)Dot(Project(j.V()),j.facilityStatus=="verified"?Color.green:new Color(1,.6f,.1f),6);
  foreach(var car in session.traffic.cars)if(car.Visible)Dot(Project(car.transform.position),new Color(.55f,.6f,.65f),3);
  foreach(var incident in session.roadConditions.incidents)Dot(Project(incident.position),Color.red,8);
  for(int pass=0;pass<2;pass++)for(int i=1;i<jobs.nav.path.Count;i++){var a=Project(jobs.nav.path[i-1]);var b=Project(jobs.nav.path[i]);Line(a,b,pass==0?Color.black:Color.cyan,pass==0?8:4);}
  for(int i=0;i<jobs.stops.Length;i++)if(jobs.people[i].activeSelf)Marker(jobs.stops[i].V(),"승",new Color(.3f,1,.55f),false);
  if(session.business.useV2)foreach(int i in session.services.indices)Marker(jobs.stops[i].V(),"거",new Color(1,.5f,1),false);
  if(jobs.trip.State==TripState.ToPickup||jobs.trip.State==TripState.OnTrip){
   if(jobs.trip.State==TripState.ToPickup)Marker(jobs.stops[jobs.current.pickup].V(),"승차",new Color(.3f,1,.55f),true);
   Marker(jobs.stops[jobs.current.drop].V(),"도착",new Color(1,.5f,.8f),true);
  }
  if(jobs.nav.active)Marker(jobs.nav.destination,jobs.trip.State==TripState.ToPickup?"승차":jobs.trip.State==TripState.OnTrip?"도착":"목표",jobs.trip.State==TripState.ToPickup?new Color(.3f,1,.55f):new Color(1,.5f,.8f),true);
  var player=Project(session.taxi.transform.position);var forward=new Vector2(session.taxi.transform.forward.x,-session.taxi.transform.forward.z).normalized;var side=new Vector2(-forward.y,forward.x);
  Dot(player,Color.black,24);Line(player-forward*6-side*7,player+forward*11,Color.yellow,5);Line(player+forward*11,player-forward*6+side*7,Color.yellow,5);Line(player-forward*6+side*7,player-forward*6-side*7,Color.yellow,5);
  GUI.EndGroup();
  GUI.Label(new Rect(x,324,Size,23),"▲ 나  ━ 경로  승: 승객  도착: 목적지",label);
  if(GUI.Button(new Rect(x,349,110,26),Facilities?"시설 표시 끄기":"시설 정보 보기"))Facilities=!Facilities;
  GUI.Label(new Rect(x+114,349,166,40),"© OSM / ODbL\n현장 규제 미검증",new GUIStyle(label){fontSize=11});
  if(Facilities){var status=GetComponent<HybridMapStatus>();Panel(new Rect(x-310,42,300,145));GUI.Label(new Rect(x-300,50,280,130),"시설 □ 주황: 임시 / 초록: 확인\n"+(status?status.current:"시설 정보 없음"),label);}
  DrawSignal(x);
 }
 void Marker(Vector3 world,string text,Color color,bool clamp){
  var p=Project(world);bool outside=p.x<14||p.y<14||p.x>Size-14||p.y>Size-14;
  if(outside&&!clamp)return;
  if(outside){var d=p-Vector2.one*(Size/2);float k=(Size/2-23)/Mathf.Max(Mathf.Abs(d.x),Mathf.Abs(d.y));p=Vector2.one*(Size/2)+d*k;text+=Mathf.Abs(d.x)>Mathf.Abs(d.y)?(d.x>0?" →":" ←"):(d.y>0?" ↓":" ↑");}
  Dot(p,Color.black,clamp?23:15);Dot(p,color,clamp?17:10);
  if(clamp){float xx=Mathf.Clamp(p.x-24,0,Size-65),yy=p.y>Size-48?p.y-23:p.y+10;GUI.color=Color.black;GUI.DrawTexture(new Rect(xx,yy,65,22),Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(new Rect(xx+2,yy,63,22),text,label);}
 }
 void DrawSignal(float x){
  Panel(new Rect(x-5,391,Size+10,110));
  if(SignalTarget==null){GUI.Label(new Rect(x+8,400,Size-16,80),"전방 신호 안내\n접근 방향의 신호를 찾는 중\n현장 신호·주변 차량을 확인하세요",label);return;}
  var j=SignalTarget;var dir=session.traffic.AxisDirection(j,SignalAxis)*SignalSign;var o=session.traffic.Observe(j,SignalAxis,SignalSign,dir,session.taxi.gameObject);
  GUI.Label(new Rect(x+8,395,Size-16,24),"전방 "+Vector3.Distance(session.taxi.transform.position,j.center).ToString("0")+"m · "+(j.facility!=null&&j.facility.facilityStatus=="verified"?"확인 시설":"임시 신호"),label);
  Lamp(new Rect(x+8,424,126,38),"↑ 직진",o.forward);
  Lamp(new Rect(x+142,424,130,38),j.leftTurnMode==LeftTurnMode.Permissive?"← 비보호":"← 좌회전",session.traffic.LeftSignal(j,SignalAxis,SignalSign));
  GUI.Label(new Rect(x+8,462,Size-16,36),o.dedicated?"→ 전용 우회전: "+State(o.right):"→ 우회전: 정지·보행자 조건 확인",label);
 }
 void Lamp(Rect r,string text,SignalColor signal){Dot(new Vector2(r.x+11,r.y+14),signal==SignalColor.Green?Color.green:signal==SignalColor.Red?Color.red:Color.yellow,18);GUI.Label(new Rect(r.x+25,r.y,r.width-25,r.height),text+"\n"+State(signal),new GUIStyle(label){fontSize=13});}
 static string State(SignalColor signal){return signal==SignalColor.Green?"녹색":signal==SignalColor.Red?"적색 · 정지":"황색";}
 static void Panel(Rect r){GUI.color=new Color(.025f,.055f,.075f,.96f);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
 static void Dot(Vector2 p,Color c,float size){GUI.color=c;GUI.DrawTexture(new Rect(p.x-size/2,p.y-size/2,size,size),Texture2D.whiteTexture);GUI.color=Color.white;}
 static void Line(Vector2 a,Vector2 b,Color c,float width){var old=GUI.matrix;GUI.color=c;GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a);GUI.DrawTexture(new Rect(a.x,a.y-width/2,Vector2.Distance(a,b),width),Texture2D.whiteTexture);GUI.matrix=old;GUI.color=Color.white;}
}
