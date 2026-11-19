using System.Collections.Generic;
using UnityEngine;

public static class LogbookNeighborhood {
 static readonly string[] Phases={"아침","낮","저녁"};
 static readonly string[] Stars={"☆☆☆","★☆☆","★★☆","★★★"};
 static readonly string[] StopIds=CreateStopIds();
 static GUIStyle heading,text,small,button,selected,marker;

 static string[] CreateStopIds(){
  var ids=new string[CampaignLogbook.NeighborhoodSize];
  for(int i=0;i<ids.Length;i++)ids[i]="EG-GAME-"+(i+1).ToString("00");
  return ids;
 }

 static void Styles(){
  if(heading!=null)return;
  heading=new GUIStyle(GUI.skin.label){font=KoreanText.BoldFont,fontSize=23,wordWrap=true};
  text=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=17,wordWrap=true};
  small=new GUIStyle(text){fontSize=15,normal={textColor=CasualUI.Muted}};
  button=KoreanText.Button(16);button.alignment=TextAnchor.MiddleLeft;button.padding=new RectOffset(12,12,0,0);
  selected=CasualUI.Action(16);selected.alignment=button.alignment;selected.padding=button.padding;
  marker=new GUIStyle(small){font=KoreanText.BoldFont,fontSize=14,alignment=TextAnchor.MiddleCenter,wordWrap=false};
 }

 public static void Draw(DriveSession session,ref int selectedStop){
  Styles();
  var logbook=session.business.Logbook;
  var catalog=session.jobs.Catalog;
  for(int i=0;i<StopIds.Length;i++){
   string id=StopIds[i];
   int stars=logbook.NeighborhoodStars(id);
   var stop=FindStop(catalog,id);
   string name=stars>0&&stop!=null?KoreanText.Stop(stop.name):UnknownStopName(i);
   string caption=name+"  ·  "+Stars[stars]+"  ·  "+logbook.NeighborhoodCount(id)+"회";
   var row=new Rect(15,89+i*49,330,41);
   if(GUI.Button(row,caption,i==selectedStop?selected:button))selectedStop=i;
  }
  DrawDetail(session,catalog,logbook,selectedStop);
 }

 static string UnknownStopName(int index){return "영업 지점 "+(index+1).ToString("00");}
 static MapStop FindStop(PassengerStopCatalog catalog,string stopId){
  int index=catalog.IndexOf(stopId);
  return index<0?null:catalog.Stops[index];
 }

 static void DrawDetail(DriveSession session,PassengerStopCatalog catalog,CampaignLogbook logbook,int index){
  var panel=new Rect(358,89,487,605);
  CasualUI.Panel(panel);
  string id=StopIds[index];
  int count=logbook.NeighborhoodCount(id),stars=logbook.NeighborhoodStars(id);
  var stop=FindStop(catalog,id);
  string name=stars>0&&stop!=null?KoreanText.Stop(stop.name):UnknownStopName(index);
  GUI.Label(new Rect(377,103,449,40),name+"  "+Stars[stars],heading);
  bool hasNext=stars<Stars.Length-1;
  int next=CampaignLogbook.NeighborhoodThreshold(stars+1);
  string progress=hasNext?count+" / "+next+"회":count+"회";
  GUI.Label(new Rect(377,150,449,29),"승차·하차 누적 "+progress,text);
  if(hasNext)CasualUI.Bar(new Rect(377,187,449,8),(float)count/next,CasualUI.Green);
  if(stars==0||stop==null){
   var lockPanel=new Rect(377,213,449,210);
   CasualUI.RoundedFill(lockPanel,CasualUI.Soft);
   HudGlyph.DrawBadge(new Rect(lockPanel.center.x-28,lockPanel.y+48,56,56),3,CasualUI.Muted);
   GUI.Label(new Rect(lockPanel.x+15,lockPanel.y+120,lockPanel.width-30,42),CampaignLogbook.NeighborhoodThreshold(1)+"회 운송 기록부터 지점과 지도를 볼 수 있어요.",marker);
  }else{
   FullMapOverlay.Place wait=null;
   if(stars>=2)wait=WaitingPlace(stop,session.GetComponent<FullMapOverlay>().Places());
   DrawMap(new Rect(377,214,449,240),session,stop,wait);
   if(wait!=null){
    int metres=Mathf.RoundToInt(PlanarDistance(stop.V(),wait.position));
    GUI.Label(new Rect(377,465,449,40),"대기 장소 · "+wait.name+" (직선 "+metres.ToString("N0")+"m)",text);
   }else if(stars>=2)GUI.Label(new Rect(377,465,449,40),"등록된 대기 장소 없음",text);
   if(stars==3)GUI.Label(new Rect(377,513,449,51),DemandHint(id),text);
  }
  CasualUI.Divider(new Rect(377,580,449,1));
  GUI.Label(new Rect(377,589,449,80),"★ "+CampaignLogbook.NeighborhoodThreshold(1)+"회 · 지점과 지도\n★★ "+CampaignLogbook.NeighborhoodThreshold(2)+"회 · 대기 장소\n★★★ "+CampaignLogbook.NeighborhoodThreshold(3)+"회 · 시간대별 수요",small);
 }

 static float PlanarDistance(Vector3 a,Vector3 b){
  return Vector2.Distance(new Vector2(a.x,a.z),new Vector2(b.x,b.z));
 }

 static FullMapOverlay.Place WaitingPlace(MapStop stop,IEnumerable<FullMapOverlay.Place> places){
  FullMapOverlay.Place best=null;
  float distance=float.PositiveInfinity;
  foreach(var place in places){
   if(place.approach==null||!place.home&&place.glyph!=10&&place.glyph!=11)continue;
   float metres=PlanarDistance(stop.V(),place.position);
   if(metres<distance){best=place;distance=metres;}
  }
  return best;
 }

 public static HashSet<FullMapOverlay.Place> WaitingPlaces(DriveSession session,IEnumerable<FullMapOverlay.Place> facilityPlaces){
  if(!session.business.SupportsCareer||session.business.Logbook.StarTotal<2)return null;
  var visible=new HashSet<FullMapOverlay.Place>();
  foreach(string id in StopIds){
   if(session.business.Logbook.NeighborhoodStars(id)<2)continue;
   var stop=FindStop(session.jobs.Catalog,id);
   if(stop==null)continue;
   var place=WaitingPlace(stop,facilityPlaces);
   if(place!=null)visible.Add(place);
  }
  return visible;
 }

 static int StrongestPhase(string region,bool arrival){
  int best=0;
  for(int phase=1;phase<Phases.Length;phase++)
   if(PassengerDemandPolicy.Weight(region,phase,arrival)>PassengerDemandPolicy.Weight(region,best,arrival))best=phase;
  return best;
 }

 public static string DemandHint(string id){
  string region=PassengerDemandPolicy.Region(id);
  return PassengerDemandPolicy.Label(region)+" 지역 · "+Phases[StrongestPhase(region,false)]+" 출발과 "+Phases[StrongestPhase(region,true)]+" 도착 수요가 많습니다.";
 }

 static void DrawMap(Rect view,DriveSession session,MapStop stop,FullMapOverlay.Place wait){
  if(Event.current.type!=EventType.Repaint)return;
  var world=session.world.map;
  var atlas=session.GetComponent<DriveHUD>().LightTile;
  if(!atlas)return;
  Vector3 a=stop.V(),b=wait?.position??a;
  var region=Crop(world,a,b);
  float scale=Mathf.Min((view.width-12)/region.width,(view.height-12)/region.height);
  var map=new Rect(view.center.x-region.width*scale*.5f,view.center.y-region.height*scale*.5f,region.width*scale,region.height*scale);
  CasualUI.RoundedFill(view,CasualUI.Soft);
  var uv=new Rect(region.x/world.width,region.y/world.depth,region.width/world.width,region.height/world.depth);
  GUI.DrawTextureWithTexCoords(map,atlas,uv);
  Vector2 pickup=ToMap(a,region,map);
  MapPresentation.Pin(pickup,CasualUI.Blue,18);
  if(wait!=null){
   var p=ToMap(b,region,map);
   HudGlyph.DrawBadge(new Rect(p.x-11,p.y-11,22,22),10,CasualUI.Amber);
  }
  var north=new Rect(view.xMax-39,view.y+4,36,25);
  CasualUI.RoundedFill(north,CasualUI.Card);
  GUI.Label(north,"북 ↑",marker);
 }

 public static Rect Crop(MapData map,Vector3 stop,Vector3 wait){
  float width=Mathf.Min(map.width,Mathf.Max(300,Mathf.Abs(stop.x-wait.x)+160));
  float height=Mathf.Min(map.depth,Mathf.Max(190,Mathf.Abs(stop.z-wait.z)+160));
  return new Rect(Mathf.Clamp((stop.x+wait.x-width)*.5f,0,map.width-width),
   Mathf.Clamp((stop.z+wait.z-height)*.5f,0,map.depth-height),width,height);
 }
 static Vector2 ToMap(Vector3 position,Rect region,Rect view){
  return new Vector2(view.x+(position.x-region.x)/region.width*view.width,view.yMax-(position.z-region.y)/region.height*view.height);
 }
}
