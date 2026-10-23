using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Paint describes the active shared graph. Provisional graph movements remain
// provisional; drawing an arrow does not establish a surveyed traffic rule.
public static class LaneDirectionPaint {
 public const int Left=1,Straight=2,Right=4;
 public sealed class Arrow {public int edgeIndex,turns;public Vector3 centre,forward;}
 struct Stroke {public Vector2 a,b;public Stroke(Vector2 a,Vector2 b){this.a=a;this.b=b;}}
 public static List<Arrow> Layout(MapData map){
  var result=new List<Arrow>();if(!map.hybrid||!map.HasHd)return result;
  for(int i=0;i<map.hdEdges.Length;i++){
   var e=map.hdEdges[i];if(e.disabled||e.status!="provisional"||e.change!=0||e.turn!=0||e.id.StartsWith("join:")||e.id.StartsWith("fuel:"))continue;
   if((e.next??new int[0]).Any(n=>!map.hdEdges[n].disabled&&map.hdEdges[n].sourceId==e.sourceId&&map.hdEdges[n].turn==0&&map.hdEdges[n].change==0&&!map.hdEdges[n].id.StartsWith("join:")))continue;
   int turns=0;foreach(int n in e.next??new int[0]){var join=map.hdEdges[n];if(join.disabled||!join.id.StartsWith("join:"))continue;turns|=join.turn<0?Left:join.turn>0?Right:Straight;}
   if(turns==0)continue;
   var p=new AssistedRoadNetwork.Path(e.points.Select(x=>x.V()).ToArray());
   // A whole glyph must fit before the connector, without crossing a split.
   if(p.length<9)continue;float at=Mathf.Max(4,p.length-10);
   result.Add(new Arrow{edgeIndex=i,turns=turns,centre=p.Position(at),forward=p.Tangent(at)});
  }return result;
 }
 public static void Paint(MapData map,Action<string,Vector3,Vector3,float,float,bool> mark){
  foreach(var arrow in Layout(map))Paint(arrow,"Lane arrow "+map.hdEdges[arrow.edgeIndex].id,mark);
 }
 public static void Paint(Arrow arrow,string name,Action<string,Vector3,Vector3,float,float,bool> mark){
  var lines=new List<Stroke>();Action<float,float,float,float> add=(x,y,u,v)=>lines.Add(new Stroke(new Vector2(x,y),new Vector2(u,v)));
  add(0,-2,0,.6f);
  if((arrow.turns&Straight)!=0){add(0,.6f,0,2);add(0,2,-.6f,1.15f);add(0,2,.6f,1.15f);}
  foreach(int side in new[]{-1,1})if((arrow.turns&(side<0?Left:Right))!=0){add(0,.6f,side*1.2f,.6f);add(side*1.2f,.6f,side*.6f,1.2f);add(side*1.2f,.6f,side*.6f,0);}
  const float width=.16f;float min=float.MaxValue,max=float.MinValue;
  foreach(var line in lines){var d=(line.b-line.a).normalized;float half=Mathf.Abs(d.y)*width*.5f;min=Mathf.Min(min,Mathf.Min(line.a.x,line.b.x)-half);max=Mathf.Max(max,Mathf.Max(line.a.x,line.b.x)+half);}
  // Centre the entire painted footprint, including arrowheads and stroke width.
  // A left-only arrow otherwise has its stem on centre and its body off centre.
  float shift=(min+max)*.5f;var right=TrafficSystem.Right(arrow.forward);
  foreach(var line in lines){var a=arrow.centre+right*(line.a.x-shift)+arrow.forward*line.a.y;var b=arrow.centre+right*(line.b.x-shift)+arrow.forward*line.b.y;mark(name,(a+b)*.5f,(b-a).normalized,width,Vector3.Distance(a,b),false);}
 }
}
