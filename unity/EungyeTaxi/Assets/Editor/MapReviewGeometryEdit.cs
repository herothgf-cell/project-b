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
 [Serializable] class Patch {public int fromRevision,toRevision;public LaneSet[] lanes;public ObjectSet[] objects;public ObjectAdd[] addObjects;public FacilitySet[] facilities;}
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
   Require(s.points!=null&&s.points.Length==l.points.Length,"lane "+s.id+" point count changed");
   Require(s.width>=2f&&s.width<=6f,"lane "+s.id+" width "+s.width);
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
  d.revision=plan.toRevision;d.projectRevision="";d.savedAt=DateTime.UtcNow.ToString("o");
  var result=ProjectMapPackage.Create(before.baseline,d);
  ProjectMapStore.Save(root,result,token);
  var saved=ProjectMapStore.Load(root);
  Require(saved.contentHash==result.contentHash&&saved.document.revision==plan.toRevision,"saved map identity");
  Debug.Log("MAP_REVIEW_GEOMETRY "+file+" lanes="+(plan.lanes??new LaneSet[0]).Length+" objects="+(plan.objects??new ObjectSet[0]).Length+" added="+(plan.addObjects??new ObjectAdd[0]).Length+" facilities="+(plan.facilities??new FacilitySet[0]).Length+" saved="+saved.contentHash);
 }
}
