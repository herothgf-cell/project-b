using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;
public static class RoadMapRuntime {
 public static MapData Baseline;public static string Notice="기본 지도";public static RoadMapDocument Active;
 public static bool Trial {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-roadMapTrial")>=0;}}
 public static string Argument(string name){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,name);return i>=0&&i+1<args.Length?args[i+1]:null;}
 public static string Hash(MapData map){if(!string.IsNullOrEmpty(map.editorBaseHash))return map.editorBaseHash;using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(JsonUtility.ToJson(map)))).Replace("-","").ToLowerInvariant();}
 public static MapData Apply(MapData baseline){baseline.editorBaseHash=Hash(baseline);Baseline=JsonUtility.FromJson<MapData>(JsonUtility.ToJson(baseline));Baseline.editorBaseHash=baseline.editorBaseHash;ReviewedLaneLayout.CopyState(baseline,Baseline);Active=null;Notice="기본 지도";if(baseline.mapId!="eungye"||!baseline.hybrid)return baseline;
  string path=Argument("-roadMapInput");if(string.IsNullOrEmpty(path)&&!Application.isBatchMode){bool test=false;foreach(var a in Environment.GetCommandLineArgs())if(a.EndsWith("Test"))test=true;if(!test)path=RoadMapStore.DefaultPath;}
  if(string.IsNullOrEmpty(path)||!File.Exists(path))return baseline;try{var d=RoadMapStore.Load(path);var issues=RoadMapValidator.Validate(baseline,d);if(issues.Exists(x=>x.error))throw new InvalidDataException(issues.Find(x=>x.error).ToString());var candidate=RoadMapComposer.Compose(baseline,d);CorridorPolish.Apply(candidate);FuelStationLayout.Apply(candidate);Active=d;Notice=d.Version;return candidate;}catch(Exception e){Notice="수정본 적용 실패 · 기본 지도: "+e.Message;Debug.LogWarning(Notice);if(Trial)throw new InvalidOperationException(Notice,e);return baseline;}
 }
}
