using UnityEngine;
// Cached geometric icons remain legible without depending on an arrow font.
public static class NavigationArrow {
 static readonly Texture2D[] icons=new Texture2D[8];
 public static void Draw(Rect rect,NavigationMove move,Color color){
  int index=(int)move;if(index<=0||index>=icons.Length)return;
  if(!icons[index])icons[index]=Create(move);
  var old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,icons[index]);GUI.color=old;
 }
 static Texture2D Create(NavigationMove move){
  Vector2[] body,head;
  if(move==NavigationMove.Left||move==NavigationMove.Right){body=new[]{new Vector2(50,56),new Vector2(50,40),new Vector2(48,34),new Vector2(43,30),new Vector2(12,30)};head=new[]{new Vector2(27,15),new Vector2(12,30),new Vector2(27,45)};if(move==NavigationMove.Right){for(int i=0;i<body.Length;i++)body[i].x=64-body[i].x;for(int i=0;i<head.Length;i++)head[i].x=64-head[i].x;}}
  else if(move==NavigationMove.UTurn){body=new[]{new Vector2(50,56),new Vector2(50,24),new Vector2(46,14),new Vector2(34,10),new Vector2(22,14),new Vector2(17,24),new Vector2(17,42)};head=new[]{new Vector2(7,30),new Vector2(17,42),new Vector2(27,30)};}
  else if(move==NavigationMove.LaneLeft||move==NavigationMove.LaneRight){body=new[]{new Vector2(48,56),new Vector2(48,44),new Vector2(20,22),new Vector2(20,8)};head=new[]{new Vector2(8,20),new Vector2(20,8),new Vector2(32,20)};if(move==NavigationMove.LaneRight){for(int i=0;i<body.Length;i++)body[i].x=64-body[i].x;for(int i=0;i<head.Length;i++)head[i].x=64-head[i].x;}}
  else if(move==NavigationMove.Arrival){body=new[]{new Vector2(20,56),new Vector2(20,12),new Vector2(48,12),new Vector2(48,30),new Vector2(20,30)};head=new Vector2[0];}
  else{body=new[]{new Vector2(32,56),new Vector2(32,10)};head=new[]{new Vector2(18,24),new Vector2(32,10),new Vector2(46,24)};}
  var texture=new Texture2D(64,64,TextureFormat.RGBA32,false);texture.name="Navigation "+move;texture.wrapMode=TextureWrapMode.Clamp;texture.filterMode=FilterMode.Bilinear;
  for(int y=0;y<64;y++)for(int x=0;x<64;x++){var p=new Vector2(x+.5f,y+.5f);float distance=Mathf.Min(Distance(p,body),Distance(p,head));texture.SetPixel(x,63-y,new Color(1,1,1,Mathf.Clamp01(4.5f-distance)));}
  texture.Apply(false,true);return texture;
 }
 static float Distance(Vector2 p,Vector2[] line){float best=float.MaxValue;for(int i=1;i<line.Length;i++){var d=line[i]-line[i-1];float t=Mathf.Clamp01(Vector2.Dot(p-line[i-1],d)/Mathf.Max(.001f,d.sqrMagnitude));best=Mathf.Min(best,Vector2.Distance(p,line[i-1]+d*t));}return best;}
}
