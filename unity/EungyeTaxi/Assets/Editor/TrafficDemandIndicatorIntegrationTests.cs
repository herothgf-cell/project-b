using System;
using UnityEngine;

// Invoked by the coordinated Editor validation. No scene load or user save IO.
public static class TrafficDemandIndicatorIntegrationTests {
 static void Check(bool value,string reason){if(!value)throw new Exception("TRAFFIC_DEMAND_INDICATOR: "+reason);}
 public static void Run(){
  var owner=new GameObject("profile integration owner");var taxiObject=new GameObject("manual indicator integration");var cars=new GameObject[3];
  try{
   var system=owner.AddComponent<TrafficSystem>();system.SendMessage("Awake");system.manualClock=true;system.manualTime=0;var route=new TrafficRoute();route.SetPoints(new[]{Vector3.zero,Vector3.forward*200});
   var red=new TrafficJunction{name="profile red fixture",scenarioSignal=true,scenarioForward=SignalColor.Red,observationAxis=0,observationSign=1};system.junctions.Add(red);
   route.stops.Add(new TrafficStop{junction=red,axis=0,turn=0,distance=15,exitDistance=50});
   for(int i=0;i<3;i++){
    cars[i]=new GameObject("profile "+i);var car=cars[i].AddComponent<TrafficCar>();car.enabled=false;car.Initialize(system,route,0,i);
    Check(car.Profile.Kind==(TrafficTemperament)i&&car.ProfileLabel.Length>0,"stable runtime profile assignment "+i);
    Check(cars[i].GetComponent<BoxCollider>().enabled,"physical body retained for profile "+i);
    Check(car.Profile.LimitSpeed(0,9)==0,"red/occupied gap stops profile "+i);
    car.Place(14.9f);for(int tick=0;tick<160;tick++)car.SendMessage("Tick");
    Check(car.distance<=15&&car.speed<.1f&&car.waitingForSignal,"real red stop retained for profile "+i);
    Check(car.stoppedAt==red,"actual stop-line evidence reachable for profile "+i);
    cars[i].SetActive(false);
   }
   var taxi=taxiObject.AddComponent<TaxiController>();taxi.body=taxiObject.AddComponent<Rigidbody>();taxi.body.isKinematic=true;
   var signals=taxiObject.AddComponent<TaxiSignals>();signals.Toggle(1);
   signals.ObserveManualMotion(4,20,.5f,Vector3.forward, .1f,false);signals.ObserveManualMotion(4,40,.5f,new Vector3(1,0,2),.1f,false);
   signals.ObserveManualMotion(0,40,0,new Vector3(1,0,2),40,false);Check(signals.direction==1,"runtime red wait has no timeout cancellation");
   signals.ObserveManualMotion(4,40,0,new Vector3(2,0,3),.5f,false);
   var snapshot=signals.CaptureLive();signals.Clear();signals.RestoreLive(snapshot);
   signals.ObserveManualMotion(4,40,0,new Vector3(3,0,4),.5f,false);Check(signals.direction==0,"runtime snapshot resumes real turn cancellation");
   signals.Toggle(-1);signals.ToggleHazards();signals.ObserveManualMotion(4,320,0,new Vector3(-3,0,4),5,false);Check(signals.Hazards&&signals.direction==0,"hazards remain authoritative");
   signals.Clear();signals.Toggle(1);signals.ObserveManualMotion(4,40,.5f,Vector3.one,.1f,false);signals.ObserveManualMotion(4,40,0,Vector3.one,2,true);Check(signals.direction==1,"assisted owner retains signal");
   signals.RestoreLive(null);Check(!signals.Hazards&&signals.direction==0,"old checkpoint gets neutral signals");
   var ownCity=new MapData{mapId="fictional-pvp",stops=new[]{new MapStop(),new MapStop(),new MapStop()}};
   var regions=SimulationDemandClock.NativeRegions(ownCity);Check(regions[0]=="residential"&&regions[1]=="office"&&regions[2]=="commercial","fictional own-city demand directions");
   Debug.Log("TRAFFIC_DEMAND_INDICATOR_INTEGRATION_OK profiles=3 body-gates=3 manual=7 demand=1");
  }finally{
   foreach(var car in cars)if(car)UnityEngine.Object.DestroyImmediate(car);UnityEngine.Object.DestroyImmediate(taxiObject);UnityEngine.Object.DestroyImmediate(owner);
  }
 }
}
