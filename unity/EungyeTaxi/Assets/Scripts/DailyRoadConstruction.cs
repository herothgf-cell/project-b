using System;
using System.Runtime.CompilerServices;
using UnityEngine;

// Survey #48 (Y08), westbound Gyeysu-ro in front of the Hyundai Oilbank.
// One existing outer lane segment closes; its adjacent lane and mapped merges stay open.
public static class DailyRoadConstruction {
 public const string LaneId="local:11589:16123:10729:15920:part:1";
 public const string RoadLabel="계수로 서행 · 현대오일뱅크 앞";
 sealed class Segment {
  public int edge=-1;
  public Vector3[] points=Array.Empty<Vector3>();
 }
 static readonly ConditionalWeakTable<MapData,Segment> segments=new ConditionalWeakTable<MapData,Segment>();
 static Segment For(MapData map){return map==null?null:segments.GetValue(map,Find);}
 static Segment Find(MapData map){
  var result=new Segment();
  if(map.mapId!="eungye"||!map.hybrid||map.hdEdges==null)return result;
  int edge=Array.FindIndex(map.hdEdges,e=>e.id==LaneId&&!e.disabled);
  if(edge<0)return result;
  result.edge=edge;
  var points=map.hdEdges[edge].points;
  result.points=Array.ConvertAll(points,p=>p.V());
  return result;
 }
 public static int EdgeIndex(MapData map){return For(map)?.edge??-1;}
 // World-space centerline for map overlays. The returned points are shared.
 public static Vector3[] Preview(MapData map){return For(map)?.points??Array.Empty<Vector3>();}
 static bool Supported(DriveSession session){
  return session&&session.world&&session.business&&!session.business.SimulationSession&&!RoadMapRuntime.Trial&&!PracticeSession.Enabled;
 }
 public static Vector3[] Preview(DriveSession session){return Supported(session)?Preview(session.world.map):Array.Empty<Vector3>();}
 public static bool Apply(MapData map,bool closed){
  int index=EdgeIndex(map);
  if(index<0)return false;
  map.hdEdges[index].ClosedForConstruction=closed;
  return true;
 }
 public static bool Apply(DriveSession session,bool closed){
  if(!Supported(session))return false;
  var map=session.world.map;
  int index=EdgeIndex(map);
  if(index<0)return false;
  var lane=map.hdEdges[index];
  if(lane.ClosedForConstruction==closed)return true;
  lane.ClosedForConstruction=closed;
  if(session.jobs&&session.jobs.nav)session.jobs.nav.LaneAvailabilityChanged(index,closed);
  var district=session.traffic?session.traffic.GetComponent<DistrictTraffic>():null;
  if(district)district.LaneAvailabilityChanged(index,closed);
  return true;
 }
}
