using UnityEngine;

public struct StopArrivalHint {
 public bool Visible,Near,Ready,Pickup;
 public float Distance;
 public MapStop Stop;
 public string Action,Detail;
}

// Read-only: uses the same geometric contract as F and never completes a trip.
public static class StopArrivalGuidance {
 public static StopArrivalHint Evaluate(MapStop stop,Vector3 position,Vector3 heading,float speed,bool pickup){
  var h=new StopArrivalHint{Stop=stop,Pickup=pickup,Action=pickup?"승차 위치로 이동":"하차 위치로 이동",Detail=""};if(stop==null)return h;
  var delta=position-stop.Kerb;delta.y=0;h.Distance=delta.magnitude;h.Visible=true;h.Near=h.Distance<55;
  h.Ready=stop.CanStop(position,heading,Mathf.Abs(speed));
  if(h.Ready){h.Action=pickup?"F · 승객 승차":"F · 승객 하차";h.Detail="정차 완료 · F를 눌러 진행하세요";}
  else if(h.Distance>=18)h.Detail="표시된 "+(pickup?"파란":"분홍")+" 정차 칸까지 "+Mathf.CeilToInt(h.Distance)+"m";
  else if(Mathf.Abs(speed)>=.56f)h.Detail="감속 후 완전히 정차하세요";
  else if(Vector3.Dot(heading,stop.Direction)<=.85f)h.Detail="바닥 화살표와 같은 방향으로 맞추세요";
  else h.Detail="표시된 정차 칸 안쪽으로 이동하세요";
  return h;
 }
 public static StopArrivalHint For(DriveSession session){
  if(!session||!session.jobs||!session.taxi)return new StopArrivalHint();var j=session.jobs;
  if(!j.Active||j.current==null)return new StopArrivalHint();
  bool pickup=j.trip.State==TripState.ToPickup;int index=pickup?j.current.pickup:j.PassengerViaPending?j.PassengerViaStop:j.current.drop;
  float speed=PvpNativeLan.IsReplica(session)?Mathf.Abs(session.taxi.Speed):session.taxi.ObservedVelocity.magnitude;
  int opposite=j.OppositeDropIndex;
  if(opposite>=0&&j.At(opposite))index=opposite;
  var hint=Evaluate(j.EffectiveStop(index),session.taxi.transform.position,session.taxi.transform.forward,speed,pickup);
  if(hint.Ready&&!j.At(index)){hint.Ready=false;hint.Action="정차 공간 확인";hint.Detail="공간 점유·승객 보행 대기 · 정차 유지";}
  if(!hint.Near)return hint;
  if(j.SituationWorkActive){hint.Ready=false;hint.Action="승객 준비 중";hint.Detail="작업이 끝날 때까지 안전 정차 유지";}
  else if(pickup&&j.Behavior!=null&&!j.Behavior.BoardingReady){hint.Ready=false;hint.Action="승차 준비 중";hint.Detail=j.Behavior.WorkRemaining>0?"적재·준비 "+Mathf.CeilToInt(j.Behavior.WorkRemaining)+"초 · 정차 유지":"승객 대화에서 준비 방법을 선택하세요";}
  else if(!pickup&&j.Errand!=null&&j.Errand.Waiting){hint.Ready=false;hint.Action="승객 용무 대기";hint.Detail=Mathf.CeilToInt(j.Errand.Remaining)+"초 · 약속한 위치에 정차 유지";}
  else if(!pickup&&j.PassengerViaPending){hint.Action=hint.Ready?"F · 경유 용무 진행":"경유 정차 위치";hint.Detail=hint.Ready?"합의한 용무 장소 · F로 진행":hint.Detail;}
  else if(!pickup&&j.DrunkRide!=null&&j.DrunkRide.Waiting){hint.Ready=hint.Ready&&j.DrunkRide.WaitElapsed>=15;hint.Action=hint.Ready?"F · 손님 다시 깨우기":"손님 기상 대기";hint.Detail=j.DrunkRide.WaitElapsed<15?Mathf.CeilToInt(j.DrunkRide.WaitUntilWake)+"초 후 다시 깨우기 · 정차 유지":"F로 깨우기 · 안전 정차 시 자동 하차 대기";}
  else if(!pickup&&j.Behavior!=null&&j.trip.ArrivalLocked&&!j.Behavior.UnloadReady){hint.Ready=false;hint.Action="하차 준비 중";hint.Detail="짐·승객 준비가 끝날 때까지 정차 유지";}
  return hint;
 }
}
