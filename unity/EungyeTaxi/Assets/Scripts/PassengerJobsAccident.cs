using UnityEngine;
public partial class PassengerJobs {
 public string ResolveAccident(){
  if(!Active)return "공차 · 운임 손실 없음";
  bool occupied=trip.State==TripState.OnTrip;
  if(occupied&&trip.ArrivalLocked&&RequestReward!=null){
   if(Behavior!=null){Behavior.PreparationRemaining=0;Behavior.UnloadRemaining=0;}
   if(DrunkRide!=null)DrunkRide.WaitElapsed=Mathf.Max(30,DrunkRide.WaitElapsed);
   int paid=RequestReward.Total;CompleteDelivery(true,true);return "도착 확정 운임 "+paid.ToString("N0")+"원 보존 · 안전 하차";
  }
  if(occupied){
   if(Behavior!=null){Behavior.ExitCancelled();ApplyBehaviorDirt();}
   if(session.requests)session.requests.CancelForFatigue();
   session.business.ledger.Care.Finish(session.law.tripId,DrunkRide!=null&&DrunkRide.Vomited);
   failed++;session.business.RecordPassenger(0,"보행자 사고 · 안전 하차/운송 취소");session.business.RecordFailure();
  }
  if(current!=null)cooldown[current.pickup]=Mathf.Max(cooldown[current.pickup],Time.time+60);
  trip.Clear();DrunkRide=null;Behavior=null;RequestReward=null;Finish();if(session.law)session.law.tripId="";
  message=occupied?"보행자 사고 · 운송 중단 · 실수령 0원":"보행자 사고 · 픽업 취소 · 운임 손실 없음";CareStatus=message;return message;
 }
 public void SuspendAccidentPickup(int index){
  if(index<0||index>=stops.Length)return;
  cooldown[index]=Mathf.Max(cooldown[index],Time.time+60);if(roadsideWaiting!=null&&index<roadsideWaiting.Length)roadsideWaiting[index]=false;
  if(current!=null&&current.pickup==index&&trip.State==TripState.ToPickup){trip.Clear();Finish();if(session.law)session.law.tripId="";message="승객 사고 처리로 호출 취소 · 기사 비용/감점 없음";}
  for(int i=0;i<offers.Length;i++)if(offers[i]!=null&&offers[i].pickup==index)offers[i]=null;
  if(people[index])people[index].SetActive(false);
 }
}
