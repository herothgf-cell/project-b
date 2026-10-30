using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Fictional competition district, authored directly in the original simulation format.
// Roads, navigation, traffic, service facilities and rendering share this geometry.
public static class PvpCityMap {
 static readonly float[] X={200,733,1267,1800,2333,2867,3400};
 static readonly float[] Z={200,600,1000,1400,1800,2200,2600};
 static readonly string[] Districts={"서남 공원","남부 시장","동남 터미널","서부 주거","중앙역 상업","동부 업무","서북 주거","북부 병원","동북 대학"};
 static MapPoint P(Vector3 p){return new MapPoint{x=p.x,z=p.z};}
 static string Key(int x,int z){return x+":"+z;}
 public static MapData Create(){
  var streets=new PvpCityRoadBuilder();var roads=streets.roads;
  var map=new MapData{mapId="pvp-city-a",source="확장 A2 · 가상 경쟁 도시",coordinateStatus="fictional metres",width=3600,depth=2800,hybrid=true,supplementApplied=true,roads=roads.ToArray(),hdEdges=streets.edges.ToArray(),junctionRules=streets.rules.ToArray(),hdMarks=streets.marks.ToArray(),hdSignals=new MapPoint[0],hdTrials=new HdTrialCase[0],osmCrossings=new OsmCrossing[0],buildings=new MapBuilding[0],areas=new MapArea[0],stops=new MapStop[0],targets=new MapTarget[0],landmarks=new MapLandmark[0]};
  var graph=new HdRoadGraph(map);var stops=new List<MapStop>();var areas=new List<MapArea>();var objects=new List<ProjectMapObject>();var landmarks=new List<MapLandmark>();
  for(int dz=0;dz<3;dz++)for(int dx=0;dx<3;dx++){
   int cx=dx*2+1,cz=dz*2+1,index=dz*3+dx;string name=Districts[index];
   landmarks.Add(new MapLandmark{name=name,kind="district",status="fictional",x=X[cx]+85,z=Z[cz]+85});
   for(int side=0;side<2;side++){
    string lane="city:"+Key(cx-1+side,cz)+">"+Key(cx+side,cz);int edge=Array.FindIndex(map.hdEdges,e=>e.id==lane);float at=graph.paths[edge].length*.5f;
    var stop=AddPassengerBay(map,edge,at,name+(side==0?" 서문":" 동문"),roads);graph=new HdRoadGraph(map);stops.Add(stop);
    var center=stop.Passenger;areas.Add(new MapArea{kind="sidewalk",name=stop.name+" 승객 보도",height=.14f,points=ProjectMapScene.Rectangle(center,stop.Direction,3,24),triangles=new[]{0,1,2,0,2,3}});
   }
  }
  map.stops=stops.ToArray();map.landmarks=landmarks.ToArray();map.areas=areas.ToArray();
  // Four independent start areas with road-bound approaches and off-lane parking.
  foreach(int index in new[]{0,4,12,16}){
   var s=stops[index];var bay=s.Kerb+TrafficSystem.Right(s.Direction)*12-s.Direction*45;
   var o=new ProjectMapObject{id="city:start:"+index,kind="parking",name="출발 주차장 "+(objects.Count+1),laneId=map.hdEdges[s.laneEdge].id,laneAt=0,serviceX=bay.x,serviceZ=bay.z,yaw=Quaternion.LookRotation(s.Direction).eulerAngles.y,points=ProjectMapScene.Rectangle(bay,s.Direction,12,28)};objects.Add(o);
   roads.Add(Surface(o.id,12,new[]{bay-s.Direction*14,bay+s.Direction*14}));roads.Add(Surface(o.id+":access",7,new[]{s.V()-TrafficSystem.Right(s.Direction)*4.5f-s.Direction*70,bay-s.Direction*12,bay+s.Direction*12,s.V()-TrafficSystem.Right(s.Direction)*4.5f-s.Direction*20}));
  }
  map.projectScene=new ProjectMapScene{objects=objects.ToArray()};
  var facilities=new List<FuelStationLayout.FuelStation>();
  foreach(int index in new[]{12,8,10})facilities.Add(Facility(map,graph,stops[index],facilities.Count,roads));
  FuelStationLayout.RegisterAuthored(map,facilities);map.roads=roads.ToArray();
  var buildings=new List<MapBuilding>();
  // Block interiors remain well clear of street, passenger and facility frontage.
  for(int z=0;z<6;z++)for(int x=0;x<6;x++)for(int bz=0;bz<3;bz++)for(int bx=0;bx<3;bx++){
   var center=new Vector3(Mathf.Lerp(X[x],X[x+1],.22f+bx*.28f),0,Mathf.Lerp(Z[z],Z[z+1],.22f+bz*.28f));
   bool park=x<2&&z<2;if(park&&bx!=2)continue;
   buildings.Add(new MapBuilding{id="city-building:"+x+":"+z+":"+bx+":"+bz,name=Districts[(z/2)*3+x/2],height=park?5:8+((x*7+z*11+bx*3+bz)%7)*5,points=ProjectMapScene.Rectangle(center,Vector3.forward,50,45),triangles=new[]{0,1,2,0,2,3}});
  }
  var roadClearance=new Clearance(roads);buildings.RemoveAll(b=>!roadClearance.Clear(b.points,3));
  AddUrbanFrontage(map,roads,buildings,areas);
  areas.Add(new MapArea{kind="land",name="서남 시민공원",points=ProjectMapScene.Rectangle(new Vector3(470,0,400),Vector3.forward,350,260),triangles=new[]{0,1,2,0,2,3}});
  map.areas=areas.ToArray();map.buildings=buildings.ToArray();
  // Preserve DriveSession's normal 23m-back / 2.5m-right placement while
  // deriving the target frame from this map's actual curved native lane.
  var launch=graph.paths[Array.FindIndex(map.hdEdges,e=>e.id=="city:0:1>1:1")];var launchPosition=launch.Position(65);var launchDirection=launch.Tangent(65);
  var target=launchPosition+launchDirection*23-TrafficSystem.Right(launchDirection)*2.5f;var ahead=target+launchDirection*200;
  map.targets=new[]{new MapTarget{name="출발 도로",x=target.x,z=target.z},new MapTarget{name="진행 방향",x=ahead.x,z=ahead.z}};
  streets.AttachDocument(map);
  return map;
 }
 // Keep geometry near street level; all cutouts use the same asphalt envelopes
 // as the physical road and facility surfaces, so sidewalks never seal a driveway.
 sealed class Clearance {
  sealed class Span {public Vector3 a,b;public float radius;}
  readonly List<Span> spans=new List<Span>();
  readonly Dictionary<Vector2Int,List<int>> cells=new Dictionary<Vector2Int,List<int>>();
  public Clearance(IEnumerable<MapRoad> roads){foreach(var road in roads)for(int i=1;i<road.points.Length;i++){
   var span=new Span{a=road.points[i-1].V(),b=road.points[i].V(),radius=road.width*.5f};int index=spans.Count;spans.Add(span);float pad=span.radius+4;
   for(int x=Mathf.FloorToInt((Mathf.Min(span.a.x,span.b.x)-pad)/64);x<=Mathf.FloorToInt((Mathf.Max(span.a.x,span.b.x)+pad)/64);x++)for(int z=Mathf.FloorToInt((Mathf.Min(span.a.z,span.b.z)-pad)/64);z<=Mathf.FloorToInt((Mathf.Max(span.a.z,span.b.z)+pad)/64);z++){var key=new Vector2Int(x,z);List<int> list;if(!cells.TryGetValue(key,out list)){list=new List<int>();cells.Add(key,list);}list.Add(index);}
  }}
  public bool Clear(MapPoint[] ring,float margin){var polygon=ring.Select(p=>new RoadGeometry.Point(p.x,p.z)).ToArray();var used=new HashSet<int>();
   for(int x=Mathf.FloorToInt(ring.Min(p=>p.x)/64);x<=Mathf.FloorToInt(ring.Max(p=>p.x)/64);x++)for(int z=Mathf.FloorToInt(ring.Min(p=>p.z)/64);z<=Mathf.FloorToInt(ring.Max(p=>p.z)/64);z++){
    List<int> list;if(!cells.TryGetValue(new Vector2Int(x,z),out list))continue;foreach(int id in list){if(!used.Add(id))continue;var span=spans[id];if(RoadGeometry.SweptLaneIntersects(new RoadGeometry.Point(span.a.x,span.a.z),new RoadGeometry.Point(span.b.x,span.b.z),polygon,span.radius+margin))return false;}
   }return true;
  }
 }
 static void AddUrbanFrontage(MapData map,List<MapRoad> roads,List<MapBuilding> buildings,List<MapArea> areas){
  var clearance=new Clearance(roads);int serial=0;
  for(int z=0;z<6;z++)for(int x=0;x<6;x++){
   int district=(z/2)*3+x/2;string name=Districts[district];bool park=district==0;
   Action<Vector3,float,float> add=(center,width,depth)=>{
    var ring=ProjectMapScene.Rectangle(center,Vector3.forward,width,depth);if(!clearance.Clear(ring,3))return;
    int n=serial++;float height=park?5+n%3*2:district==5?22+n%5*7:district==7?16+n%4*5:9+n%6*4;
    buildings.Add(new MapBuilding{id="city-frontage:"+n,name=name+(park?" 공원 상가":" 거리 건물"),height=height,points=ring,triangles=new[]{0,1,2,0,2,3}});
   };
   for(float px=X[x]+64;px<X[x+1]-45;px+=56){add(new Vector3(px,0,Z[z]+32),36,26);add(new Vector3(px,0,Z[z+1]-32),36,26);}
   for(float pz=Z[z]+90;pz<Z[z+1]-65;pz+=54){add(new Vector3(X[x]+32,0,pz),26,34);add(new Vector3(X[x+1]-32,0,pz),26,34);}
  }
  int sidewalk=0;
  foreach(var road in roads.Where(r=>r.id.StartsWith("city-road:"))){var path=new AssistedRoadNetwork.Path(road.points.Select(p=>p.V()).ToArray());
   foreach(int side in new[]{-1,1})for(float at=22;at<path.length-22;at+=12){float end=Mathf.Min(at+12,path.length-22);float middle=(at+end)*.5f;var d=path.Tangent(middle);var center=path.Position(middle)+TrafficSystem.Right(d)*(side*(road.width*.5f+2));var ring=ProjectMapScene.Rectangle(center,d,3,end-at);
    if(!clearance.Clear(ring,.4f))continue;areas.Add(new MapArea{kind="sidewalk",name="거리 보도 "+sidewalk++,height=.14f,points=ring,triangles=new[]{0,1,2,0,2,3}});
   }
  }
 }
 static MapStop AddPassengerBay(MapData map,int edge,float at,string name,List<MapRoad> roads){
  var graph=new HdRoadGraph(map);var path=graph.paths[edge];var source=map.hdEdges[edge];var dir=path.Tangent(at);var right=TrafficSystem.Right(dir);var entry=path.Position(at-32);var exit=path.Position(at+32);var center=path.Position(at)+right*4.5f;
  string prefix=source.id+":bay";var all=new List<HdEdge>(map.hdEdges);int through=all.Count,bay=through+1,post=through+2;
  string originalEnd=source.toNode;int[] onward=source.next;var originalStart=source.points[0];var originalLast=source.points.Last();
  source.points=PvpCityRoadBuilder.Slice(path,0,at-32);source.toNode=prefix+":split";source.next=new[]{through,bay};
  all.Add(new HdEdge{id=prefix+":through",sourceId=source.sourceId,roadName=source.roadName,fromNode=source.toNode,toNode=prefix+":merge",number=source.number,status="authored-fictional",points=PvpCityRoadBuilder.Slice(path,at-32,at+32),next=new[]{post}});
  var enter=Curve(entry,entry+dir*16,center-dir*20,center-dir*7,24);var leave=Curve(center+dir*7,center+dir*20,exit-dir*16,exit,24);var pocket=new List<MapPoint>(enter);pocket.Add(P(center));pocket.AddRange(leave);
  all.Add(new HdEdge{id=prefix+":pocket",sourceId=source.sourceId,roadName=name+" 승하차 진입",fromNode=source.toNode,toNode=prefix+":merge",number=source.number,status="authored-fictional",points=pocket.ToArray(),next=new[]{post}});
  all.Add(new HdEdge{id=prefix+":post",sourceId=source.sourceId,roadName=source.roadName,fromNode=prefix+":merge",toNode=originalEnd,ruleId=source.ruleId,number=source.number,status="authored-fictional",points=PvpCityRoadBuilder.Slice(path,at+32,path.length),next=onward});
  map.hdEdges=all.ToArray();roads.Add(Surface(prefix+":surface",3.5f,pocket.Select(p=>p.V()).ToArray()));
  var bayPath=new AssistedRoadNetwork.Path(pocket.Select(p=>p.V()).ToArray());float error;float stopAt=bayPath.Project(center,out error);return Stop(map,new HdRoadGraph(map),bay,stopAt,name);
 }
 static MapStop Stop(MapData map,HdRoadGraph graph,int edge,float at,string name){var p=graph.paths[edge].Position(at);var d=graph.paths[edge].Tangent(at);return new MapStop{name=name,x=p.x,z=p.z,dx=d.x,dz=d.z,width=3,graphStop=true,laneEdge=edge,laneAt=at,hasPassengerAnchor=true,passengerAnchor=p+TrafficSystem.Right(d)*3.75f+Vector3.up*.16f};}
 static MapPoint[] Curve(Vector3 a,Vector3 b,Vector3 c,Vector3 d,int count){var points=new MapPoint[count+1];for(int i=0;i<=count;i++){float t=(float)i/count,u=1-t;points[i]=P(u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d);}return points;}
 static MapRoad Surface(string id,float width,Vector3[] points){return new MapRoad{id=id,name="시설 진입·주차",width=width,lanes=1,sourceLanes=1,oneWay=1,suppressPaint=true,points=Array.ConvertAll(points,P)};}
 static FuelStationLayout.FuelStation Facility(MapData map,HdRoadGraph graph,MapStop reference,int number,List<MapRoad> roads){
  int edge=map.hdEdges[reference.laneEdge].next[0];var approachPath=graph.paths[edge];float at=approachPath.length*.5f;if(at<45||at+40>approachPath.length)throw new InvalidOperationException("City service access unavailable");var center=approachPath.Position(at);var d=approachPath.Tangent(at);var right=TrafficSystem.Right(d);var pump=center+right*24;
  var entry=approachPath.Position(at-40);var exit=approachPath.Position(at+40);var enter=Curve(entry,entry+approachPath.Tangent(at-40)*20,pump-d*22,pump-d*6,24);var leave=Curve(pump+d*6,pump+d*22,exit-approachPath.Tangent(at+40)*20,exit,24);
  string id="city-fuel:"+number;string name=new[]{"북서 주유·정비·세차","중앙 주유·정비·세차","동부 주유"}[number];
  var paving=new[]{Surface(id+":court",24,new[]{pump-d*22,pump+d*22}),Surface(id+":entrance",7,Array.ConvertAll(enter,p=>p.V())),Surface(id+":exit",7,Array.ConvertAll(leave,p=>p.V()))};roads.AddRange(paving);
  var path=new List<Vector3>(Array.ConvertAll(enter,p=>p.V()));path.Add(pump);path.AddRange(Array.ConvertAll(leave,p=>p.V()));
  return new FuelStationLayout.FuelStation(name,id,Stop(map,graph,edge,at-45,name+" 입구"),enter[0].V(),leave.Last().V(),pump,d,path.ToArray(),Array.ConvertAll(ProjectMapScene.Rectangle(pump,d,24,46),p=>p.V()),paving,number<2,number<2);
 }
}




