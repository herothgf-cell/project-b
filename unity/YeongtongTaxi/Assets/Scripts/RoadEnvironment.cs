using System.Collections.Generic;
using UnityEngine;
public class RoadEnvironment : MonoBehaviour {
 public MapData map;public int sidewalkPieces;
 struct Segment {public Vector3 a,b;public float half;}
 readonly List<Segment> segments=new List<Segment>();
 const float CellSize=32;
 readonly Dictionary<Vector2Int,List<int>> roadCells=new Dictionary<Vector2Int,List<int>>(),buildingCells=new Dictionary<Vector2Int,List<int>>();
 static readonly int[] Sides={-1,1};
 static readonly float[] SampleAcross={-.85f,0,.85f},SampleAlong={-1.35f,1.35f};
 int[] checkedSegments;int queryId;
 static int Cell(float coordinate){return Mathf.FloorToInt(coordinate/CellSize);}
 static void AddBounds(Dictionary<Vector2Int,List<int>> cells,int id,float minX,float minZ,float maxX,float maxZ){
  int x0=Cell(minX),x1=Cell(maxX),z0=Cell(minZ),z1=Cell(maxZ);
  for(int x=x0;x<=x1;x++)for(int z=z0;z<=z1;z++){
   var key=new Vector2Int(x,z);List<int> bucket;if(!cells.TryGetValue(key,out bucket)){bucket=new List<int>();cells.Add(key,bucket);}bucket.Add(id);
  }
 }
 void IndexGeometry(){
  roadCells.Clear();buildingCells.Clear();checkedSegments=new int[segments.Count];queryId=0;
  for(int i=0;i<segments.Count;i++){var s=segments[i];float radius=Mathf.Abs(s.half);
   AddBounds(roadCells,i,Mathf.Min(s.a.x,s.b.x)-radius,Mathf.Min(s.a.z,s.b.z)-radius,Mathf.Max(s.a.x,s.b.x)+radius,Mathf.Max(s.a.z,s.b.z)+radius);
  }
  for(int i=0;i<map.buildings.Length;i++){
   var points=map.buildings[i].points;if(points.Length==0)continue;
   float minX=points[0].x,maxX=minX,minZ=points[0].z,maxZ=minZ;
   for(int j=1;j<points.Length;j++){var p=points[j];minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);minZ=Mathf.Min(minZ,p.z);maxZ=Mathf.Max(maxZ,p.z);}
   AddBounds(buildingCells,i,minX,minZ,maxX,maxZ);
  }
 }
 public bool OnRoad(Vector3 point,float margin=0){
  if(roadCells.Count==0)return false;point.y=0;
  if(queryId==int.MaxValue){System.Array.Clear(checkedSegments,0,checkedSegments.Length);queryId=0;}queryId++;
  // Index the original road envelope, then expand the query by abs(margin).
  // The absolute value preserves the legacy squared-radius behavior even
  // when a caller supplies a negative margin larger than the half-width.
  float reach=Mathf.Abs(margin);int x0=Cell(point.x-reach),x1=Cell(point.x+reach),z0=Cell(point.z-reach),z1=Cell(point.z+reach);
  for(int x=x0;x<=x1;x++)for(int z=z0;z<=z1;z++){
   List<int> bucket;if(!roadCells.TryGetValue(new Vector2Int(x,z),out bucket))continue;
   foreach(int index in bucket){if(checkedSegments[index]==queryId)continue;checkedSegments[index]=queryId;var s=segments[index];
    var d=s.b-s.a;float t=Mathf.Clamp01(Vector3.Dot(point-s.a,d)/d.sqrMagnitude);if((point-(s.a+d*t)).sqrMagnitude<=(s.half+margin)*(s.half+margin))return true;
   }
  }return false;
 }
 bool InBuilding(Vector3 p){
  List<int> bucket;if(!buildingCells.TryGetValue(new Vector2Int(Cell(p.x),Cell(p.z)),out bucket))return false;
  foreach(int index in bucket){var b=map.buildings[index];bool inside=false;for(int i=0,j=b.points.Length-1;i<b.points.Length;j=i++){var a=b.points[i];var c=b.points[j];if((a.z>p.z)!=(c.z>p.z)&&p.x<(c.x-a.x)*(p.z-a.z)/(c.z-a.z)+a.x)inside=!inside;}if(inside)return true;}return false;
 }
 bool PavingClear(Vector3 p,Vector3 right,Vector3 dir){
  foreach(float x in SampleAcross)foreach(float z in SampleAlong){var q=p+right*x+dir*z;if(q.x<1||q.z<1||q.x>map.width-1||q.z>map.depth-1||OnRoad(q,-.03f)||InBuilding(q))return false;}return true;
 }
 public void Build(MapData data){map=data;segments.Clear();sidewalkPieces=0;foreach(var r in map.roads)for(int i=1;i<r.points.Length;i++)segments.Add(new Segment{a=r.points[i-1].V(),b=r.points[i].V(),half=r.width*.5f});IndexGeometry();
  var vertices=new List<Vector3>();var triangles=new List<int>();
  foreach(var s in segments){var d=s.b-s.a;float len=d.magnitude;var dir=d/len;var right=TrafficSystem.Right(dir);
   for(float at=1.5f;at<len;at+=3)foreach(int side in Sides){var p=s.a+dir*at+right*side*(s.half+1);
    if(!PavingClear(p,right,dir))continue;sidewalkPieces++;var a=p-right*.95f-dir*1.45f;var b=p-right*.95f+dir*1.45f;var c=p+right*.95f+dir*1.45f;var e=p+right*.95f-dir*1.45f;
    Quad(vertices,triangles,a+Vector3.up*.16f,b+Vector3.up*.16f,c+Vector3.up*.16f,e+Vector3.up*.16f);
    var ring=new[]{a,b,c,e};for(int i=0;i<4;i++){var u=ring[i];var v=ring[(i+1)%4];Quad(vertices,triangles,u,v,v+Vector3.up*.16f,u+Vector3.up*.16f);}
   }
  }
  // Each validated stop receives a continuous standing area, even between generated paving segments.
  foreach(var stop in map.stops){var centre=stop.V(.08f)+TrafficSystem.Right(stop.Direction)*(stop.width*.5f+1);var platform=WorldBuilder.Box("승하차 보도",centre,new Vector3(1.9f,.16f,5),WorldBuilder.Mat("Stop pavement",new Color(.65f,.63f,.58f)),transform);platform.transform.rotation=Quaternion.LookRotation(stop.Direction);}
  var mesh=new Mesh();mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();var obj=new GameObject("인도와 낮은 연석");obj.transform.SetParent(transform);obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=WorldBuilder.Mat("Sidewalk",new Color(.65f,.63f,.58f));obj.AddComponent<MeshCollider>().sharedMesh=mesh;
 }
 static void Quad(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c,Vector3 d){int k=v.Count;v.AddRange(new[]{a,b,c,d});t.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});}
}
