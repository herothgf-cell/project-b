using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
public sealed class RealLeftOnlyProbe:MonoBehaviour {
 DriveSession s;int checks;readonly StringBuilder report=new StringBuilder();
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-realLeftOnlyTest")>=0)new GameObject("Verified left-only integration").AddComponent<RealLeftOnlyProbe>();}
 void Check(bool ok,string text){checks++;report.AppendLine((ok?"PASS ":"FAIL ")+text);if(!ok)throw new Exception(text);}
 IEnumerator Start(){
  float deadline=Time.realtimeSinceStartup+100;while(!(s=FindObjectOfType<DriveSession>())||!s.StartupReady){if(Time.realtimeSinceStartup>deadline){Application.Quit(1);yield break;}yield return null;}yield return null;yield return null;
  bool ok=false;try{
   Check(Path.GetFullPath(s.business.SavePath).Replace('\\','/').Contains("/artifacts/"),"isolated test save");var zones=s.GetComponent<ServiceLawZones>();Check(zones.Ready&&zones.scenarios.Count==9&&zones.zones.Count==4,"nine transports and four other service conditions remain");
   Check(!zones.zones.Any(z=>z.variant>=12),"no fictional permissive service zone or map label");
   foreach(var j in s.traffic.junctions)Check(j.leftTurnMode!=LeftTurnMode.Permissive,"normal signal does not become unverified permissive: "+j.name);
   var original=zones.CaptureLive();var trip=s.jobs.trip;var state=trip.State;var current=s.jobs.current;
   foreach(var id in new[]{"PS05","PS06","PS07"}){
    var c=zones.scenarios.Single(q=>q.id==id);Check(c.variant==0,id+" is ordinary route with fixed road regulation");int variant=id=="PS05"?12:id=="PS06"?13:14;
    var offer=new JobOffer{scenarioId=id,zoneIndex=0};Check(zones.ZoneFor(offer)==null,id+" stale index cannot attach another zone");
    var junction=s.traffic.junctions.First();var mode=junction.leftTurnMode;Check(!zones.Live.StartInService(new ServiceLawZone{variant=variant,junction=junction})&&junction.leftTurnMode==mode,id+" retired service cannot change signal even if explicitly requested");
    zones.RestoreLive(new ServiceLawZonesSnapshot{scenarioId=id,assignedId="eungye-law-"+variant+"-1248",sequence=6,lastTitle="비보호",hasFeedback=true,lastFeedback=new PracticeFeedback{outcome="retired fictional result"},hasObservation=true,observation=new ConfusionLiveSnapshot{state=new ConfusionPracticeState(variant).Snapshot()}});
    Check(zones.CurrentScenario.id==id&&zones.Assigned==null&&!zones.Live.Active&&zones.LastFeedback==null,id+" legacy active override and current TIP cleared");Check(s.jobs.trip==trip&&trip.State==state&&s.jobs.current==current,id+" passenger trip remains unchanged during migration");
   }foreach(int variant in new[]{12,13,14}){zones.RestoreLive(new ServiceLawZonesSnapshot{lastTitle=ConfusionPracticeState.Title(variant),hasFeedback=true,lastFeedback=new PracticeFeedback{outcome="old completed result"}});Check(zones.LastFeedback==null&&string.IsNullOrEmpty(zones.LastTitle),"ended legacy trip has no retired TIP "+variant);}zones.RestoreLive(original);report.AppendLine(zones.ScenarioInventory());ok=true;
  }catch(Exception e){report.AppendLine(e.ToString());Debug.LogException(e);}
  Directory.CreateDirectory("artifacts");File.WriteAllText("artifacts/real-left-only-runtime.txt","success="+ok+"\nchecks="+checks+"\n"+report);Debug.Log("REAL_LEFT_ONLY_"+(ok?"OK":"FAILED")+" checks="+checks);Application.Quit(ok?0:1);
 }
}
