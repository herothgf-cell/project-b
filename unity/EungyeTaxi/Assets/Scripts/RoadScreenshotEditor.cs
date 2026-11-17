using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class RoadScreenshotEditor {
 public sealed class Annotation {public int tool;public Color color;public string text="";public readonly List<Vector2> points=new List<Vector2>();}
 public readonly List<Annotation> Items=new List<Annotation>();
 public Texture2D Snapshot {get;private set;}
 public bool IsOpen {get{return Snapshot;}}
 public string Description {get;private set;}
 public string Status {get;private set;}="";
 public int CopyCount {get;private set;}
 public Rect Canvas {get;private set;}
 public float UiScale {get;private set;}=1;
 readonly DevRoadOverview owner;int tool,selected=-1;Color ink=Color.red;Annotation drawing;string note="수정할 내용을 입력하세요";Vector2 pin;bool copying;
 public RoadScreenshotEditor(DevRoadOverview owner){this.owner=owner;}
 public void Open(Texture2D image,string description,Vector2 marker){Close();Snapshot=image;Description=description;pin=marker;Status="펜·화살표로 표시하거나, 텍스트를 입력한 뒤 이미지 위를 클릭하세요.";}
 public void Close(){if(Snapshot)Object.Destroy(Snapshot);Snapshot=null;Items.Clear();drawing=null;selected=-1;copying=false;}
 public void Undo(){if(Items.Count>0)Items.RemoveAt(Items.Count-1);selected=-1;drawing=null;}
 static Vector2 Unit(Vector2 p,Rect r){return new Vector2(Mathf.Clamp01((p.x-r.x)/r.width),Mathf.Clamp01((p.y-r.y)/r.height));}
 static Vector2 Pixel(Vector2 p,Rect r){return new Vector2(r.x+p.x*r.width,r.y+p.y*r.height);}
 public void RequestCopy(){if(IsOpen&&!copying){copying=true;GUIUtility.keyboardControl=0;owner.StartCoroutine(CopyAfterFrame());}}
 IEnumerator CopyAfterFrame(){yield return new WaitForEndOfFrame();if(!IsOpen){copying=false;yield break;}
  int x=Mathf.RoundToInt(Canvas.x*UiScale),y=Mathf.RoundToInt(Screen.height-Canvas.yMax*UiScale),w=Mathf.RoundToInt(Canvas.width*UiScale),h=Mathf.RoundToInt(Canvas.height*UiScale);
  var image=new Texture2D(w,h,TextureFormat.RGB24,false);var prior=RenderTexture.active;
  try{RenderTexture.active=null;image.ReadPixels(new Rect(x,y,w,h),0,0);image.Apply();string error;if(RoadImageClipboard.Copy(image,Description,out error)){CopyCount++;Status="이미지 복사 완료 · 채팅에 Ctrl+V로 붙여넣으세요.";}else Status=error;}
  catch(System.Exception e){Debug.LogWarning(e);Status="이미지 복사에 실패했어요. 다시 시도해 주세요.";}
  finally{RenderTexture.active=prior;Object.Destroy(image);copying=false;}
 }
 public void Draw(float width,float height,float scale){
  if(!IsOpen)return;UiScale=scale;var button=KoreanText.Button(16);var label=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=16,wordWrap=true};label.normal.textColor=CasualUI.Ink;
  GUI.color=CasualUI.Canvas;GUI.DrawTexture(new Rect(0,0,width,height),Texture2D.whiteTexture);GUI.color=Color.white;
  GUI.Label(new Rect(18,12,350,36),"도로 캡처 · 수정 내용 표시",KoreanText.Button(20));
  bool enabled=GUI.enabled;GUI.enabled=enabled&&!copying;
  if(GUI.Button(new Rect(width-360,12,160,38),"이미지 복사",CasualUI.Action(17)))RequestCopy();
  if(GUI.Button(new Rect(width-190,12,172,38),"도로 보기로 · Esc",button)){Close();GUI.enabled=enabled;return;}
  string[] names={"펜","화살표","텍스트"};for(int i=0;i<3;i++)if(GUI.Button(new Rect(18+i*82,58,76,34),names[i],tool==i?CasualUI.Action(16):button)){tool=i;drawing=null;}
  if(GUI.Button(new Rect(270,58,90,34),"되돌리기",button))Undo();
  if(GUI.Button(new Rect(368,58,90,34),"모두 지우기",button)){Items.Clear();selected=-1;}
  var colors=new[]{Color.red,new Color(0,.5f,1),Color.yellow};for(int i=0;i<3;i++){GUI.backgroundColor=colors[i];if(GUI.Button(new Rect(470+i*40,58,34,34),ink==colors[i]?"●":"",button))ink=colors[i];}GUI.backgroundColor=Color.white;
  if(tool==2){note=GUI.TextField(new Rect(602,58,width-620,34),note,160,new GUIStyle(GUI.skin.textField){font=KoreanText.Font,fontSize=17});if(selected>=0&&selected<Items.Count&&Items[selected].tool==2)Items[selected].text=note;}
  float availableW=width-24,availableH=height-156-68,ratio=Mathf.Min(availableW/Snapshot.width,availableH/Snapshot.height);var picture=new Rect((width-Snapshot.width*ratio)/2,106,Snapshot.width*ratio,Snapshot.height*ratio);Canvas=new Rect(picture.x,picture.y,picture.width,picture.height+68);
  GUI.DrawTexture(picture,Snapshot,ScaleMode.StretchToFill,false);
  var ev=Event.current;if(!copying&&ev.type==EventType.MouseDown&&ev.button==0&&picture.Contains(ev.mousePosition)){
   var p=Unit(ev.mousePosition,picture);if(tool==2){selected=Items.FindLastIndex(a=>a.tool==2&&Vector2.Distance(Pixel(a.points[0],picture),ev.mousePosition)<36);if(selected>=0)note=Items[selected].text;else{var a=new Annotation{tool=2,color=ink,text=note};a.points.Add(p);Items.Add(a);selected=Items.Count-1;}}
   else{drawing=new Annotation{tool=tool,color=ink};drawing.points.Add(p);drawing.points.Add(p);Items.Add(drawing);selected=-1;}ev.Use();
  }
  if(!copying&&ev.type==EventType.MouseDrag&&drawing!=null){var p=Unit(ev.mousePosition,picture);if(drawing.tool==1)drawing.points[1]=p;else drawing.points.Add(p);ev.Use();}
  if(ev.type==EventType.MouseUp){drawing=null;}
  foreach(var a in Items){var start=Pixel(a.points[0],picture);if(a.tool==2){var style=new GUIStyle(label){fontSize=20,fontStyle=FontStyle.Bold};style.normal.textColor=a.color;float w=Mathf.Min(430,picture.width);var r=new Rect(Mathf.Clamp(start.x,picture.x,picture.xMax-w),Mathf.Clamp(start.y,picture.y,picture.yMax-64),w,64);GUI.color=new Color(0,0,0,.78f);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(r,a.text,style);}
   else{for(int i=1;i<a.points.Count;i++)HudGlyph.MapLine(Pixel(a.points[i-1],picture),Pixel(a.points[i],picture),a.color,4,picture);if(a.tool==1&&a.points.Count>1){var end=Pixel(a.points[1],picture);var d=(end-start).normalized;var side=new Vector2(-d.y,d.x);HudGlyph.MapLine(end,end-d*20+side*10,a.color,4,picture);HudGlyph.MapLine(end,end-d*20-side*10,a.color,4,picture);}}
  }
  if(pin.x>=0&&pin.x<=1&&pin.y>=0&&pin.y<=1){var p=Pixel(pin,picture);HudGlyph.MapLine(p-Vector2.right*11,p+Vector2.right*11,Color.red,3,picture);HudGlyph.MapLine(p-Vector2.up*11,p+Vector2.up*11,Color.red,3,picture);}
  GUI.color=Color.white;GUI.DrawTexture(new Rect(Canvas.x,picture.yMax,Canvas.width,68),Texture2D.whiteTexture);GUI.Label(new Rect(Canvas.x+8,picture.yMax+4,Canvas.width-16,64),Description,label);
  GUI.enabled=enabled;GUI.Label(new Rect(18,height-42,width-36,38),Status,label);
 }
}
