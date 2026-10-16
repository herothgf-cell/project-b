using System.Collections.Generic;
using System.Text;
using UnityEngine;

// Regression oracle for the spatial-index optimization: retain the original
// brute-force distance/polygon checks and compare the generated paving itself.
public class RoadEnvironmentIndexSmokeTest : MonoBehaviour {
 static MapPoint Point(float x,float z){return new MapPoint{x=x,z=z};}
 static MapRoad Road(float width,params MapPoint[] points){return new MapRoad{width=width,points=points};}
 static bool BruteRoad(MapData map,Vector3 p,float margin){
  p.y=0;foreach(var road in map.roads)for(int i=1;i<road.points.Length;i++){
   var a=road.points[i-1].V();var d=road.points[i].V()-a;float t=Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude);
   if((p-(a+d*t)).sqrMagnitude<=(road.width*.5f+margin)*(road.width*.5f+margin))return true;
  }return false;
 }
 static bool BruteBuilding(MapData map,Vector3 p){
  foreach(var building in map.buildings){bool inside=false;for(int i=0,j=building.points.Length-1;i<building.points.Length;j=i++){
   var a=building.points[i];var b=building.points[j];if((a.z>p.z)!=(b.z>p.z)&&p.x<(b.x-a.x)*(p.z-a.z)/(b.z-a.z)+a.x)inside=!inside;
  }if(inside)return true;}return false;
 }
 static List<Vector3> BrutePavingTops(MapData map){
  var tops=new List<Vector3>();foreach(var road in map.roads)for(int i=1;i<road.points.Length;i++){
   var a=road.points[i-1].V();var d=road.points[i].V()-a;float length=d.magnitude;var dir=d/length;var right=TrafficSystem.Right(dir);
   for(float at=1.5f;at<length;at+=3)foreach(int side in new[]{-1,1}){
    var p=a+dir*at+right*side*(road.width*.5f+1);bool clear=true;
    foreach(float x in new[]{-.85f,0,.85f})foreach(float z in new[]{-1.35f,1.35f}){
     var q=p+right*x+dir*z;if(q.x<1||q.z<1||q.x>map.width-1||q.z>map.depth-1||BruteRoad(map,q,-.03f)||BruteBuilding(map,q))clear=false;
    }
    if(clear)tops.AddRange(new[]{p-right*.95f-dir*1.45f+Vector3.up*.16f,p-right*.95f+dir*1.45f+Vector3.up*.16f,p+right*.95f+dir*1.45f+Vector3.up*.16f,p+right*.95f-dir*1.45f+Vector3.up*.16f});
   }
  }return tops;
 }
 public static bool Verify(out string details){
  var map=new MapData{width=160,depth=160,
   roads=new[]{Road(7,Point(2,32),Point(150,32)),Road(12,Point(64,2),Point(64,150)),Road(6,Point(16,16),Point(145,145)),Road(7,Point(96,62),Point(128,62)),Road(7,Point(120,120),Point(120,120))},
   buildings=new[]{new MapBuilding{points=new[]{Point(30,36),Point(35,36),Point(35,44),Point(30,44)}},new MapBuilding{points=new[]{Point(68,61),Point(79,61),Point(79,70),Point(68,70)}},new MapBuilding{points=new[]{Point(93,63.9f),Point(103,63.9f),Point(103,72),Point(93,72)}}},
   stops=new[]{new MapStop{x=20,z=32,dx=1,dz=0,width=7}}};
  var host=new GameObject("Road environment index fixture");var env=host.AddComponent<RoadEnvironment>();env.Build(map);
  bool road=true;int queries=0;var random=new System.Random(74581);
  foreach(float margin in new[]{-.03f,0,2,33,-40}){
   for(int i=0;i<600;i++){var p=new Vector3((float)random.NextDouble()*220-30,11,(float)random.NextDouble()*220-30);road&=env.OnRoad(p,margin)==BruteRoad(map,p,margin);queries++;}
   foreach(float x in new[]{-.001f,0,31.999f,32,32.001f,63.999f,64,64.001f,96,128,160})foreach(float z in new[]{28.5f,32,35.5f,61.999f,62,64,96}){
    var p=new Vector3(x,0,z);road&=env.OnRoad(p,margin)==BruteRoad(map,p,margin);queries++;
   }
  }
  var expected=BrutePavingTops(map);var mesh=host.transform.Find("인도와 낮은 연석").GetComponent<MeshFilter>().sharedMesh;var actual=mesh.vertices;
  bool paving=expected.Count>0&&env.sidewalkPieces==expected.Count/4&&actual.Length==expected.Count*5;
  if(paving)for(int i=0;i<expected.Count;i++)paving&=(expected[i]-actual[(i/4)*20+i%4]).sqrMagnitude<.000001f;
  bool standing=host.transform.Find("승하차 보도")!=null;
  var report=new StringBuilder();report.AppendLine((road?"PASS ":"FAIL ")+"indexed road queries match brute force, including signed margins and cell boundaries: "+queries);
  report.AppendLine((paving?"PASS ":"FAIL ")+"paving piece count and every top vertex match brute-force original geometry: "+env.sidewalkPieces);
  report.AppendLine((standing?"PASS ":"FAIL ")+"stop standing platform retained");details=report.ToString();Destroy(host);return road&&paving&&standing;
 }
 void Start(){string report;bool ok=Verify(out report);Debug.Log(report);string output=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../..","artifacts","road-environment-index-smoke.txt"));System.IO.File.WriteAllText(output,"success="+ok+"\n"+report);Application.Quit(ok?0:1);}
}
