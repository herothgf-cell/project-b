using System;
using System.IO;
using System.Linq;
using UnityEngine;

// 2026-11-26 map review (r59 -> r60): Eungye pedestrian streets confirmed on Kakao roadview become plazas.
// config/maps/review/r60-plaza-lanes.json (tools/map_review_plaza_plan.py) lists the lanes to disable (through
// lanes plus every connector that starts or ends on them; the plan checks no active lane is left without an
// exit), the scene road surfaces to switch off and the pedestrian-space polygons that replace them.
public static class MapReviewPlazaEdit {
 [Serializable] class Plaza {public string id,source;public MapPoint[] points;}
 [Serializable] class Plan {public int fromRevision,toRevision;public string[] disable,disableScene,deadEnds;public Plaza[] plazas;}
 static void Require(bool pass,string message){if(!pass)throw new InvalidOperationException("map review plaza: "+message);}
 public static void Apply(){ApplyFile("config/maps/review/r60-plaza-lanes.json");}
 // r60 -> r61: the J1897 five-way pedestrian cross north of Daeeun-ro.
 public static void ApplyR61(){ApplyFile("config/maps/review/r61-plaza-lanes.json");}
 static void ApplyFile(string file){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);
  var plan=JsonUtility.FromJson<Plan>(File.ReadAllText(Path.Combine(root,file)));
  Require(plan.deadEnds==null||plan.deadEnds.Length==0,"plan leaves dead ends");
  var before=ProjectMapStore.Load(root);
  Require(before.document.revision==plan.fromRevision,"expected map r"+plan.fromRevision+", found r"+before.document.revision);
  var d=before.document.Copy();
  var lanes=d.lanes.ToDictionary(l=>l.id);
  foreach(var id in plan.disable){Require(lanes.ContainsKey(id),"missing lane "+id);lanes[id].disabled=true;}
  var off=plan.disable.ToHashSet();
  foreach(var l in d.lanes)if(!l.disabled&&l.next!=null&&l.next.Any(off.Contains)){
   l.next=l.next.Where(n=>!off.Contains(n)).ToArray();Require(l.next.Length>0,"lane "+l.id+" would lose every exit");}
  Require(d.scene!=null&&d.scene.objects!=null,"document has no scene");
  var objects=d.scene.objects.ToList();
  foreach(var id in plan.disableScene){var o=objects.SingleOrDefault(x=>x.id==id);Require(o!=null&&o.kind=="road","missing scene road "+id);o.enabled=false;}
  foreach(var p in plan.plazas){Require(!objects.Any(x=>x.id==p.id),"duplicate "+p.id);
   objects.Add(new ProjectMapObject{id=p.id,kind="sidewalk",name="검토 보행 공간",points=p.points,width=4,height=.14f,enabled=true});}
  d.scene.objects=objects.ToArray();
  d.revision=plan.toRevision;d.projectRevision="";d.savedAt=DateTime.UtcNow.ToString("o");
  var result=ProjectMapPackage.Create(before.baseline,d);
  ProjectMapStore.Save(root,result,token);
  var saved=ProjectMapStore.Load(root);
  Require(saved.contentHash==result.contentHash&&saved.document.revision==plan.toRevision,"saved map identity");
  Debug.Log("MAP_REVIEW_PLAZA lanes="+plan.disable.Length+" roads="+plan.disableScene.Length+" plazas="+plan.plazas.Length+" saved="+saved.contentHash);
 }
}
