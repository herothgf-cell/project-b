using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// One modal shares the real navigation path and situation definitions with the minimap.
public class FullMapOverlay : MonoBehaviour {
 public DriveSession session;public bool IsOpen {get;private set;}
 bool locationCopied;
 public static string DescribeLocation(MapData map,Vector3 position,float heading,IEnumerable<TrafficJunction> junctions){
  TrafficJunction nearest=null;float distance=float.MaxValue;
  foreach(var junction in junctions){var delta=junction.center-position;delta.y=0;float d=delta.magnitude;if(d<distance){nearest=junction;distance=d;}}
  string id=string.IsNullOrEmpty(map.mapId)?(HdRouteTrial.Enabled?"route42":"unknown"):map.mapId;
  return "지역: "+(id=="unknown"?"미지정":MapCatalog.Name(id))+" ("+id+")\n지도 좌표(m): x="+position.x.ToString("R",CultureInfo.InvariantCulture)+", z="+position.z.ToString("R",CultureInfo.InvariantCulture)+
   "\n방향(도, 북=0 / 동=90): "+Mathf.Repeat(heading,360).ToString("R",CultureInfo.InvariantCulture)+
   "\n가까운 교차로: "+(nearest==null?"없음":nearest.name+" ("+distance.ToString("0.0",CultureInfo.InvariantCulture)+"m)");
 }
 public string LocationText {get{return DescribeLocation(session.world.map,session.taxi.transform.position,session.taxi.transform.eulerAngles.y,session.traffic.junctions);}}
 public bool ShowFacilities=true,ShowTraffic=true,ShowLaw=true;
 public sealed class MapMarker {public Vector3 position;public string label;public Color color;public bool named;}
 public List<MapMarker> VisibleMarkers(){
  var markers=new List<MapMarker>();
  if(ShowFacilities){int i=0;foreach(var station in FuelStationLayout.For(session.world.map))markers.Add(new MapMarker{position=station.pump,label="주유 "+(++i),color=new Color(1,.75f,.15f),named=true});
   var rest=FuelStationLayout.Rest(session.world.map);if(rest!=null)markers.Add(new MapMarker{position=rest.pump,label="휴식 주차",color=Color.cyan,named=true});
   if(session.business.useV2)for(int k=1;k<session.services.indices.Length;k++)markers.Add(new MapMarker{position=session.jobs.stops[session.services.indices[k]].V(),label=k==1?"식당":"주차장",color=new Color(.9f,.6f,1),named=true});}
  if(ShowTraffic){foreach(var car in session.traffic.cars)if(car.Visible)markers.Add(new MapMarker{position=car.transform.position,label=car.speed<.2f?"대기 차량":"이동 차량",color=car.speed<.2f?new Color(1,.65f,.2f):new Color(.65f,.8f,.9f)});
   foreach(var incident in session.roadConditions.incidents)markers.Add(new MapMarker{position=incident.position,label="정체·정차",color=Color.red,named=true});}
  return markers;
 }
 public List<ServiceLawZone> VisibleLawZones(){var result=new List<ServiceLawZone>();var zones=GetComponent<ServiceLawZones>();if(ShowLaw&&zones&&zones.Ready)result.AddRange(zones.zones);return result;}
 public float ZoomLevel {get;private set;}=1;Vector2 center=new Vector2(.5f,.5f);bool dragging;
 public void Zoom(int step){ZoomLevel=Mathf.Clamp(ZoomLevel*Mathf.Pow(1.5f,step),1,8);ClampCenter();}
 public void ResetView(){ZoomLevel=1;center=new Vector2(.5f,.5f);}
 public void FocusPlayer(){float span=Mathf.Max(session.world.map.width,session.world.map.depth);center=new Vector2(.5f+(session.taxi.transform.position.x-session.world.map.width/2)/span,.5f-(session.taxi.transform.position.z-session.world.map.depth/2)/span);ClampCenter();}
 public void Pan(Vector2 pixels){center-=pixels/(MapRect.width*ZoomLevel);ClampCenter();}
 void ClampCenter(){float half=.5f/ZoomLevel;center=new Vector2(Mathf.Clamp(center.x,half,1-half),Mathf.Clamp(center.y,half,1-half));}
 Vector2 filterScroll;bool previousPause;float previousScale;int closedFrame=-1;GUIStyle text,heading,button;
 public bool ClosedThisFrame {get{return closedFrame==Time.frameCount;}}
 public Rect MapRect {get{return new Rect(28,86,Mathf.Min(Screen.height-158,Screen.width-350),Mathf.Min(Screen.height-158,Screen.width-350));}}
 public bool Open(){
  if(IsOpen)return false;
  var b=session.business;
  if(session.ChoiceOpen||session.PracticeMenuOpen||session.PracticeBusy||b.preparing||b.ShowingSummary||b.restartPrompt||b.pendingPayment>=0||b.taxPrompt||session.services.open||session.law.HistoryOpen)return false;
  previousPause=session.paused;previousScale=Time.timeScale;session.paused=true;Time.timeScale=0;locationCopied=false;IsOpen=true;return true;
 }
 public void Close(){if(!IsOpen)return;IsOpen=false;dragging=false;closedFrame=Time.frameCount;session.paused=previousPause;Time.timeScale=previousScale;}
 void Update(){if(IsOpen){if(Input.GetKeyDown(KeyCode.M)||Input.GetKeyDown(KeyCode.Escape))Close();}else if(Input.GetKeyDown(KeyCode.M))Open();}
 void OnDisable(){if(IsOpen)Close();}
 Vector2 Project(Vector3 p,Rect map){float span=Mathf.Max(session.world.map.width,session.world.map.depth);var unit=new Vector2(.5f+(p.x-session.world.map.width/2)/span,.5f-(p.z-session.world.map.depth/2)/span);return map.center+(unit-center)*map.width*ZoomLevel;}
 void OnGUI(){if(!IsOpen)return;if(text==null){text=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=15,wordWrap=true};heading=new GUIStyle(text){fontSize=22,fontStyle=FontStyle.Bold};button=KoreanText.Button(14);text.normal.textColor=heading.normal.textColor=Color.white;}GUI.depth=-220;GUI.ModalWindow(94721,new Rect(0,0,Screen.width,Screen.height),Draw,GUIContent.none,GUIStyle.none);}
 void Draw(int id){
  Fill(new Rect(0,0,Screen.width,Screen.height),new Color(.025f,.045f,.055f,.99f));
  GUI.Label(new Rect(28,18,Screen.width-240,35),MapCatalog.Name(session.world.map.mapId)+" 전체 지도 · 북쪽 ↑",heading);
  if(GUI.Button(new Rect(Screen.width-190,20,160,36),"M / Esc 닫기",button)){Close();return;}
  GUI.Label(new Rect(28,55,Screen.width-56,26),"지도를 보는 동안 영업시간과 주행이 멈춥니다.  ▲ 내 위치   청록선: 실제 내비 경로   초록: 승차   분홍: 도착·이번 상황",text);
  var viewport=MapRect;var ev=Event.current;
  if(ev.type==EventType.ScrollWheel&&viewport.Contains(ev.mousePosition)){Zoom(ev.delta.y<0?1:-1);ev.Use();}
  if(ev.type==EventType.KeyDown&&(ev.keyCode==KeyCode.Equals||ev.keyCode==KeyCode.Plus||ev.keyCode==KeyCode.KeypadPlus||ev.keyCode==KeyCode.Minus||ev.keyCode==KeyCode.KeypadMinus)){Zoom(ev.keyCode==KeyCode.Minus||ev.keyCode==KeyCode.KeypadMinus?-1:1);ev.Use();}
  if(ev.type==EventType.MouseDown&&ev.button==0&&viewport.Contains(ev.mousePosition)){dragging=true;ev.Use();}
  if(ev.type==EventType.MouseDrag&&dragging){Pan(ev.delta);ev.Use();}if(ev.type==EventType.MouseUp)dragging=false;
  GUI.BeginGroup(viewport);var map=new Rect(0,0,viewport.width,viewport.height);Fill(map,new Color(.10f,.15f,.17f));var data=session.world.map;var tile=GetComponent<DriveHUD>().tile;
  if(tile){var a=Project(new Vector3(0,0,data.depth),map);float span=Mathf.Max(data.width,data.depth);GUI.DrawTexture(new Rect(a.x,a.y,data.width/span*map.width*ZoomLevel,data.depth/span*map.height*ZoomLevel),tile);}
  var jobs=session.jobs;
  if(jobs.nav.HasDestination)for(int pass=0;pass<2;pass++)for(int i=1;i<jobs.nav.path.Count;i++)Line(Project(jobs.nav.path[i-1],map),Project(jobs.nav.path[i],map),pass==0?Color.black:Color.cyan,pass==0?7:3);
  var occupied=new List<Rect>();var player=Project(session.taxi.transform.position,map);occupied.Add(new Rect(player.x-18,player.y-18,36,36));
  if(jobs.Active){if(jobs.trip.State==TripState.ToPickup)Target(jobs.stops[jobs.current.pickup].V(),"승차",new Color(.3f,1,.55f),map,occupied);Target(jobs.stops[jobs.current.drop].V(),"도착",new Color(1,.5f,.8f),map,occupied);}
  else if(jobs.nav.HasDestination)Target(jobs.nav.destination,"목표",Color.cyan,map,occupied);
  var zones=GetComponent<ServiceLawZones>();var selected=jobs.Active?zones.ZoneFor(jobs.current):null;
  var nameStyle=new GUIStyle(text){fontSize=13,alignment=TextAnchor.MiddleCenter};
  foreach(var zone in VisibleLawZones()){
   var anchor=Project(zone.junction.center,map);if(!map.Contains(anchor))continue;var box=LawZoneMapLayout.Place(anchor,new Vector2(146,42),new Rect(map.x+3,map.y+3,map.width-6,map.height-6),occupied);occupied.Add(box);
   var color=zone==selected?new Color(1,.55f,.9f):new Color(1,.8f,.3f);Line(anchor,new Vector2(Mathf.Clamp(anchor.x,box.xMin,box.xMax),Mathf.Clamp(anchor.y,box.yMin,box.yMax)),color,2);Fill(box,new Color(.035f,.05f,.065f,.95f));nameStyle.normal.textColor=color;GUI.Label(box,zone.MapTitle,nameStyle);Dot(anchor,color,7);
  }

  foreach(var marker in VisibleMarkers()){
   var anchor=Project(marker.position,map);if(!map.Contains(anchor))continue;Dot(anchor,Color.black,marker.named?13:7);Dot(anchor,marker.color,marker.named?9:4);
   if(marker.named){var box=LawZoneMapLayout.Place(anchor,new Vector2(82,26),map,occupied);occupied.Add(box);Line(anchor,new Vector2(Mathf.Clamp(anchor.x,box.xMin,box.xMax),Mathf.Clamp(anchor.y,box.yMin,box.yMax)),marker.color,1);Fill(box,Color.black);GUI.Label(box,marker.label,text);}
  }
  var forward=new Vector2(session.taxi.transform.forward.x,-session.taxi.transform.forward.z).normalized;var side=new Vector2(-forward.y,forward.x);Dot(player,Color.black,26);Line(player-forward*7-side*8,player+forward*12,Color.yellow,5);Line(player+forward*12,player-forward*7+side*8,Color.yellow,5);Line(player-forward*7+side*8,player-forward*7-side*8,Color.yellow,5);
  GUI.EndGroup();float x=viewport.xMax+24,w=Screen.width-x-28,y=86;
  filterScroll=GUI.BeginScrollView(new Rect(x,y,w,map.height),filterScroll,new Rect(0,0,w-20,1100));x=0;y=0;w-=20;
  GUI.Label(new Rect(x,y,w,30),"현재 위치 · 문의용 좌표",heading);y+=34;
  var location=LocationText;var locationStyle=new GUIStyle(GUI.skin.textArea){font=KoreanText.Font,fontSize=14,wordWrap=true,padding=new RectOffset(7,7,6,6)};
  float locationHeight=Mathf.Max(112,locationStyle.CalcHeight(new GUIContent(location),w));
  GUI.TextArea(new Rect(x,y,w,locationHeight),location,locationStyle);y+=locationHeight+6;
  if(GUI.Button(new Rect(x,y,w,30),locationCopied?"현재 위치 복사됨":"현재 위치 복사",button)){GUIUtility.systemCopyBuffer=location;locationCopied=true;}y+=36;
  GUI.Label(new Rect(x,y,w,28),"확대 "+ZoomLevel.ToString("0.0")+"배 · 휠 / +/-",text);y+=30;
  if(GUI.Button(new Rect(x,y,w/2-3,30),"+ 확대",button))Zoom(1);if(GUI.Button(new Rect(x+w/2+3,y,w/2-3,30),"− 축소",button))Zoom(-1);y+=36;
  if(GUI.Button(new Rect(x,y,w/2-3,30),"내 위치",button))FocusPlayer();if(GUI.Button(new Rect(x+w/2+3,y,w/2-3,30),"전체 보기",button))ResetView();y+=36;
  GUI.Label(new Rect(x,y,w,26),"지도를 드래그해서 이동",text);y+=30;
  GUI.Label(new Rect(x,y,w,32),"지도 표시 필터",heading);y+=39;
  ShowFacilities=Filter(new Rect(x,y,w,32),ShowFacilities,"주요 시설 · 주유소 / 식당 / 주차장");y+=38;
  ShowTraffic=Filter(new Rect(x,y,w,32),ShowTraffic,"교통 상황 · 차량 / 대기 / 정체");y+=38;
  ShowLaw=Filter(new Rect(x,y,w,32),ShowLaw,"교통 법규 · 전체 상황 구역");y+=43;
  GUI.Label(new Rect(x,y,w,40),"승차·도착·내 위치·내비 경로는 항상 표시",text);y+=43;
  if(ShowTraffic){GUI.Label(new Rect(x,y,w,38),"하늘색: 이동 차량 · 주황: 대기 차량\n빨강: 정체·정차 상황",text);y+=44;}
  if(ShowLaw){GUI.Label(new Rect(x,y,w,26),"법규 상황 · 창작 교육 구역",text);y+=30;foreach(var z in VisibleLawZones()){var row=new Rect(x,y,w,28);Fill(row,z==selected?new Color(.27f,.08f,.23f):new Color(.07f,.10f,.12f));GUI.Label(new Rect(x+6,y,w-12,28),(z==selected?"상황 구역 · ":"")+z.Title,new GUIStyle(text){fontSize=13});y+=30;}}
  if(ShowFacilities){GUI.Label(new Rect(x,y+6,w,45),"주유소 1·2·3: 게임시설\nF7에서 입구까지 길 안내 선택",new GUIStyle(text){fontSize=13});}
  GUI.EndScrollView();
  GUI.Label(new Rect(28,Screen.height-54,Screen.width-56,45),"법규 구역은 은계 지도에 배치한 창작 상황입니다. 시설·현장 규제 확인과 구분합니다.  © OpenStreetMap contributors / ODbL",text);
 }
 bool Filter(Rect rect,bool active,string caption){if(GUI.Button(rect,(active?"[켜짐] ":"[꺼짐] ")+caption,button))return !active;return active;}
 void Target(Vector3 world,string title,Color color,Rect map,List<Rect> occupied){var p=Project(world,map);if(!map.Contains(p))return;Dot(p,Color.black,17);Dot(p,color,11);var box=LawZoneMapLayout.Place(p,new Vector2(66,26),map,occupied);Fill(box,Color.black);GUI.Label(box,title,text);occupied.Add(new Rect(p.x-10,p.y-10,20,20));occupied.Add(box);}
 static void Fill(Rect r,Color c){GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
 static void Dot(Vector2 p,Color c,float size){Fill(new Rect(p.x-size/2,p.y-size/2,size,size),c);}
 static void Line(Vector2 a,Vector2 b,Color c,float width){var old=GUI.matrix;GUI.color=c;GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a);GUI.DrawTexture(new Rect(a.x,a.y-width/2,Vector2.Distance(a,b),width),Texture2D.whiteTexture);GUI.matrix=old;GUI.color=Color.white;}
}
