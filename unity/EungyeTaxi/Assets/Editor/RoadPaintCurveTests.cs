using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Independent synthetic geometry: no project-map coordinates or image fixtures.
public static class RoadPaintCurveTests {
 sealed class Stroke {public string kind;public Vector3 p,d;public float length;public bool yellow;public Vector3 Start {get{return p-d*length*.5f;}}public Vector3 End {get{return p+d*length*.5f;}}}
 static int checks;
 static void Check(bool value,string why){checks++;if(!value)throw new Exception("ROAD_PAINT_CURVE: "+why);}
 static MapPoint P(float x,float z){return new MapPoint{x=x,z=z};}
 static MapPoint[] Arc(float radius,bool reverse=false){
  var points=Enumerable.Range(0,281).Select(i=>P(100+radius*Mathf.Cos(i*.005f),100+radius*Mathf.Sin(i*.005f))).ToArray();
  if(reverse)Array.Reverse(points);return points;
 }
 static HdEdge Lane(string id,int number,MapPoint[] points){return new HdEdge{id=id,number=number,status="provisional",sourceId="curve-fixture",points=points,next=new int[0]};}
 static MapData Map(params HdEdge[] lanes){return new MapData{mapId="synthetic-paint-curve",hybrid=true,width=200,depth=200,roads=new[]{new MapRoad{id="curve-fixture",width=30,lanes=3,points=lanes[0].points}},hdEdges=lanes,junctionRules=new HybridJunctionRule[0]};}
 static List<Stroke> Paint(MapData map){var strokes=new List<Stroke>();RoadPaintGeometry.Paint(map,new List<TrafficJunction>(),(kind,p,d,w,l,y)=>strokes.Add(new Stroke{kind=kind,p=p,d=d,length=l,yellow=y}));return strokes;}
 static float Angle(Vector3 p){return Mathf.Atan2(p.z-100,p.x-100);}
 static void CurvedCentreline(){
  // The centre line's radius is 33.5m, greater than the 30m reference
  // lane: reference-length stamps leave a visible gap on every step.
  var map=Map(Lane("curve-reference",1,Arc(30)),Lane("curve-opposite",1,Arc(37,true)),Lane("curve-outer",2,Arc(26.5f)));
  string before=JsonUtility.ToJson(map);var strokes=Paint(map);
  var yellow=strokes.Where(s=>s.yellow&&Angle(s.p)>.15f&&Angle(s.p)<1.25f).OrderBy(s=>Angle(s.p)).ToArray();
  Check(yellow.Length>=30,"curved opposing lanes emit a full interior centre line");
  float maximumGap=0;
  for(int i=0;i<yellow.Length;i++){
   var s=yellow[i];var radial=s.p-new Vector3(100,0,100);var tangent=new Vector3(-radial.z,0,radial.x).normalized;
   Check(Mathf.Abs(radial.magnitude-33.5f)<.035f,"yellow lies midway between opposing circular lanes");
   Check(Vector3.Dot(tangent,s.d)>.999f,"curved stroke follows the centre-line tangent");
   Check(s.length>1.08f&&s.length<1.17f,"outside curve stroke uses boundary arc length, not reference stamp length");
   if(i>0){float gap=Vector3.Distance(yellow[i-1].End,s.Start);maximumGap=Mathf.Max(maximumGap,gap);Check(gap<.035f,"successive yellow endpoints join within 35mm, gap="+gap);}
  }
  var white=strokes.Where(s=>s.kind=="Lane divider"&&Angle(s.p)>.15f&&Angle(s.p)<1.25f).OrderBy(s=>Angle(s.p)).ToArray();
  Check(white.Length>8&&white.Length<yellow.Length*.75f,"white same-direction divider retains its dash duty cycle");
  int breaks=0,joined=0;for(int i=1;i<white.Length;i++){float gap=Vector3.Distance(white[i-1].End,white[i].Start);if(gap>2)breaks++;if(gap<.035f)joined++;}
  Check(breaks>=3&&joined>=6,"white paint keeps repeated gaps and connected strokes inside each dash");
  Check(JsonUtility.ToJson(map)==before,"painting does not mutate source lanes");
  // Imported numbering need not contain lane 1; geometry must stay identical.
  for(int i=0;i<map.hdEdges.Length;i++)map.hdEdges[i].number=7+i;
  var noOne=Paint(map);Check(noOne.Count==strokes.Count,"missing lane number 1 preserves stroke count");
  for(int i=0;i<strokes.Count;i++)Check(strokes[i].kind==noOne[i].kind&&Vector3.Distance(strokes[i].p,noOne[i].p)<.0001f&&Vector3.Distance(strokes[i].d,noOne[i].d)<.0001f&&Mathf.Abs(strokes[i].length-noOne[i].length)<.0001f,"missing lane number 1 preserves boundary geometry");
  Debug.Log("ROAD_PAINT_CURVE maximumYellowEndpointGap="+maximumGap+" yellow="+yellow.Length+" white="+white.Length);
 }
 static void TaperDirection(){
  // Orthogonal projection of x=3.5+0.3z onto a northbound reference
  // yields boundary x=(3.5+0.3z)/(2*(1+0.3^2)). Its tangent is not north.
  var map=Map(Lane("taper-reference",1,new[]{P(0,0),P(0,60)}),Lane("taper-opposite",1,new[]{P(21.5f,60),P(3.5f,0)}));
  var yellow=Paint(map).Where(s=>s.yellow&&s.p.z>10&&s.p.z<50).OrderBy(s=>s.p.z).ToArray();
  var expected=new Vector3(.3f/(2*1.09f),0,1).normalized;
  Check(yellow.Length>=38,"taper has continuous centre paint");
  foreach(var s in yellow){Check(Vector3.Dot(s.d,expected)>.99999f,"stroke direction follows derived sloping boundary rather than reference lane");Check(Mathf.Abs(s.p.x-(3.5f+.3f*s.p.z)/(2*1.09f))<.002f,"taper boundary matches independent line-projection equation");}
  for(int i=1;i<yellow.Length;i++)Check(Vector3.Distance(yellow[i-1].End,yellow[i].Start)<.035f,"taper yellow endpoints stay joined");
 }
 public static void Run(){checks=0;CurvedCentreline();TaperDirection();Debug.Log("ROAD_PAINT_CURVE_TESTS_OK checks="+checks);}
}
