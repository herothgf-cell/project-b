using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class RoadNavigation {
 bool HomeAccessAvailable {
  get{var owner=NetworkSession;return owner&&owner.services&&owner.services.HasHomeBase;}
 }
 bool HomeEntry(int target,float targetAt,out List<Vector3> result){
  result=null;if(!HomeAccessAvailable)return false;
  var road=HomeGarage.Approach(editorMap);
  if(target!=road.laneEdge||Mathf.Abs(targetAt-road.laneAt)>.25f)return false;
  var points=HomeGarage.EntryPath(editorMap);
  var path=new AssistedRoadNetwork.Path(points);float error;
  float at=path.Project(vehicle.position,out error);
  if(error>3||!HomeGarage.Contains(editorMap,vehicle.position)&&Vector3.Dot(vehicle.forward,path.Tangent(at))<.5f)return false;
  result=RemainingAccessPath(points,path,at);return true;
 }
 // Geometry is shared by previews, dispatch quotes and active guidance. The
 // graph still decides which public lanes and legal turns are traversable.
 sealed class FacilityDrive{
  public AssistedRoadNetwork.Path path;public MapPoint[] footprint;public Bounds bounds;
  public int join;public float at;
 }
 List<FacilityDrive> facilityDrives;
 static int JoinLane(HdRoadGraph graph,Vector3 end,MapStop approach,Vector3 heading,out float at){
  at=0;if(graph==null)return -1;
  if(approach!=null&&approach.graphStop&&!graph.edges[approach.laneEdge].TraversalDisabled){
   float error;at=graph.paths[approach.laneEdge].Project(end,out error);
   if(error<.5f)return approach.laneEdge;
  }
  return graph.Nearest(end,heading,out at);
 }
 public static List<Vector3> FacilityDepartureToRoad(Vector3[] departure,MapStop approach,Vector3 heading,HdRoadGraph graph){
  var leave=new List<Vector3>(departure);
  if(leave.Count==0)return leave;
  float at;int edge=JoinLane(graph,leave[leave.Count-1],approach,heading,out at);
  if(edge<0)return leave;
  // An authored split can put the exit beyond the entrance edge. Follow the
  // actual exit lane and its connected successor until the car straightens.
  var forward=graph.Forward(edge,at);
  if(forward!=null)for(int step=1;step<=12&&step<=forward.path.length;step++)leave.Add(forward.path.Position(step));
  return leave;
 }
 void AddDrive(Vector3[] points,MapPoint[] footprint,MapStop approach,Vector3 heading){
  if(points==null||points.Length<2)return;
  float at;int join=JoinLane(hd,points[points.Length-1],approach,heading,out at);
  if(join<0)return;
  var bounds=new Bounds(points[0],Vector3.zero);foreach(var point in points)bounds.Encapsulate(point);
  if(footprint!=null)foreach(var point in footprint)bounds.Encapsulate(point.V());
  bounds.Expand(new Vector3(5,4,5));
  facilityDrives.Add(new FacilityDrive{path=new AssistedRoadNetwork.Path(points),footprint=footprint,bounds=bounds,join=join,at=at});
 }
 void AddStation(FuelStationLayout.FuelStation station){
  var points=station.drivePath;
  if(Vector3.Distance(points[points.Length-1],station.pump)<.1f)points=points.Reverse().ToArray();
  AddDrive(points,Array.ConvertAll(station.plot,q=>new MapPoint{x=q.x,z=q.z}),station.approach,station.direction);
 }
 void AddRestaurant(RestaurantSiteLayout.Site site,MapPoint[] footprint){
  // Edited sites can share the entrance and exit. Prefer the outbound leg
  // when both project to the same location in the court.
  AddDrive(site.leave,footprint,site.approach,site.approach.Direction);
  AddDrive(site.enter.Concat(site.leave).ToArray(),footprint,site.approach,site.approach.Direction);
 }
 void BuildFacilityDrives(){
  facilityDrives=new List<FacilityDrive>();
  foreach(var station in FuelStationLayout.For(editorMap))AddStation(station);
  var rest=FuelStationLayout.Rest(editorMap);if(rest!=null)AddStation(rest);
  foreach(var shop in WorkshopLayout.For(editorMap)){
   var court=shop.surfaces[0];var a=court.points[0].V();var b=court.points[1].V();
   var ring=ProjectMapScene.Rectangle((a+b)*.5f,(b-a).normalized,court.width,(b-a).magnitude);
   AddDrive(shop.departure,ring,shop.approach,shop.direction);
   AddDrive(shop.waitingToRepair.Concat(shop.departure).ToArray(),ring,shop.approach,shop.direction);
   AddDrive(shop.entry.Concat(shop.departure).ToArray(),ring,shop.approach,shop.direction);
   AddDrive(shop.waitingEntry.Concat(shop.waitingToRepair).Concat(shop.departure).ToArray(),ring,shop.approach,shop.direction);
  }
  if(ProjectMapScene.Active(editorMap)){
   foreach(var parking in ProjectMapScene.Services(editorMap,"parking")){
    var road=Array.Find(editorMap.roads,r=>r.id==parking.id+":access");
    if(road!=null)AddDrive(Array.ConvertAll(road.points,p=>p.V()),parking.points,null,parking.Direction);
    // Legacy curbside parking already uses its public lane. It has no driveway.
   }
   foreach(var obj in ProjectMapScene.Services(editorMap,"restaurant"))
    if(!obj.id.StartsWith("city:meal:"))AddRestaurant(RestaurantSiteLayout.FromObject(editorMap,obj),obj.points);
  }else{
   var site=RestaurantSiteLayout.For(editorMap);
   if(site!=null)AddRestaurant(site,ProjectMapScene.Rectangle(site.parking,site.direction,10,14));
  }
 }
 int DepartureOrigin(out float at,out List<Vector3> access,out bool home){
  var position=vehicle.position;access=null;home=false;
  int edge=hd.Nearest(position,vehicle.forward,out at);
  if(editorMap==null||Mathf.Abs(position.y)>2)return edge;
  if(HomeAccessAvailable){
   var exit=HomeGarage.ExitPath(editorMap);
   if(HomeGarage.Contains(editorMap,position)||edge<0&&HomeGarage.Paved(editorMap,position)){
    var path=new AssistedRoadNetwork.Path(exit);float error;
    access=RemainingAccessPath(exit,path,path.Project(position,out error));home=true;
    var join=HomeGarage.ExitRoad(editorMap);at=join.laneAt;return join.laneEdge;
   }
  }
  if(facilityDrives==null)BuildFacilityDrives();
  FacilityDrive best=null;float bestError=float.MaxValue,progress=0;
  foreach(var drive in facilityDrives){
   if(!drive.bounds.Contains(position))continue;
   float error;float projected=drive.path.Project(position,out error);
   bool inside=drive.footprint!=null&&ProjectMapScene.Contains(drive.footprint,position);
   if(!inside&&(edge>=0||error>2.4f)||error>=bestError)continue;
   best=drive;progress=projected;bestError=error;
  }
  if(best==null)return edge;
  if(hd.edges[best.join].TraversalDisabled)return -1;
  access=RemainingAccessPath(best.path.points,best.path,progress);at=best.at;return best.join;
 }
 internal bool RouteFromVehicle(int target,float targetAt,out List<Vector3> access,out HdRoadGraph.RoutePath road){
  float at;bool home;int start=DepartureOrigin(out at,out access,out home);road=null;
  if(start<0||hd.edges[start].TraversalDisabled)return false;
  if(start==target&&Mathf.Abs(at-targetAt)<.25f)return true;
  road=hd.Route(start,at,target,targetAt);return road!=null;
 }
 List<Vector3> PreviewHd(int target,float targetAt){
  List<Vector3> points;HdRoadGraph.RoutePath road;
  if(HomeEntry(target,targetAt,out points))return points;
  if(!RouteFromVehicle(target,targetAt,out points,out road))return new List<Vector3>();
  if(points==null)points=new List<Vector3>();
  if(road!=null)points.AddRange(road.path.points);
  if(points.Count==0)points.Add(vehicle.position);
  return points;
 }
 bool AtDirectedStop(MapStop stop){
  if(vehicle==null||Mathf.Abs(vehicle.position.y-stop.Kerb.y)>2||(vehicle.position-stop.Kerb).sqrMagnitude>36)return false;
  var body=vehicle.GetComponent<Rigidbody>();
  if(!stop.CanStop(vehicle.position,vehicle.forward,body?body.velocity.magnitude:0))return false;
  if(hd==null)return true;
  return stop.graphStop&&stop.laneEdge>=0&&stop.laneEdge<hd.edges.Length&&!hd.edges[stop.laneEdge].TraversalDisabled;
 }
 public float DistanceFromVehicle(MapStop stop){
  if(AtDirectedStop(stop))return 0;
  if(hd==null)return Vector3.Distance(vehicle.position,stop.Kerb);
  List<Vector3> access;HdRoadGraph.RoutePath road;
  if(!RouteFromVehicle(stop.laneEdge,stop.laneAt,out access,out road))return float.PositiveInfinity;
  return (access==null?0:Length(access))+(road==null?0:road.path.length);
 }
 List<Vector3> RemainingAccessPath(Vector3[] points,AssistedRoadNetwork.Path path,float progress){
  var result=new List<Vector3>{new Vector3(vehicle.position.x,0,vehicle.position.z),path.Position(progress)};
  float walked=0;for(int i=1;i<points.Length;i++){walked+=Vector3.Distance(points[i-1],points[i]);if(walked>progress)result.Add(points[i]);}return result;
 }
}
