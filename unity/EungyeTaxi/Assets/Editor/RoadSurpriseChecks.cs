using System;
using System.IO;
using UnityEngine;

// Road surprises, first batch (2026-11-24, Editor, no scene): settings defaults, when a surprise may start,
// the door's warning distance, the cut-in candidate window, and how a contact with either is judged.
public static class RoadSurpriseChecks {
 static int checks;
 static void Check(bool ok,string name){checks++;if(!ok)throw new Exception("ROADSURPRISE "+name);}
 static string B(int day=3,double elapsed=600,double left=600,int done=0,int kindDone=0,int mission=0,bool carrying=false,bool running=true,bool evening=false,bool accident=false){
  return SurpriseRules.Blocked(running,evening,accident,day,elapsed,left,done,kindDone,mission,carrying);
 }
 public static void Run(){
  checks=0;
  ContentTuning.ConfigureCatalog(File.ReadAllText(Path.GetFullPath("../../config/content/catalog.json")));
  ContentTuning.ValidateJson(File.ReadAllText(Path.GetFullPath("../../config/content/default-profile.json")));
  Check(SurpriseRules.Enabled&&SurpriseRules.PerShiftMax==5&&SurpriseRules.PerKindMax==2&&SurpriseRules.FirstDay&&SurpriseRules.MinGapSeconds==120,"defaults (2026-11-25): on, 5 per shift, 2 per kind, from day 1, 120 s apart");
  Check(SurpriseRules.DoorEnabled&&SurpriseRules.DoorTriggerMetres==18&&SurpriseRules.DoorOpenSeconds==4,"door defaults 18 m, 4 s");
  Check(SurpriseRules.CutInEnabled&&Mathf.Approximately(SurpriseRules.CutInSignalSeconds,.6f)&&Mathf.Approximately(SurpriseRules.CutInMergeSeconds,1.8f),"cut-in defaults 0.6 s signal, 1.8 s merge");
  Check(B()=="","an ordinary moment on day 3 allows a surprise");
  Check(B(day:1)=="","the first day allows surprises (firstDay=1)");
  Check(B(elapsed:179)=="영업 시작 직후"&&B(elapsed:180)=="","none in the first 3 minutes of the shift");
  Check(B(left:119)=="마감 직전"&&B(left:120)=="","none in the last 2 minutes");
  Check(B(done:5)=="하루 한도"&&B(done:4)=="","at most 5 per shift");
  Check(B(kindDone:2)=="같은 종류 한도"&&B(kindDone:1)=="","at most two of each kind");
  Check(B(mission:MissionRules.Express,carrying:true)!=""&&B(mission:MissionRules.Coffee,carrying:true)!="","none during an express or coffee mission ride");
  Check(B(mission:MissionRules.Express,carrying:false)==""&&B(mission:MissionRules.Green,carrying:true)=="","other missions, or before boarding, are not blocked");
  Check(B(running:false)!=""&&B(evening:true)!=""&&B(accident:true)!="","none outside operation, on the evening drive or while an accident is handled");
  Check(Mathf.Approximately(SurpriseRules.DoorTellDistance(0),18)&&Mathf.Approximately(SurpriseRules.DoorTellDistance(12.5f),28),"door gap shows 0.8 s of travel before the trigger distance");
  Check(SurpriseRules.CutInCandidate(14,3.5f,.99f,40,10,11),"a car 14 m ahead one lane over at 40 km/h is a candidate");
  Check(!SurpriseRules.CutInCandidate(14,3.5f,.99f,24,10,11)&&!SurpriseRules.CutInCandidate(14,3.5f,.99f,56,10,15),"taxi outside 25..55 km/h");
  Check(!SurpriseRules.CutInCandidate(9,3.5f,.99f,40,10,11)&&!SurpriseRules.CutInCandidate(23,3.5f,.99f,40,10,11),"outside 10..22 m ahead");
  Check(!SurpriseRules.CutInCandidate(14,1.2f,.99f,40,10,11)&&!SurpriseRules.CutInCandidate(14,7,.99f,40,10,11),"same lane or two lanes over");
  Check(!SurpriseRules.CutInCandidate(14,-3.5f,.9f,40,10,11),"not heading the same way");
  Check(!SurpriseRules.CutInCandidate(14,3.5f,.99f,40,12.5f,11),"a car faster than the taxi by over 1 m/s is not a cut-in threat");
  Check(SurpriseRules.CutInCandidate(14,-3.5f,.99f,40,10,11),"either side");
  // Contact judgement: the door and the cut-in car are surprise vehicles.
  string why;
  var door=new FaultInput{otherVehicle=true,surprise=true,sinceSurprise=.6f,ownSpeed=11,otherSpeed=0,contactAlong=1.9f,halfLength=2,ownAlong=11,otherAlong=0};
  Check(AccidentFault.Decide(door,out why)==FaultSide.Other,"door opened 0.6 s before the touch: the other side's fault");
  var late=door;late.sinceSurprise=2;Check(AccidentFault.Decide(late,out why)==FaultSide.Shared,"door open 2 s before the touch: shared");
  var fast=door;fast.aboveLimit=true;Check(AccidentFault.Decide(fast,out why)==FaultSide.Shared,"inside the reaction time but over the limit: shared");
  var violating=door;violating.violating=true;Check(AccidentFault.Decide(violating,out why)==FaultSide.Own,"a violation just before stays the driver's own fault");
  var parked=door;parked.surprise=false;Check(AccidentFault.Decide(parked,out why)==FaultSide.Own,"driving into the parked car's body (not the door) is own fault");
  Debug.Log("ROADSURPRISE_CHECKS_OK checks="+checks);
 }
}
