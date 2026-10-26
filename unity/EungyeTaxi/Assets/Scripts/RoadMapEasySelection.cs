using System;
using System.Linq;
using UnityEngine;
public static class RoadMapEasySelection {
 public static bool EditableLane(RoadMapLane l){return l!=null&&!l.disabled&&l.turn==0&&l.change==0&&!l.id.StartsWith("join:")&&l.points!=null&&l.points.Length>1;}
 public static Vector3 Heading(RoadMapLane l){return (l.points[l.points.Length-1].V()-l.points[Mathf.Max(0,l.points.Length-2)].V()).normalized;}
 public static string HeadingLabel(Vector3 d){string[] names={"남쪽 진입","남서쪽 진입","서쪽 진입","북서쪽 진입","북쪽 진입","북동쪽 진입","동쪽 진입","남동쪽 진입"};int oct=(Mathf.RoundToInt(Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg/45)+8)%8;return names[oct];}
 public static string Direction(RoadMapLane l){return HeadingLabel(Heading(l));}
 public static string ApproachKey(RoadMapLane l){var d=Heading(l);int oct=(Mathf.RoundToInt(Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg/45)+8)%8;return l.ruleId+"|"+l.sourceId+"|"+oct;}
 public static string RoadLabel(RoadMapLane l){return string.IsNullOrWhiteSpace(l.roadName)?"이름 없는 도로":l.roadName;}
 public static string LaneLabel(RoadMapLane l){return Direction(l)+" · "+Mathf.Max(1,l.number)+"차로";}
 public static RoadMapLane PickLane(RoadMapDocument d,Vector3 p,float radius){RoadMapLane best=null;float error=radius;foreach(var l in d.lanes.Where(EditableLane))for(int i=1;i<l.points.Length;i++){var a=l.points[i-1].V();var delta=l.points[i].V()-a;float t=Mathf.Clamp01(Vector3.Dot(p-a,delta)/Mathf.Max(.00001f,delta.sqrMagnitude));float e=Vector3.Distance(p,a+delta*t);if(e<error){error=e;best=l;}}return best;}
 public static int Turns(RoadMapDocument d,RoadMapLane l){int result=0;foreach(var id in l.next){var n=d.lanes.FirstOrDefault(e=>e.id==id&&!e.disabled&&e.change==0&&e.id.StartsWith("join:"));if(n!=null)result|=UturnMovement.PaintMask(n.turn);}return result;}
 public static string TurnLabel(int turns){return ((turns&8)>0?"↶ 유턴 ":"")+((turns&1)>0?"← ":"")+((turns&2)>0?"↑ ":"")+((turns&4)>0?"→":"");}
}
public static class RoadMapEditorText {
 public struct Fitted {public GUIStyle style;public string text;}
 public static Fitted Fit(GUIStyle source,string text,Rect bounds){var s=new GUIStyle(source){clipping=TextClipping.Clip,wordWrap=true,alignment=TextAnchor.MiddleCenter};text=text??"";for(int size=Mathf.Max(12,source.fontSize);size>=12;size--){s.fontSize=size;if(s.CalcHeight(new GUIContent(text),bounds.width)<=bounds.height)return new Fitted{style=s,text=text};}s.fontSize=12;while(text.Length>1&&s.CalcHeight(new GUIContent(text+"…"),bounds.width)>bounds.height)text=text.Substring(0,text.Length-1);return new Fitted{style=s,text=text+"…"};}
}
