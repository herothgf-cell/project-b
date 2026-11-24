using System;
using System.Linq;
using UnityEngine;

// 2026-11-24 P51 signal timing, r52 -> r53 (test feedback: the wait for green on Eungyenam-ro was about 80 s).
// The authored program gives each of the four approaches its own stage and then an all-way walk:
// 4 x (12 green + 3 yellow + 2 all-red) + (27 walk + 3 all-red) = 98 s, so one approach waits up to 86 s.
// The stage order, groups and split-approach structure are unchanged. Only durations change:
//   Eungyenam-ro approaches 12 + 3 + 1, apartment gate approaches 5 + 3 + 1, all-way walk 17 + 3
//   = 16 + 16 + 9 + 9 + 20 = 70 s, so the longest wait for a main-road green is 58 s.
// A walker starts only with length / 2.2 + 1 s of walk left (RoadMapWalker), 14.1 s for the 28.8 m diagonal,
// so a 17 s walk still lets the diagonal be crossed.
public static class P51SignalTiming {
 const string Junction="reviewed:r47:P51";
 static void Require(bool pass,string message){if(!pass)throw new InvalidOperationException("P51 signal timing: "+message);}
 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);
  var before=ProjectMapStore.Load(root);
  Require(before.document.revision==52,"expected map r52, found r"+before.document.revision);
  var d=before.document.Copy();
  var program=d.signals.Single(p=>p.junctionId==Junction);
  Require(program.stages.Length==5&&Mathf.Approximately(program.Cycle,98),"expected the 5-stage 98 s program, found "+program.stages.Length+" stages "+program.Cycle+" s");
  float longestDiagonal=d.facilities.Where(f=>f.junctionId==Junction&&f.kind=="crosswalk").Max(f=>f.width);
  foreach(var stage in program.stages){
   var groups=stage.groups.Select(id=>program.groups.Single(g=>g.id==id)).ToArray();
   if(groups.All(g=>g.pedestrian)){stage.green=17;stage.yellow=0;stage.clearance=3;Require(stage.green>=longestDiagonal/2.2f+1,"walk shorter than the longest crossing needs");}
   else{
    Require(groups.All(g=>!g.pedestrian)&&groups.Select(g=>g.axis*2+g.sign).Distinct().Count()==1,"stage "+stage.name+" is not one approach");
    stage.green=groups[0].axis==0?12:5;stage.yellow=3;stage.clearance=1;
   }
  }
  Require(Mathf.Approximately(program.Cycle,70),"new cycle "+program.Cycle);
  foreach(var g in program.groups.Where(g=>!g.pedestrian&&g.turn==0)){
   float worst=0;for(float t=0;t<program.Cycle;t+=.25f)worst=Mathf.Max(worst,program.SecondsToGreen(g.axis,g.sign,0,t));
   Debug.Log("P51_SIGNAL approach axis="+g.axis+" sign="+g.sign+" longestWait="+worst.ToString("F1"));
   Require(worst<=(g.axis==0?58.5f:65.5f),"wait still too long on axis "+g.axis+" sign "+g.sign);
  }
  d.revision=53;d.projectRevision="";d.savedAt=DateTime.UtcNow.ToString("o");
  var result=ProjectMapPackage.Create(before.baseline,d);
  ProjectMapStore.Save(root,result,token);
  var saved=ProjectMapStore.Load(root);
  Require(saved.contentHash==result.contentHash&&saved.document.revision==53,"saved map identity");
  Debug.Log("P51_SIGNAL_SAVED "+saved.contentHash+" cycle="+program.Cycle);
 }
}
