using System;
using System.IO;
using System.Linq;
using UnityEngine;
public static class RealLeftBuildChecks {
 [Serializable] sealed class Review {public int schema;public Entry[] verifiedApproaches,reviewedApproaches;}
 [Serializable] sealed class Entry {public string junctionId,incoming,status,month,url,checkedAt;public bool permissiveSignConfirmed,permissionGranted;}
 public static void Evidence(){
  var review=JsonUtility.FromJson<Review>(File.ReadAllText(Path.Combine(ProjectMapBuild.Root,"config/maps/eungye-permissive-left-review.json")));
  if(review.schema!=1||review.verifiedApproaches.Length!=0||review.reviewedApproaches.Length!=3||review.reviewedApproaches.Any(r=>r.permissionGranted||r.permissiveSignConfirmed||!r.url.StartsWith("https://map.naver.com/")||r.checkedAt!="2026-10-30"))throw new Exception("Real-left evidence must not silently promote uncertain approaches");
  var map=ProjectMapStore.Load(ProjectMapBuild.Root).Compose();if(map.junctionRules.Any(j=>j.signalMode==1))throw new Exception("No published permissive approach currently has positive directional evidence");
  Debug.Log("REAL_LEFT_EVIDENCE_OK reviewed=3 verified=0 unknown never promoted; source fixed signals retained");
 }
 public static void Build(){ProjectMapBuild.Prepare();Evidence();PrototypeScenarioTests.Run();WorldFollowupChecks.Run();PrototypeBuild.BuildHybrid();}
}
