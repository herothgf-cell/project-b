using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Explicit authoring only. Never recompiles the district, reselects stops,
// changes a movement, or writes the generated raw map input.
public static class RoadviewCorridorMapEdits {
 public static readonly string[] GreenLanes={"local:7213:14566:7238:13919","local:7335:13923:7310:14570","local:7250:13665:7291:12941","local:7388:12954:7347:13670","local:7346:12686:8782:10837","local:8824:10924:7438:12714","local:9032:10757:8270:6402","local:8265:6305:9056:10851"};
 static void Check(bool ok,string reason){if(!ok)throw new InvalidOperationException("ROADVIEW_CORRIDOR_MAP: "+reason);}
 public static bool SelectedRoad(ProjectMapObject o){return o.kind=="road"&&o.id.StartsWith("road:",StringComparison.Ordinal)&&RoadviewCorridorPresentation.GreenSource(o.id.Substring(5));}
 public static bool DuplicateMainRibbon(ProjectMapObject o){return GreenLanes.Any(id=>o.id=="road:polish:"+id);}
 public static RoadMapDocument Preview(ProjectMapPackage package){
  var d=package.document.Copy();Check(d.scene!=null,"authored project scene required");
  foreach(var id in GreenLanes){var lane=d.lanes.Single(l=>l.id==id);Check(!lane.disabled&&lane.number==1&&lane.turn==0&&lane.change==0&&RoadviewCorridorPresentation.GreenCorridor(lane.sourceId),"reviewed one-lane family changed: "+id);}
  var roads=d.scene.objects.Where(SelectedRoad).ToArray();Check(roads.Length==14,"fourteen longitudinal green source ribbons required");
  bool changed=false;
  foreach(var road in roads){changed|=road.width!=RoadviewCorridorPresentation.GreenSurfaceWidth||road.lanes!=2||road.oneWay;road.width=RoadviewCorridorPresentation.GreenSurfaceWidth;road.lanes=2;road.oneWay=false;}
  foreach(var ribbon in d.scene.objects.Where(DuplicateMainRibbon)){changed|=ribbon.enabled;ribbon.enabled=false;}
  // Intersection ribbons retain the existing swept turning space. Only the
  // redundant straight main-lane overlays above are retired.
  if(changed){d.revision++;d.projectRevision="";d.savedAt=DateTime.UtcNow.ToString("o");}
  AssertPreserved(package.document,d);return d;
 }
 public static void AssertPreserved(RoadMapDocument before,RoadMapDocument after){
  var a=new RoadMapDocument{lanes=before.lanes,facilities=before.facilities,signals=before.signals};var b=new RoadMapDocument{lanes=after.lanes,facilities=after.facilities,signals=after.signals};
  // The wrapper ID/savedAt are irrelevant; compare the actual protected arrays.
  Check(JsonUtility.ToJson(new RoadMapDocument{id="",lanes=a.lanes})==JsonUtility.ToJson(new RoadMapDocument{id="",lanes=b.lanes}),"all lane geometry, lineage and movements must remain byte-identical");
  Check(JsonUtility.ToJson(new RoadMapDocument{id="",facilities=a.facilities})==JsonUtility.ToJson(new RoadMapDocument{id="",facilities=b.facilities}),"all authored facilities and U-turn conditions must remain identical");
  Check(JsonUtility.ToJson(new RoadMapDocument{id="",signals=a.signals})==JsonUtility.ToJson(new RoadMapDocument{id="",signals=b.signals}),"all authored signal programs must remain identical");
  Check(before.scene.objects.Length==after.scene.objects.Length,"scene object count preserved");
  for(int i=0;i<before.scene.objects.Length;i++){var old=before.scene.objects[i];var current=after.scene.objects[i];Check(old.id==current.id,"scene order and identity preserved");if(!SelectedRoad(old)&&!DuplicateMainRibbon(old))Check(JsonUtility.ToJson(old)==JsonUtility.ToJson(current),"unrelated scene/service object changed: "+old.id);}
 }
 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);var package=ProjectMapStore.Load(root);var doc=Preview(package);
  RoadviewCorridorTests.Validate(package,doc);string baseline=package.baselineHash;var result=ProjectMapPackage.Create(package.baseline,doc);Check(result.baselineHash==baseline,"baseline/source immutable");
  ProjectMapStore.Save(root,result,token);ProjectMapBuild.Prepare();Verify();
  Debug.Log("ROADVIEW_CORRIDOR_APPLIED baseline="+baseline+" content="+result.contentHash+" field=2026-05 green=1+1 yellow=3+3 geometry=game-estimate");
 }
 public static void Verify(){var p=ProjectMapStore.Load(ProjectMapBuild.Root);RoadviewCorridorTests.Validate(p,p.document);}
}
