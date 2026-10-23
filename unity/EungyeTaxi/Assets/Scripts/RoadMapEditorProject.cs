using System;
using System.IO;
using UnityEngine;
public partial class RoadMapEditor {
 string projectRoot="",projectFingerprint="",projectReadToken="",projectStatus="";bool projectInitialized;
 public string ProjectRoot {get{return projectRoot;}}
 void InitializeProject(){if(projectInitialized)return;projectInitialized=true;projectReadToken=RoadMapRuntime.ProjectToken;string root=ProjectMapStore.FindRoot(Application.dataPath);if(string.IsNullOrEmpty(root))root=PlayerPrefs.GetString("roadEditorProjectRoot","");SetProjectRoot(root);}
 public void SetProjectRoot(string root){projectRoot=root??"";try{projectFingerprint=ProjectMapStore.Fingerprint(projectRoot);projectStatus=string.IsNullOrEmpty(projectFingerprint)?"이 프로젝트에는 아직 지도가 없습니다.":"프로젝트 경로 확인됨";}catch(Exception e){projectFingerprint="";projectStatus=e.Message;}}
 public bool SaveProject(){
  try{
   if(document.scene==null)Edit(()=>document.scene=ProjectMapScene.Capture(RoadMapRuntime.Baseline));
   if(!string.IsNullOrEmpty(projectFingerprint)){var current=ProjectMapStore.Load(projectRoot);if(current.contentHash!=projectReadToken)throw new IOException("저장소의 지도가 현재 편집기의 기준 버전과 다릅니다. 초안을 저장한 뒤 프로젝트 지도를 다시 여세요.");}
   var package=ProjectMapPackage.Create(RoadMapRuntime.Baseline,document);projectFingerprint=ProjectMapStore.Save(projectRoot,package,projectFingerprint);projectReadToken=package.contentHash;
   document.savedAt=package.document.savedAt;saved=document.ToJson();dirtyDocument=null;PlayerPrefs.SetString("roadEditorProjectRoot",projectRoot);PlayerPrefs.Save();projectStatus="프로젝트 저장 완료 · Git 변경 파일을 커밋·푸시하고 새로 빌드하세요.";notice=projectStatus;return true;
  }catch(Exception e){notice=projectStatus="프로젝트 저장 실패: "+e.Message;return false;}
 }
 public bool LoadProject(){
  try{
   var package=ProjectMapStore.Load(projectRoot);if(package.baselineHash!=RoadMapRuntime.Hash(RoadMapRuntime.Baseline))throw new InvalidDataException("기준 지도가 변경되었습니다. pull 후 이 프로젝트를 새로 빌드해서 실행하세요.");
   Edit(()=>document=package.document.Copy());document.projectRevision=RoadMapRuntime.ProjectToken;projectFingerprint=ProjectMapStore.Fingerprint(projectRoot);projectReadToken=package.contentHash;Select("");numbers.Clear();easyAction="select";
   saved=document.ToJson();projectStatus=notice="프로젝트 지도 불러옴 · "+document.Version;return true;
  }catch(Exception e){notice=projectStatus="프로젝트 불러오기 실패: "+e.Message;return false;}
 }
 void DrawProjectProperties(){
  InitializeProject();Caption("프로젝트 지도 · 공유와 배포");Caption("도로·건물·보도·생활 부지를 프로젝트 파일 하나로 보관합니다.");
  Caption("실행 중인 지도: "+(RoadMapRuntime.ProjectDefault==null?"기존 기본 지도":RoadMapRuntime.ProjectDefault.Version));Caption("편집 중: "+document.Version);
  Caption("프로젝트 루트 폴더 (.git / unity 포함)");string next=GUI.TextField(new Rect(0,propY,330,54),projectRoot??"",new GUIStyle(field){wordWrap=true});propY+=60;if(next!=projectRoot){projectRoot=next;projectFingerprint="";projectStatus="경로 확인을 누르세요.";}
  ActionButton("프로젝트 경로 확인",()=>SetProjectRoot(projectRoot));ActionButton("현재 실행파일에서 프로젝트 찾기",()=>SetProjectRoot(ProjectMapStore.FindRoot(Application.dataPath)));
  Caption("저장 위치: "+ProjectMapStore.RelativePath);ActionButton("프로젝트에 저장 · 배포 자료 확정",()=>SaveProject());ActionButton("저장소의 프로젝트 지도 다시 열기",()=>LoadProject());
  ActionButton("프로젝트 지도 폴더 열기",()=>{try{var path=Path.GetDirectoryName(ProjectMapStore.SourcePath(projectRoot));Directory.CreateDirectory(path);Application.OpenURL(new Uri(path).AbsoluteUri);}catch(Exception e){notice=projectStatus=e.Message;}});
  Caption(projectStatus);Caption("1. 검사와 저장하고 시험\n2. 프로젝트에 저장\n3. 변경 JSON 커밋·푸시\n4. 다른 PC에서 pull → BuildHybrid\n5. 새 실행파일에 같은 지도 적용");Caption("개인 저장/자동 초안은 이 PC에서만 보관합니다. 프로젝트 저장은 영업 중인 지도를 즉시 바꾸지 않습니다.");
 }
}
