using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Metadata for eight confirmed living-street groups, never P07, P16/P19 school
// roads or previously applied P12/P21. Preview never saves. Apply is disabled
// until the coordinator pins the exact reviewed revision AND content hash.
public static class PinkGridUnmarkedMapEdit {
 public const string Prefix="reviewed:pink-grid-unmarked:";
 public const int RequiredRevision=25;
 public const string RequiredContentHash="970525154e204fdb578e57f1c63cb9c2b602ace906b8d56a0b68baf8bf4ec5fb";
 public sealed class Group {public readonly string id,a,b,panos;public Group(string id,string a,string b,string panos){this.id=id;this.a=a;this.b=b;this.panos=panos;}}
 public static readonly Group[] Groups={
  new Group("P08","local:8424:1634:8683:1632","local:8682:1680:8424:1682","MlzMUWLScNNJaKCo77a57A,WfCoH8kU2zbiDF005t5wSA"),
  new Group("P09","local:8374:1735:8405:2479","local:8357:2481:8326:1737","OnsCrQACc9oPWKO245HEkQ,c3fsTcxzgAZ696o29tgijA,2HGD1hgUq3vuQfBJzQbzmQ"),
  new Group("P17","local:8296:2598:8098:2596","local:8099:2537:8297:2539","cayu0OzIISux-XbhY04l1Q,NgSrmP7inn6NRilodduWyQ"),
  new Group("P23","local:8848:830:9173:1574","local:9125:1575:8847:878","nnL0ZF6DJZDIxzwdMIjRGw,HRVnu_ON5y1vyq2lsB49hg"),
  new Group("P24","local:8318:1581:8293:945","local:8341:944:8366:1579","OnsCrQACc9oPWKO245HEkQ,c3fsTcxzgAZ696o29tgijA,2HGD1hgUq3vuQfBJzQbzmQ"),
  new Group("P25","local:9072:1678:8839:1681","local:8839:1633:9072:1630","HRVnu_ON5y1vyq2lsB49hg,qQVxLtI_YIpsXCwYUVuJ6Q"),
  new Group("P27","local:8474:2541:8672:2538","local:8673:2597:8474:2600","ByYrpdclLoClXjZwR2m_fg,cayu0OzIISux-XbhY04l1Q"),
  new Group("P28","local:9175:1730:8851:2587","local:8850:2539:9127:1731","HRVnu_ON5y1vyq2lsB49hg,hgbW21ZxQssicwyj2ITQFg,ByYrpdclLoClXjZwR2m_fg")
 };
 static void Check(bool ok,string why){if(!ok)throw new InvalidOperationException("PINK_GRID_UNMARKED_EDIT: "+why);}
 static AssistedRoadNetwork.Path Path(RoadMapLane lane){return new AssistedRoadNetwork.Path(lane.points.Select((p,i)=>p.V(lane.heights!=null&&i<lane.heights.Length?lane.heights[i]:0)).ToArray());}
 static RoadMapEvidence Evidence(Group group){return new RoadMapEvidence{state="observed",appearanceConfirmed=true,month="2026-05",checkedAt="2026-11-16",url="https://map.naver.com/p?c=19,0,0,0,adh&p="+group.panos.Split(',')[0]+",0,-15,80,Float",note=group.id+": latest May2026 contiguous views show no centre/divider/turn arrows on this living street in both directions. Pano chain: "+group.panos+". P08 ends at P07 pedestrian passage; P07 is not modified. P16/P19 school E-W roads retain yellow centre/bollards. Global lineage station ranges; suppression width derives from existing opposing-centre spacing+0.30m paint allowance, not surveyed width. Road/graph/height remain unchanged; raised intersections and speed bumps are not implemented."};}
 public static RoadMapDocument Preview(ProjectMapPackage package){
  if(package==null||package.document==null)throw new ArgumentNullException("package");var d=package.document.Copy();Check(!d.facilities.Any(f=>f.id.StartsWith(Prefix,StringComparison.Ordinal)),"already applied or partial preview");
  var added=new List<RoadMapFacility>();
  foreach(var group in Groups)foreach(var id in new[]{group.a,group.b}){
   var lane=d.lanes.Single(l=>l.id==id);var opposite=d.lanes.Single(l=>l.id==(id==group.a?group.b:group.a));
   Check(!lane.disabled&&lane.turn==0&&lane.change==0&&lane.points!=null&&lane.points.Length>=2,"active through lane expected "+id);
   Check(d.lanes.Count(l=>!l.disabled&&RoadMapAuthoringOperations.Origin(l)==id)==1,"split lineage requires renewed scope review "+id);
   Check(!d.facilities.Any(f=>f.laneId==id&&f.kind=="lanePresentation"),"existing presentation must be reconciled "+id);
   var path=Path(lane);var other=Path(opposite);float separation=0;
   for(float at=0;at<path.length;at+=1){float error;other.Project(path.Position(at),out error);separation=Mathf.Max(separation,error);}float endError;other.Project(path.Position(path.length),out endError);separation=Mathf.Max(separation,endError);
   Check(separation>0&&separation<8,"unexpected opposite-centre separation "+id);float width=Mathf.Max(lane.width,separation+.30f);var middle=path.Position(path.length*.5f);
   added.Add(new RoadMapFacility{id=Prefix+group.id+":lines:"+id,kind="lanePresentation",type=ReviewedUnmarkedRoadPaint.FeatureType,laneId=id,junctionId=lane.ruleId,startAt=lane.sourceOffset,endAt=lane.sourceOffset+path.length,width=width,length=1,scale=1,rule=false,x=middle.x,z=middle.z,evidence=Evidence(group)});
   added.Add(new RoadMapFacility{id=Prefix+group.id+":arrows:"+id,kind="lanePresentation",type=LaneDirectionPaint.NoArrowsType,laneId=id,junctionId=lane.ruleId,rule=false,x=lane.points.Last().x,z=lane.points.Last().z,evidence=Evidence(group)});
  }
  d.facilities=d.facilities.Concat(added).ToArray();return d;
 }
 public static void Apply(){
  Check(RequiredRevision>=0&&RequiredContentHash.Length==64&&RequiredContentHash.All(Uri.IsHexDigit),"Apply is disabled until the reviewed revision/content hash is explicitly pinned");
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);var before=ProjectMapStore.Load(root);
  Check(before.document.revision==RequiredRevision&&before.contentHash==RequiredContentHash,"Apply requires the exact pinned map; changed/already-applied maps must be reassessed");
  string original=JsonUtility.ToJson(before);var after=Preview(before);
  Check(original==JsonUtility.ToJson(before),"preview changed original package");
  after.revision=RequiredRevision+1;after.savedAt=DateTime.UtcNow.ToString("o");after.projectRevision="";
  PinkGridUnmarkedMapTests.Validate(before,after);
  var candidate=ProjectMapPackage.Create(before.baseline,after);
  Check(candidate.baselineHash==before.baselineHash,"baseline fingerprint changed");
  Check(original==JsonUtility.ToJson(before),"validation/package creation changed original package");
  ProjectMapStore.Save(root,candidate,token);
  var loaded=ProjectMapStore.Load(root);
  Check(loaded.document.revision==RequiredRevision+1&&loaded.contentHash==candidate.contentHash&&loaded.baselineHash==before.baselineHash,"persisted revision/content/baseline fingerprint mismatch");
  Check(loaded.document.ToJson()==candidate.document.ToJson(),"persisted document differs from validated candidate");
  PinkGridUnmarkedMapTests.Validate(before,loaded.document);
  ProjectMapBuild.Prepare();
  Debug.Log("PINK_GRID_UNMARKED_MAP_APPLIED revision="+loaded.document.revision+" contentHash="+loaded.contentHash+" baselineHash="+loaded.baselineHash+" groups8 directions16 appearance32 graph/width/stops-preserved");
 }
}
