using System;
using System.IO;
using System.Linq;
using UnityEngine;

// 2026-11-25 map review, markings batch 1 (r54 -> r55). Applies config/maps/review/r55-crosswalk-edits.json,
// made by tools/map_review_r55_crosswalks.py from a player dump of the generated crosswalks.
// An edit either changes an existing document crosswalk or adds one with the junction/axis/sign of a
// generated crosswalk, which then replaces the generated stripes (TrafficSystem.AddApproach). Diagonal
// crossings have no junction and are paint only. Lanes, stop lines and signal programs are not touched.
public static class MapReviewCrosswalkEdits {
 [Serializable] class Edit {public string kind,junctionId,existing,id,refs,type,text;public int axis,sign;public float x,z,yaw,width,length;}
 [Serializable] class Plan {public int fromRevision,toRevision;public Edit[] edits;}
 static void Require(bool pass,string message){if(!pass)throw new InvalidOperationException("map review crosswalks: "+message);}
 public static void Apply(){ApplyFile("config/maps/review/r55-crosswalk-edits.json");}
 // Markings batch 2 (r55 -> r56): crosswalks, school-zone paving, road text, diamonds and one removed crosswalk.
 public static void ApplyR56(){ApplyFile("config/maps/review/r56-mark-edits.json");}
 // Crosswalk moves with their stop lines (r56 -> r57).
 public static void ApplyR57(){ApplyFile("config/maps/review/r57-crosswalk-moves.json");}
 // Left-turn guide lines and the J4396 zigzag approach (r57 -> r58).
 public static void ApplyR58(){ApplyFile("config/maps/review/r58-line-marks.json");}
 // Straight centre-line bands where the lane-based yellow line wiggles (r58 -> r59).
 public static void ApplyR59(){ApplyFile("config/maps/review/r59-centre-lines.json");}
 // Yellow school-zone crossings confirmed by roadview: J044 three arms, J2564 (r61 -> r62).
 public static void ApplyR62(){ApplyFile("config/maps/review/r62-yellow-crossings.json");}
 // Roadview-confirmed school-zone marks: yellow crossings, raised red crossings, text (r62 -> r63).
 public static void ApplyR63(){ApplyFile("config/maps/review/r63-school-marks.json");}
 // Mid-block crossings clear in the aerial: J002 east, J009 roundabout approaches, J046 (r63 -> r64).
 public static void ApplyR64(){ApplyFile("config/maps/review/r64-midblock-crossings.json");}
 // Roadview-confirmed school-zone approach paving at J004 and J025 (r64 -> r65).
 public static void ApplyR65(){ApplyFile("config/maps/review/r65-school-paving.json");}
 static void ApplyFile(string file){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);
  var plan=JsonUtility.FromJson<Plan>(File.ReadAllText(Path.Combine(root,file)));
  var before=ProjectMapStore.Load(root);
  Require(before.document.revision==plan.fromRevision,"expected map r"+plan.fromRevision+", found r"+before.document.revision);
  var d=before.document.Copy();var facilities=d.facilities.ToList();int changed=0,added=0;
  foreach(var e in plan.edits){
   RoadMapFacility f;
   if(!string.IsNullOrEmpty(e.existing)){f=facilities.Single(x=>x.id==e.existing);Require(f.kind==(string.IsNullOrEmpty(e.kind)?"crosswalk":e.kind)&&f.junctionId==e.junctionId&&f.axis==e.axis&&f.sign==e.sign,"existing facility identity "+e.existing);changed++;}
   else{
    string kind=string.IsNullOrEmpty(e.kind)?"crosswalk":e.kind;
    Require(string.IsNullOrEmpty(e.junctionId)||!facilities.Any(x=>x.kind==kind&&x.junctionId==e.junctionId&&x.axis==e.axis&&x.sign==e.sign),"duplicate "+kind+" "+e.junctionId+" a"+e.axis+" s"+e.sign);
    string id=string.IsNullOrEmpty(e.id)?"review:r55:"+e.junctionId+":a"+e.axis+":s"+e.sign+":crosswalk":e.id;Require(!facilities.Any(x=>x.id==id),"duplicate id "+id);
    f=new RoadMapFacility{id=id,kind=kind,junctionId=e.junctionId,axis=e.axis,sign=e.sign,height=0};
    f.evidence.state="observed";f.evidence.note="2026-11-25 지도 검토 "+e.refs+" · 카카오 항공사진 대조";
    facilities.Add(f);added++;
   }
   f.type=e.type;f.text=e.text;f.x=e.x;f.z=e.z;f.yaw=e.yaw;f.width=e.width;f.length=e.length;
  }
  d.facilities=facilities.ToArray();
  d.revision=plan.toRevision;d.projectRevision="";d.savedAt=DateTime.UtcNow.ToString("o");
  var result=ProjectMapPackage.Create(before.baseline,d);
  ProjectMapStore.Save(root,result,token);
  var saved=ProjectMapStore.Load(root);
  Require(saved.contentHash==result.contentHash&&saved.document.revision==plan.toRevision,"saved map identity");
  Debug.Log("MAP_REVIEW_CROSSWALKS file="+file+" changed="+changed+" added="+added+" saved="+saved.contentHash);
 }
}
