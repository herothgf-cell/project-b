using UnityEngine;

// Play feedback round 7 (2026-11-26): life facilities (workshop, fuel station, rest area, restaurant, service lots)
// answer F anywhere on their own grounds once the taxi has stopped; the painted bay and the heading no longer have
// to match. Grounds = the facility's own paving (driveway and yard) or its plot outline, plus a few metres round the
// service point.
public static class FacilityZone {
 public const float Stopped=.56f;
 public static bool OnPaving(MapRoad[] roads,Vector3 p){
  if(roads==null)return false;p.y=0;
  foreach(var r in roads){if(r==null||r.points==null)continue;float half=r.width*.5f+.3f;
   for(int i=1;i<r.points.Length;i++){var a=r.points[i-1].V();var b=r.points[i].V();a.y=b.y=0;var ab=b-a;float t=ab.sqrMagnitude<1e-4f?0:Mathf.Clamp01(Vector3.Dot(p-a,ab)/ab.sqrMagnitude);if((a+ab*t-p).sqrMagnitude<=half*half)return true;}}
  return false;
 }
 public static bool InOutline(Vector3[] outline,Vector3 p){
  if(outline==null||outline.Length<3)return false;bool inside=false;
  for(int i=0,j=outline.Length-1;i<outline.Length;j=i++){var a=outline[i];var c=outline[j];if((a.z>p.z)!=(c.z>p.z)&&p.x<(c.x-a.x)*(p.z-a.z)/(c.z-a.z)+a.x)inside=!inside;}
  return inside;
 }
 public static bool Near(Vector3 point,Vector3 p,float metres){var d=p-point;d.y=0;return d.sqrMagnitude<=metres*metres;}
 public static bool Level(Vector3 point,Vector3 p){return Mathf.Abs(p.y-point.y)<1.5f;}
}
