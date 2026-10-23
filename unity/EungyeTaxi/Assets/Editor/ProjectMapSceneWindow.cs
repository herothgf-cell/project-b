using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ProjectMapSceneWindow : EditorWindow {
 ProjectMapEditSession session;
 ProjectMapAuthoringRenderer renderer;
 Scene scene;
 SceneView editView;
 [SerializeField] string reopenRoot;
 float handleYaw;
 bool serviceMode, pickApproach, draftPending, recoveryConflict;
 double draftAfter;
 Camera preview;
 RenderTexture texture;
 FaceCamera[] billboards=new FaceCamera[0];
 string selected="", search="", filter="", error="";
 string[] ids=new string[0], labels=new string[0];
 RoadMapLane[] approachLanes;
 Vector2 scroll, panelScroll;
 bool vertices, follow=true;
 double nextPreview;
 int undoGroup=-1;
 public string SelectedId {get{return selected;}}
 [MenuItem("Taxi/프로젝트 지도 3D 편집")]
 public static void Open(){GetWindow<ProjectMapSceneWindow>("지도 3D 편집").Show();}
 public static ProjectMapSceneWindow OpenForTests(string root){var w=CreateInstance<ProjectMapSceneWindow>();w.LoadProject(root);return w;}
 void OnEnable(){SceneView.duringSceneGui+=SceneGUI;EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=PlayMode;EditorApplication.delayCall+=Restore;}
 void Restore(){if(!string.IsNullOrEmpty(reopenRoot)&&session==null&&!EditorApplication.isPlayingOrWillChangePlaymode)Safe(()=>LoadProject(reopenRoot));}
 void OnDisable(){EditorApplication.delayCall-=Restore;SceneView.duringSceneGui-=SceneGUI;EditorApplication.update-=Tick;EditorApplication.playModeStateChanged-=PlayMode;Release();}
 void PlayMode(PlayModeStateChange state){if(state==PlayModeStateChange.ExitingEditMode)Release();if(state==PlayModeStateChange.EnteredEditMode)EditorApplication.delayCall+=Restore;}
 void Release(){
  try{if(session!=null)session.Dispose();}catch(Exception e){Debug.LogException(e);}finally{session=null;}
  if(renderer!=null)renderer.Dispose();renderer=null;
  if(texture){texture.Release();DestroyImmediate(texture);}texture=null;
  if(editView){SetCustomScene(editView,default(Scene));if(Application.isBatchMode)DestroyImmediate(editView);else editView.Close();}editView=null;
  if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.ClosePreviewScene(scene);
  preview=null;selected="";draftPending=false;pickApproach=false;
 }
 void LoadProject(string root){
  Release();session=ProjectMapEditSession.Open(root);
  scene=EditorSceneManager.NewPreviewScene();scene.name="지도 편집 미리보기 (저장하지 않음)";
  try{
   renderer=new ProjectMapAuthoringRenderer();renderer.Initialize(session.InitialMap);SceneManager.MoveGameObjectToScene(renderer.Root.gameObject,scene);
   renderer.Root.gameObject.hideFlags=HideFlags.DontSave;
   var cameraObject=new GameObject("Game camera preview");SceneManager.MoveGameObjectToScene(cameraObject,scene);preview=cameraObject.AddComponent<Camera>();preview.scene=scene;preview.enabled=false;preview.fieldOfView=62;preview.nearClipPlane=.08f;preview.farClipPlane=700;preview.cullingMask=1<<29;preview.backgroundColor=new Color(.48f,.65f,.8f);preview.clearFlags=CameraClearFlags.Skybox;
   var sun=new GameObject("Game sunlight").AddComponent<Light>();SceneManager.MoveGameObjectToScene(sun.gameObject,scene);sun.type=LightType.Directional;sun.intensity=1.15f;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(45,-35,0);sun.cullingMask=1<<29;
   texture=new RenderTexture(800,450,24);texture.Create();preview.targetTexture=texture;
   SetLayers();billboards=renderer.Root.GetComponentsInChildren<FaceCamera>();var list=session.Objects.ToArray();ids=list.Select(o=>o.id).ToArray();labels=list.Select(o=>KindName(o.kind)+" · "+(string.IsNullOrEmpty(o.name)?o.id:o.name)).ToArray();
   approachLanes=session.InitialMap.editorDocument.lanes;
   session.Changed+=Changed;
   recoveryConflict=false;if(session.HasDraft)try{session.RecoverDraft();}catch(IOException e){recoveryConflict=true;error=e.Message;}
   reopenRoot=root;minSize=new Vector2(380,500);if(!Application.isBatchMode){EnsureSceneView();var first=list.FirstOrDefault(o=>o.enabled&&o.kind=="restaurant")??list.FirstOrDefault(o=>o.enabled&&o.kind=="building");if(first!=null){SelectObject(first.id);Focus();}}
  }catch{Release();throw;}
 }
  // Unity 2022.3 exposes isolated SceneView rendering internally. Keep this version
 // adapter in one place and fail explicitly rather than replacing the user's scene.
 static void SetCustomScene(SceneView view,Scene value){var property=typeof(SceneView).GetProperty("customScene",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);if(property==null)throw new NotSupportedException("이 Unity 버전은 독립 지도 Scene 미리보기를 지원하지 않습니다. 2022.3.62f2를 사용하세요.");property.SetValue(view,value,null);
  // SceneView inherits the main-stage mask on creation. A custom preview scene
  // needs the same zero override used by Unity's PreviewSceneStage, otherwise
  // meshes are culled while Handles still draw the selection outline.
  var mask=typeof(SceneView).GetProperty("overrideSceneCullingMask",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
  if(mask==null)throw new NotSupportedException("이 Unity 버전은 독립 지도 Scene 표시 필터를 지원하지 않습니다.");
  mask.SetValue(view,0UL,null);}
  public void EnsureSceneView(){if(editView)return;editView=CreateInstance<SceneView>();editView.titleContent=new GUIContent("지도 3D Scene");SetCustomScene(editView,scene);if(!Application.isBatchMode)editView.Show();}
 public void ArchiveConflictingDraft(){if(session==null||!session.HasDraft)return;string path=session.DraftPath;File.Move(path,path+".preserved-"+Guid.NewGuid().ToString("N"));recoveryConflict=false;error="";}
 static string KindName(string value){switch(value){case "building":return "건물";case "road":return "도로 노면";case "sidewalk":return "보도";case "parking":return "주차장";case "restaurant":return "식당 부지";case "rest":return "휴식 부지";case "water":return "물";case "land":return "녹지";default:return value;}}
 void SetLayers(){if(renderer!=null&&renderer.Root)foreach(var t in renderer.Root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=29;}
 void Changed(string[] changed){foreach(string id in changed)renderer.UpdateObject(session.GetObject(id));draftPending=true;draftAfter=EditorApplication.timeSinceStartup+2;Repaint();SceneView.RepaintAll();}
 public void SelectObject(string id){if(session==null)return;session.GetObject(id);selected=id;handleYaw=0;Selection.activeObject=null;Repaint();SceneView.RepaintAll();}
 public void ApplyTransform(Vector3 delta,float yaw,Vector2 scale,float heightScale){if(recoveryConflict)throw new InvalidOperationException("충돌 초안을 먼저 보관하세요.");if(session!=null&&!string.IsNullOrEmpty(selected))session.Transform(selected,delta,yaw,scale,heightScale);}
 public void SaveProject(){if(session==null)return;session.Save();draftPending=false;error="";ShowNotification(new GUIContent("프로젝트 지도 저장 완료"));}
 public void CloseForTests(){reopenRoot=null;Release();DestroyImmediate(this);}
  public void CapturePreview(string path){
  var o=session.Objects.First(x=>x.enabled&&x.kind=="restaurant");var b=renderer.BoundsFor(o.id);preview.transform.position=o.Position-o.Direction*28+Vector3.up*20;preview.transform.LookAt(o.Position+Vector3.up*2);follow=false;nextPreview=0;Tick();
  var previous=RenderTexture.active;RenderTexture.active=texture;var pixels=new Texture2D(texture.width,texture.height,TextureFormat.RGB24,false);try{pixels.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());}finally{RenderTexture.active=previous;DestroyImmediate(pixels);}
 }
 public int FullBuildCount {get{return renderer.FullBuildCount;}}
 public int LastUpdatedChunkCount {get{return renderer.LastUpdatedChunkCount;}}
 void Safe(Action action){try{action();error="";}catch(Exception e){error=e.Message;Debug.LogException(e);}}
 void Focus(){if(renderer==null||string.IsNullOrEmpty(selected))return;var v=editView;if(v)v.Frame(renderer.BoundsFor(selected),false);}
 void OnGUI(){panelScroll=EditorGUILayout.BeginScrollView(panelScroll);try{DrawGUI();}finally{EditorGUILayout.EndScrollView();}}
 void DrawGUI(){
  EditorGUILayout.LabelField("실행 화면 기반 지도 편집",EditorStyles.boldLabel);
  if(!string.IsNullOrEmpty(error))EditorGUILayout.HelpBox(error,MessageType.Error);
  if(session==null){EditorGUILayout.HelpBox("프로젝트 지도를 열고 Scene 화면에서 객체를 선택하세요. W 이동 · E 회전 · R 크기 · F 선택으로 이동",MessageType.Info);if(GUILayout.Button("프로젝트 지도 열기",GUILayout.Height(32)))Safe(()=>LoadProject(ProjectMapStore.FindRoot(Application.dataPath)));return;}
  if(recoveryConflict){EditorGUILayout.HelpBox("기준 지도가 달라 초안을 자동 복구할 수 없습니다. 초안을 별도 파일로 보관하면 현재 프로젝트 지도를 편집할 수 있습니다.",MessageType.Warning);if(GUILayout.Button("충돌 초안 보관 후 현재 지도 편집"))Safe(ArchiveConflictingDraft);return;}
  if(!editView&&GUILayout.Button("3D Scene 다시 열기",GUILayout.Height(28)))Safe(()=>{EnsureSceneView();Focus();});
  EditorGUILayout.BeginHorizontal();if(GUILayout.Button("프로젝트에 저장",GUILayout.Height(30)))Safe(SaveProject);if(GUILayout.Button("실행취소",GUILayout.Height(30)))Undo.PerformUndo();if(GUILayout.Button("다시 실행",GUILayout.Height(30)))Undo.PerformRedo();EditorGUILayout.EndHorizontal();
  EditorGUILayout.LabelField(session.Dirty?"저장 전 변경: "+session.EditedObjectCount+"개":"프로젝트에 저장된 상태");
  EditorGUILayout.HelpBox("Scene에서 클릭 → W 이동 / E 회전 / R 크기. Alt+드래그로 둘러보기. 도로 형상 편집은 주행 경로를 자동으로 이동하지 않습니다.",MessageType.Info);
  string[] kinds={"전체","building","road","sidewalk","parking","restaurant","rest","water","land"};int index=Array.IndexOf(kinds,filter);int chosen=EditorGUILayout.Popup("선택 대상",Mathf.Max(0,index),kinds.Select(KindName).ToArray());filter=chosen==0?"":kinds[chosen];
  search=EditorGUILayout.TextField("이름 검색",search);
  if(!string.IsNullOrWhiteSpace(search)){scroll=EditorGUILayout.BeginScrollView(scroll,GUILayout.Height(100));int shown=0;for(int i=0;i<ids.Length&&shown<30;i++)if(labels[i].IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0){if(GUILayout.Button(labels[i],EditorStyles.miniButton)){SelectObject(ids[i]);Focus();}shown++;}EditorGUILayout.EndScrollView();}
  if(!string.IsNullOrEmpty(selected)){
   var o=session.GetObject(selected);var b=renderer.BoundsFor(selected);EditorGUILayout.LabelField(string.IsNullOrEmpty(o.name)?KindName(o.kind):o.name,EditorStyles.boldLabel);
   EditorGUILayout.LabelField("크기",string.Format("가로 {0:F1}m · 세로 {1:F1}m · 높이 {2:F1}m",b.size.x,b.size.z,o.height));
   EditorGUI.BeginChangeCheck();float height=EditorGUILayout.FloatField("높이 (m)",o.height);float width=o.kind=="road"?EditorGUILayout.FloatField("도로 폭 (m)",o.width):o.width;
   if(EditorGUI.EndChangeCheck())Safe(()=>{o.height=Mathf.Max(0,height);o.width=Mathf.Max(.1f,width);session.ApplyObject(o);});
   vertices=EditorGUILayout.Toggle("꼭짓점 직접 편집",vertices);   if(o.IsService){
    serviceMode=EditorGUILayout.Toggle("정차점만 이동·회전",serviceMode);
    if(GUILayout.Button(pickApproach?"Scene에서 접근 도로를 클릭하세요 (다시 누르면 취소)":"지도에서 접근 차로 지정"))pickApproach=!pickApproach;
    EditorGUI.BeginChangeCheck();Vector2 stop=EditorGUILayout.Vector2Field("정차점 X/Z (m)",new Vector2(o.serviceX,o.serviceZ));float heading=EditorGUILayout.FloatField("정차 방향 (도)",o.yaw);string lane=EditorGUILayout.TextField("접근 차로 ID",o.laneId);float at=EditorGUILayout.FloatField("차로 위 거리 (m)",o.laneAt);
    if(EditorGUI.EndChangeCheck())Safe(()=>session.SetService(selected,new Vector3(stop.x,0,stop.y),heading,lane,at));
   }else serviceMode=false;
   EditorGUILayout.BeginHorizontal();if(GUILayout.Button("선택으로 이동"))Focus();if(GUILayout.Button("운전자 높이에서 보기")){preview.transform.position=b.center+new Vector3(0,1.07f-b.extents.y,-Mathf.Max(8,b.extents.z+5));preview.transform.rotation=Quaternion.Euler(2,0,0);preview.fieldOfView=82;follow=false;}EditorGUILayout.EndHorizontal();
  }
  follow=EditorGUILayout.Toggle("Scene 시점으로 게임 화면 보기",follow);
  var rect=GUILayoutUtility.GetAspectRect(16f/9f);if(texture)GUI.DrawTexture(rect,texture,ScaleMode.ScaleToFit,false);
  EditorGUILayout.LabelField("게임 카메라 FOV 62° · 거리 700m / 차량·승객·HUD 제외",EditorStyles.miniLabel);
  if(!string.IsNullOrEmpty(error))EditorGUILayout.HelpBox(error,MessageType.Error);
 }
 void SceneGUI(SceneView view){
  if(session==null||renderer==null||view!=editView||recoveryConflict)return;var e=Event.current;
  if(e.type==EventType.KeyDown&&e.keyCode==KeyCode.F&&!string.IsNullOrEmpty(selected)){Focus();e.Use();}
  if(e.type==EventType.MouseDown&&e.button==0&&!e.alt){Undo.IncrementCurrentGroup();undoGroup=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("지도 객체 조작");}
  if(!string.IsNullOrEmpty(selected)){
   var o=session.GetObject(selected);var bounds=renderer.BoundsFor(selected);var pivot=ProjectMapScene.Center(o.points);pivot.y=bounds.center.y;Handles.color=Color.yellow;Handles.DrawWireCube(bounds.center,bounds.size);
   EditorGUI.BeginChangeCheck();Vector3 delta=Vector3.zero;float yaw=0,heightScale=1;Vector2 scale=Vector2.one;
   if(serviceMode&&o.IsService){EditorGUI.BeginChangeCheck();var point=Handles.PositionHandle(o.Position,Quaternion.identity);var q=Handles.Disc(Quaternion.Euler(0,o.yaw,0),o.Position,Vector3.up,HandleUtility.GetHandleSize(o.Position),false,15);if(EditorGUI.EndChangeCheck())Safe(()=>session.SetService(selected,new Vector3(point.x,0,point.z),q.eulerAngles.y,o.laneId,o.laneAt));}
   else if(vertices){for(int i=0;i<o.points.Length;i++){EditorGUI.BeginChangeCheck();var p=Handles.PositionHandle(o.points[i].V(),Quaternion.identity);if(EditorGUI.EndChangeCheck()){int vertex=i;Safe(()=>session.SetVertex(selected,vertex,p));}}}
   else if(Tools.current==Tool.Rotate){var q=Handles.Disc(Quaternion.Euler(0,handleYaw,0),pivot,Vector3.up,HandleUtility.GetHandleSize(pivot),false,15);yaw=Mathf.DeltaAngle(handleYaw,q.eulerAngles.y);handleYaw=q.eulerAngles.y;}
   else if(Tools.current==Tool.Scale){var initial=new Vector3(Mathf.Max(.1f,bounds.size.x),Mathf.Max(.1f,o.height),Mathf.Max(.1f,bounds.size.z));var s=Handles.ScaleHandle(initial,pivot,Quaternion.identity,HandleUtility.GetHandleSize(bounds.center));scale=new Vector2(Mathf.Max(.01f,s.x)/initial.x,Mathf.Max(.01f,s.z)/initial.z);heightScale=Mathf.Max(.01f,s.y)/initial.y;}
   else {delta=Handles.PositionHandle(pivot,Quaternion.identity)-pivot;delta.y=0;}
   if(EditorGUI.EndChangeCheck()&&!vertices&&!serviceMode)Safe(()=>ApplyTransform(delta,yaw,scale,heightScale));
  }
  int control=GUIUtility.GetControlID(FocusType.Passive);if(e.type==EventType.Layout&&!e.alt)HandleUtility.AddDefaultControl(control);
  if(e.type==EventType.MouseDown&&e.button==0&&!e.alt&&GUIUtility.hotControl==0&&HandleUtility.nearestControl==control){var ray=HandleUtility.GUIPointToWorldRay(e.mousePosition);if(pickApproach){Safe(()=>AssignApproach(ray));e.Use();return;}string id=renderer.Pick(ray,filter);if(!string.IsNullOrEmpty(id))SelectObject(id);e.Use();}
  if(e.rawType==EventType.MouseUp&&undoGroup>=0){Undo.CollapseUndoOperations(undoGroup);undoGroup=-1;Safe(()=>session.WriteDraft());}
 }
  void AssignApproach(Ray ray){
  float distance;if(!new Plane(Vector3.up,Vector3.zero).Raycast(ray,out distance))return;var point=ray.GetPoint(distance);float best=20,at=0;string lane=null;
  foreach(var edge in approachLanes??new RoadMapLane[0]){if(edge.disabled||edge.points==null)continue;float along=0;for(int i=1;i<edge.points.Length;i++){var a=edge.points[i-1].V();var b=edge.points[i].V();var delta=b-a;float length=delta.magnitude;if(length<.001f)continue;float t=Mathf.Clamp01(Vector3.Dot(point-a,delta)/delta.sqrMagnitude);float d=Vector3.Distance(point,a+delta*t);if(d<best){best=d;lane=edge.id;at=along+length*t;}along+=length;}}
  if(lane==null)throw new InvalidOperationException("주행 차로 가까이를 클릭하세요 (20m 이내).");var o=session.GetObject(selected);session.SetService(selected,o.Position,o.yaw,lane,at);pickApproach=false;
 }
 void Tick(){if(draftPending&&GUIUtility.hotControl==0&&EditorApplication.timeSinceStartup>draftAfter){draftPending=false;Safe(()=>session.WriteDraft());}if(!preview||EditorApplication.timeSinceStartup<nextPreview)return;nextPreview=EditorApplication.timeSinceStartup+.1;
  if(follow&&editView){var c=editView.camera;preview.transform.SetPositionAndRotation(c.transform.position,c.transform.rotation);preview.fieldOfView=62;}
  var ambient=RenderSettings.ambientLight;var mode=RenderSettings.ambientMode;try{RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.65f,.69f,.75f);foreach(var label in billboards)if(label)label.transform.rotation=preview.transform.rotation;preview.Render();}finally{RenderSettings.ambientLight=ambient;RenderSettings.ambientMode=mode;}Repaint();
 }
}
