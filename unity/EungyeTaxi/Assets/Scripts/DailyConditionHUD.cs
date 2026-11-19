using UnityEngine;

public static class DailyConditionHUD {
 const float SideWidth=286,SideCardHeight=286,CompactCardHeight=146,Gap=16;
 static GUIStyle clue,description,legend,summary,badge,closureLabel;

 public readonly struct PreparationLayout {
  public readonly float scale;
  public readonly Rect panel,card,sideGoal;
  public PreparationLayout(float width,float height,float panelWidth,float panelHeight,bool side){
   float contentWidth=panelWidth+(side?SideWidth+Gap:0);
   float contentHeight=side?Mathf.Max(panelHeight,SideCardHeight+Gap+255):panelHeight+CompactCardHeight+Gap;
   scale=Mathf.Min(1.18f,(width-24)/contentWidth,(height-24)/contentHeight);
   float x=(width/scale-contentWidth)*.5f,y=(height/scale-contentHeight)*.5f;
   panel=new Rect(x,y+(side?0:CompactCardHeight+Gap),panelWidth,panelHeight);
   card=side?new Rect(panel.xMax+Gap,y,SideWidth,SideCardHeight):new Rect(x,y,panelWidth,CompactCardHeight);
   sideGoal=side?new Rect(card.x,card.yMax+Gap,card.width,255):default;
  }
 }
 public static bool UseSide(float width,float height){return width>=1000&&width/height>=1.36f;}

 static void Styles(){
  if(clue!=null)return;
  KoreanText.PrepareSkin();
  clue=new GUIStyle(GUI.skin.label){font=KoreanText.BoldFont,fontSize=18,wordWrap=true};
  description=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=15,wordWrap=true};
  legend=new GUIStyle(description){fontSize=13,wordWrap=false};
  summary=new GUIStyle(description){fontSize=15,wordWrap=false};
  badge=new GUIStyle(summary){font=KoreanText.BoldFont};
  closureLabel=new GUIStyle(legend){font=KoreanText.BoldFont,alignment=TextAnchor.MiddleCenter,normal={textColor=Color.white}};
 }
 static Color Accent(DailyConditionKind kind){
  switch(kind){
   case DailyConditionKind.Rain:return CasualUI.Blue;
   case DailyConditionKind.Market:return CasualUI.Amber;
   case DailyConditionKind.Friday:return CasualUI.Green;
   case DailyConditionKind.Construction:return CasualUI.Danger;
   default:return CasualUI.Muted;
  }
 }
 static int Glyph(DailyConditionKind kind){
  switch(kind){
   case DailyConditionKind.Rain:return -1;
   case DailyConditionKind.Market:return 5;
   case DailyConditionKind.Friday:return 9;
   case DailyConditionKind.Construction:return 2;
   default:return 13;
  }
 }
 static void DrawGlyph(Rect r,DailyConditionKind kind,Color color){
  if(kind!=DailyConditionKind.Rain){HudGlyph.Draw(r,Glyph(kind),color);return;}
  var u=r.width/24f;
  HudGlyph.Draw(new Rect(r.x+2*u,r.y+5*u,11*u,11*u),15,color);
  HudGlyph.Draw(new Rect(r.x+7*u,r.y+u,14*u,14*u),15,color);
  HudGlyph.Draw(new Rect(r.x+14*u,r.y+6*u,10*u,10*u),15,color);
  for(int i=0;i<3;i++){
   float x=r.x+(6+i*6)*u;
   HudGlyph.MapLine(new Vector2(x+u,r.y+17*u),new Vector2(x-u,r.y+22*u),color,1.7f*u,r);
  }
 }
 public static void DrawPreparationCard(Rect r,DriveSession session){
  Styles();
  var kind=session.business.DayCondition;
  bool compact=r.height==CompactCardHeight;
  float mapWidth=compact?Mathf.Min(r.width*.45f,258):r.width-24;
  Rect map=new Rect(compact?r.xMax-mapWidth-12:r.x+12,compact?r.y+53:r.y+93,mapWidth,compact?80:125);
  CasualUI.Panel(r);
  var header=new Rect(r.x+8,r.y+8,r.width-16,37);
  CasualUI.Header(header,DailyConditions.Title(kind),Glyph(kind),Accent(kind));
  if(kind==DailyConditionKind.Rain)DrawGlyph(new Rect(header.x+14,header.center.y-14,28,28),kind,Color.white);
  float textWidth=compact?map.x-r.x-24:r.width-24;
  GUI.Label(new Rect(r.x+12,r.y+50,textWidth,39),DailyConditions.Clue(kind),clue);
  DrawDemandMap(map,session,kind);
  if(kind==DailyConditionKind.Friday&&DailyConditions.DemandMultiplier(kind,"commercial","residential",2)>1)
   DrawFridayLegend(new Rect(map.x+4,map.yMax,map.width-8,compact?13:19));
  GUI.Label(new Rect(r.x+12,r.y+(compact?89:240),textWidth,compact?51:40),DailyConditions.Description(kind),description);
 }
 static void DrawFridayLegend(Rect r){
  float y=r.center.y-3;
  HudGlyph.Draw(new Rect(r.x,y,6,6),15,CasualUI.Green);
  GUI.Label(new Rect(r.x+8,r.y,74,r.height),"상업 출발",legend);
  HudGlyph.Draw(new Rect(r.x+77,y,6,6),15,CasualUI.Blue);
  GUI.Label(new Rect(r.x+85,r.y,74,r.height),"주거 도착",legend);
 }

 static void DrawDemandMap(Rect r,DriveSession session,DailyConditionKind kind){
  if(Event.current.type!=EventType.Repaint)return;
  var world=session.world.map;
  var view=session.GetComponent<DriveHUD>();
  if(!view)return;
  CasualUI.RoundedFill(r,CasualUI.Soft);
  const float pad=5;
  float scale=Mathf.Min((r.width-2*pad)/world.width,(r.height-2*pad)/world.depth);
  var map=new Rect(r.center.x-world.width*scale*.5f,r.center.y-world.depth*scale*.5f,world.width*scale,world.depth*scale);
  GUI.DrawTexture(map,view.ConditionMapTile);
  Color accent=Accent(kind);
  bool allBoosted=DailyConditions.DemandMultiplier(kind,"neutral","neutral",2)>1;
  bool eveningCommercial=DailyConditions.DemandMultiplier(kind,"commercial","residential",2)>1;
  foreach(var entry in session.jobs.Catalog.Entries){
   string region=PassengerDemandPolicy.Region(entry.StopId);
   bool boosted=allBoosted||eveningCommercial&&region=="commercial";
   bool destination=eveningCommercial&&kind==DailyConditionKind.Friday&&region=="residential";
   if(region=="neutral"&&!boosted)continue;
   var p=Project(entry.Stop.V(),world,map);
   float size=boosted||destination?9:4;
   HudGlyph.Draw(new Rect(p.x-size*.5f,p.y-size*.5f,size,size),15,boosted?accent:destination?CasualUI.Blue:CasualUI.Muted);
  }
  if(kind==DailyConditionKind.Construction)DrawClosure(session,map,p=>Project(p,world,map),4,false);
 }
 static Vector2 Project(Vector3 p,MapData world,Rect map){return new Vector2(map.x+p.x/world.width*map.width,map.yMax-p.z/world.depth*map.height);}

 public static void DrawBadge(Rect r,DailyConditionKind kind){
  Styles();
  badge.fontSize=UiReadability.Font(12);
  var accent=Accent(kind);
  CasualUI.RoundedFill(r,CasualUI.Soft);
  DrawGlyph(new Rect(r.x+7,r.center.y-8,16,16),kind,accent);
  string label=DailyConditions.Title(kind);
  float width=r.width-33;
  GUI.Label(new Rect(r.x+27,r.y,width,r.height),new GUIContent(UiReadability.Ellipsis(label,badge,width),DailyConditions.Clue(kind)),badge);
 }

 public static string Reflection(DailyConditionKind kind,DayRecord record,float? previousWeekdayRides){
  string baseline=previousWeekdayRides.HasValue?" (평일 평균 "+previousWeekdayRides.Value.ToString("0.#")+")":"";
  return DailyConditions.Title(kind)+" · 운송 "+record.rides+"건"+baseline+" · 세차 "+record.wash.ToString("N0")+"원";
 }
 public static void DrawReflection(Rect r,BusinessProgress b,DayRecord record){
  Styles();
  CasualUI.RoundedFill(r,CasualUI.Soft);
  var kind=b.DayCondition;
  DrawGlyph(new Rect(r.x+9,r.center.y-8,16,16),kind,Accent(kind));
  GUI.Label(new Rect(r.x+33,r.y,r.width-42,r.height),Reflection(kind,record,b.PreviousWeekdayAverageRides),summary);
 }
 public static void DrawClosure(DriveSession session,Rect map,System.Func<Vector3,Vector2> project,float width,bool caption){
  if(Event.current.type!=EventType.Repaint)return;
  var points=DailyRoadConstruction.Preview(session);
  if(points.Length<2)return;
  var previous=project(points[0]);
  for(int i=1;i<points.Length;i++){
   var p=project(points[i]);
   HudGlyph.MapLine(previous,p,CasualUI.Card,width+4,map);
   previous=p;
  }
  previous=project(points[0]);
  for(int i=1;i<points.Length;i++){
   var p=project(points[i]);
   HudGlyph.MapLine(previous,p,CasualUI.Danger,width,map);
   previous=p;
  }
  var center=project(points[points.Length/2]);
  if(!map.Contains(center))return;
  if(caption){
   Styles();
   var label=new Rect(Mathf.Clamp(center.x-52,map.xMin+4,map.xMax-108),Mathf.Clamp(center.y-36,map.yMin+4,map.yMax-28),104,24);
   CasualUI.RoundedFill(label,CasualUI.Danger);
   GUI.Label(label,"공사 · 차로 통제",closureLabel);
  }else HudGlyph.DrawBadge(new Rect(center.x-8,center.y-8,16,16),2,CasualUI.Danger);
 }
}
