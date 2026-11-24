using UnityEngine;

// 08 chapter 3 stage 3 (Eungye single): optional missions picked on the call board. Success pays an extra amount
// as its own receipt row and account entry ("mission:<trip>"); failure never reduces the fare. The existing
// "late = 0 won" rule is untouched: a mission only succeeds on a trip that arrives on time.
public static class MissionRules {
 public const int None=0,Express=1,Green=2,Coffee=3,Precise=4,KnowRoad=5;
 // "길 아는 기사" (2026-11-24): guidance is hidden for the ride; a large detour, turning guidance back on (L)
 // or the driving assist ends the mission without any penalty.
 public static bool KnowRoadEnabled {get{return ContentTuning.Int("mission.knowRoad.enabled",1)!=0;}}
 public static float KnowRoadDetour {get{return Mathf.Max(1,F("mission.knowRoad.detourFactor",1.35));}}
 public static float KnowRoadSlack {get{return Mathf.Max(0,F("mission.knowRoad.slackMetres",120));}}
 public static float KnowRoadLimit(JobOffer o){return Mathf.Max(0,o.distance)*KnowRoadDetour+KnowRoadSlack;}
 public const string KnowRoadLine="기사님 길 잘 아시네요.";
 public static int KindCount {get{return KnowRoadEnabled?5:4;}}
 static float F(string id,double fallback){return (float)ContentTuning.Number(id,fallback);}
 public static bool Enabled {get{return ContentTuning.Int("mission.enabled",1)!=0;}}
 public static int BoardChance {get{return ContentTuning.Int("mission.boardChance",100);}}
 public static float ExpressFactor {get{return F("mission.express.targetFactor",1.1);}}
 public static int GreenCount {get{return ContentTuning.Int("mission.green.count",3);}}
 public static int BonusPercent(int kind){return kind==Express?ContentTuning.Int("mission.express.bonusPercent",30):kind==Green?ContentTuning.Int("mission.green.bonusPercent",25):kind==Coffee?ContentTuning.Int("mission.coffee.bonusPercent",20):kind==Precise?ContentTuning.Int("mission.precise.bonusPercent",20):kind==KnowRoad?ContentTuning.Int("mission.knowRoad.bonusPercent",15):0;}
 public static int Bonus(int kind,int fare){return Mathf.CeilToInt(Mathf.Max(0,fare)*Mathf.Max(0,BonusPercent(kind))/10000f)*100;}
 public static string Label(int kind){return kind==Express?"급행":kind==Green?"초록불 타기":kind==Coffee?"커피 든 손님":kind==Precise?"칼정차":kind==KnowRoad?"길 아는 기사":"";}
 public static string Goal(int kind,JobOffer o){
  if(kind==Express)return "정차 시간을 뺀 주행 "+RoadNavigation.FormatEta(ExpressTarget(o))+" 안";
  if(kind==Green)return "신호 "+GreenCount+"곳 연속 멈추지 않고 통과";
  if(kind==Coffee)return "급가속·급제동 없이";
  if(kind==Precise)return "하차 지점 30cm 안에 정차";
  if(kind==KnowRoad)return "길안내 없이 도착 · 크게 돌아가면 실패";
  return "";
 }
 public static float ExpressTarget(JobOffer o){float ride=o.safeRouteSeconds>0?o.safeRouteSeconds:o.distance/8f;return Mathf.Ceil(ride*ExpressFactor);}
}

public partial class PassengerJobs {
 // One mission per fresh board (about one call in three), on any call except a student reunion; missions
 // change neither fare nor deadline, so route-situation calls can carry one too. Last-call boards stay plain.
 void ApplyMissions(){
  if(!CallBoardSingle||!MissionRules.Enabled||FirstHomeDispatchPending||System.Array.Exists(offers,IsLastCall))return;
  var pool=new System.Collections.Generic.List<JobOffer>();foreach(var o in offers)if(o!=null&&o.mission==MissionRules.None&&o.reunionKind==0)pool.Add(o);
  if(pool.Count==0)return;string seed=pool[0].candidateSeed+":mission";
  if(GoodDrivingRules.Roll(seed)>=MissionRules.BoardChance)return;
  var pick=pool[GoodDrivingRules.Roll(seed+":offer")%pool.Count];
  int kind=1+GoodDrivingRules.Roll(seed+":kind")%MissionRules.KindCount;
  pick.mission=kind;pick.missionBonus=MissionRules.Bonus(kind,pick.fare);
 }
 public string MissionCardLine(JobOffer o){return o==null||o.mission==MissionRules.None?"":"\n[미션] "+MissionRules.Label(o.mission)+" · 성공 시 +"+o.missionBonus.ToString("N0")+"원 · "+MissionRules.Goal(o.mission,o);}
 // Express counts only time the taxi actually moves with the passenger aboard.
 void TickMission(float seconds){
  if(current==null||current.mission!=MissionRules.Express||trip.State!=TripState.OnTrip||trip.ArrivalLocked||!session.taxi||PvpNativeLan.IsReplica(session))return;
  if(session.taxi.body.velocity.magnitude>.5f)current.missionDriveSeconds+=seconds;
 }
 // Called by DrivingManners when the taxi enters a signal junction; stopped = waited at that stop line.
 public void ObserveSignalEntry(bool stopped){
  if(current==null||current.mission!=MissionRules.Green||trip.State!=TripState.OnTrip||trip.ArrivalLocked)return;
  current.missionGreenRun=stopped?0:current.missionGreenRun+1;current.missionGreenBest=Mathf.Max(current.missionGreenBest,current.missionGreenRun);
 }
 void SettleMission(PassengerReward r){
  if(current==null||r==null||current.mission==MissionRules.None)return;var m=session.manners;
  bool ok=trip.ArrivedOnTime&&r.Total>0;
  if(current.mission==MissionRules.Express)ok&=current.missionDriveSeconds<=MissionRules.ExpressTarget(current);
  else if(current.mission==MissionRules.Green)ok&=current.missionGreenBest>=MissionRules.GreenCount;
  else if(current.mission==MissionRules.Coffee)ok&=m&&m.TripHarsh==0;
  else if(current.mission==MissionRules.Precise)ok&=m&&m.goodPrecise>0;
  else if(current.mission==MissionRules.KnowRoad)ok&=!current.missionVoid&&current.meterMetres<=MissionRules.KnowRoadLimit(current);
  r.missionKind=current.mission;r.missionSuccess=ok;r.missionBonus=ok?current.missionBonus:0;
 }
 void PayMission(){var r=RequestReward;if(r==null||r.missionBonus<=0)return;session.business.RecordMission(session.law.tripId,r.missionBonus);}
 // Guidance stays hidden only while the passenger is aboard on a live "길 아는 기사" trip.
 public bool KnowRoadActive {get{return CallBoardSingle&&current!=null&&current.mission==MissionRules.KnowRoad&&!current.missionVoid&&trip.State==TripState.OnTrip;}}
 public string KnowRoadHint {get{return current==null?"":"목적지 "+KoreanText.Stop(stops[current.drop].name)+"\n아는 길로 가세요 · L 길안내 켜기(미션 포기)";}}
 public void TickKnowRoad(){
  bool hide=KnowRoadActive;if(nav.GuidanceHidden!=hide){nav.GuidanceHidden=hide;nav.Refresh();}
  if(!hide||trip.ArrivalLocked)return;
  if(Time.timeScale>0&&!session.paused&&!session.FullMapOpen&&Input.GetKeyDown(KeyCode.L))EndKnowRoad("길안내를 다시 켰어요");
  else if(session.assisted&&session.assisted.Active)EndKnowRoad("주행 보조를 켰어요");
  else if(current.meterMetres>MissionRules.KnowRoadLimit(current))EndKnowRoad("경로를 크게 벗어났어요");
 }
 // Failure has no penalty: guidance comes back and the agreed fare is untouched.
 public void EndKnowRoad(string why){
  if(!KnowRoadActive)return;current.missionVoid=true;nav.GuidanceHidden=false;nav.Refresh();
  var feed=session.GetComponent<DriveNotifications>();if(feed)feed.Card("미션 길 아는 기사 종료 · "+why+" · 길안내를 다시 켭니다. 기본 운임은 그대로예요");
 }
}
