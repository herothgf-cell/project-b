using System;
using UnityEngine;
using Pvp.Map;
using Pvp.Network;
using Pvp.Driving;
namespace Pvp.Tests.EditMode {
public static class PvpDrivingTrafficTests {
 static void Check(bool ok,string reason){if(!ok)throw new Exception("PVP_DRIVING: "+reason);}
 public static void RunAll(){PvpCompetitionTests.Run();Run();PvpRuleTrafficTests.Run();}
 public static void Run(){var d=PvpMapDefinition.CreateDefault();try{var map=new PvpMapRuntime(d);int routes=0;float maximum=0;
  for(int spawn=0;spawn<4;spawn++)for(int stop=0;stop<4;stop++){
   var motor=new PvpNetworkMotor(map,spawn);var pilot=new PvpDrivingPilot(map);int tick=0;
   for(;tick<9000&&!pilot.Arrived(motor.Capture(),stop);tick++){var input=pilot.Input(motor.Capture(),stop,tick);motor.Step(input,tick);}
   var pose=motor.Capture();Check(pilot.Arrived(pose,stop),"route spawn="+spawn+" stop="+stop+" tick="+tick+" edge="+pose.Edge+" fraction="+pose.Fraction+" pos="+pose.State.Position+" yaw="+pose.State.YawDegrees+" speed="+pose.State.Speed+" boundary="+motor.BoundaryStops);
   Check(motor.TravelDistance>40&&pilot.ThrottleFrames>0&&pilot.SteeringFrames>0&&pilot.BrakeFrames>0,"actual bounded inputs and accepted distance");maximum=Mathf.Max(maximum,tick*.02f);routes++;
  }
  for(int spawn=0;spawn<4;spawn++)for(int destination=0;destination<4;destination++)if(spawn!=destination){
   var motor=new PvpNetworkMotor(map,spawn,true);var pilot=new PvpDrivingPilot(map);var traffic=new Pvp.Traffic.PvpNpcAuthority(map);var ledger=new Pvp.Competition.PvpScoreLedger();var law=new Pvp.Rules.PvpRuleAuthority(map,"driving-unit",(p,id,rev,amount,t)=>ledger.ApplyIncident(p,id,rev,amount,t));int target=spawn,leg=0,tick=1;
   for(;tick<14000;tick++){var state=motor.Capture();state.Connected=true;traffic.Advance(tick,new[]{state});var shared=traffic.Snapshot();motor.ObserveTraffic(shared);pilot.ObserveTraffic(shared);if(pilot.Arrived(state,target)){if(leg==1)break;target=destination;leg=1;}motor.Step(pilot.Input(state,target,tick),tick);var observed=motor.Capture();observed.Connected=true;law.Observe(0,state,observed,motor.LastMotion,shared,true);}
   var final=motor.Capture();Check(leg==1&&pilot.Arrived(final,destination),"continuous traffic route spawn="+spawn+" destination="+destination+" tick="+tick+" edge="+final.Edge+" pos="+final.State.Position+" yaw="+final.State.YawDegrees+" speed="+final.State.Speed+" boundary="+motor.BoundaryStops+" leg="+leg);Check(ledger.Read(0).Penalty==0,"lawful input driver penalty="+ledger.Read(0).Penalty+" spawn="+spawn+" destination="+destination+" evidence="+(law.Recent().Length>0?law.Recent()[law.Recent().Length-1].Evidence:"none"));Debug.Log("PVP_CONTINUOUS_ROUTE spawn="+spawn+" destination="+destination+" seconds="+(tick*.02f)+" distance="+motor.TravelDistance+" boundaries="+motor.BoundaryStops);
  }
  Debug.Log("PVP_INPUT_DRIVING_MODEL_OK routes="+routes+" maxSeconds="+maximum);
 }finally{UnityEngine.Object.DestroyImmediate(d);}}
}}
