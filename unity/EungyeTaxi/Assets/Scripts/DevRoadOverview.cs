using UnityEngine;

// X/Y in this model are world X/Z. North stays at the top of the viewport.
public sealed class DevRoadOverviewView {
 public Rect Bounds {get;private set;}
 public Vector2 Center {get;private set;}
 public float Zoom {get;private set;}=1;
 public void Configure(Rect bounds){Bounds=bounds;Reset();}
 public void Reset(){Center=Bounds.center;Zoom=1;}
 public float MetresPerPixel(Rect viewport){return Mathf.Max(Bounds.width,Bounds.height)/(Mathf.Max(1,Mathf.Min(viewport.width,viewport.height))*Zoom);}
 public Vector2 WorldAt(Vector2 pixel,Rect viewport){var d=(pixel-viewport.center)*MetresPerPixel(viewport);return Center+new Vector2(d.x,-d.y);}
 public void ZoomAt(int step,Vector2 pixel,Rect viewport){var before=WorldAt(pixel,viewport);Zoom=Mathf.Clamp(Zoom*Mathf.Pow(1.5f,step),1,64);Center+=before-WorldAt(pixel,viewport);Clamp();}
 public void Pan(Vector2 delta,Rect viewport){Center+=new Vector2(-delta.x,delta.y)*MetresPerPixel(viewport);Clamp();}
 public void Focus(Vector3 point){Center=new Vector2(point.x,point.z);Zoom=Mathf.Max(Zoom,8);Clamp();}
 void Clamp(){Center=new Vector2(Mathf.Clamp(Center.x,Bounds.xMin,Bounds.xMax),Mathf.Clamp(Center.y,Bounds.yMin,Bounds.yMax));}
}

public sealed class DevRoadOverview : MonoBehaviour {
 public bool IsOpen {get;private set;}
 public readonly DevRoadOverviewView View=new DevRoadOverviewView();
 public RenderTexture Texture {get{return texture;}}
 public int RenderCount {get;private set;}
 DriveSession session;Camera overhead;RenderTexture texture;bool dragging,dirty;float nextRender;
 public void Open(DriveSession owner){session=owner;var map=owner.world.map;
  float minX=0,minZ=0,maxX=map.width,maxZ=map.depth;
  foreach(var road in map.roads)foreach(var p in road.points){float half=road.width*.5f;minX=Mathf.Min(minX,p.x-half);minZ=Mathf.Min(minZ,p.z-half);maxX=Mathf.Max(maxX,p.x+half);maxZ=Mathf.Max(maxZ,p.z+half);}
  View.Configure(Rect.MinMaxRect(minX,minZ,maxX,maxZ));IsOpen=true;dirty=true;dragging=false;
 }
 public void Close(){IsOpen=false;dragging=false;Release();}
 void Release(){if(overhead){overhead.targetTexture=null;Destroy(overhead.gameObject);overhead=null;}if(texture){texture.Release();Destroy(texture);texture=null;}}
 void OnDisable(){Close();}
 void OnDestroy(){Release();}
 public void Render(Rect viewport){
  if(!IsOpen||!session)return;
  int w=Mathf.Clamp(Mathf.RoundToInt(viewport.width),64,1920),h=Mathf.Clamp(Mathf.RoundToInt(viewport.height),64,1200);
  if(!overhead){var go=new GameObject("Developer road overview camera");go.transform.SetParent(transform,false);overhead=go.AddComponent<Camera>();if(Camera.main)overhead.CopyFrom(Camera.main);overhead.enabled=false;overhead.rect=new Rect(0,0,1,1);overhead.orthographic=true;overhead.nearClipPlane=.1f;overhead.farClipPlane=2000;overhead.allowHDR=false;overhead.allowMSAA=false;}
  if(!texture||texture.width!=w||texture.height!=h){if(texture){overhead.targetTexture=null;texture.Release();Destroy(texture);}texture=new RenderTexture(w,h,24,RenderTextureFormat.ARGB32){name="Developer road overview",filterMode=FilterMode.Bilinear};texture.Create();dirty=true;}
  if(!dirty&&Time.unscaledTime<nextRender)return;
  overhead.targetTexture=texture;overhead.aspect=viewport.width/Mathf.Max(1,viewport.height);overhead.orthographicSize=viewport.height*View.MetresPerPixel(viewport)*.5f;
  overhead.transform.SetPositionAndRotation(new Vector3(View.Center.x,600,View.Center.y),Quaternion.Euler(90,0,0));
  var prior=RenderTexture.active;try{overhead.Render();}finally{RenderTexture.active=prior;}
  nextRender=Time.unscaledTime+.25f;dirty=false;RenderCount++;
 }
 public void Draw(){
  if(!IsOpen)return;
  float scale=Mathf.Min(1,Mathf.Min(Screen.width/1000f,Screen.height/600f)),width=Screen.width/scale,height=Screen.height/scale;
  var old=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
  try{
   GUI.depth=-750;var text=KoreanText.Button(16);var heading=KoreanText.Button(20);
   GUI.color=CasualUI.Canvas;GUI.DrawTexture(new Rect(0,0,width,height),Texture2D.whiteTexture);GUI.color=Color.white;
   GUI.Label(new Rect(18,12,300,36),"개발 기능 · 도로 전체 보기 · 북 ↑",heading);
   if(GUI.Button(new Rect(width-590,12,120,38),"전체 보기",text)){View.Reset();dirty=true;}
   if(GUI.Button(new Rect(width-460,12,120,38),"내 위치",text)){View.Focus(session.taxi.transform.position);dirty=true;}
   if(GUI.Button(new Rect(width-330,12,46,38),"+",text)){View.ZoomAt(1,new Vector2(width/2,height/2),new Rect(12,62,width-24,height-104));dirty=true;}
   if(GUI.Button(new Rect(width-276,12,46,38),"−",text)){View.ZoomAt(-1,new Vector2(width/2,height/2),new Rect(12,62,width-24,height-104));dirty=true;}
   if(GUI.Button(new Rect(width-218,12,200,38),"설정으로 돌아가기 · Esc",text)){Close();return;}
   var viewport=new Rect(12,62,width-24,height-104);var e=Event.current;
   if(e.type==EventType.ScrollWheel&&viewport.Contains(e.mousePosition)){View.ZoomAt(e.delta.y<0?1:-1,e.mousePosition,viewport);dirty=true;e.Use();}
   if(e.type==EventType.MouseDown&&e.button==0&&viewport.Contains(e.mousePosition)){dragging=true;e.Use();}
   if(e.type==EventType.MouseDrag&&dragging){View.Pan(e.delta,viewport);dirty=true;e.Use();}
   if(e.type==EventType.MouseUp&&e.button==0)dragging=false;
   if(e.type==EventType.Repaint)Render(viewport);
   if(texture)GUI.DrawTexture(viewport,texture,ScaleMode.StretchToFill,false);
   var cursor=View.WorldAt(e.mousePosition,viewport);string location=viewport.Contains(e.mousePosition)?$"커서 x={cursor.x:F1}, z={cursor.y:F1}":$"중앙 x={View.Center.x:F1}, z={View.Center.y:F1}";
   GUI.Label(new Rect(18,height-36,width-36,28),$"드래그 이동 · 휠 확대/축소 · {View.Zoom:F1}배 · {location} · 읽기 전용",text);
  }finally{GUI.matrix=old;}
 }
}
