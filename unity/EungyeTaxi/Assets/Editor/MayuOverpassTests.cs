using System;
using System.Linq;
using UnityEngine;

public static class MayuOverpassTests {
    public static void VerifyRelease() {
        Run();
        LatestFeedbackTests.FacilityDispatch();
        SuinroRepaintReview.VerifyAndBuild();
    }
    static void Check(bool pass,string message) {
        if (!pass) throw new InvalidOperationException("J10580 "+message);
        Debug.Log("J10580_PASS "+message);
    }
    static HdEdge StackedLane(float x,float height) {
        return new HdEdge{next=Array.Empty<int>(),points=new[]{
            new MapPoint{x=x,elevation=height},new MapPoint{x=x,z=20,elevation=height}}};
    }
    public static void Run() {
        var map=ProjectMapStore.Load(ProjectMapBuild.Root).Compose();
        var road=map.roads.Single(r=>r.id==MayuOverpass.RoadId);
        var root=new GameObject("J10580 regression");
        try {
            var graph=new HdRoadGraph(map);
            int upper=Array.FindIndex(map.hdEdges,e=>e.id=="join:local:10300:20:10559:1292:0");
            var point=graph.paths[upper].Position(graph.paths[upper].length*.5f);
            Check(point.y>6 && graph.Nearest(point+Vector3.up*.6f,graph.paths[upper].Tangent(10),out _)==upper,
                  "navigation follows elevated through lane");
            int lower=Array.FindIndex(map.hdEdges,e=>e.id=="A2218G611743:0");
            var under=graph.paths[lower].Position(graph.paths[lower].length*.5f);
            Check(under.y==0 && graph.Nearest(under,graph.paths[lower].Tangent(20),out _)==lower,
                  "navigation follows ground Suin-ro through lane");
            // The identical XZ centreline on the other level must not count as
            // either the best lane or its runner-up in the wrong-way detector.
            var stacked=new HdRoadGraph(new MapData{hdEdges=new[]{StackedLane(0,0),StackedLane(0,6.2f),
                StackedLane(4,0),StackedLane(4,6.2f)}});
            Check(stacked.NearestPair(new Vector3(0,6.8f,10),out _,out _,out var upperRunner)==1&&upperRunner>3,
                  "wrong-way nearest pair excludes both ground candidates from deck");
            Check(stacked.NearestPair(new Vector3(0,.6f,10),out _,out _,out var lowerRunner)==0&&lowerRunner>3,
                  "wrong-way nearest pair excludes both deck candidates from ground");
            var frontage=new RoadSurfaceIndex(new MapData{roads=map.roads.Where(r=>r.id.StartsWith("mayu:frontage:")).ToArray(),buildings=Array.Empty<MapBuilding>()});
            foreach(string id in new[]{"A2218G611504:0","A2218G611730:0","A222BC004212:0","A2218G611524:0","A2218G611505:0","A2218G611512:0"}) {
                var path=graph.paths[Array.FindIndex(map.hdEdges,e=>e.id==id)];
                Check(frontage.OnRoad(path.Position(path.length)+5*path.Tangent(path.length),-.9f),
                      "ground carriageway continues a vehicle width beyond capture "+id);
            }
            var retired=map.hdEdges.Select((e,i)=>new{e,i}).Where(x=>x.e.ruleId=="J10580_1415" && x.e.id.StartsWith("join:") && x.e.turn!=0).ToArray();
            Check(retired.Length==2 && retired.All(x=>x.e.disabled&&map.hdEdges.All(e=>!e.next.Contains(x.i))),
                  "both artificial deck-to-ramp turns are unreachable");
            Check(road.points.First().elevation==0&&road.points.Last().elevation==0,"both approach ends meet ground");
            var world=root.AddComponent<WorldBuilder>();
            MayuOverpass.Draw(world,road,true,WorldBuilder.Mat("test road",Color.gray));
            Physics.SyncTransforms();
            Check(Physics.Raycast(point+Vector3.up*2,Vector3.down,out var hit,3)&&hit.collider.transform.IsChildOf(root.transform)&&Mathf.Abs(hit.point.y-point.y)<.1f,
                  "upper road has continuous physical support at crossing");
            Check(!Physics.Raycast(under+Vector3.up*1.5f,Vector3.up,3),"lower through lane has vehicle clearance");

            var traffic=root.AddComponent<TrafficSystem>();
            traffic.junctions.Add(new TrafficJunction{name="J10580_1415",center=new Vector3(1058,0,141.5f),direction=Vector3.forward,signalized=true,halfWidth=new[]{16f,16f}});
            var district=root.AddComponent<DistrictTraffic>();
            district.Initialize(traffic,map,false);
            int start=Array.FindIndex(map.hdEdges,e=>e.id=="local:10300:20:10559:1292:part:2");
            int goal=Array.FindIndex(map.hdEdges,e=>e.id=="local:10645:1523:14427:13317:part:2");
            var route=district.CreateRoute(start,0,goal,20);
            Check(route!=null&&route.graphPath.edges.Contains(upper)&&route.stops.All(s=>s.junction.name!="J10580_1415"),
                  "district NPC crosses above the ground signal");
            var groundRoute=district.CreateRoute(lower,0,lower,graph.paths[lower].length);
            Check(groundRoute!=null&&groundRoute.stops.Any(s=>s.junction.name=="J10580_1415"),
                  "district NPC still observes the ground signal");
        } finally { UnityEngine.Object.DestroyImmediate(root); }
        Debug.Log("J10580_OVERPASS_PASS");
    }
}
