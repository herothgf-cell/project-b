using System;
using System.Linq;
using UnityEngine;

public static class LaneDirectionPaintTests {
 static void Check(bool condition,string text){if(!condition)throw new Exception("LANE_DIRECTION_PAINT: "+text);}
 public static void Run(){
  GeometryRegressions();ConnectedMovementRegressions();
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
    if(e.status=="ngii"){
     var assignment=map.editorDocument.facilities.SingleOrDefault(f=>f.kind=="lanePresentation"&&f.laneId==e.id);
     Check(assignment!=null,"native glyph requires an explicitly selected approach "+e.id);
     var junction=map.junctionRules.Single(j=>j.id==assignment.junctionId);
     flags=ReviewedNativeLanePresentation.MovementMask(map,e,junction);
     // Independent site expectations catch the source turn=0 return curves
     // being mistaken for straight movement by the old one-hop assertion.
     if(new[]{"A2218G611780:0","A2218G611350:2","A2218G611401:0"}.Contains(e.id))Check(flags==(LaneDirectionPaint.Left|LaneDirectionPaint.Uturn),"reviewed median lane retains existing left and return paths "+e.id);
     else if(e.id=="A2218G611305:0")Check(flags==(LaneDirectionPaint.Straight|LaneDirectionPaint.Right),"reviewed outer lane retains straight and right paths");
     else if(new[]{"A2218G611284:0","A2218G611282:0","A222BC004202:4","A222BC004204:3","A222BC004206:4","A2218G611404:0","A2218G611403:0","A2218G611402:0"}.Contains(e.id))Check(flags==LaneDirectionPaint.Straight,"reviewed native through lane keeps only its existing through role "+e.id);
    }else foreach(int i in e.next){var j=map.hdEdges[i];if(j.disabled||!j.id.StartsWith("join:")||!(j.next??new int[0]).Any(n=>!map.hdEdges[n].disabled))continue;flags|=UturnMovement.PaintMask(j.turn);}
    Check(arrow.turns==flags,"paint uses current shared route movements "+e.id);
   }
   var detachedLeft=arrows.Single(a=>map.hdEdges[a.edgeIndex].id=="local:14385:13336:10603:1539:part:2");
   Check(detachedLeft.turns==LaneDirectionPaint.Straight,"actual approach paints only straight when its authored left connector has no exit");
   var approach=arrows.Where(a=>a.centre.z>585&&a.centre.z<619&&a.forward.z>.95f&&map.hdEdges[a.edgeIndex].ruleId=="J8066_6364").OrderBy(a=>a.centre.x).ToArray();
   Check(approach.Select(a=>a.turns).SequenceEqual(new[]{LaneDirectionPaint.Left,LaneDirectionPaint.Straight,LaneDirectionPaint.Straight,LaneDirectionPaint.Straight,LaneDirectionPaint.Right}),"J8066 northbound arrows left / three straight / right");
   Check(UturnRoadMarkings.FallbackArrows(map).Any(a=>a.centre.z>585&&a.centre.z<619&&a.forward.z>.95f&&map.hdEdges[a.edgeIndex].ruleId=="J8066_6364"),"J8066 northbound left lane also paints a U-turn");
   int solid=0;UturnRoadMarkings.Paint(map,(name,p,d,w,l,y)=>{if(name=="U-turn permitted median"&&p.z>600&&p.z<619&&Mathf.Abs(p.x-805.9f)<.5f&&d.z>.95f&&l>2&&!y)solid++;});
   Check(solid==1,"J8066 northbound median opening is continuous white");
   bool yellowInOpening=false;
   RoadPaintGeometry.Paint(map,null,(name,p,d,w,l,y)=>{if(y&&p.z>607&&p.z<611&&Mathf.Abs(p.x-805.9f)<.5f&&Mathf.Abs(d.z)>.95f)yellowInOpening=true;});
   Check(!yellowInOpening,"J8066 replaces the yellow median at the U-turn opening");
   int junctions=arrows.Select(a=>map.hdEdges[a.edgeIndex].ruleId).Distinct().Count();
   Debug.Log("LANE_DIRECTION_PAINT_TEST_OK glyphCases="+glyphs+" lanes="+arrows.Count+" junctions="+junctions);
  }finally{MapCatalog.LockedRegion=previous;}
 }
 static void ConnectedMovementRegressions(){
  var incoming=new HdEdge{id="approach",sourceId="approach",status="provisional",points=new[]{new MapPoint{x=0,z=0},new MapPoint{x=0,z=30}},next=new[]{1,2}};
  var left=new HdEdge{id="join:left",turn=-1,next=new int[0]};
  var straight=new HdEdge{id="join:straight",turn=0,next=new[]{3}};
  var exit=new HdEdge{id="exit",points=new[]{new MapPoint{x=0,z=30},new MapPoint{x=0,z=60}},next=new int[0]};
  var map=new MapData{hybrid=true,hdEdges=new[]{incoming,left,straight,exit}};
  var arrows=LaneDirectionPaint.Layout(map);Check(arrows.Count==1&&arrows[0].turns==LaneDirectionPaint.Straight,"detached left connector never creates a painted left permission");
  left.next=new[]{3};arrows=LaneDirectionPaint.Layout(map);Check(arrows.Count==1&&arrows[0].turns==(LaneDirectionPaint.Left|LaneDirectionPaint.Straight),"connecting the existing left exit restores its declared glyph");
  exit.disabled=true;Check(LaneDirectionPaint.Layout(map).Count==0,"disabled shared exit removes both movement glyphs");
 }
 static void GeometryRegressions(){
  var wide=new HybridJunctionRule{id="wide",dx=0,dz=1,crosswalk=true,stopLine=true,approaches=new[]{new HybridApproach{dx=0,dz=1,width=42.6f},new HybridApproach{dx=1,dz=0,width=14}}};
  var map=new MapData{junctionRules=new[]{wide}};var zones=RoadJunctionGeometry.PaintClearances(map);
  Check(zones.Any(z=>z.Contains(new Vector3(20,0,6))),"wide physical junction corner beyond old 18m circle clipped");
  Check(!zones.Any(z=>z.Contains(new Vector3(24,0,10))),"clearance rectangle preserves nearby guide outside actual road width");
  Check(!zones.Any(z=>z.Contains(new Vector3(0,0,-15))),"upstream guide beyond physical stop survives");
  map.editorDocument=new RoadMapDocument{facilities=new[]{new RoadMapFacility{kind="crosswalk",junctionId="wide",axis=0,sign=1,z=-30,yaw=0,width=42.6f,length=2.4f},new RoadMapFacility{kind="stopLine",junctionId="wide",axis=0,sign=1,z=-34,yaw=0,width=21.3f,length=.4f}}};
  zones=RoadJunctionGeometry.PaintClearances(map);
  Check(zones.Any(z=>z.Contains(new Vector3(0,0,-34.1f))),"edited stop outer edge respected");
  Check(!zones.Any(z=>z.Contains(new Vector3(0,0,-35))),"paint retained immediately upstream of edited stop band");
  var mixed=new LaneDirectionPaint.Arrow{turns=LaneDirectionPaint.Left|LaneDirectionPaint.Uturn,centre=new Vector3(5,0,-45),forward=Vector3.forward};
  var glyphs=LaneDirectionPaint.Glyphs(mixed);
  Check(glyphs.Count==2&&glyphs[0].turns==LaneDirectionPaint.Left&&glyphs[1].turns==LaneDirectionPaint.Uturn,"combined legal mask rendered as separate left and U-turn glyphs");
  Check(Vector3.Distance(glyphs[0].centre,glyphs[1].centre)>=4.4f,"glyph footprints have longitudinal clearance");
  Check(mixed.turns==(LaneDirectionPaint.Left|LaneDirectionPaint.Uturn),"layout never changes legal movement mask");
  Check(LaneDirectionPaint.Clear(mixed,zones),"combined glyphs fit upstream of edited stop");
  mixed.centre=new Vector3(5,0,-34);Check(!LaneDirectionPaint.Clear(mixed,zones),"full glyph footprint rejects stop overlap");
  var arrows=new System.Collections.Generic.List<LaneDirectionPaint.Arrow>{new LaneDirectionPaint.Arrow{edgeIndex=1,turns=1,forward=Vector3.forward},new LaneDirectionPaint.Arrow{edgeIndex=2,turns=1,centre=new Vector3(.1f,0,1),forward=Vector3.forward},new LaneDirectionPaint.Arrow{edgeIndex=3,turns=1,centre=new Vector3(3.5f,0,0),forward=Vector3.forward},new LaneDirectionPaint.Arrow{edgeIndex=4,turns=2,forward=Vector3.forward}};
  var unique=LaneDirectionPaint.Deduplicate(arrows);
  Check(unique.Count==3&&unique.Any(a=>a.edgeIndex==3)&&unique.Any(a=>a.edgeIndex==4),"coincident equal masks deduplicated; adjacent lanes and different legal masks retained");
  int strokes=0;var paint=RoadPaintGeometry.UniquePaint((n,p,d,w,l,y)=>strokes++);
  paint("Centre line",Vector3.zero,Vector3.forward,.14f,.96f,true);paint("Centre line",new Vector3(.01f,0,.01f),Vector3.back,.14f,.96f,true);paint("Centre line",new Vector3(3.5f,0,0),Vector3.forward,.14f,.96f,true);paint("Centre line",Vector3.zero,Vector3.right,.14f,.96f,true);
  Check(strokes==3,"coincident compiled paint removed while adjacent and crossing guides survive");
 }
}
