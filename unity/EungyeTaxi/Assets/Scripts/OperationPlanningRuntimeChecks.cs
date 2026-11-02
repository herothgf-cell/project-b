using System;
using System.Collections.Generic;
using UnityEngine;

// Explicit position-control fixture, never a physical-driving acceptance result.
// Inactive session roots prevent gameplay startup and all save IO; colliders are
// real native physics objects so admission also proves physical occupancy checks.
public static class OperationPlanningRuntimeChecks {
 static int checks;
 static void Check(bool value,string label){checks++;if(!value)throw new Exception("OPERATION_PLANNING_RUNTIME "+label);}
 static DriveSession Fixture(GameObject root,MapData map,HdRoadGraph graph,PvpSimulationMatch match,int index){
  var owner=new GameObject("operation planning inactive fixture "+index);owner.SetActive(false);owner.transform.SetParent(root.transform);
  var s=owner.AddComponent<DriveSession>();s.IsSimulationBot=true;s.simulationMatch=match;
  s.world=owner.AddComponent<WorldBuilder>();s.world.map=map;
  var taxi=new GameObject("operation planning physical taxi "+index);taxi.transform.SetParent(root.transform);taxi.layer=9;
  s.taxi=taxi.AddComponent<TaxiController>();s.taxi.body=taxi.AddComponent<Rigidbody>();s.taxi.body.isKinematic=true;
  var box=taxi.AddComponent<BoxCollider>();box.center=Vector3.up*.8f;box.size=new Vector3(1.8f,1.5f,4);
  s.jobs=owner.AddComponent<PassengerJobs>();s.jobs.session=s;s.jobs.stops=map.stops;s.jobs.nav=owner.AddComponent<RoadNavigation>();s.jobs.nav.hd=graph;
  // A real directed query requires an actual lane pose. World origin is outside
  // this authored city and correctly has no graph entry; do not weaken routing.
  var start=map.stops[index%map.stops.Length];var lane=graph.paths[start.laneEdge];
  taxi.transform.SetPositionAndRotation(lane.Position(start.laneAt),Quaternion.LookRotation(lane.Tangent(start.laneAt)));
  s.business=owner.AddComponent<BusinessProgress>();s.business.session=s;s.business.settings=PrototypeSettings.ForOneDay();s.business.ledger=new EconomyLedger(s.business.settings);
  s.services=owner.AddComponent<ServiceStops>();s.services.session=s;
  return s;
 }
 public static void Run(){
  checks=0;float scale=Time.timeScale;var root=new GameObject("Operation planning isolated checks");
  try{
   var map=PvpCityMap.Create();var graph=new HdRoadGraph(map);var matchOwner=new GameObject("inactive shared match fixture");matchOwner.SetActive(false);matchOwner.transform.SetParent(root.transform);var match=matchOwner.AddComponent<PvpSimulationMatch>();
   var drivers=new List<DriveSession>();for(int i=0;i<4;i++)drivers.Add(Fixture(root,map,graph,match,i));
   float initialAt;Check(graph.Nearest(Vector3.zero,Vector3.forward,out initialAt)<0,"off-network city origin remains unavailable");int initialEdge=graph.Nearest(drivers[0].taxi.transform.position,drivers[0].taxi.transform.forward,out initialAt);Check(initialEdge>=0,"fixture begins on a real directed city lane");Debug.Log("OPERATION_FACILITY_INPUT position="+drivers[0].taxi.transform.position+" heading="+drivers[0].taxi.transform.forward+" nearestEdge="+initialEdge);
   var options=drivers[0].services.FacilityOptions(0);foreach(var option in options)Debug.Log("OPERATION_FACILITY_ROUTE "+option.Name+" approachEdge="+option.Approach.laneEdge+" approachAt="+option.Approach.laneAt+" travel="+option.TravelSeconds+" reachable="+option.Reachable);Check(options.Count>=2,"authored reachable fuel alternatives");FacilityOption site=null;foreach(var o in options)if(o.Reachable){site=o;break;}Check(site!=null,"connected authored facility");Check(site.Entry.Length>=2&&site.Departure.Length>=2,"separate actual entry and departure");
   for(int i=0;i<4;i++){drivers[i].taxi.transform.position=site.Approach.V()-site.Approach.Direction*(15+i*8);drivers[i].taxi.transform.forward=site.Approach.Direction;}Physics.SyncTransforms();
   string money=JsonUtility.ToJson(drivers[0].business.ledger.Account.Snapshot());double elapsed=drivers[0].business.ledger.Elapsed;
   Check(drivers[0].services.CanEnterFacility(site),"first taxi admitted before entry");
   for(int i=1;i<4;i++)Check(!drivers[i].services.CanEnterFacility(site),"shared FIFO waits taxi "+i);
   Check(drivers[3].services.QueueStatus.Contains("4번째"),"fourth taxi sees order");
   Check(money==JsonUtility.ToJson(drivers[0].business.ledger.Account.Snapshot())&&elapsed==drivers[0].business.ledger.Elapsed,"admission and queue create no charge/time/condition effects");
   drivers[0].taxi.transform.position=site.Bay;Physics.SyncTransforms();drivers[0].services.TickFacilityQueue();drivers[0].services.CancelQueue();
   Check(!drivers[1].services.CanEnterFacility(site),"logical release cannot bypass real taxi occupying bay");
   drivers[0].taxi.transform.position=site.Bay+site.Direction*25;Physics.SyncTransforms();drivers[1].services.TickFacilityQueue();Check(!drivers[1].services.QueueWaiting,"real Tick admits entrance ticket after physical departure without another entry request");
   UnityEngine.Object.DestroyImmediate(drivers[2].services);drivers[3].services.TickFacilityQueue();Check(drivers[3].services.QueueStatus.Contains("2번째"),"destroy/disconnect releases ticket");
   drivers[1].services.CancelQueue();Check(drivers[3].services.CanEnterFacility(site),"cancel releases next");drivers[3].services.CancelQueue();
   Check(drivers[0].services.CanEnterFacility(site),"transient restore reconstructs physical ticket");drivers[0].services.CancelQueue();
   var s=drivers[0];int pickup=0,drop=1;while(drop<map.stops.Length&&graph.Route(map.stops[pickup].laneEdge,map.stops[pickup].laneAt,map.stops[drop].laneEdge,map.stops[drop].laneAt)==null)drop++;Check(drop<map.stops.Length,"connected passenger fixture");
   var pickupLane=graph.paths[map.stops[pickup].laneEdge];float pickupAt=Mathf.Max(0,map.stops[pickup].laneAt-20);s.taxi.transform.SetPositionAndRotation(pickupLane.Position(pickupAt),Quaternion.LookRotation(pickupLane.Tangent(pickupAt)));
   var ride=graph.Route(map.stops[pickup].laneEdge,map.stops[pickup].laneAt,map.stops[drop].laneEdge,map.stops[drop].laneAt);var offer=new JobOffer{pickup=pickup,drop=drop,timingPolicyVersion=1,safeRouteSeconds=OperationForecastAdapter.RouteSeconds(s,ride),distance=ride.path.length,seconds=300,fare=5000};
   string before=JsonUtility.ToJson(s.business.ledger.Snapshot())+JsonUtility.ToJson(s.business.ledger.Account.Snapshot());
   var first=OperationForecastAdapter.ForSelection(s,offer);Check(first.Available,"selection pickup plus ride forecast available");Check(ReferenceEquals(first,OperationForecastAdapter.ForSelection(s,offer)),"selection cache returns same immutable view within one second");
   Check(before==JsonUtility.ToJson(s.business.ledger.Snapshot())+JsonUtility.ToJson(s.business.ledger.Account.Snapshot()),"selection forecast mutates neither account nor condition/ledger");
   s.jobs.current=offer;s.jobs.trip.Accept(offer.fare,offer.seconds,100);s.jobs.trip.Board();s.jobs.nav.active=true;s.jobs.nav.hdRoute=ride;s.jobs.nav.estimatedSeconds=15;s.jobs.nav.remaining=100;
   var active=OperationForecastAdapter.ForActiveTrip(s);Check(active.Available&&Math.Abs(active.TravelSeconds-15)<.001,"onboard forecast uses remaining ETA only");
   s.jobs.nav.hdRoute=null;s.jobs.nav.path.Clear();Check(!OperationForecastAdapter.ForActiveTrip(s).Available,"route disappearance invalidates cache as unavailable");
   Debug.Log("OPERATION_PLANNING_RUNTIME_CHECKS_OK checks="+checks+" fixture=position-control noSaveIO=true physicalDriving=false");
  }finally{UnityEngine.Object.DestroyImmediate(root);Time.timeScale=scale;}
 }
}
