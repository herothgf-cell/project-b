using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;

// Complete, portable authoring source; no AppData or raw GIS files are needed to compose it.
[Serializable] public sealed class ProjectMapPackage {
 public int schema=1;
 public string baselineHash,contentHash;
 public MapData baseline;
 public RoadMapDocument document;
 [NonSerialized] string validatedBaseline,validatedContent;
 public static string Digest(string text){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-","").ToLowerInvariant();}
 public static ProjectMapPackage Create(MapData map,RoadMapDocument doc){
  var p=new ProjectMapPackage{baseline=JsonUtility.FromJson<MapData>(JsonUtility.ToJson(map)),document=doc.Copy()};
  p.baselineHash=Digest(JsonUtility.ToJson(p.baseline));p.baseline.editorBaseHash=p.baselineHash;
  p.document.projectRevision="";p.document.savedAt=DateTime.UtcNow.ToString("o");p.contentHash=Digest(JsonUtility.ToJson(p.document));p.Validate();return p;
 }
 public static ProjectMapPackage Parse(string json){
  if(string.IsNullOrWhiteSpace(json)||json.Length>128*1024*1024||!json.TrimStart().StartsWith("{"))throw new InvalidDataException("프로젝트 지도 JSON이 올바르지 않습니다.");
  var watch=System.Diagnostics.Stopwatch.StartNew();
  var p=JsonUtility.FromJson<ProjectMapPackage>(json);if(p==null)throw new InvalidDataException("프로젝트 지도 자료가 없습니다.");
  long parse=watch.ElapsedMilliseconds;p.Validate();Debug.Log("STARTUP_MAP parse_ms="+parse+" validate_ms="+(watch.ElapsedMilliseconds-parse));return p;
 }
 public void Validate(){
  if(schema!=1||baseline==null||document==null||document.scene==null)throw new InvalidDataException("프로젝트 지도 형식/장면 자료가 누락되었습니다.");
  if(baseline.mapId!="eungye"||!baseline.hybrid||baseline.roads==null||baseline.buildings==null||baseline.stops==null||baseline.stops.Length==0||!baseline.HasHd)throw new InvalidDataException("은계 기본 지도 구조를 확인하세요.");
  if(baselineHash!=Digest(JsonUtility.ToJson(baseline))||contentHash!=Digest(JsonUtility.ToJson(document)))throw new InvalidDataException("프로젝트 지도 해시 불일치 · 정상 자료를 다시 받으세요.");
  baseline.editorBaseHash=baselineHash;
  if(validatedBaseline==baselineHash&&validatedContent==contentHash)return;
  var errors=RoadMapValidator.Validate(baseline,document).Where(i=>i.error).ToArray();if(errors.Length>0)throw new InvalidDataException("프로젝트 지도 검사 실패: "+string.Join(" / ",errors.Take(5).Select(i=>i.ToString())));
  validatedBaseline=baselineHash;validatedContent=contentHash;
 }
 public MapData Compose(){Validate();return RoadMapComposer.Compose(baseline,document);}
}
