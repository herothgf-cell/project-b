using System.IO;
using System;
using System.Linq;

public static class CampaignSaveLocation {
 const string ActiveCareer="active-career-slot.txt";
 // Test feedback 2026-11-24: three save slots only. Career files older than the newest three stay on
 // disk untouched but are not listed.
 public const int MaxCareers=3;
 const string RetiredFolder="replaced-careers";
 public static bool Full(string directory){return ListCareers(directory).Length>=MaxCareers;}
 public static string SlotLabel(int index,string path){return "슬롯 "+(index+1)+" · "+File.GetLastWriteTime(File.Exists(path)?path:path+".bak").ToString("yyyy-MM-dd HH:mm");}
 // Frees a slot for a new career. The career, its backup and its side files move into a folder
 // beside the saves instead of being erased, so a mistaken overwrite can still be recovered by hand.
 public static void Retire(string path){
  string name=Path.GetFileName(path),directory=Path.GetDirectoryName(path);
  if(!CareerFile(name))throw new IOException("Invalid career slot");
  string target=Path.Combine(directory,RetiredFolder,DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,6));
  Directory.CreateDirectory(target);
  foreach(string file in Directory.GetFiles(directory,name+"*"))File.Move(file,Path.Combine(target,Path.GetFileName(file)));
 }
 public static string NewCareer(string directory){return Path.Combine(directory,"career-"+Guid.NewGuid().ToString("N")+".json");}
 static bool CareerFile(string name){
  if(name==PrototypeSettings.ThreeDaySaveFile)return true;
  if(name==null||name.Length!=44||!name.StartsWith("career-",StringComparison.Ordinal)||!name.EndsWith(".json",StringComparison.Ordinal))return false;
  Guid value;return Guid.TryParseExact(name.Substring(7,32),"N",out value);
 }
 public static string SelectedCareer(string fallback){
  var directory=Path.GetDirectoryName(fallback);
  var marker=Path.Combine(directory,ActiveCareer);
  if(!File.Exists(marker))return fallback;
  string name=File.ReadAllText(marker).Trim();
  if(!CareerFile(name))return fallback;
  string selected=Path.Combine(directory,name);
  return File.Exists(selected)||File.Exists(selected+".bak")?selected:fallback;
 }
 public static void SelectCareer(string path){
  string name=Path.GetFileName(path);
  if(!CareerFile(name)||(!File.Exists(path)&&!File.Exists(path+".bak")))throw new IOException("Invalid career slot");
  CampaignCheckpointWriter.Write(Path.Combine(Path.GetDirectoryName(path),ActiveCareer),name);
 }
 public static string[] ListCareers(string directory){
  if(!Directory.Exists(directory))return new string[0];
  return Directory.GetFiles(directory,"*.json*").Select(p=>p.EndsWith(".bak",StringComparison.Ordinal)?p.Substring(0,p.Length-4):p).Where(p=>CareerFile(Path.GetFileName(p))).Distinct().OrderByDescending(p=>File.GetLastWriteTimeUtc(File.Exists(p)?p:p+".bak")).Take(MaxCareers).ToArray();
 }
 // Stable data identity; changing the displayed product name must not hide saves.
 public static string DirectoryFor(string companyDirectory){return Path.Combine(companyDirectory,"은계 택시 시뮬레이터");}
 public static string Resolve(string companyDirectory,string fileName){
  string current=Path.Combine(DirectoryFor(companyDirectory),fileName);
  // One-time compatibility copy from the previous product's folder. Never move
  // the original or replace an existing Eungye campaign/backup.
  string previous=Path.Combine(companyDirectory,"\uC601\uD1B5 \uD0DD\uC2DC \uC2DC\uBBAC\uB808\uC774\uD130",fileName);
  if(!File.Exists(current))foreach(string suffix in new[]{"",".bak"})if(File.Exists(previous+suffix)&&!File.Exists(current+suffix)){Directory.CreateDirectory(Path.GetDirectoryName(current));File.Copy(previous+suffix,current+suffix,false);}
  return current;
 }
}
