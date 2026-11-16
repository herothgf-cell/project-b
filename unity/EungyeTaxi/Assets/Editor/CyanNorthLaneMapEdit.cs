using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

// Explicit, deterministic preview. No runtime hook and no write until Apply.
// Field evidence establishes lane count/movement, not metre-accurate geometry.
public static class CyanNorthLaneMapEdit {
 public const string Prefix="reviewed:cyan-north:inner:";
 public static readonly string[] North={"local:8824:10924:7438:12714","local:7388:12954:7347:13670","local:7335:13923:7310:14570"};
 public static readonly string[] South={"local:7346:12686:8782:10837","local:7250:13665:7291:12941","local:7213:14566:7238:13919"};
 static readonly string[] Panos={"jFGgn9NnhikzoVgrM25uOQ","49BZzsptK-aM18ceIgg3Xw","xLEUJnDRhw1QuTbU-YfD_g"};
 public static string Inner(string outer){return Prefix+outer;}
 public static int[] InnerTurns(int group){return new[]{-1,0};}
 public static int[] OuterTurns(int group){return group==2?new[]{1}:new[]{0};}
 static T Copy<T>(T value){return JsonUtility.FromJson<T>(JsonUtility.ToJson(value));}
 static void Check(bool ok,string reason){if(!ok)throw new InvalidOperationException("CYAN_NORTH_EDIT: "+reason);}
 static MapPoint P(Vector3 p){return new MapPoint{x=p.x,z=p.z,elevation=p.y};}
 static AssistedRoadNetwork.Path Path(RoadMapLane lane){return new AssistedRoadNetwork.Path(lane.points.Select((p,i)=>p.V(lane.heights!=null&&i<lane.heights.Length?lane.heights[i]:0)).ToArray());}
 static RoadMapEvidence Evidence(int group){return new RoadMapEvidence{state="observed",appearanceConfirmed=true,month="2026-05",checkedAt="2026-11-13",url="https://map.naver.com/p?c=19,0,0,0,adh&p="+Panos[group]+",0,-20,80,Float",note="Cyan north J8921→J7355→J7292→J7254: north2 / south1, per-approach arrows verified separately. Lane width3.5m, road11m and connector geometry are game estimates, not survey measurements."};}
 public static bool SelectedRoad(ProjectMapObject road){return road.kind=="road"&&road.id.StartsWith("road:",StringComparison.Ordinal)&&RoadviewCorridorPresentation.CyanNorthSource(road.id.Substring(5));}
 public static RoadMapDocument Preview(ProjectMapPackage package){
  var d=package.document.Copy();Check(d.scene!=null,"authored scene required");
  Check(!d.lanes.Any(l=>l.id.StartsWith(Prefix,StringComparison.Ordinal)),"already applied or partial previous operation");
  var old=package.document.lanes.ToDictionary(l=>l.id);var lanes=d.lanes.ToDictionary(l=>l.id);var added=new List<RoadMapLane>();
  var innerByOuter=new Dictionary<string,RoadMapLane>();var shifted=new HashSet<string>();
  for(int group=0;group<3;group++){
   var outer=lanes[North[group]];var south=lanes[South[group]];
   Check(!outer.disabled&&!south.disabled&&outer.number==1&&south.number==1,"expected intact one-per-direction base "+outer.id);
   var np=Path(old[outer.id]);var sp=Path(old[south.id]);var inner=Copy(outer);
   inner.id=Inner(outer.id);inner.fromNode=inner.id+":start";inner.toNode=inner.id+":end";inner.number=1;inner.next=new string[0];
   Recenter(outer,np,sp,3.5f);Recenter(inner,np,sp,0);Recenter(south,sp,np,3.5f);
   outer.number=2;foreach(var lane in new[]{outer,inner,south}){lane.width=3.5f;lane.evidence=Evidence(group);shifted.Add(lane.id);}
   added.Add(inner);innerByOuter.Add(outer.id,inner);
  }
  foreach(var lane in added)lanes.Add(lane.id,lane);
  // Move every touching existing connector's endpoints, preserving ID/order.
  foreach(var join in d.lanes.Where(l=>!l.disabled&&l.id.StartsWith("join:")&&l.change==0)){
   var incoming=d.lanes.Where(l=>!l.disabled&&(l.next??new string[0]).Contains(join.id)).ToArray();
   var exits=(join.next??new string[0]).Where(lanes.ContainsKey).Select(id=>lanes[id]).ToArray();
   if(!incoming.Any(l=>shifted.Contains(l.id))&&!exits.Any(l=>shifted.Contains(l.id)))continue;
   Check(incoming.Length==1&&exits.Length==1,"ambiguous touching connector "+join.id);
   Anchor(join,incoming[0],exits[0]);
  }
  // Existing left connector IDs move to the inner lane; outer IDs and their
  // straight/right connectors survive. Duplicate straight only where observed.
  for(int group=0;group<3;group++){
   var outer=lanes[North[group]];var inner=innerByOuter[outer.id];var innerNext=new List<string>();var outerNext=new List<string>();
   foreach(string nextId in old[outer.id].next){
    var join=lanes[nextId];Check(!join.disabled&&join.change==0&&join.id.StartsWith("join:"),"unexpected north exit "+nextId);
    Check(join.next.Length==1,"ambiguous exit "+nextId);var exit=lanes[join.next[0]];
    bool i=InnerTurns(group).Contains(join.turn),o=OuterTurns(group).Contains(join.turn);
    Check(i||o,"existing movement not covered by field roles "+nextId);
    if(o){Anchor(join,outer,exit);outerNext.Add(join.id);}
    if(i){
     var insideExit=join.turn==0&&innerByOuter.ContainsKey(exit.id)?innerByOuter[exit.id]:exit;
     var inside=o?Copy(join):join;
     if(o){inside.id="join:reviewed:cyan-north:inner:"+group+":"+join.turn;added.Add(inside);lanes.Add(inside.id,inside);}
     Anchor(inside,inner,insideExit);innerNext.Add(inside.id);
    }
   }
   inner.next=innerNext.ToArray();outer.next=outerNext.ToArray();
  }
  // Every outside approach that previously entered the north lane also gets
  // an entry into the new inner lane. Internal north-through pairs are already
  // one-to-one above, so do not fan them across the opposing connector.
  for(int group=0;group<3;group++){
   string target=North[group];var inner=innerByOuter[target];
   foreach(var originalJoin in package.document.lanes.Where(l=>!l.disabled&&l.id.StartsWith("join:")&&l.change==0&&l.next.Contains(target))){
    var originalEntries=package.document.lanes.Where(l=>!l.disabled&&l.next.Contains(originalJoin.id)).ToArray();
    if(originalEntries.Any(l=>North.Contains(l.id)))continue;
    Check(originalEntries.Length==1,"ambiguous upstream entry "+originalJoin.id);
    var entry=lanes[originalEntries[0].id];var copy=Copy(lanes[originalJoin.id]);copy.id="join:reviewed:cyan-north:entry:"+group+":"+originalJoin.id;
    Anchor(copy,entry,inner);added.Add(copy);entry.next=entry.next.Concat(new[]{copy.id}).Distinct().ToArray();
   }
  }
  d.lanes=d.lanes.Concat(added).ToArray();
  // User and May 2026 pano confirm NO painted arrows at J7292. Keep the
  // existing left/straight permissions; absence of paint is not prohibition.
  foreach(string id in new[]{North[1],Inner(North[1])}){
   var lane=lanes[id];var evidence=Evidence(1);evidence.note="Cyan north J7292: user-confirmed no painted direction arrows, panorama49BZzsptK-aM18ceIgg3Xw May2026 heading5.79 tilt-15.38. Existing L/S connectivity retained; unmarked pavement does not prohibit movement.";
   lane.evidence=Copy(evidence);var position=lane.points.Last();
   d.facilities=d.facilities.Concat(new[]{new RoadMapFacility{id="reviewed:cyan-north:no-arrows:"+id,kind="lanePresentation",type=LaneDirectionPaint.NoArrowsType,laneId=id,junctionId=lane.ruleId,x=position.x,z=position.z,rule=false,evidence=evidence}}).ToArray();
  }
  var roads=d.scene.objects.Where(SelectedRoad).ToArray();Check(roads.Length==9,"nine source road ribbons required, actual "+roads.Length);
  foreach(var road in roads){road.width=11;road.lanes=3;road.oneWay=false;}
  foreach(var ribbon in d.scene.objects.Where(o=>North.Concat(South).Any(id=>o.id=="road:polish:"+id)))ribbon.enabled=false;
  // Preview is deterministic: timestamps/revision are assigned only on save.
  return d;
 }
 static void Recenter(RoadMapLane lane,AssistedRoadNetwork.Path own,AssistedRoadNetwork.Path opposite,float offset){
  var positions=new MapPoint[lane.points.Length];float walked=0;
  for(int i=0;i<positions.Length;i++){
   if(i>0)walked+=Vector3.Distance(own.points[i-1],own.points[i]);
   var point=own.points[i];var right=TrafficSystem.Right(own.Tangent(walked));float error;
   float otherAt=opposite.Project(point,out error);float separation=Vector3.Dot(point-opposite.Position(otherAt),right);
   Check(separation>5&&separation<15,"unexpected opposing centre separation "+lane.id+" "+separation);
   positions[i]=P(point+right*(offset-separation*.5f));
  }
  lane.points=positions;
 }
 static void Anchor(RoadMapLane join,RoadMapLane from,RoadMapLane to){
  var original=join.points.Select(p=>p.V()).ToArray();var path=new AssistedRoadNetwork.Path(original);float walked=0;
  var start=from.points.Last().V()-original[0];var end=to.points[0].V()-original.Last();
  join.points=original.Select((p,i)=>{if(i>0)walked+=Vector3.Distance(original[i-1],p);return P(p+Vector3.Lerp(start,end,Mathf.SmoothStep(0,1,walked/Mathf.Max(.001f,path.length))));}).ToArray();
  join.fromNode=from.toNode;join.toNode=to.fromNode;join.next=new[]{to.id};join.width=3.5f;join.number=to.number;
  int group=Array.IndexOf(North,from.id);if(group<0)group=Array.FindIndex(North,id=>Inner(id)==from.id);if(group<0)group=0;
  join.evidence=Evidence(group);
 }
 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);var p=ProjectMapStore.Load(root);var d=Preview(p);
  CyanNorthLaneMapTests.Validate(p,d);d.revision++;d.savedAt=DateTime.UtcNow.ToString("o");d.projectRevision="";
  var result=ProjectMapPackage.Create(p.baseline,d);Check(result.baselineHash==p.baselineHash,"baseline changed");
  ProjectMapStore.Save(root,result,token);ProjectMapBuild.Prepare();CyanNorthLaneMapTests.Verify();Debug.Log("CYAN_NORTH_MAP_APPLIED "+result.contentHash);
 }
}
