using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class RoadReadabilityTests {
 static void Check(bool yes,string message){if(!yes)throw new Exception("ROAD_READABILITY: "+message);}
 static MapPoint P(float x,float z){return new MapPoint{x=x,z=z};}
 static HdEdge Lane(string id,float x,int number,bool reverse=false){return new HdEdge{id=id,sourceId="road",status="provisional",number=number,points=reverse?new[]{P(x,100),P(x,0)}:new[]{P(x,0),P(x,100)},next=new int[0]};}
 public static void Run(){
  var road=new MapRoad{id="road",width=18,sourceLanes=4,points=new[]{P(0,0),P(0,100)}};
  var map=new MapData{hybrid=true,width=120,depth=120,roads=new[]{road},buildings=new MapBuilding[0],stops=new MapStop[0],hdEdges=new[]{Lane("local:forward",2.25f,1),Lane("local:forward2",6.75f,2),Lane("local:back",-2.25f,1,true),Lane("local:back2",-6.75f,2,true)}};
  var lanes=new RoadPaintGeometry(map).Lanes(road,new Vector3(0,0,50),Vector3.forward);var boundaries=RoadGeometry.Boundaries(lanes);
  Check(lanes.Count==4&&boundaries.Count(x=>x.yellow)==1&&boundaries.Count(x=>!x.yellow)==2,"compiled four-lane corridor has one yellow and two white boundaries");
  int yellow=0,white=0;RoadPaintGeometry.Paint(map,new List<TrafficJunction>(),(name,p,d,w,l,y)=>{if(name=="Centre line"){Check(y&&Mathf.Abs(p.x)<.01f,"median between opposing paths");yellow++;}if(name=="Lane divider"){Check(!y&&Mathf.Abs(Mathf.Abs(p.x)-4.5f)<.01f,"white divider between same-direction paths");white++;}});
  Check(yellow>50&&white>40,"continuous compiled paths produce visible paint independent of tiny source segments");
  map.hdEdges=new[]{Lane("local:forward",2.25f,1),Lane("local:forward2",6.75f,2)};road.oneWay=1;lanes=new RoadPaintGeometry(map).Lanes(road,new Vector3(0,0,50),Vector3.forward);boundaries=RoadGeometry.Boundaries(lanes);
  Check(boundaries.Count==1&&!boundaries[0].yellow,"one-way paint has no yellow median");
  road.id="fuel:test";int fuelPaint=0;RoadPaintGeometry.Paint(map,new List<TrafficJunction>(),(n,p,d,w,l,y)=>fuelPaint++);Check(fuelPaint==0,"forecourt never receives ordinary road paint");road.id="road";
  var blocked=new MapBuilding{id="fixture-blocked",height=3,points=new[]{P(2,49),P(3,49),P(3,51),P(2,51)}};var nearby=new MapBuilding{id="fixture-nearby",height=3,points=new[]{P(21,49),P(23,49),P(23,51),P(21,51)}};map.buildings=new[]{blocked,nearby};
  Check(SceneBuildingVisibility.Hidden(map,blocked.id)&&!SceneBuildingVisibility.Hidden(map,nearby.id),"only building swept by actual lane is provisionally withheld");
  var fixture=new GameObject("Building visibility fixture");try{var env=fixture.AddComponent<RoadEnvironment>();env.Build(map);var query=typeof(RoadEnvironment).GetMethod("InBuilding",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);Check(!(bool)query.Invoke(env,new object[]{new Vector3(2.5f,0,50)})&&(bool)query.Invoke(env,new object[]{new Vector3(22,0,50)}),"paving queries match visible building selection");}finally{UnityEngine.Object.DestroyImmediate(fixture);}
  var asset=Resources.Load<TextAsset>("eungye-hybrid-map");var actual=JsonUtility.FromJson<MapData>(asset.text);MapSupplements.Apply(actual,asset.bytes);
  var conflicts=SceneBuildingVisibility.Conflicts(actual);Check(conflicts.Any(x=>x.buildingId=="37612022:B0010000:158"),"actual curved-road 3m box footprint identified");
  foreach(var id in new[]{"37612022:B0010000:1","37612022:B0010000:158","37612023:B0010000:4537"})Check(SceneBuildingVisibility.Hidden(actual,id),"actual additional road obstruction withheld: "+id);
  int retained=actual.buildings.Count(x=>!SceneBuildingVisibility.Hidden(actual,x.id));Check(retained>3300,"legitimate nearby buildings retained");
  var actualLayout=new RoadPaintGeometry(actual);bool four=false;foreach(var r in actual.roads.Where(x=>!x.suppressPaint&&x.sourceLanes==4)){
   var path=new AssistedRoadNetwork.Path(r.points.Select(x=>x.V()).ToArray());for(float at=1;at<path.length;at+=3){var section=actualLayout.Lanes(r,path.Position(at),path.Tangent(at));var lines=RoadGeometry.Boundaries(section);if(section.Count==4&&lines.Count(x=>x.yellow)==1&&lines.Count(x=>!x.yellow)==2){four=true;Debug.Log("ROAD_READABILITY_FOUR_LANES road="+r.id+" width="+r.width+" graph=2+2 median=1 whiteDividers=2 position="+path.Position(at));break;}}if(four)break;
  }Check(four,"actual broad four-lane graph paints both white dividers");
  int fourCorridors=0,fourSections=0;
  Func<string,string> family=id=>id.Contains(":part:")?id.Substring(0,id.IndexOf(":part:")):id;
  var local=actual.hdEdges.Where(x=>!x.disabled&&x.status=="provisional"&&x.turn==0&&x.change==0&&!x.id.StartsWith("join:"));
  foreach(var group in local.GroupBy(x=>x.sourceId)){
   if(group.Max(x=>x.number)!=2||group.Select(x=>family(x.id)).Distinct().Count()!=4)continue;fourCorridors++;
   string reference=family(group.First(x=>x.number==1).id);
   foreach(var edge in group.Where(x=>family(x.id)==reference)){
    var path=new AssistedRoadNetwork.Path(edge.points.Select(x=>x.V()).ToArray());if(path.length<8)continue;
    foreach(float fraction in new[]{.25f,.5f,.75f}){float at=path.length*fraction;var section=actualLayout.Lanes(edge,path.Position(at),path.Tangent(at));var lines=RoadGeometry.Boundaries(section);fourSections++;
     Check(section.Count==4&&lines.Count(x=>x.yellow)==1&&lines.Count(x=>!x.yellow)==2,"every active 2+2 corridor section keeps both white dividers: "+edge.id+" at="+at+" observedLanes="+section.Count);
    }
   }
  }Check(fourCorridors>5&&fourSections>20,"actual map checks multiple complete 2+2 corridors");Debug.Log("ROAD_READABILITY_ALL_FOUR_LANES corridors="+fourCorridors+" sections="+fourSections+" each=4/1yellow/2white");
  foreach(var c in conflicts.Where(x=>x.buildingId=="37612022:B0010000:1"||x.buildingId=="37612022:B0010000:158"||x.buildingId=="37612023:B0010000:4537"))Debug.Log("ROAD_READABILITY_BOX id="+c.buildingId+" edge="+c.edgeId+" height="+c.height+" position="+c.position);
  Debug.Log("ROAD_READABILITY_TEST_OK conflicts="+conflicts.Count+" retainedBuildings="+retained+" syntheticMedianPieces="+yellow+" syntheticWhitePieces="+white);
 }
}
