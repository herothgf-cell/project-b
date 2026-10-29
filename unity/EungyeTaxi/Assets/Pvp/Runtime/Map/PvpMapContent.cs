using System;
using UnityEngine;
namespace Pvp.Map {
[Serializable] public struct PvpSignalZone {
 public string Id;public int ApproachEdge,RightTurnEdge,GreenTicks,RedTicks,OffsetTicks,AlternateEdgePlusOne;public float StopFraction,EntryFraction;
 public bool Applies(int edge){return edge==ApproachEdge||AlternateEdgePlusOne>0&&edge==AlternateEdgePlusOne-1;}
 public int Phase(int tick){int cycle=GreenTicks+RedTicks;return (tick+OffsetTicks)%cycle<GreenTicks?0:1;}
 public int NextChange(int tick){int cycle=GreenTicks+RedTicks,at=(tick+OffsetTicks)%cycle;return tick+(at<GreenTicks?GreenTicks-at:cycle-at);}
}
[Serializable] public sealed class PvpNpcPath {public int[] Edges;public float StartDistance,Speed=6;}
[Serializable] public struct PvpPedestrianPath {public string Id;public int SignalZone;public Vector3 From,To;public float Speed;}
[Serializable] public struct PvpServicePoint {public string Id,Name;public int StopIndex,DurationTicks,CostPoints;}
[Serializable] public struct PvpDistrict {public string Name;public Vector3 Center;public Vector2 Size;public Color Color;}
[Serializable] public struct PvpMovingObstacle {public Vector3 From,To;public int PeriodTicks;public float Radius;}
}
