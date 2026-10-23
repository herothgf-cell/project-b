using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

// Fictional restaurant on an empty plot south of the user-marked block road.
// Navigation ends before the driveway; only the off-road bay permits meals.
public static class RestaurantSiteLayout {
 public sealed class Site {
  public MapStop approach;public Vector3 parking,direction,origin,building;public Vector3[] enter,leave;internal MapRoad[] surfaces;
  public bool CanDine(Vector3 position,Vector3 heading,float speed){var d=position-parking;return speed<.56f&&Mathf.Abs(d.y)<1.5f&&Vector3.Dot(heading.normalized,direction)>.9f&&Mathf.Abs(Vector3.Dot(d,direction))<2.7f&&Mathf.Abs(Vector3.Dot(d,TrafficSystem.Right(direction)))<1.15f;}
  public MapStop ParkingStop {get{return new MapStop{name="식당 앞 주차",x=parking.x,z=parking.z,elevation=parking.y,dx=direction.x,dz=direction.z,width=3};}}
 }
 static readonly ConditionalWeakTable<MapData,Site> sites=new ConditionalWeakTable<MapData,Site>();
 static MapRoad Surface(string id,float width,Vector3[] points){var ps=new MapPoint[points.Length];for(int i=0;i<ps.Length;i++)ps[i]=new MapPoint{x=points[i].x,z=points[i].z,elevation=points[i].y};return new MapRoad{id="restaurant:"+id,name="식당 진입·주차 공간",width=width,lanes=1,sourceLanes=1,oneWay=1,suppressPaint=true,points=ps};}
 static Vector3[] Curve(Vector3 a,Vector3 b,Vector3 c,Vector3 d){var points=new Vector3[33];for(int i=0;i<points.Length;i++){float t=i/32f,u=1-t;points[i]=u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d;}return points;}
 public static void Reset(MapData map){sites.Remove(map);}
 public static Site FromObject(MapData map,ProjectMapObject o){var stop=ProjectMapScene.ServiceStop(map,o);var enter=new Vector3[33];for(int i=0;i<33;i++)enter[i]=Vector3.Lerp(stop.V(),o.Position,i/32f);var leave=(Vector3[])enter.Clone();Array.Reverse(leave);var entryRoad=Array.Find(map.roads,r=>r.id=="restaurant:entrance");var exitRoad=Array.Find(map.roads,r=>r.id=="restaurant:exit");if(o.id=="service:restaurant"&&entryRoad!=null&&exitRoad!=null&&Vector3.Distance(entryRoad.points[entryRoad.points.Length-1].V(),o.Position)<.1f){enter=Array.ConvertAll(entryRoad.points,p=>p.V());leave=Array.ConvertAll(exitRoad.points,p=>p.V());}return new Site{approach=stop,parking=o.Position,direction=o.Direction,origin=o.Position,building=ProjectMapScene.Center(o.points),enter=enter,leave=leave,surfaces=new MapRoad[0]};}
 public static Site For(MapData map){if(ProjectMapScene.Active(map)){foreach(var o in ProjectMapScene.Services(map,"restaurant"))return FromObject(map,o);return null;}if(map==null||map.mapId!="eungye"||!map.HasHd)return null;Site site;if(sites.TryGetValue(map,out site))return site;
  var graph=new HdRoadGraph(map);int edge=Array.FindIndex(map.hdEdges,e=>e.id=="local:6575:3281:7166:2686"&&!e.disabled);if(edge<0)throw new InvalidOperationException("Restaurant south access lane is unavailable");var path=graph.paths[edge];const float at=45;if(path.length<at+20)throw new InvalidOperationException("Restaurant access lane too short");
  var origin=path.Position(at);var dir=path.Tangent(at);var right=TrafficSystem.Right(dir);Func<float,float,Vector3> q=(along,across)=>origin+dir*along+right*across;
  site=new Site{origin=origin,direction=dir,parking=q(0,12),building=q(-5,21)};
  site.enter=Curve(path.Position(at-14),q(-10,0),q(-10,12),site.parking);
  site.leave=Curve(site.parking,q(10,12),q(10,0),path.Position(at+14));
  var before=path.Position(at-18);var heading=path.Tangent(at-18);site.approach=new MapStop{name="은계 남부 식당 입구 (게임시설)",x=before.x,z=before.z,elevation=before.y,dx=heading.x,dz=heading.z,width=3,graphStop=true,laneEdge=edge,laneAt=at-18};
  site.surfaces=new[]{Surface("court",10,new[]{q(-7,12),q(7,12)}),Surface("entrance",6,site.enter),Surface("exit",6,site.leave)};
  FuelStationLayout.ValidatePlot(map,site.surfaces,"restaurant");
  FuelStationLayout.ValidatePlot(map,new[]{Surface("building",5,new[]{q(-10,21),q(0,21)})},"restaurant-building");
  sites.Add(map,site);return site;
 }
 public static void Apply(MapData map){if(ProjectMapScene.Active(map))return;var site=For(map);if(site==null||Array.Exists(map.roads,r=>r.id=="restaurant:court"))return;var roads=new List<MapRoad>(map.roads);roads.AddRange(site.surfaces);map.roads=roads.ToArray();}
 public static void Draw(MapData map,Transform parent){if(ProjectMapScene.Active(map))return;var site=For(map);if(site==null||parent.Find("은계 남부 식당"))return;var root=new GameObject("은계 남부 식당");root.transform.SetParent(parent,false);root.transform.position=site.origin;root.transform.rotation=Quaternion.LookRotation(site.direction);var t=root.transform;
  var wall=WorldBuilder.Mat("Restaurant cream",new Color(.86f,.75f,.54f));var roof=WorldBuilder.Mat("Restaurant tile",new Color(.38f,.13f,.08f));var dark=WorldBuilder.Mat("Restaurant glass",new Color(.07f,.21f,.25f));var white=WorldBuilder.Mat("Restaurant white",Color.white);var bay=WorldBuilder.Mat("Restaurant bay",new Color(.95f,.75f,.15f));
  WorldBuilder.Box("식당 건물",new Vector3(21,2,-5),new Vector3(5,4,10),wall,t);
  WorldBuilder.Box("식당 지붕",new Vector3(21,4.1f,-5),new Vector3(5.4f,.3f,10.4f),roof,t);
  WorldBuilder.Box("식당 출입문",new Vector3(18.45f,1.2f,-5),new Vector3(.08f,2.4f,1.25f),dark,t,false);
  foreach(float z in new[]{-8f,-2f})WorldBuilder.Box("식당 창문",new Vector3(18.44f,1.8f,z),new Vector3(.08f,1.25f,2.7f),dark,t,false);
  WorldBuilder.Box("식당 간판",new Vector3(18.4f,3.35f,-5),new Vector3(.12f,1,9),roof,t,false);
  Label(t,"식당 간판 글씨","은계 밥집",new Vector3(18.25f,3.35f,-5),.13f);
  foreach(float x in new[]{10.5f,13.5f})WorldBuilder.Box("식당 주차 칸",new Vector3(x,.07f,0),new Vector3(.12f,.025f,7),bay,t,false);
  foreach(float z in new[]{-3.5f,3.5f})WorldBuilder.Box("식당 주차 칸",new Vector3(12,.07f,z),new Vector3(3,.025f,.12f),bay,t,false);
  Label(t,"식당 주차 안내","P · 식사 주차\n빈차 정차 후 F7",new Vector3(15,2.2f,0),.07f);
  Label(t,"식당 입구","식당 입구 →\n여기부터 수동 진입",t.InverseTransformPoint(site.enter[7])+Vector3.up*2.4f,.055f);
  Label(t,"식당 출구","출구 · 도로 차량에 양보",t.InverseTransformPoint(site.leave[24])+Vector3.up*2.4f,.055f);
  foreach(var path in new[]{site.enter,site.leave})for(int i=5;i<path.Length-2;i+=9){var obj=new GameObject("식당 진출입 화살표");obj.transform.SetParent(t,false);obj.transform.position=path[i]+Vector3.up*.075f;obj.transform.rotation=Quaternion.LookRotation((path[i+2]-path[i]).normalized);WorldBuilder.Box("방향",Vector3.zero,new Vector3(.14f,.02f,1.4f),white,obj.transform,false);foreach(float sign in new[]{-1f,1f}){var head=WorldBuilder.Box("화살촉",new Vector3(sign*.22f,0,.42f),new Vector3(.12f,.02f,.65f),white,obj.transform,false);head.transform.localRotation=Quaternion.Euler(0,sign*45,0);}}
 }
 static void Label(Transform parent,string name,string text,Vector3 position,float size){var obj=new GameObject(name);obj.transform.SetParent(parent,false);obj.transform.localPosition=position;var label=obj.AddComponent<TextMesh>();label.text=text;label.fontSize=48;label.characterSize=size;label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.color=Color.white;KoreanText.Apply(label);obj.AddComponent<FaceCamera>();}
}
