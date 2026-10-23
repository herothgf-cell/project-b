using System;
[Serializable] public sealed class RightPracticeSnapshot {public int variant;public bool started;public float elapsed;}

// Training timing only; these durations are not legal stopping requirements.
public class RightPracticeState {
 public readonly int Variant;
 public bool Started {get;private set;}
 public float Elapsed {get;private set;}
 public RightPracticeState(int variant){Variant=Math.Max(1,Math.Min(6,variant));}
 public RightPracticeSnapshot Snapshot(){return new RightPracticeSnapshot{variant=Variant,started=Started,elapsed=Elapsed};}
 public static RightPracticeState Restore(RightPracticeSnapshot s){if(s==null||s.variant<1||s.variant>6||float.IsNaN(s.elapsed)||float.IsInfinity(s.elapsed)||s.elapsed<0||!s.started&&s.elapsed>0)throw new ArgumentException("Invalid pedestrian timer");return new RightPracticeState(s.variant){Started=s.started,Elapsed=s.elapsed};}
 public void Advance(float seconds,bool stoppedAtDecisionPoint){
  if(float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<0)return;
  if(stoppedAtDecisionPoint)Started=true;
  if(Started)Elapsed+=seconds;
 }
 public RoadObservation Observe(){
  var o=new RoadObservation{known=true,trafficClear=true,forward=Variant==1?SignalColor.Red:SignalColor.Green,pedestrianGreen=Variant!=4,dedicated=Variant==3||Variant==4,right=Variant==3&&Elapsed<4?SignalColor.Red:SignalColor.Green};
  if(Variant>=5&&Elapsed<6)o.pedestrian=Variant==5&&Elapsed<1?PedestrianState.Intending:PedestrianState.Crossing;
  return o;
 }
 public float CrossingProgress {get{return Variant==6?Math.Min(1,.35f+Elapsed/6*.65f):Math.Max(0,Math.Min(1,(Elapsed-1)/5));}}
 public static string Title(int v){return new[]{"","R1 · 적색 신호에서 우회전","R2 · 녹색 신호 · 보행자 없음","R3 · 우회전 전용 적색","R4 · 우회전 녹색 화살표","R5 · 건너려는 보행자","R5 · 횡단 중인 보행자"}[Math.Max(1,Math.Min(6,v))];}
 public static string Conditions(int v){return v==1?"차량 적색 / 전용신호 없음 / 보행 녹색 · 사람 없음":v==2?"차량 녹색 / 전용신호 없음 / 보행 녹색 · 사람 없음":v==3?"차량 녹색 / 우회전 전용 적색 / 보행 녹색 · 사람 없음":v==4?"차량 녹색 / 우회전 녹색 화살표 / 보행 적색 · 사람 없음":v==5?"차량 녹색 / 전용신호 없음 / 보행 녹색 · 횡단 의도 있음":"차량 녹색 / 전용신호 없음 / 보행 녹색 · 횡단 중";}
 public static string Guidance(int v){return v==1?"정지선 앞에서 완전히 정지 → 사람·주변 교통 확인 → 서행 우회전. 이 연습은 적색을 유지합니다.":v==2?"보행 신호 색만으로 판단하지 말고 사람과 주변 교통을 확인하며 서행 우회전하세요.":v==3?"정지선 앞에서 정지하고 전용 녹색 화살표가 켜질 때까지 대기하세요. 정차 확인 뒤 연습 신호가 바뀝니다.":v==4?"우회전 녹색 화살표를 확인하고 주변을 살피며 서행 우회전하세요.":"정지선 앞에서 정차하고 보행자가 건너기를 기다리세요. 보행자가 횡단을 마친 뒤 주변을 확인하고 진행하세요.";}
}
