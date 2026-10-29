using System;
using UnityEngine;
using Pvp.Map;
using Pvp.Network;
namespace Pvp.Traffic {
[Serializable] public struct PvpTrafficActor {
 public int Id,Kind,Edge,Xmm,Zmm,Yaw,SpeedMm,StopReason;public bool Active;
 public Vector3 Position {get{return new Vector3(Xmm/1000f,0,Zmm/1000f);}}
 public string Signature {get{return Id+","+Kind+","+Edge+","+Xmm+","+Zmm+","+SpeedMm+","+StopReason+","+Active;}}
}
// Instantiated and advanced on the authority only. Clients receive actor snapshots.
public sealed class PvpNpcAuthority {
 readonly PvpMapRuntime map;readonly float[] distance={150,300};readonly int[] reasons=new int[2],speeds=new int[2];int lastTick;
 int pedestrianZ=-65000,pedestrianTarget=-55000,pedestrianSpeed,pedestrianReason,lastPedestrianCycle=-1;bool pedestrianCrossing;
 public PvpNpcAuthority(PvpMapRuntime arena){if(arena==null||arena.Nodes.Count<12||arena.Connections.Count<18)throw new ArgumentException("Circuit traffic layout required");map=arena;int[] from={0,4,1,5,2,6,3,7},to={4,1,5,2,6,3,7,0};var nodes=new[]{new Vector3(-60,0,-60),new Vector3(60,0,-60),new Vector3(60,0,60),new Vector3(-60,0,60),new Vector3(0,0,-60),new Vector3(60,0,0),new Vector3(0,0,60),new Vector3(-60,0,0)};for(int i=0;i<8;i++)if(map.Nodes[i]!=nodes[i]||map.Connections[i].From!=from[i]||map.Connections[i].To!=to[i])throw new ArgumentException("NPC/rule trial requires circuit01 geometry");if(map.Connections[16].From!=4||map.Connections[16].To!=6||map.Connections[8].From!=4||map.Connections[8].To!=8)throw new ArgumentException("NPC/rule trial requires circuit01 approaches");}
 public void Advance(int tick,PvpDriveSnapshot[] players){if(tick<lastTick||tick-lastTick>10000)throw new ArgumentException("Invalid traffic tick");for(int t=lastTick+1;t<=tick;t++){for(int i=0;i<2;i++){
  float d=distance[i],step=.12f;int edge=(int)(d/60)%8;float fraction=(d%60)/60;Vector3 position=map.Position(new PvpRoadPosition(edge,fraction)),forward=map.Forward(new PvpRoadPosition(edge,fraction));reasons[i]=0;
  if(edge==0&&d<56&&(PvpTrafficAuthority.At(t).Phase==1||pedestrianCrossing)){step=Mathf.Min(step,Mathf.Max(0,(d<=50?50:d)-d));if(step<.12f)reasons[i]=1;}
  float gap=(distance[1-i]-d+480)%480;if(gap<8.12f){step=Mathf.Min(step,Mathf.Max(0,gap-8));reasons[i]=2;}
  if(players!=null)foreach(var player in players){if(!player.Connected)continue;var delta=player.State.Position-position;float ahead=Vector3.Dot(delta,forward);float lateral=(delta-forward*ahead).magnitude;if(ahead>0&&ahead<7.12f&&lateral<3.6f){step=Mathf.Min(step,Mathf.Max(0,ahead-7));reasons[i]=3;}}
  distance[i]=(d+step)%480;speeds[i]=Mathf.RoundToInt(step/.02f*1000);
 }AdvancePedestrian(t,players);}lastTick=tick;}
 Vector3 NpcPosition(int i){return map.Position(new PvpRoadPosition((int)(distance[i]/60)%8,(distance[i]%60)/60));}
 bool Occupied(Vector3 point,float radius,PvpDriveSnapshot[] players){for(int i=0;i<2;i++)if(Vector3.Distance(NpcPosition(i),point)<radius)return true;if(players!=null)foreach(var p in players)if(p.Connected&&Vector3.Distance(p.State.Position,point)<radius)return true;return false;}
 void AdvancePedestrian(int tick,PvpDriveSnapshot[] players){pedestrianSpeed=0;pedestrianReason=0;int cycle=tick/500;if(!pedestrianCrossing){if(PvpTrafficAuthority.At(tick).Phase==0||cycle==lastPedestrianCycle)return;if(Occupied(new Vector3(-6,0,-60),3.6f,players)){pedestrianReason=3;return;}pedestrianTarget=pedestrianZ < -60000?-55000:-65000;pedestrianCrossing=true;lastPedestrianCycle=cycle;}
  int direction=Math.Sign(pedestrianTarget-pedestrianZ),next=pedestrianZ+direction*Math.Min(40,Math.Abs(pedestrianTarget-pedestrianZ));if(Occupied(new Vector3(-6,0,next/1000f),3.3f,players)){pedestrianReason=3;return;}pedestrianZ=next;pedestrianSpeed=2000;if(pedestrianZ==pedestrianTarget)pedestrianCrossing=false;
 }
 public PvpTrafficSnapshot Snapshot(){var state=PvpTrafficAuthority.At(lastTick);state.Npcs=new PvpTrafficActor[2];for(int i=0;i<2;i++){int edge=(int)(distance[i]/60)%8;var road=new PvpRoadPosition(edge,(distance[i]%60)/60);var pos=map.Position(road);var forward=map.Forward(road);state.Npcs[i]=new PvpTrafficActor{Id=i,Kind=1,Edge=edge,Xmm=Mathf.RoundToInt(pos.x*1000),Zmm=Mathf.RoundToInt(pos.z*1000),Yaw=Mathf.RoundToInt(Mathf.Atan2(forward.x,forward.z)*Mathf.Rad2Deg),SpeedMm=speeds[i],StopReason=reasons[i],Active=true};}
  state.Pedestrians=new[]{new PvpTrafficActor{Id=0,Kind=2,Edge=0,Xmm=-6000,Zmm=pedestrianZ,Yaw=pedestrianTarget==-55000?0:180,SpeedMm=pedestrianSpeed,StopReason=pedestrianReason,Active=pedestrianCrossing}};return state;
 }
 public static Vector3 PresentedPosition(PvpTrafficActor actor,int observationTick,int tick,PvpMapRuntime map){float dt=Mathf.Clamp((tick-observationTick)*.02f,0,.2f);if(actor.Kind==2)return actor.Position+Quaternion.Euler(0,actor.Yaw,0)*Vector3.forward*(actor.SpeedMm/1000f*dt);var e=map.Connections[actor.Edge];var forward=(map.Nodes[e.To]-map.Nodes[e.From]).normalized;float remaining=Vector3.Distance(actor.Position,map.Nodes[e.To]);return actor.Position+forward*Mathf.Min(remaining,actor.SpeedMm/1000f*dt);}
 public static bool Contact(PvpTrafficSnapshot state,PvpMapRuntime map,Vector3 from,Vector3 to,int tick,out int kind,out int id){kind=id=0;if(state.Npcs!=null)foreach(var actor in state.Npcs)if(actor.Active&&Near(from,to,PresentedPosition(actor,state.Tick,tick,map),3.6f)){kind=1;id=actor.Id;return true;}if(state.Pedestrians!=null)foreach(var actor in state.Pedestrians)if(actor.Active&&Near(from,to,PresentedPosition(actor,state.Tick,tick,map),2.6f)){kind=2;id=actor.Id;return true;}return false;}
 static bool Near(Vector3 from,Vector3 to,Vector3 center,float radius){var delta=to-from;float fromDistance=(from-center).sqrMagnitude,toDistance=(to-center).sqrMagnitude;if(fromDistance<radius*radius&&toDistance>fromDistance+1e-7f&&Vector3.Dot(from-center,delta)>=0)return false;float t=delta.sqrMagnitude<.000001f?0:Mathf.Clamp01(Vector3.Dot(center-from,delta)/delta.sqrMagnitude);return Vector3.Distance(from+delta*t,center)<radius;}
}
}
