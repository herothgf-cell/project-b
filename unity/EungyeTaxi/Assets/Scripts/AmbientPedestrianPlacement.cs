using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class AmbientPedestrianRoute {
 public string id,hub,reason;
 public Vector3 a,b;
 public Vector3 via;
 public bool bent;
 public TrafficJunction junction;
 public int axis,sign;
 public bool crossing;
 public float Length {get{return bent?Vector3.Distance(a,via)+Vector3.Distance(via,b):Vector3.Distance(a,b);}}
 public Vector3 Point(float progress){progress=Mathf.Clamp01(progress);if(!bent)return Vector3.Lerp(a,b,progress);float first=Vector3.Distance(a,via)/Length;return progress<=first?Vector3.Lerp(a,via,progress/first):Vector3.Lerp(via,b,(progress-first)/(1-first));}
}

// Built once from the loaded project scene. Nothing here searches map polygons per frame.
public sealed class AmbientPedestrianPlacement {
 const float BodyMargin=.38f;
 readonly MapData map;
 readonly TrafficSystem traffic;
 readonly RoadEnvironment roads;
 readonly Dictionary<string,List<MapArea>> walks=new Dictionary<string,List<MapArea>>();
 readonly Dictionary<string,List<MapArea>> hazards=new Dictionary<string,List<MapArea>>();
 readonly Dictionary<string,List<MapBuilding>> buildings=new Dictionary<string,List<MapBuilding>>();
 readonly Dictionary<string,List<Lane>> lanes=new Dictionary<string,List<Lane>>();
 readonly Dictionary<string,Dictionary<Vector2Int,List<Lane>>> laneCells=new Dictionary<string,Dictionary<Vector2Int,List<Lane>>>();
 readonly Dictionary<string,List<ReviewedRoadFacilities.Line>> obstacles=new Dictionary<string,List<ReviewedRoadFacilities.Line>>();
 readonly Dictionary<string,Dictionary<Vector2Int,List<MapArea>>> walkCells=new Dictionary<string,Dictionary<Vector2Int,List<MapArea>>>();
 struct Lane {public Vector3 a,b;public float radius;}
 public readonly List<AmbientPedestrianRoute> routes=new List<AmbientPedestrianRoute>();
 public readonly List<string> report=new List<string>();
 public AmbientPedestrianPlacement(MapData data,TrafficSystem simulation,RoadEnvironment pavement){map=data;traffic=simulation;roads=pavement;Build();}
 static readonly string[] Hubs={"J5057_4449","J5076_2580","J4396_13458","J6199_11987"};
 static int Cell(float value){return Mathf.FloorToInt(value/8);}
 static bool Near(MapPoint[] points,Vector3 center,float radius){if(points==null||points.Length<3)return false;float minX=float.MaxValue,maxX=float.MinValue,minZ=float.MaxValue,maxZ=float.MinValue;foreach(var p in points){minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);minZ=Mathf.Min(minZ,p.z);maxZ=Mathf.Max(maxZ,p.z);}return minX<=center.x+radius&&maxX>=center.x-radius&&minZ<=center.z+radius&&maxZ>=center.z-radius;}
 static void Index(Dictionary<Vector2Int,List<MapArea>> cells,MapArea area){float minX=float.MaxValue,maxX=float.MinValue,minZ=float.MaxValue,maxZ=float.MinValue;foreach(var p in area.points){minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);minZ=Mathf.Min(minZ,p.z);maxZ=Mathf.Max(maxZ,p.z);}for(int x=Cell(minX);x<=Cell(maxX);x++)for(int z=Cell(minZ);z<=Cell(maxZ);z++){var key=new Vector2Int(x,z);List<MapArea> bucket;if(!cells.TryGetValue(key,out bucket)){bucket=new List<MapArea>();cells.Add(key,bucket);}bucket.Add(area);}}
 static void IndexLane(Dictionary<Vector2Int,List<Lane>> cells,Lane lane){float radius=lane.radius+3;for(int x=Cell(Mathf.Min(lane.a.x,lane.b.x)-radius);x<=Cell(Mathf.Max(lane.a.x,lane.b.x)+radius);x++)for(int z=Cell(Mathf.Min(lane.a.z,lane.b.z)-radius);z<=Cell(Mathf.Max(lane.a.z,lane.b.z)+radius);z++){var key=new Vector2Int(x,z);List<Lane> bucket;if(!cells.TryGetValue(key,out bucket)){bucket=new List<Lane>();cells.Add(key,bucket);}bucket.Add(lane);}}
 void Build(){
  var widths=new Dictionary<string,float>();if(map.editorDocument!=null)foreach(var lane in map.editorDocument.lanes??new RoadMapLane[0])if(lane!=null&&!string.IsNullOrEmpty(lane.id))widths[lane.id]=Mathf.Max(.5f,lane.width);
  var facilityAsset=Resources.Load<TextAsset>("eungye-road-facilities");var facilityLayout=facilityAsset?ReviewedRoadFacilities.Plan(map,JsonUtility.FromJson<ReviewedRoadFacilities.Catalog>(facilityAsset.text)):new ReviewedRoadFacilities.Layout();
  foreach(string hub in Hubs){var j=traffic.junctions.Find(candidate=>candidate.name==hub);if(j==null){report.Add(hub+" missing runtime junction");continue;}
   var localWalks=new List<MapArea>();var localHazards=new List<MapArea>();var localBuildings=new List<MapBuilding>();var localLanes=new List<Lane>();var cells=new Dictionary<Vector2Int,List<MapArea>>();var localLaneCells=new Dictionary<Vector2Int,List<Lane>>();
   foreach(var area in map.areas??new MapArea[0]){if(!Near(area.points,j.center,65))continue;if(area.kind=="sidewalk"){localWalks.Add(area);Index(cells,area);}else if(area.kind=="water")localHazards.Add(area);}
   foreach(var building in map.buildings??new MapBuilding[0])if(Near(building.points,j.center,65)&&!SceneBuildingVisibility.Hidden(map,building.id))localBuildings.Add(building);
   foreach(var lane in map.hdEdges??new HdEdge[0]){if(lane.disabled||lane.points==null)continue;float width;if(!widths.TryGetValue(lane.id??"",out width))width=3.5f;for(int n=1;n<lane.points.Length;n++){Vector3 a=lane.points[n-1].V(),b=lane.points[n].V();if((a-j.center).sqrMagnitude>70*70&&(b-j.center).sqrMagnitude>70*70)continue;var span=new Lane{a=a,b=b,radius=width*.5f+BodyMargin};localLanes.Add(span);IndexLane(localLaneCells,span);}}
   var localObstacles=new List<ReviewedRoadFacilities.Line>();foreach(var line in facilityLayout.barriers)if((line.a-j.center).sqrMagnitude<70*70||(line.b-j.center).sqrMagnitude<70*70)localObstacles.Add(line);foreach(var line in facilityLayout.cushions)if((line.a-j.center).sqrMagnitude<70*70||(line.b-j.center).sqrMagnitude<70*70)localObstacles.Add(line);
   walks[hub]=localWalks;hazards[hub]=localHazards;buildings[hub]=localBuildings;lanes[hub]=localLanes;laneCells[hub]=localLaneCells;obstacles[hub]=localObstacles;walkCells[hub]=cells;
   int sidewalk=0;for(int slot=0;slot<2;slot++){AmbientPedestrianRoute route;if(TrySidewalk(j,slot,out route)){routes.Add(route);sidewalk++;report.Add(route.id+" "+route.a.ToString("F2")+" -> "+route.b.ToString("F2"));}else report.Add(hub+" sidewalk slot "+slot+" unavailable: no continuous 20-40m full-body safe path");}
   AmbientPedestrianRoute crossing;if(TryCrossing(j,out crossing)){routes.Add(crossing);report.Add(crossing.id+" "+crossing.a.ToString("F2")+" -> "+crossing.b.ToString("F2"));}else report.Add(hub+" crossing slot unavailable: painted crossing lacks safe sidewalk endpoints or pedestrian phase");
   report.Add(hub+" cached sidewalks="+localWalks.Count+" hazards="+localHazards.Count+" buildings="+localBuildings.Count+" laneSegments="+localLanes.Count+" barriers="+localObstacles.Count+" placed="+sidewalk+"+"+(crossing==null?0:1));
  }
 }
 static bool Contains(MapPoint[] polygon,Vector3 p){return ProjectMapScene.Contains(polygon,p);}
 static float SegmentDistance(Vector3 p,Vector3 a,Vector3 b){var d=b-a;float length=d.x*d.x+d.z*d.z;if(length<.00001f)return Vector2.Distance(new Vector2(p.x,p.z),new Vector2(a.x,a.z));float t=Mathf.Clamp01(((p.x-a.x)*d.x+(p.z-a.z)*d.z)/length);return Vector2.Distance(new Vector2(p.x,p.z),new Vector2(a.x+d.x*t,a.z+d.z*t));}
 static float PolygonDistance(MapPoint[] ring,Vector3 p){if(Contains(ring,p))return 0;float best=float.MaxValue;for(int i=0;i<ring.Length;i++)best=Mathf.Min(best,SegmentDistance(p,ring[i].V(),ring[(i+1)%ring.Length].V()));return best;}
 bool FixedObstacleClear(string hub,Vector3 p){foreach(var area in hazards[hub])if(PolygonDistance(area.points,p)<BodyMargin)return false;foreach(var b in buildings[hub])if(PolygonDistance(b.points,p)<BodyMargin)return false;foreach(var line in obstacles[hub])if(p.y<=Mathf.Max(line.a.y,line.b.y)+line.height+.2f&&SegmentDistance(p,line.a,line.b)<line.width*.5f+BodyMargin)return false;return true;}
 static float Height(MapArea area,Vector3 point){
   var triangles=area.triangles;if(triangles!=null)for(int n=0;n+2<triangles.Length;n+=3){MapPoint a=area.points[triangles[n]],b=area.points[triangles[n+1]],c=area.points[triangles[n+2]];float den=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);if(Mathf.Abs(den)<.000001f)continue;float u=((b.z-c.z)*(point.x-c.x)+(c.x-b.x)*(point.z-c.z))/den,v=((c.z-a.z)*(point.x-c.x)+(a.x-c.x)*(point.z-c.z))/den,w=1-u-v;if(u>=-.001f&&v>=-.001f&&w>=-.001f)return area.height+u*a.elevation+v*b.elevation+w*c.elevation;}
  return area.height;
 }
 bool Support(string hub,Vector3 p,out float height){height=0;List<MapArea> local;if(!walkCells[hub].TryGetValue(new Vector2Int(Cell(p.x),Cell(p.z)),out local))return false;MapArea selected=null;foreach(var area in local)if(Contains(area.points,p)){selected=area;break;}if(selected==null)return false;height=Height(selected,p);
  // Adjacent sidewalk polygons may support different parts of the body footprint.
  foreach(float x in new[]{-BodyMargin,BodyMargin})foreach(float z in new[]{-BodyMargin,BodyMargin}){var corner=p+new Vector3(x,0,z);bool found=false;List<MapArea> bucket;if(!walkCells[hub].TryGetValue(new Vector2Int(Cell(corner.x),Cell(corner.z)),out bucket))return false;foreach(var area in bucket)if(Contains(area.points,corner)&&Mathf.Abs(Height(area,corner)-height)<.22f){found=true;break;}if(!found)return false;}
   if(!FixedObstacleClear(hub,new Vector3(p.x,height,p.z)))return false;
   p.y=height;List<Lane> nearby;if(laneCells[hub].TryGetValue(new Vector2Int(Cell(p.x),Cell(p.z)),out nearby))foreach(var lane in nearby){var d=lane.b-lane.a;float flat=d.x*d.x+d.z*d.z;if(flat<.001f)continue;float t=Mathf.Clamp01(((p.x-lane.a.x)*d.x+(p.z-lane.a.z)*d.z)/flat);var near=Vector3.Lerp(lane.a,lane.b,t);float dx=p.x-near.x,dz=p.z-near.z;if(Mathf.Abs(p.y-near.y)<1.5f&&dx*dx+dz*dz<lane.radius*lane.radius)return false;}
  return !roads.OnRoad(p,BodyMargin);
 }
 bool SafeWalk(string hub,Vector3 start,Vector3 end,out Vector3 a,out Vector3 b){a=b=Vector3.zero;float last=0;for(float t=0;t<=1.001f;t+=1f/32){var p=Vector3.Lerp(start,end,Mathf.Min(1,t));float h;if(!Support(hub,p,out h)||t>0&&Mathf.Abs(h-last)>.2f)return false;if(t<=.001f)a=new Vector3(p.x,h,p.z);if(t>=.999f)b=new Vector3(p.x,h,p.z);last=h;}return true;}
 bool CrossingClear(string hub,Vector3 a,Vector3 b){float length=Vector3.Distance(a,b);for(float at=0;at<=length;at+=.5f){var p=Vector3.Lerp(a,b,Mathf.Min(1,at/length));if(!FixedObstacleClear(hub,p))return false;if(at>2&&at<length-2&&!roads.OnRoad(p,0))return false;float nearest=float.MaxValue,vertical=0;List<Lane> nearby;if(laneCells[hub].TryGetValue(new Vector2Int(Cell(p.x),Cell(p.z)),out nearby))foreach(var lane in nearby){float d=SegmentDistance(p,lane.a,lane.b);if(d<nearest){nearest=d;var v=lane.b-lane.a;float t=Mathf.Clamp01(Vector3.Dot(new Vector3(p.x-lane.a.x,0,p.z-lane.a.z),new Vector3(v.x,0,v.z))/Mathf.Max(.001f,v.x*v.x+v.z*v.z));vertical=Mathf.Abs(p.y-Mathf.Lerp(lane.a.y,lane.b.y,t));}}if(nearest<3&&vertical>1)return false;}return true;}
 bool TrySidewalk(TrafficJunction j,int slot,out AmbientPedestrianRoute route){route=null;var dirs=new[]{traffic.AxisDirection(j,0),traffic.AxisDirection(j,1),-traffic.AxisDirection(j,0),-traffic.AxisDirection(j,1)};float best=float.MaxValue;Vector3 bestA=Vector3.zero,bestB=Vector3.zero;
  for(float radius=10;radius<=54;radius+=2)for(int angle=0;angle<32;angle++){var unit=new Vector3(Mathf.Cos(angle*Mathf.PI/16),0,Mathf.Sin(angle*Mathf.PI/16));var seed=j.center+unit*radius;float elevation;if(!Support(j.name,seed,out elevation))continue;foreach(var direction in dirs)foreach(float length in new[]{20.5f,24f,30f,36f}){Vector3 a,b;if(!SafeWalk(j.name,seed,seed+direction*length,out a,out b))continue;bool duplicate=false;foreach(var placed in routes)if(!placed.crossing&&placed.hub==j.name&&(Vector3.Distance(placed.a,a)<9||Vector3.Distance(placed.b,b)<9))duplicate=true;if(duplicate)continue;float score=radius+length*.01f+(slot==0?0:Vector3.Distance(a,j.center)*.01f);if(score<best){best=score;bestA=a;bestB=b;}}}
  if(best!=float.MaxValue){route=new AmbientPedestrianRoute{id=j.name+":sidewalk:"+slot,hub=j.name,a=bestA,b=bestB};return true;}
  // Follow a corner of adjacent narrow sidewalk polygons when no straight route fits.
  Vector3 bestVia=Vector3.zero;for(float radius=12;radius<=54;radius+=3)for(int angle=0;angle<24;angle++){var unit=new Vector3(Mathf.Cos(angle*Mathf.PI/12),0,Mathf.Sin(angle*Mathf.PI/12));var seed=j.center+unit*radius;float elevation;if(!Support(j.name,seed,out elevation))continue;foreach(var direction in dirs){Vector3 a,via;if(!SafeWalk(j.name,seed,seed+direction*11,out a,out via))continue;foreach(float turn in new[]{-45f,-30f,30f,45f}){var next=Quaternion.Euler(0,turn,0)*direction;Vector3 join,b;if(!SafeWalk(j.name,seed+direction*11,seed+direction*11+next*11,out join,out b)||Mathf.Abs(join.y-via.y)>.1f)continue;bool duplicate=false;foreach(var placed in routes)if(!placed.crossing&&placed.hub==j.name&&(Vector3.Distance(placed.a,a)<9||Vector3.Distance(placed.b,b)<9))duplicate=true;if(duplicate)continue;float score=radius+Mathf.Abs(turn)*.01f;if(score<best){best=score;bestA=a;bestVia=via;bestB=b;}}}}
  if(best==float.MaxValue)return false;route=new AmbientPedestrianRoute{id=j.name+":sidewalk:"+slot,hub=j.name,a=bestA,via=bestVia,b=bestB,bent=true};return true;
 }
 bool TryCrossing(TrafficJunction j,out AmbientPedestrianRoute route){route=null;if(!traffic.HasPedestrianPhase(j)||j.facility==null||!j.facility.crosswalk)return false;
   foreach(var approach in j.facility.approaches??new HybridApproach[0]){var incoming=new Vector3(approach.dx,0,approach.dz).normalized;if(incoming.sqrMagnitude<.5f)continue;int axis=Mathf.Abs(Vector3.Dot(incoming,traffic.AxisDirection(j,0)))>.7f?0:1,sign=traffic.Sign(incoming,axis,j);var across=TrafficSystem.Right(incoming);float width=approach.width*.5f;var center=j.center-incoming*(j.halfWidth[1-axis]+TrafficJunction.CrosswalkCentreOffset);var f=map.editorDocument==null?null:Array.Find(map.editorDocument.facilities,x=>x.kind=="crosswalk"&&x.junctionId==j.name&&x.axis==axis&&x.sign==sign);if(f!=null){center=f.Position;var explicitAcross=TrafficSystem.Right(Quaternion.Euler(0,f.yaw,0)*Vector3.forward);if(Vector3.Dot(explicitAcross,across)<0)explicitAcross=-explicitAcross;across=explicitAcross;width=f.width*.5f;}
   // The center and width are exactly those used by TrafficSystem's rendered stripes.
     for(float along=-1f;along<=1.01f;along+=.25f){var lefts=new List<Vector3>();var rights=new List<Vector3>();for(float outside=.5f;outside<=7.51f;outside+=.25f){Vector3 left=center-across*(width+outside)+incoming*along,right=center+across*(width+outside)+incoming*along;float height;if(Support(j.name,left,out height))lefts.Add(new Vector3(left.x,height,left.z));if(Support(j.name,right,out height))rights.Add(new Vector3(right.x,height,right.z));}
     foreach(var left in lefts)foreach(var right in rights){if(Mathf.Abs(left.y-right.y)>.3f||!CrossingClear(j.name,left,right))continue;route=new AmbientPedestrianRoute{id=j.name+":crossing",hub=j.name,a=left,b=right,junction=j,axis=axis,sign=sign,crossing=true};return true;}
    }
  }return false;
 }
 public bool IsSafe(AmbientPedestrianRoute route,float progress){if(progress<0||progress>1)return false;if(route.crossing){var p=route.Point(progress);return FixedObstacleClear(route.hub,p)&&(progress<=.01f||progress>=.99f||roads.OnRoad(p,0));}float h;return Support(route.hub,route.Point(progress),out h);}
}
