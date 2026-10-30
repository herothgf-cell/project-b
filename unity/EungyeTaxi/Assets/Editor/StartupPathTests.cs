using System;
using System.Collections.Generic;
using UnityEngine;

public static class StartupPathTests {
 public static void RunValidationBroadPhase(){
  var package=JsonUtility.FromJson<ProjectMapPackage>(Resources.Load<TextAsset>(ProjectMapStore.ResourceName).text);
  int cases=0;
  Action<RoadMapDocument,bool> compare=(document,expectError)=>{
   var slow=ProjectMapScene.ValidateWithoutBroadPhase(package.baseline,document);
   var fast=ProjectMapScene.Validate(package.baseline,document);
   Check(slow.Count==fast.Count,"broad phase issue count");
   for(int i=0;i<slow.Count;i++)Check(slow[i].id==fast[i].id&&slow[i].message==fast[i].message&&slow[i].error==fast[i].error&&slow[i].position.Equals(fast[i].position),"broad phase ordered issue equality");
   if(expectError)Check(fast.Exists(x=>x.error&&x.id=="startup-validation-obstacle"&&x.message.StartsWith("주행 차로 침범")),"intentional lane overlap still rejected");cases++;
  };
  compare(package.document,false);
  var edited=package.document.Copy();var lane=Array.Find(edited.lanes,l=>!l.disabled&&l.points.Length>1&&l.turn==0&&l.points[0].x>10&&l.points[0].z>10&&l.points[0].x<package.baseline.width-40&&l.points[0].z<package.baseline.depth-40);var center=lane.points[0].V();
  var objects=new List<ProjectMapObject>(edited.scene.objects);var blocker=new ProjectMapObject{id="startup-validation-obstacle",kind="building",name="regression obstacle",height=4,width=4,points=ProjectMapScene.Rectangle(center,Vector3.forward,6,6)};objects.Add(blocker);edited.scene.objects=objects.ToArray();
  compare(edited,true);
  // Reuse and mutate the very same arrays: per-call bounds must never be stale.
  for(int i=0;i<blocker.points.Length;i++){blocker.points[i].x+=25;blocker.points[i].z+=25;}
  compare(edited,false);
  Debug.Log("STARTUP_VALIDATION_BROAD_PHASE_OK cases="+cases);
 }
 static void Check(bool value,string message){if(!value)throw new Exception("STARTUP_CURRENT_MAP: "+message);}
 public static void RunCurrentMapRoutes(){
  var map=MapCatalog.Load();var host=new GameObject("startup current map routes");
  try{
   var system=host.AddComponent<TrafficSystem>();system.Build(map,false);
   var district=host.AddComponent<DistrictTraffic>();district.Initialize(system,map,false);
   int pairs=0,samples=0,signals=0,left=0,right=0;
   foreach(var a in map.stops)foreach(var b in map.stops){
    if(a==b)continue;var route=district.CreateRoute(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt);
    Check(route!=null&&route.graphPath!=null,"current map route exists");pairs++;
    // Independent linear interpolation checks the entire cached junction grid.
    var cumulative=new float[route.points.Length];for(int i=1;i<cumulative.Length;i++)cumulative[i]=cumulative[i-1]+Vector3.Distance(route.points[i-1],route.points[i]);
    var cached=new Vector3[Mathf.FloorToInt(route.length/2)+1];for(int i=0;i<cached.Length;i++)cached[i]=route.Position(i*2f);
    for(float d=0;d<=route.length+2;d+=2)if(d<=route.length){Check(cached[(int)(d/2)].Equals(Linear(route.points,cumulative,d)),"junction sample grid equals legacy");samples++;}
    foreach(var stop in route.stops){Check(stop.distance>=0&&stop.exitDistance<=route.length,"stop bounds");if(stop.junction.signalized)signals++;if(stop.turn<0)left++;if(stop.turn>0)right++;}
   }
   Check(pairs>0&&signals>0&&left>0&&right>0,"current routes include signals and both turns");
   Debug.Log("STARTUP_CURRENT_MAP_OK pairs="+pairs+" samples="+samples+" signals="+signals+" left="+left+" right="+right);
  }finally{UnityEngine.Object.DestroyImmediate(host);}
 }
 // Runtime callers can validate the fully built scene without edit-mode car creation.
 public static void ValidatePopulation(TrafficSystem system){
  Check(system.cars.Count==DistrictTraffic.PoolCap,"production allocated car pool");int visible=0;var cells=new HashSet<Vector2Int>();
  foreach(var car in system.cars){Check(car.Visible,"seed visible");visible++;var p=car.transform.position;cells.Add(new Vector2Int(Mathf.FloorToInt(p.x/250),Mathf.FloorToInt(p.z/250)));Check(car.route.graphPath!=null,"shared directed graph");float error;car.route.graphPath.path.Project(p,out error);Check(error<.2f,"seed on route");
   foreach(var other in system.cars)if(other!=car)Check((p-other.transform.position).sqrMagnitude>=99.9f,"10m initial spacing");
  }
  Check(cells.Count>=24,"district distribution at least24 cells");Debug.Log("STARTUP_POPULATION_OK visible="+visible+" cells="+cells.Count);
 }
 static Vector3 Linear(Vector3[] points,float[] cumulative,float d){for(int i=1;i<points.Length;i++)if(d<=cumulative[i])return Vector3.Lerp(points[i-1],points[i],Mathf.InverseLerp(cumulative[i-1],cumulative[i],d));return points[points.Length-1];}
 public static void Run(){
  int checks=0;var random=new System.Random(1927);
  foreach(int count in new[]{1,2,3,32,511}){
   var points=new Vector3[count];var cumulative=new float[count];
   for(int i=1;i<count;i++){points[i]=i%7==0?points[i-1]:points[i-1]+new Vector3((float)random.NextDouble()*4,(float)random.NextDouble(),(float)random.NextDouble()*5);cumulative[i]=cumulative[i-1]+Vector3.Distance(points[i-1],points[i]);}
   var path=new AssistedRoadNetwork.Path(points);
   Action<float> check=d=>{if(!path.Position(d).Equals(Linear(points,cumulative,d)))throw new Exception("STARTUP_PATH mismatch count="+count+" distance="+d);checks++;};
   check(float.NaN);check(float.NegativeInfinity);check(float.PositiveInfinity);check(-10);check(path.length+10);
   for(int i=0;i<count;i++){check(cumulative[i]);check(cumulative[i]-.0001f);check(cumulative[i]+.0001f);}
   for(int i=0;i<2000;i++)check((float)random.NextDouble()*(path.length+20)-10);
  }
  Debug.Log("STARTUP_PATH_OK checks="+checks);
 }
}
