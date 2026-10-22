using System;
using System.IO;
using UnityEngine;
public static class RoadMapStore {
 public static string DirectoryPath {get{return Path.Combine(Application.persistentDataPath,"RoadMaps");}}
 public static string DefaultPath {get{return Path.Combine(DirectoryPath,"active.json");}}
 public static RoadMapDocument Load(string path){var text=File.ReadAllText(path);if(!text.TrimStart().StartsWith("{"))throw new InvalidDataException("JSON 문서가 아닙니다");var d=JsonUtility.FromJson<RoadMapDocument>(text);if(d==null||d.schema!=1||string.IsNullOrWhiteSpace(d.baseHash)||d.lanes==null||d.lanes.Length==0||d.facilities==null||d.signals==null)throw new InvalidDataException("지원하지 않는 수정본 또는 누락된 자료");return d;}
 public static void Save(string path,RoadMapDocument d){if(d.schema!=1)throw new InvalidDataException("지원하지 않는 스키마");string full=Path.GetFullPath(path);Directory.CreateDirectory(Path.GetDirectoryName(full));string temp=full+"."+Guid.NewGuid().ToString("N")+".tmp";try{var copy=d.Copy();copy.savedAt=DateTime.UtcNow.ToString("o");File.WriteAllText(temp,JsonUtility.ToJson(copy,true));Load(temp);if(File.Exists(full))File.Replace(temp,full,full+".bak");else File.Move(temp,full);d.savedAt=copy.savedAt;}finally{if(File.Exists(temp))File.Delete(temp);}}
 public static void SetDefault(string path,MapData baseline){var d=Load(path);var issues=RoadMapValidator.Validate(baseline,d);if(issues.Exists(x=>x.error))throw new InvalidDataException("검사 오류가 있어 기본 지도로 지정할 수 없습니다");RoadMapComposer.Compose(baseline,d);Save(DefaultPath,d);}
 public static void RestoreDefault(){if(File.Exists(DefaultPath)){string previous=DefaultPath+".disabled";File.Copy(DefaultPath,previous,true);File.Delete(DefaultPath);}}
}
