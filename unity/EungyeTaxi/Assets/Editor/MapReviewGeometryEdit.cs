using System;
using System.IO;
using System.Linq;
using UnityEngine;

// 2026-11-26 map review: generic road-geometry patch (lane points and widths, scene road strips and
// sidewalk polygons, painted marks) computed by tools/map_geometry_edit.py from a cross-section
// deformation (move a curb, narrow or shift a carriageway). Every listed id must exist; the patch only
// sets the geometry fields it names (a lane-bound presentation gets its range rescaled to the new lane length), then the map package validator checks the result before saving.
public static class MapReviewGeometryEdit {
 [Serializable] class LaneSet {public string id;public float width;public MapPoint[] points;}
 [Serializable] class ObjectSet {public string id;public float width;public MapPoint[] points;}
 [Serializable] class ObjectAdd {public string id,copyOf;public float width;public MapPoint[] points;}
 [Serializable] class FacilitySet {public string id;public float x,z,yaw,width,length,startAt,endAt;public bool range;}
 [Serializable] class LaneAdd {public string id,copyOf;public float width;public int number,turn,change;public MapPoint[] points;public string[] next,prev;}
 [Serializable] class LaneNext {public string id;public string[] next;}
 [Serializable] class FacilityAdd {public string id,copyOf,laneId;public float x,z,yaw,width,length;}
 [Serializable] class Patch {public int fromRevision,toRevision;public LaneSet[] lanes;public ObjectSet[] objects;public ObjectAdd[] addObjects;public FacilitySet[] facilities;
  // lane-count edits (tools/map_lane_edit.py): retire lanes (DeleteLane also drops their facilities and signal
  // bindings), add lanes copied from an existing one, replace exits. Applied after the geometry sets above.
  public string[] disableLanes;public LaneAdd[] addLanes;public LaneNext[] nexts;public FacilityAdd[] addFacilities;public string[] removeFacilities;}
 static float[] Heights(float[] old,int count){var h=new float[count];if(old==null||old.Length==0)return h;for(int i=0;i<count;i++){float t=count==1?0:(float)i/(count-1)*(old.Length-1);int a=Mathf.Min(old.Length-1,(int)t),b=Mathf.Min(old.Length-1,a+1);h[i]=Mathf.Lerp(old[a],old[b],t-a);}return h;}
 static void Require(bool pass,string message){if(!pass)throw new InvalidOperationException("map review geometry: "+message);}
 public static void ApplyFromCommandLine(){
  var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-geometryPatch");
  Require(i>=0&&i+1<args.Length,"pass -geometryPatch <config/maps/review/....json>");ApplyFile(args[i+1]);
 }
 public static void ApplyFile(string file){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);
  var plan=JsonUtility.FromJson<Patch>(File.ReadAllText(Path.Combine(root,file)));
  var before=ProjectMapStore.Load(root);
  Require(before.document.revision==plan.fromRevision,"expected map r"+plan.fromRevision+", found r"+before.document.revision);
  var d=before.document.Copy();
  var lanes=d.lanes.ToDictionary(l=>l.id);
  foreach(var s in plan.lanes??new LaneSet[0]){
   RoadMapLane l;Require(lanes.TryGetValue(s.id,out l),"missing lane "+s.id);
   Require(s.points!=null&&s.points.Length>=2,"lane "+s.id+" points");
   Require(s.width>=2f&&s.width<=6f,"lane "+s.id+" width "+s.width);
   if(s.points.Length!=l.points.Length)l.heights=Heights(l.heights,s.points.Length);
   l.points=s.points;l.width=s.width;
  }
  var objects=d.scene.objects.ToList();var byId=objects.ToDictionary(o=>o.id);
  foreach(var s in plan.objects??new ObjectSet[0]){
   ProjectMapObject o;Require(byId.TryGetValue(s.id,out o),"missing scene object "+s.id);
   Require(o.kind=="road"||o.kind=="sidewalk","scene object "+s.id+" is "+o.kind);
   Require(s.points!=null&&s.points.Length>=(o.kind=="road"?2:3),"scene object "+s.id+" points");
   o.points=s.points;if(o.kind=="road")o.width=s.width;
  }
  foreach(var a in plan.addObjects??new ObjectAdd[0]){
   ProjectMapObject src;Require(byId.TryGetValue(a.copyOf,out src),"missing source object "+a.copyOf);Require(!byId.ContainsKey(a.id),"duplicate "+a.id);
   var o=JsonUtility.FromJson<ProjectMapObject>(JsonUtility.ToJson(src));o.id=a.id;o.points=a.points;if(o.kind=="road")o.width=a.width;
   objects.Add(o);byId.Add(o.id,o);
  }
  d.scene.objects=objects.ToArray();
  var facilities=d.facilities.ToDictionary(f=>f.id);
  foreach(var s in plan.facilities??new FacilitySet[0]){
   RoadMapFacility f;Require(facilities.TryGetValue(s.id,out f),"missing facility "+s.id);
   if(s.range){f.startAt=s.startAt;f.endAt=s.endAt;}else{f.x=s.x;f.z=s.z;f.yaw=s.yaw;f.width=s.width;f.length=s.length;}
  }
  ApplyLanes(d,plan);facilities=d.facilities.ToDictionary(f=>f.id);
  foreach(var a in plan.addFacilities??new FacilityAdd[0]){
   RoadMapFacility src;Require(facilities.TryGetValue(a.copyOf,out src),"missing source facility "+a.copyOf);Require(!facilities.ContainsKey(a.id),"duplicate facility "+a.id);
   var f=JsonUtility.FromJson<RoadMapFacility>(JsonUtility.ToJson(src));f.id=a.id;f.x=a.x;f.z=a.z;f.yaw=a.yaw;f.width=a.width;f.length=a.length;if(!string.IsNullOrEmpty(a.laneId)){Require(d.lanes.Any(l=>l.id==a.laneId&&!l.disabled),"facility lane "+a.laneId);f.laneId=a.laneId;}
   d.facilities=d.facilities.Append(f).ToArray();facilities.Add(f.id,f);
  }
  foreach(var id in plan.removeFacilities??new string[0]){Require(facilities.ContainsKey(id),"missing facility "+id);RoadMapAuthoringOperations.DeleteFacility(d,id);}
  d.revision=plan.toRevision;d.projectRevision="";d.savedAt=DateTime.UtcNow.ToString("o");
  var result=ProjectMapPackage.Create(before.baseline,d);
  ProjectMapStore.Save(root,result,token);
  var saved=ProjectMapStore.Load(root);
  Require(saved.contentHash==result.contentHash&&saved.document.revision==plan.toRevision,"saved map identity");
  Debug.Log("MAP_REVIEW_GEOMETRY "+file+" lanes="+(plan.lanes??new LaneSet[0]).Length+" objects="+(plan.objects??new ObjectSet[0]).Length+" added="+(plan.addObjects??new ObjectAdd[0]).Length+" facilities="+(plan.facilities??new FacilitySet[0]).Length+" disabled="+(plan.disableLanes??new string[0]).Length+" addedLanes="+(plan.addLanes??new LaneAdd[0]).Length+" saved="+saved.contentHash);
 }
 // An exit shares its start node with its entry lane's end node. A target with other active entries keeps
 // its node (the lane takes it); a target this lane alone enters takes the lane's end node.
 static void Join(RoadMapDocument d,RoadMapLane l){
  foreach(var id in l.next){var t=d.lanes.SingleOrDefault(x=>x.id==id&&!x.disabled);Require(t!=null,"lane "+l.id+" exit "+id);
   if(d.lanes.Any(x=>x!=l&&!x.disabled&&x.next.Contains(id)))l.toNode=t.fromNode;else t.fromNode=l.toNode;}
 }
 static void ApplyLanes(RoadMapDocument d,Patch plan){
  var had=d.lanes.Where(l=>!l.disabled&&l.next!=null&&l.next.Length>0).Select(l=>l.id).ToHashSet();
  foreach(var id in plan.disableLanes??new string[0]){Require(d.lanes.Any(l=>l.id==id&&!l.disabled),"missing active lane "+id);RoadMapAuthoringOperations.DeleteLane(d,id);}
  // re-paired exits first (an added lane may take over an exit they release)
  foreach(var n in plan.nexts??new LaneNext[0]){var l=d.lanes.SingleOrDefault(x=>x.id==n.id);Require(l!=null&&!l.disabled,"missing lane "+n.id);l.next=n.next??new string[0];Join(d,l);}
  foreach(var a in plan.addLanes??new LaneAdd[0]){
   Require(!d.lanes.Any(l=>l.id==a.id),"duplicate lane "+a.id);var src=d.lanes.SingleOrDefault(l=>l.id==a.copyOf);Require(src!=null,"missing source lane "+a.copyOf);
   Require(a.points!=null&&a.points.Length>=2&&a.width>=2f&&a.width<=6f,"lane "+a.id+" geometry");
   var l=JsonUtility.FromJson<RoadMapLane>(JsonUtility.ToJson(src));l.id=a.id;l.disabled=false;l.width=a.width;l.number=a.number;l.turn=a.turn;l.change=a.change;
   l.points=a.points;l.heights=Heights(src.heights,a.points.Length);l.next=a.next??new string[0];l.status="provisional";
   var prev=a.prev??new string[0];
   l.fromNode=prev.Length>0?d.lanes.Single(x=>x.id==prev[0]).toNode:a.id+":from";l.toNode=a.id+":to";
   d.lanes=d.lanes.Append(l).ToArray();Join(d,l);
   foreach(var p in prev){var from=d.lanes.SingleOrDefault(x=>x.id==p);Require(from!=null&&!from.disabled,"missing entry lane "+p);from.next=from.next.Append(a.id).Distinct().ToArray();}
   // a copied approach or connector moves with the same signal group(s) as its source
   foreach(var program in d.signals)foreach(var g in program.groups)if(g.laneIds.Contains(src.id))g.laneIds=g.laneIds.Append(a.id).ToArray();
  }
  var active=d.lanes.Where(l=>!l.disabled).ToDictionary(l=>l.id);
  foreach(var l in active.Values){
   foreach(var n in l.next)Require(active.ContainsKey(n),"lane "+l.id+" exits to missing or retired "+n);
   Require(!had.Contains(l.id)||l.next.Length>0,"lane "+l.id+" lost every exit");
  }
 }
}
