using System;
using System.Reflection;
using UnityEngine;
public static class StopOccupancyAlternativeIntegrationTests {
 static void Check(bool value,string message){if(!value)throw new Exception("OCCUPANCY_ALTERNATIVE: "+message);}
 public static void Run(){
  float priorTimeScale=Time.timeScale;int priorFrameRate=Application.targetFrameRate,priorVSync=QualitySettings.vSyncCount;bool priorBackground=Application.runInBackground;string priorRegion=MapCatalog.LockedRegion;DriveSession fixture=null;
  var root=new GameObject("alternative integration");
  try{
   MapData map;
#if UNITY_EDITOR
   map=ProjectMapStore.Load(System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../.."))).Compose();
#else
   map=MapCatalog.Load();
#endif
   FuelStationLayout.Apply(map);var catalog=PassengerStopCatalog.For(map);
   int index=catalog.IndexOf("EG-GAME-02");var alternative=catalog.AlternativeFor("EG-GAME-02");Check(index>=0&&alternative!=null,"catalog pair available");
   var s=root.AddComponent<DriveSession>();fixture=s;s.paused=false;Time.timeScale=1;Check(!s.paused&&Time.timeScale>0,"fixture session is running");s.world=root.AddComponent<WorldBuilder>();s.world.map=map;s.business=root.AddComponent<BusinessProgress>();s.business.session=s;s.business.ledger=new EconomyLedger(PrototypeSettings.ForOneDay());typeof(BusinessProgress).GetProperty("SimulationSession").GetSetMethod(true).Invoke(s.business,new object[]{true});s.law=root.AddComponent<DrivingLaw>();s.law.tripId="trip-test";
   s.taxi=root.AddComponent<TaxiController>();s.taxi.body=root.AddComponent<Rigidbody>();s.traffic=root.AddComponent<TrafficSystem>();
   s.jobs=root.AddComponent<PassengerJobs>();s.jobs.session=s;typeof(PassengerJobs).GetProperty("Catalog").GetSetMethod(true).Invoke(s.jobs,new object[]{catalog});s.jobs.stops=catalog.Stops;s.jobs.people=new GameObject[catalog.Stops.Length];s.jobs.cooldown=new float[catalog.Stops.Length];var ids=(System.Collections.Generic.List<string>)typeof(PassengerJobs).GetField("runtimeStopIds",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(s.jobs);foreach(var e in catalog.Entries)ids.Add(e.StopId);
   s.jobs.people[index]=new GameObject("pickup person");s.jobs.people[index].transform.SetParent(root.transform);s.jobs.people[index].transform.position=catalog.Stops[index].Passenger;
   s.jobs.nav=root.AddComponent<RoadNavigation>();s.jobs.nav.Build(map,root.transform);var marker=new GameObject("marker");marker.transform.SetParent(root.transform);typeof(PassengerJobs).GetField("marker",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(s.jobs,marker);
   s.jobs.current=new JobOffer{pickup=index,drop=0,fare=5000,timingPolicyVersion=1,safeRouteSeconds=300};s.jobs.trip.Accept(5000,300,90);
   var lane=s.jobs.nav.hd.paths[catalog.Stops[index].laneEdge];float poseAt=Mathf.Max(0,catalog.Stops[index].laneAt-20);root.transform.SetPositionAndRotation(lane.Position(poseAt),Quaternion.LookRotation(lane.Tangent(poseAt)));
   var unrelated=new JobOffer{pickup=index,drop=0,fare=5000,timingPolicyVersion=1,safeRouteSeconds=300};var original=OperationForecastAdapter.ForSelection(s,s.jobs.current);var otherBefore=OperationForecastAdapter.ForSelection(s,unrelated);Check(original.Available&&otherBefore.Available,"original and unrelated offer forecasts available");
   var occupancy=root.AddComponent<StopOccupancySystem>();occupancy.session=s;
   occupancy.Restore(new StopOccupancySnapshot{version=2,active=true,eventId="event",stopId="EG-GAME-02",remaining=10,alternativeAccepted=true,alternativePickup=true,alternativeId=alternative.Id,alternativeTrip="trip-test",walkingMetres=alternative.WalkingMetres,walkedMetres=12});
   MapStop effective;Check(occupancy.TryGetAlternative("EG-GAME-02",out effective)&&ReferenceEquals(effective,alternative.Stop),"restored agreement selects the physical bay");
   var changed=OperationForecastAdapter.ForActiveTrip(s);float pickupDistance=PassengerJobs.DirectedDistance(s.jobs.nav.hd,s.taxi.transform.position,s.taxi.transform.forward,effective);var changedRide=s.jobs.nav.hd.Route(effective.laneEdge,effective.laneAt,catalog.Stops[0].laneEdge,catalog.Stops[0].laneAt);Check(changed.Available&&changedRide!=null&&Math.Abs(changed.TravelMetres-(pickupDistance+changedRide.path.length))<.01,"active forecast follows accepted alternative pickup and onward route");Check(!ReferenceEquals(changed,original),"alternative change invalidates cached active selection");var otherAfter=OperationForecastAdapter.ForSelection(s,unrelated);Check(otherAfter.Available&&Math.Abs(otherBefore.TravelMetres-otherAfter.TravelMetres)<.01,"unrelated offer remains on original pickup");
   Check(Vector3.Distance(s.jobs.people[index].transform.position,alternative.WalkingPath[0])>11.9f,"restored passenger resumes at saved walking distance");
   root.transform.SetPositionAndRotation(alternative.Stop.Kerb,Quaternion.LookRotation(alternative.Stop.Direction));Check(!s.jobs.At(index),"passenger cannot board before walking arrival even at bay");root.transform.SetPositionAndRotation(catalog.Stops[index].Kerb-catalog.Stops[index].Direction*10,Quaternion.LookRotation(catalog.Stops[index].Direction));
   var bad=occupancy.Capture();bad.alternativeId="invented";bool rejected=false;try{occupancy.Restore(bad);}catch(ArgumentException){rejected=true;}Check(rejected,"unknown saved pair fails closed");
   occupancy.Restore(new StopOccupancySnapshot{active=true,eventId="event",stopId="EG-GAME-02",remaining=10});
   var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.transform.position=(alternative.WalkingPath[0]+alternative.WalkingPath[1])*.5f+Vector3.up;Physics.SyncTransforms();
   Check(!occupancy.Resolve(true),"blocked path prevents agreement");UnityEngine.Object.DestroyImmediate(blocker);Physics.SyncTransforms();
   s.taxi.body.velocity=Vector3.forward;Check(!occupancy.Resolve(true),"driver must explicitly stop");s.taxi.body.velocity=Vector3.zero;
   Check(occupancy.Resolve(true),"stopped driver accepts clear alternative");Check(s.jobs.current.pickup==index&&s.jobs.current.fare==5000,"original trip identity and fare preserved");
   var advance=typeof(StopOccupancySystem).GetMethod("AdvanceAlternativeRuntime",BindingFlags.NonPublic|BindingFlags.Instance);advance.Invoke(occupancy,new object[]{10f});Check(Mathf.Abs(occupancy.Capture().walkedMetres-12)<.01f,"actual person advances at 1.2 metres per second");
   var ready=occupancy.Capture();ready.walkedMetres=ready.walkingMetres;occupancy.Restore(ready);root.transform.SetPositionAndRotation(alternative.Stop.Kerb,Quaternion.LookRotation(alternative.Stop.Direction));Physics.SyncTransforms();Check(s.jobs.At(index),"arrival at bay boards only when walker is ready");blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.transform.position=alternative.Stop.Kerb+Vector3.up;Physics.SyncTransforms();Check(!s.jobs.At(index),"new bay obstruction prevents boarding");UnityEngine.Object.DestroyImmediate(blocker);root.transform.SetPositionAndRotation(catalog.Stops[index].Kerb-catalog.Stops[index].Direction*10,Quaternion.LookRotation(catalog.Stops[index].Direction));ready.walkedMetres=12;occupancy.Restore(ready);
   var before=s.jobs.people[index].transform.position;s.jobs.trip.Clear();var returning=occupancy.Capture();Check(returning.alternativeReturning&&Vector3.Distance(before,s.jobs.people[index].transform.position)<.01f,"cancel starts return without teleport");
   occupancy.Restore(returning);s.jobs.RefreshWaitingPassengers();Check(s.jobs.people[index].activeSelf,"waiting refresh keeps returning pedestrian visible");advance.Invoke(occupancy,new object[]{5f});Check(Mathf.Abs(occupancy.Capture().walkedMetres-6)<.01f,"return continues after restore");advance.Invoke(occupancy,new object[]{5f});Check(!occupancy.Capture().alternativeAccepted&&Vector3.Distance(s.jobs.people[index].transform.position,catalog.Stops[index].Passenger)<.01f,"return reaches original anchor");
   s.jobs.current=new JobOffer{pickup=0,drop=index,fare=5000};s.jobs.trip.Accept(5000,300,90);s.jobs.trip.Board();s.law.tripId="drop-trip";occupancy.Restore(new StopOccupancySnapshot{active=true,eventId="drop",stopId="EG-GAME-02",remaining=10,compensated=true});
   var oldPerson=s.jobs.people[index].transform.position;Check(occupancy.Resolve(true),"clear drop alternative accepted");Check(!occupancy.Capture().alternativePickup&&Vector3.Distance(oldPerson,s.jobs.people[index].transform.position)<.01f,"drop keeps passenger in taxi without moving roadside person");
   Debug.Log("STOP_OCCUPANCY_ALTERNATIVE_INTEGRATION_OK");
  }finally{
   var camera=fixture?(Camera)typeof(DriveSession).GetField("loadingCamera",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(fixture):null;if(camera)UnityEngine.Object.DestroyImmediate(camera.gameObject);
   UnityEngine.Object.DestroyImmediate(root);Time.timeScale=priorTimeScale;Application.targetFrameRate=priorFrameRate;QualitySettings.vSyncCount=priorVSync;Application.runInBackground=priorBackground;MapCatalog.LockedRegion=priorRegion;
  }
 }
}
