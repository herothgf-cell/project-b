using System;
using System.IO;
using System.Reflection;
using UnityEngine;

public static class ContentTuningBuildChecks {
 public static void RuntimeThenBuild(){ContentTuningChecks.Run();Run();}
 public static void Run(){
  var guard=typeof(ContentTuningBuildChecks).Assembly.GetType("ContentTuningBuild");
  if(guard==null)throw new Exception("CONTENT_BUILD_RED missing catalog build guard");
  var verify=guard.GetMethod("VerifyCopies",BindingFlags.Public|BindingFlags.Static);
  string root=Path.GetFullPath("../.."),temp=Path.Combine(Path.GetTempPath(),"taxi-content-build-"+Guid.NewGuid().ToString("N"));
  try{
   foreach(string name in new[]{"catalog","default-profile"})foreach(string dir in new[]{"config/content","unity/EungyeTaxi/Assets/Resources/Content"}){
    string target=Path.Combine(temp,dir,name+".json");Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(Path.Combine(root,dir,name+".json"),target);
   }
   verify.Invoke(null,new object[]{temp});
   string modified=Path.Combine(temp,"unity/EungyeTaxi/Assets/Resources/Content/default-profile.json");File.AppendAllText(modified," ");
   bool rejected=false;try{verify.Invoke(null,new object[]{temp});}catch(TargetInvocationException e){rejected=e.GetBaseException() is IOException;}
   if(!rejected)throw new Exception("Changed Resource copy was accepted");
   File.Copy(Path.Combine(root,"config/content/default-profile.json"),modified,true);
   File.WriteAllText(Path.Combine(temp,"config/content/default-profile.json"),"{}");File.WriteAllText(modified,"{}");
   rejected=false;try{verify.Invoke(null,new object[]{temp});}catch(TargetInvocationException e){rejected=e.GetBaseException() is ArgumentException;}
   if(!rejected)throw new Exception("Invalid default profile was accepted");
   Debug.Log("CONTENT_TUNING_BUILD_CHECKS_OK checks=3");
  }finally{if(Directory.Exists(temp))Directory.Delete(temp,true);}
 }
}
