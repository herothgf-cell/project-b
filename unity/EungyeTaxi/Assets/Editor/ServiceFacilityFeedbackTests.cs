using System;
using UnityEngine;

public static class ServiceFacilityFeedbackTests {
 static void Check(bool ok,string message){if(!ok)throw new Exception("SERVICE_FACILITY_FEEDBACK: "+message);Debug.Log("SERVICE_FACILITY_PASS "+message);}
 static string Actions(ServiceStops services){var property=typeof(ServiceStops).GetProperty("LocalActions");Check(property!=null,"facility menu exposes its actual local action rows");return string.Join(",",(int[])property.GetValue(services,null));}
 public static void RunPickupMap(){RunPickupMap(false);}
 public static void RunPickupReturn(){RunPickupMap(true);}
 static void RunPickupMap(bool returnToPickup){
  var root=new GameObject("pickup map gas guidance regression");
  try{
   var s=root.AddComponent<DriveSession>();MapCatalog.LockedRegion="eungye";s.world=root.AddComponent<WorldBuilder>();s.world.map=MapCatalog.Load();FuelStationLayout.Apply(s.world.map);
   s.jobs=root.AddComponent<PassengerJobs>();s.jobs.session=s;s.jobs.stops=s.world.map.stops;s.jobs.nav=root.AddComponent<RoadNavigation>();s.jobs.nav.Build(s.world.map,root.transform);
   var marker=new GameObject("fixture passenger destination marker");marker.transform.SetParent(root.transform,false);typeof(PassengerJobs).GetField("marker",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(s.jobs,marker);
   s.business=root.AddComponent<BusinessProgress>();s.business.settings=PrototypeSettings.ForThreeDays();s.business.ledger=new EconomyLedger(s.business.settings);s.taxi=root.AddComponent<TaxiController>();s.taxi.body=root.AddComponent<Rigidbody>();s.taxi.body.isKinematic=true;s.law=root.AddComponent<DrivingLaw>();s.services=root.AddComponent<ServiceStops>();s.services.session=s;
   var map=root.AddComponent<FullMapOverlay>();map.session=s;s.paused=false;Time.timeScale=1;var station=s.services.Stations[0];root.transform.SetPositionAndRotation(station.approach.V()-station.direction*12,Quaternion.LookRotation(station.direction));var accepted=new JobOffer{pickup=0,drop=1,fare=5000,seconds=120};s.jobs.current=accepted;s.jobs.trip.Accept(5000,120,90);
   Check(map.Open(),"pickup can open M map");var place=map.Places().Find(p=>p.glyph==8&&p.approach==station.approach);Check(map.SelectPlace(place)&&map.PreviewPointCount>1,"pickup previews actual connected gas route");
   Check(map.StartPlaceGuidance(),"pickup explicitly starts gas guidance from M");Check(!map.IsOpen&&!s.paused&&Time.timeScale==1&&s.jobs.current==accepted&&s.jobs.trip.State==TripState.ToPickup&&s.jobs.trip.PickupRemaining==90,"gas guidance preserves request and waiting clock and resumes driving");Check(s.jobs.nav.hdGoal==station.approach.laneEdge,"gas guidance targets selected station entrance");
   Check(map.Open(),"pickup map can reopen");Check(s.jobs.nav.hdGoal==station.approach.laneEdge,"opening M preserves the chosen gas diversion until explicit return");var meal=map.Places().Find(p=>p.glyph==9);Check(map.SelectPlace(meal)&&!map.StartPlaceGuidance(),"pickup map does not route away for unrelated restaurant service");
   if(returnToPickup){var pickup=s.jobs.stops[accepted.pickup];Check(pickup.laneEdge!=station.approach.laneEdge,"return fixture distinguishes pickup from gas lane");Check(map.RestoreTripRoute(),"explicit return exits facility preview");Check(s.jobs.nav.hdGoal==pickup.laneEdge&&Mathf.Abs(s.jobs.nav.hdGoalAt-pickup.laneAt)<.01f,"return from gas diversion restores actual pickup navigation destination");Check(s.jobs.current==accepted&&s.jobs.trip.State==TripState.ToPickup&&s.jobs.trip.PickupRemaining==90,"return preserves accepted request and pickup deadline");Check(s.services.destination=="","return clears obsolete gas guidance TIP");}
   map.Close();
   s.jobs.trip.Board();Check(map.Open()&&map.SelectPlace(place)&&!map.StartPlaceGuidance(),"occupied taxi cannot divert to gas service");map.Close();
   Debug.Log("SERVICE_PICKUP_MAP_TESTS_OK");
  }finally{UnityEngine.Object.DestroyImmediate(root);Time.timeScale=1;}
 }
 public static void Run(){
  var root=new GameObject("facility menu regression");
  try{
   var s=root.AddComponent<DriveSession>();MapCatalog.LockedRegion="eungye";s.world=root.AddComponent<WorldBuilder>();s.world.map=MapCatalog.Load();FuelStationLayout.Apply(s.world.map);
   s.jobs=root.AddComponent<PassengerJobs>();s.jobs.stops=s.world.map.stops;
   s.business=root.AddComponent<BusinessProgress>();s.business.settings=PrototypeSettings.ForThreeDays();s.business.ledger=new EconomyLedger(s.business.settings);
   s.taxi=root.AddComponent<TaxiController>();s.taxi.body=root.AddComponent<Rigidbody>();s.taxi.body.isKinematic=true;s.law=root.AddComponent<DrivingLaw>();
   s.services=root.AddComponent<ServiceStops>();s.services.session=s;s.paused=false;Time.timeScale=1;
   root.transform.position=new Vector3(-100,0,-100);s.services.Toggle();
   Check(!s.services.open&&!s.paused&&Time.timeScale==1,"outside every facility F7 keeps driving and does not open the full service catalogue");
   Check(s.services.notice.Contains("M"),"outside facility directs player to the map");
   foreach(var station in s.services.Stations){
    root.transform.SetPositionAndRotation(station.pump,Quaternion.LookRotation(station.direction));s.services.Toggle();
    Check(s.services.open&&s.services.FuelMenuOnly,"pump opens local fuel menu");
    Check(Actions(s.services)=="0,4","pump menu shows fuel and wash rows only");
    Check(s.services.CanUse(0)&&s.services.CanUse(4)&&!s.services.CanUse(1)&&!s.services.CanUse(2),"existing station permits fuel and wash but no remote meal or end-day");
    s.services.Toggle();
   }
   var restaurant=s.services.Restaurant;root.transform.SetPositionAndRotation(restaurant.parking,Quaternion.LookRotation(restaurant.direction));s.services.Toggle();
   Check(s.services.open&&s.services.CanUse(1)&&!s.services.CanUse(0)&&!s.services.CanUse(4),"restaurant permits meal only");Check(Actions(s.services)=="1","restaurant menu shows meal row only");s.services.Toggle();
   s.jobs.trip.Accept(5000,120,90);root.transform.position=new Vector3(-100,0,-100);s.services.Toggle();
   Check(!s.services.open&&s.jobs.trip.State==TripState.ToPickup&&s.jobs.trip.PickupRemaining==90,"pickup outside facility cannot pause through a remote service catalogue");
   Debug.Log("SERVICE_FACILITY_FEEDBACK_TESTS_OK");
  }finally{UnityEngine.Object.DestroyImmediate(root);Time.timeScale=1;}
 }
}
