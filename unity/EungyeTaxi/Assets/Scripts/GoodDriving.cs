using System;
using UnityEngine;

// 08 chapter 2 (2026-11-23): immediate praise for good driving, passenger tips and a clean-run streak.
// - No grade or rating accumulates; the streak lives for one working day and a broken streak costs nothing.
// - Every threshold is a content setting (goodDriving.*, tip.*). Older profiles fall back to the catalog.
// - Tips are decided once per trip from the trip id, so a reload or a repeated settlement cannot change or
//   duplicate them (the account and the PvP ledger both refuse a second "tip:<trip>" entry).
public static class GoodDrivingRules {
 public const int Smooth=0,Lane=1,Precise=2;
 static float F(string id,double fallback){return (float)ContentTuning.Number(id,fallback);}
 static int I(string id,int fallback){return ContentTuning.Int(id,fallback);}
 public static float SmoothMinKph {get{return F("goodDriving.smoothStop.minKph",15);}}
 public static float SmoothRecentSeconds {get{return F("goodDriving.smoothStop.recentSeconds",8);}}
 public static float SmoothWindowSeconds {get{return F("goodDriving.smoothStop.windowSeconds",4);}}
 public static float SmoothMaxDecel {get{return F("goodDriving.smoothStop.maxDecel",2.5);}}
 public static float SmoothFinalDecel {get{return F("goodDriving.smoothStop.finalDecel",1.2);}}
 public static float LaneMinKph {get{return F("goodDriving.lane.minKph",20);}}
 public static float LaneSignalLead {get{return F("goodDriving.lane.signalLeadSeconds",1);}}
 public static float LaneCooldown {get{return F("goodDriving.lane.cooldownSeconds",20);}}
 public static int LaneMaxPerTrip {get{return I("goodDriving.lane.maxPerTrip",2);}}
 public static float PreciseDistance {get{return F("goodDriving.precise.distance",.3);}}
 public static float PreciseHeading {get{return F("goodDriving.precise.headingDegrees",5);}}
 public static int LineChance {get{return I("goodDriving.lineChance",50);}}
 public static int TipBaseChance {get{return I("tip.chance.base",20);}}
 public static int TipChancePerPoint {get{return I("tip.chance.perPoint",10);}}
 public static int TipChanceMax {get{return I("tip.chance.max",80);}}
 public static int StreakChancePerTrip {get{return I("tip.streak.chancePerTrip",5);}}
 public static int StreakChanceMax {get{return I("tip.streak.chanceMax",25);}}
 public static int TipBase {get{return I("tip.amount.base",100);}}
 public static int TipPerTwoPoints {get{return I("tip.amount.perTwoPoints",100);}}
 public static int TipPreciseBonus {get{return I("tip.amount.preciseBonus",100);}}
 public static int TipMax {get{return I("tip.amount.max",500);}}

 // Stable 0-99 roll for a trip; never Unity's random state.
 public static int Roll(string key){unchecked{uint h=2166136261;foreach(char c in key??""){h^=c;h*=16777619;}h^=h>>15;h*=2246822519;h^=h>>13;return (int)(h%100);}}
 public static int Points(int smooth,int lane,bool precise,bool request){return Math.Min(2,Math.Max(0,smooth))+Math.Min(2,Math.Max(0,lane))+(precise?1:0)+(request?1:0);}
 public static int Chance(int points,int streak){
  int bonus=Math.Min(Math.Max(0,StreakChanceMax),Math.Max(0,streak)*Math.Max(0,StreakChancePerTrip));
  return Math.Max(0,Math.Min(Math.Min(100,TipChanceMax),TipBaseChance+points*TipChancePerPoint+bonus));
 }
 public static int Amount(int points,bool precise){
  int amount=TipBase+points/2*TipPerTwoPoints+(precise?TipPreciseBonus:0);
  return Math.Max(0,Math.Min(TipMax,amount))/10*10;
 }
 // Returns 0 when the trip is not eligible or the roll misses.
 public static int Tip(string tripId,bool eligible,int points,bool precise,int streak,out int chance){
  chance=eligible?Chance(points,streak):0;
  if(!eligible||string.IsNullOrEmpty(tripId)||Roll(tripId+":tip")>=chance)return 0;
  return Amount(points,precise);
 }
 static readonly string[][] Lines={
  new[]{"멈출 때 하나도 안 쏠리네요.","살살 서 주셔서 편하네요."},
  new[]{"깜빡이 켜고 부드럽게 들어가시네요.","차선 바꾸는 것도 안 불안하네요."},
  new[]{"딱 여기 내려 주셨네요!","문 앞에 딱 맞춰 주셨네요."}};
 static readonly string[] TipLines={"편하게 왔어요. 이건 커피값이에요.","운전 참 잘하시네요. 잔돈은 넣어 두세요.","덕분에 멀미 안 했어요. 조금 더 드릴게요."};
 public static string Label(int kind){return kind==Smooth?"부드러운 정차":kind==Lane?"깔끔한 차선 변경":"칼정차";}
 public static string Line(int kind,string key){var set=Lines[Math.Max(0,Math.Min(2,kind))];return set[Roll(key)%set.Length];}
 public static string TipLine(string tripId){return TipLines[Roll(tripId+":line")%TipLines.Length];}
 // Same praise in one trip fades: 1st sound and maybe a line, 2nd sound only, then silent.
 public static int FeedbackLevel(int occurrence){return occurrence<=1?2:occurrence==2?1:0;}
}

// Physics-tick observer owned by DrivingManners (host/authority only; replicas receive the snapshot).
public sealed class GoodDrivingTracker {
 const int Size=320;
 readonly float[] times=new float[Size],decels=new float[Size];readonly bool[] harsh=new bool[Size];int head,count;
 float lastFast=-999,previousSpeed;
 // HasStop, not a negative time: scaled game time can still be 0 right after the preparation pause.
 public bool HasStop;public float StopTime;public Vector3 StopPosition;public bool StopSmooth;
 // Lane history sampled every 0.1 s.
 const int LaneSize=60;readonly float[] laneTimes=new float[LaneSize];readonly Vector3[] lanePositions=new Vector3[LaneSize],laneHeadings=new Vector3[LaneSize];int laneHead,laneCount;
 string lane;float laneSample=-1,lastLanePraise=-999;int indicator;float indicatorSince;
 public void Reset(){head=count=laneHead=laneCount=0;lastFast=-999;previousSpeed=0;HasStop=false;lane=null;laneSample=-1;indicator=0;indicatorSince=0;}
 public bool HarshSince(float time){for(int i=0;i<count;i++){int k=(head-1-i+Size)%Size;if(times[k]<time)break;if(harsh[k])return true;}return false;}
 float PeakDecel(float from,float to){float peak=0;for(int i=0;i<count;i++){int k=(head-1-i+Size)%Size;if(times[k]<from)break;if(times[k]<=to)peak=Mathf.Max(peak,decels[k]);}return peak;}
 // speed is the signed forward speed; acceleration the filtered travel acceleration used by the harsh rule.
 public void Observe(float now,float speed,float acceleration,bool harshMotion,Vector3 position){
  float decel=speed>=0?-acceleration:acceleration;
  times[head]=now;decels[head]=Mathf.Max(0,decel);harsh[head]=harshMotion;head=(head+1)%Size;count=Mathf.Min(Size,count+1);
  float abs=Mathf.Abs(speed);if(abs*3.6f>=GoodDrivingRules.SmoothMinKph)lastFast=now;
  if(abs<.56f&&previousSpeed>=.56f){
   float window=GoodDrivingRules.SmoothWindowSeconds;
   HasStop=true;StopTime=now;StopPosition=position;
   StopSmooth=now-lastFast<=GoodDrivingRules.SmoothRecentSeconds&&!HarshSince(now-window)&&
    PeakDecel(now-window,now)<=GoodDrivingRules.SmoothMaxDecel&&PeakDecel(now-.5f,now)<=GoodDrivingRules.SmoothFinalDecel;
  }
  previousSpeed=abs;
 }
 // A stop counts once, only while the taxi is still standing where it stopped.
 public bool TakeSmoothStop(float now,Vector3 position){
  bool ok=HasStop&&now-StopTime<=30&&Vector3.Distance(position,StopPosition)<=1.5f&&StopSmooth;HasStop=false;return ok;
 }
 public bool LaneDue(float now){return laneSample<0||now-laneSample>=.1f;}
 public void ObserveIndicator(float now,int direction){if(direction!=indicator){indicator=direction;indicatorSince=now;}}
 // Returns +1/-1 for a clean lane change to the right/left, 0 otherwise.
 public int ObserveLane(float now,string laneId,Vector3 position,Vector3 heading,float speed){
  if(laneSample>=0&&now-laneSample<.1f)return 0;laneSample=now;
  laneTimes[laneHead]=now;lanePositions[laneHead]=position;laneHeadings[laneHead]=heading;laneHead=(laneHead+1)%LaneSize;laneCount=Mathf.Min(LaneSize,laneCount+1);
  bool valid=!string.IsNullOrEmpty(laneId)&&!laneId.StartsWith("join:",StringComparison.Ordinal);
  string previous=lane;lane=valid?laneId:null;
  if(previous==null||!valid||previous==laneId)return 0;
  if(Mathf.Abs(speed)*3.6f<GoodDrivingRules.LaneMinKph||now-lastLanePraise<GoodDrivingRules.LaneCooldown)return 0;
  // Compare with the pose about 1.5 s earlier: a real lane change moves sideways with little turning;
  // a split of the same lane (new id, no sideways motion) or a junction turn does not qualify.
  int found=-1;for(int i=0;i<laneCount;i++){int k=(laneHead-1-i+LaneSize)%LaneSize;if(now-laneTimes[k]>=1.5f){found=k;break;}}
  if(found<0)return 0;
  var start=lanePositions[found];var startHeading=laneHeadings[found];
  float lateral=Vector3.Dot(position-start,TrafficSystem.Right(startHeading));
  if(Mathf.Abs(lateral)<.8f||Vector3.Angle(startHeading,heading)>12)return 0;
  int side=lateral>0?1:-1;
  if(indicator!=side||now-indicatorSince<GoodDrivingRules.LaneSignalLead||HarshSince(now-3))return 0;
  lastLanePraise=now;return side;
 }
}
