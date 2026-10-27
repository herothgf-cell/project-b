using System;
using System.Linq;
using UnityEngine;

// Explicit source editing, never called by a running game.
public static class RoadFollowupAuthoring {
 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);var package=ProjectMapStore.Load(root);
  var north=new RoadMapEvidence{state="observed",month="2026-05",checkedAt="2026-10-27",appearanceConfirmed=true,url="https://map.naver.com/p/entry/coordinates/3zanLi,2AGD8X?c=15.99,0,0,0,adh&p=8tKhgd4xCQY5MqAZhqKJMQ,21.47,10,80,Float",note="Viewed northbound approach and through-road separators. Same-direction lane changes are provisional game geometry, not surveyed regulation."};
  var west=new RoadMapEvidence{state="observed",month="2026-05",checkedAt="2026-10-27",appearanceConfirmed=true,url="https://map.naver.com/p/entry/coordinates/3zanLi,2AGD8X?c=15.99,0,0,0,adh&p=HSQQcjKQMYNVjekPRSdUxw,118.88,10,80,Float",note="Three same-direction Suin-ro lanes with dashed separators before the existing left pocket. Two sequential 24m game lane changes, no new intersection turn permission."};
  var doc=PlaytestLaneChangeRepair.Apply(package,north,west);
  var baseline=JsonUtility.FromJson<MapData>(JsonUtility.ToJson(package.baseline));
  var j=baseline.junctionRules.Single(r=>r.id=="J5750_2576");
  j.signalMode=0;j.facilityStatus="user-corrected";j.facilityCheckedAt="2026-10-27";
  j.facilityEvidence="User screenshot4: crossing without vehicle signals. Naver nearby approach reviewed; exact on-site dimensions and private access layout not surveyed. Preserve mapped T-junction left/right connections and crosswalk, remove fictitious prototype lights.";
  doc.baseHash=ProjectMapPackage.Digest(JsonUtility.ToJson(baseline));baseline.editorBaseHash=doc.baseHash;
  var updated=ProjectMapPackage.Create(baseline,doc);ProjectMapStore.Save(root,updated,token);ProjectMapBuild.Prepare();
  Debug.Log("ROAD_FOLLOWUP_SOURCE_SAVED hash="+updated.contentHash+" lanes="+updated.document.lanes.Length);
  PlaytestDirectTurnTests.Run();RoadFollowupTests.Run();
 }
}
