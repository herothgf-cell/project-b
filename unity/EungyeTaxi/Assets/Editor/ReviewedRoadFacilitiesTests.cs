using System;
using System.IO;
using System.Linq;
using UnityEngine;

public static class ReviewedRoadFacilitiesTests {
 static void Check(bool pass,string message){if(!pass)throw new Exception("REVIEWED_ROAD_FACILITIES: "+message);}
 static HdMark Mark(string id,float z){return new HdMark{id=id,type="111",kind="501",points=new[]{new MapPoint{x=0,z=z},new MapPoint{x=100,z=z}}};}
 public static void Run(){
  var map=new MapData{mapId="fixture",hdMarks=new[]{Mark("north",2),Mark("south",-2)},hdEdges=new HdEdge[0],roads=new MapRoad[0]};
  var facility=new ReviewedRoadFacilities.Facility{id="median",kind="paired-median",sourceUrl="https://example.test/panorama",northMarkIds=new[]{"north"},southMarkIds=new[]{"south"},hatchFromX=10,hatchToX=90,barrierFromX=10,barrierToX=90,barrierWidth=.4f,barrierHeight=.75f,openings=new[]{new ReviewedRoadFacilities.Opening{fromX=40,toX=60}}};
  var catalog=new ReviewedRoadFacilities.Catalog{mapId="fixture",facilities=new[]{facility}};
  var layout=ReviewedRoadFacilities.Plan(map,catalog);
  Check(layout.barriers.Count>0&&layout.hatches.Count>0,"barrier and hatching derive from paired source lines");
  foreach(var segment in layout.barriers.Concat(layout.hatches)){
   Check(Mathf.Min(segment.a.x,segment.b.x)>=9.99f&&Mathf.Max(segment.a.x,segment.b.x)<=90.01f,"segment stays in reviewed bounds");
   Check(Mathf.Max(segment.a.x,segment.b.x)<=40.01f||Mathf.Min(segment.a.x,segment.b.x)>=59.99f,"declared opening remains clear");
   Check(Mathf.Abs(segment.a.z)<=2.01f&&Mathf.Abs(segment.b.z)<=2.01f,"geometry remains inside source boundaries, clear of live lanes at z +/-7");
  }
  Check(ReviewedRoadFacilities.Plan(new MapData{mapId="other",hdMarks=map.hdMarks},catalog).barriers.Count==0,"map ID guards facility placement");
  var root=new GameObject("facility test");try{ReviewedRoadFacilities.Draw(map,root.transform,false);Check(root.GetComponentsInChildren<Collider>().Length==0,"authoring preview has no colliders");}finally{UnityEngine.Object.DestroyImmediate(root);}
  var package=ProjectMapStore.Load(ProjectMapBuild.Root);
  var actual=package.baseline;
  var json=File.ReadAllText(Path.Combine(ProjectMapBuild.Root,"config/maps/eungye-road-facilities.json"));
  var reviewed=JsonUtility.FromJson<ReviewedRoadFacilities.Catalog>(json);
  var real=ReviewedRoadFacilities.Plan(actual,reviewed);
  Check(real.barriers.Count>15&&real.hatches.Count>15,"reviewed Suin-ro site has segmented rail and strip hatching");
  Check(real.cushions.Count==1&&real.cushions[0].a.x>=230&&real.cushions[0].b.x<=235,"western crash cushion stays within reviewed strip before the barrier");
  foreach(var rail in real.barriers){
   var point=(rail.a+rail.b)*.5f;float nearest=float.MaxValue;
   foreach(var lane in actual.hdEdges){if(lane.disabled||lane.points==null)continue;for(int i=1;i<lane.points.Length;i++){Vector3 a=lane.points[i-1].V(),b=lane.points[i].V(),delta=b-a;float t=Mathf.Clamp01(Vector3.Dot(point-a,delta)/Mathf.Max(.0001f,delta.sqrMagnitude));nearest=Mathf.Min(nearest,Vector3.Distance(point,a+delta*t));}}
   Check(nearest>2.4f,"median rail clear of all live lane center corridors: "+nearest);
  }
  var live=new GameObject("live median test");try{ReviewedRoadFacilities.Draw(actual,live.transform,true);Check(live.GetComponentsInChildren<Collider>().Length>=real.barriers.Count+real.cushions.Count,"every rail and cushion segment is collidable");}finally{UnityEngine.Object.DestroyImmediate(live);}
  Debug.Log("REVIEWED_ROAD_FACILITIES_TEST_OK barriers="+layout.barriers.Count+" hatch="+layout.hatches.Count);
 }
}
