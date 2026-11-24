using System;
using System.IO;
using System.Linq;
using UnityEngine;

// Speed bumps (2026-11-24, Editor, no scene): the road facility input carries five bumps with evidence, the layout
// plans them as bumps (not barriers), and the comfort rule and bounce.
public static class SpeedBumpChecks {
 static int checks;
 static void Check(bool ok,string name){checks++;if(!ok)throw new Exception("SPEEDBUMP "+name);}
 public static void Run(){
  checks=0;
  ContentTuning.ConfigureCatalog(File.ReadAllText(Path.GetFullPath("../../config/content/catalog.json")));
  ContentTuning.ValidateJson(File.ReadAllText(Path.GetFullPath("../../config/content/default-profile.json")));
  string source=File.ReadAllText(Path.GetFullPath("../../config/maps/eungye-road-facilities.json"));
  Check(source==File.ReadAllText(Path.GetFullPath("Assets/Resources/eungye-road-facilities.json")),"the Resources copy matches config/maps");
  var catalog=JsonUtility.FromJson<ReviewedRoadFacilities.Catalog>(source);var bumps=catalog.facilities.Where(f=>f.kind=="speed-bump").ToArray();
  // 2026-11-24 round 5: the three NGII 수인로 side-road bumps were turned off (the side road is not in the game map).
  Check(bumps.Length==2&&bumps.All(f=>!string.IsNullOrEmpty(f.observed)&&!string.IsNullOrEmpty(f.estimated)&&f.segments.Length==1&&f.segments[0].kind=="speed-bump"),"two bumps, each with observed evidence and estimated parts");
  Check(bumps.Count(f=>f.id.Contains("ngii"))==0&&bumps.Count(f=>f.id.EndsWith("p12")||f.id.EndsWith("p24"))==2,"two field-survey bumps, no NGII side-road bumps");
  Check(catalog.facilities.Length==38,"the 36 earlier facilities are kept");
  var layout=ReviewedRoadFacilities.Plan(new MapData{mapId="eungye"},new ReviewedRoadFacilities.Catalog{mapId="eungye",facilities=bumps});
  Check(layout.bumps.Count==2&&layout.barriers.Count==0&&layout.hatches.Count==0,"planned as bumps only, never as barriers");
  Check(layout.bumps.All(b=>{float l=Vector3.Distance(b.a,b.b);return l>2.5f&&l<10;}),"each spans 2.9..9.0 m of road");
  Check(SpeedBumpRules.Enabled&&SpeedBumpRules.ComfortKmh==25&&SpeedBumpRules.Penalty==2,"defaults: 25 km/h, 2 points");
  Check(SpeedBumpRules.Penalised(30,true)&&!SpeedBumpRules.Penalised(25,true)&&!SpeedBumpRules.Penalised(40,false),"penalty only over 25 km/h with a passenger");
  Check(SpeedBumpRules.Bounce(10)==0&&SpeedBumpRules.Bounce(55)>.9f&&SpeedBumpRules.Bounce(30)>0,"bounce grows with speed");
  Debug.Log("SPEEDBUMP_CHECKS_OK checks="+checks);
 }
}
