using System.Collections;
using System.Text;
using UnityEngine;

// A narrow loop has a 100 m direct link and a 300 m detour. A long blockage
// must change the time-optimal route without changing a fare's distance quote.
public class NavigationChoiceSmokeTest : MonoBehaviour {
 public DriveSession session;
 bool ok=true; readonly StringBuilder report=new StringBuilder();
 void Check(bool value,string label){ok&=value;report.AppendLine((value?"PASS ":"FAIL ")+label);}
 static MapRoad Road(float ax,float az,float bx,float bz,int oneWay=0){return new MapRoad{width=7,sourceLanes=2,oneWay=oneWay,points=new[]{new MapPoint{x=ax,z=az},new MapPoint{x=bx,z=bz}}};}
 static bool VisitsNorth(RoadNavigation nav){foreach(var p in nav.path)if(p.z>80)return true;return false;}
 IEnumerator Start(){
  yield return null;
  var host=new GameObject("Navigation choice fixture");
  var car=new GameObject("Navigation fixture heading");car.transform.SetPositionAndRotation(new Vector3(1.75f,0,-40),Quaternion.identity);
  var map=new MapData{roads=new[]{Road(0,-100,0,0),Road(0,0,100,0),Road(0,0,0,100),Road(0,100,100,100),Road(100,100,100,0),Road(100,0,200,0)}};
  var nav=host.AddComponent<RoadNavigation>();nav.Build(map,car.transform);
  var conditions=host.AddComponent<RoadConditions>();conditions.Initialize(session);nav.conditions=conditions;
  float quote=nav.Distance(car.transform.position,new Vector3(200,0,0));
  nav.SetDestination(new Vector3(200,0,0));
  Check(!VisitsNorth(nav),"clear-road route takes direct link");
  var incident=conditions.AddIncident(new Vector3(50,0,0),Vector3.right,7,180);
  nav.Refresh();
  Check(VisitsNorth(nav),"fast route reroutes around directed blockage");
  Check(nav.estimatedSeconds<nav.alternativeSeconds&&nav.remaining>nav.alternativeDistance,"longer fast route offers shorter-distance alternative with slower ETA");
  Check(Mathf.Abs(nav.Distance(car.transform.position,new Vector3(200,0,0))-quote)<.01f,"traffic does not change distance-based fare quote");
  Check(nav.path.Count>1&&Vector3.Dot((nav.path[1]-nav.path[0]).normalized,Vector3.forward)>.8f,"reroute preserves current heading");
  int recalc=nav.recalculationCount;for(int i=0;i<5;i++)nav.Refresh();
  Check(nav.recalculationCount==recalc,"unchanged telemetry does not repeatedly announce reroutes");
  var stop=new MapStop{x=200,z=0,dx=1,dz=0,width=7};nav.SetDestination(stop);
  Check(VisitsNorth(nav)&&nav.path.Count>2&&Vector3.Dot((nav.path[nav.path.Count-1]-nav.path[nav.path.Count-2]).normalized,stop.Direction)>.99f&&Vector3.Distance(nav.path[nav.path.Count-1],stop.Kerb)<.01f,"traffic detour retains legal destination curb approach");
  nav.TogglePreference();
  Check(!VisitsNorth(nav)&&nav.estimatedSeconds>nav.alternativeSeconds,"short-distance preference deliberately retains slow direct link");
  incident.expiresAt=Time.time-1;nav.TogglePreference();nav.Refresh();
  Check(!VisitsNorth(nav),"cleared incident restores direct fast route");
  nav.Clear();Check(nav.path.Count==0&&nav.alternativePath.Count==0,"clear removes both route choices");
  // There is no legal exit after traversing this one-way segment. The router
  // must not invent the dead-end U-turn available on a two-way road.
  nav.Build(new MapData{roads=new[]{Road(0,0,0,100,1)}},car.transform);
  car.transform.SetPositionAndRotation(new Vector3(1.75f,0,40),Quaternion.identity);
  nav.SetDestination(Vector3.zero);
  Check(nav.path.Count==0,"one-way dead end never returns against traffic");
  nav.Build(new MapData{roads=new[]{Road(0,0,0,100,-1)}},car.transform);
  car.transform.SetPositionAndRotation(new Vector3(-1.75f,0,40),Quaternion.Euler(0,180,0));nav.SetDestination(Vector3.zero);
  Check(nav.path.Count>1&&nav.remaining<50,"reverse one-way accepts its legal direction");
  var network=new AssistedRoadNetwork();network.Build(new MapData{roads=new[]{Road(0,0,0,100,1),Road(0,100,0,200,1)}});
  bool northOnly=network.lanes.Count==1;foreach(var lane in network.lanes)northOnly&=Vector3.Dot(lane.path.Tangent(0),Vector3.forward)>.99f;
  Check(northOnly,"assisted network merges consistent one-way segments without opposing lanes");
  network.Build(new MapData{roads=new[]{Road(0,0,0,100,1),Road(0,100,0,200,-1)}});
  Check(network.lanes.Count==2,"assisted network does not merge opposing directed segments");
  network.Build(new MapData{roads=new[]{Road(0,0,0,100,1),Road(0,100,0,0,1)}});
  bool north=false,south=false;foreach(var lane in network.lanes){north|=lane.path.Tangent(0).z>.9f;south|=lane.path.Tangent(0).z<-.9f;}
  Check(north&&south,"opposite directed road records are not deduplicated away");
  var approach=Road(0,-150,0,0);approach.width=21;approach.sourceLanes=6;
  var west=Road(0,0,-150,0);west.width=21;west.sourceLanes=6;
  var east=Road(0,0,150,0);east.width=21;east.sourceLanes=6;
  nav.conditions=null;nav.Build(new MapData{roads=new[]{approach,west,east}},car.transform);
  car.transform.SetPositionAndRotation(new Vector3(8.75f,0,-120),Quaternion.identity);
  nav.SetDestination(new Vector3(-150,0,0));
  Check(nav.path.Count>1&&Mathf.Abs(nav.path[0].x-1.75f)<.1f,"left turn guide starts in inner lane instead of third lane");
  Check(nav.instruction.Contains("좌회전")&&nav.instruction.Contains("1차로"),"left turn tells driver to prepare first lane");
  nav.SetDestination(new Vector3(150,0,0));
  Check(nav.path.Count>1&&Mathf.Abs(nav.path[0].x-8.75f)<.1f,"right turn guide stays in outer lane");
  Check(nav.instruction.Contains("우회전")&&nav.instruction.Contains("3차로"),"right turn tells driver to prepare third lane");
  Destroy(host);Destroy(car);
  string output=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../..","artifacts","navigation-choice-smoke.txt"));
  System.IO.File.WriteAllText(output,"success="+ok+"\n"+report);Debug.Log(report.ToString());Application.Quit(ok?0:1);
 }
}
