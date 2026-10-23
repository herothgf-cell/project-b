using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public partial class RoadMapEditor {
 static readonly string[] SceneKinds={"","building","road","sidewalk","parking","restaurant","rest","water","land"};
 static readonly string[] SceneNames={"전체","건물","도로 노면","보도","주차장","식당 부지","휴식 부지","물","일반 부지"};
 ProjectMapScene capturedScene;MapData capturedBaseline;
 string sceneFilter="",sceneSearch="",sceneCreateKind="building";bool sceneVertices,dragScene,sceneMoved;
 int sceneVertex=-1;Vector3 sceneDragPosition;RoadMapDocument sceneBefore;Vector2 sceneListScroll;
 ProjectMapScene SceneView {get{if(document.scene!=null)return document.scene;if(capturedScene==null||capturedBaseline!=RoadMapRuntime.Baseline){capturedBaseline=RoadMapRuntime.Baseline;capturedScene=ProjectMapScene.Capture(capturedBaseline);}return capturedScene;}}
 public ProjectMapObject SceneObject {get{return SceneView.objects.FirstOrDefault(o=>o.id==selected);}}
 static string SceneName(string kind){int i=Array.IndexOf(SceneKinds,kind);return i<0?kind:SceneNames[i];}
 static bool SceneService(ProjectMapObject o){return o.kind=="parking"||o.kind=="restaurant"||o.kind=="rest";}
 static Vector3 SceneCentre(ProjectMapObject o){return o.points==null||o.points.Length==0?Vector3.zero:new Vector3(o.points.Average(p=>p.x),0,o.points.Average(p=>p.z));}
 void EnsureScene(){if(document.scene==null)document.scene=JsonUtility.FromJson<ProjectMapScene>(JsonUtility.ToJson(SceneView));}
 ProjectMapObject WritableSceneObject(){EnsureScene();return SceneObject;}
 void SceneEdit(Action<ProjectMapObject> action){if(SceneObject==null)return;Edit(()=>{EnsureScene();action(SceneObject);});numbers.Clear();}
 public void SelectSceneObject(string id){tool="건물·부지";Select(id);easyAction="select";sceneVertex=-1;show3D=false;}
 public ProjectMapObject CreateSceneObject(string kind,Vector3 position){
  if(!SceneKinds.Skip(1).Contains(kind))throw new ArgumentException("Unknown scene kind");
  ProjectMapObject created=null;
  Edit(()=>{EnsureScene();float half=kind=="road"?15:kind=="building"?6:10;
   float x=Mathf.Clamp(position.x,half,RoadMapRuntime.Baseline.width-half),z=Mathf.Clamp(position.z,half,RoadMapRuntime.Baseline.depth-half);
   created=new ProjectMapObject{id="project:"+Guid.NewGuid().ToString("N"),kind=kind,name="새 "+SceneName(kind),width=7,height=kind=="building"?8:kind=="sidewalk"?.14f:0,lanes=2,enabled=true,serviceX=x,serviceZ=z,
    points=kind=="road"?new[]{new MapPoint{x=x,z=z-half},new MapPoint{x=x,z=z+half}}:new[]{new MapPoint{x=x-half,z=z-half},new MapPoint{x=x+half,z=z-half},new MapPoint{x=x+half,z=z+half},new MapPoint{x=x-half,z=z+half}}};
   document.scene.objects=document.scene.objects.Concat(new[]{created}).ToArray();selected=created.id;
  });
  SelectSceneObject(created.id);sceneFilter=kind;sceneSearch="";notice="새 "+SceneName(kind)+" 생성 · 드래그로 이동하고 속성을 조정하세요.";return created;
 }
 static void TransformScene(ProjectMapObject o,Vector3 delta,float yaw,float scale){
  var centre=SceneCentre(o);var rotation=Quaternion.Euler(0,yaw,0);
  foreach(var p in o.points){var v=centre+delta+rotation*(p.V()-centre)*scale;p.x=v.x;p.z=v.z;}
  if(SceneService(o)){var p=centre+delta+rotation*(new Vector3(o.serviceX,0,o.serviceZ)-centre)*scale;o.serviceX=p.x;o.serviceZ=p.z;}
  o.yaw=Mathf.Repeat(o.yaw+yaw,360);if(o.kind=="road")o.width*=scale;
 }
 public void TransformSceneObject(Vector3 delta,float yaw,float scale){if(scale<=0||float.IsNaN(scale)||float.IsInfinity(scale))return;SceneEdit(o=>TransformScene(o,delta,yaw,scale));}
 public void DeleteSceneObject(){var o=SceneObject;if(o==null)return;Edit(()=>{EnsureScene();document.scene.objects=document.scene.objects.Where(x=>x.id!=o.id).ToArray();selected="";});sceneVertex=-1;}
 public void MoveSceneVertex(int index,Vector3 position){if(SceneObject==null||index<0||index>=SceneObject.points.Length)return;SceneEdit(o=>{o.points[index]=ScenePoint(position);});sceneVertex=index;}
 MapPoint ScenePoint(Vector3 p){return new MapPoint{x=Mathf.Clamp(p.x,0,RoadMapRuntime.Baseline.width),z=Mathf.Clamp(p.z,0,RoadMapRuntime.Baseline.depth)};}
 public void AddSceneVertex(){var o=SceneObject;if(o==null)return;int at=Mathf.Clamp(sceneVertex,0,o.points.Length-1);if(o.kind=="road")at=Mathf.Min(at,o.points.Length-2);SceneEdit(x=>{var points=x.points.ToList();var p=(points[at].V()+points[(at+1)%points.Count].V())*.5f;points.Insert(at+1,ScenePoint(p));x.points=points.ToArray();});sceneVertex=at+1;}
 public void RemoveSceneVertex(){var o=SceneObject;if(o==null||o.points.Length<=(o.kind=="road"?2:3)||sceneVertex<0)return;int at=sceneVertex;SceneEdit(x=>x.points=x.points.Where((p,i)=>i!=at).ToArray());sceneVertex=Mathf.Min(at,SceneObject.points.Length-1);}
 public void SetSceneServicePosition(Vector3 position){if(SceneObject==null||!SceneService(SceneObject))return;SceneEdit(o=>{var p=ScenePoint(position);o.serviceX=p.x;o.serviceZ=p.z;});easyAction="select";}
 public bool BindSceneApproach(Vector3 position){
  if(SceneObject==null||!SceneService(SceneObject))return false;
  var lane=RoadMapEasySelection.PickLane(document,position,Mathf.Max(3,Span/950*14));if(lane==null){notice="접근할 차로 선을 클릭하세요.";return false;}
  float best=float.MaxValue,travel=0,at=0;Vector3 heading=Vector3.forward;
  for(int i=1;i<lane.points.Length;i++){var a=lane.points[i-1].V();var d=lane.points[i].V()-a;float t=Mathf.Clamp01(Vector3.Dot(position-a,d)/Mathf.Max(.0001f,d.sqrMagnitude));float distance=(position-a-d*t).sqrMagnitude;if(distance<best){best=distance;at=travel+d.magnitude*t;heading=d.normalized;}travel+=d.magnitude;}
  SceneEdit(o=>{o.laneId=lane.id;o.laneAt=at;o.yaw=Quaternion.LookRotation(heading).eulerAngles.y;});easyAction="select";notice="서비스 접근 차로 연결 완료 · 주차 위치와 방향을 확인하세요.";return true;
 }
 void DrawSceneLeft(Rect rect){
  Fill(rect,new Color(.065f,.095f,.13f));GUI.BeginGroup(rect);
  EasyLabel(new Rect(10,8,210,28),"건물·부지 / 도로 노면",17);sceneSearch=GUI.TextField(new Rect(10,42,210,32),sceneSearch,field);
  for(int i=0;i<SceneKinds.Length;i++){int n=i;if(EasyButton(new Rect(8+(i%3)*73,82+(i/3)*37,69,33),(sceneFilter==SceneKinds[i]?"● ":"")+SceneNames[i])){sceneFilter=SceneKinds[n];if(n>0)sceneCreateKind=sceneFilter;sceneListScroll=Vector2.zero;}}
  var items=SceneView.objects.Where(o=>(sceneFilter==""||o.kind==sceneFilter)&&(string.IsNullOrWhiteSpace(sceneSearch)||((o.name??"")+" "+o.id).IndexOf(sceneSearch,StringComparison.OrdinalIgnoreCase)>=0)).ToArray();
  EasyLabel(new Rect(10,199,210,28),items.Length+"개 · 지도에서도 선택",14);
  sceneListScroll=GUI.BeginScrollView(new Rect(8,231,214,355),sceneListScroll,new Rect(0,0,192,Mathf.Max(340,items.Length*48)));
  int first=Mathf.Max(0,Mathf.FloorToInt(sceneListScroll.y/48)-1),last=Mathf.Min(items.Length,first+11);
  for(int i=first;i<last;i++){var o=items[i];if(EasyButton(new Rect(0,i*48,190,44),(o.id==selected?"● ":"")+SceneName(o.kind)+" · "+(string.IsNullOrEmpty(o.name)?Short(o.id):o.name))){SelectSceneObject(o.id);var p=SceneCentre(o);Centre=new Vector2(p.x,p.z);}}
  GUI.EndScrollView();
  if(EasyButton(new Rect(10,597,210,42),SceneName(sceneCreateKind)+" 새로 놓기")){easyAction="sceneCreate";show3D=false;}
  if(EasyButton(new Rect(10,647,102,36),"내 위치")){var p=session.taxi.transform.position;Centre=new Vector2(p.x,p.z);Span=180;}
  if(EasyButton(new Rect(118,647,102,36),"전체 보기")){Centre=new Vector2(RoadMapRuntime.Baseline.width/2,RoadMapRuntime.Baseline.depth/2);Span=Mathf.Max(RoadMapRuntime.Baseline.width,RoadMapRuntime.Baseline.depth*950/600)*1.05f;}
  EasyLabel(new Rect(10,691,210,40),"종류 선택 → 새로 놓기 → 지도 클릭",13);GUI.EndGroup();
 }
 void DrawSceneProperties(){
  Caption("객체 속성");var o=SceneObject;
  if(o==null){Caption("왼쪽 목록이나 지도에서 선택하세요. 종류를 고른 뒤 새로 놓기로 추가합니다.");return;}
  Caption(SceneName(o.kind)+" · "+Short(o.id));Text("이름",o.name,v=>WritableSceneObject().name=v);Toggle("표시 / 사용",o.enabled,v=>WritableSceneObject().enabled=v);
  Caption("도형을 끌어서 이동 · 회전 / 크기");
  ActionButton("왼쪽으로 15° 회전",()=>TransformSceneObject(Vector3.zero,-15,1));ActionButton("오른쪽으로 15° 회전",()=>TransformSceneObject(Vector3.zero,15,1));
  ActionButton("크기 10% 확대",()=>TransformSceneObject(Vector3.zero,0,1.1f));ActionButton("크기 10% 축소",()=>TransformSceneObject(Vector3.zero,0,1/1.1f));
  var centre=SceneCentre(o);Number("중심 X",centre.x,v=>TransformScene(WritableSceneObject(),new Vector3(v-centre.x,0,0),0,1),0,RoadMapRuntime.Baseline.width);Number("중심 Z",centre.z,v=>TransformScene(WritableSceneObject(),new Vector3(0,0,v-centre.z),0,1),0,RoadMapRuntime.Baseline.depth);
  if(o.kind=="building")Number("건물 높이 m",o.height,v=>WritableSceneObject().height=v,.2f,300);if(o.kind=="sidewalk")Number("보도 높이 m",o.height,v=>WritableSceneObject().height=v,0,1);
  if(o.kind=="road"){Number("노면 폭 m",o.width,v=>WritableSceneObject().width=v,.5f,80);Number("표시 차로 수",o.lanes,v=>WritableSceneObject().lanes=(int)v,1,12);Toggle("일방통행 표시",o.oneWay,v=>WritableSceneObject().oneWay=v);Toggle("자동 노면선 숨김",o.suppressPaint,v=>WritableSceneObject().suppressPaint=v);Caption("차량 경로는 차로 탭에서 별도로 수정합니다.");}
  ActionButton(sceneVertices?"✓ 꼭짓점 편집 켜짐":"꼭짓점 편집",()=>{sceneVertices=!sceneVertices;sceneVertex=-1;});
  if(sceneVertices){Caption("점 선택 후 드래그 · 변 가운데 점 추가");ActionButton("선택점 다음 변에 점 추가",AddSceneVertex);if(sceneVertex>=0&&sceneVertex<o.points.Length){int index=sceneVertex;var p=o.points[index];Number("선택점 X",p.x,v=>WritableSceneObject().points[index].x=v,0,RoadMapRuntime.Baseline.width);Number("선택점 Z",p.z,v=>WritableSceneObject().points[index].z=v,0,RoadMapRuntime.Baseline.depth);ActionButton("선택 꼭짓점 삭제",RemoveSceneVertex);}}
  if(SceneService(o)){Caption("서비스 종류");foreach(var kind in new[]{"parking","restaurant","rest"}){string k=kind;ActionButton((o.kind==k?"● ":"")+SceneName(k),()=>SceneEdit(x=>x.kind=k));}Caption("서비스 위치 / 접근");ActionButton("부지 안 주차 위치를 지도에서 지정",()=>{easyAction="sceneService";show3D=false;});Number("주차 방향 °",o.yaw,v=>WritableSceneObject().yaw=v,-360,360);ActionButton("접근 차로를 지도에서 지정",()=>{easyAction="sceneLane";show3D=false;});Caption("접근: "+ReadableLane(o.laneId));Number("접근 차로 위 거리 m",o.laneAt,v=>WritableSceneObject().laneAt=v,0,100000);Caption("주차점은 부지 안에 있어야 합니다. 검사 후 저장하고 시험하세요.");}
  ActionButton("선택 객체 삭제 · 취소로 복원",DeleteSceneObject);
 }
 float SceneDistance(ProjectMapObject o,Vector3 p){if(o.points==null||o.points.Length<2)return float.MaxValue;if(o.kind!="road")return RoadMapRules.Polygon(o.points,p)?0:float.MaxValue;float best=float.MaxValue;for(int i=1;i<o.points.Length;i++){var a=o.points[i-1].V();var d=o.points[i].V()-a;float t=Mathf.Clamp01(Vector3.Dot(p-a,d)/Mathf.Max(.0001f,d.sqrMagnitude));best=Mathf.Min(best,Vector3.Distance(p,a+d*t));}return best<=Mathf.Max(o.width*.5f,Span/950*7)?best:float.MaxValue;}
 void SceneMapClick(Vector3 position){
  if(easyAction=="sceneCreate"){CreateSceneObject(sceneCreateKind,position);return;}
  if(easyAction=="sceneService"){SetSceneServicePosition(position);return;}
  if(easyAction=="sceneLane"){BindSceneApproach(position);return;}
  sceneVertex=-1;var o=SceneObject;
  if(sceneVertices&&o!=null){float best=Span/950*12;for(int i=0;i<o.points.Length;i++){float d=Vector3.Distance(position,o.points[i].V());if(d<best){best=d;sceneVertex=i;}}}
  if(sceneVertex<0){o=SceneView.objects.Where(x=>(sceneFilter==""||x.kind==sceneFilter)&&SceneDistance(x,position)<float.MaxValue).OrderBy(x=>x.kind=="road"?1:0).ThenBy(x=>(x.points.Max(p=>p.x)-x.points.Min(p=>p.x))*(x.points.Max(p=>p.z)-x.points.Min(p=>p.z))).FirstOrDefault();if(o==null){Select("");return;}Select(o.id);}
  SelectedPoint=sceneVertex;dragScene=true;sceneMoved=false;sceneBefore=document.Copy();sceneDragPosition=position;
 }
 void DragScene(Vector3 position){
  if(!dragScene||SceneObject==null)return;if((position-sceneDragPosition).sqrMagnitude<.000001f)return;
  EnsureScene();var o=SceneObject;
  if(sceneVertex>=0)o.points[sceneVertex]=ScenePoint(position);
  else{var delta=position-sceneDragPosition;delta.x=Mathf.Clamp(delta.x,-o.points.Min(p=>p.x),RoadMapRuntime.Baseline.width-o.points.Max(p=>p.x));delta.z=Mathf.Clamp(delta.z,-o.points.Min(p=>p.z),RoadMapRuntime.Baseline.depth-o.points.Max(p=>p.z));TransformScene(o,delta,0,1);}
  sceneDragPosition=position;sceneMoved=true;numbers.Clear();
 }
 void EndSceneDrag(){if(dragScene&&sceneMoved&&sceneBefore!=null){history.Commit(sceneBefore);document.revision++;issues.Clear();notice="장면 위치 변경 · 미저장";if(preview)preview.Clear();}dragScene=false;sceneMoved=false;sceneBefore=null;}
 public static Color SceneColor(string kind){switch(kind){case "building":return new Color(.58f,.52f,.43f);case "road":return new Color(.27f,.3f,.33f);case "sidewalk":return new Color(.52f,.51f,.47f);case "parking":return new Color(.3f,.44f,.5f);case "restaurant":return new Color(.6f,.36f,.24f);case "rest":return new Color(.43f,.35f,.58f);case "water":return new Color(.13f,.37f,.55f);default:return new Color(.25f,.36f,.24f);}}
 void DrawSceneRaster(){
  if(!rasterizing)return;
  foreach(var o in SceneView.objects.Where(x=>x.enabled).OrderBy(x=>x.kind=="building"?3:x.kind=="road"?2:0)){if(o.points==null||o.points.Length<2)continue;var color=SceneColor(o.kind);if(o.kind=="road")DrawPath(o.points,color,Mathf.Max(2,o.width*canvasScale));else{var pts=o.points.Select(Project).ToArray();int low=Mathf.Max(0,Mathf.CeilToInt(pts.Min(p=>p.y))),high=Mathf.Min(599,Mathf.FloorToInt(pts.Max(p=>p.y)));if(pts.Max(p=>p.x)<0||pts.Min(p=>p.x)>950)continue;var intersections=new List<float>();Color32 fill=color;for(int y=low;y<=high;y++){intersections.Clear();for(int i=0,j=pts.Length-1;i<pts.Length;j=i++){var a=pts[i];var b=pts[j];if((a.y>y)!=(b.y>y))intersections.Add(a.x+(y-a.y)*(b.x-a.x)/(b.y-a.y));}intersections.Sort();for(int i=1;i<intersections.Count;i+=2){int start=Mathf.Max(0,Mathf.CeilToInt(intersections[i-1])),end=Mathf.Min(949,Mathf.FloorToInt(intersections[i]));for(int x=start;x<=end;x++)mapPixels[(599-y)*950+x]=fill;}}}
  }
  var chosen=SceneObject;if(chosen!=null){DrawPath(chosen.points,Color.yellow,3);if(chosen.kind!="road"&&chosen.points.Length>2)Line(Project(chosen.points.Last()),Project(chosen.points[0]),Color.yellow,3);}
 }
 void SceneCanvasOverlay(){
  if(tool!="건물·부지")return;var o=SceneObject;
  if(o!=null){if(sceneVertices)for(int i=0;i<o.points.Length;i++){var p=Project(o.points[i]);if(canvas.Contains(p))Dot(p,i==sceneVertex?Color.red:Color.white,10);}
   if(SceneService(o)){var p=Project(new Vector3(o.serviceX,0,o.serviceZ));if(canvas.Contains(p)){Dot(p,Color.yellow,13);GUI.Label(new Rect(p.x+10,p.y,160,26),"주차 / 서비스",label);var direction=Quaternion.Euler(0,o.yaw,0)*Vector3.forward;for(int n=1;n<=5;n++)Dot(Project(new Vector3(o.serviceX,0,o.serviceZ)+direction*n),Color.yellow,3);}}
  }
  if(easyAction=="sceneCreate"&&canvas.Contains(Event.current.mousePosition)){Dot(Event.current.mousePosition,Color.yellow,18);GUI.Label(new Rect(Event.current.mousePosition.x+14,Event.current.mousePosition.y,180,28),SceneName(sceneCreateKind)+" 추가",label);}
 }
}
