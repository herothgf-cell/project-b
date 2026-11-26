using UnityEngine;

// Play feedback 2026-11-26: a mission must be visible for the whole ride, not only as a 5-second alert.
// While a mission call is accepted (waiting to board) or aboard, a small panel under the signal timer shows the
// mission, its goal, live progress and the bonus. Presentation only; settlement stays in PassengerJobsMissions.
public sealed class MissionTracker : MonoBehaviour {
 public DriveSession session;
 public static bool Drawn=false;
 public bool Showing {get{var j=session?session.jobs:null;return j&&j.current!=null&&j.current.mission!=MissionRules.None&&(j.trip.State==TripState.OnTrip||j.trip.State==TripState.ToPickup)&&!session.IsSimulationBot;}}
 public string Progress {get{
  var j=session.jobs;var o=j.current;if(j.trip.State==TripState.ToPickup)return "손님이 타면 시작해요";
  if(o.mission==MissionRules.Express){float target=MissionRules.ExpressTarget(o);return "주행 "+Clock(o.missionDriveSeconds)+" / "+Clock(target)+(o.missionDriveSeconds>target?" · 넘음":"");}
  if(o.mission==MissionRules.Green)return "연속 통과 "+o.missionGreenRun+" / "+MissionRules.GreenCount+(o.missionGreenBest>=MissionRules.GreenCount?" · 달성":"");
  if(o.mission==MissionRules.Coffee){int harsh=session.manners?session.manners.TripHarsh:0;return harsh==0?"급가속·급제동 없음 · 유지 중":"급가속·급제동 "+harsh+"번 · 실패";}
  if(o.mission==MissionRules.Precise)return "하차 지점에 30cm 안으로 정차";
  if(o.mission==MissionRules.KnowRoad)return o.missionVoid?"실패 · 길안내 다시 켜짐":"주행 "+Mathf.RoundToInt(o.meterMetres)+"m / "+Mathf.RoundToInt(MissionRules.KnowRoadLimit(o))+"m 안";
  return "";}}
 // A lost-item errand (return to the owner / hand in at the police box) shows in the same place while no mission runs.
 public string LostErrand {get{var l=session?session.lostItems:null;if(!l)return "";var j=session.jobs;
  if(l.State==LostItems.Returning&&l.OwnerIndex>=0)return "분실물 · "+LostItemRules.Names[l.Item]+" 돌려주기 · "+KoreanText.Stop(j.stops[l.OwnerIndex].name)+" 정차 · 남은 "+Clock(l.OwnerSecondsLeft);
  if(l.State==LostItems.ToPolice&&LostItemRules.Police!=null)return "분실물 · "+LostItemRules.Police.name+"에 맡기기 · 앞 도로에 정차 (M 지도 방패 표시)";
  return "";}}
 void DrawLostErrand(){
  string text=LostErrand;if(text.Length==0)return;
  var previous=GUI.matrix;float scale=CockpitUI.Scale;GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
  try{float w=288,h=84,x=Screen.width/scale-312,y=542;GUI.depth=-5;CasualUI.Panel(new Rect(x,y,w,h),new Color(1f,.97f,.88f,.96f));HudGlyph.Draw(new Rect(x+10,y+10,24,24),21,CasualUI.Blue);
   KoreanText.PrepareSkin();var line=new GUIStyle(GUI.skin.label){font=KoreanText.BoldFont,fontSize=UiReadability.Font(14),alignment=TextAnchor.MiddleLeft,wordWrap=true};line.normal.textColor=new Color(.25f,.18f,.05f);
   GUI.Label(new Rect(x+42,y+2,w-50,h-4),text,line);}finally{GUI.matrix=previous;}
 }
 static string Clock(float s){int t=Mathf.Max(0,Mathf.RoundToInt(s));return t/60+":"+(t%60).ToString("00");}
 void OnGUI(){
  // Play feedback 2026-11-26 (2nd): a separate panel makes the driving screen too busy. Drawing is off until the
  // mission line moves into the navigation card (draft under review); Progress / LostErrand stay for that card.
  if(!Drawn)return;
  if(session.paused||session.FullMapOpen||session.IsSimulationBot)return;
  if(!Showing){DrawLostErrand();return;}var o=session.jobs.current;
  var previous=GUI.matrix;float scale=CockpitUI.Scale;GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
  try{
   float w=288,h=104,x=Screen.width/scale-312,y=542; // right column, under the destination card (CockpitUI.DrawJob 382+150)GUI.depth=-5;
   CasualUI.Panel(new Rect(x,y,w,h),new Color(.93f,.96f,1f,.96f));
   HudGlyph.Draw(new Rect(x+12,y+14,26,26),18,CasualUI.Blue);
   KoreanText.PrepareSkin();var title=new GUIStyle(GUI.skin.label){font=KoreanText.BoldFont,fontSize=UiReadability.Font(17),alignment=TextAnchor.MiddleLeft};title.normal.textColor=CasualUI.Blue;
   var line=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=UiReadability.Font(14),alignment=TextAnchor.MiddleLeft};line.normal.textColor=new Color(.15f,.18f,.25f);
   GUI.Label(new Rect(x+44,y+6,w-52,26),"미션 · "+MissionRules.Label(o.mission),title);
   line.wordWrap=true;GUI.Label(new Rect(x+44,y+32,w-52,40),MissionRules.Goal(o.mission,o)+(o.missionBonus>0?" · 성공 시 +"+o.missionBonus.ToString("N0")+"원":""),line);
   var bold=new GUIStyle(line){font=KoreanText.BoldFont};bold.normal.textColor=CasualUI.Blue;GUI.Label(new Rect(x+44,y+74,w-52,24),Progress,bold);
  }finally{GUI.matrix=previous;}
 }
}
