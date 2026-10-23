using UnityEngine;

// Placement checks all adjoining roads, rather than only the incoming road width.
public static class RoadsidePlacement {
 public static bool OnRoad(MapData map,Vector3 p,float clearance=.4f){p.y=0;foreach(var road in map.roads)for(int i=1;i<road.points.Length;i++){var a=road.points[i-1].V();var d=road.points[i].V()-a;if(d.sqrMagnitude<.001f)continue;var q=a+d*Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude);if((p-q).sqrMagnitude<=Mathf.Pow(road.width*.5f+clearance,2))return true;}return false;}
 public static bool InBuilding(MapData map,Vector3 p){foreach(var b in map.buildings){bool inside=false;for(int i=0,k=b.points.Length-1;i<b.points.Length;k=i++){var a=b.points[i];var c=b.points[k];if((a.z>p.z)!=(c.z>p.z)&&p.x<(c.x-a.x)*(p.z-a.z)/(c.z-a.z)+a.x)inside=!inside;}if(inside)return true;}return false;}
 public static Vector3 FindPole(MapData map,Vector3 origin){
  if(!OnRoad(map,origin)&&!InBuilding(map,origin))return origin;
  for(float radius=1;radius<=40;radius+=1)for(int i=0;i<32;i++){float angle=i*Mathf.PI/16;var p=origin+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius;if(!OnRoad(map,p)&&!InBuilding(map,p))return p;}
  throw new System.InvalidOperationException("No clear roadside signal support near "+origin);
 }
}
