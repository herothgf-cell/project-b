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
  var probe=gameObject.AddComponent<FuelStationProbe>();probe.externallyDriven=true;probe.session=s;var points=OneDayTrafficDriveTest.RestaurantDeparture(site,s.jobs.nav.hd,site.parking);yield return probe.Drive(points,"restaurant exit regression");
  float at;var graph=s.jobs.nav.hd;int edge=graph.Nearest(t.transform.position,t.transform.forward,out at);Debug.Log("FACILITY_EXIT_RESULT passed="+probe.Passed+" position="+t.transform.position+" forward="+t.transform.forward+" edge="+edge+" at="+at);
  float best=float.MaxValue;for(int i=0;i<graph.paths.Length;i++){if(graph.edges[i].disabled||graph.edges[i].change!=0)continue;float error;float q=graph.paths[i].Project(t.transform.position,out error);if(error<best){best=error;Debug.Log("FACILITY_EXIT_NEAR error="+error+" lane="+graph.edges[i].id+" facing="+Vector3.Dot(t.transform.forward,graph.paths[i].Tangent(q))+" at="+q);}}
  bool ok=probe.Passed&&edge>=0;Application.Quit(ok?0:1);
 }
}
