using System;
using System.Collections.Generic;
using UnityEngine;

public static class RoadPaintStrokeContinuityTests {
 struct Stroke {public Vector3 p,d;public float length;public Vector3 A {get{return p-d*(length-.02f)*.5f;}}public Vector3 B {get{return p+d*(length-.02f)*.5f;}}}
 static void Check(bool value,string why){if(!value)throw new Exception("ROAD_PAINT_STROKE: "+why);}
 static List<Stroke> Render(Vector3[] points,int gap=-1,Func<Vector3,Vector3,float,bool> accept=null){
  var result=new List<Stroke>();Action flush;var emit=RoadPaintStrokeContinuity.Wrap((n,p,d,w,l,y)=>result.Add(new Stroke{p=p,d=d,length=l}),out flush,accept);
  for(int i=1;i<points.Length;i++){if(i==gap)continue;var delta=points[i]-points[i-1];emit("Centre line",(points[i]+points[i-1])*.5f,delta.normalized,.14f,delta.magnitude+.02f,true);}flush();return result;
 }
 static float MaxAngle(List<Stroke> strokes){float angle=0;for(int i=1;i<strokes.Count;i++)angle=Mathf.Max(angle,Vector3.Angle(strokes[i-1].d,strokes[i].d));return angle;}
 public static void Run(){
  var points=new[]{new Vector3(0,0,0),new Vector3(1,0,0),new Vector3(2,0,.6f),new Vector3(3,0,0),new Vector3(4,0,0)};
  var smooth=Render(points);Check(smooth.Count==4,"short V preserves stroke count");
  Check(MaxAngle(smooth)<12,"two passes remove the sharp short V, angle="+MaxAngle(smooth));
  Check(Vector3.Distance(smooth[0].A,points[0])<.0001f&&Vector3.Distance(smooth[3].B,points[4])<.0001f,"run endpoints remain fixed");
  for(int i=1;i<smooth.Count;i++)Check(Vector3.Distance(smooth[i-1].B,smooth[i].A)<.0001f,"smoothed strokes remain contiguous");
  var gap=Render(points,3);Check(gap.Count==3,"missing stroke is not filled");Check(Vector3.Distance(gap[1].B,points[2])<.0001f&&Vector3.Distance(gap[2].A,points[3])<.0001f,"gap endpoints remain fixed");
  var circle=new Vector3[31];for(int i=0;i<circle.Length;i++)circle[i]=new Vector3(30*Mathf.Cos(i*.03f),0,30*Mathf.Sin(i*.03f));
  var circular=Render(circle);Check(circular.Count==30,"smooth circle stroke count unchanged");for(int i=0;i<circular.Count;i++)Check(Vector3.Distance(circular[i].A,circle[i])<.00001f&&Vector3.Distance(circular[i].B,circle[i+1])<.00001f,"smooth circle endpoints unchanged");
  int callbacks=0;Render(points,-1,(p,d,l)=>{callbacks++;return false;});Check(callbacks==4,"every moved footprint is offered to clearance predicate");
  int white=0;Action flush;var emit=RoadPaintStrokeContinuity.Wrap((n,p,d,w,l,y)=>{if(!y)white++;},out flush);emit("Lane divider",Vector3.zero,Vector3.forward,.13f,.96f,false);Check(white==1,"white marks pass through before flushing");flush();
  Debug.Log("ROAD_PAINT_STROKE_CONTINUITY_OK V/gaps/endpoints/circle/clearance/white");
 }
}
