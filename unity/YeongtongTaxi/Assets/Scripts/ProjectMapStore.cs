using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class ProjectMapStore {
 public const string RelativePath="config/maps/project/eungye.project.json";
 public const string ResourceName="eungye-project-map";
 public static bool IsRoot(string root){return !string.IsNullOrWhiteSpace(root)&&(File.Exists(Path.Combine(root,".git"))||Directory.Exists(Path.Combine(root,".git")))&&File.Exists(Path.Combine(root,"unity/YeongtongTaxi/ProjectSettings/ProjectVersion.txt"));}
 public static string FindRoot(string start){try{var dir=new DirectoryInfo(Path.GetFullPath(start));for(int i=0;dir!=null&&i<10;i++,dir=dir.Parent)if(IsRoot(dir.FullName))return dir.FullName;}catch{}return "";}
 public static string SourcePath(string root){if(!IsRoot(root))throw new InvalidOperationException("프로젝트 루트 폴더를 지정하세요 (.git 및 unity 폴더가 있는 project-b).");return Path.Combine(Path.GetFullPath(root),RelativePath);}
 public static string Fingerprint(string root){string path=SourcePath(root);return File.Exists(path)?ProjectMapPackage.Digest(File.ReadAllText(path)):"";}
 public static ProjectMapPackage Load(string root){return ProjectMapPackage.Parse(File.ReadAllText(SourcePath(root)));}
 public static string Save(string root,ProjectMapPackage package,string expectedFingerprint){
  string path=SourcePath(root);package.Validate();Directory.CreateDirectory(Path.GetDirectoryName(path));
  // The lock serializes validation/read/replace across editor processes. The token rejects stale drafts.
  using(var gate=new FileStream(path+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){
   if(Fingerprint(root)!=(expectedFingerprint??""))throw new IOException("다른 편집기 또는 pull이 프로젝트 지도를 변경했습니다. 초안을 별도로 저장하고 프로젝트 지도를 다시 여세요.");
   string text=JsonUtility.ToJson(package,false),temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
   try{File.WriteAllText(temp,text,new UTF8Encoding(false));ProjectMapPackage.Parse(File.ReadAllText(temp));if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);return ProjectMapPackage.Digest(text);}finally{if(File.Exists(temp))File.Delete(temp);}
  }
 }
}
