using System;
using System.IO;
using UnityEngine;

// Road surprises, second batch (2026-11-24, Editor, no scene): settings, the scooter relief rule and the
// relieved person accident (half fee and penalty, no major grade), the ambulance trigger window.
public static class RoadSurprise2Checks {
 static int checks;
 static void Check(bool ok,string name){checks++;if(!ok)throw new Exception("ROADSURPRISE2 "+name);}
 public static void Run(){
  checks=0;
  ContentTuning.ConfigureCatalog(File.ReadAllText(Path.GetFullPath("../../config/content/catalog.json")));
  ContentTuning.ValidateJson(File.ReadAllText(Path.GetFullPath("../../config/content/default-profile.json")));
  Check(SurpriseRules.ScooterEnabled&&SurpriseRules.ScooterTriggerMetres==22&&SurpriseRules.ScooterSpeed==4&&SurpriseRules.ReliefPercent==50,"scooter defaults");
  Check(SurpriseRules.AmbulanceEnabled&&SurpriseRules.SirenSeconds==5&&SurpriseRules.AmbulanceSpeed==12&&SurpriseRules.YieldScorePenalty==5&&SurpriseRules.YieldSatisfactionPenalty==8,"ambulance defaults (game rule: -5 score, -8 satisfaction)");
  Check(SurpriseRules.Label(SurpriseRules.Scooter)=="킥보드"&&SurpriseRules.Label(SurpriseRules.Ambulance)=="구급차","labels");
  Check(SurpriseRules.Relieved(1.0f,false,true),"within 1.2 s, under the limit, braking: relieved");
  Check(!SurpriseRules.Relieved(1.3f,false,true)&&!SurpriseRules.Relieved(.5f,true,true)&&!SurpriseRules.Relieved(.5f,false,false),"any condition missing: no relief");
  Check(SurpriseRules.AmbulanceCandidate(60,2,true,30),"a junction 60 m ahead on green at 30 km/h");
  Check(!SurpriseRules.AmbulanceCandidate(60,2,false,30)&&!SurpriseRules.AmbulanceCandidate(40,2,true,30)&&!SurpriseRules.AmbulanceCandidate(95,2,true,30)&&!SurpriseRules.AmbulanceCandidate(60,12,true,30)&&!SurpriseRules.AmbulanceCandidate(60,2,true,10),"not on red, too near or far, off the road, or below 15 km/h");
  string reason;Check(SurpriseRules.Blocked(true,false,false,3,600,600,5,0,0,false)=="하루 한도","the two batches share the daily limit of 5");
  // Relief on a person accident.
  foreach(AccidentSeverity sev in Enum.GetValues(typeof(AccidentSeverity))){
   var st=new AccidentState();var r=AccidentSeverityPolicy.Resolve(AccidentKind.Person,sev);Check(st.BeginResolved("pedestrian-accident:1","scooter:1",r),"begin "+sev);
   int fullFee=st.Fee,fullPenalty=st.ScorePenalty;Check(st.ReliefPerson(50)&&st.Shared,"relief applies to "+sev);
   var expect=sev==AccidentSeverity.Major?AccidentSeverityPolicy.Resolve(AccidentKind.Person,AccidentSeverity.Moderate):r;
   Check(st.Severity!=AccidentSeverity.Major&&st.Fee==AccidentFault.SharedFee(expect.Fee,50)&&st.ScorePenalty==AccidentFault.SharedPenalty(expect.ScorePenalty,50),sev+": half of "+(sev==AccidentSeverity.Major?"the moderate":"its own")+" fee and penalty ("+fullFee+" -> "+st.Fee+", "+fullPenalty+" -> "+st.ScorePenalty+")");
   var saved=JsonUtility.FromJson<AccidentState>(JsonUtility.ToJson(st));saved.Validate();Check(saved.Fee==st.Fee&&saved.Severity==st.Severity,sev+": saved and restored");
  }
  var vehicle=new AccidentState();vehicle.BeginResolved("collision-accident:1","x",AccidentSeverityPolicy.Resolve(AccidentKind.Vehicle,AccidentSeverity.Minor));Check(!vehicle.ReliefPerson(50),"relief is for person accidents only");
  Debug.Log("ROADSURPRISE2_CHECKS_OK checks="+checks);
 }
}
