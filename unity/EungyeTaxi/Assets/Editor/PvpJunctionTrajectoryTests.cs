using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Final-suite geometry checks only. They do not simulate a driver or claim
// physics/manual-play coverage; the combined release suite owns those checks.
public static class PvpJunctionTrajectoryTests {
 sealed class Movement {
  public HdEdge join,from,to;
  public AssistedRoadNetwork.Path path;
  public Vector3 entering,leaving;
  public int exitLanes;
 }
 struct Pose {public Vector3 p,d;}
 static void Check(bool value,string message){if(!value)throw new Exception("PVP_JUNCTION_TRAJECTORY: "+message);}
 static Vector3 First(HdEdge e){return (e.points[1].V()-e.points[0].V()).normalized;}
 static Vector3 Last(HdEdge e){return (e.points.Last().V()-e.points[e.points.Length-2].V()).normalized;}
 static Pose[] Samples(Movement m){
  var poses=new List<Pose>();
  // Include 5 m of the receiving lane: near-exit corner clearance matters as
  // much as the intersection centre when the two cars arrive asynchronously.
  for(float at=0;at<m.path.length;at+=.5f)poses.Add(new Pose{p=m.path.Position(at),d=m.path.Tangent(at)});
  var exit=new AssistedRoadNetwork.Path(m.to.points.Select(p=>p.V()).ToArray());
  for(float at=0;at<=Mathf.Min(5,exit.length);at+=.5f)poses.Add(new Pose{p=exit.Position(at),d=exit.Tangent(at)});
  return poses.ToArray();
 }
 static float Radius(Vector3 a,Vector3 b,Vector3 c){
  float cross=Vector3.Cross(b-a,c-a).magnitude;
  return cross<.000001f?float.PositiveInfinity:Vector3.Distance(a,b)*Vector3.Distance(b,c)*Vector3.Distance(c,a)/(2*cross);
 }
 // Independent 2D separating-axis test, using the production NPC obstacle
 // query's .95 x 2.25 m half-extents (larger than the 1.8 x 4.2 m body).
 static bool Overlap(Pose a,Pose b){
  var delta=b.p-a.p;if(delta.sqrMagnitude>24)return false;
  var ar=TrafficSystem.Right(a.d);var br=TrafficSystem.Right(b.d);
  foreach(var axis in new[]{a.d,ar,b.d,br}){
   float extent=.95f*(Mathf.Abs(Vector3.Dot(ar,axis))+Mathf.Abs(Vector3.Dot(br,axis)))+2.25f*(Mathf.Abs(Vector3.Dot(a.d,axis))+Mathf.Abs(Vector3.Dot(b.d,axis)));
   if(Mathf.Abs(Vector3.Dot(delta,axis))>=extent-.001f)return false;
  }
  return true;
 }
 public static void Run(){
  var builder=new PvpCityRoadBuilder();var edges=builder.edges.ToArray();var movements=new List<Movement>();
  int turns=0,pairs=0,shared=0;float minimumRadius=float.PositiveInfinity;
  // This is the assisted taxi's strictest low-speed steering radius. A path
  // passing this geometric check still needs real wheel/driver verification.
  float taxiRadius=2.6f/Mathf.Tan(DriveMath.ParkingSteerAngle*Mathf.Deg2Rad);
  for(int i=0;i<edges.Length;i++){
   var join=edges[i];if(!join.id.StartsWith("join:")||Mathf.Abs(join.turn)!=1)continue;
   int index=i;var from=edges.Single(e=>e.next.Contains(index));var to=edges[join.next.Single()];
   var path=new AssistedRoadNetwork.Path(join.points.Select(p=>p.V()).ToArray());
   var m=new Movement{join=join,from=from,to=to,path=path,entering=Last(from),leaving=First(to),exitLanes=builder.roads.Single(r=>r.id==to.sourceId).DisplayLanes/2};movements.Add(m);turns++;
   Check(Vector3.Distance(from.points.Last().V(),path.points[0])<.01f&&Vector3.Distance(path.points.Last(),to.points[0].V())<.01f,"connected endpoints "+join.id);
   Check(Vector3.Dot(m.entering,path.Tangent(0))>.97f&&Vector3.Dot(m.leaving,path.Tangent(path.length))>.97f,"native road end tangents "+join.id);
   for(int p=1;p<path.points.Length-1;p++){
    float radius=Radius(path.points[p-1],path.points[p],path.points[p+1]);minimumRadius=Mathf.Min(minimumRadius,radius);
    Check(radius>=taxiRadius,"turn tighter than native taxi steering radius "+join.id+" radius="+radius);
    Check(Vector3.Dot(path.points[p]-path.points[p-1],path.points[p+1]-path.points[p])>0,"no folded turn segment "+join.id);
   }
  }
  foreach(var left in movements.Where(m=>m.join.turn<0))foreach(var right in movements.Where(m=>m.join.turn>0)){
   if(left.join.ruleId!=right.join.ruleId||left.to.sourceId!=right.to.sourceId||Vector3.Dot(left.entering,right.entering)>-.9f)continue;
   pairs++;
   Check(left.to.number==1&&right.to.number==right.exitLanes,"opposite turns choose inner/outer exit lanes "+left.join.id+" / "+right.join.id);
   if(left.exitLanes==1){shared++;Check(left.to==right.to,"single exit remains shared and must yield "+left.join.ruleId);continue;}
   Check(left.to!=right.to,"multilane opposite turns have distinct exit edges "+left.join.ruleId);
   var a=Samples(left);var b=Samples(right);
   foreach(var x in a)foreach(var y in b)Check(!Overlap(x,y),"disjoint exit turn envelopes overlap "+left.join.id+" / "+right.join.id+" positions="+x.p+" / "+y.p);
  }
  Check(turns>100&&pairs>20&&shared>0&&pairs>shared,"all-city fixtures include ordinary turns, separated exits, and genuine shared exits");
  Debug.Log("PVP_JUNCTION_TRAJECTORY_OK turns="+turns+" oppositeLeftRightPairs="+pairs+" separated="+(pairs-shared)+" shared="+shared+" minimumRadius="+minimumRadius+" nativeTaxiRadius="+taxiRadius+" envelopeSampleMetres=0.5");
 }
}
