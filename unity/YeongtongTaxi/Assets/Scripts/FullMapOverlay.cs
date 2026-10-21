using System.Collections.Generic;
using UnityEngine;

// One modal shares the real navigation path and situation definitions with the minimap.
public class FullMapOverlay : MonoBehaviour {
 public DriveSession session;public bool IsOpen {get;private set;}
 bool previousPause;float previousScale;int closedFrame=-1;GUIStyle text,heading,button;
 public bool ClosedThisFrame {get{return closedFrame==Time.frameCount;}}
 public Rect MapRect {get{return new Rect(28,86,Mathf.Min(Screen.height-158,Screen.width-350),Mathf.Min(Screen.height-158,Screen.width-350));}}
 public bool Open(){
  if(IsOpen)return false;
  var b=session.business;
  if(session.PracticeMenuOpen||session.PracticeBusy||b.preparing||b.ShowingSummary||b.restartPrompt||b.pendingPayment>=0||b.taxPrompt||session.services.open||session.law.HistoryOpen)return false;
  previousPause=session.paused;previousScale=Time.timeScale;session.paused=true;Time.timeScale=0;IsOpen=true;return true;
 }
 public void Close(){if(!IsOpen)return;IsOpen=false;closedFrame=Time.frameCount;session.paused=previousPause;Time.timeScale=previousScale;}
 void Update(){if(IsOpen){if(Input.GetKeyDown(KeyCode.G)||Input.GetKeyDown(KeyCode.Escape))Close();}else if(Input.GetKeyDown(KeyCode.G))Open();}
 void OnDisable(){if(IsOpen)Close();}
 Vector2 Project(Vector3 p,Rect map){float span=Mathf.Max(session.world.map.width,session.world.map.depth);return new Vector2(map.x+(p.x-(session.world.map.width-span)/2)/span*map.width,map.y+(1-(p.z-(session.world.map.depth-span)/2)/span)*map.height);}
 void OnGUI(){if(!IsOpen)return;if(text==null){text=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=15,wordWrap=true};heading=new GUIStyle(text){fontSize=22,fontStyle=FontStyle.Bold};button=KoreanText.Button(14);text.normal.textColor=heading.normal.textColor=Color.white;}GUI.depth=-220;GUI.ModalWindow(94721,new Rect(0,0,Screen.width,Screen.height),Draw,GUIContent.none,GUIStyle.none);}
 void Draw(int id){
  Fill(new Rect(0,0,Screen.width,Screen.height),new Color(.025f,.045f,.055f,.99f));
  GUI.Label(new Rect(28,18,Screen.width-240,35),"은계 전체 지도 · 북쪽 ↑",heading);
  if(GUI.Button(new Rect(Screen.width-190,20,160,36),"G / Esc 닫기",button)){Close();return;}
  GUI.Label(new Rect(28,55,Screen.width-56,26),"지도를 보는 동안 영업시간과 주행이 멈춥니다.  ▲ 내 위치   청록선: 실제 내비 경로   초록: 승차   분홍: 도착·이번 상황",text);
  var map=MapRect;Fill(map,new Color(.10f,.15f,.17f));var data=session.world.map;var tile=GetComponent<DriveHUD>().tile;
  if(tile){var a=Project(new Vector3(0,0,data.depth),map);float span=Mathf.Max(data.width,data.depth);GUI.DrawTexture(new Rect(a.x,a.y,data.width/span*map.width,data.depth/span*map.height),tile);}
  var jobs=session.jobs;
  for(int pass=0;pass<2;pass++)for(int i=1;i<jobs.nav.path.Count;i++)Line(Project(jobs.nav.path[i-1],map),Project(jobs.nav.path[i],map),pass==0?Color.black:Color.cyan,pass==0?7:3);
  var occupied=new List<Rect>();var player=Project(session.taxi.transform.position,map);occupied.Add(new Rect(player.x-18,player.y-18,36,36));
  if(jobs.Active){if(jobs.trip.State==TripState.ToPickup)Target(jobs.stops[jobs.current.pickup].V(),"승차",new Color(.3f,1,.55f),map,occupied);Target(jobs.stops[jobs.current.drop].V(),"도착",new Color(1,.5f,.8f),map,occupied);}
  else if(jobs.nav.active)Target(jobs.nav.destination,"목표",Color.cyan,map,occupied);
  var zones=GetComponent<ServiceLawZones>();var selected=jobs.Active?zones.ZoneFor(jobs.current):null;
  var nameStyle=new GUIStyle(text){fontSize=13,alignment=TextAnchor.MiddleCenter};
  if(zones.Ready)foreach(var zone in zones.zones){
   var anchor=Project(zone.junction.center,map);var box=LawZoneMapLayout.Place(anchor,new Vector2(146,42),new Rect(map.x+3,map.y+3,map.width-6,map.height-6),occupied);occupied.Add(box);
   var color=zone==selected?new Color(1,.55f,.9f):new Color(1,.8f,.3f);Line(anchor,new Vector2(Mathf.Clamp(anchor.x,box.xMin,box.xMax),Mathf.Clamp(anchor.y,box.yMin,box.yMax)),color,2);Fill(box,new Color(.035f,.05f,.065f,.95f));nameStyle.normal.textColor=color;GUI.Label(box,zone.MapTitle,nameStyle);Dot(anchor,color,7);
  }
  var forward=new Vector2(session.taxi.transform.forward.x,-session.taxi.transform.forward.z).normalized;var side=new Vector2(-forward.y,forward.x);Dot(player,Color.black,26);Line(player-forward*7-side*8,player+forward*12,Color.yellow,5);Line(player+forward*12,player-forward*7+side*8,Color.yellow,5);Line(player-forward*7+side*8,player-forward*7-side*8,Color.yellow,5);
  float x=map.xMax+24,w=Screen.width-x-28,y=86;
  GUI.Label(new Rect(x,y,w,33),"법규 상황 구역",heading);y+=39;
  if(zones.Ready)foreach(var z in zones.zones){bool chosen=z==selected;Fill(new Rect(x,y,w,53),chosen?new Color(.27f,.08f,.23f):new Color(.07f,.10f,.12f));var s=new GUIStyle(text){fontSize=14};s.normal.textColor=chosen?new Color(1,.65f,.93f):Color.white;GUI.Label(new Rect(x+9,y+5,w-18,45),(chosen?"이번 경유 · ":"")+z.Title+"\n창작 교육 상황",s);y+=59;}
  var stations=FuelStationLayout.For(data);if(stations.Count>0){
   GUI.Label(new Rect(x,y+4,w,24),"주유소 · 게임시설 / 입구까지 내비 안내",text);
   for(int i=0;i<stations.Count;i++){
    var station=stations[i];var anchor=Project(station.pump,map);var box=LawZoneMapLayout.Place(anchor,new Vector2(82,26),map,occupied);occupied.Add(box);Line(anchor,new Vector2(Mathf.Clamp(anchor.x,box.xMin,box.xMax),Mathf.Clamp(anchor.y,box.yMin,box.yMax)),new Color(1,.75f,.15f),2);Dot(anchor,new Color(1,.75f,.15f),9);Fill(box,Color.black);GUI.Label(box,"주유 "+(i+1),text);GUI.Label(new Rect(x,y+31+i*25,w,25),(i+1)+" · "+station.name.Replace(" (게임시설)",""),text);
   }
  }else GUI.Label(new Rect(x,Mathf.Max(y+10,map.yMax-48),w,60),jobs.nav.active?"현재 경로: "+jobs.nav.remaining.ToString("0")+"m · "+jobs.nav.PreferenceLabel:"호출을 선택하면 승차·도착과 경유 경로가 표시됩니다.",text);
  GUI.Label(new Rect(28,Screen.height-54,Screen.width-56,45),"법규 구역은 은계 지도에 배치한 창작 상황입니다. 시설·현장 규제 확인과 구분합니다.  © OpenStreetMap contributors / ODbL",text);
 }
 void Target(Vector3 world,string title,Color color,Rect map,List<Rect> occupied){var p=Project(world,map);Dot(p,Color.black,17);Dot(p,color,11);var box=LawZoneMapLayout.Place(p,new Vector2(66,26),map,occupied);Fill(box,Color.black);GUI.Label(box,title,text);occupied.Add(new Rect(p.x-10,p.y-10,20,20));occupied.Add(box);}
 static void Fill(Rect r,Color c){GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
 static void Dot(Vector2 p,Color c,float size){Fill(new Rect(p.x-size/2,p.y-size/2,size,size),c);}
 static void Line(Vector2 a,Vector2 b,Color c,float width){var old=GUI.matrix;GUI.color=c;GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a);GUI.DrawTexture(new Rect(a.x,a.y-width/2,Vector2.Distance(a,b),width),Texture2D.whiteTexture);GUI.matrix=old;GUI.color=Color.white;}
}
