using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

// Keep the source footprint; withhold a provisional extrusion only when it
// collides with the taxi's actual lane corridor. Rendering and paving agree.
public static class SceneBuildingVisibility {
 public sealed class Conflict {public string buildingId,edgeId;public Vector3 position;public float height;}
 sealed class State {public readonly HashSet<string> hidden=new HashSet<string>();public readonly List<Conflict> conflicts=new List<Conflict>();}
 sealed class Footprint {public MapBuilding building;public RoadGeometry.Point[] ring;}
 static readonly ConditionalWeakTable<MapData,State> states=new ConditionalWeakTable<MapData,State>();
 public const float TaxiHalfWidth=.95f;
 public static bool Hidden(MapData map,string id){return map!=null&&StateFor(map).hidden.Contains(id??"");}
 public static IList<Conflict> Conflicts(MapData map){return StateFor(map).conflicts.AsReadOnly();}
 static State StateFor(MapData map){State result;if(states.TryGetValue(map,out result))return result;result=Build(map);states.Add(map,result);return result;}
 static RoadGeometry.Point P(Vector3 p){return new RoadGeometry.Point(p.x,p.z);}
 static State Build(MapData map){
  var state=new State();if(!map.hybrid)return state;
  var asset=Resources.Load<TextAsset>("eungye-scene-overrides");if(asset){var overrides=JsonUtility.FromJson<SceneOverrides>(asset.text);if(overrides.hiddenBuildings!=null)foreach(var id in overrides.hiddenBuildings)state.hidden.Add(id);}
  if(!map.HasHd||map.buildings==null)return state;
  var cells=new Dictionary<Vector2Int,List<Footprint>>();
  foreach(var building in map.buildings){
   if(building.points==null||building.points.Length<3)continue;
   float minX=float.MaxValue,minZ=float.MaxValue,maxX=float.MinValue,maxZ=float.MinValue;var ring=new RoadGeometry.Point[building.points.Length];
   for(int i=0;i<ring.Length;i++){var p=building.points[i];ring[i]=new RoadGeometry.Point(p.x,p.z);minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);minZ=Mathf.Min(minZ,p.z);maxZ=Mathf.Max(maxZ,p.z);}
   var footprint=new Footprint{building=building,ring=ring};
   for(int x=Mathf.FloorToInt(minX/32);x<=Mathf.FloorToInt(maxX/32);x++)for(int z=Mathf.FloorToInt(minZ/32);z<=Mathf.FloorToInt(maxZ/32);z++){
    var key=new Vector2Int(x,z);List<Footprint> list;if(!cells.TryGetValue(key,out list)){list=new List<Footprint>();cells.Add(key,list);}list.Add(footprint);
   }
  }
  var detected=new HashSet<string>();var checkedFootprints=new HashSet<Footprint>();
  foreach(var edge in map.hdEdges){if(edge.disabled||edge.points==null)continue;for(int i=1;i<edge.points.Length;i++){
   var a=edge.points[i-1].V();var b=edge.points[i].V();checkedFootprints.Clear();
   float clearance=CorridorPolish.Includes(map,edge.id)?(edge.turn!=0?2.1f:CorridorPolish.HalfLane)+1.9f:TaxiHalfWidth;
   for(int x=Mathf.FloorToInt((Mathf.Min(a.x,b.x)-clearance)/32);x<=Mathf.FloorToInt((Mathf.Max(a.x,b.x)+clearance)/32);x++)for(int z=Mathf.FloorToInt((Mathf.Min(a.z,b.z)-clearance)/32);z<=Mathf.FloorToInt((Mathf.Max(a.z,b.z)+clearance)/32);z++){
    List<Footprint> list;if(!cells.TryGetValue(new Vector2Int(x,z),out list))continue;
    foreach(var footprint in list){if(detected.Contains(footprint.building.id)||!checkedFootprints.Add(footprint))continue;
     if(!RoadGeometry.SweptLaneIntersects(P(a),P(b),footprint.ring,clearance))continue;
     detected.Add(footprint.building.id);state.hidden.Add(footprint.building.id);var center=Vector3.zero;foreach(var p in footprint.building.points)center+=p.V();center/=footprint.ring.Length;
     state.conflicts.Add(new Conflict{buildingId=footprint.building.id,edgeId=edge.id,position=center,height=footprint.building.height});
    }
   }
  }}return state;
 }
}
