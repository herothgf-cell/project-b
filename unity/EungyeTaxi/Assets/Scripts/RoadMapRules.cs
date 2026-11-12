using System;
using System.Linq;
using UnityEngine;
public static class RoadMapRules {
 // Speed-sign facilities grouped by lane, rebuilt from live editor data by the
 // caller before each bulk scan. TravelSeconds(index,...) matches TravelSeconds(map,...)
 // term-for-term (same facilities, same order) without an O(edges*facilities) scan.
 public sealed class SpeedSignIndex {
  readonly System.Collections.Generic.Dictionary<string,System.Collections.Generic.List<RoadMapFacility>> lanes=new System.Collections.Generic.Dictionary<string,System.Collections.Generic.List<RoadMapFacility>>();
  public void Rebuild(MapData map){
   foreach(var list in lanes.Values)list.Clear();
   if(map==null||map.editorDocument==null||map.editorDocument.facilities==null)return;
   foreach(var f in map.editorDocument.facilities){if(f.kind!="sign"||!f.rule||f.type!="제한속도"||f.laneId==null)continue;System.Collections.Generic.List<RoadMapFacility> list;if(!lanes.TryGetValue(f.laneId,out list)){list=new System.Collections.Generic.List<RoadMapFacility>();lanes.Add(f.laneId,list);}list.Add(f);}
  }
  public float TravelSeconds(string laneId,float length,float cruise){
   float result=length/cruise;System.Collections.Generic.List<RoadMapFacility> list;if(laneId==null||!lanes.TryGetValue(laneId,out list))return result;
   foreach(var f in list){float overlap=Mathf.Max(0,Mathf.Min(length,f.endAt)-Mathf.Max(0,f.startAt));result+=overlap*(1/Mathf.Min(cruise,f.speed/3.6f)-1/cruise);}return result;
  }
 }
 public static float SpeedLimit(MapData map,string laneId,float at){if(map==null||map.editorDocument==null)return 0;foreach(var f in map.editorDocument.facilities)if(f.rule&&f.kind=="sign"&&f.type=="제한속도"&&f.laneId==laneId&&at>=f.startAt&&at<=f.endAt)return f.speed;return 0;}
 public static float TravelSeconds(MapData map,string laneId,float length,float cruise){float result=length/cruise;if(map==null||map.editorDocument==null)return result;foreach(var f in map.editorDocument.facilities){if(f.kind!="sign"||!f.rule||f.type!="제한속도"||f.laneId!=laneId)continue;float overlap=Mathf.Max(0,Mathf.Min(length,f.endAt)-Mathf.Max(0,f.startAt));result+=overlap*(1/Mathf.Min(cruise,f.speed/3.6f)-1/cruise);}return result;} public static bool Polygon(MapPoint[] points,Vector3 p){bool inside=false;for(int i=0,j=points.Length-1;i<points.Length;j=i++){var a=points[i];var b=points[j];if((a.z>p.z)!=(b.z>p.z)&&p.x<(b.x-a.x)*(p.z-a.z)/(b.z-a.z)+a.x)inside=!inside;}return inside;}
}
