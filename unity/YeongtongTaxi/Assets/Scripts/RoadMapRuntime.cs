using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;
public static class RoadMapRuntime {
 public static MapData Baseline;public static string Notice="기본 지도";public static RoadMapDocument Active,ProjectDefault;public static string ProjectToken="";
 public static bool Trial {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-roadMapTrial")>=0;}}
 public static string Argument(string name){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,name);return i>=0&&i+1<args.Length?args[i+1]:null;}
 public static string Hash(MapData map){if(!string.IsNullOrEmpty(map.editorBaseHash))return map.editorBaseHash;using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(JsonUtility.ToJson(map)))).Replace("-","").ToLowerInvariant();}
 public static MapData Apply(MapData baseline){return Apply(baseline,null,"");}
 public static MapData Apply(MapData baseline,RoadMapDocument project,string projectToken){
  baseline.editorBaseHash=Hash(baseline);Baseline=JsonUtility.FromJson<MapData>(JsonUtility.ToJson(baseline));Baseline.editorBaseHash=baseline.editorBaseHash;ReviewedLaneLayout.CopyState(baseline,Baseline);
  ProjectToken=projectToken??"";ProjectDefault=project==null?null:project.Copy();Active=null;Notice="기본 지도";
  MapData defaultMap=baseline;
  if(ProjectDefault!=null){ProjectDefault.projectRevision=ProjectToken;defaultMap=RoadMapComposer.Compose(Baseline,ProjectDefault);Active=ProjectDefault.Copy();Notice="프로젝트 지도 · "+ProjectDefault.Version;}
  if(baseline.mapId!="eungye"||!baseline.hybrid)return defaultMap;
  string path=Argument("-roadMapInput");if(string.IsNullOrEmpty(path)&&!Application.isBatchMode){bool test=false;foreach(var a in Environment.GetCommandLineArgs())if(a.EndsWith("Test"))test=true;if(!test)path=RoadMapStore.DefaultPath;}
  if(string.IsNullOrEmpty(path)||!File.Exists(path))return defaultMap;
  try{
   var d=RoadMapStore.Load(path);if(ProjectDefault!=null&&d.projectRevision!=ProjectToken)throw new InvalidDataException("개인 수정본의 프로젝트 버전이 다릅니다. 새 프로젝트 지도를 기준으로 다시 편집하세요.");
   var issues=RoadMapValidator.Validate(Baseline,d);if(issues.Exists(x=>x.error))throw new InvalidDataException(issues.Find(x=>x.error).ToString());
   var candidate=RoadMapComposer.Compose(Baseline,d);CorridorPolish.Apply(candidate);FuelStationLayout.Apply(candidate);Active=d;Notice=d.Version;return candidate;
  }catch(Exception e){Notice="개인 수정본 적용 실패 · "+(ProjectDefault==null?"기본 지도":"프로젝트 지도")+": "+e.Message;Debug.LogWarning(Notice);if(Trial)throw new InvalidOperationException(Notice,e);return defaultMap;}
 }
}
