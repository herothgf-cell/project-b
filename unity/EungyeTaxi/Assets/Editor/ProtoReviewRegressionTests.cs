using System;
using System.Reflection;
using UnityEngine;

public static class ProtoReviewRegressionTests {
 static int failed;
 static void Check(bool ok,string text){Debug.Log((ok?"REVIEW_PASS ":"REVIEW_FAIL ")+text);if(!ok)failed++;}
 public static void Run(){
  failed=0;var root=new GameObject("review regression");
  try{
   var s=root.AddComponent<DriveSession>();s.business=root.AddComponent<BusinessProgress>();s.business.useV2=true;s.business.enabledCampaign=true;s.business.ledger=new EconomyLedger(PrototypeSettings.ForThreeDays());
   s.jobs=root.AddComponent<PassengerJobs>();s.jobs.session=s;s.jobs.nav=root.AddComponent<RoadNavigation>();s.jobs.trip.Accept(5000,300,90);s.jobs.trip.Board();
   s.taxi=root.AddComponent<TaxiController>();s.taxi.body=root.AddComponent<Rigidbody>();s.taxi.body.useGravity=false;s.traffic=root.AddComponent<TrafficSystem>();s.law=root.AddComponent<DrivingLaw>();s.law.tripId="regression";s.manners=root.AddComponent<DrivingManners>();s.manners.session=s;s.requests=root.AddComponent<PassengerRequestUI>();s.requests.session=s;
   s.requests.BeginKind(RequestKind.LargeRoad);Check(s.requests.request.Kind==RequestKind.LargeRoad&&s.requests.request.Resolved&&s.requests.request.SatisfactionDelta==0,"late unavailable large road keeps assigned identity and closes neutrally");
   s.requests.BeginKind(RequestKind.Quiet);var worker=root.GetComponent<PassengerRequestObservation>();typeof(PassengerRequestObservation).GetField("stoppedSeconds",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(worker,3f);worker.Observe(false);Check(!worker.request.Snapshot().opportunity,"hidden pending dialogue does not acquire response opportunity");
   MapCatalog.LockedRegion="eungye";var map=MapCatalog.Load();FuelStationLayout.Apply(map);var catalog=PassengerStopCatalog.For(map);typeof(PassengerJobs).GetProperty("Catalog").GetSetMethod(true).Invoke(s.jobs,new object[]{catalog});s.jobs.stops=catalog.Stops;
   var stop=catalog.Stops[catalog.LegacyCount];root.transform.position=stop.Kerb-stop.Direction*5.5f;var occupancy=root.AddComponent<StopOccupancySystem>();occupancy.session=s;occupancy.Restore(new StopOccupancySnapshot{active=true,eventId="restore-test",stopId=catalog.Entry(catalog.LegacyCount).StopId,remaining=10});
   typeof(StopOccupancySystem).GetField("lastElapsed",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(occupancy,0d);s.business.ledger.AdvanceOperation(1,0,false);typeof(StopOccupancySystem).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(occupancy,null);Check(occupancy.Capture().remaining==9,"restored bay countdown advances with taxi waiting 5.5 metres behind");
   s.business.ledger.RequestClose();s.business.ledger.Settle();s.business.ledger.NextDay();typeof(StopOccupancySystem).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(occupancy,null);Check(!occupancy.Capture().active&&occupancy.Capture().nextAt==60,"new business day clears yesterday occupancy and restarts event clock");
   var old=occupancy.Capture();old.active=true;old.eventId="old campaign";old.stopId=catalog.Entry(catalog.LegacyCount).StopId;old.remaining=10;occupancy.Restore(old);s.law.ids=new PrototypeIds("new-campaign");typeof(StopOccupancySystem).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(occupancy,null);Check(!occupancy.Capture().active&&occupancy.Capture().campaign=="new-campaign","explicit new campaign cannot inherit compensated occupancy");
  }finally{UnityEngine.Object.DestroyImmediate(root);}
  if(failed>0)throw new Exception("Review regressions: "+failed);Debug.Log("PROTO_REVIEW_OK");
 }
 public static void RunFinal(){Run();ProtoExpansionTests.RunP12();ProtoExpansionTests.RunP13();CampaignCheckpointWriterTests.Run();ServiceStartupFollowupTests.Run();Debug.Log("PROTO_FINAL_EDITOR_OK");}
}
