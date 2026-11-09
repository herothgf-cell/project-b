using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// One modal shares the real navigation path and situation definitions with the minimap.
public partial class FullMapOverlay : MonoBehaviour {
 public DriveSession session;public bool IsOpen {get;private set;}
 public float MapUiScale {get{return UiReadability.MapScale(Screen.width,Screen.height);}}
 public float CanvasWidth {get{return Screen.width/MapUiScale;}}
 public float CanvasHeight {get{return Screen.height/MapUiScale;}}
 int MapFont(int reference){return UiReadability.FontFor(reference,MapUiScale);}
 float highlightUntil;
 bool locationCopied,locationOpen; public int SelectedOffer {get;private set;}=-1; JobOffer selectedOffer;
 string acceptFeedback="";bool selectedPickupReachable;public int AcceptButtonClickCount {get;private set;}
 public string AcceptFeedback {get{return acceptFeedback;}}
 public Rect LogicalAcceptButtonBounds {get{var viewport=MapRect;float x=viewport.xMax+16;return new Rect(x,viewport.yMax-50,CanvasWidth-x-20,44);}}
 public Rect AcceptButtonBounds {get{return UiReadability.ScreenRect(LogicalAcceptButtonBounds,MapUiScale);}}
 public int RepaintCount {get;private set;}
 public string AcceptBlockedReason {get{
  var j=session.jobs;if(!IsOpen)return "지도에서 호출을 선택하세요";
  if(locationOpen)return "위치 정보창을 닫으세요";
  if(session.simulationMatch&&session.simulationMatch.Finished)return "경기가 종료되었습니다";
  if(previousPause)return "일시정지를 해제하세요";
  if(j.Active)return "현재 운송을 먼저 마치세요";
  if(selectedPlace!=null)return "아래 호출 목록에서 호출을 선택하세요";
  if(SelectedOffer<0||SelectedOffer>=j.offers.Length||selectedOffer==null)return "호출을 선택하세요";
  if(j.offers[SelectedOffer]!=selectedOffer)return "새 호출로 바뀌었어요. 다시 선택하세요";
  if(j.AcceptanceLocked)return "세차가 필요해요 · 주유소 F7";
  if(!session.business.CanAccept)return session.business.ledger.Driver!=null&&!session.business.ledger.Driver.CanAccept?"피로가 높아요. 먼저 쉬세요":"현재 영업 상태에서는 호출을 수락할 수 없습니다";
  if(float.IsInfinity(selectedOffer.distance)||float.IsNaN(selectedOffer.distance))return "운송 경로 없음 · 다른 호출 선택";
  if(!selectedPickupReachable)return "승차 경로를 찾을 수 없어요";
  return "";
 }}
 public static string DescribeLocation(MapData map,Vector3 position,float heading,IEnumerable<TrafficJunction> junctions){
  TrafficJunction nearest=null;float distance=float.MaxValue;
  foreach(var junction in junctions){var delta=junction.center-position;delta.y=0;float d=delta.magnitude;if(d<distance){nearest=junction;distance=d;}}
  string id=string.IsNullOrEmpty(map.mapId)?(HdRouteTrial.Enabled?"route42":"unknown"):map.mapId;
  return "지역: "+(id=="unknown"?"미지정":MapCatalog.Name(id))+" ("+id+")\n지도 좌표(m): x="+position.x.ToString("R",CultureInfo.InvariantCulture)+", z="+position.z.ToString("R",CultureInfo.InvariantCulture)+
   "\n방향(도, 북=0 / 동=90): "+Mathf.Repeat(heading,360).ToString("R",CultureInfo.InvariantCulture)+
   "\n가까운 교차로: "+(nearest==null?"없음":nearest.name+" ("+distance.ToString("0.0",CultureInfo.InvariantCulture)+"m)");
 }
 public string LocationText {get{return DescribeLocation(session.world.map,session.taxi.transform.position,session.taxi.transform.eulerAngles.y,session.traffic.junctions);}}
 public bool ShowFacilities=true,ShowTraffic=false,ShowLaw=false;
 public sealed class MapMarker {public Vector3 position;public string label;public Color color;public bool named;}
 public List<MapMarker> VisibleMarkers(){
  var markers=new List<MapMarker>();
  var occupancy=session.GetComponent<StopOccupancySystem>();if(occupancy){var state=occupancy.Capture();int index=session.jobs.Catalog.IndexOf(state.stopId);if(state.active&&index>=0)markers.Add(new MapMarker{position=session.jobs.stops[index].Kerb,label="일시 점유",color=new Color(1,.55f,.2f),named=true});}
  if(ShowFacilities){int i=0;foreach(var station in FuelStationLayout.For(session.world.map))markers.Add(new MapMarker{position=station.pump,label=station.ServiceLabel+" "+(++i),color=new Color(1,.75f,.15f),named=true});
   if(ProjectMapScene.Active(session.world.map)){foreach(var o in session.world.map.projectScene.objects)if(o.enabled&&o.IsService)markers.Add(new MapMarker{position=o.Position,label=o.name,color=o.kind=="rest"?Color.cyan:new Color(.9f,.6f,1),named=true});}
   else {var rest=FuelStationLayout.Rest(session.world.map);if(rest!=null)markers.Add(new MapMarker{position=rest.pump,label="휴식 주차",color=Color.cyan,named=true});
    if(session.business.useV2)for(int k=1;k<session.services.indices.Length;k++)markers.Add(new MapMarker{position=(k==1?session.services.MealPosition:session.services.StopFor(k).V()),label=k==1?"식당":"주차장",color=new Color(.9f,.6f,1),named=true});}}
  if(ShowTraffic){foreach(var car in session.traffic.cars)if(car.Visible)markers.Add(new MapMarker{position=car.transform.position,label=car.speed<.2f?"대기 차량":"이동 차량",color=car.speed<.2f?new Color(1,.65f,.2f):new Color(.65f,.8f,.9f)});
   foreach(var incident in session.roadConditions.incidents)markers.Add(new MapMarker{position=incident.position,label="정체·정차",color=Color.red,named=true});}
  return markers;
 }
 public List<ServiceLawZone> VisibleLawZones(){var result=new List<ServiceLawZone>();var zones=GetComponent<ServiceLawZones>();if(ShowLaw&&zones&&zones.Ready)result.AddRange(zones.zones);return result;}
 public float ZoomLevel {get;private set;}=1;Vector2 center=new Vector2(.5f,.5f);bool dragging;Vector2 dragStart;bool dragMoved;
 public void Zoom(int step){ZoomLevel=Mathf.Clamp(ZoomLevel*Mathf.Pow(1.5f,step),1,8);ClampCenter();}
 public void ResetView(){ZoomLevel=1;center=new Vector2(.5f,.5f);}
 public void FocusPlayer(){highlightUntil=Time.unscaledTime+3;FocusWorld(session.taxi.transform.position);}
 public void FocusWorld(Vector3 point){ZoomLevel=Mathf.Max(2,ZoomLevel);float span=Mathf.Max(session.world.map.width,session.world.map.depth);center=new Vector2(.5f+(point.x-session.world.map.width/2)/span,.5f-(point.z-session.world.map.depth/2)/span);ClampCenter();}
 public void Pan(Vector2 pixels){center-=pixels/(Mathf.Min(MapRect.width,MapRect.height)*ZoomLevel);ClampCenter();}
 void ClampCenter(){center=new Vector2(Mathf.Clamp01(center.x),Mathf.Clamp01(center.y));}
 Vector2 filterScroll;bool previousPause;float previousScale;int closedFrame=-1;GUIStyle text,heading,button;
 public bool ClosedThisFrame {get{return closedFrame==Time.frameCount;}}
 public Rect MapRect {get{return new Rect(20,86,Mathf.Max(280,CanvasWidth-370),Mathf.Max(250,CanvasHeight-112));}}
 public bool Open(){
  if(session.SettingsOpen||IsOpen)return false;
  var b=session.business;
  if(session.ChoiceOpen||session.RideResultOpen||session.PracticeMenuOpen||session.PracticeBusy||b.preparing||b.ShowingSummary||b.restartPrompt||b.pendingPayment>=0||b.taxPrompt||session.services.open||session.law.HistoryOpen)return false;
  previousPause=session.paused;previousScale=Time.timeScale;if(!session.simulationMatch||previousPause||previousScale<=0){session.paused=true;Time.timeScale=0;}locationCopied=false;locationOpen=false;if(SelectedOffer>=0&&session.jobs.offers[SelectedOffer]!=selectedOffer){SelectedOffer=-1;selectedOffer=null;}IsOpen=true;highlightUntil=Time.unscaledTime+3;if(session.jobs.Active)RestoreTripRoute(false);else if(selectedOffer!=null)SelectOffer(SelectedOffer);return true;
 }
 public void Close(){if(!IsOpen)return;HideFacilityPlanner();IsOpen=false;dragging=false;closedFrame=Time.frameCount;if(session.simulationMatch&&session.simulationMatch.Finished){session.paused=true;Time.timeScale=0;}else {session.paused=previousPause;Time.timeScale=previousPause?0:previousScale>0?previousScale:1;}}
 void Update(){if(session.SettingsOpen)return;RefreshRoutePaint();if(IsOpen){if(Input.GetKeyDown(KeyCode.M)||Input.GetKeyDown(KeyCode.Escape)){if(locationOpen)locationOpen=false;else Close();}}else if(Input.GetKeyDown(KeyCode.M))Open();}
 void OnDisable(){if(IsOpen)Close();}
 Vector2 Project(Vector3 p,Rect map){float span=Mathf.Max(session.world.map.width,session.world.map.depth);var unit=new Vector2(.5f+(p.x-session.world.map.width/2)/span,.5f-(p.z-session.world.map.depth/2)/span);return map.center+(unit-center)*Mathf.Min(map.width,map.height)*ZoomLevel;}
 void OnGUI(){if(!IsOpen)return;if(text==null){text=new GUIStyle(GUI.skin.label){font=KoreanText.Font,wordWrap=true};heading=new GUIStyle(text){font=KoreanText.BoldFont};button=KoreanText.Button(17);text.normal.textColor=heading.normal.textColor=CasualUI.Ink;}text.fontSize=MapFont(16);heading.fontSize=MapFont(22);button.fontSize=MapFont(15);GUI.depth=-220;
  var before=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(MapUiScale,MapUiScale,1));try{GUI.ModalWindow(94721,new Rect(0,0,CanvasWidth,CanvasHeight),Draw,GUIContent.none,GUIStyle.none);}finally{GUI.matrix=before;}
 }
 public bool FocusStop(int index){if(index<0||index>=session.jobs.stops.Length)return false;FocusWorld(session.jobs.Active?session.jobs.EffectiveStop(index).V():session.jobs.stops[index].V());return true;}
 public bool SelectOffer(int index){var j=session.jobs;if(!IsOpen||j.Active||index<0||index>=j.offers.Length||j.offers[index]==null)return false;HideFacilityPlanner();filterScroll=Vector2.zero;SelectedOffer=index;selectedOffer=j.offers[index];selectedPlace=null;acceptFeedback="";previewPath=j.nav.Find(j.stops[selectedOffer.pickup],j.stops[selectedOffer.drop]);pickupPath=j.nav.PreviewTo(j.stops[selectedOffer.pickup]);float distance=PassengerJobs.DirectedDistance(j.nav.hd,session.taxi.transform.position,session.taxi.transform.forward,j.stops[selectedOffer.pickup]);selectedPickupReachable=!float.IsInfinity(distance)&&!float.IsNaN(distance);FitRoutes(pickupPath,previewPath);RefreshRoutePaint(true);return true;}
 public bool FocusSelectedStop(bool pickup){var o=session.jobs.Active?session.jobs.current:selectedOffer;return o!=null&&FocusStop(pickup?o.pickup:o.drop);}
 public bool AcceptSelected(){var j=session.jobs;string reason=AcceptBlockedReason;if(!string.IsNullOrEmpty(reason)){acceptFeedback=reason;return false;}int index=SelectedOffer;Close();bool accepted=j.Accept(index);if(!accepted){string message=j.message;Open();acceptFeedback=string.IsNullOrEmpty(message)?"호출을 수락하지 못했습니다 · 목록에서 다시 선택하세요":message;}else acceptFeedback="";return accepted;}
 void Draw(int id){
  if(Event.current.type==EventType.Repaint)RepaintCount++;
  Fill(new Rect(0,0,CanvasWidth,CanvasHeight),CasualUI.Canvas);
  CasualUI.Panel(new Rect(12,10,CanvasWidth-24,66));
  GUI.enabled=!locationOpen;
  GUI.Label(new Rect(24,20,CanvasWidth-420,38),MapCatalog.Name(session.world.map.mapId)+" 지도 · 북 ↑"+(session.simulationMatch?(session.LivePvpMapOpen?" · 경기 진행 중 · 내 차 정지":" · 일시정지"):""),heading);
  if(GUI.Button(new Rect(CanvasWidth-330,20,150,38),"위치 정보",button))ShowLocation();
  if(GUI.Button(new Rect(CanvasWidth-166,20,142,38),"닫기 M",button)){Close();return;}
  var viewport=MapRect;var ev=Event.current;var jobs=session.jobs;
  GUI.BeginGroup(viewport);var map=new Rect(0,0,viewport.width,viewport.height);Fill(map,MapPresentation.Land);var data=session.world.map;var tile=GetComponent<DriveHUD>().LightTile;
  if(tile){var a=Project(new Vector3(0,0,data.depth),map);float span=Mathf.Max(data.width,data.depth),pixels=Mathf.Min(map.width,map.height);GUI.DrawTexture(new Rect(a.x,a.y,data.width/span*pixels*ZoomLevel,data.depth/span*pixels*ZoomLevel),tile);}
  for(int pass=0;pass<2;pass++)foreach(var segment in RoutePaint)HudGlyph.MapLine(Project(segment.a,map),Project(segment.b,map),segment.retained?(pass==0?Color.white:MapPresentation.PreviousRoute):pass==0?Color.white:ShowTraffic?segment.color:MapPresentation.RouteColor(segment.passenger),pass==0?(segment.passenger?7:11):4,new Rect(0,0,MapRect.width,MapRect.height));
  var occupied=FacilityLabelObstacles(map);
  if(ShowFacilities){var labelStyle=FacilityLabelStyle();var labels=LayoutFacilityLabels(map,labelStyle,occupied);int labelIndex=0;foreach(var place in Places()){var p=Project(place.position,map);if(!map.Contains(p))continue;if(HudGlyph.Button(new Rect(p.x-15,p.y-15,30,30),place.glyph,place.name,true))SelectPlace(place);if(!string.IsNullOrEmpty(place.capabilityLabel)){var badge=labels[labelIndex++];if(badge.width>0){CasualUI.Panel(badge);GUI.Label(new Rect(badge.x+6,badge.y+4,badge.width-12,badge.height-8),place.capabilityLabel,labelStyle);}}}}
  if(selectedPlace!=null){var p=Project(selectedPlace.position,map);Dot(p,Color.magenta,10);}

  foreach(var marker in VisibleMarkers()){if(marker.named&&marker.label!="정체·정차"&&marker.label!="일시 점유")continue;
   var p=Project(marker.position,map);if(!map.Contains(p))continue;Dot(p,Color.black,marker.named?13:7);Dot(p,marker.color,marker.named?9:4);
   if(marker.named){var box=LawZoneMapLayout.Place(p,new Vector2(82,26),map,occupied);occupied.Add(box);CasualUI.Panel(box);GUI.Label(box,marker.label,text);}
  }
  foreach(var zone in VisibleLawZones()){var p=Project(zone.junction.center,map);if(!map.Contains(p))continue;Dot(p,new Color(1,.55f,.1f),14);var box=LawZoneMapLayout.Place(p,new Vector2(146,42),map,occupied);occupied.Add(box);CasualUI.Panel(box);GUI.Label(box,zone.MapTitle,text);}
  // Active pickup, drop and via pins remain visible while browsing facilities.
  if(jobs.Active&&jobs.current!=null){StopPin(jobs.current.pickup,"승차",MapPresentation.Pickup,map);StopPin(jobs.current.drop,"하차",MapPresentation.Passenger,map);if(jobs.PassengerViaPending)StopPin(jobs.PassengerViaStop,"경유",new Color(1,.8f,.2f),map);}
  else if(!jobs.Active){
   for(int i=0;i<jobs.offers.Length;i++){var o=jobs.offers[i];if(o==null)continue;var p=CallPinPosition(i,map);var anchor=Project(jobs.stops[o.pickup].V(),map);if((p-anchor).sqrMagnitude>1){Line(anchor,p,new Color(.06f,.2f,.23f),2);if(map.Contains(anchor))Dot(anchor,new Color(.06f,.2f,.23f),7);}MapPresentation.Pin(p,MapPresentation.Pickup,26,(i+1).ToString());if(GUI.Button(MapPresentation.PinHitBounds(p,26),new GUIContent("",KoreanText.Stop(jobs.stops[o.pickup].name)+" · 호출 "+(i+1)),GUIStyle.none))SelectOffer(i);}
   if(selectedOffer!=null){StopPin(selectedOffer.pickup,"승차",MapPresentation.Pickup,map);StopPin(selectedOffer.drop,"하차",MapPresentation.Passenger,map);}
  }
  if(session.simulationMatch)foreach(var bot in session.simulationMatch.Bots){int driver=bot.SimulationDriver;var rival=Project(bot.taxi.transform.position,map);if(map.Contains(rival)){Dot(rival,Color.black,22);Dot(rival,PvpDriverPresentation.Color(driver),15);GUI.Label(new Rect(rival.x+12,rival.y-12,100,24),PvpDriverPresentation.Name(driver),text);}} var player=Project(session.taxi.transform.position,map);var forward=new Vector2(session.taxi.transform.forward.x,-session.taxi.transform.forward.z).normalized;MapPresentation.EmphasizedPlayer(player,forward,map,Time.unscaledTime<highlightUntil);
  if(HudGlyph.Button(new Rect(12,12,44,42),0,"주요 시설",ShowFacilities))ShowFacilities=!ShowFacilities;
  if(HudGlyph.Button(new Rect(62,12,44,42),1,"교통 상황",ShowTraffic))ShowTraffic=!ShowTraffic;
  if(HudGlyph.Button(new Rect(112,12,44,42),2,"교통 법규",ShowLaw))ShowLaw=!ShowLaw;
  float cx=map.width-60,cy=map.height-178;
  if(HudGlyph.Button(new Rect(cx,cy,44,44),3,"내 위치",true))FocusPlayer();
  if(GUI.Button(new Rect(cx,cy+50,44,44),"+",button))Zoom(1);
  if(GUI.Button(new Rect(cx,cy+100,44,44),"-",button))Zoom(-1);
  if(!locationOpen){if(ev.type==EventType.ScrollWheel&&map.Contains(ev.mousePosition)){Zoom(ev.delta.y<0?1:-1);ev.Use();}
   if(ev.type==EventType.MouseDown&&ev.button==0&&map.Contains(ev.mousePosition)){dragging=true;dragStart=ev.mousePosition;dragMoved=false;ev.Use();}
   if(ev.type==EventType.MouseDrag&&dragging){if(Vector2.Distance(ev.mousePosition,dragStart)>5)dragMoved=true;Pan(ev.delta);ev.Use();}if(ev.type==EventType.MouseUp&&dragging){if(!dragMoved&&map.Contains(ev.mousePosition)){float span=Mathf.Max(data.width,data.depth);var unit=center+(ev.mousePosition-map.center)/(Mathf.Min(map.width,map.height)*ZoomLevel);SelectRoadPoint(new Vector3(data.width/2+(unit.x-.5f)*span,0,data.depth/2-(unit.y-.5f)*span));}dragging=false;ev.Use();}
   if(ev.type==EventType.KeyDown&&(ev.keyCode==KeyCode.Equals||ev.keyCode==KeyCode.KeypadPlus||ev.keyCode==KeyCode.Plus||ev.keyCode==KeyCode.Minus||ev.keyCode==KeyCode.KeypadMinus)){Zoom(ev.keyCode==KeyCode.Minus||ev.keyCode==KeyCode.KeypadMinus?-1:1);ev.Use();}}
  GUI.EndGroup();
  float x=viewport.xMax+16,w=CanvasWidth-x-20;DrawSidebar(x,w,viewport);
  GUI.Label(new Rect(24,CanvasHeight-23,CanvasWidth-48,22),(session.world.map.mapId=="pvp-city-a"?"승하차 화살표 = 차량 정차 방향 · 자체 제작 PVP 도시 · 가상 도로":"승하차 화살표 = 차량 정차 방향 · © OpenStreetMap contributors / ODbL · NGII 기반 게임 지도"),new GUIStyle(text){fontSize=MapFont(11)});
  if(!string.IsNullOrEmpty(GUI.tooltip)&&!locationOpen){float tw=Mathf.Min(330,viewport.width-24),th=text.CalcHeight(new GUIContent(GUI.tooltip),tw-16)+16;var tip=new Rect(viewport.x+12,viewport.y+64,tw,th);CasualUI.Panel(tip);GUI.Label(new Rect(tip.x+8,tip.y+8,tw-16,th-16),GUI.tooltip,text);}
  if(locationOpen){Fill(new Rect(0,0,CanvasWidth,CanvasHeight),new Color(0,0,0,.7f));var r=new Rect(CanvasWidth/2-240,CanvasHeight/2-150,480,300);CasualUI.Panel(r);GUI.BeginGroup(r);LocationPopup(94722);GUI.EndGroup();}
 }
 public void ShowLocation(){locationOpen=true;locationCopied=false;}
 public bool LocationOpen {get{return locationOpen;}}
 public void HideLocation(){locationOpen=false;}
 void LocationPopup(int id){GUI.Label(new Rect(20,8,440,34),"현재 위치",heading);GUI.TextArea(new Rect(20,48,440,165),LocationText,new GUIStyle(GUI.skin.textArea){font=KoreanText.Font,fontSize=MapFont(16),wordWrap=true,normal={textColor=CasualUI.Ink}});if(GUI.Button(new Rect(20,230,210,42),locationCopied?"복사됨":"좌표 복사",button)){GUIUtility.systemCopyBuffer=LocationText;locationCopied=true;}if(GUI.Button(new Rect(250,230,210,42),"닫기",button))locationOpen=false;}
 public Vector2 CallPinPosition(int index,Rect map){
  var placed=PvpPinObstacles(map);Vector2 result=Vector2.zero;for(int i=0;i<=index;i++){if(session.jobs.offers[i]==null)continue;var anchor=Project(session.jobs.stops[session.jobs.offers[i].pickup].V(),map);result=PlacePvpCallPin(anchor,map,placed);placed.Add(MapPresentation.PinHitBounds(result,26));}return result;}
 List<Rect> PvpPinObstacles(Rect map){
  var occupied=new List<Rect>();
  if(session.taxi){var p=Project(session.taxi.transform.position,map);occupied.Add(new Rect(p.x-16,p.y-16,32,32));}
  if(session.simulationMatch)foreach(var bot in session.simulationMatch.Bots)if(bot&&bot.taxi){var p=Project(bot.taxi.transform.position,map);occupied.Add(new Rect(p.x-13,p.y-13,26,26));occupied.Add(new Rect(p.x+12,p.y-12,100,24));}
  if(ShowFacilities&&session.services)foreach(var place in Places()){var p=Project(place.position,map);if(!map.Contains(p))continue;occupied.Add(new Rect(p.x-16,p.y-16,32,32));}
  return occupied;
 }
 static Vector2 PlacePvpCallPin(Vector2 anchor,Rect map,List<Rect> occupied){
  var best=new Vector2(Mathf.Clamp(anchor.x,20,map.width-20),Mathf.Clamp(anchor.y,80,map.height-20));float score=float.PositiveInfinity;
  for(int y=-3;y<=3;y++)for(int x=-3;x<=3;x++){var p=new Vector2(Mathf.Clamp(anchor.x+x*36,20,map.width-20),Mathf.Clamp(anchor.y+y*36,80,map.height-20));if(p.x>map.width-80&&p.y>map.height-185)continue;bool clear=true;foreach(var other in occupied)if(MapPresentation.PinHitBounds(p,26).Overlaps(other)){clear=false;break;}float distance=(p-anchor).sqrMagnitude;if(clear&&distance<score){best=p;score=distance;}}
  return best;
 }
 public int AvailableCallCount {get{if(session.jobs.Active)return 0;int count=0;foreach(var o in session.jobs.offers)if(o!=null)count++;return count;}}
 static Rect StopPinLabel(Vector2 p,Rect map){return new Rect(Mathf.Clamp(p.x+32,map.xMin,map.xMax-54),Mathf.Clamp(p.y-15,map.yMin,map.yMax-30),54,30);}
 void StopPin(int stop,string caption,Color color,Rect map){var target=session.jobs.Active?session.jobs.EffectiveStop(stop):session.jobs.stops[stop];var p=Project(target.V(),map);if(!map.Contains(p))return;var old=GUI.backgroundColor;GUI.backgroundColor=Color.white;if(GUI.Button(StopPinLabel(p,map),caption,button))FocusStop(stop);if(GUI.Button(MapPresentation.PinHitBounds(p,26),new GUIContent("",caption+" · 화살표 방향으로 정차"),GUIStyle.none))FocusStop(stop);MapPresentation.Pin(p,color,26);GUI.backgroundColor=old;
  // Map north points up: the stop's lane heading, never its lateral kerb offset.
  var forward=new Vector2(target.dx,-target.dz).normalized;if(forward.sqrMagnitude<.1f)return;var side=new Vector2(-forward.y,forward.x);var tip=p+forward*28;var a=tip-forward*10+side*7;var b=tip-forward*10-side*7;
  foreach(float width in new[]{8f,4f}){var paint=width>4?Color.black:color;Line(p,tip,paint,width);Line(a,tip,paint,width);Line(tip,b,paint,width);}
 }
 static void Fill(Rect r,Color c){GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
 static void Dot(Vector2 p,Color c,float size){Fill(new Rect(p.x-size/2,p.y-size/2,size,size),c);}
 void Line(Vector2 a,Vector2 b,Color c,float width){HudGlyph.MapLine(a,b,c,width,new Rect(0,0,MapRect.width,MapRect.height));}
}
