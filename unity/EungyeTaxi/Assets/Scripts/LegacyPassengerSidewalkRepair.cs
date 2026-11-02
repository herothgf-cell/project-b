using System;
using System.Collections.Generic;
using UnityEngine;
// Repairs the pre-existing, exact guarded S09 clone's missing sidewalk. This
// is a game-only scene overlay, independent of the new alternative bay pair.
public sealed partial class PassengerStopCatalog {
 public bool LegacyS09SidewalkRepaired {get;private set;}
 void RepairLegacyS09Sidewalk(MapData map){
  if(map.mapId!="eungye"||Stops.Length<=8||ReferenceEquals(Stops[8],map.stops[8]))return;
  var source=map.stops[8];var stop=Stops[8];
  if(source.name!="은계 승하차 9"||Mathf.Abs(source.width-3)>.01f||Mathf.Abs(stop.width-6.6f)>.01f||Mathf.Abs(source.x-675.727905f)>.02f||Mathf.Abs(source.z-1476.793335f)>.02f||source.laneEdge<0||map.hdEdges[source.laneEdge].id!="local:7003:14854:5837:14446:part:1")return;
  var geometry=new Geometry(map);var pos=stop.V();var dir=stop.Direction.normalized;var right=TrafficSystem.Right(dir);var anchor=stop.Passenger;
  var footprint=ProjectMapScene.Rectangle(anchor,dir,1.5f,18);
  if(geometry.FootprintBlocked(footprint)||geometry.FootprintOnRoad(footprint,.05f))throw new InvalidOperationException("S09 game-only sidewalk repair rejected: asphalt/polygon overlap");
  foreach(float along in new[]{-9f,-6,-3,0,3,6,9})foreach(float side in new[]{-.7f,0,.7f}){
   var sample=anchor+dir*along+right*side;
   if(sample.x<=3||sample.z<=3||sample.x>=map.width-3||sample.z>=map.depth-3||geometry.OnRoad(sample,.05f)||geometry.Blocked(sample))throw new InvalidOperationException("S09 game-only sidewalk repair rejected: unsafe full strip");
  }
  // Both ends overlap the existing sidewalk outside the vehicle standing bay.
  if(!ProjectMapScene.OnArea(map,anchor-dir*8-right*.65f,true)||!ProjectMapScene.OnArea(map,anchor+dir*8-right*.65f,true))throw new InvalidOperationException("S09 game-only sidewalk repair rejected: disconnected sidewalk");
  var prior=map.areas;var areas=new List<MapArea>();
  foreach(var area in prior){
   if(area.kind!="sidewalk"){areas.Add(area);continue;}
   // Existing stopped body occupies offset1.8m. Clear its full longitudinal
   // interaction range plus body length, while retaining outer pedestrian space.
   var pieces=Subtract(area.points,pos,dir,right,-7,7,.65f,2.95f);
   if(pieces.Count==1&&ReferenceEquals(pieces[0],area.points)){areas.Add(area);continue;}
   foreach(var ring in pieces)if(ring.Length>=3)areas.Add(new MapArea{kind=area.kind,name=area.name,evidence=area.evidence,height=area.height,points=ring,triangles=ProjectMapScene.Triangulate(ring)});
  }
  areas.Add(Walk(anchor,dir,1.5f,18,"game-s09-sidewalk-repair:2026-11-02"));map.areas=areas.ToArray();ProjectMapScene.InvalidateAreaIndex(map);
  bool clear=true;foreach(float along in new[]{-6.5f,-4,0,4,6.5f})foreach(float side in new[]{-1f,0,1f})if(ProjectMapScene.OnArea(map,stop.Kerb+dir*along+right*side,true))clear=false;
  foreach(float along in new[]{-8f,0,8})foreach(float side in new[]{-.7f,0,.7f})if(!ProjectMapScene.OnArea(map,anchor+dir*along+right*side,true))clear=false;
  if(!clear){map.areas=prior;ProjectMapScene.InvalidateAreaIndex(map);throw new InvalidOperationException("S09 game-only sidewalk repair rejected: body/walking continuity");}
  LegacyS09SidewalkRepaired=true;
 }
}
