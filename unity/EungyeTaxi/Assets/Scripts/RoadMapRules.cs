using System;
using System.Linq;
using UnityEngine;
public static class RoadMapRules {
 public static float SpeedLimit(MapData map,string laneId,float at){if(map==null||map.editorDocument==null)return 0;var sign=map.editorDocument.facilities.FirstOrDefault(f=>f.rule&&f.kind=="sign"&&f.type=="제한속도"&&f.laneId==laneId&&at>=f.startAt&&at<=f.endAt);return sign==null?0:sign.speed;}
 public static float TravelSeconds(MapData map,string laneId,float length,float cruise){float result=length/cruise;if(map==null||map.editorDocument==null)return result;foreach(var f in map.editorDocument.facilities.Where(f=>f.kind=="sign"&&f.rule&&f.type=="제한속도"&&f.laneId==laneId)){float overlap=Mathf.Max(0,Mathf.Min(length,f.endAt)-Mathf.Max(0,f.startAt));result+=overlap*(1/Mathf.Min(cruise,f.speed/3.6f)-1/cruise);}return result;} public static bool Polygon(MapPoint[] points,Vector3 p){bool inside=false;for(int i=0,j=points.Length-1;i<points.Length;j=i++){var a=points[i];var b=points[j];if((a.z>p.z)!=(b.z>p.z)&&p.x<(b.x-a.x)*(p.z-a.z)/(b.z-a.z)+a.x)inside=!inside;}return inside;}
}
