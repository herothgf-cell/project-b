using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// Isolated acceptance probe. Each case has one explicit approach placement;
// entry and departure thereafter use the taxi's real wheel/pedal controller.
public sealed class WorkshopPhysicalProbe : MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Attach(){var args=Environment.GetCommandLineArgs();if(Array.IndexOf(args,"-workshopPhysicalCheck")>=0||Array.IndexOf(args,"-pvpWorkshopPhysicalCheck")>=0)new GameObject("Workshop physical acceptance").AddComponent<WorkshopPhysicalProbe>();}
 bool ok=true;readonly StringBuilder report=new StringBuilder();DriveSession session;
 void Check(bool value,string message){ok&=value;report.AppendLine((value?"PASS ":"FAIL ")+message);Debug.Log("WORKSHOP_PHYSICAL "+(value?"PASS ":"FAIL ")+message);}
 IEnumerator Start(){
  Application.runInBackground=true;bool pvp=Array.IndexOf(Environment.GetCommandLineArgs(),"-pvpWorkshopPhysicalCheck")>=0;float deadline=Time.realtimeSinceStartup+180;
  while(Time.realtimeSinceStartup<deadline){
   if(pvp){var match=FindObjectOfType<PvpSimulationMatch>();session=match?match.Human:null;}
   else {session=null;foreach(var candidate in FindObjectsOfType<DriveSession>())if(!candidate.IsSimulationBot){session=candidate;break;}}
   if(session&&session.StartupReady)break;
   if(!session){if(pvp){var b=FindObjectOfType<Pvp.PvpBootstrap>();if(b)b.StartSimulation(true,7200);}else {var b=FindObjectOfType<UnifiedModeBootstrap>();if(b)b.StartSingle();}}
   yield return null;
  }
  if(!session||!session.StartupReady){Check(false,"startup timeout");Finish();yield break;}
  foreach(var s in FindObjectsOfType<DriveSession>()){var pilot=s.GetComponent<PvpSimulationBot>();if(pilot)pilot.enabled=false;s.business.enabled=false;s.business.StopAllCoroutines();s.business.preparing=false;s.business.taxPrompt=false;s.jobs.ClearForDay();s.paused=false;if(s.accident)s.accident.enabled=false;s.taxi.automated=true;s.taxi.autoBrake=true;}
  if(session.simulationMatch)session.simulationMatch.enabled=false;
  Check(!session.IsSimulationBot&&(!pvp||session.simulationMatch&&session.simulationMatch.Human==session),"probe selects human camera/vehicle session");
  Time.timeScale=4;var taxi=session.taxi;taxi.enabled=true;taxi.body.isKinematic=false;taxi.body.constraints=RigidbodyConstraints.None;taxi.body.WakeUp();
  bool wheels=taxi.wheels.Length==4;foreach(var wheel in taxi.wheels)wheels&=wheel&&wheel.enabled;
  Check(wheels&&!taxi.body.isKinematic&&taxi.body.constraints==RigidbodyConstraints.None,"native dynamic taxi and all four WheelColliders active");
  var graph=session.jobs.nav.hd;var sites=session.services.Workshops;Check(sites.Count==2,"two workshops "+session.world.map.mapId);
  var driver=gameObject.AddComponent<FuelStationProbe>();driver.externallyDriven=true;driver.session=session;
  foreach(var site in sites){
   var path=graph.paths[site.approach.laneEdge];float at=site.approach.laneAt;var initial=path.Position(Mathf.Max(0,at-5));
   taxi.SetSpawn(initial+Vector3.up*.65f,Quaternion.LookRotation(path.Tangent(Mathf.Max(0,at-5))));taxi.ResetTaxi();taxi.automated=true;taxi.autoBrake=true;session.assisted.SetActive(false);yield return new WaitForSeconds(.8f);
   var entry=new List<Vector3>{initial,site.approach.V()};entry.AddRange(site.entry);
   Check(!site.CanRepair(initial,taxi.transform.forward,0),site.id+" roadside is not repair bay");
   yield return driver.Drive(entry,site.id+" physical entry");
   Check(driver.Passed,site.id+" actual entry clear of side/curb collision");Check(site.CanRepair(taxi.transform.position,taxi.transform.forward,taxi.body.velocity.magnitude),site.id+" actual stop meets repair heading/speed");
   Capture(site);var leave=new List<Vector3>(site.departure);leave.Add(path.Position(Mathf.Min(path.length,at+40+48)));
   yield return driver.Drive(leave,site.id+" physical exit");
   Check(driver.Passed,site.id+" actual exit clear of side/curb collision");Check(!site.CanRepair(taxi.transform.position,taxi.transform.forward,0),site.id+" left repair zone");Check(Vector3.Dot(taxi.transform.forward,site.direction)>.85f,site.id+" road join direction");
  }
  Finish();
 }
 void Capture(WorkshopLayout.Site site){
  var camera=Camera.main;if(!session.drivingCamera||!camera){Check(false,"capture unavailable: human="+(!session.IsSimulationBot)+" drivingCamera="+(bool)session.drivingCamera+" mainCamera="+(bool)camera);return;}bool enabled=session.drivingCamera.enabled;session.drivingCamera.enabled=false;var position=camera.transform.position;var rotation=camera.transform.rotation;
  var eye=site.repair-site.direction*38-TrafficSystem.Right(site.direction)*28+Vector3.up*36;camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(site.repair+TrafficSystem.Right(site.direction)*6-eye));
  var texture=new RenderTexture(1280,720,24);var previous=RenderTexture.active;var target=camera.targetTexture;camera.targetTexture=texture;camera.Render();RenderTexture.active=texture;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();Directory.CreateDirectory("artifacts");string file="artifacts/"+site.id.Replace(':','-')+"-physical.png";File.WriteAllBytes(file,pixels.EncodeToPNG());camera.targetTexture=target;RenderTexture.active=previous;Destroy(pixels);texture.Release();Destroy(texture);camera.transform.SetPositionAndRotation(position,rotation);session.drivingCamera.enabled=enabled;Check(File.Exists(file),"placement capture "+file);
 }
 void Finish(){Time.timeScale=1;Directory.CreateDirectory("artifacts");string region=session?session.world.map.mapId:"unknown";File.WriteAllText("artifacts/workshop-physical-"+region+".txt","success="+ok+"\n"+report);Debug.Log("WORKSHOP_PHYSICAL_COMPLETE success="+ok+" map="+region);Application.Quit(ok?0:1);}
}
