using System;
using System.Collections.Generic;
class RoadGeometryTests {
 static int failures;
 static void Check(bool value,string description){if(!value){failures++;Console.WriteLine("FAIL "+description);}}
 public static int Main(){
  var lanes=new List<RoadGeometry.Lane>{new RoadGeometry.Lane(-7.5f,-1),new RoadGeometry.Lane(-2.5f,-1),new RoadGeometry.Lane(2.5f,1),new RoadGeometry.Lane(7.5f,1)};
  var lines=RoadGeometry.Boundaries(lanes);
  Check(lines.Count==3,"two lanes in each direction produce one yellow median and two white lane dividers");
  Check(lines.Exists(x=>x.yellow&&!x.dashed&&Math.Abs(x.offset)<.001f),"opposing directions alone produce the yellow median");
  Check(lines.Exists(x=>!x.yellow&&x.dashed&&Math.Abs(x.offset-5)<.001f)&&lines.Exists(x=>!x.yellow&&x.dashed&&Math.Abs(x.offset+5)<.001f),"white dividers sit between actual graph lane centres");
  lines=RoadGeometry.Boundaries(new List<RoadGeometry.Lane>{new RoadGeometry.Lane(2,1),new RoadGeometry.Lane(6,1)});
  Check(lines.Count==1&&!lines[0].yellow&&lines[0].dashed&&Math.Abs(lines[0].offset-4)<.001f,"one-way carriageway has white divider and no yellow median");
  Check(RoadGeometry.Boundaries(new List<RoadGeometry.Lane>{new RoadGeometry.Lane(0,1)}).Count==0,"single one-way lane has no line through its centre");
  var block=new[]{new RoadGeometry.Point(4,-.2f),new RoadGeometry.Point(5,-.2f),new RoadGeometry.Point(5,.2f),new RoadGeometry.Point(4,.2f)};
  Check(RoadGeometry.SweptLaneIntersects(new RoadGeometry.Point(0,0),new RoadGeometry.Point(10,0),block,.95f),"thin building crossing between path vertices blocks the driven corridor");
  Check(RoadGeometry.SweptLaneIntersects(new RoadGeometry.Point(0,1),new RoadGeometry.Point(10,1),block,.95f),"taxi body clearance detects grazing building footprint");
  Check(!RoadGeometry.SweptLaneIntersects(new RoadGeometry.Point(0,3),new RoadGeometry.Point(10,3),block,.95f),"legitimate nearby building outside swept lane is retained");
  RoadGeometry.Point crossing;
  Check(RoadGeometry.TryCrossing(new RoadGeometry.Point(0,0),new RoadGeometry.Point(10,0),new RoadGeometry.Point(5,-4),new RoadGeometry.Point(5,4),out crossing)&&Math.Abs(crossing.x-5)<.001f,"interior crossing is found without a shared source vertex");
  Console.WriteLine("ROAD_GEOMETRY_TESTS failures="+failures);return failures==0?0:1;
 }
}
