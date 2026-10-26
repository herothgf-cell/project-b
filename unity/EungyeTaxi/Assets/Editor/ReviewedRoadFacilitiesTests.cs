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
  var postRail=new ReviewedRoadFacilities.Catalog{mapId="fixture",facilities=new[]{new ReviewedRoadFacilities.Facility{id="post-rail",segments=new[]{new ReviewedRoadFacilities.ExplicitSegment{kind="post-rail",width=.25f,height=.9f,points=new[]{new MapPoint{x=20,z=0},new MapPoint{x=20,z=5}}}}}}};
  var railLayout=ReviewedRoadFacilities.Plan(map,postRail);Check(railLayout.barriers.Count==3&&railLayout.barriers.All(l=>Vector3.Distance(l.a,l.b)<=2.001f),"observed post rail is collidable layout with posts at most2m apart");
  Check(ReviewedRoadFacilities.Plan(new MapData{mapId="other",hdMarks=map.hdMarks},catalog).barriers.Count==0,"map ID guards facility placement");
  var root=new GameObject("facility test");try{ReviewedRoadFacilities.Draw(map,root.transform,false);Check(root.GetComponentsInChildren<Collider>().Length==0,"authoring preview has no colliders");}finally{UnityEngine.Object.DestroyImmediate(root);}
  var package=ProjectMapStore.Load(ProjectMapBuild.Root);
  var actual=package.Compose();
  var json=File.ReadAllText(Path.Combine(ProjectMapBuild.Root,"config/maps/eungye-road-facilities.json"));
  var reviewed=JsonUtility.FromJson<ReviewedRoadFacilities.Catalog>(json);
  var real=ReviewedRoadFacilities.Plan(actual,reviewed);
  Check(real.barriers.Count>15&&real.hatches.Count>15,"reviewed Suin-ro site has segmented rail and strip hatching");
  Check(real.cushions.Count==1&&real.cushions[0].a.x>=230&&real.cushions[0].b.x<=235,"western crash cushion stays within reviewed strip before the barrier");
  Check(SegmentDistance(Vector3.zero,Vector3.right*10,new Vector3(10,0,2.2f),new Vector3(20,0,2.2f))<RequiredClearance(3.5f,.25f),"endpoint-near corridor is rejected although the rail midpoint is far away");
  Check(SegmentDistance(Vector3.zero,Vector3.right*10,new Vector3(5,0,-5),new Vector3(5,0,5))==0,"interior crossing is rejected although all endpoints are distant");
  Check(RequiredClearance(4,.4f)>RequiredClearance(3.5f,.25f),"clearance accounts for actual lane and rail widths");
  var widths=package.document.lanes.ToDictionary(l=>l.id,l=>l.width);var nearby=new System.Collections.Generic.HashSet<int>();
  float minimumGap=float.MaxValue;string closest="";
  foreach(var rail in real.barriers.Concat(real.cushions)){
   for(int laneIndex=0;laneIndex<actual.hdEdges.Length;laneIndex++){var lane=actual.hdEdges[laneIndex];if(lane.disabled||lane.points==null)continue;
    for(int i=1;i<lane.points.Length;i++){float distance=SegmentDistance(rail.a,rail.b,lane.points[i-1].V(),lane.points[i].V());
     if(distance<8)nearby.Add(laneIndex);float gap=distance-widths[lane.id]*.5f-rail.width*.5f;
     if(gap<minimumGap){minimumGap=gap;closest=rail.facilityId+" / "+lane.id;}
     Check(distance>=RequiredClearance(widths[lane.id],rail.width),"complete rail/cushion and lane corridor plus 0.4m margin: "+rail.facilityId+" / "+lane.id+" gap="+gap);
    }
   }
  }
  Debug.Log("REVIEWED_ROAD_FACILITY_MIN_GAP "+minimumGap+"m "+closest);
  var live=new GameObject("live median test");try{
   ReviewedRoadFacilities.Draw(actual,live.transform,true);Check(live.GetComponentsInChildren<Collider>().Length>=real.barriers.Count+real.cushions.Count,"every rail and cushion segment is collidable");Physics.SyncTransforms();int samples=0;
   foreach(int index in nearby){var lane=actual.hdEdges[index];var path=new AssistedRoadNetwork.Path(lane.points.Select(p=>p.V()).ToArray());
    for(float station=0;station<=path.length;station+=.5f){var point=path.Position(station);bool close=false;foreach(var rail in real.barriers.Concat(real.cushions))if(PointDistance(point,rail.a,rail.b)<5){close=true;break;}if(!close)continue;
     var hits=Physics.OverlapBox(point+Vector3.up*.85f,new Vector3(1.05f,.55f,2.3f),Quaternion.LookRotation(path.Tangent(station)),~0,QueryTriggerInteraction.Ignore);
     Check(!hits.Any(c=>c.transform.IsChildOf(live.transform)),"4.6x2.1m taxi box clears actual rail colliders: "+lane.id+" station="+station);samples++;
    }
   }Check(samples>100,"actual nearby lane taxi sweep has meaningful coverage");Debug.Log("REVIEWED_ROAD_FACILITY_TAXI_SWEEP samples="+samples);
  }finally{UnityEngine.Object.DestroyImmediate(live);}
  Debug.Log("REVIEWED_ROAD_FACILITIES_TEST_OK barriers="+layout.barriers.Count+" hatch="+layout.hatches.Count);
 }

 // Reserve a 0.4m shoulder outside the entire authored lane, in addition to each physical half-width.
 static float RequiredClearance(float laneWidth,float railWidth){return laneWidth*.5f+railWidth*.5f+.4f;}
 static float PointDistance(Vector3 point,Vector3 a,Vector3 b){var delta=b-a;float t=Mathf.Clamp01(Vector3.Dot(point-a,delta)/Mathf.Max(.000001f,delta.sqrMagnitude));return Vector3.Distance(point,a+delta*t);}
 static float SegmentDistance(Vector3 a,Vector3 b,Vector3 c,Vector3 d){RoadGeometry.Point hit;if(RoadGeometry.TryCrossing(new RoadGeometry.Point(a.x,a.z),new RoadGeometry.Point(b.x,b.z),new RoadGeometry.Point(c.x,c.z),new RoadGeometry.Point(d.x,d.z),out hit))return 0;return Mathf.Min(Mathf.Min(PointDistance(a,c,d),PointDistance(b,c,d)),Mathf.Min(PointDistance(c,a,b),PointDistance(d,a,b)));}
}
