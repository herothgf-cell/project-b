using UnityEngine;
using System.Collections.Generic;

// Presentation only: shares navigation and signal state with the simulation.
public class DrivingReadability : MonoBehaviour {
 public sealed class ZoneLabel {public ServiceLawZone zone;public Vector2 anchor;public Rect rect;public string text;public bool selected;}
 public readonly List<ZoneLabel> ZoneLabels=new List<ZoneLabel>();
 public DriveSession session;public bool Facilities;
 static readonly float[] ZoomSpans={300,450,650,1000,1500,2300};int zoomIndex=2;
 public float ZoomSpan {get{return ZoomSpans[zoomIndex];}}
 public bool Overview {get{return zoomIndex==ZoomSpans.Length-1;}set{zoomIndex=value?ZoomSpans.Length-1:2;}}
 public void Zoom(int direction){zoomIndex=Mathf.Clamp(zoomIndex-direction,0,ZoomSpans.Length-1);}
 public TrafficJunction SignalTarget {get;private set;}
 public int SignalAxis {get;private set;}public int SignalSign {get;private set;}
 Rect bounds;GUIStyle label;const float Size=280;
 readonly Dictionary<Vector2,Rect> markerLabels=new Dictionary<Vector2,Rect>();
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
 void Update(){if(!session.paused){if(Input.GetKeyDown(KeyCode.Equals)||Input.GetKeyDown(KeyCode.Plus)||Input.GetKeyDown(KeyCode.KeypadPlus))Zoom(1);if(Input.GetKeyDown(KeyCode.Minus)||Input.GetKeyDown(KeyCode.KeypadMinus))Zoom(-1);}SelectSignal();}
 public Vector2 Project(Vector3 p){return new Vector2((p.x-bounds.x)/bounds.width*Size,(1-(p.z-bounds.y)/bounds.height)*Size);}
 public void SetBounds(){
  var map=session.world.map;var p=session.taxi.transform.position;
  float span=ZoomSpan;
  bounds=new Rect(Overview?(map.width-span)/2:p.x-span/2,Overview?(map.depth-span)/2:p.z-span/2,span,span);
 }
 void OnGUI(){
  if(session.RideResultOpen||session.PracticeMenuOpen||session.business.preparing||session.business.ShowingSummary||session.services.open||session.law.HistoryOpen||session.business.restartPrompt||session.business.taxPrompt)return;
  if(label==null){label=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=14,wordWrap=true};label.normal.textColor=Color.white;}
  float x=Screen.width-Size-16;SetBounds();PrepareMarkers();var jobs=session.jobs;var map=session.world.map;
  Panel(new Rect(x-5,10,Size+10,397));
  GUI.Label(new Rect(x+5,14,145,24),"주변 "+ZoomSpan.ToString("0")+"m · 북 ↑",label);
  GUI.enabled=zoomIndex>0;if(GUI.Button(new Rect(x+166,14,53,24),"+ 확대",KoreanText.Button(12)))Zoom(1);
  GUI.enabled=zoomIndex<ZoomSpans.Length-1;if(GUI.Button(new Rect(x+224,14,56,24),"− 축소",KoreanText.Button(12)))Zoom(-1);GUI.enabled=true;
  GUI.BeginGroup(new Rect(x,42,Size,Size));
  var tile=GetComponent<DriveHUD>().tile;
  if(tile)GUI.DrawTextureWithTexCoords(new Rect(-bounds.x/bounds.width*Size,(1-(map.depth-bounds.y)/bounds.height)*Size,map.width/bounds.width*Size,map.depth/bounds.height*Size),tile,new Rect(0,0,1,1));
  if(Facilities&&map.hybrid)foreach(var j in map.junctionRules)Dot(Project(j.V()),j.facilityStatus=="verified"?Color.green:new Color(1,.6f,.1f),6);
  foreach(var car in session.traffic.cars)if(car.Visible)Dot(Project(car.transform.position),new Color(.55f,.6f,.65f),3);
  foreach(var incident in session.roadConditions.incidents)Dot(Project(incident.position),Color.red,8);
  if(jobs.nav.HasDestination)for(int pass=0;pass<2;pass++)for(int i=1;i<jobs.nav.path.Count;i++){var a=Project(jobs.nav.path[i-1]);var b=Project(jobs.nav.path[i]);Line(a,b,pass==0?Color.black:Color.cyan,pass==0?8:4);}
  for(int i=0;i<jobs.stops.Length;i++)if(jobs.people[i].activeSelf)Marker(jobs.stops[i].V(),"승",new Color(.3f,1,.55f),false);
  var rest=FuelStationLayout.Rest(session.world.map);if(rest!=null)Marker(rest.pump,"P 휴식",Color.cyan,false);
  if(session.business.useV2)for(int k=1;k<session.services.indices.Length;k++)Marker(session.services.StopFor(k).V(),k==1?"식":"주차",new Color(1,.5f,1),false);
  DrawFuelStations();
  DrawLawZones();
  if(jobs.trip.State==TripState.ToPickup||jobs.trip.State==TripState.OnTrip){
   if(jobs.trip.State==TripState.ToPickup)Marker(jobs.stops[jobs.current.pickup].V(),"승차",new Color(.3f,1,.55f),true);
   Marker(jobs.stops[jobs.current.drop].V(),"도착",new Color(1,.5f,.8f),true);
  }
  if(jobs.nav.HasDestination)Marker(jobs.nav.destination,jobs.trip.State==TripState.ToPickup?"승차":jobs.trip.State==TripState.OnTrip?"도착":"목표",jobs.trip.State==TripState.ToPickup?new Color(.3f,1,.55f):new Color(1,.5f,.8f),true);
  var player=Project(session.taxi.transform.position);var forward=new Vector2(session.taxi.transform.forward.x,-session.taxi.transform.forward.z).normalized;var side=new Vector2(-forward.y,forward.x);
  Circle(player,Color.black,34);Circle(player,new Color(.95f,.95f,.95f),29);Circle(player,new Color(.12f,.16f,.18f),24);Line(player-forward*6-side*7,player+forward*11,Color.yellow,5);Line(player+forward*11,player-forward*6+side*7,Color.yellow,5);Line(player-forward*6+side*7,player-forward*6-side*7,Color.yellow,5);
  GUI.EndGroup();
  GUI.Label(new Rect(x,324,Size,23),"노랑: 나 · 초록: 승차 · 분홍: 도착",new GUIStyle(label){fontSize=12});
  if(GUI.Button(new Rect(x,349,140,26),Facilities?"상세 숨기기":"상세 표시",KoreanText.Button(13)))Facilities=!Facilities;
  if(GUI.Button(new Rect(x+146,349,134,26),"M 전체 지도",KoreanText.Button(13)))GetComponent<FullMapOverlay>().Open();
  GUI.Label(new Rect(x+4,379,276,22),"© OSM / ODbL · 현장 규제 미검증",new GUIStyle(label){fontSize=11});
  if(Facilities){var status=GetComponent<HybridMapStatus>();Panel(new Rect(x-310,42,300,145));GUI.Label(new Rect(x-300,50,280,130),"시설 □ 주황: 임시 / 초록: 확인\n"+(status?status.current:"시설 정보 없음"),label);}
  DrawSignal(x);
  DrawNavigation(x);
 }
 public string NavigationSummary {get{var nav=session.jobs.nav;return nav.HasDestination?nav.PreferenceLabel+" · "+nav.remaining.ToString("0")+"m · 약 "+RoadNavigation.FormatEta(nav.estimatedSeconds):"목적지를 선택하면 경로를 안내합니다";}}
 void DrawNavigation(float x){
  var nav=session.jobs.nav;const float y=553;Panel(new Rect(x-5,y,Size+10,124));
  GUI.Label(new Rect(x+8,y+5,Size-16,37),nav.HasDestination?nav.instruction:"내비게이션 · 목적지 대기",new GUIStyle(label){fontSize=15,fontStyle=FontStyle.Bold});
  GUI.Label(new Rect(x+8,y+43,Size-16,23),NavigationSummary,new GUIStyle(label){fontSize=12});
  GUI.enabled=nav.HasDestination&&!session.paused;
  if(GUI.Button(new Rect(x+8,y+69,Size-16,25),"Tab · "+(nav.PreferFastest?"짧은 길":"빠른 길")+" 선택",KoreanText.Button(13)))nav.TogglePreference();
  GUI.enabled=true;
  GUI.Label(new Rect(x+8,y+97,Size-16,23),!nav.HasDestination?"호출·승객·지도에서 목적지 선택":string.IsNullOrEmpty(nav.rerouteNotice)?"정체·정차 차량을 반영한 예상 시간":nav.rerouteNotice,new GUIStyle(label){fontSize=11});
 }
 void DrawFuelStations(){
  var stations=FuelStationLayout.For(session.world.map);var style=new GUIStyle(label){fontSize=11,alignment=TextAnchor.MiddleCenter};
  for(int i=0;i<stations.Count;i++){var p=Project(stations[i].pump);if(p.x<9||p.y<9||p.x>Size-9||p.y>Size-9)continue;Dot(p,Color.black,19);Dot(p,new Color(1,.75f,.15f),15);style.normal.textColor=Color.black;GUI.Label(new Rect(p.x-7,p.y-9,14,18),(i+1).ToString(),style);}
 }
 static Vector2 ClampedZone(Vector2 p){var d=p-Vector2.one*(Size/2);float k=(Size/2-14)/Mathf.Max(Mathf.Abs(d.x),Mathf.Abs(d.y));return Vector2.one*(Size/2)+d*Mathf.Min(1,k);}
 public void LayoutLawZones(){
  ZoneLabels.Clear();var zones=GetComponent<ServiceLawZones>();if(!zones||!zones.Ready)return;
  SetBounds();PrepareMarkers();var occupied=new List<Rect>();var player=Project(session.taxi.transform.position);occupied.Add(new Rect(player.x-18,player.y-18,36,36));
  if(session.jobs.nav.HasDestination)ReserveMarker(session.jobs.nav.destination,occupied);
  foreach(var station in FuelStationLayout.For(session.world.map)){var p=Project(station.pump);if(p.x>=4&&p.y>=4&&p.x<=Size-4&&p.y<=Size-4)occupied.Add(new Rect(p.x-8,p.y-8,16,16));}
  if(session.jobs.Active){ReserveMarker(session.jobs.stops[session.jobs.current.drop].V(),occupied);if(session.jobs.trip.State==TripState.ToPickup)ReserveMarker(session.jobs.stops[session.jobs.current.pickup].V(),occupied);}
  var selected=session.jobs.Active?zones.ZoneFor(session.jobs.current):null;var ordered=new List<ServiceLawZone>();if(selected!=null)ordered.Add(selected);foreach(var z in zones.zones)if(z!=selected)ordered.Add(z);
  foreach(var zone in ordered){var anchor=Project(zone.junction.center);bool outside=anchor.x<4||anchor.y<4||anchor.x>Size-4||anchor.y>Size-4;bool chosen=zone==selected;if(outside&&!chosen)continue;if(outside)anchor=ClampedZone(anchor);
   ZoneLabels.Add(new ZoneLabel{zone=zone,anchor=anchor,selected=chosen,text=zone.MapTitle+(chosen?"\n"+(outside?"경유 · 지도 밖":"이번 경유"):"")});occupied.Add(new Rect(anchor.x-5,anchor.y-5,10,10));
  }
  foreach(var item in ZoneLabels){var size=Facilities?new Vector2(92,item.selected?48:34):new Vector2(18,18);item.rect=LawZoneMapLayout.Place(item.anchor,size,new Rect(4,4,Size-8,Size-8),occupied);occupied.Add(item.rect);}
 }
 void DrawLawZones(){
  LayoutLawZones();var style=new GUIStyle(label){fontSize=11,alignment=TextAnchor.MiddleCenter,wordWrap=false,padding=new RectOffset(2,2,1,1)};
  if(!Facilities){foreach(var item in ZoneLabels){var p=item.anchor;var c=item.selected?new Color(1,.55f,.85f):new Color(1,.7f,.25f);Circle(p,Color.black,20);Circle(p,new Color(.12f,.17f,.2f),16);
    if(item.zone.Title.Contains("좌회전")){Line(p+new Vector2(4,5),p+new Vector2(4,-4),c,2);Line(p+new Vector2(4,-4),p+new Vector2(-5,-4),c,2);Line(p+new Vector2(-5,-4),p+new Vector2(-1,-8),c,2);Line(p+new Vector2(-5,-4),p+new Vector2(-1,0),c,2);}
    else if(item.zone.Title.Contains("출구")){for(int k=-1;k<=1;k++)Line(p+new Vector2(-5,k*4),p+new Vector2(5,k*4),c,2);}
    else {Circle(p+Vector2.up*-4,c,4);Circle(p,c,4);Circle(p+Vector2.up*4,c,4);}
   }return;}
  foreach(var item in ZoneLabels){var r=item.rect;Line(item.anchor,new Vector2(Mathf.Clamp(item.anchor.x,r.xMin,r.xMax),Mathf.Clamp(item.anchor.y,r.yMin,r.yMax)),item.selected?Color.magenta:new Color(1,.7f,.2f),1.5f);}
  foreach(var item in ZoneLabels){Panel(item.rect);style.normal.textColor=item.selected?new Color(1,.65f,.95f):new Color(1,.84f,.5f);GUI.Label(item.rect,item.text,style);}
  foreach(var item in ZoneLabels){Dot(item.anchor,Color.black,item.selected?12:9);Dot(item.anchor,item.selected?Color.magenta:new Color(1,.7f,.2f),item.selected?8:5);}
 }
 Vector2 MarkerPoint(Vector3 world,bool clamp,out bool outside){
  var p=Project(world);outside=p.x<14||p.y<14||p.x>Size-14||p.y>Size-14;
  if(outside&&clamp){var d=p-Vector2.one*(Size/2);float k=(Size/2-23)/Mathf.Max(Mathf.Abs(d.x),Mathf.Abs(d.y));p=Vector2.one*(Size/2)+d*k;}return p;
 }
 static Rect MarkerTextRect(Vector2 p){return new Rect(Mathf.Clamp(p.x-24,0,Size-65),p.y>Size-48?p.y-23:p.y+10,65,22);}
 void PrepareMarkers(){
  markerLabels.Clear();var jobs=session.jobs;var occupied=new List<Rect>();var player=Project(session.taxi.transform.position);occupied.Add(new Rect(player.x-16,player.y-16,32,32));var targets=new List<Vector3>();
  if(jobs.Active){if(jobs.trip.State==TripState.ToPickup)targets.Add(jobs.stops[jobs.current.pickup].V());targets.Add(jobs.stops[jobs.current.drop].V());}if(jobs.nav.HasDestination)targets.Add(jobs.nav.destination);
  foreach(var target in targets){var key=new Vector2(target.x,target.z);if(markerLabels.ContainsKey(key))continue;bool outside;var p=MarkerPoint(target,true,out outside);occupied.Add(new Rect(p.x-10,p.y-10,20,20));var box=LawZoneMapLayout.Place(p,new Vector2(65,22),new Rect(3,3,Size-6,Size-6),occupied);markerLabels[key]=box;occupied.Add(box);}
 }
 public Rect MarkerTextBounds(Vector3 world){Rect r;if(markerLabels.TryGetValue(new Vector2(world.x,world.z),out r))return r;bool outside;return MarkerTextRect(MarkerPoint(world,true,out outside));}
 void ReserveMarker(Vector3 world,List<Rect> occupied){bool outside;var p=MarkerPoint(world,true,out outside);occupied.Add(new Rect(p.x-12,p.y-12,24,24));occupied.Add(MarkerTextBounds(world));}
 void Marker(Vector3 world,string text,Color color,bool clamp){
  bool outside;var p=MarkerPoint(world,clamp,out outside);if(outside&&!clamp)return;
  if(outside){var d=Project(world)-Vector2.one*(Size/2);text+=Mathf.Abs(d.x)>Mathf.Abs(d.y)?(d.x>0?" →":" ←"):(d.y>0?" ↓":" ↑");}
  if(clamp&&!Facilities){Circle(p,Color.black,26);Circle(p,color,21);
   if(text.StartsWith("승차")){Circle(p+new Vector2(0,-4),Color.black,6);Line(p+new Vector2(0,0),p+new Vector2(0,7),Color.black,3);Line(p+new Vector2(-5,3),p+new Vector2(5,3),Color.black,2);}
   else {Line(p+new Vector2(-4,-7),p+new Vector2(-4,8),Color.black,2);for(int row=0;row<2;row++)for(int col=0;col<3;col++)Dot(p+new Vector2(-1+col*3,-5+row*3),(row+col)%2==0?Color.black:Color.white,3);}
   if(outside){var d=(Project(world)-Vector2.one*(Size/2)).normalized;var side=new Vector2(-d.y,d.x);Line(p+d*17,p+d*11+side*4,color,3);Line(p+d*17,p+d*11-side*4,color,3);}return;
  }
  Dot(p,Color.black,clamp?23:15);Dot(p,color,clamp?17:10);
  if(clamp){var r=MarkerTextBounds(world);GUI.color=Color.black;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(new Rect(r.x+2,r.y,r.width-2,r.height),text,label);}
 }
 void DrawSignal(float x){
  Panel(new Rect(x-5,411,Size+10,138));
  if(SignalTarget==null){GUI.Label(new Rect(x+8,420,Size-16,80),"전방 신호 안내\n접근 방향의 신호를 찾는 중\n현장 신호·주변 차량을 확인하세요",label);return;}
  var j=SignalTarget;var dir=session.traffic.AxisDirection(j,SignalAxis)*SignalSign;var o=session.traffic.Observe(j,SignalAxis,SignalSign,dir,session.taxi.gameObject);
  GUI.Label(new Rect(x+8,415,Size-16,24),"전방 "+Vector3.Distance(session.taxi.transform.position,j.center).ToString("0")+"m · "+(j.facility!=null&&j.facility.facilityStatus=="verified"?"확인 시설":"임시 신호"),label);
  if(j.leftTurnMode==LeftTurnMode.Permissive){GUI.color=new Color(1,.75f,.12f);GUI.DrawTexture(new Rect(x-5,411,Size+10,4),Texture2D.whiteTexture);GUI.color=Color.white;}
  float next=session.traffic.SecondsToSignalChange(j,SignalAxis,SignalSign);
  GUI.Label(new Rect(x+8,519,Size-16,25),next>=0?"다음 신호 변경까지 "+Mathf.CeilToInt(next)+"초":"상황 조건 진행 · 고정 시간 없음",new GUIStyle(label){fontSize=12});
  var training=GetComponent<ConfusionPractice>();var zones=GetComponent<ServiceLawZones>();if(zones&&zones.Owns(j))training=zones.Live;
  if(training&&training.Owns(j)){
   string signal=training.State.Flashing?(training.State.Variant==8?"적색":"황색")+" 점멸":State(o.forward);
   GUI.Label(new Rect(x+8,444,Size-16,38),"가상 교육 · "+signal+"\n"+(training.State.Left?"← 비보호 · 녹색에서 대향차 양보":"↑ 직진 · 주변·출구 확인"),label);
   GUI.Label(new Rect(x+8,485,Size-16,32),"실제 현장 규제가 아닙니다",label);return;
  }
  Lamp(new Rect(x+8,444,126,38),"↑ 직진",o.forward);
  Lamp(new Rect(x+142,444,130,38),j.leftTurnMode==LeftTurnMode.Permissive?"← 비보호":"← 좌회전",session.traffic.LeftSignal(j,SignalAxis,SignalSign));
  GUI.Label(new Rect(x+8,482,Size-16,36),j.leftTurnMode==LeftTurnMode.Permissive?"← 비보호 좌회전 · 녹색에서 대향차 양보":o.dedicated?"→ 전용 우회전: "+State(o.right):"→ 우회전: 정지·보행자 조건 확인",label);
 }
 void Lamp(Rect r,string text,SignalColor signal){Dot(new Vector2(r.x+11,r.y+14),signal==SignalColor.Green?Color.green:signal==SignalColor.Red?Color.red:Color.yellow,18);GUI.Label(new Rect(r.x+25,r.y,r.width-25,r.height),text+"\n"+State(signal),new GUIStyle(label){fontSize=13});}
 static string State(SignalColor signal){return signal==SignalColor.Green?"녹색":signal==SignalColor.Red?"적색 · 정지":"황색";}
 static void Panel(Rect r){GUI.color=new Color(.025f,.055f,.075f,.96f);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
 static Texture2D circleTexture;
 static void Circle(Vector2 p,Color c,float size){if(!circleTexture){circleTexture=new Texture2D(32,32,TextureFormat.RGBA32,false);for(int y=0;y<32;y++)for(int x=0;x<32;x++)circleTexture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(16-Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(16,16)))));circleTexture.Apply();}GUI.color=c;GUI.DrawTexture(new Rect(p.x-size/2,p.y-size/2,size,size),circleTexture);GUI.color=Color.white;}
 static void Dot(Vector2 p,Color c,float size){GUI.color=c;GUI.DrawTexture(new Rect(p.x-size/2,p.y-size/2,size,size),Texture2D.whiteTexture);GUI.color=Color.white;}
 static void Line(Vector2 a,Vector2 b,Color c,float width){var old=GUI.matrix;GUI.color=c;GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a);GUI.DrawTexture(new Rect(a.x,a.y-width/2,Vector2.Distance(a,b),width),Texture2D.whiteTexture);GUI.matrix=old;GUI.color=Color.white;}
}
