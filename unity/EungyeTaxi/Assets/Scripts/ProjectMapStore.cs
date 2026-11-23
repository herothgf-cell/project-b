using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class ProjectMapStore {
 public const string RelativePath="config/maps/project/eungye.project.json";
 public const string ResourceName="eungye-project-map";
 // Builds ship the package gzip-compressed (eungye-project-map.bytes, ~4 MB instead of ~24 MB JSON).
 // An uncompressed .json resource from an older prepare step is still accepted.
 public static bool IsGzip(byte[] data){return data!=null&&data.Length>2&&data[0]==0x1f&&data[1]==0x8b;}
 public static string Decompress(byte[] data){using(var input=new MemoryStream(data))using(var gzip=new System.IO.Compression.GZipStream(input,System.IO.Compression.CompressionMode.Decompress))using(var reader=new StreamReader(gzip,new UTF8Encoding(false)))return reader.ReadToEnd();}
 public static byte[] Compress(string text){using(var output=new MemoryStream()){using(var gzip=new System.IO.Compression.GZipStream(output,System.IO.Compression.CompressionLevel.Optimal,true)){var bytes=new UTF8Encoding(false).GetBytes(text);gzip.Write(bytes,0,bytes.Length);}return output.ToArray();}}
 public static string ReadResource(){var asset=Resources.Load<TextAsset>(ResourceName);if(!asset)return null;var data=asset.bytes;return IsGzip(data)?Decompress(data):asset.text;}
 public static bool IsRoot(string root){return !string.IsNullOrWhiteSpace(root)&&(File.Exists(Path.Combine(root,".git"))||Directory.Exists(Path.Combine(root,".git")))&&File.Exists(Path.Combine(root,"unity/EungyeTaxi/ProjectSettings/ProjectVersion.txt"));}
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
