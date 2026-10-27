using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Explicit authoring operation for the reported J7973 -> J8066 corridor.
// Never runs at startup or changes route snapping. The caller supplies the
// observed separator evidence and persists the resulting project document.
public static class PlaytestLaneChangeRepair {
 public const string Prefix="reviewed:playtest:lane-change:";
 static readonly string[] LaneIds={"local:8017:3985:8089:6162:part:2","local:8074:3981:8146:6160:part:2","local:8131:3977:8203:6159:part:2"};
 sealed class Window {public string from,to;public float start,end;public int side;public MapPoint[] points;public RoadMapEvidence evidence;}
 static AssistedRoadNetwork.Path Path(RoadMapLane lane){return new AssistedRoadNetwork.Path(lane.points.Select((p,i)=>p.V(lane.heights!=null&&i<lane.heights.Length?lane.heights[i]:0)).ToArray());}
 static MapPoint P(Vector3 p){return new MapPoint{x=p.x,z=p.z};}
 static string Number(float value){return value.ToString("F3",System.Globalization.CultureInfo.InvariantCulture);}
 static RoadMapLane Split(RoadMapDocument doc,string origin,float at,bool upstream){
  var lane=RoadMapAuthoringOperations.LineageSegment(doc,origin,at);if(lane==null)throw new InvalidOperationException("Lane-change cut outside lane "+origin);
  float local=at-lane.sourceOffset;var path=Path(lane);
  if(local>.01f&&local<path.length-.01f){
   var points=lane.points.ToList();var heights=Enumerable.Range(0,lane.points.Length).Select(i=>lane.heights!=null&&i<lane.heights.Length?lane.heights[i]:0f).ToList();float walked=0;int point=1;
   for(;point<path.points.Length;point++){float next=walked+Vector3.Distance(path.points[point-1],path.points[point]);if(next>=local-.0001f)break;walked=next;}
   if(Mathf.Abs(walked-local)<.001f)point--;
   else if(Mathf.Abs(walked+Vector3.Distance(path.points[point-1],path.points[point])-local)>.001f){var position=path.Position(local);points.Insert(point,P(position));heights.Insert(point,position.y);lane.points=points.ToArray();lane.heights=heights.ToArray();}
   RoadMapAuthoringOperations.SplitLane(doc,lane.id,point);
  }
  return doc.lanes.Where(l=>RoadMapAuthoringOperations.Origin(l)==origin).First(l=>Mathf.Abs((upstream?l.sourceOffset+Path(l).length:l.sourceOffset)-at)<.01f);
 }
 static void RequireEvidence(RoadMapEvidence evidence){if(evidence==null||!evidence.appearanceConfirmed||string.IsNullOrWhiteSpace(evidence.url)||string.IsNullOrWhiteSpace(evidence.checkedAt))throw new ArgumentException("Observed dashed-separator evidence is required");}
 static Window Plan(RoadMapLane from,RoadMapLane to,float start,int side,float endMargin,RoadMapEvidence evidence,float distance=32){
  var a=Path(from);var b=Path(to);float error,targetStart=b.Project(a.Position(start),out error),end=targetStart+distance;
  if(from.disabled||to.disabled||from.change!=0||to.change!=0||from.turn!=0||to.turn!=0||to.number-from.number!=side||start+distance>a.length-endMargin||end>b.length-endMargin||error<2||error>7||Vector3.Dot(a.Tangent(start),b.Tangent(targetStart))<.98f||Vector3.Dot(b.Position(targetStart)-a.Position(start),TrafficSystem.Right(a.Tangent(start)))*side<2)throw new InvalidOperationException("Lane-change geometry is not an adjacent forward window");
  var points=new MapPoint[49];for(int k=0;k<points.Length;k++){float t=k/(float)(points.Length-1),blend=t*t*t*(t*(t*6-15)+10);points[k]=P(Vector3.Lerp(a.Position(start+distance*t),b.Position(targetStart+distance*t),blend));}
  // Imported HD capture slices already carry a nonzero sourceOffset. The
  // editor lineage API takes global source distance, while Path takes local.
  return new Window{from=from.id,to=to.id,start=from.sourceOffset+start,end=to.sourceOffset+end,side=side,points=points,evidence=evidence};
 }
 public static RoadMapDocument Apply(ProjectMapPackage package,RoadMapEvidence northEvidence,RoadMapEvidence westEvidence){
  RequireEvidence(northEvidence);RequireEvidence(westEvidence);
  var doc=package.document.Copy();int existing=doc.lanes.Count(l=>l.id.StartsWith(Prefix));if(existing==14)return doc;if(existing>0)throw new InvalidOperationException("Incomplete previous lane-change authoring");
  var lanes=LaneIds.Select(id=>doc.lanes.Single(l=>l.id==id&&!l.disabled&&l.turn==0&&l.change==0)).ToArray();
  var windows=new List<Window>();
  for(int i=0;i<lanes.Length;i++)foreach(int side in new[]{-1,1}){
   int j=i+side;if(j<0||j>=lanes.Length)continue;
   if(lanes[i].sourceId!=lanes[j].sourceId||lanes[j].number-lanes[i].number!=side)throw new InvalidOperationException("Lane-change family/adjacency mismatch");
   foreach(float start in new[]{20f,70f,120f})windows.Add(Plan(lanes[i],lanes[j],start,side,20,northEvidence));
  }
  // Captured NGII segments retain their original directed next links. Change
  // twice before the existing lane-1 fork into the captured left-turn pocket.
  // These are 84m capture slices, not junction approaches; the actual stop
  // line is over 120m farther along the authored pocket continuation.
  var outer=doc.lanes.Single(l=>l.id=="A2218G611417:3");var middle=doc.lanes.Single(l=>l.id=="A2218G611415:3");var inner=doc.lanes.Single(l=>l.id=="A2218G611416:2");
  windows.Add(Plan(outer,middle,12,-1,8,westEvidence,24));windows.Add(Plan(middle,inner,48,-1,8,westEvidence,24));
  // Split all endpoints before wiring changes. Existing turns/signals/service
  // distances are remapped by the same authoring operations as the map editor.
  foreach(var window in windows){Split(doc,window.from,window.start,true);Split(doc,window.to,window.end,false);}
  foreach(var window in windows){
   var from=Split(doc,window.from,window.start,true);var to=Split(doc,window.to,window.end,false);
   string id=RoadMapAuthoringOperations.Connect(doc,from.id,to.id,0,window.side);var change=doc.lanes.Single(l=>l.id==id);
   string stable=Prefix+window.from+":"+window.to+":"+Number(window.start);
   from.next=from.next.Select(n=>n==id?stable:n).ToArray();change.id=stable;change.points=window.points;change.heights=new float[window.points.Length];change.evidence=JsonUtility.FromJson<RoadMapEvidence>(JsonUtility.ToJson(window.evidence));change.status="provisional";
  }
  var errors=RoadMapValidator.Validate(package.baseline,doc).Where(e=>e.error).ToArray();if(errors.Length>0)throw new InvalidOperationException("Lane-change document rejected: "+string.Join(" / ",errors.Take(5).Select(e=>e.ToString())));
  var map=RoadMapComposer.Compose(package.baseline,doc);
  foreach(var edge in map.hdEdges.Where(e=>e.id.StartsWith(Prefix)))if(!EungyeConnectivityRepair.FootprintClear(map,edge))throw new InvalidOperationException("Lane-change footprint outside road or inside sidewalk/building: "+edge.id);
  Debug.Log("PLAYTEST_LANE_CHANGE_PREVIEW changes="+windows.Count+" / authoring only; source file has not been saved");return doc;
 }
}
