using System;
using UnityEngine;

public partial class DrivingLaw {
 // Explicit game-observation sites. This number is not a claimed statutory slow speed.
 public static bool SlowTurnZoneEnabled(string mapId,string junction){return !string.IsNullOrEmpty(junction)&&(mapId=="pvp-city-a"&&junction.StartsWith("city-junction:")||mapId=="eungye"&&(junction=="J7841_867"||junction=="J8066_6364"));}
 public string SlowTurnHint {get{return entry!=null&&entry.slow!=null?"우회전 게임 서행 기준 "+SlowRightTurnState.GameLimitKmh+"km/h":"";}}
 static SlowRightTurnState RestoreSlowTurn(LawEntrySnapshot e){if(!e.slowPresent)return null;if(e.slow==null||e.necessaryBefore<0)throw new ArgumentException("Missing slow-turn evidence");e.slow.Validate();return e.slow.Copy();}
 void ObserveSlowTurn(){
  if(entry==null||session==null||session.world==null||!SlowTurnZoneEnabled(session.world.map.mapId,entry.junction.name))return;
  if(entry.slow==null)entry.slow=new SlowRightTurnState();
  var taxi=session.taxi;float side=Vector3.Dot(taxi.transform.forward,TrafficSystem.Right(entry.direction)),forward=Vector3.Dot(taxi.transform.forward,entry.direction);
  bool rightTurning=side>.17f&&forward>-.1f&&Vector3.Dot(taxi.body.velocity,taxi.transform.forward)>.1f;
  bool necessary=session.manners&&session.manners.necessaryBrakeEvents>entry.necessaryBefore;
  entry.slow.Observe(Time.fixedDeltaTime,taxi.body.velocity.magnitude*3.6f,rightTurning,entry.observed.known,necessary);
 }
 void AddSlowTurnFinding(LawEvent incident){
  if(entry==null||entry.slow==null)return;
  var v=entry.slow.Verdict;var verdict=v==SlowRightTurnVerdict.Violation?LawVerdict.Violation:v==SlowRightTurnVerdict.Compliant?LawVerdict.Compliant:v==SlowRightTurnVerdict.Deferred?LawVerdict.Deferred:LawVerdict.NoOpportunity;
  incident.Add("게임 우회전 서행",verdict,verdict==LawVerdict.Violation?5:0);
  incident.reason+=" · 게임 서행 관찰 "+SlowRightTurnState.GameLimitKmh+"km/h, 여유2km/h 초과0.75초. 법정 서행속도 재현 아님";
 }
}
