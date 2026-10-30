using System;
using UnityEngine;
public static class PvpLiveMapTests {
 public static void Run(){Case(false,false);Case(true,false);Case(true,true);Debug.Log("PVP_LIVE_MAP_OK");}
 static void Case(bool pvp,bool paused){
  float scale=Time.timeScale;var root=new GameObject("map policy test");root.SetActive(false);
  try {
   var s=root.AddComponent<DriveSession>();s.jobs=root.AddComponent<PassengerJobs>();s.jobs.session=s;
   s.business=root.AddComponent<BusinessProgress>();s.business.session=s;s.business.enabledCampaign=true;s.business.ledger=new EconomyLedger(PrototypeSettings.ForOneDay());
   s.services=root.AddComponent<ServiceStops>();s.law=root.AddComponent<DrivingLaw>();
   s.taxi=root.AddComponent<TaxiController>();s.taxi.business=s.business;s.taxi.body=root.AddComponent<Rigidbody>();
   if(pvp)s.simulationMatch=root.AddComponent<PvpSimulationMatch>();
   var map=root.AddComponent<FullMapOverlay>();map.session=s;
   s.paused=paused;Time.timeScale=paused?0:1;map.Open();
   Check(map.IsOpen,"map opens");bool live=pvp&&!paused;
   Check(Time.timeScale==(live?1:0)&&s.paused==!live,"PVP map preserves live simulation; campaign and Esc stay paused");
   Check(s.business.DrowsinessRunning==live,"live PVP map advances native operation/fatigue; campaign and pause do not");
   if(live){double before=s.business.ledger.Elapsed;typeof(BusinessProgress).GetMethod("FixedUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(s.business,null);Check(s.business.ledger.Elapsed>before,"native operating clock advances through live map");}
   if(pvp){var input=typeof(TaxiController).GetProperty("PvpMapInputBlocked");Check(input!=null&&(bool)input.GetValue(s.taxi,null),"map blocks human driving input");}
   map.Close();Check(s.paused==paused&&Time.timeScale==(paused?0:1),"close restores prior pause state");
   if(pvp)Check(!(bool)typeof(TaxiController).GetProperty("PvpMapInputBlocked").GetValue(s.taxi,null),"close releases input guard");
  }finally{UnityEngine.Object.DestroyImmediate(root);Time.timeScale=scale;}
 }
 static void Check(bool value,string message){if(!value)throw new Exception("PVP_LIVE_MAP: "+message);}
}