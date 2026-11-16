using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class FlexibleBollardTests {
 static void Check(bool pass,string message){if(!pass)throw new Exception("FLEXIBLE_BOLLARD: "+message);}
 static ReviewedRoadFacilities.ExplicitSegment Segment(params MapPoint[] points){return new ReviewedRoadFacilities.ExplicitSegment{kind="flexible-bollard",width=.08f,height=.75f,points=points};}
 static MapPoint Point(float x,float z){return new MapPoint{x=x,z=z};}
 static List<ReviewedRoadFacilities.Line> Posts(ReviewedRoadFacilities.Layout layout){
  var field=typeof(ReviewedRoadFacilities.Layout).GetField("bollards");
  Check(field!=null,"explicit flexible bollards need a separate point layout, never W-beam barriers");
  return (List<ReviewedRoadFacilities.Line>)field.GetValue(layout);
 }
 public static void Run(){
  var map=new MapData{mapId="bollard-fixture"};
  var catalog=new ReviewedRoadFacilities.Catalog{mapId=map.mapId,facilities=new[]{new ReviewedRoadFacilities.Facility{id="observed-posts",segments=new[]{
   Segment(Point(0,0),Point(3,0),Point(3,3)),Segment(Point(10,0),Point(14,0))
  }}}};
  var layout=ReviewedRoadFacilities.Plan(map,catalog);var posts=Posts(layout);
  Check(posts.Count==7,"2m spacing follows the bent polyline continuously and retains endpoints");
  Check(layout.barriers.Count==0&&layout.hatches.Count==0,"bollards never create a continuous barrier or inferred hatch");
  var expected=new[]{new Vector3(0,0,0),new Vector3(2,0,0),new Vector3(3,0,1),new Vector3(3,0,3),new Vector3(10,0,0),new Vector3(12,0,0),new Vector3(14,0,0)};
  foreach(var point in expected)Check(posts.Any(p=>Vector3.Distance(p.a,point)<.001f),"post follows authored path at "+point);
  Check(posts.All(p=>p.a==p.b&&p.width==.08f&&p.height==.75f),"each post has only its authored narrow footprint");
  Check(Posts(ReviewedRoadFacilities.Plan(new MapData{mapId="other"},catalog)).Count==0,"map guard prevents blanket placement");
  catalog.facilities[0].segments=new[]{Segment(Point(20,0),Point(20,0),Point(20.5f,0))};
  var shortPosts=Posts(ReviewedRoadFacilities.Plan(map,catalog));
  Check(shortPosts.Count==2,"duplicate vertices do not duplicate posts and short segments retain endpoints");
  var draw=typeof(ReviewedRoadFacilities).GetMethod("Draw",BindingFlags.Public|BindingFlags.Static,null,new[]{typeof(ReviewedRoadFacilities.Layout),typeof(Transform),typeof(bool)},null);
  Check(draw!=null,"fixture layout can exercise actual rendering");
  var materials=new List<Material>();Action<Material> capture=m=>materials.Add(m);WorldBuilder.MaterialCreated+=capture;
  try{foreach(bool collisions in new[]{false,true}){
   var root=new GameObject("flexible bollard fixture");
   try{
    draw.Invoke(null,new object[]{layout,root.transform,collisions});Physics.SyncTransforms();
    var colliders=root.GetComponentsInChildren<Collider>();
    Check(colliders.Length==(collisions?posts.Count:0),"exactly one narrow collider per post, zero in preview");
    foreach(var collider in colliders)Check(collider.bounds.size.x<=.081f&&collider.bounds.size.z<=.081f,"no invisible rail spans between posts");
    foreach(var point in new[]{new Vector3(1,0,0),new Vector3(7,0,0)}){
     var hits=Physics.OverlapBox(point+Vector3.up*.35f,new Vector3(.2f,.3f,.2f));
     Check(!hits.Any(c=>c.transform.IsChildOf(root.transform)),"space between posts and declared opening stay clear");
    }
    var renderers=root.GetComponentsInChildren<Renderer>();
    Check(renderers.Count(r=>r.sharedMaterial.name=="Reviewed flexible bollard orange")==posts.Count,"every post has orange body");
    Check(renderers.Count(r=>r.sharedMaterial.name=="Reviewed flexible bollard reflector")==posts.Count*2,"every post has two white reflective bands");
   }finally{UnityEngine.Object.DestroyImmediate(root);}
  }}finally{WorldBuilder.MaterialCreated-=capture;foreach(var material in materials)UnityEngine.Object.DestroyImmediate(material);}
  Debug.Log("FLEXIBLE_BOLLARD_TEST_OK posts="+posts.Count+" preview/live geometry and individual collision checked");
 }

 public static void BuildCommercialMap(){
  Run();
  var package=ProjectMapStore.Load(ProjectMapBuild.Root);
  var map=package.Compose();
  EungyeConnectivityRepair.Apply(map,package.baseline);
  var graph=new HdRoadGraph(map);
  string catalogPath=Path.Combine(ProjectMapBuild.Root,"config/maps/eungye-road-facilities.json");
  var catalog=JsonUtility.FromJson<ReviewedRoadFacilities.Catalog>(File.ReadAllText(catalogPath));
  var layout=ReviewedRoadFacilities.Plan(map,catalog);
  string commerceId="eungye-beonyeong-gil-P13-bollards";
  string parkingId="eungye-public-parking-P20-P26-bollards";
  var commerce=layout.bollards.Where(p=>p.facilityId==commerceId).ToArray();
  var parking=layout.bollards.Where(p=>p.facilityId==parkingId).ToArray();
  Check(commerce.Count(p=>p.a.x<643.5f)==3&&commerce.Count(p=>p.a.x>652.5f)==3&&commerce.Length==6,
   "P13 has three posts on each side of the signal crossing with both stop lines clear");
  Check(parking.Count(p=>p.a.x<642)==4&&parking.Count(p=>p.a.x>660)==7&&parking.Length==11,
   "parking has two rows with a clear crossing and driveway opening");
  Check(!layout.barriers.Concat(layout.hatches).Any(p=>p.facilityId==commerceId||p.facilityId==parkingId),
   "new post groups have discrete collision footprints");
  float minimum=float.MaxValue;
  foreach(var post in commerce.Concat(parking)){
   for(int i=0;i<graph.edges.Length;i++){
    if(graph.edges[i].disabled)continue;
    graph.paths[i].Project(post.a,out float distance);
    if(distance<minimum)minimum=distance;
    Check(distance>=1.4f,"post must clear an active lane or connector: "+post.a+" / "+graph.edges[i].id+" at "+distance);
   }
  }
  Debug.Log("COMMERCIAL_BOLLARDS_OK P13="+commerce.Length+" parking="+parking.Length+" laneClearance="+minimum.ToString("F2")+"m");
  PrototypeBuild.BuildHybrid();
 }
}
