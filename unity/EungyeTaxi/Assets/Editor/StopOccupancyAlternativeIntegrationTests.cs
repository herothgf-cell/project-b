using System;
using System.Reflection;
using UnityEngine;
public static class StopOccupancyAlternativeIntegrationTests {
 static void Check(bool value,string message){if(!value)throw new Exception("OCCUPANCY_ALTERNATIVE: "+message);}
 public static void Run(){
  var root=new GameObject("alternative integration");
  try{
   var map=ProjectMapStore.Load(ProjectMapBuild.Root).Compose();FuelStationLayout.Apply(map);var catalog=PassengerStopCatalog.For(map);
   int index=catalog.IndexOf("EG-GAME-02");var alternative=catalog.AlternativeFor("EG-GAME-02");Check(index>=0&&alternative!=null,"catalog pair available");
   var s=root.AddComponent<DriveSession>();s.business=root.AddComponent<BusinessProgress>();s.business.session=s;s.business.ledger=new EconomyLedger(PrototypeSettings.ForThreeDays());typeof(BusinessProgress).GetProperty("SimulationSession").GetSetMethod(true).Invoke(s.business,new object[]{true});s.law=root.AddComponent<DrivingLaw>();s.law.tripId="trip-test";
   s.taxi=root.AddComponent<TaxiController>();s.taxi.body=root.AddComponent<Rigidbody>();s.traffic=root.AddComponent<TrafficSystem>();
   s.jobs=root.AddComponent<PassengerJobs>();s.jobs.session=s;typeof(PassengerJobs).GetProperty("Catalog").GetSetMethod(true).Invoke(s.jobs,new object[]{catalog});s.jobs.stops=catalog.Stops;s.jobs.people=new GameObject[catalog.Stops.Length];s.jobs.cooldown=new float[catalog.Stops.Length];var ids=(System.Collections.Generic.List<string>)typeof(PassengerJobs).GetField("runtimeStopIds",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(s.jobs);foreach(var e in catalog.Entries)ids.Add(e.StopId);
   s.jobs.people[index]=new GameObject("pickup person");s.jobs.people[index].transform.SetParent(root.transform);s.jobs.people[index].transform.position=catalog.Stops[index].Passenger;
   s.jobs.nav=root.AddComponent<RoadNavigation>();s.jobs.nav.Build(map,root.transform);var marker=new GameObject("marker");marker.transform.SetParent(root.transform);typeof(PassengerJobs).GetField("marker",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(s.jobs,marker);
   s.jobs.current=new JobOffer{pickup=index,drop=0,fare=5000};s.jobs.trip.Accept(5000,300,90);
   var occupancy=root.AddComponent<StopOccupancySystem>();occupancy.session=s;
   occupancy.Restore(new StopOccupancySnapshot{version=2,active=true,eventId="event",stopId="EG-GAME-02",remaining=10,alternativeAccepted=true,alternativePickup=true,alternativeId=alternative.Id,alternativeTrip="trip-test",walkingMetres=alternative.WalkingMetres,walkedMetres=12});
   MapStop effective;Check(occupancy.TryGetAlternative("EG-GAME-02",out effective)&&ReferenceEquals(effective,alternative.Stop),"restored agreement selects the physical bay");
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
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
}
