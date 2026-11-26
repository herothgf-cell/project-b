using System;
using System.Collections.Generic;
using UnityEngine;

// 2026-11-26 (user pick: design A, CCTV shape). Fixed speed cameras on the full map and the minimap: a yellow round
// badge with a CCTV camera (Resources/UI/speed-camera-cctv.png, tools/art/draw_speed_camera_icon.py) and a small
// limit tag. Cameras closer than one icon merge into one badge with a blue count. A section-start limit disc with
// the same limit under a camera icon is not drawn again (SpeedLimitSections.DrawMap asks Covers).
// The full map's "단속 카메라" button hides them; the minimap follows the same switch.
public static class SpeedCameraMap {
 static readonly Color Ink=new Color(.10f,.11f,.13f),Ring=new Color(.725f,.125f,.13f),Count=new Color(.145f,.39f,.92f);
 static Texture2D icon;static GUIStyle tag;
 public static Texture2D Icon {get{if(!icon)icon=Resources.Load<Texture2D>("UI/speed-camera-cctv");return icon;}}
 public struct Group {public Vector2 p;public int kmh,count;}
 public static bool Available(DriveSession s){return s&&s.traffic&&s.traffic.EungyeSingle&&!s.simulationMatch&&SpeedCameraRules.Enabled&&SpeedCameraRules.Cameras.Length>0;}
 // Screen groups: same-point pairs (both directions) count once; icons that would overlap merge.
 public static List<Group> Groups(Func<Vector3,Vector2> project,float size){
  var groups=new List<Group>();var seen=new List<Vector3>();
  foreach(var c in SpeedCameraRules.Cameras){
   bool pair=false;foreach(var v in seen)if(Vector3.Distance(v,c.V())<3){pair=true;break;}if(pair)continue;seen.Add(c.V());
   var p=project(c.V());int hit=-1;for(int i=0;i<groups.Count;i++)if((groups[i].p-p).magnitude<size*.9f){hit=i;break;}
   if(hit>=0){var g=groups[hit];g.count++;groups[hit]=g;}else groups.Add(new Group{p=p,kmh=c.kmh,count=1});
  }
  return groups;
 }
 // True when a camera icon already shows this limit at this spot.
 public static bool Covers(List<Group> groups,Vector2 p,float kmh,float size){if(groups==null)return false;foreach(var g in groups)if(Mathf.Abs(g.kmh-kmh)<.5f&&(g.p-p).magnitude<size*1.1f)return true;return false;}
 public static void Draw(List<Group> groups,Rect clip,float size){
  if(Event.current==null||Event.current.type!=EventType.Repaint||groups==null)return;
  var grown=new Rect(clip.x-size,clip.y-size,clip.width+2*size,clip.height+2*size);
  foreach(var g in groups){if(!grown.Contains(g.p))continue;Badge(new Rect(g.p.x-size*.5f,g.p.y-size*.5f,size,size),g.kmh,g.count);}
 }
 public static void Badge(Rect r,int kmh,int count){
  var old=GUI.color;GUI.color=Color.white;
  if(Icon)GUI.DrawTexture(r,Icon,ScaleMode.ScaleToFit,true);else HudGlyph.Draw(r,15,new Color(1f,.8f,.1f));
  if(kmh>0){float t=r.width*.6f;var tr=new Rect(r.xMax-t*.62f,r.yMax-t*.7f,t,t);HudGlyph.Draw(tr,15,Ring);HudGlyph.Draw(new Rect(tr.x+t*.14f,tr.y+t*.14f,t*.72f,t*.72f),15,Color.white);Label(tr,kmh.ToString(),t*.5f,Ink);}
  if(count>1){float n=r.width*.5f;var nr=new Rect(r.x-n*.35f,r.y-n*.35f,n,n);HudGlyph.Draw(new Rect(nr.x-1,nr.y-1,n+2,n+2),15,Color.white);HudGlyph.Draw(nr,15,Count);Label(nr,count.ToString(),n*.62f,Color.white);}
  GUI.color=old;
 }
 static void Label(Rect r,string text,float px,Color color){
  if(tag==null)tag=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold,padding=new RectOffset(0,0,0,0),clipping=TextClipping.Overflow};
  tag.fontSize=Mathf.Max(6,Mathf.RoundToInt(px));tag.normal.textColor=color;if(KoreanText.BoldFont)tag.font=KoreanText.BoldFont;GUI.Label(r,text,tag);
 }
}
