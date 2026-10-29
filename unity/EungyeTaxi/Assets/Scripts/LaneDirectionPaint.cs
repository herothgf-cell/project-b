using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Paint describes the active shared graph. Provisional graph movements remain
// provisional; drawing an arrow does not establish a surveyed traffic rule.
public static class LaneDirectionPaint {
 public const int Left=1,Straight=2,Right=4,Uturn=8;
 public sealed class Arrow {public int edgeIndex,turns;public Vector3 centre,forward;}
 public const float GlyphSpacing=6;
 // Keep the legal movement mask on Arrow; paint combined U-turn indications
 // as separate longitudinal glyphs rather than superimposed arrowheads.
 public static List<Arrow> Glyphs(Arrow arrow){
  var result=new List<Arrow>();int other=arrow.turns&~Uturn;
  if((arrow.turns&Uturn)!=0&&other!=0){result.Add(new Arrow{edgeIndex=arrow.edgeIndex,turns=other,centre=arrow.centre,forward=arrow.forward});result.Add(new Arrow{edgeIndex=arrow.edgeIndex,turns=Uturn,centre=arrow.centre-arrow.forward*GlyphSpacing,forward=arrow.forward});}
  else result.Add(arrow);return result;
 }
 public static bool Clear(Arrow arrow,List<RoadJunctionGeometry.Clearance> clearances){foreach(var glyph in Glyphs(arrow))foreach(var zone in clearances)if(zone.Overlaps(glyph.centre,glyph.forward,1.3f,2.2f))return false;return true;}
 static bool ConflictingFootprint(Arrow arrow,List<Arrow> placed){
  List<Arrow> glyphs=null;foreach(var prior in placed){if(prior.turns==arrow.turns||(prior.centre-arrow.centre).sqrMagnitude>20*20)continue;if(glyphs==null)glyphs=Glyphs(arrow);foreach(var a in glyphs)foreach(var b in Glyphs(prior)){
   var footprint=new RoadJunctionGeometry.Clearance{center=b.centre,forward=b.forward,halfWidth=1.3f,halfDepth=2.2f};
   if(footprint.Overlaps(a.centre,a.forward,1.3f,2.2f))return true;
  }}return false;
 }
 public static List<Arrow> Deduplicate(List<Arrow> candidates){
  var result=new List<Arrow>();var cells=new Dictionary<Vector2Int,List<Arrow>>();
  foreach(var arrow in candidates){var key=new Vector2Int(Mathf.FloorToInt(arrow.centre.x/4),Mathf.FloorToInt(arrow.centre.z/4));bool duplicate=false;
   for(int x=-1;x<=1&&!duplicate;x++)for(int z=-1;z<=1&&!duplicate;z++){List<Arrow> near;if(!cells.TryGetValue(key+new Vector2Int(x,z),out near))continue;
    foreach(var prior in near){var delta=arrow.centre-prior.centre;if(arrow.turns==prior.turns&&Vector3.Dot(arrow.forward,prior.forward)>.995f&&Mathf.Abs(Vector3.Dot(delta,arrow.forward))<3&&Mathf.Abs(Vector3.Dot(delta,TrafficSystem.Right(arrow.forward)))<.8f){duplicate=true;break;}}
   }if(duplicate)continue;List<Arrow> list;if(!cells.TryGetValue(key,out list)){list=new List<Arrow>();cells.Add(key,list);}list.Add(arrow);result.Add(arrow);
  }return result;
 }
 struct Stroke {public Vector2 a,b;public Stroke(Vector2 a,Vector2 b){this.a=a;this.b=b;}}
 public static List<Arrow> Layout(MapData map){
  var result=new List<Arrow>();if(!map.hybrid||!map.HasHd)return result;var clearances=RoadJunctionGeometry.PaintClearances(map);
  for(int i=0;i<map.hdEdges.Length;i++){
   var e=map.hdEdges[i];if(e.disabled||e.status!="provisional"||e.change!=0||e.turn!=0||e.id.StartsWith("join:")||e.id.StartsWith("fuel:"))continue;
   if((e.next??new int[0]).Any(n=>!map.hdEdges[n].disabled&&map.hdEdges[n].sourceId==e.sourceId&&map.hdEdges[n].turn==0&&map.hdEdges[n].change==0&&!map.hdEdges[n].id.StartsWith("join:")))continue;
   int turns=0;foreach(int n in e.next??new int[0]){var join=map.hdEdges[n];if(join.disabled||!join.id.StartsWith("join:"))continue;turns|=UturnMovement.PaintMask(join.turn);}
   if(turns==0)continue;
   var p=new AssistedRoadNetwork.Path(e.points.Select(x=>x.V()).ToArray());
   // A whole glyph must fit before the connector, without crossing a split.
   bool combined=(turns&Uturn)!=0&&(turns&~Uturn)!=0;float minimum=combined?GlyphSpacing+2.2f:4;
   if(p.length<Mathf.Max(9,minimum+2.2f))continue;float at=Mathf.Max(minimum,p.length-10);
   // Different legal masks on coincident taper candidates cannot be ORed.
   // Give them separate, clear stations along their own approach instead.
   for(;at>=minimum;at-=1){var arrow=new Arrow{edgeIndex=i,turns=turns,centre=p.Position(at),forward=p.Tangent(at)};if(!Clear(arrow,clearances)||ConflictingFootprint(arrow,result))continue;result.Add(arrow);break;}
  }return Deduplicate(result);
 }
 public static void Paint(MapData map,Action<string,Vector3,Vector3,float,float,bool> mark){
  foreach(var arrow in Layout(map))Paint(arrow,"Lane arrow "+map.hdEdges[arrow.edgeIndex].id,mark);
 }
 public static void Paint(Arrow arrow,string name,Action<string,Vector3,Vector3,float,float,bool> mark){
  foreach(var glyph in Glyphs(arrow))PaintGlyph(glyph,name,mark);
 }
 static void PaintGlyph(Arrow arrow,string name,Action<string,Vector3,Vector3,float,float,bool> mark){
  var lines=new List<Stroke>();Action<float,float,float,float> add=(x,y,u,v)=>lines.Add(new Stroke(new Vector2(x,y),new Vector2(u,v)));
  add(0,-2,0,.6f);
  if((arrow.turns&Straight)!=0){add(0,.6f,0,2);add(0,2,-.6f,1.15f);add(0,2,.6f,1.15f);}
  foreach(int side in new[]{-1,1})if((arrow.turns&(side<0?Left:Right))!=0){add(0,.6f,side*1.2f,.6f);add(side*1.2f,.6f,side*.6f,1.2f);add(side*1.2f,.6f,side*.6f,0);}
  if((arrow.turns&Uturn)!=0){add(0,.6f,0,1.4f);add(0,1.4f,-.3f,1.8f);add(-.3f,1.8f,-.9f,1.8f);add(-.9f,1.8f,-1.2f,1.4f);add(-1.2f,1.4f,-1.2f,-.3f);add(-1.2f,-.3f,-1.65f,.3f);add(-1.2f,-.3f,-.75f,.3f);}const float width=.16f;float min=float.MaxValue,max=float.MinValue;
  foreach(var line in lines){var d=(line.b-line.a).normalized;float half=Mathf.Abs(d.y)*width*.5f;min=Mathf.Min(min,Mathf.Min(line.a.x,line.b.x)-half);max=Mathf.Max(max,Mathf.Max(line.a.x,line.b.x)+half);}
  // Centre the entire painted footprint, including arrowheads and stroke width.
  // A left-only arrow otherwise has its stem on centre and its body off centre.
  float shift=(min+max)*.5f;var right=TrafficSystem.Right(arrow.forward);
  foreach(var line in lines){var a=arrow.centre+right*(line.a.x-shift)+arrow.forward*line.a.y;var b=arrow.centre+right*(line.b.x-shift)+arrow.forward*line.b.y;mark(name,(a+b)*.5f,(b-a).normalized,width,Vector3.Distance(a,b),false);}
 }
}
