using System;

[Serializable] public sealed class ConfusionPracticeSnapshot {
 public int variant;public bool known,started,entered,finished,correctRoute,stopped,entryBlocked,entryRed,conflictSeen,failedYield,sawBlock,hasBlocked,blocked,hasOncoming,oncoming;public float elapsed;
}
// Local teaching fixtures only. Durations and penalties are game values, not legal thresholds/fines.
public sealed class ConfusionPracticeState {
 public const int First=8,Last=14;
 public readonly int Variant;
 public bool Known=true,Started,Entered,Finished,CorrectRoute,Stopped,EntryBlocked,EntryRed,ConflictSeen,FailedYield;
 public float Elapsed {get;private set;}
 bool sawBlock;bool? observedBlocked,observedOncoming;
 public void ObserveTraffic(bool blocked,bool oncoming){observedBlocked=blocked;observedOncoming=oncoming;}
 public ConfusionPracticeState(int variant){if(!Contains(variant))throw new ArgumentOutOfRangeException("variant");Variant=variant;}
 public ConfusionPracticeSnapshot Snapshot(){return new ConfusionPracticeSnapshot{variant=Variant,known=Known,started=Started,entered=Entered,finished=Finished,correctRoute=CorrectRoute,stopped=Stopped,entryBlocked=EntryBlocked,entryRed=EntryRed,conflictSeen=ConflictSeen,failedYield=FailedYield,sawBlock=sawBlock,hasBlocked=observedBlocked.HasValue,blocked=observedBlocked.GetValueOrDefault(),hasOncoming=observedOncoming.HasValue,oncoming=observedOncoming.GetValueOrDefault(),elapsed=Elapsed};}
 public static ConfusionPracticeState Restore(ConfusionPracticeSnapshot s){if(s==null||!Contains(s.variant)||float.IsNaN(s.elapsed)||float.IsInfinity(s.elapsed)||s.elapsed<0||s.finished&&!s.entered)throw new ArgumentException("Invalid saved road observation");return new ConfusionPracticeState(s.variant){Known=s.known,Started=s.started,Entered=s.entered,Finished=s.finished,CorrectRoute=s.correctRoute,Stopped=s.stopped,EntryBlocked=s.entryBlocked,EntryRed=s.entryRed,ConflictSeen=s.conflictSeen,FailedYield=s.failedYield,Elapsed=s.elapsed,sawBlock=s.sawBlock,observedBlocked=s.hasBlocked?(bool?)s.blocked:null,observedOncoming=s.hasOncoming?(bool?)s.oncoming:null};}
 public static bool Contains(int v){return v>=First&&v<=Last;}
 public bool Left {get{return Variant>=12;}}
 public bool Flashing {get{return Variant<=9;}}
 public SignalColor Signal {get{return Variant==8?SignalColor.Red:Variant==9?SignalColor.Yellow:Variant==12&&Elapsed<4?SignalColor.Red:SignalColor.Green;}}
 public bool Blocked {get{return observedBlocked??(Variant==10&&Elapsed<5);}}
 public bool Oncoming {get{return observedOncoming??(Variant==13&&Elapsed<12);}}
 public void Advance(float seconds,bool atDecision,bool stopped){
  if(float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<0||Finished)return;
  if(atDecision&&stopped){Started=true;if(Blocked)sawBlock=true;}
  if(Started)Elapsed+=seconds;
 }
 public void Enter(bool stopped){if(Entered||Finished)return;Entered=true;Stopped=stopped;EntryBlocked=Blocked;EntryRed=Left&&Signal!=SignalColor.Green;}
 public void Conflict(){if(!Entered||Finished||ConflictSeen)return;ConflictSeen=true;FailedYield=Left&&Oncoming;}
 public void Complete(bool correct){if(!Entered||Finished)return;Finished=true;CorrectRoute=correct&&(!Left||ConflictSeen);}
 public LawEvent Event(string id,int day){
  var e=new LawEvent{id=id,day=day,family="confusion",scenario="C"+Variant,action=Left?"비보호 좌회전":"직진",reason="가상 교육 구간 · "+Title(Variant)+" · ① 정지선 / ② 교차로 통과",observation=new RoadObservation{known=Known,forward=EntryRed?SignalColor.Red:Variant==8?SignalColor.Red:Variant==9?SignalColor.Yellow:SignalColor.Green,trafficClear=!FailedYield,pedestrian=PedestrianState.None}};
  string rule=Variant==8?"F02":Variant==9?"F03":Left?"F05":"F06";
  bool violation=Variant==8?!Stopped:Left?EntryRed||FailedYield:EntryBlocked;
  var verdict=!Known||!Entered?LawVerdict.Deferred:violation?LawVerdict.Violation:!Finished||!CorrectRoute?LawVerdict.Deferred:Variant==9?LawVerdict.Deferred:Variant==11?LawVerdict.NoOpportunity:LawVerdict.Compliant;
  e.Add(rule,verdict,15);return e;
 }
 public PracticeFeedback Feedback(){
  var f=new PracticeFeedback{outcome="연습 미완료 · 아직 구간을 벗어나지 않음",good="아직 완료한 통과 기록이 없습니다.",issues="① 정지선과 ② 통과 경로를 지나야 최종 결과를 표시합니다.",next=Guidance(Variant)};
  if(!Entered)return f;
  string bad="",good="";
  if(Variant==8){if(Stopped)good+="① 적색 점멸 정지선 앞 완전 정지를 확인했습니다.\n";else bad+="① 적색 점멸에서 완전 정지 없이 정지선을 넘었습니다.\n";}
  if(EntryBlocked)bad+="① 출구에 차량 공간이 없는 상태에서 정지선을 넘었습니다. 나중에 공간이 생겨도 진입 기록은 유지합니다.\n";
  if(EntryRed)bad+="① 적색에서 비보호 좌회전 구간에 진입했습니다. 나중에 녹색이 되어도 진입 기록은 유지합니다.\n";
  if(FailedYield)bad+="② 대향 직진차가 남아 있을 때 좌회전 충돌 구역에 들어갔습니다.\n";
  if(!Known){f.outcome="판정 보류 · 관찰 정보 부족";f.issues="신호 또는 차량 상태를 확정하지 못했습니다.";return f;}
  if(!Finished){f.issues=bad!=""?bad.Trim():f.issues;return f;}
  if(!CorrectRoute)bad+="② 안내된 "+(Left?"왼쪽":"맞은편")+" 도로로 통과하지 않았습니다. 경로를 다시 확인하세요.\n";
  if(Variant==10&&sawBlock&&!EntryBlocked)good+="① 출구가 열릴 때까지 정지선 밖에서 기다린 뒤 진입했습니다.\n";
  if(Left&&!EntryRed)good+="① 녹색에서 비보호 좌회전 구간에 진입했습니다.\n";
  if(Variant==13&&!FailedYield&&ConflictSeen)good+="② 대향차가 지나간 뒤 좌회전 충돌 구역을 통과했습니다.\n";
  if(CorrectRoute)good+="② 안내한 도로까지 운행을 마쳤습니다.\n";
  f.outcome=!CorrectRoute?"경로 보완 필요 · 안내 도로 미통과":bad!=""?"보완 필요 · 판단 위치를 확인하세요":Variant==9?"통과 완료 · 주의 확인은 판정 보류":"연습 완료 · 관찰 조건에서 위반 없음";
  f.good=good==""?"이번 기록에서 확인한 준수 항목이 없습니다.":good.Trim();
  f.issues=bad==""?"관찰한 조건에서 위반 없음. 시선 확인·전체 주의 의무는 자동 판정하지 않습니다.":bad.Trim();return f;
 }
 public static string Title(int v){switch(v){case 8:return "적색 점멸 · 정지 후 진행";case 9:return "황색 점멸 · 주변 확인";case 10:return "녹색 · 출구 정체";case 11:return "녹색 · 출구 공간 있음";case 12:return "비보호 좌회전 · 적색 대기";case 13:return "비보호 좌회전 · 대향차 양보";default:return "비보호 좌회전 · 녹색 / 대향차 없음";}}
 public static string Conditions(int v){return "은계 가상 교육 구간 · 실제 현장 규제가 아닙니다.\n"+(v==8?"전방 적색 점멸 / 보행자·교차 차량 없음":v==9?"전방 황색 점멸 / 보행자·교차 차량 없음":v==10?"전방 녹색 / 출구에 대기 차량 / 정지선 앞 대기 후 공간 확보":v==11?"전방 녹색 / 출구 공간 확보 / 직진":v==12?"비보호 표지 / 전방 적색 / 정지선 앞 대기 후 녹색":v==13?"비보호 표지 / 녹색 / 대향차가 지나갈 때까지 양보":"비보호 표지 / 녹색 / 대향차·보행자 없음");}
 public static string Guidance(int v){return v==8?"① 정지선 앞 완전 정지 → 다른 교통 확인 → ② 맞은편 도로로 직진. 적색 점멸은 유지됩니다.":v==9?"① 다른 교통·안전표지에 주의 → ② 직진. 이 장면에는 정지 의무가 따로 없으며, 시선 확인은 자동 채점하지 않습니다.":v==10?"① 녹색이어도 출구가 막히면 정지선 밖에서 대기 → 차량이 이동해 공간이 생기면 ② 직진. 연습 대기 시간은 법정 기준이 아닙니다.":v==11?"① 녹색과 출구 공간 확인 → ② 직진. 앞 조건과 달리 공간이 확보되어 있습니다.":v==12?"① 적색에서는 정지선 앞 대기 → 녹색과 대향차 확인 → ② 왼쪽 도로로 좌회전. 비보호는 적색 진입 허가가 아닙니다.":v==13?"① 녹색이어도 대향차 양보 → ② 대향차가 교차로를 벗어난 뒤 왼쪽 도로로 회전. 이 연습에서는 정지선 앞에서 기다리면 대향차가 출발합니다.":"① 녹색·비보호 표지·대향차 없음 확인 → ② 왼쪽 도로로 좌회전. 불필요한 완전 정지를 요구하지 않습니다.";}
}
