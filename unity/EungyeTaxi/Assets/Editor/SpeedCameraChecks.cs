using System;
using System.IO;
using System.Linq;
using UnityEngine;

// Speed cameras (2026-11-24, Editor, no scene): the data file and its source, defaults, fines by how far over.
public static class SpeedCameraChecks {
 static int checks;
 static void Check(bool ok,string name){checks++;if(!ok)throw new Exception("SPEEDCAMERA "+name);}
 public static void Run(){
  checks=0;
  ContentTuning.ConfigureCatalog(File.ReadAllText(Path.GetFullPath("../../config/content/catalog.json")));
  ContentTuning.ValidateJson(File.ReadAllText(Path.GetFullPath("../../config/content/default-profile.json")));
  string source=File.ReadAllText(Path.GetFullPath("../../config/maps/eungye-speed-cameras.json"));
  Check(source==File.ReadAllText(Path.GetFullPath("Assets/Resources/eungye-speed-cameras.json")),"the Resources copy matches config/maps");
  SpeedCameraRules.Configure(source);var cams=SpeedCameraRules.Cameras;
  Check(cams.Length==17&&cams.Select(c=>c.x+","+c.z).Distinct().Count()==16,"17 records at 16 points");
  Check(cams.Count(c=>c.kmh==60)==3&&cams.Count(c=>c.kmh==50)==2&&cams.Count(c=>c.kmh==30)==12,"limits: 수인로 60 x3, 대은로 50 x2, school zones 30 x12");
  Check(SpeedCameraRules.Source.Contains("공공데이터포털")&&SpeedCameraRules.Source.Contains("15028200"),"source and attribution recorded");
  Check(SpeedCameraRules.Enabled&&SpeedCameraRules.Tolerance==10&&SpeedCameraRules.WarnMetres==150,"defaults: +10 tolerance, 150 m warning");
  Check(SpeedCameraRules.Fine(70,60)==0&&SpeedCameraRules.Fine(70.5f,60)==2000&&SpeedCameraRules.Fine(80,60)==2000&&SpeedCameraRules.Fine(80.5f,60)==4000&&SpeedCameraRules.Fine(100,60)==4000&&SpeedCameraRules.Fine(101,60)==7000,"fines 2,000 / 4,000 / 7,000 by +10..20 / ..40 / beyond");
  Check(SpeedCameraRules.Fine(45,30)==2000&&SpeedCameraRules.Fine(39,30)==0,"school zone 30: 45 km/h fined, 39 not");
  Check(SpeedCameraRules.Fine(200,0)==0,"no limit, no fine");
  Debug.Log("SPEEDCAMERA_CHECKS_OK checks="+checks);
 }
}
