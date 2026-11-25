using System;
using System.Linq;
using UnityEngine;

// 2026-11-25 map review P01, r53 -> r54. The far-side westbound signal at J5692_14220 (Y05 marker 44)
// stood with its pole on the asphalt where the apartment-gate road joins the main road: 0.1 m from the
// centre of the turn path join:local:7003:14854:5837:14446:0. The pole moves 5 m along the arm (north,
// onto the corner footway east of the gate road) and 2 m along the road (west), and the arm grows from
// 3.1 m to 8.1 m so the signal head still hangs over the westbound lanes. Only this facility changes:
// no lane, stop line, signal group or timing changes, so routes and law checks are unaffected.
public static class P01SignalPoleMove {
 const string Id="reviewed:Y05:44:0:-1:signal";
 const float Along=-2f,Out=5f;
 static void Require(bool pass,string message){if(!pass)throw new InvalidOperationException("P01 signal pole: "+message);}
 static float Distance(Vector2 p,Vector2 a,Vector2 b){var d=b-a;float t=d.sqrMagnitude<1e-6f?0:Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude);return Vector2.Distance(p,a+d*t);}
 static float LaneMargin(RoadMapDocument d,Vector2 p,out string lane){
  float best=float.MaxValue;lane="";
  foreach(var l in d.lanes){
   if(l.points==null||l.points.Length<2)continue;
   float m=float.MaxValue;for(int i=1;i<l.points.Length;i++)m=Mathf.Min(m,Distance(p,new Vector2(l.points[i-1].x,l.points[i-1].z),new Vector2(l.points[i].x,l.points[i].z)));
   m-=l.width*.5f;if(m<best){best=m;lane=l.id;}
  }
  return best;
 }
 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);
  var before=ProjectMapStore.Load(root);
  Require(before.document.revision==53,"expected map r53, found r"+before.document.revision);
  var d=before.document.Copy();
  var f=d.facilities.Single(x=>x.id==Id);
  Require(Mathf.Abs(f.x-569.27f)<.05f&&Mathf.Abs(f.z-1438.92f)<.05f&&Mathf.Abs(f.width-3.1f)<.01f,"unexpected start "+f.x+","+f.z+" width "+f.width);
  string lane;float start=LaneMargin(d,new Vector2(f.x,f.z),out lane);
  Require(start<-1.5f,"pole was expected inside a lane, margin "+start);
  var rotation=Quaternion.Euler(0,f.yaw,0);Vector3 right=rotation*Vector3.right,forward=rotation*Vector3.forward;
  Vector3 headBefore=new Vector3(f.x,0,f.z)-right*f.width;
  Vector3 pole=new Vector3(f.x,0,f.z)+forward*Along+right*Out;
  f.x=pole.x;f.z=pole.z;f.width+=Out;
  Vector3 headAfter=pole-right*f.width;
  float after=LaneMargin(d,new Vector2(f.x,f.z),out lane);
  Debug.Log("P01_POLE before margin="+start.ToString("F2")+" after=("+f.x.ToString("F2")+","+f.z.ToString("F2")+") margin="+after.ToString("F2")+" nearest="+lane+" headShift="+Vector3.Distance(headBefore,headAfter).ToString("F2"));
  Require(after>=.8f,"pole still within 0.8 m of a lane ("+lane+" "+after+")");
  Require(Vector3.Distance(headBefore,headAfter)<=Mathf.Abs(Along)+.01f,"signal head moved more than the along-road shift");
  d.revision=54;d.projectRevision="";d.savedAt=DateTime.UtcNow.ToString("o");
  var result=ProjectMapPackage.Create(before.baseline,d);
  ProjectMapStore.Save(root,result,token);
  var saved=ProjectMapStore.Load(root);
  Require(saved.contentHash==result.contentHash&&saved.document.revision==54,"saved map identity");
  Debug.Log("P01_POLE_SAVED "+saved.contentHash);
 }
}
