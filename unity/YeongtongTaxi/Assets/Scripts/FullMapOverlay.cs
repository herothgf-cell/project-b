using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// One modal shares the real navigation path and situation definitions with the minimap.
public partial class FullMapOverlay : MonoBehaviour {
 public DriveSession session;public bool IsOpen {get;private set;}
 bool locationCopied,locationOpen; public int SelectedOffer {get;private set;}=-1; JobOffer selectedOffer;
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
  if(ShowFacilities){int i=0;foreach(var station in FuelStationLayout.For(session.world.map))markers.Add(new MapMarker{position=station.pump,label="주유 "+(++i),color=new Color(1,.75f,.15f),named=true});
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
 public void FocusPlayer(){FocusWorld(session.taxi.transform.position);}
 public void FocusWorld(Vector3 point){ZoomLevel=Mathf.Max(2,ZoomLevel);float span=Mathf.Max(session.world.map.width,session.world.map.depth);center=new Vector2(.5f+(point.x-session.world.map.width/2)/span,.5f-(point.z-session.world.map.depth/2)/span);ClampCenter();}
 public void Pan(Vector2 pixels){center-=pixels/(Mathf.Min(MapRect.width,MapRect.height)*ZoomLevel);ClampCenter();}
 void ClampCenter(){center=new Vector2(Mathf.Clamp01(center.x),Mathf.Clamp01(center.y));}
 Vector2 filterScroll;bool previousPause;float previousScale;int closedFrame=-1;GUIStyle text,heading,button;
 public bool ClosedThisFrame {get{return closedFrame==Time.frameCount;}}
 public Rect MapRect {get{return new Rect(20,86,Mathf.Max(280,Screen.width-370),Mathf.Max(250,Screen.height-112));}}
 public bool Open(){
  if(IsOpen)return false;
  var b=session.business;
  if(session.ChoiceOpen||session.PracticeMenuOpen||session.PracticeBusy||b.preparing||b.ShowingSummary||b.restartPrompt||b.pendingPayment>=0||b.taxPrompt||session.services.open||session.law.HistoryOpen)return false;
  previousPause=session.paused;previousScale=Time.timeScale;session.paused=true;Time.timeScale=0;locationCopied=false;locationOpen=false;if(SelectedOffer>=0&&session.jobs.offers[SelectedOffer]!=selectedOffer){SelectedOffer=-1;selectedOffer=null;}IsOpen=true;return true;
 }
 public void Close(){if(!IsOpen)return;IsOpen=false;dragging=false;closedFrame=Time.frameCount;session.paused=previousPause;Time.timeScale=previousScale;}
 void Update(){RefreshRoutePaint();if(IsOpen){if(Input.GetKeyDown(KeyCode.M)||Input.GetKeyDown(KeyCode.Escape)){if(locationOpen)locationOpen=false;else Close();}}else if(Input.GetKeyDown(KeyCode.M))Open();}
 void OnDisable(){if(IsOpen)Close();}
 Vector2 Project(Vector3 p,Rect map){float span=Mathf.Max(session.world.map.width,session.world.map.depth);var unit=new Vector2(.5f+(p.x-session.world.map.width/2)/span,.5f-(p.z-session.world.map.depth/2)/span);return map.center+(unit-center)*Mathf.Min(map.width,map.height)*ZoomLevel;}
 void OnGUI(){if(!IsOpen)return;if(text==null){text=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=15,wordWrap=true};heading=new GUIStyle(text){fontSize=22,fontStyle=FontStyle.Bold};button=KoreanText.Button(14);text.normal.textColor=heading.normal.textColor=Color.white;}GUI.depth=-220;GUI.ModalWindow(94721,new Rect(0,0,Screen.width,Screen.height),Draw,GUIContent.none,GUIStyle.none);}
 public bool FocusStop(int index){if(index<0||index>=session.jobs.stops.Length)return false;FocusWorld(session.jobs.stops[index].V());return true;}
 public bool SelectOffer(int index){var j=session.jobs;if(!IsOpen||j.Active||index<0||index>=j.offers.Length||j.offers[index]==null)return false;SelectedOffer=index;selectedOffer=j.offers[index];selectedPlace=null;previewPath=j.nav.Find(j.stops[selectedOffer.pickup],j.stops[selectedOffer.drop]);pickupPath=j.nav.PreviewTo(j.stops[selectedOffer.pickup]);FitRoutes(pickupPath,previewPath);RefreshRoutePaint(true);return true;}
 public bool FocusSelectedStop(bool pickup){var o=session.jobs.Active?session.jobs.current:selectedOffer;return o!=null&&FocusStop(pickup?o.pickup:o.drop);}
 public bool AcceptSelected(){var j=session.jobs;if(!IsOpen||previousPause||SelectedOffer<0||SelectedOffer>=j.offers.Length||selectedOffer==null||j.offers[SelectedOffer]!=selectedOffer||!session.business.CanAccept||j.Active)return false;int index=SelectedOffer;Close();bool accepted=j.Accept(index);if(!accepted)Open();return accepted;}
 void Draw(int id){
  Fill(new Rect(0,0,Screen.width,Screen.height),new Color(.035f,.07f,.09f));
  GUI.enabled=!locationOpen;
  GUI.Label(new Rect(24,20,Screen.width-420,38),MapCatalog.Name(session.world.map.mapId)+" 지도 · 북 ↑",heading);
  if(GUI.Button(new Rect(Screen.width-330,20,150,38),"위치 정보",button))ShowLocation();
  if(GUI.Button(new Rect(Screen.width-166,20,142,38),"M 닫기",button)){Close();return;}
  var viewport=MapRect;var ev=Event.current;var jobs=session.jobs;
  GUI.BeginGroup(viewport);var map=new Rect(0,0,viewport.width,viewport.height);Fill(map,new Color(.78f,.84f,.79f));var data=session.world.map;var tile=GetComponent<DriveHUD>().LightTile;
  if(tile){var a=Project(new Vector3(0,0,data.depth),map);float span=Mathf.Max(data.width,data.depth),pixels=Mathf.Min(map.width,map.height);GUI.DrawTexture(new Rect(a.x,a.y,data.width/span*pixels*ZoomLevel,data.depth/span*pixels*ZoomLevel),tile);}
  for(int pass=0;pass<2;pass++)foreach(var segment in RoutePaint)HudGlyph.MapLine(Project(segment.a,map),Project(segment.b,map),pass==0?(segment.passenger?new Color(.5f,.08f,.35f):Color.black):segment.color,pass==0?7:4,new Rect(0,0,MapRect.width,MapRect.height));
  if(ShowFacilities)foreach(var place in Places()){var p=Project(place.position,map);if(!map.Contains(p))continue;if(HudGlyph.Button(new Rect(p.x-15,p.y-15,30,30),place.glyph,place.name,true))SelectPlace(place);}
  if(selectedPlace!=null){var p=Project(selectedPlace.position,map);Dot(p,Color.magenta,10);}

  var occupied=new List<Rect>();
  foreach(var marker in VisibleMarkers()){if(marker.named&&marker.label!="정체·정차")continue;
   var p=Project(marker.position,map);if(!map.Contains(p))continue;Dot(p,Color.black,marker.named?13:7);Dot(p,marker.color,marker.named?9:4);
   if(marker.named){var box=LawZoneMapLayout.Place(p,new Vector2(82,26),map,occupied);occupied.Add(box);Fill(box,new Color(.05f,.12f,.15f,.9f));GUI.Label(box,marker.label,text);}
  }
  foreach(var zone in VisibleLawZones()){var p=Project(zone.junction.center,map);if(!map.Contains(p))continue;Dot(p,new Color(1,.55f,.1f),14);var box=LawZoneMapLayout.Place(p,new Vector2(146,42),map,occupied);occupied.Add(box);Fill(box,new Color(.08f,.13f,.17f,.95f));GUI.Label(box,zone.MapTitle,text);}
  // Both pickup and destination remain clickable; clicking changes only the view.
  if(jobs.Active&&selectedPlace==null){StopPin(jobs.current.pickup,"승차",new Color(.2f,.8f,.5f),map);StopPin(jobs.current.drop,"하차",new Color(1,.35f,.7f),map);}
  else if(!jobs.Active){
   for(int i=0;i<jobs.offers.Length;i++){var o=jobs.offers[i];if(o==null)continue;var p=Project(jobs.stops[o.pickup].V(),map);p+=new Vector2((i-1)*24,0);if(map.Contains(p)&&GUI.Button(new Rect(p.x-15,p.y-15,30,30),(i+1).ToString(),button))SelectOffer(i);}
   if(selectedOffer!=null){StopPin(selectedOffer.pickup,"승차",new Color(.2f,.8f,.5f),map);StopPin(selectedOffer.drop,"하차",new Color(1,.35f,.7f),map);}
  }
  var player=Project(session.taxi.transform.position,map);var forward=new Vector2(session.taxi.transform.forward.x,-session.taxi.transform.forward.z).normalized;var side=new Vector2(-forward.y,forward.x);Dot(player,Color.black,26);Line(player-forward*7-side*8,player+forward*12,Color.yellow,5);Line(player+forward*12,player-forward*7+side*8,Color.yellow,5);Line(player-forward*7+side*8,player-forward*7-side*8,Color.yellow,5);
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
  float x=viewport.xMax+16,w=Screen.width-x-20;GUI.Label(new Rect(x,90,w,32),selectedPlace!=null?"선택한 장소":jobs.Active?"진행 중인 운송":"주변 호출",heading);
  if(selectedPlace==null){
  GUI.Label(new Rect(x,122,w,25),"실선: 승차까지 · 점선: 승객 운송",new GUIStyle(text){fontSize=11});
  filterScroll=GUI.BeginScrollView(new Rect(x,152,w,viewport.height-138),filterScroll,new Rect(0,0,w-20,jobs.Active?220:jobs.offers.Length*160+110));float rw=w-24;
  if(!jobs.Active)for(int i=0;i<jobs.offers.Length;i++){var o=jobs.offers[i];if(o==null)continue;float y=i*160;var old=GUI.backgroundColor;if(i==SelectedOffer)GUI.backgroundColor=new Color(.25f,.9f,.7f);
   if(GUI.Button(new Rect(0,y,rw,138),(i+1)+"  "+KoreanText.Stop(jobs.stops[o.pickup].name)+"\n→ "+KoreanText.Stop(jobs.stops[o.drop].name)+"\n예상 "+o.fare.ToString("N0")+"원 · 운송 기한 "+RoadNavigation.FormatEta(o.seconds),new GUIStyle(button){alignment=TextAnchor.MiddleLeft,wordWrap=true,padding=new RectOffset(12,12,10,10)}))SelectOffer(i);GUI.backgroundColor=old;}
  var chosen=jobs.Active?jobs.current:selectedOffer;float foot=jobs.Active?0:jobs.offers.Length*160;
  if(chosen!=null){if(GUI.Button(new Rect(0,foot,rw,40),"승차 · "+KoreanText.Stop(jobs.stops[chosen.pickup].name),button))FocusSelectedStop(true);if(GUI.Button(new Rect(0,foot+48,rw,40),"하차 · "+KoreanText.Stop(jobs.stops[chosen.drop].name),button))FocusSelectedStop(false);}
  GUI.EndScrollView();GUI.enabled=!locationOpen&&!previousPause&&!jobs.Active&&selectedOffer!=null&&session.business.CanAccept;
  if(GUI.Button(new Rect(x,viewport.yMax-50,w,44),"선택한 호출 수락",button))AcceptSelected();GUI.enabled=true;
  }else DrawPlaceDetails(x,w,viewport);
  GUI.Label(new Rect(24,Screen.height-23,Screen.width-48,22),"© OpenStreetMap contributors / ODbL · NGII 기반 게임 지도",new GUIStyle(text){fontSize=11});
  if(!string.IsNullOrEmpty(GUI.tooltip)&&!locationOpen){Fill(new Rect(32,144,180,28),new Color(.04f,.1f,.14f,.95f));GUI.Label(new Rect(40,145,170,26),GUI.tooltip,text);}
  if(locationOpen){Fill(new Rect(0,0,Screen.width,Screen.height),new Color(0,0,0,.7f));var r=new Rect(Screen.width/2-240,Screen.height/2-150,480,300);Fill(r,new Color(.04f,.08f,.12f));GUI.BeginGroup(r);LocationPopup(94722);GUI.EndGroup();}
 }
 public void ShowLocation(){locationOpen=true;locationCopied=false;}
 public bool LocationOpen {get{return locationOpen;}}
 public void HideLocation(){locationOpen=false;}
 void LocationPopup(int id){GUI.Label(new Rect(20,8,440,24),"현재 위치",heading);GUI.TextArea(new Rect(20,35,440,170),LocationText,new GUIStyle(GUI.skin.textArea){font=KoreanText.Font,fontSize=15,wordWrap=true,normal={textColor=Color.white}});if(GUI.Button(new Rect(20,230,210,42),locationCopied?"복사됨":"좌표 복사",button)){GUIUtility.systemCopyBuffer=LocationText;locationCopied=true;}if(GUI.Button(new Rect(250,230,210,42),"닫기",button))locationOpen=false;}
 void StopPin(int stop,string caption,Color color,Rect map){var p=Project(session.jobs.stops[stop].V(),map);if(!map.Contains(p))return;var old=GUI.backgroundColor;GUI.backgroundColor=color;if(GUI.Button(new Rect(p.x-27,p.y+18,54,30),caption,button))FocusStop(stop);if(GUI.Button(new Rect(p.x-10,p.y-10,20,20),GUIContent.none))FocusStop(stop);GUI.backgroundColor=old;}
 static void Fill(Rect r,Color c){GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
 static void Dot(Vector2 p,Color c,float size){Fill(new Rect(p.x-size/2,p.y-size/2,size,size),c);}
 void Line(Vector2 a,Vector2 b,Color c,float width){HudGlyph.MapLine(a,b,c,width,new Rect(0,0,MapRect.width,MapRect.height));}
}
