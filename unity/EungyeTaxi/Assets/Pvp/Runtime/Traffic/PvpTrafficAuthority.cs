using System;
using UnityEngine;
namespace Pvp.Traffic {
[Serializable] public struct PvpTrafficSnapshot {
 public int Tick,Phase,NextChangeTick,ObstacleXmm;
 public Vector3 ObstaclePosition {get{return new Vector3(ObstacleXmm/1000f,0,0);}}
 public string Signature {get{return Tick+":"+Phase+":"+NextChangeTick+":"+ObstacleXmm;}}
}
// Minimal central test crossing, not a complete public-road traffic controller.
public sealed class PvpTrafficAuthority {
 public const string Version="central-crossing-v1";
 public static PvpTrafficSnapshot At(int tick){if(tick<0)throw new ArgumentOutOfRangeException(nameof(tick));int t=tick%400;return new PvpTrafficSnapshot{Tick=tick,Phase=(tick/250)%2,NextChangeTick=checked((tick/250+1)*250),ObstacleXmm=-5000+(t<=200?t:400-t)*50};}
 public static bool Blocks(Vector3 from,Vector3 to,int tick){var center=At(tick).ObstaclePosition;var delta=to-from;float t=delta.sqrMagnitude<.00001f?0:Mathf.Clamp01(Vector3.Dot(center-from,delta)/delta.sqrMagnitude);return Vector3.Distance(from+delta*t,center)<1.8f;}
}
}
