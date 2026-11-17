using System;
using System.Collections.Generic;
using UnityEngine;

// Presentation-only repair for short sharp joins in sampled yellow paint.
// No graph/path mutation, no gap filling, and no change to dashed/white marks.
public static class RoadPaintStrokeContinuity {
 const float Overlap=.02f,JoinTolerance=.08f,MaxArm=2.5f,SharpAngle=12f;
 sealed class Stroke {public string kind;public Vector3 p,d;public float width,length;public bool yellow;public Vector3 Start {get{return p-d*(length-Overlap)*.5f;}}public Vector3 End {get{return p+d*(length-Overlap)*.5f;}}}
 sealed class Buffer {
  readonly Action<string,Vector3,Vector3,float,float,bool> output;
  readonly Func<Vector3,Vector3,float,bool> acceptMoved;
  readonly List<Stroke> run=new List<Stroke>();
  public Buffer(Action<string,Vector3,Vector3,float,float,bool> output,Func<Vector3,Vector3,float,bool> acceptMoved){this.output=output;this.acceptMoved=acceptMoved;}
  public void Emit(string kind,Vector3 p,Vector3 d,float width,float length,bool yellow){
   if(!yellow||kind!="Centre line"||length<=Overlap){output(kind,p,d,width,length,yellow);return;}
   var stroke=new Stroke{kind=kind,p=p,d=d.normalized,width=width,length=length,yellow=yellow};
   if(run.Count>0){var previous=run[run.Count-1];if(Vector3.Distance(previous.End,stroke.Start)>JoinTolerance||Mathf.Abs(previous.width-width)>.001f)Flush();}
   run.Add(stroke);
  }
  public void Flush(){
   if(run.Count==0)return;
   var vertices=new Vector3[run.Count+1];vertices[0]=run[0].Start;vertices[run.Count]=run[run.Count-1].End;
   for(int i=1;i<run.Count;i++)vertices[i]=(run[i-1].End+run[i].Start)*.5f;
   bool changed=false;
   for(int pass=0;pass<2;pass++){
    var next=(Vector3[])vertices.Clone();
    for(int i=1;i<run.Count;i++){
     var incoming=vertices[i]-vertices[i-1];var outgoing=vertices[i+1]-vertices[i];
     if(incoming.magnitude>=MaxArm||outgoing.magnitude>=MaxArm||incoming.sqrMagnitude<.0001f||outgoing.sqrMagnitude<.0001f||Vector3.Angle(incoming,outgoing)<=SharpAngle)continue;
     next[i]=vertices[i]*.5f+(vertices[i-1]+vertices[i+1])*.25f;changed=true;
    }
    vertices=next;
   }
   for(int i=0;i<run.Count;i++){
    var original=run[i];
    // Preserve completely smooth runs byte-for-byte, including their tiny
    // original seams; do not resample circles or regular dashed geometry.
    if(!changed){output(original.kind,original.p,original.d,original.width,original.length,true);continue;}
    var delta=vertices[i+1]-vertices[i];float distance=delta.magnitude;
    if(distance<.0001f)continue;
    var center=(vertices[i]+vertices[i+1])*.5f;var direction=delta/distance;float length=distance+Overlap;
    bool moved=(center-original.p).sqrMagnitude>.00000001f||(direction-original.d).sqrMagnitude>.00000001f||Mathf.Abs(length-original.length)>.0001f;
    // Caller rechecks moved footprints against junction/crosswalk clearances
    // and U-turn openings before they enter the final uniqueness filter.
    if(!moved||acceptMoved==null||acceptMoved(center,direction,length))output(original.kind,center,direction,original.width,length,true);
   }
   run.Clear();
  }
 }
 public static Action<string,Vector3,Vector3,float,float,bool> Wrap(Action<string,Vector3,Vector3,float,float,bool> output,out Action flush,Func<Vector3,Vector3,float,bool> acceptMoved=null){
  if(output==null)throw new ArgumentNullException("output");var buffer=new Buffer(output,acceptMoved);flush=buffer.Flush;return buffer.Emit;
 }
}
