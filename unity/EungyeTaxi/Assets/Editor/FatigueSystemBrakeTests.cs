using System;
using UnityEngine;
public static class FatigueSystemBrakeTests {
 static void Check(bool pass,string why){if(!pass)throw new Exception("FATIGUE_SYSTEM_BRAKE_FAIL "+why);Debug.Log("PASS "+why);}
 public static void Run(){
  var root=new GameObject("fatigue system brake grading regression");
  try{
   var s=root.AddComponent<DriveSession>();s.taxi=root.AddComponent<TaxiController>();s.taxi.body=root.AddComponent<Rigidbody>();s.taxi.body.useGravity=false;
   s.business=root.AddComponent<BusinessProgress>();s.business.session=s;s.business.ledger=new EconomyLedger(PrototypeSettings.ForThreeDays());
   s.jobs=root.AddComponent<PassengerJobs>();s.jobs.session=s;s.services=root.AddComponent<ServiceStops>();s.law=root.AddComponent<DrivingLaw>();
   s.traffic=root.AddComponent<TrafficSystem>();s.assisted=root.AddComponent<AssistedDriveController>();
   s.manners=root.AddComponent<DrivingManners>();s.manners.session=s;
   Time.timeScale=1;Check(!s.manners.NecessaryBraking(),"ordinary free-road braking is not exempt");
   s.business.ledger.Driver.fatigue=100;s.business.ledger.Driver.drowsiness.Advance(180,true,100);
   s.taxi.body.velocity=Vector3.forward*16;typeof(BusinessProgress).GetMethod("FixedUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(s.business,null);
   Check(s.taxi.body.velocity.z<16,"real system stop reduces velocity");
   Check(s.manners.NecessaryBraking(),"system-generated fatigue braking must not become passenger harsh braking");
   s.business.ledger.Driver.fatigue=80;s.business.ledger.Driver.drowsiness.Recover();
   Check(!s.manners.NecessaryBraking(),"manual braking after recovery is not permanently exempt");
   Debug.Log("FATIGUE_SYSTEM_BRAKE_TESTS_OK");
  }finally{UnityEngine.Object.DestroyImmediate(root);Time.timeScale=1;}
 }
}
