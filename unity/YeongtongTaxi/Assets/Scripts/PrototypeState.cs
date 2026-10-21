using System;
using System.Collections.Generic;

// Gameplay proposal values, not statutory fines or real-world prices.
[Serializable] public class PrototypeSettings {
 public int days=3,target=30000,startCash=10000,meal=3000,parking=500,insurance=6000,tax=1500;
 public float daySeconds=720,initialFuel=60,fuelPerKm=4,idlePerMinute=.1f,fuelUnitPrice=40;
 public PrototypeSettings WithCurrentOperationBalance(){var copy=(PrototypeSettings)MemberwiseClone();var current=new PrototypeSettings();copy.daySeconds=current.daySeconds;copy.initialFuel=current.initialFuel;copy.fuelPerKm=current.fuelPerKm;copy.idlePerMinute=current.idlePerMinute;return copy;}
 public int insuranceExpiryDay=2,taxDueDay=3;
 public int redStraightPenalty=20,redRightPenalty=15,dedicatedRedPenalty=20,pedestrianPenalty=25,insertionPenalty=15,interferencePenalty=15;
 public const int SaveVersion=2;
 public const string SaveFile="campaign-v2.json";
}
[Serializable] public class PrototypeIds {
 public string campaign; public int sequence;
 public PrototypeIds(string campaign){if(string.IsNullOrEmpty(campaign))throw new ArgumentException("campaign id required");this.campaign=campaign;}
 public string Next(string kind){return campaign+":"+kind+":"+(++sequence);}
 public string Settlement(int day){return campaign+":settlement:"+day;}
}
public enum LawVerdict { Compliant, Violation, Deferred, NoOpportunity }
public enum PedestrianState { None, Intending, Crossing, Unknown }
[Serializable] public class RoadObservation {
 public bool known,trafficClear,dedicated,pedestrianGreen;
 public SignalColor forward,right;
 public PedestrianState pedestrian;
 public bool PedestrianClear {get{return known&&pedestrian==PedestrianState.None;}}
 public bool MayTurnRight(bool stopped){
  if(!known||!trafficClear||!PedestrianClear)return false;
  return RightSignalPermits(stopped);
 }
 public bool RightSignalPermits(bool stopped){
  if(!known)return false;
  if(dedicated)return right==SignalColor.Green;
  return forward==SignalColor.Green||(forward==SignalColor.Red&&stopped);
 }
 public RoadObservation Copy(){return (RoadObservation)MemberwiseClone();}
}
[Serializable] public class LawFinding {
 public string rule;public LawVerdict verdict;public int penalty;
 public LawFinding Copy(){return (LawFinding)MemberwiseClone();}
}
[Serializable] public class LawEvent {
 public PracticeRoute practice;
 public string id,tripId,family,scenario,junction,reason,action;
 public int day;public float seconds,speed;public bool assisted,occupied;
 public RoadObservation observation;
 public List<LawFinding> rules=new List<LawFinding>();
 public void Add(string rule,LawVerdict verdict,int penalty){foreach(var r in rules)if(r.rule==rule)return;rules.Add(new LawFinding{rule=rule,verdict=verdict,penalty=penalty});}
 public int Deduction {get{int max=0;foreach(var r in rules)if(r.verdict==LawVerdict.Violation)max=Math.Max(max,r.penalty);return max;}}
 public LawEvent Copy(){var c=(LawEvent)MemberwiseClone();c.rules=rules.ConvertAll(r=>r.Copy());c.observation=observation==null?null:observation.Copy();c.practice=practice==null?null:practice.Copy();return c;}
}
[Serializable] public class PracticeRoute {
 public string junction;public int axis;public float stop,exit;public float[] points;
 public PracticeRoute Copy(){var c=(PracticeRoute)MemberwiseClone();c.points=points==null?null:(float[])points.Clone();return c;}
}
[Serializable] public class PracticeResult {public string sourceId,action,latestOutcome,feedback;public int events,score,validFindings;public string EvaluationText(){if(!string.IsNullOrEmpty(latestOutcome))return latestOutcome;return validFindings==0?"유효 평가 없음":score+"점 / 유효 "+validFindings+"건";}}
[Serializable] public class PassengerResult {
 public string tripId,pickup,destination,outcome,request,mannerReason;
 public int day,quoted,paid,score,required;public float elapsed;
}
// No dependency on passenger manners, fares, Unity time, or physics.
public class LawLedger {
 readonly List<LawEvent> events=new List<LawEvent>();
 public LawEvent[] Events {get{return events.ConvertAll(e=>e.Copy()).ToArray();}}
 public bool Record(LawEvent e){
  if(e==null||string.IsNullOrEmpty(e.id)||e.day<1)throw new ArgumentException("Invalid law event");
  foreach(var old in events)if(old.id==e.id)return false;
  events.Add(e.Copy());return true;
 }
 public int Score(int day){int score=100;foreach(var e in events)if(e.day==day)score-=e.Deduction;return Math.Max(0,score);}
 public string DaySummary(int day){int count=0,good=0,bad=0,deferred=0,none=0;foreach(var e in events){if(e.day!=day)continue;count++;foreach(var r in e.rules){if(r.verdict==LawVerdict.Compliant)good++;else if(r.verdict==LawVerdict.Violation)bad++;else if(r.verdict==LawVerdict.Deferred)deferred++;else none++;}}
  if(count==0)return "평가 기록 없음";
  return (good+bad==0?"유효 평가 없음":Score(day)+"점")+" · 사건 "+count+" / 유효 "+(good+bad)+"\n규칙별 준수 "+good+" · 위반 "+bad+" · 보류 "+deferred+" · 기회 없음 "+none;
 }
 public int ValidForDay(int day){int count=0;foreach(var e in events)if(e.day==day)foreach(var r in e.rules)if(r.verdict==LawVerdict.Compliant||r.verdict==LawVerdict.Violation)count++;return count;}
 public int ValidCount(string family,string rule){return Recent(family,rule,int.MaxValue).Count;}
 List<LawFinding> Recent(string family,string rule,int limit){var result=new List<LawFinding>();for(int i=events.Count-1;i>=0&&result.Count<limit;i--)if(events[i].family==family)foreach(var r in events[i].rules)if(r.rule==rule&&(r.verdict==LawVerdict.Compliant||r.verdict==LawVerdict.Violation))result.Add(r);return result;}
 public string Summary(string family,string rule){var recent=Recent(family,rule,5);if(recent.Count<5)return "기록 부족 · 유효 관찰 "+recent.Count+"/5건";int violations=recent.FindAll(r=>r.verdict==LawVerdict.Violation).Count;return "최근 유효 관찰 5건 · 준수 "+(5-violations)+" / 위반 "+violations+"건";}
}
public static class LawEvaluator {
 public static LawEvent Exit(string id,int day,bool knownSignalQueue,bool changedLane,bool insertedAhead,bool obstructed,PrototypeSettings settings=null){
  var s=settings??new PrototypeSettings();var e=new LawEvent{id=id,day=day,family="exit",action=insertedAhead?"앞부분 진입":changedLane?"뒤 합류":"직진·우회",reason="신호 대기열 "+(knownSignalQueue?"확인":"불명확")+" · 차로 진입 "+(changedLane?"있음":"없음")+" · 앞부분 진입 "+(insertedAhead?"있음":"없음")+" · 상대 진행 방해 "+(obstructed?"관찰":"미관찰")};
  e.Add("L06",!knownSignalQueue?LawVerdict.Deferred:!changedLane?LawVerdict.NoOpportunity:insertedAhead?LawVerdict.Violation:LawVerdict.Compliant,s.insertionPenalty);
  e.Add("L07",!knownSignalQueue?LawVerdict.Deferred:!changedLane?LawVerdict.NoOpportunity:obstructed?LawVerdict.Violation:LawVerdict.Compliant,s.interferencePenalty);return e;
 }
 public static LawEvent RightTurn(string id,int day,RoadObservation o,bool stopped,PrototypeSettings settings=null,bool yielded=false){
  var s=settings??new PrototypeSettings();var e=new LawEvent{id=id,day=day,family="right",observation=o.Copy(),action="우회전",reason="관찰한 신호·보행 상태와 정지 여부"};
  if(!o.known||o.pedestrian==PedestrianState.Unknown){e.Add("L02",LawVerdict.Deferred,0);e.reason="관찰 불명확 · 판정 보류";return e;}
  if(o.dedicated)e.Add("L03",o.right==SignalColor.Green?LawVerdict.Compliant:o.right==SignalColor.Red?LawVerdict.Violation:LawVerdict.Deferred,s.dedicatedRedPenalty);
  else e.Add("L02",o.forward==SignalColor.Red?(stopped?LawVerdict.Compliant:LawVerdict.Violation):LawVerdict.NoOpportunity,s.redRightPenalty);
  // Evaluate at passage: stopping earlier does not grant permission to drive through a pedestrian.
  e.Add("L04",o.pedestrian==PedestrianState.None?(yielded?LawVerdict.Compliant:LawVerdict.NoOpportunity):LawVerdict.Violation,s.pedestrianPenalty);
  e.Add("L08",LawVerdict.Deferred,0);return e;
 }
}
