using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Preview is read-only. Apply is an explicit, exact-r20-guarded authoring action.
// No runtime hook or global importer change.
// The original 40.18695m corridor was skipped by AssistedRoadNetwork's
// 16.5m + 16.5m + 8m = 41m trim threshold. Both intersections remain separate.
public static class ShortCorridorGapEdit {
 public const string Prefix="reviewed:daeun-short-gap:";
 public const string RequiredContentHash="7bf7fb04facef74f2c4027ac93cdbc24244a278a5980f805854c70611e2cc35b";
 public static readonly string[] SourceIds={"37612022:A0020000:1086:0","37612022:A0020000:1094:0","37612022:A0020000:1786:0"};
 public static readonly string[] NorthEastIncoming={"local:33:10578:1758:11679:part:2","local:65:10524:1791:11626:part:2"};
 public static readonly string[] NorthEastOutgoing={"local:2383:12059:2455:12108","local:2419:12002:2497:12055"};
 public static readonly string[] SouthWestIncoming={"local:2414:12161:2348:12117","local:2372:12214:2312:12174"};
 public static readonly string[] SouthWestOutgoing={"local:1725:11732:1:10631","local:1692:11785:-31:10685"};
 public static string LaneId(int direction,int lane){return Prefix+(direction==0?"ne:":"sw:")+(lane+1);}
 public static string JoinId(int direction,int lane,bool entry){return "join:"+LaneId(direction,lane)+(entry?":entry":":exit");}
 static void Check(bool ok,string reason){if(!ok)throw new InvalidOperationException("SHORT_CORRIDOR_EDIT: "+reason);}
 static T Copy<T>(T value){return JsonUtility.FromJson<T>(JsonUtility.ToJson(value));}
 static MapPoint Point(Vector3 p){return new MapPoint{x=p.x,z=p.z,elevation=p.y};}
 static RoadMapEvidence Evidence(){return new RoadMapEvidence{state="game",checkedAt="2026-11-13",appearanceConfirmed=false,ruleConfirmed=false,note="Targeted source-continuity restoration: NGII source1086/1094/1786 exists but 40.18695m < importer trim threshold41m. Field traversal supports physical mainline continuity; exact lane distribution/turn law still under survey. Two lanes per direction retain neighboring authored counts, width3.5m and16m trims are game estimates. No new side turns or U-turns."};}
 public static RoadMapDocument Preview(ProjectMapPackage package){
  var d=package.document.Copy();
  Check(!d.lanes.Any(l=>l.id.StartsWith(Prefix)||l.id.StartsWith("join:"+Prefix)),"already present/partial repair");
  Check(!d.lanes.Any(l=>(l.sourceId??"").Split(',').Any(SourceIds.Contains)),"source corridor now has lanes; reassess instead of duplicating");
  var sourcePoints=new List<Vector3>();float width=27;
  foreach(string id in SourceIds){
   var road=package.baseline.roads.Single(r=>r.id==id);
   Check(road.oneWay==0&&Mathf.Abs(road.width-width)<.01f,"source width/direction changed "+id);
   foreach(var p in road.points){var v=p.V();if(sourcePoints.Count==0||Vector3.Distance(sourcePoints.Last(),v)>.001f)sourcePoints.Add(v);}
  }
  var centre=new AssistedRoadNetwork.Path(sourcePoints.ToArray());
  Check(centre.length>40&&centre.length<41,"source length no longer short-corridor case "+centre.length);
  Check(Vector3.Distance(sourcePoints.First(),package.baseline.junctionRules.Single(j=>j.id=="J1882_11791").V())<.02f,"source start");
  Check(Vector3.Distance(sourcePoints.Last(),package.baseline.junctionRules.Single(j=>j.id=="J2225_12001").V())<.02f,"source end");
  var lookup=d.lanes.ToDictionary(l=>l.id);var additions=new List<RoadMapLane>();
  for(int direction=0;direction<2;direction++){
   var path=new AssistedRoadNetwork.Path(direction==0?sourcePoints.ToArray():sourcePoints.AsEnumerable().Reverse().ToArray());
   string upstream=direction==0?"J1882_11791":"J2225_12001",downstream=direction==0?"J2225_12001":"J1882_11791";
   for(int lane=0;lane<2;lane++){
    var input=lookup[direction==0?NorthEastIncoming[lane]:SouthWestIncoming[lane]];
    var output=lookup[direction==0?NorthEastOutgoing[lane]:SouthWestOutgoing[lane]];
    Check(!input.disabled&&!output.disabled&&input.number==lane+1&&output.number==lane+1,"neighbor lane identity/count changed");
    Check(input.ruleId==upstream,"unexpected approach junction "+input.id);
    var mid=Copy(input);mid.id=LaneId(direction,lane);mid.fromNode=mid.id+":start";mid.toNode=mid.id+":end";
    mid.sourceId=string.Join(",",SourceIds);mid.sourceOffset=0;mid.ruleId=downstream;mid.status="provisional";mid.width=3.5f;mid.turn=0;mid.change=0;mid.disabled=false;
    const float trim=16f;float offset=(lane+.5f)*width/4f;
    int samples=Mathf.CeilToInt((path.length-2*trim)/.5f);var points=new MapPoint[samples+1];
    for(int k=0;k<=samples;k++){float at=Mathf.Lerp(trim,path.length-trim,k/(float)samples);points[k]=Point(path.Position(at)+TrafficSystem.Right(path.Tangent(at))*offset);}
    mid.points=points;mid.heights=new float[points.Length];mid.evidence=Evidence();
    var entry=Join(JoinId(direction,lane,true),input,mid,upstream);var exit=Join(JoinId(direction,lane,false),mid,output,downstream);
    mid.next=new[]{exit.id};input.next=input.next.Concat(new[]{entry.id}).ToArray();
    additions.Add(mid);additions.Add(entry);additions.Add(exit);
   }
  }
  d.lanes=d.lanes.Concat(additions).ToArray();return d;
 }
 static RoadMapLane Join(string id,RoadMapLane from,RoadMapLane to,string junction){
  var a=from.points.Last().V();var b=to.points.First().V();
  var forward=(a-from.points[from.points.Length-2].V()).normalized;var exit=(to.points[1].V()-b).normalized;
  float handle=Vector3.Distance(a,b)/3f;var c=a+forward*handle;var e=b-exit*handle;
  int samples=Mathf.Max(16,Mathf.CeilToInt(Vector3.Distance(a,b)/.5f));var points=new MapPoint[samples+1];
  for(int i=0;i<=samples;i++){float t=i/(float)samples,u=1-t;points[i]=Point(u*u*u*a+3*u*u*t*c+3*u*t*t*e+t*t*t*b);}
  return new RoadMapLane{id=id,sourceId=string.Join(",",SourceIds),fromNode=from.toNode,toNode=to.fromNode,ruleId=junction,roadName="대은로",status="provisional",number=to.number,turn=0,change=0,width=3.5f,points=points,heights=new float[points.Length],next=new[]{to.id},evidence=Evidence()};
 }
 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);
  var p=ProjectMapStore.Load(root);
  Check(p.document.revision==20&&p.contentHash==RequiredContentHash,"Apply requires exact reviewed r20 content hash; reassess changed/already-applied map");
  // Reuse the full deterministic/identity-preservation checks before any write.
  ShortCorridorGapTests.PreviewChecks();
  var d=Preview(p);ShortCorridorGapTests.Validate(p,d);
  d.revision=21;d.savedAt=DateTime.UtcNow.ToString("o");d.projectRevision="";
  var result=ProjectMapPackage.Create(p.baseline,d);
  Check(result.baselineHash==p.baselineHash,"baseline changed");
  ProjectMapStore.Save(root,result,token);
  ProjectMapBuild.Prepare();ShortCorridorGapTests.Verify();
  var saved=ProjectMapStore.Load(root);
  Check(saved.document.revision==21&&saved.contentHash==result.contentHash&&saved.baselineHash==p.baselineHash,"persisted package fingerprint mismatch");
  Debug.Log("SHORT_CORRIDOR_MAP_APPLIED revision="+saved.document.revision+" contentHash="+saved.contentHash+" baselineHash="+saved.baselineHash+" direct=4 stops=39 pairs=1482 counts=neighbor-provisional");
 }
}
