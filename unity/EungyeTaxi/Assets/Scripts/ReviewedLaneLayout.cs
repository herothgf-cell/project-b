using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

// Field-observed lane roles with explicitly provisional game dimensions.
// Applied after the unchanged OSM base, before pavement and all graph consumers.
public static class ReviewedLaneLayout {
 [Serializable] public class Pocket {public string id,junctionId,evidence,checkedAt;public string[] approachLaneIds;public float laneLength,taperLength,offset;}
 [Serializable] public class Slip {public string id,junctionId,laneId,evidence,checkedAt;public float takeoff=35,taperLength=14,offset=3;}
 [Serializable] public class Settings {public Pocket[] pockets;public Slip[] slips;}
 sealed class Taper {public string source;public AssistedRoadNetwork.Path path;public float start,length,offset;}
 sealed class Applied {public readonly List<Tuple<HdEdge,int>> arrows=new List<Tuple<HdEdge,int>>();public readonly List<Taper> tapers=new List<Taper>();}
 static readonly ConditionalWeakTable<MapData,Applied> applied=new ConditionalWeakTable<MapData,Applied>();
 static MapPoint P(Vector3 p){return new MapPoint{x=p.x,z=p.z};}
 static AssistedRoadNetwork.Path Path(HdEdge e){return new AssistedRoadNetwork.Path(e.points.Select(p=>p.V()).ToArray());}
 static MapPoint[] Slice(AssistedRoadNetwork.Path p,float a,float b){var points=new List<MapPoint>{P(p.Position(a))};for(float d=a+1;d<b;d+=1)points.Add(P(p.Position(d)));points.Add(P(p.Position(b)));return points.ToArray();}
 public static void CopyState(MapData from,MapData to){Applied state;if(applied.TryGetValue(from,out state)&&!applied.TryGetValue(to,out var ignored))applied.Add(to,state);}
 public static void Apply(MapData map){
  if(map.mapId!="eungye"||!map.hybrid)return;
  var asset=Resources.Load<TextAsset>("eungye-reviewed-lanes");if(!asset)throw new InvalidOperationException("Missing reviewed lane settings");
  Apply(map,JsonUtility.FromJson<Settings>(asset.text));
 }
 public static void Apply(MapData map,Settings settings){
  // A composed project document already owns its lane geometry.
  if(map.editorDocument!=null)return;
  Applied old;if(applied.TryGetValue(map,out old))return;
  var state=new Applied();var edges=new List<HdEdge>(map.hdEdges);
  foreach(var item in settings.pockets??new Pocket[0]){
   DateTime date;
   if(string.IsNullOrWhiteSpace(item.id)||string.IsNullOrWhiteSpace(item.evidence)||!DateTime.TryParseExact(item.checkedAt,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out date)||item.approachLaneIds==null||item.approachLaneIds.Length<2||item.laneLength<15||item.taperLength<20||item.offset<2.6f||item.offset>3.5f)throw new InvalidOperationException("Invalid reviewed pocket "+item.id);
   var indices=item.approachLaneIds.Select(id=>edges.FindIndex(e=>e.toNode==id+":end"&&e.ruleId==item.junctionId&&!e.disabled)).ToArray();
   if(indices.Any(i=>i<0)||indices.Distinct().Count()!=indices.Length)throw new InvalidOperationException("Reviewed approach no longer matches "+item.id);
   int inner=indices[0];var original=edges[inner];var path=Path(original);
   int left=original.next.Where(i=>edges[i].turn==-1).DefaultIfEmpty(-1).First();
   if(left<0||edges[left].status=="ngii"||edges[left].disabled)throw new InvalidOperationException("Missing provisional left join "+item.id);
   float length=item.laneLength+item.taperLength,cut=path.length-length;
   if(cut<10)throw new InvalidOperationException("Approach too short "+item.id);
   string prefix="reviewed:"+item.id;
   var join=edges[left];var exit=Path(join);
   var tailIndices=new List<int>();
   for(int lane=0;lane<indices.Length;lane++){
    int index=indices[lane];var e=edges[index];var p=Path(e);float from=p.length-length;
    if(from<10||e.number!=lane+1||e.sourceId!=original.sourceId)throw new InvalidOperationException("Inconsistent reviewed lane "+item.id);
    string split=prefix+":split:"+lane;
    int tailIndex=edges.Count;tailIndices.Add(tailIndex);
    var tail=new HdEdge{id=item.approachLaneIds[lane]+":part:reviewed:"+item.id,sourceId=e.sourceId,fromNode=split,toNode=e.toNode,points=Slice(p,from,p.length),number=lane+2,status="provisional",ruleId=e.ruleId,roadName=e.roadName,next=e.next.Where(n=>n!=left).ToArray(),sourceOffset=e.sourceOffset+from};
    edges.Add(tail);
    foreach(var stop in map.stops)if(stop.laneEdge==index&&stop.laneAt>from){stop.laneEdge=tailIndex;stop.laneAt-=from;}
    e.points=Slice(p,0,from);e.toNode=split;e.next=new[]{tailIndex};
   }
   var taperPoints=new List<MapPoint>();
   for(int i=0;i<=30;i++){float t=i/30f,d=cut+item.taperLength*t,blend=t*t*(3-2*t);taperPoints.Add(P(path.Position(d)-TrafficSystem.Right(path.Tangent(d))*item.offset*blend));}
   var pocketPoints=new List<MapPoint>();
   for(int i=0;i<=25;i++){float d=cut+item.taperLength+item.laneLength*i/25f;pocketPoints.Add(P(path.Position(d)-TrafficSystem.Right(path.Tangent(d))*item.offset));}
   int taperIndex=edges.Count,pocketIndex=taperIndex+1,joinIndex=taperIndex+2;
   var pocket=new HdEdge{id=prefix+":pocket",sourceId=original.sourceId,fromNode=prefix+":lane-start",toNode=prefix+":lane-end",points=pocketPoints.ToArray(),number=1,status="provisional",ruleId=item.junctionId,evidence=item.evidence,updated=item.checkedAt,roadName=original.roadName,next=new[]{joinIndex}};
   edges.Add(new HdEdge{id=prefix+":taper",sourceId=original.sourceId,fromNode=original.toNode,toNode=pocket.fromNode,points=taperPoints.ToArray(),number=1,change=-1,status="provisional",ruleId=item.junctionId,next=new[]{pocketIndex}});
   edges.Add(pocket);
   var start=pocket.points[pocket.points.Length-1].V();var end=exit.Position(exit.length);var forward=Path(pocket).Tangent(item.laneLength);var heading=exit.Tangent(exit.length);float handle=Vector3.Distance(start,end)*.4f;var curve=new MapPoint[33];
   for(int i=0;i<curve.Length;i++){float t=i/32f,u=1-t;curve[i]=P(u*u*u*start+3*u*u*t*(start+forward*handle)+3*u*t*t*(end-heading*handle)+t*t*t*end);}
   edges.Add(new HdEdge{id="join:"+prefix+":left",sourceId=join.sourceId,fromNode=pocket.toNode,toNode=join.toNode,points=curve,number=join.number,turn=-1,status="provisional",ruleId=item.junctionId,evidence=item.evidence,updated=item.checkedAt,next=join.next});
   original.next=original.next.Concat(new[]{taperIndex}).ToArray();join.disabled=true;join.next=new int[0];
   state.arrows.Add(Tuple.Create(pocket,-1));state.arrows.Add(Tuple.Create(edges[tailIndices[0]],0));
   state.tapers.Add(new Taper{source=original.sourceId,path=path,start=cut,length=item.taperLength,offset=item.offset});
  }
  foreach(var item in settings.slips??new Slip[0]){
   DateTime date;
   if(string.IsNullOrWhiteSpace(item.id)||string.IsNullOrWhiteSpace(item.evidence)||!DateTime.TryParseExact(item.checkedAt,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out date)||item.takeoff<30||item.taperLength<10||item.takeoff-item.taperLength<18||item.offset<2.6f||item.offset>3.5f)throw new InvalidOperationException("Invalid reviewed slip "+item.id);
   int index=edges.FindIndex(e=>e.toNode==item.laneId+":end"&&e.ruleId==item.junctionId&&!e.disabled);
   if(index<0)throw new InvalidOperationException("Missing slip approach "+item.id);
   var original=edges[index];var path=Path(original);float cut=path.length-item.takeoff;
   int right=original.next.Where(i=>edges[i].turn==1&&!edges[i].disabled).DefaultIfEmpty(-1).First();
   if(cut<10||right<0||edges[right].status=="ngii")throw new InvalidOperationException("Missing provisional right join "+item.id);
   var oldJoin=edges[right];var exit=Path(oldJoin);string prefix="reviewed:"+item.id;
   int tailIndex=edges.Count,taperIndex=tailIndex+1,slipIndex=tailIndex+2,joinIndex=tailIndex+3;
   string split=prefix+":split";
   edges.Add(new HdEdge{id=original.id+":part:"+prefix,sourceId=original.sourceId,fromNode=split,toNode=original.toNode,points=Slice(path,cut,path.length),number=original.number,status="provisional",ruleId=item.junctionId,roadName=original.roadName,next=original.next.Where(i=>i!=right).ToArray(),sourceOffset=original.sourceOffset+cut});
   foreach(var stop in map.stops)if(stop.laneEdge==index&&stop.laneAt>cut){stop.laneEdge=tailIndex;stop.laneAt-=cut;}
   original.points=Slice(path,0,cut);original.toNode=split;original.next=new[]{tailIndex,taperIndex};
   var taper=new List<MapPoint>();for(int i=0;i<=20;i++){float t=i/20f,d=cut+item.taperLength*t;var p=path.Position(d)+TrafficSystem.Right(path.Tangent(d))*item.offset*t*t*(3-2*t);taper.Add(P(p));}
   var lane=new List<MapPoint>();float begin=cut+item.taperLength,end=path.length-6;
   for(float d=begin;d<end;d+=1)lane.Add(P(path.Position(d)+TrafficSystem.Right(path.Tangent(d))*item.offset));lane.Add(P(path.Position(end)+TrafficSystem.Right(path.Tangent(end))*item.offset));
   edges.Add(new HdEdge{id=prefix+":taper",sourceId=original.sourceId,fromNode=split,toNode=prefix+":lane-start",points=taper.ToArray(),number=original.number+1,change=1,status="provisional",ruleId=item.junctionId,next=new[]{slipIndex}});
   var slip=new HdEdge{id=prefix+":lane",sourceId=original.sourceId,fromNode=prefix+":lane-start",toNode=prefix+":lane-end",points=lane.ToArray(),number=original.number+1,status="provisional",ruleId=item.junctionId,evidence=item.evidence,updated=item.checkedAt,next=new[]{joinIndex}};edges.Add(slip);
   var from=lane[lane.Count-1].V();var to=exit.Position(exit.length);var forward=path.Tangent(end);var heading=exit.Tangent(exit.length);float handle=Vector3.Distance(from,to)*.5f;var curve=new MapPoint[33];
   for(int i=0;i<curve.Length;i++){float t=i/32f,u=1-t;curve[i]=P(u*u*u*from+3*u*u*t*(from+forward*handle)+3*u*t*t*(to-heading*handle)+t*t*t*to);}
   edges.Add(new HdEdge{id="join:"+prefix+":right",sourceId=oldJoin.sourceId,fromNode=slip.toNode,toNode=oldJoin.toNode,points=curve,number=oldJoin.number,turn=1,status="provisional",ruleId=item.junctionId,evidence=item.evidence,updated=item.checkedAt,next=oldJoin.next});
   oldJoin.disabled=true;oldJoin.next=new int[0];
  }
  map.hdEdges=edges.ToArray();applied.Add(map,state);
 }
 public static Vector3 Centreline(MapData map,string source,Vector3 point){
  Applied state;if(!applied.TryGetValue(map,out state))return point;
  foreach(var taper in state.tapers){if(taper.source!=source)continue;float error;float at=taper.path.Project(point,out error);
   if(error>10||at<taper.start||at>=taper.start+taper.length)continue;
   float t=(at-taper.start)/taper.length,blend=t*t*(3-2*t);return point-TrafficSystem.Right(taper.path.Tangent(at))*taper.offset*blend*.5f;
  }return point;
 }
 public static void Paint(MapData map,Action<string,Vector3,Vector3,float,float,bool> mark){
  Applied state;if(!applied.TryGetValue(map,out state)){LaneDirectionPaint.Paint(map,mark);return;}
  foreach(var taper in state.tapers)for(float at=0;at<taper.length;at+=1){float t=at/taper.length,blend=t*t*(3-2*t);if(taper.offset*blend<1.8f||at%6>=3)continue;
   var direction=taper.path.Tangent(taper.start+at);var point=taper.path.Position(taper.start+at)-TrafficSystem.Right(direction)*taper.offset*blend*.5f;mark("Reviewed pocket taper",point,direction,.13f,.96f,false);
  }
  LaneDirectionPaint.Paint(map,mark);
 }
}
