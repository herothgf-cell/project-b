using System.IO;

public static class CampaignSaveLocation {
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
