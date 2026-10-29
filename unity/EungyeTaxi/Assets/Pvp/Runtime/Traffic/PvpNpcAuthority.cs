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
 readonly PvpMapRuntime map;readonly PvpNpcPath[] paths;readonly float[] distance,length;readonly int[] reasons,speeds;readonly Walk[] walks;int lastTick;
 sealed class Walk {public Vector3 Position;public bool Forward=true,Active;public int Speed,Reason,Cycle=-1;}
 public PvpNpcAuthority(PvpMapRuntime arena){
  if(arena==null||arena.NpcPaths.Count==0)throw new ArgumentException("Traffic routes required");map=arena;
  // Compatibility guard only for the archived circuit fixture. Expanded maps use authored data.
  if(map.LegacyLayout){int[] from={0,4,1,5,2,6,3,7},to={4,1,5,2,6,3,7,0};var nodes=new[]{new Vector3(-60,0,-60),new Vector3(60,0,-60),new Vector3(60,0,60),new Vector3(-60,0,60),new Vector3(0,0,-60),new Vector3(60,0,0),new Vector3(0,0,60),new Vector3(-60,0,0)};for(int i=0;i<8;i++)if(map.Nodes[i]!=nodes[i]||map.Connections[i].From!=from[i]||map.Connections[i].To!=to[i])throw new ArgumentException("Legacy fixture geometry changed");if(map.Connections[16].From!=4||map.Connections[16].To!=6||map.Connections[8].From!=4||map.Connections[8].To!=8)throw new ArgumentException("Legacy fixture approaches changed");}
  var definitions=map.NpcPaths;paths=new PvpNpcPath[definitions.Count];distance=new float[paths.Length];length=new float[paths.Length];reasons=new int[paths.Length];speeds=new int[paths.Length];for(int i=0;i<paths.Length;i++){paths[i]=definitions[i];distance[i]=paths[i].StartDistance;foreach(int e in paths[i].Edges)length[i]+=EdgeLength(e);}
  walks=new Walk[map.PedestrianPaths.Count];for(int i=0;i<walks.Length;i++)walks[i]=new Walk{Position=map.PedestrianPaths[i].From};
 }
 float EdgeLength(int edge){var e=map.Connections[edge];return Vector3.Distance(map.Nodes[e.From],map.Nodes[e.To]);}
 PvpRoadPosition Road(int i){float remaining=distance[i];foreach(int e in paths[i].Edges){float size=EdgeLength(e);if(remaining<size)return new PvpRoadPosition(e,remaining/size);remaining-=size;}return new PvpRoadPosition(paths[i].Edges[0],0);}
 bool SamePath(int a,int b){if(paths[a].Edges.Length!=paths[b].Edges.Length)return false;for(int k=0;k<paths[a].Edges.Length;k++)if(paths[a].Edges[k]!=paths[b].Edges[k])return false;return true;}
 public void Advance(int tick,PvpDriveSnapshot[] players){if(tick<lastTick||tick-lastTick>10000)throw new ArgumentException("Invalid traffic tick");for(int t=lastTick+1;t<=tick;t++){
  for(int i=0;i<paths.Length;i++){
   var road=Road(i);Vector3 position=map.Position(road),forward=map.Forward(road);float step=paths[i].Speed*.02f;reasons[i]=0;
   for(int z=0;z<map.SignalZones.Count;z++){var zone=map.SignalZones[z];if(!zone.Applies(road.Edge))continue;bool crossing=false;for(int p=0;p<walks.Length;p++)if(map.PedestrianPaths[p].SignalZone==z&&walks[p].Active)crossing=true;var canonical=map.Connections[zone.ApproachEdge];float canonicalLength=EdgeLength(zone.ApproachEdge),current=Vector3.Dot(position-map.Nodes[canonical.From],map.Forward(new PvpRoadPosition(zone.ApproachEdge,0))),line=zone.StopFraction*canonicalLength,entry=zone.EntryFraction*canonicalLength;if(current<entry&&(zone.Phase(t)==1||crossing)){step=Mathf.Min(step,Mathf.Max(0,current<=line?line-current:0));if(step<paths[i].Speed*.02f)reasons[i]=1;}}
   for(int j=0;j<paths.Length;j++)if(i!=j&&SamePath(i,j)){float gap=(distance[j]-distance[i]+length[i])%length[i];if(gap<8+step){step=Mathf.Min(step,Mathf.Max(0,gap-8));reasons[i]=2;}}
   if(players!=null)foreach(var player in players){if(!player.Connected)continue;var relative=player.State.Position-position;float ahead=Vector3.Dot(relative,forward),side=(relative-forward*ahead).magnitude;if(ahead>0&&ahead<7+step&&side<3.6f){step=Mathf.Min(step,Mathf.Max(0,ahead-7));reasons[i]=3;}}
   distance[i]=(distance[i]+step)%length[i];speeds[i]=Mathf.RoundToInt(step/.02f*1000);
  }
  for(int p=0;p<walks.Length;p++)AdvanceWalk(p,t,players);
 }lastTick=tick;}
 bool Occupied(Vector3 point,float radius,PvpDriveSnapshot[] players){for(int i=0;i<paths.Length;i++)if(Vector3.Distance(map.Position(Road(i)),point)<radius)return true;if(players!=null)foreach(var p in players)if(p.Connected&&Vector3.Distance(p.State.Position,point)<radius)return true;return false;}
 void AdvanceWalk(int index,int tick,PvpDriveSnapshot[] players){var path=map.PedestrianPaths[index];var walk=walks[index];var zone=map.SignalZones[path.SignalZone];int cycle=(tick+zone.OffsetTicks)/(zone.GreenTicks+zone.RedTicks);walk.Speed=walk.Reason=0;
  if(!walk.Active){if(zone.Phase(tick)==0||cycle==walk.Cycle)return;if(Occupied((path.From+path.To)*.5f,3.6f,players)){walk.Reason=3;return;}walk.Active=true;walk.Cycle=cycle;walk.Forward=Vector3.Distance(walk.Position,path.From)<.1f;}
  var target=walk.Forward?path.To:path.From;var next=Vector3.MoveTowards(walk.Position,target,path.Speed*.02f);if(Occupied(next,3.3f,players)){walk.Reason=3;return;}walk.Speed=Mathf.RoundToInt(Vector3.Distance(next,walk.Position)/.02f*1000);walk.Position=next;if(Vector3.Distance(next,target)<.001f)walk.Active=false;
 }
 public PvpTrafficSnapshot Snapshot(){var state=PvpTrafficAuthority.At(map,lastTick);state.Npcs=new PvpTrafficActor[paths.Length];for(int i=0;i<paths.Length;i++){var road=Road(i);var pos=map.Position(road);var forward=map.Forward(road);state.Npcs[i]=new PvpTrafficActor{Id=i,Kind=1,Edge=road.Edge,Xmm=Mathf.RoundToInt(pos.x*1000),Zmm=Mathf.RoundToInt(pos.z*1000),Yaw=Mathf.RoundToInt(Mathf.Atan2(forward.x,forward.z)*Mathf.Rad2Deg),SpeedMm=speeds[i],StopReason=reasons[i],Active=true};}
  state.Pedestrians=new PvpTrafficActor[walks.Length];for(int i=0;i<walks.Length;i++){var path=map.PedestrianPaths[i];var walk=walks[i];var direction=(walk.Forward?path.To-path.From:path.From-path.To).normalized;state.Pedestrians[i]=new PvpTrafficActor{Id=i,Kind=2,Edge=map.SignalZones[path.SignalZone].ApproachEdge,Xmm=Mathf.RoundToInt(walk.Position.x*1000),Zmm=Mathf.RoundToInt(walk.Position.z*1000),Yaw=Mathf.RoundToInt(Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg),SpeedMm=walk.Speed,StopReason=walk.Reason,Active=walk.Active};}return state;
 }
 public static Vector3 PresentedPosition(PvpTrafficActor actor,int observationTick,int tick,PvpMapRuntime map){float dt=Mathf.Clamp((tick-observationTick)*.02f,0,.2f);if(actor.Kind==2)return actor.Position+Quaternion.Euler(0,actor.Yaw,0)*Vector3.forward*(actor.SpeedMm/1000f*dt);var e=map.Connections[actor.Edge];var forward=(map.Nodes[e.To]-map.Nodes[e.From]).normalized;float remaining=Vector3.Distance(actor.Position,map.Nodes[e.To]);return actor.Position+forward*Mathf.Min(remaining,actor.SpeedMm/1000f*dt);}
 public static bool Contact(PvpTrafficSnapshot state,PvpMapRuntime map,Vector3 from,Vector3 to,int tick,out int kind,out int id){kind=id=0;if(state.Npcs!=null)foreach(var actor in state.Npcs)if(actor.Active&&Near(from,to,PresentedPosition(actor,state.Tick,tick,map),3.6f)){kind=1;id=actor.Id;return true;}if(state.Pedestrians!=null)foreach(var actor in state.Pedestrians)if(actor.Active&&Near(from,to,PresentedPosition(actor,state.Tick,tick,map),2.6f)){kind=2;id=actor.Id;return true;}return false;}
 static bool Near(Vector3 from,Vector3 to,Vector3 center,float radius){var delta=to-from;float fromDistance=(from-center).sqrMagnitude,toDistance=(to-center).sqrMagnitude;if(fromDistance<radius*radius&&toDistance>fromDistance+1e-7f&&Vector3.Dot(from-center,delta)>=0)return false;float t=delta.sqrMagnitude<.000001f?0:Mathf.Clamp01(Vector3.Dot(center-from,delta)/delta.sqrMagnitude);return Vector3.Distance(from+delta*t,center)<radius;}
}
}
