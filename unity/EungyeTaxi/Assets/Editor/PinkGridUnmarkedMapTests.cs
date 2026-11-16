using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PinkGridUnmarkedMapTests {
 sealed class Stroke {public string kind;public Vector3 position,direction;public float width,length;public bool yellow;public string Key {get{return kind+"|"+position.ToString("F4")+"|"+direction.ToString("F4")+"|"+width.ToString("F4")+"|"+length.ToString("F4")+"|"+yellow;}}}
 static void Check(bool ok,string why){if(!ok)throw new InvalidOperationException("PINK_GRID_UNMARKED: "+why);}
 static AssistedRoadNetwork.Path Path(RoadMapLane lane){return new AssistedRoadNetwork.Path(lane.points.Select(p=>p.V()).ToArray());}
 static List<Stroke> Paint(MapData map){var lines=new List<Stroke>();RoadPaintGeometry.Paint(map,null,(n,p,d,w,l,y)=>lines.Add(new Stroke{kind=n,position=p,direction=d,width=w,length=l,yellow=y}));return lines;}
 static bool Near(Stroke stroke,AssistedRoadNetwork.Path path,bool interior){float error;float at=path.Project(stroke.position,out error);return error<8.5f&&Mathf.Abs(Vector3.Dot(stroke.direction,path.Tangent(at)))>.85f&&(!interior||at>path.length*.2f&&at<path.length*.8f);}
 static void Diagnose(Stroke stroke,PinkGridUnmarkedMapEdit.Group group,RoadMapDocument document,MapData map){
  Debug.Log("PINK_GRID_RETAINED "+group.id+" "+stroke.Key);
  foreach(string id in new[]{group.a,group.b}){
   var lane=document.lanes.Single(l=>l.id==id);var path=Path(lane);var metadata=document.facilities.Single(f=>f.laneId==id&&ReviewedUnmarkedRoadPaint.IsFeature(f));
   foreach(float t in new[]{-.5f,0,.5f})foreach(float side in new[]{-.5f,.5f}){var q=stroke.position+stroke.direction*(stroke.length*t)+TrafficSystem.Right(stroke.direction)*(stroke.width*side);float error;float at=path.Project(q,out error);var tangent=path.Tangent(at);var delta=q-path.Position(at);Debug.Log("PINK_GRID_CORNER lane="+id+" station="+at+"/"+path.length+" error="+error+" along="+Vector3.Dot(delta,tangent)+" lateral="+Vector3.Dot(delta,TrafficSystem.Right(tangent))+" align="+Mathf.Abs(Vector3.Dot(stroke.direction,tangent))+" bandHalf="+metadata.width*.5f+" point="+q.ToString("F4"));}
  }
  var nearest=(map.roads??new MapRoad[0]).Where(r=>r.points!=null&&r.points.Length>1).Select(r=>{float error;new AssistedRoadNetwork.Path(r.points.Select(p=>p.V()).ToArray()).Project(stroke.position,out error);return new{r.id,error};}).OrderBy(r=>r.error).Take(4);Debug.Log("PINK_GRID_NEAREST_ROADS "+string.Join(" / ",nearest.Select(r=>r.id+" error="+r.error)));
 }
 public static void PreviewChecks(){
  var package=ProjectMapStore.Load(ProjectMapBuild.Root);string original=JsonUtility.ToJson(package);var d=PinkGridUnmarkedMapEdit.Preview(package);
  Check(d.ToJson()==PinkGridUnmarkedMapEdit.Preview(package).ToJson(),"preview not deterministic");
  Validate(package,d);Check(original==JsonUtility.ToJson(package),"input package/baseline modified");
  Debug.Log("PINK_GRID_UNMARKED_PREVIEW_OK groups8 directions16 facilities32 graph/width/stops unchanged; P07/P16/P19 excluded; no save");
 }
 public static void Validate(ProjectMapPackage package,RoadMapDocument d){
  string original=JsonUtility.ToJson(package);var expectedDocument=PinkGridUnmarkedMapEdit.Preview(package);var normalized=d.Copy();
  normalized.revision=expectedDocument.revision;normalized.savedAt=expectedDocument.savedAt;normalized.projectRevision=expectedDocument.projectRevision;
  Check(normalized.ToJson()==expectedDocument.ToJson(),"only the exact 32 appearance additions and save bookkeeping may change");
  var expected=new[]{"P08","P09","P17","P23","P24","P25","P27","P28"};Check(PinkGridUnmarkedMapEdit.Groups.Select(g=>g.id).SequenceEqual(expected),"exact approved eight groups");
  var ids=PinkGridUnmarkedMapEdit.Groups.SelectMany(g=>new[]{g.a,g.b}).ToArray();Check(ids.Length==16&&ids.Distinct().Count()==16,"sixteen directed segments");
  Check(d.lanes.Length==package.document.lanes.Length,"lane count changed");for(int i=0;i<d.lanes.Length;i++)Check(JsonUtility.ToJson(d.lanes[i])==JsonUtility.ToJson(package.document.lanes[i]),"geometry/width/graph/source changed "+d.lanes[i].id);
  Check(JsonUtility.ToJson(d.scene)==JsonUtility.ToJson(package.document.scene),"scene roads/heights/services modified");Check(d.signals.Length==package.document.signals.Length,"signal count changed");for(int i=0;i<d.signals.Length;i++)Check(JsonUtility.ToJson(d.signals[i])==JsonUtility.ToJson(package.document.signals[i]),"signal program changed");
  Check(d.facilities.Length==package.document.facilities.Length+32,"exactly 32 new appearance declarations");foreach(var old in package.document.facilities)Check(JsonUtility.ToJson(old)==JsonUtility.ToJson(d.facilities.Single(f=>f.id==old.id)),"existing facility overwritten "+old.id);
  var added=d.facilities.Where(f=>f.id.StartsWith(PinkGridUnmarkedMapEdit.Prefix,StringComparison.Ordinal)).ToArray();Check(added.Length==32&&added.All(f=>ids.Contains(f.laneId)&&!f.rule),"declaration scope leaked outside approved groups");
  foreach(string id in ids){var lane=d.lanes.Single(l=>l.id==id);var unmarked=added.Single(f=>f.laneId==id&&ReviewedUnmarkedRoadPaint.IsFeature(f));var arrows=added.Single(f=>f.laneId==id&&LaneDirectionPaint.IsArrowSuppression(f));Check(unmarked.startAt==lane.sourceOffset&&Mathf.Abs(unmarked.endAt-lane.sourceOffset-Path(lane).length)<.01f&&unmarked.width<8.31f,"bounded whole active piece "+id);Check(arrows.junctionId==lane.ruleId,"arrow suppression junction mismatch");}
  var errors=RoadMapValidator.Validate(package.baseline,d).Where(e=>e.error).ToArray();Check(errors.Length==0,string.Join(" / ",errors.Take(10).Select(e=>e.ToString())));
  var beforeMap=package.Compose();var map=RoadMapComposer.Compose(package.baseline,d);var before=Paint(beforeMap);var after=Paint(map);int priorMarkings=0;
  foreach(var group in PinkGridUnmarkedMapEdit.Groups){var path=Path(d.lanes.Single(l=>l.id==group.a));int was=before.Count(s=>Near(s,path,true)&&(s.kind=="Centre line"||s.kind=="Lane divider"));var retained=after.Where(s=>Near(s,path,true)&&(s.kind=="Centre line"||s.kind=="Lane divider")).ToArray();int now=retained.Length;foreach(var stroke in retained)Diagnose(stroke,group,d,map);Check(now==0,group.id+" interior retains centre/divider paint: "+now);priorMarkings+=was;Debug.Log("PINK_GRID_PAINT "+group.id+" interior before="+was+" after="+now);}
  Check(priorMarkings>0,"actual current wrong synthetic paint covered by regression");
  Check(before.Where(s=>s.kind=="Road edge").Select(s=>s.Key).SequenceEqual(after.Where(s=>s.kind=="Road edge").Select(s=>s.Key)),"road edges changed anywhere");
  var paths=ids.Select(id=>Path(d.lanes.Single(l=>l.id==id))).ToArray();Func<Stroke,bool> outside=s=>!paths.Any(path=>Near(s,path,false));Check(before.Where(outside).Select(s=>s.Key).SequenceEqual(after.Where(outside).Select(s=>s.Key)),"paint changed outside bounded selected streets");
  var oldArrows=LaneDirectionPaint.Layout(beforeMap);var arrowsAfter=LaneDirectionPaint.Layout(map);Check(!arrowsAfter.Any(a=>ids.Contains(map.hdEdges[a.edgeIndex].id)),"selected street arrows remain");Check(oldArrows.Where(a=>!ids.Contains(beforeMap.hdEdges[a.edgeIndex].id)).Select(a=>beforeMap.hdEdges[a.edgeIndex].id+":"+a.turns).OrderBy(x=>x).SequenceEqual(arrowsAfter.Where(a=>!ids.Contains(map.hdEdges[a.edgeIndex].id)).Select(a=>map.hdEdges[a.edgeIndex].id+":"+a.turns).OrderBy(x=>x)),"outside legal arrow masks changed");
  Check(beforeMap.stops.Length==map.stops.Length,"stop count changed");for(int i=0;i<map.stops.Length;i++)Check(JsonUtility.ToJson(beforeMap.stops[i])==JsonUtility.ToJson(map.stops[i]),"stop identity/position/binding changed");
  Check(original==JsonUtility.ToJson(package),"validation modified original package");
  Debug.Log("PINK_GRID_UNMARKED_VALIDATE_OK groups8 directions16 facilities32 graph/width/stops unchanged; P07/P16/P19 excluded");
 }
}
