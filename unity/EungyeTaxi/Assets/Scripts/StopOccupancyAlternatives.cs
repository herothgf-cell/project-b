using System;
using UnityEngine;

public sealed partial class StopOccupancySystem {
 AlternativePassengerStop Pair {get{var s=policy.Capture();return session.jobs.Catalog.AlternativeFor(s.stopId);}}
 int ActiveIndex {get{var j=session.jobs;if(!j.Active||j.current==null||j.PassengerViaPending)return -1;return j.trip.State==TripState.ToPickup?j.current.pickup:j.current.drop;}}
 bool AlternativeMatchesActive(){var s=policy.Capture();int i=ActiveIndex;return s.alternativeAccepted&&!s.alternativeReturning&&i>=0&&session.jobs.Catalog.Entry(i)!=null&&session.jobs.Catalog.Entry(i).StopId==s.stopId&&s.alternativeTrip==session.law.tripId&&s.alternativePickup==(session.jobs.trip.State==TripState.ToPickup);}
 public bool TryGetAlternative(string stopId,out MapStop alternative){alternative=null;if(!AlternativeMatchesActive()||policy.Capture().stopId!=stopId)return false;var pair=Pair;if(pair==null)return false;alternative=pair.Stop;return true;}
 public bool AlternativeReadyFor(int index){var entry=session.jobs.Catalog.Entry(index);MapStop ignored;return entry==null||!TryGetAlternative(entry.StopId,out ignored)||!policy.Capture().alternativePickup||policy.AlternativeReady;}
 public bool IsWalkingPassenger(int index){var s=policy.Capture();var entry=session.jobs.Catalog.Entry(index);return s.alternativeAccepted&&s.alternativePickup&&entry!=null&&entry.StopId==s.stopId;}
 public bool AlternativeBayClearFor(int index){var entry=session.jobs.Catalog.Entry(index);MapStop target;if(entry==null||!TryGetAlternative(entry.StopId,out target))return true;foreach(var c in session.traffic.cars)if(c&&c.Visible&&Vector3.Distance(target.Kerb,c.transform.position)<6)return false;foreach(var hit in Physics.OverlapBox(target.Kerb+Vector3.up*.9f,new Vector3(1.1f,.7f,2.7f),Quaternion.LookRotation(target.Direction),~0,QueryTriggerInteraction.Ignore))if(hit&&!hit.transform.IsChildOf(session.taxi.transform)&&hit.bounds.max.y>target.Kerb.y+.5f)return false;return true;}
 public bool CanOfferAlternative {get{int index=ActiveIndex;return index>=0&&!policy.AlternativeActive&&IsOccupied(index)&&session.jobs.Catalog.AlternativeFor(session.jobs.Catalog.Entry(index).StopId)!=null;}}
 bool AcceptAlternative(){
  if(!CanOfferAlternative)return false;var j=session.jobs;if(j.nav==null||j.nav.hd==null)return false;int index=ActiveIndex;var pair=j.Catalog.AlternativeFor(j.Catalog.Entry(index).StopId);
  float distance=PassengerJobs.DirectedDistance(j.nav.hd,session.taxi.transform.position,session.taxi.transform.forward,pair.Stop);
  if(!policy.AgreeAlternative(session.law.tripId,pair.OriginalStopId,pair.Id,pair.WalkingMetres,!float.IsInfinity(distance)&&!float.IsNaN(distance),PathClear(pair),BayClear(pair)))return false;
  policy.SetAlternativePickup(j.trip.State==TripState.ToPickup);Protect();j.RefreshActiveGoal();PlaceWalker(pair);j.message=Status;session.business.SaveCheckpoint();return true;
 }
 bool BayClear(AlternativePassengerStop pair){
  var p=pair.Stop.Kerb;if(Vector3.Distance(p,session.taxi.transform.position)<5)return false;
  foreach(var c in session.traffic.cars)if(c&&c.Visible&&Vector3.Distance(p,c.transform.position)<7)return false;
  foreach(var collider in Physics.OverlapBox(p+Vector3.up*.9f,new Vector3(1.1f,.7f,2.7f),Quaternion.LookRotation(pair.Stop.Direction),~0,QueryTriggerInteraction.Ignore))if(collider&&collider.bounds.max.y>p.y+.5f)return false;
  return true;
 }
 bool PathClear(AlternativePassengerStop pair){
  int index=session.jobs.Catalog.IndexOf(pair.OriginalStopId);var person=index>=0&&session.jobs.people!=null&&index<session.jobs.people.Length?session.jobs.people[index]:null;
  for(int i=1;i<pair.WalkingPath.Length;i++){
   var a=pair.WalkingPath[i-1]+Vector3.up*.8f;var b=pair.WalkingPath[i]+Vector3.up*.8f;
   foreach(var hit in Physics.OverlapCapsule(a,b,.65f,~0,QueryTriggerInteraction.Ignore))if(hit&&(!person||!hit.transform.IsChildOf(person.transform))&&hit.bounds.max.y>a.y-.2f)return false;
  }return true;
 }
 Vector3 WalkingPosition(AlternativePassengerStop pair,float metres){for(int i=1;i<pair.WalkingPath.Length;i++){var a=pair.WalkingPath[i-1];var b=pair.WalkingPath[i];float length=Vector3.Distance(a,b);if(metres<=length)return Vector3.Lerp(a,b,length<.001f?0:metres/length);metres-=length;}return pair.WalkingPath[pair.WalkingPath.Length-1];}
 void PlaceWalker(AlternativePassengerStop pair){var s=policy.Capture();if(!s.alternativePickup)return;int index=session.jobs.Catalog.IndexOf(s.stopId);if(index<0||session.jobs.people==null||index>=session.jobs.people.Length||!session.jobs.people[index])return;var person=session.jobs.people[index];person.transform.position=WalkingPosition(pair,s.walkedMetres);person.SetActive(true);}
 void ReconcileAlternativeTrip(){
  var s=policy.Capture();if(!s.alternativeAccepted||s.alternativeReturning||AlternativeMatchesActive())return;
  var j=session.jobs;int index=j.Catalog.IndexOf(s.stopId);
  if(s.alternativePickup&&j.trip.State==TripState.OnTrip&&j.current!=null&&j.current.pickup==index){if(j.people!=null&&index>=0&&index<j.people.Length&&j.people[index]){j.people[index].SetActive(false);j.people[index].transform.position=j.stops[index].Passenger;}policy.ClearAlternative();}
  else if(s.alternativePickup)policy.BeginReturn();else policy.ClearAlternative();
 }
 public void NotifyGoalChanged(){ReconcileAlternativeTrip();}
 void AdvanceAlternativeRuntime(float delta){
  ReconcileAlternativeTrip();var s=policy.Capture();if(!s.alternativeAccepted)return;var pair=Pair;if(pair==null)return;
  if(s.alternativeReturning){policy.AdvanceReturn(delta,PathClear(pair));if(!policy.AlternativeActive){int index=session.jobs.Catalog.IndexOf(s.stopId);if(index>=0&&session.jobs.people!=null&&index<session.jobs.people.Length&&session.jobs.people[index])session.jobs.people[index].transform.position=session.jobs.stops[index].Passenger;session.jobs.RefreshWaitingPassengers();session.business.SaveCheckpoint();return;}}
  else if(s.alternativePickup)policy.AdvanceAlternative(delta,PathClear(pair));
  PlaceWalker(pair);
 }
 void RestoreAlternative(){
  var s=policy.Capture();if(!s.alternativeAccepted)return;var pair=Pair;
  if(pair==null||pair.Id!=s.alternativeId||Mathf.Abs(pair.WalkingMetres-s.walkingMetres)>.01f||s.alternativeReturning&&!s.alternativePickup||!s.alternativeReturning&&!AlternativeMatchesActive()||!s.alternativePickup&&Mathf.Abs(s.walkedMetres-s.walkingMetres)>.01f)throw new ArgumentException("Saved alternative trip or geometry unavailable");
  PlaceWalker(pair);if(!s.alternativeReturning)session.jobs.RefreshActiveGoal();
 }
}
