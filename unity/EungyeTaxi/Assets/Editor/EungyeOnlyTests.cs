using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
public static class EungyeOnlyTests {
 static void Check(bool ok,string message){if(!ok)throw new Exception("EUNGYE_ONLY_FAIL "+message);}
 public static void Run(){
  string previous=PlayerPrefs.GetString("next-region","");string locked=MapCatalog.LockedRegion;
  try{
   MapCatalog.LockedRegion="";Check(MapCatalog.Ids.SequenceEqual(new[]{"eungye"}),"only Eungye selectable");PlayerPrefs.SetString("next-region","retired-region");Check(MapCatalog.NextRegion=="eungye","obsolete preference defaults Eungye");Check(!MapCatalog.Available("retired-region")&&!MapCatalog.SelectNext("retired-region"),"obsolete map rejected");Check(MapCatalog.Load().mapId=="eungye","ordinary load uses Eungye");
   var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CampaignSaveLocation")).FirstOrDefault(t=>t!=null);Check(type!=null,"campaign save migration exists");var method=type.GetMethod("Resolve");string root=Path.GetFullPath("../../artifacts/eungye-only/save-"+Guid.NewGuid().ToString("N"));string old=Path.Combine(root,"\uC601\uD1B5 \uD0DD\uC2DC \uC2DC\uBBAC\uB808\uC774\uD130");Directory.CreateDirectory(old);File.WriteAllText(Path.Combine(old,"campaign-v2.json"),"original campaign");File.WriteAllText(Path.Combine(old,"campaign-v2.json.bak"),"original backup");
   string path=(string)method.Invoke(null,new object[]{root,"campaign-v2.json"});Check(path==Path.Combine(root,"은계 택시 시뮬레이터","campaign-v2.json"),"Eungye save directory");Check(File.ReadAllText(path)=="original campaign"&&File.ReadAllText(path+".bak")=="original backup","old save and backup copied");Check(File.ReadAllText(Path.Combine(old,"campaign-v2.json"))=="original campaign","old bytes retained");File.WriteAllText(path,"new campaign");method.Invoke(null,new object[]{root,"campaign-v2.json"});Check(File.ReadAllText(path)=="new campaign","existing Eungye save wins");Debug.Log("EUNGYE_ONLY_TESTS_OK");
  }finally{MapCatalog.LockedRegion=locked;if(previous=="")PlayerPrefs.DeleteKey("next-region");else PlayerPrefs.SetString("next-region",previous);PlayerPrefs.Save();}
 }
}
