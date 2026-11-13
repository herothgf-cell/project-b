using System.Collections.Generic;
using UnityEngine;

// Index only the yellow centreline segments drawn on the current map.
public sealed class CentreLineCrossing {
 const float CellSize=8;
 struct Segment {public Vector2 from,to;}
 readonly Dictionary<Vector2Int,List<Segment>> cells=new Dictionary<Vector2Int,List<Segment>>();

 static Vector2 Flat(Vector3 point){return new Vector2(point.x,point.z);}
 static int Cell(float coordinate){return Mathf.FloorToInt(coordinate/CellSize);}
 static float Cross(Vector2 a,Vector2 b){return a.x*b.y-a.y*b.x;}

 public void Add(Vector3 start,Vector3 end){
  var a=Flat(start);var b=Flat(end);
  if((b-a).sqrMagnitude<.0001f)return;
  var segment=new Segment{from=a,to=b};
  for(int x=Cell(Mathf.Min(a.x,b.x));x<=Cell(Mathf.Max(a.x,b.x));x++)
   for(int z=Cell(Mathf.Min(a.y,b.y));z<=Cell(Mathf.Max(a.y,b.y));z++){
    var key=new Vector2Int(x,z);
    if(!cells.TryGetValue(key,out var segments)){segments=new List<Segment>();cells.Add(key,segments);}
    segments.Add(segment);
   }
 }

 public bool Crossed(Vector3 before,Vector3 after){
  var a=Flat(before);var b=Flat(after);var movement=b-a;
  if(movement.sqrMagnitude<.000001f||movement.sqrMagnitude>9)return false;
  for(int x=Cell(Mathf.Min(a.x,b.x));x<=Cell(Mathf.Max(a.x,b.x));x++)
   for(int z=Cell(Mathf.Min(a.y,b.y));z<=Cell(Mathf.Max(a.y,b.y));z++){
    if(!cells.TryGetValue(new Vector2Int(x,z),out var segments))continue;
    foreach(var line in segments){
     var direction=line.to-line.from;
     float denominator=Cross(movement,direction);
     if(Mathf.Abs(denominator)<.000001f)continue;
     var delta=line.from-a;
     float along=Cross(delta,direction)/denominator;
     float onLine=Cross(delta,movement)/denominator;
     if(along>=0&&along<1&&onLine>=0&&onLine<=1)return true;
    }
   }
  return false;
 }
}
