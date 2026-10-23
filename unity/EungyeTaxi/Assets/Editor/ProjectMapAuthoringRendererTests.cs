using System;
using UnityEngine;
using System.Linq;
public static class ProjectMapAuthoringRendererTests {
 static void Check(bool ok,string message){if(!ok)throw new Exception("AUTHORING_RENDERER: "+message);}
 public static void FragmentedAreas(){
  var o=new ProjectMapObject{id="area:split",kind="sidewalk",width=10,height=.14f,enabled=true,points=ProjectMapScene.Rectangle(new Vector3(5,0,5),Vector3.forward,10,10)};
  Func<float,string,MapArea> area=(x,id)=>{var points=ProjectMapScene.Rectangle(new Vector3(x,0,5),Vector3.forward,4,10);return new MapArea{kind="sidewalk",evidence=id,height=.14f,points=points,triangles=ProjectMapScene.Triangulate(points)};};
  var map=new MapData{mapId="fixture",width=40,depth=40,roads=new MapRoad[0],buildings=new MapBuilding[0],targets=new MapTarget[0],stops=new MapStop[0],areas=new[]{area(2,o.id),area(8,o.id),area(22,"runtime:pocket")},projectScene=new ProjectMapScene{objects=new[]{o}}};
  using(var renderer=new ProjectMapAuthoringRenderer()){
   renderer.Initialize(map);Check(renderer.Root.GetComponentsInChildren<MeshFilter>().Any(f=>f.sharedMesh.vertices.Any(v=>v.x>20)),"runtime-only surface remains rendered");Check(renderer.Pick(new Ray(new Vector3(2,20,5),Vector3.down),"sidewalk")==o.id,"first fragment retained for authored area");Check(renderer.Pick(new Ray(new Vector3(8,20,5),Vector3.down),"sidewalk")==o.id,"last fragment retained");Check(renderer.BoundsFor(o.id).size.x>9.9f,"bounds include all fragments");Check(renderer.Pick(new Ray(new Vector3(5,20,5),Vector3.down),"sidewalk")==null,"cut gap stays empty");Check(renderer.Pick(new Ray(new Vector3(22,20,5),Vector3.down),"sidewalk")==null,"runtime-only area is rendered but not offered as an editable source ID");
  }Debug.Log("FRAGMENTED_AREA_TEST_OK");
 }
 public static void Run(){
  var type=typeof(WorldBuilder).Assembly.GetType("ProjectMapAuthoringRenderer")??Type.GetType("ProjectMapAuthoringRenderer, Assembly-CSharp-Editor");
  Check(type!=null,"renderer implementation exists");
  foreach(var name in new[]{"Initialize","UpdateObject","Pick","BoundsFor","Dispose"})Check(type.GetMethod(name)!=null,name+" public contract");
  Check(type.GetProperty("Root")!=null,"owned root");
  var points=new[]{new MapPoint{x=10,z=10},new MapPoint{x=20,z=10},new MapPoint{x=20,z=20},new MapPoint{x=10,z=20}};
  var obj=new ProjectMapObject{id="building:test",kind="building",height=12,points=points};
  var map=new MapData{width=512,depth=512,roads=new MapRoad[0],buildings=new MapBuilding[0],areas=new MapArea[0],targets=new MapTarget[0],stops=new MapStop[0],projectScene=new ProjectMapScene{objects=new[]{obj,new ProjectMapObject{id="building:far",kind="building",height=7,points=Array.ConvertAll(points,p=>new MapPoint{x=p.x+256,z=p.z})},new ProjectMapObject{id="sidewalk:concave",kind="sidewalk",height=.14f,points=new[]{new MapPoint{x=40,z=10},new MapPoint{x=60,z=10},new MapPoint{x=60,z=15},new MapPoint{x=45,z=15},new MapPoint{x=45,z=30},new MapPoint{x=40,z=30}}}}}};
  ProjectMapScene.Apply(map,map.projectScene);
  var renderer=Activator.CreateInstance(type);type.GetMethod("Initialize").Invoke(renderer,new object[]{map,null});
  try{
   var bounds=(Bounds)type.GetMethod("BoundsFor").Invoke(renderer,new object[]{obj.id});Check(bounds.size.y>=12,"runtime building height");
   Check((string)type.GetMethod("Pick").Invoke(renderer,new object[]{new Ray(new Vector3(15,100,15),Vector3.down),"building"})==obj.id,"exact geometry picking");
   Check(type.GetMethod("Pick").Invoke(renderer,new object[]{new Ray(new Vector3(55,100,25),Vector3.down),"sidewalk"})==null,"concave AABB hole is not selectable");
   var root=(Transform)type.GetProperty("Root").GetValue(renderer);
   Check(root.GetComponentsInChildren<Collider>().Length==0,"editor creates no per-object colliders");
   var reference=new GameObject("runtime geometry parity");
   try{reference.AddComponent<WorldBuilder>().BuildStatic(map,false,false);
    foreach(var group in reference.GetComponentsInChildren<MeshFilter>().GroupBy(f=>f.GetComponent<Renderer>().sharedMaterial.name)){
     var editor=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.GetComponent<Renderer>().sharedMaterial.name==group.Key).ToArray();
     var expected=group.SelectMany(f=>f.sharedMesh.vertices).ToArray();var actual=editor.SelectMany(f=>f.sharedMesh.vertices).ToArray();
     Check(expected.SequenceEqual(actual),"runtime vertex parity "+group.Key);
     Check(group.Sum(f=>f.sharedMesh.triangles.Length)==editor.Sum(f=>f.sharedMesh.triangles.Length),"runtime triangle parity "+group.Key);
     Check(editor.All(f=>f.GetComponent<Renderer>().sharedMaterial.color==WorldBuilder.SurfaceColor(group.Key=="Buildings"?"building":group.Key=="Road"?"road":group.Key)),"runtime material parity "+group.Key);
    }
   }finally{foreach(var f in reference.GetComponentsInChildren<MeshFilter>())UnityEngine.Object.DestroyImmediate(f.sharedMesh);foreach(var m in reference.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Distinct())UnityEngine.Object.DestroyImmediate(m);UnityEngine.Object.DestroyImmediate(reference);}
   var far=root.GetComponentsInChildren<MeshFilter>().First(f=>f.GetComponent<Renderer>().sharedMaterial.name=="Buildings"&&f.sharedMesh.bounds.center.x>128).sharedMesh;
   int full=(int)type.GetProperty("FullBuildCount").GetValue(renderer);
   obj.height=24;type.GetMethod("UpdateObject").Invoke(renderer,new object[]{obj});
   Check((int)type.GetProperty("FullBuildCount").GetValue(renderer)==full,"drag does not rebuild world");
   Check(far&&far.bounds.size.y==7,"unaffected chunk mesh survives update");
   Check((int)type.GetProperty("LastUpdatedChunkCount").GetValue(renderer)==1,"one building updates one chunk");
   Check(((Bounds)type.GetMethod("BoundsFor").Invoke(renderer,new object[]{obj.id})).size.y>=24,"incremental bounds");
   obj.points=Array.ConvertAll(obj.points,p=>new MapPoint{x=p.x+128,z=p.z,elevation=p.elevation});type.GetMethod("UpdateObject").Invoke(renderer,new object[]{obj});
   Check((int)type.GetProperty("LastUpdatedChunkCount").GetValue(renderer)==2,"moving across boundary updates old and new chunks");
   Check(type.GetMethod("Pick").Invoke(renderer,new object[]{new Ray(new Vector3(15,100,15),Vector3.down),"building"})==null,"moved footprint removed from old chunk");
   Check((string)type.GetMethod("Pick").Invoke(renderer,new object[]{new Ray(new Vector3(143,100,15),Vector3.down),"building"})==obj.id,"moved footprint selectable in new chunk");
   obj.enabled=false;type.GetMethod("UpdateObject").Invoke(renderer,new object[]{obj});Check(type.GetMethod("Pick").Invoke(renderer,new object[]{new Ray(new Vector3(143,100,15),Vector3.down),"building"})==null,"disabled object removed from old chunk");
  }finally{type.GetMethod("Dispose").Invoke(renderer,null);}
  Debug.Log("AUTHORING_RENDERER_TESTS_OK");
 }
 public static void ClippedRingRoundTrip(){
  var baseline=ProjectMapStore.Load(ProjectMapBuild.Root).baseline;
  var capture=ProjectMapScene.Capture(baseline);var source=capture.objects.First(o=>o.kind=="building"&&o.points.All(p=>p.x>2&&p.z>2&&p.x<baseline.width-2&&p.z<baseline.depth-2));
  var building=baseline.buildings.First(b=>"building:"+b.id==source.id);
  building.points=building.points.Concat(new[]{new MapPoint{x=building.points.Last().x,z=building.points.Last().z},new MapPoint{x=building.points.Last().x,z=building.points.Last().z}}).ToArray();
  var affine=Array.ConvertAll(building.points,p=>{var q=Quaternion.Euler(0,37,0)*new Vector3(p.x*1.2f,0,p.z*.8f)+new Vector3(7,0,11);return new MapPoint{x=q.x,z=q.z};});
  Check(ProjectMapScene.AffineRing(building.points,affine),"rotation and nonuniform positive scale preserve trusted clipped topology");
  Check(!ProjectMapScene.AffineRing(building.points,Array.ConvertAll(building.points,p=>new MapPoint{x=-p.x,z=p.z})),"reflection cannot reverse trusted triangle winding");
  Check(!ProjectMapScene.AffineRing(building.points,Array.ConvertAll(building.points,p=>new MapPoint{x=p.x,z=0})),"collapsed clipped ring rejected");
  baseline.editorBaseHash="";var document=RoadMapDocument.Create(baseline);document.scene=ProjectMapScene.Capture(baseline);var target=document.scene.objects.First(o=>o.id==source.id);
  foreach(var p in target.points){p.x+=.01f;p.z+=.01f;}
  Check(!ProjectMapScene.Validate(baseline,document).Any(i=>i.error),"affine transformed clipped source ring validates");
  var package=ProjectMapPackage.Create(baseline,document);var composed=package.Compose();
  Check(composed.buildings.First(b=>"building:"+b.id==source.id).triangles.SequenceEqual(building.triangles),"trusted clipped triangulation retained");
  string root=System.IO.Path.GetFullPath("../../artifacts/clipped-ring-"+Guid.NewGuid().ToString("N"));System.IO.Directory.CreateDirectory(System.IO.Path.Combine(root,".git"));System.IO.Directory.CreateDirectory(System.IO.Path.Combine(root,"unity/EungyeTaxi/ProjectSettings"));System.IO.File.WriteAllText(System.IO.Path.Combine(root,"unity/EungyeTaxi/ProjectSettings/ProjectVersion.txt"),"fixture");
  ProjectMapStore.Save(root,package,"");var restored=ProjectMapStore.Load(root).Compose();Check(ProjectMapScene.SamePoints(restored.buildings.First(b=>"building:"+b.id==source.id).points,target.points),"clipped affine geometry survives canonical JSON save/load/compose");
  var invalid=document.Copy();invalid.scene.objects.First(o=>o.id==source.id).points.Last().x+=.2f;Check(ProjectMapScene.Validate(baseline,invalid).Any(i=>i.error&&i.id==source.id),"non-affine clipped topology rejected");
  bool rejected=false;try{RoadMapComposer.Compose(baseline,invalid);}catch(ArgumentException){rejected=true;}Check(rejected,"composition cannot bypass invalid clipped topology");
  Debug.Log("CLIPPED_RING_ROUNDTRIP_TEST_OK");
 }

}
