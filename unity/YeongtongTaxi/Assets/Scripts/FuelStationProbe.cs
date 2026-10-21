using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// Test-only pedal/steering controller. H stays disabled inside the facilities.
// Position is set only at the initial approach to each independent station case.
public class FuelStationProbe : MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-fuelFacilitiesTest")>=0)new GameObject("Fuel facilities physical test").AddComponent<FuelStationProbe>();}
 DriveSession session;bool success=true;readonly StringBuilder report=new StringBuilder();float maxError;int sideContacts,curbSamples;bool driving;
 void Check(bool ok,string message){report.AppendLine((ok?"PASS ":"FAIL ")+message);if(!ok){success=false;Debug.LogError("FUEL_TEST "+message);}}
 IEnumerator Start(){
  yield return new WaitForSecondsRealtime(2);session=FindObjectOfType<DriveSession>();
  if(!session){Debug.LogError("FUEL_TEST no session");Application.Quit(1);yield break;}
  session.business.StopAllCoroutines();session.business.preparing=false;session.business.enabledCampaign=false;session.business.taxPrompt=false;session.business.useV2=true;
  session.business.settings=new PrototypeSettings();session.business.ledger=new EconomyLedger(session.business.settings);session.services.enabled=true;session.jobs.ClearForDay();session.paused=false;Time.timeScale=8;
  var taxi=session.taxi;taxi.assistance.SetActive(false);taxi.automated=true;taxi.autoBrake=true;
  var contacts=taxi.gameObject.AddComponent<FuelStationContactRecorder>();contacts.owner=this;
  var stations=FuelStationLayout.For(session.world.map);Check(stations.Count==3,"three facilities on Eungye");var graph=session.jobs.nav.hd;
  foreach(var station in stations){
   var account=session.business.ledger.Account;account.Consume(0,10000); // Leave room to exercise a real purchase.
   Check(station.name.Contains("게임시설"),"game status "+station.name);
   foreach(var stop in session.jobs.stops)Check(graph.Route(stop.laneEdge,stop.laneAt,station.approach.laneEdge,station.approach.laneAt)!=null,"shared route "+stop.name+" → "+station.id);
   float routeAt=station.approach.laneAt;var roadPath=graph.paths[station.approach.laneEdge];var initial=roadPath.Position(routeAt-5);
   taxi.SetSpawn(initial+Vector3.up*.65f,Quaternion.LookRotation(roadPath.Tangent(routeAt-5)));taxi.ResetTaxi();taxi.automated=true;taxi.autoBrake=true;
   yield return new WaitForSeconds(.8f);
   session.services.Toggle();Check(session.services.open,"station menu opens safely");Check(session.services.GuideFuel(Index(stations,station)),"guide specific station");Check(session.jobs.nav.hdGoal==station.approach.laneEdge&&Mathf.Abs(session.jobs.nav.hdGoalAt-station.approach.laneAt)<.01f,"H/navigation destination is entrance approach");
   Check(!station.CanRefuel(station.entrance,station.direction,0),"entrance does not refuel");Check(!station.CanRefuel(station.pump,-station.direction,0),"opposite pump heading does not refuel");Check(!station.CanRefuel(station.pump+station.direction*3.1f,station.direction,0),"outside pump does not refuel");
   session.services.Toggle();int cash=account.Cash;Check(!session.services.Buy(0)&&account.Cash==cash,"roadside Buy rejects without charge");session.services.Toggle();Time.timeScale=8;
   var first=new List<Vector3>{initial,station.approach.V()};for(int i=0;i<25;i++)first.Add(station.drivePath[i]);first.Add(station.pump);
   var second=new List<Vector3>{station.pump};for(int i=26;i<station.drivePath.Length;i++)second.Add(station.drivePath[i]);second.Add(roadPath.Position(routeAt+28+35));
   Geometry(new AssistedRoadNetwork.Path(first.ToArray()),station.id+" entrance");Geometry(new AssistedRoadNetwork.Path(second.ToArray()),station.id+" exit");
   sideContacts=curbSamples=0;maxError=0;
   yield return Drive(first,station.id+" entrance to pump");
   Check(session.services.FuelAtPump==Index(stations,station),"actual taxi in pump bay "+station.id);
   session.services.Toggle();Check(session.services.open,"F7 at actual pump");Check(session.services.RequestAction(0),"request actual pump purchase");
   float fuel=account.Fuel;cash=account.Cash;double elapsed=session.business.ledger.Elapsed;
   session.business.enabledCampaign=true;
   yield return new WaitForSecondsRealtime(.2f);Check(Time.timeScale==0&&session.paused&&Math.Abs(session.business.ledger.Elapsed-elapsed)<.001,"menu freezes enabled operating clock");session.business.enabledCampaign=false;
   if(Array.IndexOf(Environment.GetCommandLineArgs(),"-captureFuelStations")>=0)Capture(station);
   Check(session.services.ConfirmAction(true),"actual pump purchase "+station.id);Check(account.Fuel>fuel&&account.Cash<cash,"fuel added and charged");Check(Math.Abs(session.business.ledger.Elapsed-elapsed-10)<.01,"exactly ten service seconds");
   Time.timeScale=8;yield return Drive(second,station.id+" pump to separate exit");
   Check(!session.services.AtFuelPump(),"after exit no refueling");Check(Vector3.Dot(taxi.transform.forward,station.direction)>.9f,"exit joins same traffic direction");
   Check(sideContacts==0&&curbSamples==0,"no side collision/curb contact "+station.id+" sides="+sideContacts+" curbSamples="+curbSamples+" maxError="+maxError.ToString("0.00"));
  }
  taxi.autoBrake=true;Time.timeScale=1;string text="success="+success+"\n"+report;string path=Path.GetFullPath("artifacts/fuel-stations-physical-test.txt");Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,text);Debug.Log(text);Application.Quit(success?0:1);
 }
 static int Index(IReadOnlyList<FuelStationLayout.FuelStation> stations,FuelStationLayout.FuelStation station){for(int i=0;i<stations.Count;i++)if(stations[i]==station)return i;return -1;}
 void Geometry(AssistedRoadNetwork.Path path,string label){
  bool onRoad=true,noCurbs=true;
  for(float at=0;at<path.length;at+=.75f){var p=path.Position(at);var dir=path.Tangent(at);var right=TrafficSystem.Right(dir);
   foreach(float x in new[]{-.9f,.9f})foreach(float z in new[]{-1.5f,1.5f})if(!session.roadEnvironment.OnRoad(p+right*x+dir*z))onRoad=false;
   foreach(var hit in Physics.OverlapBox(p+Vector3.up*.105f,new Vector3(.9f,.045f,1.9f),Quaternion.LookRotation(dir),1,QueryTriggerInteraction.Ignore))if(hit.name.Contains("연석")||hit.name.Contains("승하차 보도"))noCurbs=false;
  }
  Check(onRoad,"full vehicle footprint paved "+label);Check(noCurbs,"curb-free swept footprint "+label);
 }
 IEnumerator Drive(List<Vector3> points,string label){
  var path=new AssistedRoadNetwork.Path(points.ToArray());var taxi=session.taxi;float begin=Time.time,at=0;driving=true;bool arrived=false;
  taxi.assistance.SetActive(false);taxi.automated=true;taxi.autoBrake=false;taxi.autoServiceBrake=0;
  while(Time.time-begin<100){
   float error;at=path.ProjectRange(taxi.transform.position,Mathf.Max(0,at-2),Mathf.Min(path.length,at+6),out error);maxError=Mathf.Max(maxError,error);
   float remaining=path.length-at;var target=path.Position(Mathf.Min(path.length,at+2.8f));var local=taxi.transform.InverseTransformPoint(target+Vector3.up*taxi.transform.position.y);
   float angle=Mathf.Atan2(2*2.6f*local.x,Mathf.Max(1,local.x*local.x+local.z*local.z))*Mathf.Rad2Deg;
   taxi.autoSteer=Mathf.Clamp(angle/DriveMath.SteerAngle(taxi.Speed*3.6f),-1,1);
   float desired=Mathf.Min(2.2f,Mathf.Sqrt(Mathf.Max(0,remaining-.3f)*1.5f));float speed=taxi.body.velocity.magnitude;
   taxi.autoThrottle=Mathf.Clamp((desired-speed)*.5f,0,.5f);taxi.autoServiceBrake=speed>desired+.15f?Mathf.Clamp((speed-desired)*.7f,.08f,.6f):0;
   if(remaining<.65f){taxi.autoThrottle=0;taxi.autoBrake=true;if(speed<.12f){arrived=true;break;}}
   if(!session.roadEnvironment.OnRoad(taxi.transform.position))Check(false,"left drivable surface "+label);
   if(error>4){Check(false,"path deviation "+label+" error="+error);break;}
   foreach(var hit in Physics.OverlapBox(new Vector3(taxi.transform.position.x,.105f,taxi.transform.position.z),new Vector3(.88f,.045f,1.85f),taxi.transform.rotation,1,QueryTriggerInteraction.Ignore))if(hit.name.Contains("연석")||hit.name.Contains("승하차 보도"))curbSamples++;
   yield return new WaitForFixedUpdate();
  }
  taxi.autoThrottle=0;taxi.autoSteer=0;taxi.autoServiceBrake=0;taxi.autoBrake=true;yield return new WaitForSeconds(.5f);driving=false;
  Check(arrived,label+" physical completion seconds="+(Time.time-begin).ToString("0.0")+" error="+maxError.ToString("0.00"));
 }
 internal void Contact(Collision collision){if(!driving)return;foreach(var point in collision.contacts)if(Mathf.Abs(point.normal.y)<.6f){sideContacts++;break;}}
 void Capture(FuelStationLayout.FuelStation station){
  session.drivingCamera.enabled=false;var camera=Camera.main;var eye=station.pump-station.direction*32-TrafficSystem.Right(station.direction)*20+Vector3.up*26;camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(station.pump-eye));
  string path=Path.GetFullPath("artifacts/fuel-station-"+station.id+".png");var texture=new RenderTexture(1280,720,24);var previous=RenderTexture.active;camera.targetTexture=texture;camera.Render();RenderTexture.active=texture;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=previous;Destroy(pixels);texture.Release();Destroy(texture);
 }
}
public class FuelStationContactRecorder : MonoBehaviour {public FuelStationProbe owner;void OnCollisionEnter(Collision collision){if(owner)owner.Contact(collision);}}
