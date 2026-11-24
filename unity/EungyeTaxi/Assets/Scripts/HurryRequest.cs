using UnityEngine;

// Temptation, "좀 빨리 가 주세요" (design 08 ch.5.6 item 4.1, Eungye single, 2026-11-24). Right after boarding a
// passenger may ask the driver to hurry and offer a reward. The reward is paid only when the taxi arrives within the
// moving-time target (signal waits do not count, like the express mission) AND no speed-limit record was made on
// the ride: the money goes to the driver who is quick through signals and route choice, not to the one who speeds.
public static class HurryRules {
 public const int None=0,Fast=1,Safe=2,Legal=3;
 static float F(string id,double fallback){return (float)ContentTuning.Number(id,fallback);}
 static int I(string id,int fallback){return ContentTuning.Int(id,fallback);}
 public static bool Enabled {get{return I("hurry.enabled",1)!=0;}}
 public static int PerShiftMax {get{return Mathf.Max(0,I("hurry.perShiftMax",1));}}
 public static int RushChance {get{return Mathf.Clamp(I("hurry.rushChancePercent",60),0,100);}}
 public static int OtherChance {get{return Mathf.Clamp(I("hurry.otherChancePercent",15),0,100);}}
 public static float MinDistance {get{return Mathf.Max(0,F("hurry.minDistanceMetres",600));}}
 public static float FastFactor {get{return Mathf.Clamp(F("hurry.targetFactor",.9),.3f,2);}}
 public static int FastPercent {get{return Mathf.Clamp(I("hurry.rewardPercent",20),0,200);}}
 public static float SafeFactor {get{return Mathf.Clamp(F("hurry.safeTargetFactor",.95),.3f,2);}}
 public static int SafePercent {get{return Mathf.Clamp(I("hurry.safeRewardPercent",10),0,200);}}
 public static bool ForfeitOnSpeeding {get{return I("hurry.forfeitOnSpeeding",1)!=0;}}
 public const float AnswerSeconds=15;
 // The passenger's estimate: the same moving-time reference as the express mission, before its factor.
 public static float RideSeconds(float safeRouteSeconds,float distance){return safeRouteSeconds>0?safeRouteSeconds:Mathf.Max(0,distance)/8f;}
 public static float Target(int choice,float ride){return choice==Fast?Mathf.Ceil(ride*FastFactor):choice==Safe?Mathf.Ceil(ride*SafeFactor):0;}
 public static int Reward(int choice,int fare){int percent=choice==Fast?FastPercent:choice==Safe?SafePercent:0;return Mathf.CeilToInt(Mathf.Max(0,fare)*percent/10000f)*100;}
 // Why the passenger does not ask on this boarding; "" = may ask (then the chance roll decides).
 public static string Blocked(bool single,int done,int mission,bool drunk,float distance){
  if(!Enabled)return "꺼짐";if(!single)return "은계 싱글 아님";
  if(mission!=MissionRules.None)return "미션 호출";if(drunk)return "취객";if(distance<MinDistance)return "가까운 거리";
  if(done>=PerShiftMax)return "하루 한도";return "";
 }
 public static int Chance(bool rush){return rush?RushChance:OtherChance;}
 // Settled at arrival. Speeding forfeits the reward even when on time.
 public static string Outcome(int choice,float moving,float target,bool speeding,out bool paid){
  paid=false;if(choice!=Fast&&choice!=Safe)return "";
  if(speeding&&ForfeitOnSpeeding)return "과속으로 사례금 없음";
  if(moving<=target){paid=true;return "목표 안 도착";}
  return "목표 시간 넘김";
 }
 public static string ChoiceLine(int choice){return choice==Fast?"네, 서둘러 볼게요":choice==Safe?"안전하게 최대한 빨리요":choice==Legal?"규정 속도로 갑니다":"";}
}

public sealed class HurryRequest : MonoBehaviour {
 public DriveSession session;
 int day=-1,done,cameraTickets;string boardTrip="";TripState lastState=TripState.Idle;bool asked,answered,settled,speeding;int choice;float askAt=-1,askedAt,moving,ride,target;int fare,reward,lawCount;string trip="";
 public int Asked,Paid,Forfeited,Late,Declined;public string LastOutcome="",LastBlocked="";
 public bool Showing {get{return asked&&!answered;}}
 public int Choice {get{return choice;}}
 public float Moving {get{return moving;}}
 public float TargetSeconds {get{return target;}}
 public bool SpeedingOnRide {get{return speeding;}}
 public int RewardIfPaid {get{return reward;}}
 bool Single {get{return session&&session.traffic&&session.traffic.EungyeSingle&&!session.simulationMatch&&!session.IsSimulationBot&&!PvpNativeLan.IsReplica(session)&&!PracticeSession.Enabled&&session.business&&session.business.enabledCampaign;}}
 void Card(string text){var n=session.GetComponent<DriveNotifications>();if(n)n.Card(text);}
 void Update(){
  if(!session||!session.jobs||!session.business||session.business.ledger==null)return;
  var l=session.business.ledger;if(l.Day!=day){day=l.Day;done=0;}
  var state=session.jobs.trip.State;
  // A new boarding is also a new trip id while aboard (the board can be replaced within one frame).
  string tripNow=session.law?session.law.tripId:"";
  if(state==TripState.OnTrip&&lastState==TripState.OnTrip&&tripNow!=boardTrip){Leave(TripState.Idle);lastState=TripState.Idle;}
  if(state==TripState.OnTrip&&lastState!=TripState.OnTrip){boardTrip=tripNow;Board();}
  if(state!=TripState.OnTrip&&lastState==TripState.OnTrip)Leave(state);
  lastState=state;
  if(state!=TripState.OnTrip)return;
  if(askAt>=0&&!asked&&Time.time>=askAt&&CanShow()){asked=true;askedAt=Time.time;Asked++;done++;}
  if(Showing){
   if(Time.time-askedAt>HurryRules.AnswerSeconds){Choose(HurryRules.Legal,true);return;}
   int index=NumericChoiceInput.Read(3,i=>true);if(index>=0)Choose(index+1,false);
  }
  if((choice==HurryRules.Fast||choice==HurryRules.Safe)&&!speeding&&session.cameras&&session.cameras.Tickets>cameraTickets){speeding=true;if(HurryRules.ForfeitOnSpeeding)Card("유혹 · 사례금 없음 · 단속 카메라 · “아니, 그렇게까지는…”");}
  if(choice==HurryRules.Fast||choice==HurryRules.Safe){
   if(!session.jobs.trip.ArrivalLocked&&session.taxi.body.velocity.magnitude>.5f&&Time.timeScale>0)moving+=Time.deltaTime;
   if(!speeding&&session.law.ledger.Count>lawCount){var events=session.law.ledger.Events;for(int i=lawCount;i<events.Length;i++)if(events[i].family=="제한속도"){speeding=true;if(HurryRules.ForfeitOnSpeeding){Card("유혹 · 사례금 없음 · “아니, 그렇게까지는…”");Debug.Log("HURRY speeding forfeits the reward");}}lawCount=events.Length;}
  }
 }
 // The panel waits for the passenger's other dialogue to close.
 bool CanShow(){var r=session.requests;return r&&r.PresentationAvailable&&!r.HasPendingChoice&&!session.jobs.trip.ArrivalLocked&&!(session.GetComponent<PassengerBehaviorUI>()&&session.GetComponent<PassengerBehaviorUI>().Showing);}
 void Board(){
  Reset();var o=session.jobs.current;if(o==null)return;
  string why=o.variety==CallBoardRules.OutOfTown?"시외 콜":HurryRules.Blocked(Single,done,o.mission,session.jobs.DrunkRide!=null&&session.jobs.DrunkRide.IsDrunk,o.distance);LastBlocked=why;if(why!="")return;
  string seed=(session.law?session.law.tripId:"")+":"+o.candidateSeed+":hurry";
  if(GoodDrivingRules.Roll(seed)>=HurryRules.Chance(o.variety==CallBoardRules.Rush)){LastBlocked="확률";return;}
  Prepare(o);
 }
 // Also used by the acceptance probe to ask on a given boarding.
 public void Prepare(JobOffer o){
  Reset();trip=session.law?session.law.tripId:"";fare=o.fare;ride=HurryRules.RideSeconds(o.safeRouteSeconds,o.distance);askAt=Time.time+3;
  Debug.Log("HURRY prepared trip="+trip+" ride="+ride.ToString("0"));
 }
 void Reset(){asked=answered=settled=speeding=false;choice=HurryRules.None;askAt=-1;moving=0;target=0;reward=0;trip="";}
 // Acceptance probe: moving time the game would have counted.
 public void AddMoving(float seconds){moving+=Mathf.Max(0,seconds);}
 public void Choose(int value,bool timeout){
  if(!Showing)return;answered=true;choice=value;target=HurryRules.Target(choice,ride);reward=HurryRules.Reward(choice,fare);lawCount=session.law.ledger.Count;cameraTickets=session.cameras?session.cameras.Tickets:0;
  session.requests.BlockResponseThisFrame();
  if(choice==HurryRules.Legal){Declined++;LastOutcome="규정 속도";bool agree=GoodDrivingRules.Roll(trip+":hurry:legal")<50;
   if(!timeout)Card("유혹 · 규정 속도 · "+(agree?"“그렇죠, 안전이 먼저죠.”":"“…알겠어요.”"));return;}
  Card("유혹 · "+HurryRules.ChoiceLine(choice)+" · 목표 "+Mathf.CeilToInt(target)+"초(신호 대기 제외) · 과속 없으면 +"+reward.ToString("N0")+"원");
  Debug.Log("HURRY choice="+choice+" target="+target.ToString("0")+" reward="+reward);
 }
 void Leave(TripState state){
  if(settled||choice!=HurryRules.Fast&&choice!=HurryRules.Safe){Reset();return;}
  settled=true;bool paid;string outcome=HurryRules.Outcome(choice,moving,target,speeding,out paid);
  // Delivered and paid: the recorded passenger result for this trip, not the transient trip state.
  var result=session.business.passengers.FindLast(r=>r!=null&&r.tripId==trip);bool delivered=result!=null&&result.paid>0;
  if(!delivered){paid=false;outcome="운송 실패·지각";}
  LastOutcome=outcome;Debug.Log("HURRY outcome="+outcome+" moving="+moving.ToString("0.0")+" target="+target.ToString("0"));
  if(paid&&reward>0&&session.business.RecordHurry(trip,reward)){Paid++;Card("유혹 · 사례금 +"+reward.ToString("N0")+"원 · “덕분에 늦지 않았어요!”");}
  else if(speeding&&HurryRules.ForfeitOnSpeeding)Forfeited++;
  else if(delivered){Late++;Card("유혹 · 목표 "+Mathf.CeilToInt(target)+"초 넘김("+Mathf.CeilToInt(moving)+"초) · “조금 늦었네요. 그래도 고마워요.”");}
  choice=HurryRules.None;
 }
 void OnGUI(){
  if(!Showing||!session||session.IsSimulationBot||!CanShow())return;
  var options=new PassengerDialogueUI.Option[3];
  int fast=HurryRules.Reward(HurryRules.Fast,fare),safe=HurryRules.Reward(HurryRules.Safe,fare);
  options[0]=new PassengerDialogueUI.Option("1",HurryRules.ChoiceLine(HurryRules.Fast),Mathf.CeilToInt(HurryRules.Target(HurryRules.Fast,ride))+"초 안 · +"+fast.ToString("N0")+"원 · 과속하면 없음");
  options[1]=new PassengerDialogueUI.Option("2",HurryRules.ChoiceLine(HurryRules.Safe),Mathf.CeilToInt(HurryRules.Target(HurryRules.Safe,ride))+"초 안 · +"+safe.ToString("N0")+"원 · 과속하면 없음");
  options[2]=new PassengerDialogueUI.Option("3",HurryRules.ChoiceLine(HurryRules.Legal),"손님 반응만");
  int selected=PassengerDialogueUI.Draw("승객 부탁","좀 빨리 가 주세요. 제때 도착하면 더 드릴게요.",options,"신호 대기 제외 · "+Mathf.CeilToInt(HurryRules.AnswerSeconds-(Time.time-askedAt))+"초 뒤 규정 속도",PassengerDialogueUI.PortraitIdentity(session));
  if(selected>=0&&NumericChoiceInput.TryConsume(Time.frameCount))Choose(selected+1,false);
 }
}
