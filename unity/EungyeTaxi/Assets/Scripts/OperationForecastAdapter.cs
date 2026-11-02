using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

// Queries only: the live account, condition, route and accepted offer are never mutated.
public static class OperationForecastAdapter {
 sealed class Entry {public float until;public object route;public string key;public OperationForecast value;}
 sealed class Cache {public readonly Dictionary<JobOffer,Entry> offers=new Dictionary<JobOffer,Entry>();public Entry active;}
 static readonly ConditionalWeakTable<DriveSession,Cache> caches=new ConditionalWeakTable<DriveSession,Cache>();
 static OperationForecast Missing(){return OperationForecast.Calculate(null,null,double.NaN,0,false,0,false,0);}
 static OperationForecast Make(DriveSession s,double seconds,double metres){var b=s.business.ledger;return OperationForecast.Calculate(b.Account,b.Driver,seconds,metres,b.OneDay,b.SecondsLeft,s.simulationMatch,s.simulationMatch?s.simulationMatch.Remaining:0);}
 static bool Valid(float x){return !float.IsNaN(x)&&!float.IsInfinity(x)&&x>=0;}
 static bool ValidOffer(DriveSession s,JobOffer o){return s&&s.business&&s.business.ledger!=null&&s.jobs&&o!=null&&o.pickup>=0&&o.drop>=0&&o.pickup<s.jobs.stops.Length&&o.drop<s.jobs.stops.Length;}
 public static OperationForecast ForSelection(DriveSession s,JobOffer offer){
  if(!ValidOffer(s,offer))return Missing();var c=caches.GetOrCreateValue(s);Entry e;
  var pickupStop=ReferenceEquals(s.jobs.current,offer)&&s.jobs.trip.State==TripState.ToPickup?s.jobs.EffectiveStop(offer.pickup):s.jobs.stops[offer.pickup];
  string key=offer.pickup+":"+offer.drop+":"+pickupStop.laneEdge+":"+pickupStop.laneAt+":"+pickupStop.x+":"+pickupStop.z+":"+offer.safeRouteSeconds+":"+offer.waitSeconds+":"+s.jobs.nav.PreferFastest+":"+s.jobs.nav.AvoidEdge;
  if(c.offers.TryGetValue(offer,out e)&&Time.unscaledTime<e.until&&e.key==key&&ReferenceEquals(e.route,s.jobs.nav.hdRoute))return e.value;
  if(c.offers.Count>16)c.offers.Clear();float pickup=PassengerJobs.DirectedDistance(s.jobs.nav.hd,s.taxi.transform.position,s.taxi.transform.forward,pickupStop);
  var ride=s.jobs.nav.hd==null?null:s.jobs.nav.hd.Route(pickupStop.laneEdge,pickupStop.laneAt,s.jobs.stops[offer.drop].laneEdge,s.jobs.stops[offer.drop].laneAt);
  float rideSeconds=ReferenceEquals(pickupStop,s.jobs.stops[offer.pickup])?offer.safeRouteSeconds:RouteSeconds(s,ride);
  var value=!Valid(pickup)||ride==null||offer.timingPolicyVersion<1||!Valid(rideSeconds)||rideSeconds<=0||!Valid(offer.waitSeconds)?Missing():Make(s,pickup/4+rideSeconds+offer.waitSeconds,pickup+ride.path.length);
  c.offers[offer]=new Entry{until=Time.unscaledTime+1,key=key,route=s.jobs.nav.hdRoute,value=value};return value;
 }
 public static OperationForecast ForActiveTrip(DriveSession s){
  if(!s||!s.jobs||!s.jobs.Active||!ValidOffer(s,s.jobs.current))return Missing();
  if(s.jobs.trip.State==TripState.ToPickup)return ForSelection(s,s.jobs.current);
  var n=s.jobs.nav;var c=caches.GetOrCreateValue(s);var errand=s.jobs.Errand;string key=s.jobs.OfferId+":"+s.jobs.PassengerViaStop+":"+n.hdGoal+":"+n.PreferFastest+":"+n.AvoidEdge+":"+(errand==null?"none":errand.TripId+":"+errand.Completed);
  if(c.active!=null&&Time.unscaledTime<c.active.until&&c.active.key==key&&ReferenceEquals(c.active.route,n.hdRoute))return c.active.value;
  double seconds=n.estimatedSeconds,metres=n.remaining;bool available=n.active&&(n.hdRoute!=null||n.path.Count>1)&&Valid(n.estimatedSeconds)&&Valid(n.remaining)&&!(n.remaining>1&&n.estimatedSeconds<=0);
  if(errand!=null&&!errand.Completed)seconds+=errand.Remaining;
  if(available&&s.jobs.PassengerViaPending){var via=s.jobs.stops[s.jobs.PassengerViaStop];var drop=s.jobs.stops[s.jobs.current.drop];var onward=n.hd==null?null:n.hd.Route(via.laneEdge,via.laneAt,drop.laneEdge,drop.laneAt);if(onward==null)available=false;else {metres+=onward.path.length;seconds+=RouteSeconds(s,onward);}}
  var value=available?Make(s,seconds,metres):Missing();c.active=new Entry{until=Time.unscaledTime+1,key=key,route=n.hdRoute,value=value};return value;
 }
 public static float RouteSeconds(DriveSession s,HdRoadGraph.RoutePath route){if(route==null||s.jobs.nav.hd==null)return float.PositiveInfinity;float total=0,prior=0;for(int i=0;i<route.edges.Count;i++){int edge=route.edges[i];float length=route.ends[i]-prior;prior=route.ends[i];var lane=s.jobs.nav.hd.edges[edge];float cruise=lane.turn!=0||lane.id.StartsWith("join:")?4.5f:8;total+=RoadMapRules.TravelSeconds(s.world.map,lane.id,s.jobs.nav.hd.paths[edge].length,cruise)*length/Mathf.Max(.01f,s.jobs.nav.hd.paths[edge].length);}return total;}
 public static float DirectedSeconds(DriveSession s,MapStop stop,out float metres){
  metres=float.PositiveInfinity;if(stop==null||s.jobs.nav.hd==null)return float.PositiveInfinity;float at;int edge=s.jobs.nav.hd.Nearest(s.taxi.transform.position,s.taxi.transform.forward,out at);if(edge<0||stop.laneEdge<0||stop.laneEdge>=s.jobs.nav.hd.edges.Length)return float.PositiveInfinity;
  var route=s.jobs.nav.hd.Route(edge,at,stop.laneEdge,stop.laneAt);if(route==null)return float.PositiveInfinity;metres=route.path.length;return RouteSeconds(s,route);
 }
}
