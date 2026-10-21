using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

// Game-only pavement envelope. The compiled, directed lanes remain authoritative;
// never infer a new legal lane or turn from a panorama or a wide source polygon.
public static class CorridorPolish {
 [Serializable] public class Settings {public string status;public string[] roadIds,edgeIds;}
 public sealed class Change {public string roadId;public float before,after;}
 public sealed class Result {public readonly List<Change> widths=new List<Change>();public readonly HashSet<string> edges=new HashSet<string>();public int ribbons;}
 static readonly ConditionalWeakTable<MapData,Result> results=new ConditionalWeakTable<MapData,Result>();
 public const float HalfLane=1.9f;
 public static Result For(MapData map){Result result;return results.TryGetValue(map,out result)?result:null;}
 public static bool Includes(MapData map,string edge){var r=For(map);return r!=null&&r.edges.Contains(edge);}
 public static void Apply(MapData map){
  if(map==null||map.mapId!="eungye"||!map.hybrid||!map.HasHd||For(map)!=null)return;
  var asset=Resources.Load<TextAsset>("eungye-corridor-polish");if(!asset)throw new InvalidOperationException("Missing Eungye corridor polish settings");
  var config=JsonUtility.FromJson<Settings>(asset.text);Apply(map,config);
 }
 public static void Apply(MapData map,Settings config){
  if(For(map)!=null)return;
  var selected=new HashSet<string>(config.roadIds);var result=new Result();
  var roads=map.roads.ToDictionary(r=>r.id);foreach(var id in selected)if(!roads.ContainsKey(id))throw new InvalidOperationException("Corridor source changed: "+id);
  var lanes=map.hdEdges.Where(e=>!e.disabled&&e.status=="provisional"&&e.change==0&&e.turn==0&&!e.id.StartsWith("join:")).ToArray();
  var bySource=new Dictionary<string,List<HdEdge>>();foreach(var e in lanes)foreach(var id in e.sourceId.Split(',')){List<HdEdge> list;if(!bySource.TryGetValue(id,out list)){list=new List<HdEdge>();bySource[id]=list;}list.Add(e);}
  foreach(var id in selected){
   var road=roads[id];List<HdEdge> group;if(road.suppressPaint||!bySource.TryGetValue(id,out group))continue;
   // Keep enough width for EVERY lane sharing the source, not just the taxi route.
   float extent=0;int samples=0;
   foreach(var e in group)foreach(var point in e.points)for(int i=1;i<road.points.Length;i++){
    var a=road.points[i-1].V();var d=road.points[i].V()-a;float length=d.magnitude;if(length<.01f)continue;
    var dir=d/length;float along=Vector3.Dot(point.V()-a,dir);if(along<0||along>length)continue;
    float offset=Mathf.Abs(Vector3.Dot(point.V()-a,TrafficSystem.Right(dir)));if(offset>road.width*.5f+1)continue;
    extent=Mathf.Max(extent,offset);samples++;
   }
   if(samples==0)continue;float width=Mathf.Min(road.width,Mathf.Max(6.5f,2*(extent+HalfLane)));
   if(road.width-width>.25f){result.widths.Add(new Change{roadId=id,before=road.width,after=width});road.width=width;}
  }
  var touchedNodes=new HashSet<string>();foreach(var e in lanes)if(e.sourceId.Split(',').Any(selected.Contains)){result.edges.Add(e.id);touchedNodes.Add(e.fromNode);touchedNodes.Add(e.toNode);}
  // Cover the same turns/lane changes driven by navigation and NPCs, including
  // their swept lateral space. These are surfaces, never extra graph edges.
  foreach(var e in map.hdEdges)if(!e.disabled&&e.status!="ngii"&&(touchedNodes.Contains(e.fromNode)||touchedNodes.Contains(e.toNode)))result.edges.Add(e.id);
  var surfaces=new List<MapRoad>(map.roads);
  foreach(var e in map.hdEdges)if(result.edges.Contains(e.id)){
   surfaces.Add(new MapRoad{id="polish:"+e.id,name="우선 동선 게임용 주행 여유",width=e.turn!=0?4.2f:HalfLane*2,lanes=1,sourceLanes=1,oneWay=1,suppressPaint=true,points=e.points});result.ribbons++;
  }
  map.roads=surfaces.ToArray();results.Add(map,result);
  Debug.Log("CORRIDOR_POLISH widths="+result.widths.Count+" sharedLaneSurfaces="+result.ribbons+" · game geometry / field widths unverified");
 }
}
