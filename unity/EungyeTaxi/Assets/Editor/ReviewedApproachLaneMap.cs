using System;
using System.Linq;
using UnityEngine;

// Repeatable appearance-only assignments for the three reviewed approaches.
// Source lanes and all existing movement permissions remain unchanged.
public static class ReviewedApproachLaneMap {
 public const string Prefix="reviewed:approach-paint:2026-11-09:";
 public static readonly string[] Junctions={"J866_5285","J5057_4449","J5057_4449"};
 public static readonly int[] Axes={0,1,1},Signs={-1,1,-1};
 public static readonly string[] Urls={
  "https://map.naver.com/p?c=19.00,0,0,0,adh&p=U_JkvVOycY0upzQZK5OK6w,276,-20,75,Float",
  "https://map.naver.com/p?c=19.00,0,0,0,adh&p=4lVm7HmiiXmWi7mbrwLPsA,105,-18,70,Float",
  "https://map.naver.com/p?c=19.00,0,0,0,adh&p=H8kJAUid-f2UdktwCnF3wA,285,-18,70,Float"
 };
 // First four IDs own the role glyphs; connected later pieces extend only
 // the selected physical lane's separator/stray-line replacement coverage.
 public static readonly string[][] Lanes={
  new[]{"A2218G611780:0","A2218G611284:0","A2218G611282:0","A2218G611305:0","A2218G610037:0","A2218G611773:0","A2218G611781:0","A2218G611774:0"},
  new[]{"A2218G611350:2","A222BC004202:4","A222BC004204:3","A222BC004206:4","A222BC004208:0","A222BC004203:0","A222BC004205:0","A222BC004207:0","A222BC004199:0","A222BC004198:0","A222BC004197:0","A222BC004196:0"},
  new[]{"A2218G611401:0","A2218G611404:0","A2218G611403:0","A2218G611402:0","A2218G611335:0","A2218G611333:0","A2218G611334:0","A2218G611332:0","A2218G611887:0","A2218G611888:0","A2218G611889:0","A2218G611890:0"}
 };
 public static readonly int[][] Masks={new[]{9,2,2,6},new[]{9,2,2,2},new[]{9,2,2,2}};
 static void Check(bool ok,string reason){if(!ok)throw new Exception("REVIEWED_APPROACH_LANE_MAP: "+reason);}
 public static string ProtectedContent(RoadMapDocument source){var d=source.Copy();d.facilities=d.facilities.Where(f=>!f.id.StartsWith(Prefix)).ToArray();d.revision=0;d.savedAt="";d.projectRevision="";return ProjectMapPackage.Digest(d.ToJson());}
 public static ProjectMapPackage Draft(ProjectMapPackage source){
  source.Validate();var d=source.document.Copy();var facilities=d.facilities.Where(f=>!f.id.StartsWith(Prefix)).ToList();
  for(int approach=0;approach<Lanes.Length;approach++)foreach(string id in Lanes[approach]){
   Check(!facilities.Any(f=>f.kind=="lanePresentation"&&f.laneId==id),"another author already owns selected lane "+id);
   var lane=d.lanes.Single(l=>l.id==id);Check(!lane.disabled&&lane.status=="ngii"&&lane.turn==0&&lane.change==0,"selected source approach remains active "+id);
   var path=new AssistedRoadNetwork.Path(lane.points.Select(p=>p.V()).ToArray());
   facilities.Add(new RoadMapFacility{id=Prefix+id,kind="lanePresentation",type="원본 차로",junctionId=Junctions[approach],laneId=id,axis=Axes[approach],sign=Signs[approach],width=lane.width,startAt=0,endAt=path.length,x=lane.points[0].x,z=lane.points[0].z,text="",evidence=new RoadMapEvidence{state="observed",appearanceConfirmed=true,ruleConfirmed=false,url=Urls[approach],month="2026-05",checkedAt="2026-11-09",note="해당 접근의 중앙 유턴 공간/표지와 차로 외형을 확인. 기존 활성 원본 연결에서 표시 역할을 분류하며, 원본 turn=0 유턴 모양 경로의 통행 조건을 새로 승인하지 않음. 앞쪽 원본 조각은 구분선/중복선 교정 구간을 연장하고 차로별 역할 화살표는 상류에 한 번 표시. 좌표·폭·표시 위치는 게임 추정."}});
  }
  d.facilities=facilities.ToArray();d.revision++;var candidate=ProjectMapPackage.Create(source.baseline,d);
  Check(candidate.baselineHash==source.baselineHash&&ProtectedContent(candidate.document)==ProtectedContent(source.document),"baseline, geometry, graph, scene, signals and unowned facilities must remain exact");
  return candidate;
 }
 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);var source=ProjectMapStore.Load(root);var candidate=Draft(source);
  ReviewedApproachLaneMapTests.Verify(source,candidate);ProjectMapStore.Save(root,candidate,token);ProjectMapBuild.Prepare();Verify();
  Debug.Log("REVIEWED_APPROACH_LANE_MAP_APPLIED assignments=32 approaches=3 baseline="+candidate.baselineHash+" content="+candidate.contentHash);
 }
 public static void Verify(){var package=ProjectMapStore.Load(ProjectMapBuild.Root);ReviewedApproachLaneMapTests.Verify(package,package);Debug.Log("REVIEWED_APPROACH_LANE_MAP_OK assignments=32 roleGlyphs=12");}
}
