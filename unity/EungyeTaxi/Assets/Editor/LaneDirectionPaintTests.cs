using System;
using System.Linq;
using UnityEngine;

public static class LaneDirectionPaintTests {
 static void Check(bool condition,string text){if(!condition)throw new Exception("LANE_DIRECTION_PAINT: "+text);}
 public static void Run(){
  int glyphs=0;
  foreach(float angle in new[]{0f,17f,90f,135f,217f,270f,359f})foreach(int turns in Enumerable.Range(1,7)){
   var heading=Quaternion.Euler(0,angle,0)*Vector3.forward;var right=TrafficSystem.Right(heading);var centre=new Vector3(41,0,83);
   float min=float.MaxValue,max=float.MinValue;
   LaneDirectionPaint.Paint(new LaneDirectionPaint.Arrow{turns=turns,centre=centre,forward=heading},"test",(name,p,d,w,l,y)=>{
    float x=Vector3.Dot(p-centre,right),half=Mathf.Abs(Vector3.Dot(d,right))*l*.5f+Mathf.Abs(Vector3.Dot(TrafficSystem.Right(d),right))*w*.5f;
    min=Mathf.Min(min,x-half);max=Mathf.Max(max,x+half);
   });
   Check(Mathf.Abs((min+max)*.5f)<.001f,"whole glyph centred, heading="+angle+" turns="+turns);
   Check(max-min<2.6f,"glyph fits a narrow lane");glyphs++;
  }
  string previous=MapCatalog.LockedRegion;
  try{
   MapCatalog.LockedRegion="eungye";var map=MapCatalog.Load();var arrows=LaneDirectionPaint.Layout(map);
   Check(arrows.Count>100,"arrows cover all generated junction approaches, not just T1");
   Check(arrows.Select(a=>a.edgeIndex).Distinct().Count()==arrows.Count,"one glyph per terminal lane");
   foreach(var arrow in arrows){var e=map.hdEdges[arrow.edgeIndex];int flags=0;
    foreach(int i in e.next){var j=map.hdEdges[i];if(j.disabled||!j.id.StartsWith("join:"))continue;flags|=j.turn<0?1:j.turn>0?4:2;}
    Check(arrow.turns==flags,"paint uses current shared route movements "+e.id);
   }
   int junctions=arrows.Select(a=>map.hdEdges[a.edgeIndex].ruleId).Distinct().Count();
   Debug.Log("LANE_DIRECTION_PAINT_TEST_OK glyphCases="+glyphs+" lanes="+arrows.Count+" junctions="+junctions);
  }finally{MapCatalog.LockedRegion=previous;}
 }
}
