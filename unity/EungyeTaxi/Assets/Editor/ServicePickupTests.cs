using System;
using UnityEngine;

public static class ServicePickupTests {
 static void Check(bool ok,string message){if(!ok)throw new Exception("SERVICE_PICKUP "+message);Debug.Log("SERVICE_PICKUP_PASS "+message);}
 public static void Run(){
  var root=new GameObject("service pickup regression");
  try{
   MapCatalog.LockedRegion="eungye";
   var s=root.AddComponent<DriveSession>();s.world=root.AddComponent<WorldBuilder>();s.world.map=MapCatalog.Load();
   FuelStationLayout.Apply(s.world.map);
   s.jobs=root.AddComponent<PassengerJobs>();s.jobs.stops=s.world.map.stops;
   s.business=root.AddComponent<BusinessProgress>();s.business.settings=PrototypeSettings.ForThreeDays();s.business.ledger=new EconomyLedger(s.business.settings);
   s.taxi=root.AddComponent<TaxiController>();s.taxi.body=root.AddComponent<Rigidbody>();s.taxi.body.isKinematic=true;
   var services=root.AddComponent<ServiceStops>();services.session=s;
   Check(services.Stations.Count==3,"three real fuel stations loaded");
   foreach(var station in services.Stations){
    root.transform.SetPositionAndRotation(station.pump,Quaternion.LookRotation(station.direction));
    s.jobs.trip.Clear();s.jobs.trip.Accept(5000,120,90);
    Check(services.CanUse(0),"accepted pickup may refuel at "+station.id);
    Check(services.CanUse(4),"accepted pickup may wash at "+station.id);
    services.open=true;Check(services.RequestAction(4),"wash confirmation available");
    Check(!services.ConfirmAction(false)&&s.jobs.trip.State==TripState.ToPickup&&s.jobs.trip.PickupRemaining==90,"cancel preserves pickup and deadline");
    s.jobs.trip.Board();Check(!services.CanUse(0)&&!services.CanUse(4),"occupied taxi cannot refuel/wash");
    s.jobs.trip.Clear();root.transform.position=station.pump+Vector3.right*100;
    Check(!services.CanUse(0)&&!services.CanUse(4),"off-site purchase remains blocked");
   }
   Debug.Log("SERVICE_PICKUP_TESTS_OK");
  }finally{UnityEngine.Object.DestroyImmediate(root);Time.timeScale=1;}
 }
}
