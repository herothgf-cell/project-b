using System;
using UnityEngine;
using Pvp.Map;
namespace Pvp.Traffic {
[Serializable] public struct PvpTrafficSnapshot {
 public PvpTrafficActor[] Npcs,Pedestrians;public int Tick,Phase,NextChangeTick,ObstacleXmm,ObstacleZmm;
 public Vector3 ObstaclePosition {get{return new Vector3(ObstacleXmm/1000f,0,ObstacleZmm/1000f);}}
 public string Signature {get{string s=Tick+":"+Phase+":"+NextChangeTick+":"+ObstacleXmm+":"+ObstacleZmm;if(Npcs!=null)foreach(var actor in Npcs)s+="|N"+actor.Signature;if(Pedestrians!=null)foreach(var actor in Pedestrians)s+="|P"+actor.Signature;return s;}}
}
// Minimal central test crossing, not a complete public-road traffic controller.
public sealed class PvpTrafficAuthority {
 public const string Version="central-crossing-v1";
 public static PvpTrafficSnapshot At(int tick){if(tick<0)throw new ArgumentOutOfRangeException(nameof(tick));int t=tick%400;return new PvpTrafficSnapshot{Tick=tick,Phase=(tick/250)%2,NextChangeTick=checked((tick/250+1)*250),ObstacleXmm=-5000+(t<=200?t:400-t)*50};}
 public static PvpTrafficSnapshot At(PvpMapRuntime map,int tick){var state=At(tick);var obstacle=map.ObstacleAt(tick);state.ObstacleXmm=Mathf.RoundToInt(obstacle.x*1000);state.ObstacleZmm=Mathf.RoundToInt(obstacle.z*1000);if(map.SignalZones.Count>0){state.Phase=map.SignalZones[0].Phase(tick);state.NextChangeTick=map.SignalZones[0].NextChange(tick);}return state;}
 public static bool Blocks(PvpMapRuntime map,Vector3 from,Vector3 to,int tick){var center=map.ObstacleAt(tick);var delta=to-from;float t=delta.sqrMagnitude<.00001f?0:Mathf.Clamp01(Vector3.Dot(center-from,delta)/delta.sqrMagnitude);return Vector3.Distance(from+delta*t,center)<map.Obstacle.Radius;}
 public static bool Blocks(Vector3 from,Vector3 to,int tick){var center=At(tick).ObstaclePosition;var delta=to-from;float t=delta.sqrMagnitude<.00001f?0:Mathf.Clamp01(Vector3.Dot(center-from,delta)/delta.sqrMagnitude);return Vector3.Distance(from+delta*t,center)<1.8f;}
}
}
