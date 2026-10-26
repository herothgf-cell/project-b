using System;

[Serializable] public sealed class OperationMetricsSnapshot {
 public bool captured;public double startedAt,emptyMetres,occupiedMetres,emptyMovingSeconds,emptyWaitingSeconds,serviceSeconds;
 public OperationMetricsSnapshot Copy(){return (OperationMetricsSnapshot)MemberwiseClone();}
}
// Observational only: never advances clocks, consumes fuel or creates transactions.
public sealed class OperationMetrics {
 OperationMetricsSnapshot state=new OperationMetricsSnapshot{captured=true};
 public void Observe(double metres,double seconds,bool occupied,bool service){
  if(!Finite(metres)||!Finite(seconds)||metres<0||seconds<0)return;
  if(service){state.serviceSeconds+=seconds;return;}
  // A discontinuous position sample must not create earnings/distance statistics.
  if(metres>Math.Max(5,seconds*70))return;
  if(occupied)state.occupiedMetres+=metres;
  else{state.emptyMetres+=metres;if(metres>seconds*.1)state.emptyMovingSeconds+=seconds;else state.emptyWaitingSeconds+=seconds;}
 }
 public OperationMetricsSnapshot Capture(){return state.Copy();}
 static bool Finite(double v){return !double.IsNaN(v)&&!double.IsInfinity(v);}
 public static OperationMetrics Restore(OperationMetricsSnapshot s,double elapsed=0){
  var result=new OperationMetrics();if(s==null||!s.captured){result.state.startedAt=Math.Max(0,elapsed);return result;}
  foreach(double v in new[]{s.startedAt,s.emptyMetres,s.occupiedMetres,s.emptyMovingSeconds,s.emptyWaitingSeconds,s.serviceSeconds})if(!Finite(v)||v<0)throw new ArgumentException("Invalid operation metrics");
  result.state=s.Copy();return result;
 }
 public static string Describe(OperationMetricsSnapshot s){if(s==null||!s.captured)return "공차 기록 없음 · 구형 저장";return "공차 "+(s.emptyMetres/1000).ToString("0.00")+"km / 탑승 "+(s.occupiedMetres/1000).ToString("0.00")+"km\n미탑승 이동 "+(s.emptyMovingSeconds/60).ToString("0.0")+"분 · 대기 "+(s.emptyWaitingSeconds/60).ToString("0.0")+"분 · 서비스 "+(s.serviceSeconds/60).ToString("0.0")+"분"+(s.startedAt>0?" (경과 "+(s.startedAt/60).ToString("0.0")+"분부터 집계)":"");}
}
