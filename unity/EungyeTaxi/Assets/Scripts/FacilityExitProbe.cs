using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Position is set once at the existing parking bay; departure is physical.
public sealed class FacilityExitProbe : MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-restaurantExitTest")>=0)new GameObject("Restaurant departure regression").AddComponent<FacilityExitProbe>();}
 IEnumerator Start(){
  DriveSession s;while(!(s=FindObjectOfType<DriveSession>())||!s.StartupReady)yield return null;yield return null;yield return null;
  var site=s.services.Restaurant;var t=s.taxi;t.SetSpawn(site.parking+Vector3.up*.65f,Quaternion.LookRotation(site.direction));t.ResetTaxi();t.automated=true;t.autoBrake=true;s.business.preparing=false;s.paused=false;Time.timeScale=4;
  // Exercise the same M actions as a driver before moving the taxi.
  var map=s.GetComponent<FullMapOverlay>();var jobs=s.jobs;
  s.business.taxPrompt=false;jobs.ClearForDay();
  var station=s.services.Stations[0];
  bool place=map.Open()&&map.SelectRoadPoint(station.approach.V())&&map.PreviewPointCount>1&&map.StartPlaceGuidance();
  jobs.Generate();int selected=Array.FindIndex(jobs.offers,o=>o!=null&&jobs.AcceptanceRejection(o)=="");
  bool call=place&&selected>=0&&map.Open()&&map.SelectOffer(selected)&&map.PickupPreviewPointCount>1&&map.AcceptSelected();
  if(!call||jobs.nav.path.Count<2||jobs.nav.hdRoute!=null){
   Debug.LogError("FACILITY_DISPATCH_FAIL at restaurant place="+place+" offer="+selected+" accept="+call+" feedback="+map.AcceptFeedback+" message="+jobs.message);
   Application.Quit(1);yield break;
  }
  int goal=jobs.nav.hdGoal;float goalAt=jobs.nav.hdGoalAt;
  Debug.Log("FACILITY_DISPATCH_INTERIOR place="+place+" accept="+call+" pickup="+jobs.current.pickup+" metres="+jobs.nav.remaining);
  var probe=gameObject.AddComponent<FuelStationProbe>();probe.externallyDriven=true;probe.session=s;var displayed=new AssistedRoadNetwork.Path(jobs.nav.path.ToArray());
  float exitError;float exitAt=displayed.Project(site.leave[site.leave.Length-1],out exitError);
  if(exitError>.5f||displayed.length<exitAt+12){Debug.LogError("FACILITY_DISPATCH_FAIL pickup guidance misses authored driveway exit");Application.Quit(1);yield break;}
  // Physically follow the driver's accepted pickup guidance through the exit
  // and twelve metres onto its selected road, with no pose reset.
  var points=new List<Vector3>();for(float d=0;d<exitAt+12;d+=.75f)points.Add(displayed.Position(d));
  points.Add(displayed.Position(exitAt+12));yield return probe.Drive(points,"restaurant exit regression");
  float at;var graph=s.jobs.nav.hd;int edge=graph.Nearest(t.transform.position,t.transform.forward,out at);Debug.Log("FACILITY_EXIT_RESULT passed="+probe.Passed+" position="+t.transform.position+" forward="+t.transform.forward+" edge="+edge+" at="+at);
  float best=float.MaxValue;for(int i=0;i<graph.paths.Length;i++){if(graph.edges[i].disabled||graph.edges[i].change!=0)continue;float error;float q=graph.paths[i].Project(t.transform.position,out error);if(error<best){best=error;Debug.Log("FACILITY_EXIT_NEAR error="+error+" lane="+graph.edges[i].id+" facing="+Vector3.Dot(t.transform.forward,graph.paths[i].Tangent(q))+" at="+q);}}
  jobs.nav.Refresh();bool ok=probe.Passed&&edge>=0&&jobs.trip.State==TripState.ToPickup&&jobs.nav.hdGoal==goal&&jobs.nav.hdGoalAt==goalAt&&jobs.nav.hdRoute!=null&&jobs.nav.path.Count>1;Debug.Log("FACILITY_DISPATCH_REJOIN passed="+ok+" metres="+jobs.nav.remaining+" goal="+jobs.nav.hdGoal);Application.Quit(ok?0:1);
 }
}
