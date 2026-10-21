using System;
using System.Collections.Generic;

// Pure geometry shared by the road renderer and its regression tests.
public static class RoadGeometry {
 public struct Point {public float x,z;public Point(float x,float z){this.x=x;this.z=z;}}
 public struct Lane {public float offset;public int direction;public Lane(float offset,int direction){this.offset=offset;this.direction=direction;}}
 public struct Boundary {public float offset;public bool yellow,dashed;public Boundary(float offset,bool yellow,bool dashed){this.offset=offset;this.yellow=yellow;this.dashed=dashed;}}
 public static List<Boundary> Boundaries(List<Lane> lanes){
  var sorted=new List<Lane>(lanes);sorted.Sort((a,b)=>a.offset.CompareTo(b.offset));var result=new List<Boundary>();
  for(int i=1;i<sorted.Count;i++){
   var a=sorted[i-1];var b=sorted[i];if(b.offset-a.offset<.5f)continue;
   bool opposing=a.direction!=b.direction;
   result.Add(new Boundary((a.offset+b.offset)*.5f,opposing,!opposing));
  }return result;
 }
 public static float DistanceSquared(Point p,Point a,Point b){float x=b.x-a.x,z=b.z-a.z,s=x*x+z*z;float t=s<.000001f?0:Math.Max(0,Math.Min(1,((p.x-a.x)*x+(p.z-a.z)*z)/s));x=p.x-a.x-t*x;z=p.z-a.z-t*z;return x*x+z*z;}
 static float Cross(Point a,Point b,Point c){return (b.x-a.x)*(c.z-a.z)-(b.z-a.z)*(c.x-a.x);}
 public static bool TryCrossing(Point a,Point b,Point c,Point d,out Point point){
  point=new Point();float x=b.x-a.x,z=b.z-a.z,u=d.x-c.x,v=d.z-c.z,denominator=x*v-z*u;
  if(Math.Abs(denominator)<.0001f)return false;
  float t=((c.x-a.x)*v-(c.z-a.z)*u)/denominator,s=((c.x-a.x)*z-(c.z-a.z)*x)/denominator;
  if(t<0||t>1||s<0||s>1)return false;point=new Point(a.x+x*t,a.z+z*t);return true;
 }
 static bool Intersects(Point a,Point b,Point c,Point d){
  if(Math.Max(a.x,b.x)<Math.Min(c.x,d.x)||Math.Max(c.x,d.x)<Math.Min(a.x,b.x)||Math.Max(a.z,b.z)<Math.Min(c.z,d.z)||Math.Max(c.z,d.z)<Math.Min(a.z,b.z))return false;
  return Cross(a,b,c)*Cross(a,b,d)<=0&&Cross(c,d,a)*Cross(c,d,b)<=0;
 }
 public static bool Contains(Point p,Point[] ring){bool inside=false;for(int i=0,j=ring.Length-1;i<ring.Length;j=i++){var a=ring[i];var b=ring[j];if((a.z>p.z)!=(b.z>p.z)&&p.x<(b.x-a.x)*(p.z-a.z)/(b.z-a.z)+a.x)inside=!inside;}return inside;}
 public static bool SweptLaneIntersects(Point a,Point b,Point[] polygon,float clearance){
  if(polygon==null||polygon.Length<3)return false;if(Contains(a,polygon)||Contains(b,polygon))return true;
  float square=clearance*clearance;
  for(int i=0,j=polygon.Length-1;i<polygon.Length;j=i++){
   var c=polygon[j];var d=polygon[i];if(Intersects(a,b,c,d)||DistanceSquared(a,c,d)<=square||DistanceSquared(b,c,d)<=square||DistanceSquared(c,a,b)<=square||DistanceSquared(d,a,b)<=square)return true;
  }return false;
 }
}
