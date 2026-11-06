using System;
using System.IO;
using System.Text;
using UnityEngine;

// One immutable profile for the process's native world. Only explicit session
// boundaries call LoadActive; editing the disk file never mutates a live ride.
public static class ContentTuningRuntime {
 // Reserve the 1 MiB LAN packet budget for escaped JSON in a UTF16 string.
 public const int MaximumProfileBytes=240*1024;
 static bool initialized;
 static string status="기본 콘텐츠 설정",testDirectory;
 public static string Status {get{return status;}}
 public static bool TestMode {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-contentTest")>=0;}}
 public static bool HasOverride {get;private set;}
 public static string TestDirectory {get{Initialize();if(testDirectory==null){testDirectory=Path.Combine(Path.GetTempPath(),"EungyeTaxi-content-tests",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(testDirectory);}return testDirectory;}}
 public static string ActivePath {get{string requested=Argument("-contentProfile");if(TestMode&&!string.IsNullOrEmpty(requested))return Path.GetFullPath(requested);return Path.GetFullPath(Path.Combine(Application.dataPath,"..","content","active-profile.json"));}}
 public static string Argument(string name){var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,name);return at>=0&&at+1<args.Length?args[at+1]:null;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
 static void BeforeScene(){Initialize();}
 public static void Initialize(){
  if(initialized)return;
  var catalog=Resources.Load<TextAsset>("Content/catalog");var defaults=Resources.Load<TextAsset>("Content/default-profile");
  if(catalog==null||defaults==null)throw new InvalidOperationException("콘텐츠 기본 Resources가 없습니다. 실행본을 다시 배포하세요.");
  ContentTuning.ConfigureCatalog(catalog.text);ContentTuning.ValidateJson(defaults.text);ContentTuning.UseJson(defaults.text);initialized=true;
 }
 public static void LoadActive(){Initialize();LoadActivePath(ActivePath);}
 // Also used by the scene-free Editor acceptance checks with a temporary file.
 public static void LoadActivePath(string path){
  Initialize();
  try{
   if(!File.Exists(path)){ContentTuning.UseDefaults();HasOverride=false;status="기본 콘텐츠 설정";return;}
   var info=new FileInfo(path);if(info.Length>MaximumProfileBytes)throw new ArgumentException("프로필 크기 제한 초과");
   string json=File.ReadAllText(path,new UTF8Encoding(false,true));ValidateSize(json);string upgraded=ContentTuning.UpgradeJson(json);ValidateSize(upgraded);ContentTuning.UseJson(upgraded);HasOverride=true;status="사용자 콘텐츠 설정 적용 · 실행 중 변경은 다음 새 세션부터";
  }catch(Exception e){ContentTuning.UseDefaults();HasOverride=false;status="콘텐츠 설정 오류 · 기본값 사용: "+e.Message;Debug.LogWarning(status);}
 }
 public static string Snapshot(){Initialize();return ContentTuning.CaptureJson();}
 public static void Restore(string json){
  Initialize();
  if(json==null){ContentTuning.UseDefaults();HasOverride=false;status="이전 저장 · 기본 콘텐츠 설정 유지";return;}
  ValidateSize(json);ContentTuning.UseJson(json);HasOverride=true;status="저장/호스트의 콘텐츠 설정 유지 · 디스크 변경은 다음 새 세션부터";
 }
 // Candidate passenger validation uses the candidate's complete settings.
 // A rejected primary must not contaminate validation of its backup.
 public static void RestoreAndValidate(string json,Action validate){
  string previous=Snapshot(),previousStatus=status;bool previousOverride=HasOverride;
  try{Restore(json);validate();}catch{ContentTuning.UseJson(previous);status=previousStatus;HasOverride=previousOverride;throw;}
 }
 public static void ValidateHostSnapshot(string json,string frozen){
  Initialize();if(string.IsNullOrEmpty(json))throw new ArgumentException("호스트 콘텐츠 snapshot 누락 · 양쪽 실행본을 갱신하세요");ValidateSize(json);ContentTuning.ValidateJson(json);
  if(frozen!=null&&!string.Equals(json,frozen,StringComparison.Ordinal))throw new ArgumentException("경기 중 호스트 콘텐츠 설정 변경 거부");
 }
 static void ValidateSize(string json){if(json==null||Encoding.UTF8.GetByteCount(json)>MaximumProfileBytes)throw new ArgumentException("콘텐츠 프로필 크기 제한 초과");}
 public static string IsolatedSavePath(string directory,string filename){if(string.IsNullOrEmpty(filename)||Path.GetFileName(filename)!=filename)throw new ArgumentException("Invalid isolated save name");return Path.Combine(Path.GetFullPath(directory),filename);}
 public static string CampaignPath(string filename){return IsolatedSavePath(TestDirectory,filename);}
}
