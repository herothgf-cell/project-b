using System;
using System.Collections.Generic;
using UnityEngine;
using Pvp.Map;
using Pvp.Driving;
namespace Pvp.Network {
[Serializable] public struct PvpDriveCommand {
 public int Sequence,Epoch; public float Throttle,Brake,Steering; public bool Reverse,Connected;
 public PvpVehicleInput Input {get{return new PvpVehicleInput(Throttle,Brake,Steering,Reverse,Connected);}}
 public bool Valid {get{return Sequence>0&&Finite(Throttle)&&Finite(Brake)&&Finite(Steering)&&Throttle>=0&&Throttle<=1&&Brake>=0&&Brake<=1&&Mathf.Abs(Steering)<=1;}}
 static bool Finite(float f){return !float.IsNaN(f)&&!float.IsInfinity(f);}
}
// Commands contain held control state. Coalesce bursts instead of replaying old controls
// over future server ticks. Authority still advances exactly once per tick.
public sealed class PvpInputBuffer {
 readonly Queue<PvpDriveCommand> pending=new Queue<PvpDriveCommand>();int received,silentTicks;PvpDriveCommand held;
 public const int FreshTicks=10; // 200ms trial threshold at50Hz, not a product guarantee.
 public int Count {get{return pending.Count;}}
 public void Enqueue(PvpDriveCommand command){if(!command.Valid||command.Sequence!=received+1||pending.Count>=128)throw new ArgumentException("invalid input sequence/range/backlog");pending.Enqueue(command);received=command.Sequence;}
 public PvpVehicleInput InputForTick(out int acknowledgement){PvpDriveCommand latest;if(TakeLatest(out latest)){held=latest;silentTicks=0;}else silentTicks=Math.Min(FreshTicks,silentTicks+1);acknowledgement=held.Sequence;return silentTicks<FreshTicks?held.Input:new PvpVehicleInput(0,0,0,false,false);}
 public bool TakeLatest(out PvpDriveCommand command){command=default(PvpDriveCommand);if(pending.Count==0)return false;while(pending.Count>0)command=pending.Dequeue();return true;}
}
[Serializable] public struct PvpDriveSnapshot {public ulong Owner; public int Ack,Tick,Edge,Epoch,PassengerIndex;public bool Connected;public float Fraction;public PvpVehicleState State;}
// Shared authoritative and replay step: motor plus directed road anchor.
public sealed class PvpNetworkMotor {
 public Pvp.Rules.PvpMotionTrace LastMotion {get;private set;}
 public const float Delta=.02f;public float TravelDistance {get;private set;}public int BoundaryStops {get;private set;}public void ResetAudit(){TravelDistance=0;BoundaryStops=0;}
 Pvp.Traffic.PvpTrafficSnapshot traffic;public void ObserveTraffic(Pvp.Traffic.PvpTrafficSnapshot value){traffic=value;}
 readonly bool sharedWorld;readonly PvpMapRuntime map;readonly PvpVehicleMotor motor=new PvpVehicleMotor();PvpRoadPosition road;
 public PvpNetworkMotor(PvpMapRuntime m,int slot,bool shared=false){sharedWorld=shared;map=m;int node=m.Spawns[slot%m.Spawns.Count];int edge=-1;for(int i=0;i<m.Connections.Count;i++)if(m.Connections[i].From==node){edge=i;break;}if(edge<0)throw new ArgumentException("No spawn edge");road=new PvpRoadPosition(edge,.05f);Vector3 forward=m.Forward(road);var state=new PvpVehicleState{Position=m.Position(road),YawDegrees=Mathf.Atan2(forward.x,forward.z)*Mathf.Rad2Deg};motor.Restore(in state);}
 public void Step(PvpVehicleInput input,int worldTick=-1){var before=motor.Capture();motor.Simulate(in input,Delta);var trace=new Pvp.Rules.PvpMotionTrace{Tick=worldTick,Applied=input.Connected,Brake=input.Brake,Attempted=motor.Capture().Position};int kind=0,id=0;bool blocked=false;
  if(sharedWorld&&worldTick>=0){if(Traffic.PvpTrafficAuthority.Blocks(before.Position,motor.Capture().Position,worldTick)){kind=3;blocked=true;}else blocked=Traffic.PvpNpcAuthority.Contact(traffic,map,before.Position,motor.Capture().Position,worldTick,out kind,out id);}
  trace.ContactKind=kind;trace.ContactId=id;
  if(blocked){before.Speed=0;motor.Restore(in before);LastMotion=trace;return;}
  PvpRoadPosition next;if(map.TryLocate(motor.Capture().Position,road,out next)){road=next;TravelDistance+=Vector3.Distance(before.Position,motor.Capture().Position);}else{BoundaryStops++;trace.Boundary=true;before.Speed=0;motor.Restore(in before);}LastMotion=trace;
 }

 public PvpDriveSnapshot Capture(){return new PvpDriveSnapshot{State=motor.Capture(),Edge=road.Edge,Fraction=road.Fraction};}
 public void Restore(PvpDriveSnapshot s){var next=new PvpRoadPosition(s.Edge,s.Fraction);if(!map.IsLegalTransition(next,next))throw new ArgumentException("Invalid road snapshot");motor.Restore(in s.State);road=next;}
}
public sealed class PvpPrediction {
 readonly PvpNetworkMotor motor;readonly List<PvpDriveCommand> pending=new List<PvpDriveCommand>();int ack,lastTick=-1,next;
 public int PendingCount {get{return pending.Count;}} public float MaxCorrection {get;private set;}
 public PvpPrediction(PvpMapRuntime map,int slot,bool shared=false){motor=new PvpNetworkMotor(map,slot,shared);}
 public PvpDriveSnapshot State {get{return motor.Capture();}}public void ObserveTraffic(Pvp.Traffic.PvpTrafficSnapshot value){motor.ObserveTraffic(value);}
 public PvpDriveCommand Predict(PvpVehicleInput input){if(pending.Count>=128)throw new InvalidOperationException("Authority timeout: input history full");var cmd=new PvpDriveCommand{Sequence=++next,Throttle=input.Throttle,Brake=input.Brake,Steering=input.Steering,Reverse=input.ReverseRequested,Connected=input.Connected};if(!cmd.Valid)throw new ArgumentException("Invalid input");pending.Add(cmd);motor.Step(input,Math.Max(0,lastTick)+pending.Count);return cmd;}
 public void Reconcile(PvpDriveSnapshot s){if(s.Tick<=lastTick||s.Ack<ack)return;if(s.Ack>next)throw new ArgumentException("Future acknowledgement");var before=motor.Capture().State.Position;motor.Restore(s);pending.RemoveAll(c=>c.Sequence<=s.Ack);int replayTick=s.Tick;foreach(var c in pending)motor.Step(c.Input,++replayTick);MaxCorrection=Mathf.Max(MaxCorrection,Vector3.Distance(before,motor.Capture().State.Position));ack=s.Ack;lastTick=s.Tick;}
}
[Serializable] public sealed class PvpJoinHello {public string Protocol,Build,Map,Rules,Ticket;}
public static class PvpAdmission {
 public const string Protocol="pvp-drive-4",Rules="50hz-A-no-contact-npc-rules-v4";
 public static string Build {get{return string.IsNullOrEmpty(Application.buildGUID)?"editor-driving-lab-1":Application.buildGUID;}}
 public static string Reject(PvpJoinHello hello,string map){if(hello==null)return "missing handshake";if(hello.Protocol!=Protocol)return "protocol mismatch";if(hello.Build!=Build)return "build mismatch";if(hello.Map!=map)return "map mismatch";if(hello.Rules!=Rules)return "settings mismatch";return null;}
 public static PvpJoinHello Hello(string map){return new PvpJoinHello{Protocol=Protocol,Build=Build,Map=map,Rules=Rules};}
}
}