using System;
using System.Linq;
using UnityEngine;
/// <summary>Explicit surveyed U-turn semantics. Conditions live in existing lane-bound sign facilities.</summary>
public static class UturnMovement {
 public const int Turn=-2;
 public const string Yield="유턴 (상시 양보)",LeftGreen="유턴 (좌회전 신호시)",PedestrianGreen="유턴 (보행 신호시)",LeftOrPedestrianGreen="유턴 (좌회전·보행 신호시)";
 public static readonly string[] Conditions={Yield,LeftGreen,PedestrianGreen,LeftOrPedestrianGreen};
 public static bool IsCondition(string type){return Conditions.Contains(type);}
 public static string Label(int turn){return turn==Turn?"유턴":turn<0?"좌회전":turn>0?"우회전":"직진";}
 public static int Indicator(int turn){return turn==Turn?-1:turn;}
 public static int PaintMask(int turn){return turn==Turn?8:turn<0?1:turn>0?4:2;}
 public static bool Allows(string condition,bool leftGreen,bool pedestrianGreen,bool clear){return clear&&(condition==Yield||condition==LeftGreen&&leftGreen||condition==PedestrianGreen&&pedestrianGreen||condition==LeftOrPedestrianGreen&&(leftGreen||pedestrianGreen));}
 public static RoadMapFacility Condition(RoadMapDocument d,string laneId){if(d==null)return null;var signs=d.facilities.Where(f=>f.rule&&f.kind=="sign"&&f.laneId==laneId&&IsCondition(f.type)).ToArray();return signs.Length==1?signs[0]:null;}
 public static Vector3[] Geometry(RoadMapLane from,RoadMapLane target){
  if(from.disabled||target.disabled||from.change!=0||target.change!=0||from.turn!=0||target.turn!=0)throw new ArgumentException("유턴은 활성 일반 차로 사이에만 연결하세요");
  var a=from.points.Last().V();var b=target.points[0].V();var f=(a-from.points[from.points.Length-2].V()).normalized;var h=(target.points[1].V()-b).normalized;var left=-TrafficSystem.Right(f);float across=Vector3.Dot(b-a,left),along=Vector3.Dot(b-a,f);
  if(Vector3.Dot(f,h)>-.985f||across<10||across>40||Mathf.Abs(along)>across*.5f)throw new ArgumentException("유턴 대상은 왼쪽 반대 방향 차로이며 간격10~40m, 종방향 어긋남은 간격의 절반 이내여야 합니다");
  float radius=across*.5f,k=.55228475f;var mid=(a+b)*.5f+f*radius;var result=new Vector3[65];
  for(int i=0;i<65;i++){float t=i<=32?i/32f:(i-32)/32f,u=1-t;var p=i<=32?a:mid;var q=i<=32?mid:b;var c1=i<=32?a+f*(radius*k):mid+left*(radius*k);var c2=i<=32?mid-left*(radius*k):b-h*(radius*k);result[i]=u*u*u*p+3*u*u*t*c1+3*u*t*t*c2+t*t*t*q;}
  string error=GeometryError(result,f,h);if(error!=null)throw new ArgumentException(error);return result;
 }
 public static string GeometryError(Vector3[] points,Vector3 incoming,Vector3 outgoing){
  if(points==null||points.Length<5)return "유턴 곡선의 제어점이 부족합니다";
  if(Vector3.Dot(incoming.normalized,outgoing.normalized)>-.985f||Vector3.Dot((points[1]-points[0]).normalized,incoming.normalized)<.98f||Vector3.Dot((points.Last()-points[points.Length-2]).normalized,outgoing.normalized)<.98f)return "유턴 입출구 방향이 연속하지 않습니다";
  for(int i=1;i<points.Length-1;i++){var a=points[i]-points[i-1];var b=points[i+1]-points[i];float cross=Vector3.Cross(a,b).magnitude;if(a.magnitude<.01f||b.magnitude<.01f)return "유턴 중복 제어점";if(cross>.000001f&&a.magnitude*b.magnitude*(a+b).magnitude/(2*cross)<4.5f)return "유턴 최소 반경4.5m 미달";}
  return null;
 }
}
