using System;
using UnityEngine;
using Pvp.Map;
using Pvp.Network;
namespace Pvp.Driving {
// Input-only route follower for local trial opponents and explicit probes; never writes vehicle state.
public sealed class PvpDrivingPilot {
 Pvp.Traffic.PvpTrafficSnapshot traffic;readonly int[] rightHold;readonly bool[] rightServed;public bool ObeyTraffic=true;public float CruiseSpeed=8;public void ObserveTraffic(Pvp.Traffic.PvpTrafficSnapshot snapshot){traffic=snapshot;}
 readonly PvpMapRuntime map;
 public string Status {get;private set;}public int ThrottleFrames,SteeringFrames,BrakeFrames;
 public PvpDrivingPilot(PvpMapRuntime arena){map=arena??throw new ArgumentNullException(nameof(arena));rightHold=new int[map.SignalZones.Count];rightServed=new bool[rightHold.Length];}
 public int IncomingEdge(int stop){for(int e=0;e<map.Connections.Count;e++)if(map.Connections[e].To==map.Stops[stop].Node)return e;throw new ArgumentException("No stop approach");}
 public bool Arrived(PvpDriveSnapshot pose,int stop){var edge=IncomingEdge(stop);var forward=map.Forward(new PvpRoadPosition(edge,1));return pose.Edge==edge&&Vector3.Distance(pose.State.Position,map.Stops[stop].Position)<=2.5f&&Mathf.Abs(Mathf.DeltaAngle(pose.State.YawDegrees,Mathf.Atan2(forward.x,forward.z)*Mathf.Rad2Deg))<20&&Mathf.Abs(pose.State.Speed)<.35f;}
 public PvpVehicleInput Input(PvpDriveSnapshot pose,int stop,int tick){
  if(stop<0){Status="운송 완료 대기";return Count(new PvpVehicleInput(0,1,0));}
  int incoming=IncomingEdge(stop);var goal=map.Stops[stop].Position;var goalForward=map.Forward(new PvpRoadPosition(incoming,1));float endHeading=Mathf.Atan2(goalForward.x,goalForward.z)*Mathf.Rad2Deg;
  float distance=Vector3.Distance(pose.State.Position,goal);
  if(pose.Edge==incoming&&distance<=2.4f&&Mathf.Abs(Mathf.DeltaAngle(pose.State.YawDegrees,endHeading))<20){Status="현장 정차 유지";return Count(new PvpVehicleInput(0,1,0));}
  var road=new PvpRoadPosition(pose.Edge,pose.Fraction);var route=PvpRoutePlanner.FindRoute(map,road,map.Stops[stop].Node);
  if(!route.Found||route.Points.Length<2){Status="합법 경로 없음";return Count(new PvpVehicleInput(0,1,0));}
  float speed=Mathf.Abs(pose.State.Speed);float look=2.2f+Mathf.Max(0,speed-3)*.18f;var target=route.Points[route.Points.Length-1];
  float remaining=look;
  for(int i=1;i<route.Points.Length;i++){float length=Vector3.Distance(route.Points[i-1],route.Points[i]);if(remaining<=length){target=Vector3.Lerp(route.Points[i-1],route.Points[i],length>.001f?remaining/length:0);break;}remaining-=length;}
  var direction=target-pose.State.Position;float heading=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;float angle=Mathf.DeltaAngle(pose.State.YawDegrees,heading);float steering=Mathf.Clamp(angle/30,-1,1);
  float desired=ObeyTraffic?Mathf.Min(CruiseSpeed,map.SpeedLimit(pose.Edge)-.5f):CruiseSpeed;
  var edgeNow=map.Connections[pose.Edge];float toCorner=Vector3.Distance(map.Position(road),map.Nodes[edgeNow.To]);
  if(route.Points.Length>2){var next=route.Points[2]-route.Points[1];if(Vector3.Angle(map.Forward(road),next)>20&&toCorner<12)desired=2.8f;}
  if(Mathf.Abs(angle)>25)desired=Mathf.Min(desired,2.8f);
  if(pose.Edge==incoming)desired=Mathf.Min(desired,Mathf.Sqrt(12*Mathf.Max(0,distance-2.1f)));
  for(int z=0;z<map.SignalZones.Count;z++){var zone=map.SignalZones[z];if(!zone.Applies(pose.Edge)){rightHold[z]=0;rightServed[z]=false;continue;}var approach=map.Connections[zone.ApproachEdge];float length=Vector3.Distance(map.Nodes[approach.From],map.Nodes[approach.To]),along=Vector3.Dot(pose.State.Position-map.Nodes[approach.From],map.Forward(new PvpRoadPosition(zone.ApproachEdge,0))),line=zone.StopFraction*length;
   bool right=zone.RightTurnEdge>=0&&route.Nodes.Length>1&&route.Nodes[1]==map.Connections[zone.RightTurnEdge].To;
   if(right&&along>=line-4&&along<=line&&speed<.35f){rightHold[z]++;if(rightHold[z]>=15)rightServed[z]=true;}else if(!rightServed[z])rightHold[z]=0;
   bool red=traffic.NextChangeTick>0&&(zone.Phase(tick)==1||zone.NextChange(tick)-tick<150);
   if(ObeyTraffic&&(red||right&&!rightServed[z])&&along<line+.5f)desired=Mathf.Min(desired,Mathf.Sqrt(12*Mathf.Max(0,line-2-along)));
  }
  var pathForward=map.Forward(road);
  if(traffic.NextChangeTick>0){var relative=traffic.ObstaclePosition-pose.State.Position;float ahead=Vector3.Dot(relative,pathForward);float side=(relative-pathForward*ahead).magnitude;if(ahead>0&&ahead<10&&side<2.8f)desired=Mathf.Min(desired,Mathf.Sqrt(10*Mathf.Max(0,ahead-3.5f)));}
  if(traffic.Npcs!=null)foreach(var actor in traffic.Npcs){var relative=Pvp.Traffic.PvpNpcAuthority.PresentedPosition(actor,traffic.Tick,tick,map)-pose.State.Position;float ahead=Vector3.Dot(relative,pathForward);float side=(relative-pathForward*ahead).magnitude;if(ahead>0&&ahead<15&&side<3.8f)desired=Mathf.Min(desired,Mathf.Max(0,(ahead-7)*.7f+actor.SpeedMm/1000f));}
  if(traffic.Pedestrians!=null)foreach(var actor in traffic.Pedestrians)if(actor.Active){var relative=actor.Position-pose.State.Position;float ahead=Vector3.Dot(relative,pathForward);float side=(relative-pathForward*ahead).magnitude;if(ahead>0&&ahead<12&&side<4)desired=Mathf.Min(desired,Mathf.Sqrt(10*Mathf.Max(0,ahead-4)));}
  float throttle=desired>speed+.15f?Mathf.Clamp01((desired-speed)*.8f):0;float brake=speed>desired+.15f?Mathf.Clamp01((speed-desired)*.7f):0;
  Status="입력 경로 추종";return Count(new PvpVehicleInput(throttle,brake,steering));
 }
 PvpVehicleInput Count(PvpVehicleInput input){if(input.Throttle>0)ThrottleFrames++;if(input.Brake>0)BrakeFrames++;if(Mathf.Abs(input.Steering)>.1f)SteeringFrames++;return input;}
}
}
