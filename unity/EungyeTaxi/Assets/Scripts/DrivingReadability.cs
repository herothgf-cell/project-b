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
 MapRouteRaster routeRaster;Texture2D routeTexture;Rect routeBounds;float nextRouteRaster;int routeCount=-1,routeZoom=-1;bool routeTraffic;
 FullMapOverlay.PaintedSegment routeFirst,routeLast;
 void DrawRouteRaster(FullMapOverlay overlay){
  if(Event.current.type!=EventType.Repaint)return;var segments=overlay.RoutePaint;int count=segments.Count;var first=count>0?segments[0]:null;var last=count>0?segments[count-1]:null;
  bool changed=routeCount!=count||routeFirst!=first||routeLast!=last||routeBounds!=bounds||routeZoom!=zoomIndex||routeTraffic!=overlay.ShowTraffic;
  if(routeTexture==null||changed&&Time.unscaledTime>=nextRouteRaster){
   if(routeRaster==null)routeRaster=new MapRouteRaster((int)Size,(int)Size);
   if(routeTexture==null){routeTexture=new Texture2D((int)Size,(int)Size,TextureFormat.RGBA32,false);routeTexture.name="Cached minimap routes";routeTexture.filterMode=FilterMode.Point;routeTexture.wrapMode=TextureWrapMode.Clamp;}
   routeRaster.Clear();for(int pass=0;pass<2;pass++)foreach(var segment in segments){var color=segment.retained?(pass==0?Color.white:MapPresentation.PreviousRoute):pass==0?Color.white:overlay.ShowTraffic?segment.color:MapPresentation.RouteColor(segment.passenger);routeRaster.Line(Project(segment.a),Project(segment.b),color,pass==0?(segment.passenger?7:11):4);}
   routeTexture.SetPixels32(routeRaster.Pixels);routeTexture.Apply(false,false);routeCount=count;routeFirst=first;routeLast=last;routeBounds=bounds;routeZoom=zoomIndex;routeTraffic=overlay.ShowTraffic;nextRouteRaster=Time.unscaledTime+.1f;
  }
  var oldColor=GUI.color;try{GUI.color=Color.white;GUI.DrawTexture(new Rect(0,0,Size,Size),routeTexture);}finally{GUI.color=oldColor;}
 }
 void OnDestroy(){if(routeTexture)Destroy(routeTexture);}
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
 void OnGUI(){using(TrafficPerformanceProbe.Measure("DrivingReadability.OnGUI"))DrawInterface();}
 void DrawInterface(){
  if(session.FullMapOpen||session.RideResultOpen||session.PracticeMenuOpen||session.business.preparing||session.business.ShowingSummary||session.services.open||session.law.HistoryOpen||session.business.restartPrompt||session.business.taxPrompt)return;
  if(label==null){label=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=UiReadability.Font(14),wordWrap=true};label.normal.textColor=CasualUI.Ink;}
  label.fontSize=UiReadability.Font(14);var originalMatrix=GUI.matrix;float uiScale=CockpitUI.Scale;GUI.matrix=Matrix4x4.Scale(new Vector3(uiScale,uiScale,1));float x=Screen.width/uiScale-Size-16;SetBounds();PrepareMarkers();var jobs=session.jobs;var map=session.world.map;
  Panel(new Rect(x-5,10,Size+10,358));
  CasualUI.RoundedFill(new Rect(x-5,10,Size+10,28),CasualUI.Blue);
  GUI.Label(new Rect(x+5,14,145,24),ZoomSpan.ToString("0")+"m · 북 ↑",new GUIStyle(label){normal={textColor=Color.white}});
  GUI.enabled=zoomIndex>0;if(GUI.Button(new Rect(x+146,12,64,28),new GUIContent("+","미니맵 확대"),KoreanText.Button(UiReadability.Font(12))))Zoom(1);
  GUI.enabled=zoomIndex<ZoomSpans.Length-1;if(GUI.Button(new Rect(x+216,12,64,28),new GUIContent("−","미니맵 축소"),KoreanText.Button(UiReadability.Font(12))))Zoom(-1);GUI.enabled=true;
  GUI.BeginGroup(new Rect(x,42,Size,Size));
  var tile=GetComponent<DriveHUD>().LightTile;
  if(tile)GUI.DrawTextureWithTexCoords(new Rect(-bounds.x/bounds.width*Size,(1-(map.depth-bounds.y)/bounds.height)*Size,map.width/bounds.width*Size,map.depth/bounds.height*Size),tile,new Rect(0,0,1,1));
  if(Facilities&&map.hybrid)foreach(var j in map.junctionRules)Dot(Project(j.V()),j.facilityStatus=="verified"?Color.green:new Color(1,.6f,.1f),6);
  var overlay=GetComponent<FullMapOverlay>();
  if(overlay.ShowTraffic){foreach(var car in session.traffic.cars)if(car.Visible)Dot(Project(car.transform.position),new Color(.55f,.6f,.65f),3);foreach(var incident in session.roadConditions.incidents)Dot(Project(incident.position),Color.red,8);}
  using(TrafficPerformanceProbe.Measure("DrivingReadability.RouteRaster"))DrawRouteRaster(overlay);
  if(overlay.ShowFacilities)foreach(var place in overlay.Places()){var p=Project(place.position);if(p.x<14||p.y<14||p.x>Size-14||p.y>Size-14)continue;if(HudGlyph.Button(new Rect(p.x-13,p.y-13,26,26),place.glyph,place.name,true)){if(overlay.Open())overlay.SelectPlace(place);}}
  if(!jobs.Active)for(int i=0;i<jobs.offers.Length;i++){var offer=jobs.offers[i];if(offer==null)continue;var p=ClampedZone(Project(jobs.stops[offer.pickup].V()))+new Vector2((i-1)*20,0);p=new Vector2(Mathf.Clamp(p.x,12,Size-12),Mathf.Clamp(p.y,30,Size-12));MapPresentation.Pin(p,MapPresentation.Pickup,20,(i+1).ToString());if(GUI.Button(MapPresentation.PinHitBounds(p,20),new GUIContent("","호출 "+(i+1)),GUIStyle.none)){if(overlay.Open())overlay.SelectOffer(i);}}
  if(!jobs.Active&&overlay.PreviewOffer!=null){Marker(jobs.stops[overlay.PreviewOffer.pickup].V(),"승차",MapPresentation.Pickup,true);Marker(jobs.stops[overlay.PreviewOffer.drop].V(),"도착",MapPresentation.Passenger,true);}
  if(overlay.ShowLaw)DrawLawZones();
  if(jobs.trip.State==TripState.ToPickup||jobs.trip.State==TripState.OnTrip){
   if(jobs.trip.State==TripState.ToPickup)Marker(jobs.EffectiveStop(jobs.current.pickup).V(),"승차",MapPresentation.Pickup,true);
   Marker(jobs.EffectiveStop(jobs.current.drop).V(),"도착",MapPresentation.Passenger,true);
  }
  if(session.simulationMatch)foreach(var bot in session.simulationMatch.Bots)Marker(bot.taxi.transform.position,PvpDriverPresentation.Name(bot.SimulationDriver),PvpDriverPresentation.Color(bot.SimulationDriver),true);if(jobs.nav.HasDestination)Marker(jobs.nav.destination,jobs.trip.State==TripState.ToPickup?"승차":jobs.trip.State==TripState.OnTrip?"도착":"목표",jobs.trip.State==TripState.ToPickup?MapPresentation.Pickup:MapPresentation.Passenger,true);
  var player=Project(session.taxi.transform.position);var forward=new Vector2(session.taxi.transform.forward.x,-session.taxi.transform.forward.z).normalized;var side=new Vector2(-forward.y,forward.x);
  MapPresentation.Player(player,forward);
  if(Event.current.type==EventType.MouseUp&&Event.current.button==0&&new Rect(0,0,Size,Size).Contains(Event.current.mousePosition)){var p=Event.current.mousePosition;var world=new Vector3(bounds.x+p.x/Size*bounds.width,0,bounds.y+(1-p.y/Size)*bounds.height);if(overlay.Open())overlay.SelectRoadPoint(world);Event.current.Use();}
  GUI.EndGroup();
  if(GUI.Button(new Rect(x,328,Size,32),"지도 (M)",KoreanText.Button(UiReadability.Font(13))))overlay.Open();
  if(Facilities){var status=GetComponent<HybridMapStatus>();Panel(new Rect(x-310,42,300,145));GUI.Label(new Rect(x-300,50,280,130),"시설 □ 주황: 임시 / 초록: 확인\n"+(status?status.current:"시설 정보 없음"),label);}
  DrawNavigation((Screen.width/uiScale-620)/2+5);GUI.matrix=originalMatrix;
 }
 public string GoalDistance {get{var j=session.jobs;if(j.nav.RetainingPreviousRoute)return "경로 재탐색 중";return (j.trip.State==TripState.ToPickup?"승차까지":j.trip.State==TripState.OnTrip?(j.PassengerViaPending?"경유까지":"하차까지"):"도착까지")+" "+NavigationCue.FormatDistance(j.nav.remaining);}}
 public string NavigationSummary {get{var nav=session.jobs.nav;if(nav.RetainingPreviousRoute)return "도로로 복귀하면 새 길을 안내합니다";return nav.HasDestination?GoalDistance+" · 약 "+RoadNavigation.FormatEta(nav.estimatedSeconds):"목적지를 선택하세요";}}
 public NavigationCue TurnCue {get{
  var nav=session.jobs.nav;if(!nav.HasDestination||nav.hd==null||nav.hdRoute==null||nav.path.Count<2)return new NavigationCue();
  float error;float at=nav.ProjectVehicle(out error);return error>4.5f?new NavigationCue():NavigationCue.FromRoute(nav.hd,nav.hdRoute,at);
 }}
 public static Rect FollowingManeuverBounds(float x,float y){return new Rect(x+222,y+10,80,64);}
 void DrawNavigation(float x){
  if(session.drivingCamera&&session.drivingCamera.FirstPerson){DrawCockpitNavigation(x);return;}
  var nav=session.jobs.nav;if(!nav.HasDestination){if(SignalTarget!=null){DrivingHudArt.Glass(new Rect(x+310,20,300,90));DrawCompactSignal(x+310,42,false);}return;}const float y=20;var cue=TurnCue;DrivingHudArt.Glass(new Rect(x-5,y,620,164));
  var white=new GUIStyle(label){normal={textColor=CasualUI.Ink}};
  GUI.Label(new Rect(x+16,y+8,580,26),nav.RetainingPreviousRoute?"경로 재탐색 중":nav.HasDestination?"안내 경로를 따라 이동하세요":"어디로 갈까요?",DrivingHudArt.Text(16,DrivingHudArt.Light,true));
  if(cue.Valid&&nav.remaining<18){cue.Move=NavigationMove.Arrival;cue.Distance=nav.remaining;cue.Road="목적지 주변";cue.LaneHint="도착";cue.HasNext=false;}
  if(cue.Valid){
   NavigationArrow.Draw(new Rect(x+12,y+48,64,58),cue.Move,DrivingHudArt.Cyan);
   GUI.Label(new Rect(x+88,y+35,135,46),NavigationCue.FormatDistance(cue.Distance),DrivingHudArt.Text(32,null,true));
   string road=string.IsNullOrEmpty(cue.Road)?NavigationCue.Label(cue.Move):cue.Road;var roadStyle=DrivingHudArt.Text(15);
   GUI.Label(new Rect(x+88,y+83,135,27),new GUIContent(UiReadability.Ellipsis(road,roadStyle,135),road),roadStyle);
   if(cue.HasNext&&!session.services.PostTripVisible){var next=FollowingManeuverBounds(x,y+30);NavigationArrow.Draw(new Rect(next.x+24,next.y,32,32),cue.NextMove,DrivingHudArt.Cyan);GUI.Label(new Rect(next.x-12,next.y+35,next.width+24,26),"이후 "+NavigationCue.FormatDistance(cue.NextDistance),new GUIStyle(white){fontSize=UiReadability.Font(12),alignment=TextAnchor.MiddleCenter,wordWrap=false});}
   if(cue.Move==NavigationMove.Arrival||cue.Move==NavigationMove.Straight)GUI.Label(new Rect(x+16,y+119,284,25),cue.Move==NavigationMove.Arrival?(session.world.map.hybrid?"오른쪽 승하차 지점":"목적지에서 정차"):"목적지까지 직진",DrivingHudArt.Text(13,DrivingHudArt.Light));
  }else{
   GUI.Label(new Rect(x+16,y+44,290,106),nav.RetainingPreviousRoute?"도로로 복귀하면 새 길을 안내합니다\n흐린 선은 이전 경로":nav.HasDestination?nav.instruction:"M 지도에서 호출을 선택하세요",new GUIStyle(DrivingHudArt.Text(16,null,true)){wordWrap=true});
  }
  GUI.Label(new Rect(x+330,y+35,268,29),nav.HasDestination?GoalDistance:"길안내 대기",DrivingHudArt.Text(16,null,true));
  if(SignalTarget!=null)DrawCompactSignal(x+310,y+80,false);
  else GUI.Label(new Rect(x+330,y+72,268,28),nav.HasDestination?"예상 "+RoadNavigation.FormatEta(nav.estimatedSeconds):"새로운 운행을 시작하세요",DrivingHudArt.Text(14,DrivingHudArt.Light));

 }
 void DrawCockpitNavigation(float x){
  var nav=session.jobs.nav;var cue=TurnCue;if(!nav.HasDestination){if(SignalTarget!=null){DrivingHudArt.Glass(new Rect(x+310,20,300,90));DrawCompactSignal(x+310,42,false);}return;}
  if(cue.Valid&&nav.remaining<18){cue.Move=NavigationMove.Arrival;cue.Distance=nav.remaining;cue.Road="목적지 주변";cue.LaneHint="도착";cue.HasNext=false;}
  DrivingHudArt.Glass(new Rect(x-5,20,620,114));
  if(cue.Valid){NavigationArrow.Draw(new Rect(x+12,28,42,42),cue.Move,DrivingHudArt.Cyan);GUI.Label(new Rect(x+66,25,240,35),NavigationCue.FormatDistance(cue.Distance)+" · "+NavigationCue.Label(cue.Move),DrivingHudArt.Text(22,null,true));var road=DrivingHudArt.Text(13,DrivingHudArt.Light);GUI.Label(new Rect(x+66,63,235,24),new GUIContent(UiReadability.Ellipsis(cue.Road??"",road,235),cue.Road),road);if(cue.Move==NavigationMove.Arrival||cue.Move==NavigationMove.Straight&&!cue.HasNext)GUI.Label(new Rect(x+16,92,284,25),cue.Move==NavigationMove.Arrival?(session.world.map.hybrid?"오른쪽 승하차 지점":"목적지에서 정차"):"목적지까지 직진",road);else if(cue.HasNext){NavigationArrow.Draw(new Rect(x+16,94,24,24),cue.NextMove,DrivingHudArt.Cyan);GUI.Label(new Rect(x+50,92,240,25),"이후 "+NavigationCue.FormatDistance(cue.NextDistance),road);}}
  else GUI.Label(new Rect(x+16,29,285,88),nav.RetainingPreviousRoute?"경로 재탐색 중\n도로로 복귀하세요":nav.instruction,new GUIStyle(DrivingHudArt.Text(16,null,true)){wordWrap=true});
  GUI.Label(new Rect(x+330,27,268,27),GoalDistance,DrivingHudArt.Text(16,null,true));if(SignalTarget!=null)DrawCompactSignal(x+310,76,false);else GUI.Label(new Rect(x+330,66,268,30),"예상 "+RoadNavigation.FormatEta(nav.estimatedSeconds),DrivingHudArt.Text(14,DrivingHudArt.Light));
 }
 void DrawFuelStations(){
  var stations=FuelStationLayout.For(session.world.map);var style=new GUIStyle(label){fontSize=UiReadability.Font(11),alignment=TextAnchor.MiddleCenter};
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
  LayoutLawZones();var style=new GUIStyle(label){fontSize=UiReadability.Font(11),alignment=TextAnchor.MiddleCenter,wordWrap=false,padding=new RectOffset(2,2,1,1)};
  if(!Facilities){foreach(var item in ZoneLabels){var p=item.anchor;var c=item.selected?new Color(1,.55f,.85f):new Color(1,.7f,.25f);Circle(p,Color.black,20);Circle(p,new Color(.12f,.17f,.2f),16);
    if(item.zone.Title.Contains("좌회전")){Line(p+new Vector2(4,5),p+new Vector2(4,-4),c,2);Line(p+new Vector2(4,-4),p+new Vector2(-5,-4),c,2);Line(p+new Vector2(-5,-4),p+new Vector2(-1,-8),c,2);Line(p+new Vector2(-5,-4),p+new Vector2(-1,0),c,2);}
    else if(item.zone.Title.Contains("출구")){for(int k=-1;k<=1;k++)Line(p+new Vector2(-5,k*4),p+new Vector2(5,k*4),c,2);}
    else {Circle(p+Vector2.up*-4,c,4);Circle(p,c,4);Circle(p+Vector2.up*4,c,4);}
   }return;}
  foreach(var item in ZoneLabels){var r=item.rect;Line(item.anchor,new Vector2(Mathf.Clamp(item.anchor.x,r.xMin,r.xMax),Mathf.Clamp(item.anchor.y,r.yMin,r.yMax)),item.selected?Color.magenta:new Color(1,.7f,.2f),1.5f);}
  foreach(var item in ZoneLabels){Panel(item.rect);style.normal.textColor=item.selected?new Color(.65f,.1f,.43f):CasualUI.Amber;GUI.Label(item.rect,item.text,style);}
  foreach(var item in ZoneLabels){Dot(item.anchor,Color.black,item.selected?12:9);Dot(item.anchor,item.selected?Color.magenta:new Color(1,.7f,.2f),item.selected?8:5);}
 }
 Vector2 MarkerPoint(Vector3 world,bool clamp,out bool outside){
  var p=Project(world);outside=p.x<14||p.y<14||p.x>Size-14||p.y>Size-14;
  if(outside&&clamp){var d=p-Vector2.one*(Size/2);float k=(Size/2-23)/Mathf.Max(Mathf.Abs(d.x),Mathf.Abs(d.y));p=Vector2.one*(Size/2)+d*k;}if(clamp)p=new Vector2(Mathf.Clamp(p.x,16,Size-16),Mathf.Clamp(p.y,38,Size-12));return p;
 }
 static Rect MarkerTextRect(Vector2 p){return new Rect(Mathf.Clamp(p.x-24,0,Size-65),p.y>Size-48?p.y-23:p.y+10,65,22);}
 void PrepareMarkers(){
  markerLabels.Clear();var jobs=session.jobs;var occupied=new List<Rect>();var player=Project(session.taxi.transform.position);occupied.Add(new Rect(player.x-16,player.y-16,32,32));var targets=new List<Vector3>();
  if(jobs.Active){if(jobs.trip.State==TripState.ToPickup)targets.Add(jobs.EffectiveStop(jobs.current.pickup).V());targets.Add(jobs.EffectiveStop(jobs.current.drop).V());}if(jobs.nav.HasDestination)targets.Add(jobs.nav.destination);
  foreach(var target in targets){var key=new Vector2(target.x,target.z);if(markerLabels.ContainsKey(key))continue;bool outside;var p=MarkerPoint(target,true,out outside);occupied.Add(MapPresentation.PinHitBounds(p,26));var box=LawZoneMapLayout.Place(p,new Vector2(65,22),new Rect(3,3,Size-6,Size-6),occupied);markerLabels[key]=box;occupied.Add(box);}
 }
 public Rect MarkerTextBounds(Vector3 world){Rect r;if(markerLabels.TryGetValue(new Vector2(world.x,world.z),out r))return r;bool outside;return MarkerTextRect(MarkerPoint(world,true,out outside));}
 void ReserveMarker(Vector3 world,List<Rect> occupied){bool outside;var p=MarkerPoint(world,true,out outside);occupied.Add(MapPresentation.PinHitBounds(p,26));occupied.Add(MarkerTextBounds(world));}
 void Marker(Vector3 world,string text,Color color,bool clamp){
  bool outside;var p=MarkerPoint(world,clamp,out outside);if(outside&&!clamp)return;
  if(outside){var d=Project(world)-Vector2.one*(Size/2);text+=Mathf.Abs(d.x)>Mathf.Abs(d.y)?(d.x>0?" →":" ←"):(d.y>0?" ↓":" ↑");}
  MapPresentation.Pin(p,color,clamp?26:20);
  if(clamp&&Facilities){var r=MarkerTextBounds(world);CasualUI.RoundedFill(r,CasualUI.Card);GUI.Label(new Rect(r.x+2,r.y,r.width-2,r.height),text,label);}
 }
 public string SignalCountdownText {get{
  if(!session||!session.traffic||SignalTarget==null)return "";
  float seconds=session.traffic.SecondsToSignalChange(SignalTarget,SignalAxis,SignalSign);
  return seconds>=0&&!float.IsNaN(seconds)&&!float.IsInfinity(seconds)?"신호 변경까지 "+Mathf.CeilToInt(seconds)+"초":"";
 }}
 public NextSignalPreview NextSignalPreview {get{return !session||!session.traffic||SignalTarget==null?global::NextSignalPreview.Unknown:session.traffic.PreviewNextSignal(SignalTarget,SignalAxis,SignalSign);}}
 public SignalHeadPresentation.Head SignalHead {get{return SignalHeadPresentation.Resolve(session.world.map,SignalTarget,SignalAxis,SignalSign);}}
 void DrawCompactSignal(float x,float y,bool blue){
  if(SignalTarget==null)return;var next=NextSignalPreview;var head=SignalHead;Color ink=blue?Color.white:CasualUI.Ink;
  var caption=new GUIStyle(label){fontSize=UiReadability.Font(11),wordWrap=false,normal={textColor=ink}};
  GUI.Label(new Rect(x+8,y-10,284,22),head.lamps==0?"신호 형식 미확인":"다음 · "+head.Caption,caption);
  if(head.lamps==0)return;
  float lampX=x+24,cy=y+29;CasualUI.RoundedFill(new Rect(x+10,y+14,head.lamps*27+4,26),new Color(.10f,.14f,.19f));
  for(int i=0;i<head.lamps;i++)Circle(new Vector2(lampX+i*27,cy),new Color(.24f,.29f,.34f),18);
  bool left=head.left&&session.traffic.HasLeftMovement(SignalTarget,SignalAxis,SignalSign);
  if(next.Known){
   var forward=head.turn<0?next.Left:head.turn>0?next.Right:next.Straight;
   if(forward==SignalColor.Red)Circle(new Vector2(lampX,cy),new Color(1,.23f,.20f),18);
   if(forward==SignalColor.Yellow||left&&next.Left==SignalColor.Yellow)Circle(new Vector2(lampX+27,cy),new Color(1,.79f,.12f),18);
   if(left&&next.Left==SignalColor.Green)NavigationArrow.Draw(new Rect(lampX+44,cy-10,20,20),NavigationMove.Left,new Color(.22f,1,.46f));
   if(forward==SignalColor.Green)Circle(new Vector2(lampX+(head.lamps-1)*27,cy),new Color(.22f,1,.46f),18);
  }
  GUI.Label(new Rect(x+135,y+12,154,30),next.Known?next.CountdownText:"—",new GUIStyle(label){fontSize=UiReadability.Font(22),normal={textColor=ink}});
 }
 static void Panel(Rect r){CasualUI.Panel(r);}
 static Texture2D circleTexture;
 static void Circle(Vector2 p,Color c,float size){if(!circleTexture){circleTexture=new Texture2D(32,32,TextureFormat.RGBA32,false);for(int y=0;y<32;y++)for(int x=0;x<32;x++)circleTexture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(16-Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(16,16)))));circleTexture.Apply();}GUI.color=c;GUI.DrawTexture(new Rect(p.x-size/2,p.y-size/2,size,size),circleTexture);GUI.color=Color.white;}
 static void Dot(Vector2 p,Color c,float size){GUI.color=c;GUI.DrawTexture(new Rect(p.x-size/2,p.y-size/2,size,size),Texture2D.whiteTexture);GUI.color=Color.white;}
 static void Line(Vector2 a,Vector2 b,Color c,float width){HudGlyph.MapLine(a,b,c,width,new Rect(0,0,Size,Size));}
}
