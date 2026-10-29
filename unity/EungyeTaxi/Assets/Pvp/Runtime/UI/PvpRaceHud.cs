using System;
using UnityEngine;
using Pvp.Map;
using Pvp.Network;
using Pvp.Competition;

namespace Pvp.UI {
// Independent presentation of server snapshots. No campaign or business session is consulted.
public sealed class PvpRaceHud:MonoBehaviour {
 PvpNetworkSession session;PvpMapRuntime map;PvpPracticeSession practice;PvpCompetitionBridge bridge;
 GUIStyle text,small,title,digits;string duration="",grace="0",message="";Vector2 passengerScroll,serviceScroll;
 readonly Color mint=new Color(.3f,.91f,.72f),amber=new Color(1,.78f,.28f),muted=new Color(.62f,.75f,.8f);
 public void Begin(PvpNetworkSession session,PvpMapRuntime map,PvpPracticeSession practice,PvpCompetitionBridge bridge){this.session=session;this.map=map;this.practice=practice;this.bridge=bridge;}
 void Styles(){KoreanText.PrepareSkin();if(text!=null)return;text=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=18,wordWrap=true};small=new GUIStyle(text){fontSize=14};title=new GUIStyle(text){font=KoreanText.BoldFont,fontSize=25};digits=new GUIStyle(title){fontSize=60};}
 void Label(Rect r,string value,GUIStyle style=null){GUI.Label(r,value??"",style??text);}
 void Panel(Rect r,Color accent){KoreanText.Panel(r);Fill(new Rect(r.x,r.y,4,r.height),accent);}
 void Fill(Rect r,Color c){var old=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
 bool Button(Rect r,string caption,bool enabled=true){bool old=GUI.enabled;GUI.enabled=old&&enabled;bool hit=GUI.Button(r,caption,KoreanText.Button(16));GUI.enabled=old;return hit;}
 int Player {get{return session.IsHost?0:1;}}
 string StopName(int stop){return stop>=0&&stop<map.Stops.Count?map.Stops[stop].Name:"거점 확인 중";}
 PvpPassengerView OwnTrip(PvpCompetitionSnapshot match){if(match==null||match.Passengers==null)return null;foreach(var p in match.Passengers)if(p.Owner==Player&&p.Status==PvpPassengerStatus.Onboard)return p;return null;}
 PvpServiceView OwnService(){var rows=bridge.CurrentServices;if(rows!=null)foreach(var row in rows)if(row.Player==Player)return row;return null;}
 static string Phase(PvpRoundPhase phase){switch(phase){case PvpRoundPhase.Countdown:return "출발 준비";case PvpRoundPhase.Running:return "경기 진행";case PvpRoundPhase.Closing:return "마감 유예 · 운송만 완료";case PvpRoundPhase.Settling:return "서버 정산 중";case PvpRoundPhase.Results:return "확정 결과";case PvpRoundPhase.Aborted:return "경기 중단";default:return "대기실";}}
 void OnGUI(){if(session==null||map==null||bridge==null||practice==null)return;Styles();var old=GUI.matrix;var oldColor=GUI.color;bool oldEnabled=GUI.enabled;float scale=Mathf.Max(.1f,CockpitUI.Scale);GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));float w=Screen.width/scale,h=Screen.height/scale;
  try {
   var match=bridge.CurrentMatch;var trip=OwnTrip(match);int tick=match==null?bridge.CurrentTraffic.Tick:match.Tick;
   Panel(new Rect(24,20,790,88),mint);Label(new Rect(42,29,335,34),"PVP · 확장 도시 시험장",title);Label(new Rect(42,66,390,26),session.IsOfflineBot?"혼자 경기 · 같은 규칙의 봇 상대":"직접 IP · "+(session.IsHost?"호스트":"게스트"),small);
   Label(new Rect(435,29,260,32),match==null?"상대 준비 대기":Phase(match.Phase),title);
   string clock=match==null?"경기 시간은 서버 공통":match.Phase==PvpRoundPhase.Countdown?"출발까지 "+CockpitUI.Clock((match.StartTick-tick)/50.0):"남은 "+CockpitUI.Clock(((match.Phase==PvpRoundPhase.Closing?match.FinalEndTick:match.MainEndTick)-tick)/50.0);
   Label(new Rect(435,66,340,28),clock,small);
   if(Button(new Rect(w-164,20,140,44),"초기 화면 / 나가기"))session.Stop();
   Panel(new Rect(24,126,320,258),amber);Label(new Rect(40,138,290,32),"공통 승객 · 운송",title);
   DrawPassengers(match,trip,new Rect(40,180,288,155));
   if(Button(new Rect(40,339,288,32),bridge.PendingAction?"서버 응답 대기":"E · 현장에서 승차 / 하차",session.InputReady&&!bridge.PendingAction&&match!=null))bridge.RequestInteraction();
   if(trip!=null&&!string.IsNullOrEmpty(bridge.SelectedService)&&Button(new Rect(365,172,220,32),"운송 목적지로 길안내"))bridge.ClearServiceRoute();
   Panel(new Rect(24,400,320,108),mint);Label(new Rect(40,410,290,26),"주행 안내",small);Label(new Rect(40,439,290,58),trip==null?"승객 선택 → 표시된 승차 거점\n정차 후 E · 하차도 지정 거점에서": "하차 · "+StopName(trip.Destination)+"\n약정 "+trip.QuotedFare.ToString("N0")+"원",text);
   DrawRules(new Rect(24,524,320,142));
   DrawSpeed(new Rect(24,h-218,320,194));
   DrawScores(match,new Rect(w-366,80,342,220));
   DrawServices(new Rect(w-366,316,342,224),tick);
   DrawMap(new Rect(w-366,h-340,342,264),trip,false);
   Panel(new Rect(365,h-96,w-755,72),mint);Label(new Rect(379,h-87,w-783,25),"WASD / 방향키 주행 · Space 제동 · 정차 후 Q 전후진",small);Label(new Rect(379,h-59,w-783,25),"E 승하차 · C/V 시점 · M 전체 지도 · "+practice.CameraModeName,small);
   string status=string.IsNullOrEmpty(message)?bridge.ActionMessage:message;Label(new Rect(365,119,w-755,48),status+(bridge.PendingAction?" · 서버 응답 대기":""),small);
   if(match==null||match.Phase==PvpRoundPhase.Lobby)DrawLobby(new Rect((w-500)/2,200,500,332));
   else if(match.Phase==PvpRoundPhase.Results||match.Phase==PvpRoundPhase.Aborted){Panel(new Rect((w-500)/2,190,500,216),mint);Label(new Rect((w-464)/2,204,464,38),Phase(match.Phase),title);Label(new Rect((w-464)/2,251,464,86),Result(match),text);if(Button(new Rect((w-464)/2,350,464,40),"초기 화면으로 돌아가기"))session.Stop();}
   if(practice.Overview){var area=new Rect(365,126,w-755,h-238);Panel(area,mint);DrawMap(new Rect(area.x+16,area.y+12,area.width-32,area.height-24),trip,true);}
  }finally{GUI.matrix=old;GUI.color=oldColor;GUI.enabled=oldEnabled;}
 }
 void DrawPassengers(PvpCompetitionSnapshot match,PvpPassengerView trip,Rect r){
  if(trip!=null){var card=Pvp.Passengers.PvpPassengerCatalog.At(trip.ContentIndex);Label(new Rect(r.x,r.y,r.width,42),"운송 중 → "+StopName(trip.Destination));Label(new Rect(r.x,r.y+45,r.width,r.height-45),card.TypeLabel+" · "+card.Dialogue,small);return;}
  if(match==null){Label(r,"같은 시간, 같은 승객 공급.\n운임 + 안전 보너스 − 위반 − 서비스 비용으로 경쟁합니다.",small);return;}
  int count=0;foreach(var p in match.Passengers)if(p.Status==PvpPassengerStatus.Waiting)count++;
  if(count==0){Label(r,"현재 대기 승객이 없습니다.\n도로와 연료를 확인하세요.",small);return;}
  passengerScroll=GUI.BeginScrollView(r,passengerScroll,new Rect(0,0,r.width-20,count*64));int index=0;
  foreach(var p in match.Passengers)if(p.Status==PvpPassengerStatus.Waiting){if(Button(new Rect(0,index++*64,r.width-22,58),(bridge.SelectedPassenger==p.Id?"▶ ":"")+StopName(p.Pickup)+" → "+StopName(p.Destination)+"\n약정 "+p.QuotedFare.ToString("N0")+"원",session.InputReady&&!bridge.PendingAction))bridge.SelectPassenger(p.Id);}
  GUI.EndScrollView();
 }
 void DrawScores(PvpCompetitionSnapshot match,Rect r){Panel(r,mint);Label(new Rect(r.x+16,r.y+9,r.width-32,30),"경기 점수"+(match!=null&&match.Phase==PvpRoundPhase.Results?" · 확정":" · 잠정"),title);
  if(match==null||match.Scores==null){Label(new Rect(r.x+16,r.y+54,r.width-32,94),"경기 시작 후 서버 점수를 표시합니다.\n무운송 참가자는 순위에서 제외됩니다.",small);return;}
  for(int i=0;i<2;i++){PvpScoreRow row=null;foreach(var value in match.Scores)if(value.Player==(i==0?Player:1-Player))row=value;if(row==null)continue;float y=r.y+49+i*80;
   Label(new Rect(r.x+16,y,155,28),(i==0?"나":session.IsOfflineBot?"봇":"상대")+" · 완료 "+row.Completed,small);Label(new Rect(r.x+173,y-6,153,38),row.Score.ToString("N0")+"점",title);
   Label(new Rect(r.x+16,y+29,r.width-32,42),"운임 "+row.FarePoints+" + 안전 "+row.Bonus+"\n− 위반 "+row.Penalty+" − 서비스 "+row.ServiceExpenses,small);
  }
 }
 void DrawSpeed(Rect r){Panel(r,mint);var state=session.DriverPose.State;Label(new Rect(r.x+16,r.y+9,r.width-32,24),(state.Speed<-.1f?"R":"D")+" · "+practice.CameraModeName,small);Label(new Rect(r.x+13,r.y+25,220,80),Mathf.Abs(state.Speed*3.6f).ToString("000"),digits);Label(new Rect(r.x+238,r.y+76,70,26),"km/h",small);
  HudGlyph.Draw(new Rect(r.x+18,r.y+126,26,26),8,mint);var service=OwnService();float fuel=service==null?100:service.Fuel;Label(new Rect(r.x+55,r.y+117,235,28),service==null?"연료 정보 동기화 대기":"연료 "+fuel.ToString("F0")+"%",small);Fill(new Rect(r.x+55,r.y+151,235,8),new Color(.15f,.26f,.3f));Fill(new Rect(r.x+55,r.y+151,235*Mathf.Clamp01(fuel/100f),8),fuel<15?amber:mint);
  Label(new Rect(r.x+16,r.y+166,290,22),"현재 도로 제한 "+(map.SpeedLimit(session.DriverPose.Edge)*3.6f).ToString("F0")+" km/h",small);
 }
 void DrawServices(Rect r,int tick){Panel(r,amber);HudGlyph.Draw(new Rect(r.x+16,r.y+13,25,25),8,amber);Label(new Rect(r.x+52,r.y+10,r.width-65,30),"주유 · 경기 시간은 계속 진행",small);var own=OwnService();
  Label(new Rect(r.x+16,r.y+47,r.width-32,40),own==null?"시설에서 정차 후 주유하세요.":own.Busy?"주유 중 · 남은 "+CockpitUI.Clock((own.BusyUntilTick-tick)/50.0):own.Status,small);
  var area=new Rect(r.x+16,r.y+91,r.width-32,82);serviceScroll=GUI.BeginScrollView(area,serviceScroll,new Rect(0,0,area.width-20,map.Services.Count*52+38));int i=0;
  foreach(var facility in map.Services){if(Button(new Rect(0,i++*52,area.width-22,48),facility.Name+"\n"+(facility.DurationTicks/50f).ToString("0.#")+"초 / "+facility.CostPoints+"점 · 길안내 / 현장 주유",session.InputReady&&!bridge.PendingAction&&(own==null||!own.Busy)))bridge.RequestService(facility.Id);}
  if(Button(new Rect(0,i*52,area.width-22,34),"제자리 연료 지원 · 8초 / 6점",session.InputReady&&!bridge.PendingAction&&own!=null&&own.Fuel<=PvpServiceAuthority.EmptyFuel&&!own.Busy))bridge.RequestService("roadside");GUI.EndScrollView();
  if(Button(new Rect(r.x+16,r.y+182,r.width-32,30),"진행 중 주유 취소",session.InputReady&&own!=null&&own.Busy&&!bridge.PendingAction))bridge.CancelService();
 }
 void DrawRules(Rect r){Panel(r,amber);HudGlyph.Draw(new Rect(r.x+16,r.y+12,23,23),7,amber);Label(new Rect(r.x+49,r.y+8,r.width-62,29),"법규 · 서버 판정",small);var events=bridge.CurrentEvents;Pvp.Rules.PvpRuleEvent latest=null;if(events!=null)for(int i=events.Length-1;i>=0;i--)if(events[i].Player==Player){latest=events[i];break;}
  Label(new Rect(r.x+16,r.y+43,r.width-32,r.height-51),latest==null?"신호·정지선·제한속도 준수\n우회전 시 정차와 보행자 확인":(latest.Deferred?"판정 보류 · ":"감점 "+latest.Penalty+" · ")+RuleName(latest.Kind)+"\n"+latest.Evidence,small);
 }
 static string RuleName(string kind){switch(kind){case "Speeding":case "speed":case "speeding":return "제한속도 위반";case "RedStopLine":return "적색 정지선";case "RedEntry":return "적색 진입";case "RightStop":return "우회전 정차";case "RightSignal":return "우회전 신호";case "PedestrianObstruction":return "보행 진로 방해";case "SignalTransition":return "신호 전환";case "NpcRearContact":return "NPC 후방 접촉";case "NpcContact":case "PedestrianContact":return "접촉 확인";case "RedLight":case "signal":case "red-light":return "신호 위반";case "Contact":case "contact":return "접촉";case "WrongWay":case "wrong-way":return "역주행";case "RightTurn":case "right-turn":return "우회전 준수";default:return kind;}}
 void DrawLobby(Rect r){Panel(r,mint);Label(new Rect(r.x+20,r.y+15,r.width-40,38),"공통 출발 준비",title);Label(new Rect(r.x+20,r.y+62,r.width-40,48),session.Status+"\n"+(session.IsOfflineBot?session.BotStatus:session.GuestConnected?"상대 연결됨":"상대 연결 대기"),small);
  if(session.IsHost){Label(new Rect(r.x+20,r.y+118,218,28),"경기 시간 (초)",small);Label(new Rect(r.x+258,r.y+118,218,28),"마감 유예 (초)",small);duration=GUI.TextField(new Rect(r.x+20,r.y+150,218,36),duration,5);grace=GUI.TextField(new Rect(r.x+258,r.y+150,218,36),grace,4);
   if(Button(new Rect(r.x+20,r.y+202,r.width-40,44),"준비된 경기 시작",session.InputReady&&session.GuestConnected&&bridge.GuestReady)){try{int seconds,finish;if(!int.TryParse(duration,out seconds)||!int.TryParse(grace,out finish))throw new ArgumentException("초 단위 정수를 입력하세요.");bridge.StartRound(seconds,finish);message="";}catch(Exception e){message=e.Message;}}
  }else if(Button(new Rect(r.x+20,r.y+202,r.width-40,44),bridge.GuestReady?"준비 완료 · 호스트 시작 대기":"준비 완료",session.InputReady&&!bridge.GuestReady))session.IssueCompetitionCommand(new PvpPassengerCommand{Kind=0});
  Label(new Rect(r.x+20,r.y+260,r.width-40,58),(bridge.GuestReady?"상대 준비 완료":"상대 준비 대기")+"\n점수 = 운임 + 안전 − 위반 − 서비스",small);
 }
 string Result(PvpCompetitionSnapshot match){if(match.Phase==PvpRoundPhase.Aborted)return match.Reason;string result="";foreach(var row in match.Scores)result+=(row.Player==Player?"나":session.IsOfflineBot?"봇":"상대")+" · "+row.Score+"점 · "+(row.Eligible?row.Rank+"위":row.Exclusion)+"\n";return result;}
 void DrawMap(Rect r,PvpPassengerView trip,bool full){Panel(r,mint);Label(new Rect(r.x+12,r.y+6,r.width-24,26),full?"전체 지도 · M 닫기 / C·V 운전 시점":"도시 지도 · M 확대",small);var plot=new Rect(r.x+16,r.y+42,r.width-32,r.height-66);Vector3 min=map.BoundsMin,max=map.BoundsMax;float zoom=Mathf.Min(plot.width/Mathf.Max(1,max.x-min.x),plot.height/Mathf.Max(1,max.z-min.z));var center=(min+max)*.5f;
  foreach(var district in map.Districts){var c=Point(district.Center,plot,center,zoom);var color=district.Color;color.a=.22f;Fill(new Rect(c.x-district.Size.x*zoom*.5f,c.y-district.Size.y*zoom*.5f,district.Size.x*zoom,district.Size.y*zoom),color);if(full){float offset=0;foreach(var stop in map.Stops){var p=Point(stop.Position,plot,center,zoom);if(Mathf.Abs(p.x-c.x)<120&&Mathf.Abs(p.y-c.y)<36)offset=44;}Label(new Rect(c.x-80,c.y-18-offset,160,28),district.Name,small);}}
  foreach(var edge in map.Connections)HudGlyph.MapLine(Point(map.Nodes[edge.From],plot,center,zoom),Point(map.Nodes[edge.To],plot,center,zoom),new Color(.3f,.43f,.47f),full?4:2,plot);
  var route=practice.CurrentRoute;if(route!=null&&route.Found&&(trip!=null||!string.IsNullOrEmpty(bridge.SelectedPassenger)||!string.IsNullOrEmpty(bridge.SelectedService)))for(int i=1;i<route.Points.Length;i++)HudGlyph.MapLine(Point(route.Points[i-1],plot,center,zoom),Point(route.Points[i],plot,center,zoom),mint,full?4:2,plot);
  int goal=trip==null?-1:trip.Destination;var match=bridge.CurrentMatch;if(goal<0&&match!=null)foreach(var passenger in match.Passengers)if(passenger.Id==bridge.SelectedPassenger)goal=passenger.Pickup;
  foreach(var facility in map.Services)if(facility.Id==bridge.SelectedService)goal=facility.StopIndex;
  for(int i=0;i<map.Stops.Count;i++){var p=Point(map.Stops[i].Position,plot,center,zoom);Fill(new Rect(p.x-3,p.y-3,6,6),i==goal?amber:muted);if(full)Label(new Rect(p.x+7,p.y-14,155,28),map.Stops[i].Name,small);}
  foreach(var facility in map.Services)if(facility.StopIndex>=0&&facility.StopIndex<map.Stops.Count){var p=Point(map.Stops[facility.StopIndex].Position,plot,center,zoom);HudGlyph.Draw(new Rect(p.x-6,p.y-6,12,12),8,amber);}
  var self=Point(session.DriverPose.State.Position,plot,center,zoom);HudGlyph.Draw(new Rect(self.x-8,self.y-8,16,16),1,mint);var heading=Quaternion.Euler(0,session.DriverPose.State.YawDegrees,0)*Vector3.forward;HudGlyph.MapLine(self,self+new Vector2(heading.x,-heading.z)*14,mint,2,plot);
  Label(new Rect(r.x+12,r.yMax-24,r.width-24,22),"민트: 나 / 경로   노랑: 목적지 / 주유",small);
 }
 static Vector2 Point(Vector3 p,Rect r,Vector3 center,float scale){return new Vector2(r.center.x+(p.x-center.x)*scale,r.center.y-(p.z-center.z)*scale);}
}
}
