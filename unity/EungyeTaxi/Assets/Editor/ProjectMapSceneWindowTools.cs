using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed partial class ProjectMapSceneWindow {
 ProjectMapTrafficTools trafficTools;
 int editorMode=1,createKind,objectVertex;
 bool trafficRefresh,placingObject,previewStale;
 double trafficRefreshAfter;
 string status="",trialStart="",trialGoal="";
 List<RoadMapIssue> validationIssues=new List<RoadMapIssue>();
 System.Diagnostics.Process trialProcess;
 readonly string[] createKinds={"building","road","sidewalk","parking","restaurant","rest","water","land"};
 void ShowStatus(string text){status=text;Repaint();}
 void FrameTraffic(Bounds bounds){if(editView)editView.Frame(bounds,false);}
 void QueueTrafficRefresh(){previewStale=true;trafficRefresh=true;trafficRefreshAfter=EditorApplication.timeSinceStartup+.25;draftPending=true;draftAfter=EditorApplication.timeSinceStartup+2;validationIssues.Clear();SceneView.RepaintAll();Repaint();}
 void RefreshObjectList(){var list=session.Objects.ToArray();ids=list.Select(o=>o.id).ToArray();labels=list.Select(o=>KindName(o.kind)+" · "+(string.IsNullOrEmpty(o.name)?o.id:o.name)).ToArray();}
 void RebuildTrafficPreview(){
  var map=session.Compose();EungyeConnectivityRepair.Apply(map,session.Baseline);
  var next=new ProjectMapAuthoringRenderer();try{next.Initialize(map);SceneManager.MoveGameObjectToScene(next.Root.gameObject,scene);next.Root.gameObject.hideFlags=HideFlags.DontSave;}catch{next.Dispose();throw;}
  renderer.Dispose();renderer=next;SetLayers();billboards=renderer.Root.GetComponentsInChildren<FaceCamera>();
  previewStale=false;status="3D 미리보기 갱신 완료 · 차로 규칙은 검사 후 저장하세요";SceneView.RepaintAll();
 }
 void DrawObjectCreation(){
  EditorGUILayout.BeginHorizontal();createKind=EditorGUILayout.Popup(createKind,createKinds.Select(KindName).ToArray());if(GUILayout.Button(placingObject?"배치 취소":"새로 놓기",GUILayout.Height(26)))placingObject=!placingObject;EditorGUILayout.EndHorizontal();
  if(placingObject)EditorGUILayout.HelpBox("Scene에서 새 객체를 놓을 지면을 클릭하세요. Esc로 취소합니다.",MessageType.Info);
  if(!string.IsNullOrEmpty(selected)){
   var value=session.GetObject(selected);EditorGUI.BeginChangeCheck();string name=EditorGUILayout.TextField("이름",value.name);bool enabled=EditorGUILayout.Toggle("표시 / 사용",value.enabled);if(EditorGUI.EndChangeCheck())Safe(()=>{value.name=name;value.enabled=enabled;session.ApplyObject(value);RefreshObjectList();});
   if(value.kind=="road"){EditorGUI.BeginChangeCheck();int lanes=EditorGUILayout.IntSlider("표시 차로 수",value.lanes,1,12);bool one=EditorGUILayout.Toggle("노면 일방통행 표시",value.oneWay);bool paint=EditorGUILayout.Toggle("자동 노면선 숨김",value.suppressPaint);if(EditorGUI.EndChangeCheck())Safe(()=>{value.lanes=lanes;value.oneWay=one;value.suppressPaint=paint;session.ApplyObject(value);});}
   if(vertices){objectVertex=EditorGUILayout.IntSlider("편집 꼭짓점",objectVertex,0,value.points.Length-1);EditorGUILayout.BeginHorizontal();if(GUILayout.Button("점 추가"))Safe(()=>session.AddVertex(selected,objectVertex));if(GUILayout.Button("점 삭제"))Safe(()=>session.DeleteVertex(selected,objectVertex));EditorGUILayout.EndHorizontal();}
   if(GUILayout.Button("선택 객체 삭제 (실행취소 가능)"))Safe(()=>{session.DeleteObject(selected);selected="";});
  }
 }
 bool HandleObjectCreation(SceneView view,Event e){if(!placingObject)return false;if(e.type==EventType.KeyDown&&e.keyCode==KeyCode.Escape){placingObject=false;e.Use();return true;}int control=GUIUtility.GetControlID(FocusType.Passive);if(e.type==EventType.Layout&&!e.alt)HandleUtility.AddDefaultControl(control);if(e.type==EventType.MouseDown&&e.button==0&&!e.alt&&GUIUtility.hotControl==0&&HandleUtility.nearestControl==control){var ray=HandleUtility.GUIPointToWorldRay(e.mousePosition);float distance;if(new Plane(Vector3.up,Vector3.zero).Raycast(ray,out distance))Safe(()=>{selected=session.CreateObject(createKinds[createKind],ray.GetPoint(distance));placingObject=false;RefreshObjectList();});e.Use();}return true;}
 public void CheckProject(){validationIssues=RoadMapValidator.Validate(session.Baseline,session.Document).OrderByDescending(i=>i.error).ToList();ShowStatus("검사: 오류 "+validationIssues.Count(i=>i.error)+" / 참고 "+validationIssues.Count(i=>!i.error));}
 void DrawProjectTools(){
  EditorGUILayout.HelpBox("Unity에서 편집 → 검사 → 프로젝트에 저장 → Build Eungye Hybrid. 정식 지도 JSON을 Git에 커밋·푸시하면 다른 PC의 pull/빌드에 같은 내용이 적용됩니다.",MessageType.Info);
  if(GUILayout.Button("전체 지도 검사",GUILayout.Height(30)))Safe(CheckProject);
  foreach(var issue in validationIssues.Take(60)){if(GUILayout.Button((issue.error?"수정 필요 · ":"참고 · ")+issue.message,new GUIStyle(EditorStyles.miniButton){wordWrap=true}))FrameTraffic(new Bounds(issue.position,Vector3.one*35));}
  EditorGUILayout.BeginHorizontal();if(GUILayout.Button("선택 차로를 시험 출발로"))trialStart=trafficTools.SelectedLaneId;if(GUILayout.Button("선택 차로를 시험 도착으로"))trialGoal=trafficTools.SelectedLaneId;EditorGUILayout.EndHorizontal();
  EditorGUILayout.LabelField("시험 출발",string.IsNullOrEmpty(trialStart)?"선택 차로/기본 위치":trialStart);EditorGUILayout.LabelField("시험 도착",string.IsNullOrEmpty(trialGoal)?"자유 주행":trialGoal);
  if(GUILayout.Button("현재 편집본으로 시험 주행",GUILayout.Height(30)))Safe(LaunchNativeTrial);
  EditorGUILayout.HelpBox("시험은 exe의 별도 창에서 운전하며 영업 저장과 분리됩니다. 편집 작업은 Unity 창에서 계속합니다. 최근 빌드와 기준 지도가 다르면 다시 빌드하세요.",MessageType.None);
  EditorGUILayout.BeginHorizontal();if(GUILayout.Button("JSON 내보내기"))Safe(()=>{string path=EditorUtility.SaveFilePanel("지도 편집본 내보내기",reopenRoot,"eungye-edit","json");if(!string.IsNullOrEmpty(path)){File.WriteAllText(path,session.Document.ToJson(true));ShowStatus("편집본 내보내기 완료");}});if(GUILayout.Button("JSON 가져오기"))Safe(ImportNativeDocument);EditorGUILayout.EndHorizontal();
  if(GUILayout.Button("최신 프로젝트 다시 읽기 · 내 초안 복구"))Safe(()=>{session.WriteDraft();LoadProject(reopenRoot);});
  if(GUILayout.Button("정식 저장 상태로 모든 편집 복원"))Safe(()=>{if(EditorUtility.DisplayDialog("편집 복원","현재 초안의 모든 편집을 마지막 정식 저장 상태로 되돌립니다. 실행취소할 수 있습니다.","복원","취소"))session.RestoreAll();});
  if(GUILayout.Button("프로젝트에 저장",GUILayout.Height(30)))Safe(SaveProject);
  if(GUILayout.Button("저장하고 은계 실행파일 빌드",GUILayout.Height(32)))Safe(()=>{SaveProject();PrototypeBuild.BuildHybrid();ShowStatus("빌드 완료 · builds/EungyeTaxi/EungyeTaxi.exe");});
  if(!string.IsNullOrEmpty(status))EditorGUILayout.HelpBox(status,MessageType.Info);
 }
 void ImportNativeDocument(){string path=EditorUtility.OpenFilePanel("지도 편집본 가져오기",reopenRoot,"json");if(string.IsNullOrEmpty(path))return;var text=File.ReadAllText(path);RoadMapDocument doc;try{doc=ProjectMapPackage.Parse(text).document;}catch{doc=RoadMapDocument.Parse(text);}if(doc==null||doc.baseHash!=session.Traffic.baseHash||doc.mapId!=session.Traffic.mapId)throw new InvalidDataException("기준 지도가 다릅니다. 동일한 기준 지도 JSON을 사용하세요.");
  session.ImportDocument(doc);RefreshObjectList();ShowStatus("가져오기 완료 · 검사 후 정식 저장하세요");
 }

 public void LaunchNativeTrial(){CheckProject();if(validationIssues.Any(i=>i.error))throw new InvalidOperationException("오류를 수정한 뒤 시험하세요.");if(trialProcess!=null&&!trialProcess.HasExited)throw new InvalidOperationException("이미 실행 중인 시험 창을 종료하세요.");string exe=Path.Combine(reopenRoot,"builds/EungyeTaxi/EungyeTaxi.exe");if(!File.Exists(exe))throw new FileNotFoundException("먼저 은계 실행파일을 빌드하세요.");
  string effectiveStart=string.IsNullOrEmpty(trialStart)?trafficTools.SelectedLaneId:trialStart;
  foreach(var id in new[]{effectiveStart,trialGoal})if(!string.IsNullOrEmpty(id)&&!session.Traffic.lanes.Any(l=>l.id==id&&!l.disabled))throw new InvalidOperationException("시험 출발/도착 차로가 삭제·폐쇄됐습니다. 다시 선택하세요.");
  string folder=Path.Combine(reopenRoot,"artifacts/unity-map-trial");Directory.CreateDirectory(folder);string input=Path.Combine(folder,"edit-"+Guid.NewGuid().ToString("N")+".json"),request=input+".request.json";var doc=session.Document;string stampPath=Path.Combine(Path.GetDirectoryName(exe),ProjectMapBuildStamp.FileName);if(!File.Exists(stampPath))throw new InvalidOperationException("먼저 새 편집기에서 은계 실행파일을 한 번 빌드하세요.");var stamp=JsonUtility.FromJson<ProjectMapBuildStamp>(File.ReadAllText(stampPath));if(stamp==null||stamp.baselineHash!=doc.baseHash||string.IsNullOrEmpty(stamp.contentHash))throw new InvalidOperationException("시험 실행파일의 기준 지도가 다릅니다. 다시 빌드하세요.");doc.projectRevision=stamp.contentHash;RoadMapStore.Save(input,doc);File.WriteAllText(request,JsonUtility.ToJson(new RoadMapTrialRequest{start=effectiveStart,goal=trialGoal}));
  trialProcess=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo{FileName=exe,WorkingDirectory=reopenRoot,Arguments="-eungye -roadMapTrial -roadMapInput \""+input+"\" -roadMapTrialRequest \""+request+"\" -screen-fullscreen 0",UseShellExecute=false});ShowStatus("시험 주행 실행 · 영업 저장과 분리됨");
 }
}
