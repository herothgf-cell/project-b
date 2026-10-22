using UnityEngine;
public class PassengerRequestUI : MonoBehaviour {
 public DriveSession session;public readonly PassengerRequest request=new PassengerRequest();
 string observedTrip="";float stoppedSeconds,resultUntil,travelled,broadTravel,expectedDistance,expectedBroad,etaAt,etaPredicted,etaNav,acceptedAt;int motionBefore,lawBefore,accelBefore,brakeBefore,necessaryBefore;Vector3 previous;bool sample,etaSelected,quietReply,adviceFastest;
 public bool SuppressFollowupDialogue {get{return quietReply;}}
 int proposal=-1;float proposalUntil;Vector3 proposalPosition;float addedDistance,newRemaining;int newFare;
 static readonly string[] lines={"조용히 운행해 주세요.","도착까지 얼마나 걸릴까요?","차멀미가 심해요. 부드럽게 운전해 주세요.","급해요. 규칙을 지키며 제시간에 도착해 주세요.","골목 대신 큰길로 가 주세요.","가능한 짧은 길로 가 주세요.","가까운 안전한 승하차 지점에 내려 주세요.","오늘 속상한 일이 있었어요.","어느 길이 좋을까요? 빠른 길을 추천해 주세요.","오늘 면접인데 잘할 수 있을까요?","라디오 음악을 바꿔 주세요. (텍스트 연출)","택시비가 부족한데 요금을 알려 주세요.","이 동네에서 오래 일하셨어요?","다른 승하차 지점으로 목적지를 바꿔 주세요.","안전한 승하차 지점에 잠깐 들러 주세요.","하차 후 분실물 발견 시 보관·신고 절차를 부탁드려요.","빨간불이어도 그냥 가 주세요."};
 public static string UpcomingTitle(int completed){return lines[Mathf.Abs(completed)%lines.Length];}
 public void Begin(){if(!session.business.useV2)return;BeginKind((RequestKind)(session.jobs.completed%lines.Length));}
 public void BeginKind(RequestKind kind){observedTrip=session.law.tripId;if(kind==RequestKind.LargeRoad&&!session.jobs.nav.HasLargeRoadAlternative())kind=RequestKind.Eta;
  if(!request.Begin(observedTrip,kind))return;stoppedSeconds=travelled=broadTravel=0;resultUntil=0;proposal=-1;proposalUntil=0;sample=etaSelected=quietReply=false;session.manners.ComfortAccepted=false;
  motionBefore=MotionCount();lawBefore=session.law.ledger.Events.Length;
 }
 int MotionCount(){var m=session.manners;return m.accelEvents+m.brakeEvents+m.collisionEvents;}
 int Violations(){int count=0;var events=session.law.ledger.Events;for(int i=lawBefore;i<events.Length;i++)if(events[i].Deduction>0)count++;return count;}
 public bool LawClear {get{return Violations()==0;}}
 public bool RespondEta(int option){if(request.Kind!=RequestKind.Eta||request.Resolved||!SafeToRespond()||session.jobs.nav.path.Count<2)return false;float eta=session.jobs.nav.estimatedSeconds;float predicted=PassengerRequest.EtaSeconds(eta,option);if(float.IsNaN(predicted))return false;etaNav=eta;etaPredicted=predicted;etaAt=Time.time;etaSelected=true;request.Accept("내비 예상 "+EtaChoice(eta)+" / 안내 예상 "+EtaChoice(predicted)+" 뒤 도착 · 교통에 따라 달라질 수 있는 예상");resultUntil=Time.time+8;return true;}
 public bool SafeToRespond(){if(session.taxi.body.velocity.magnitude>.1f||session.paused||Time.timeScale==0)return false;
  foreach(var j in session.traffic.junctions)if(Vector3.Distance(session.taxi.transform.position,j.center)<Mathf.Max(j.halfWidth[0],j.halfWidth[1])+12)return false;
  return true;
 }
 bool ChangesRoute {get{return request.Kind==RequestKind.SafeStop||request.Kind==RequestKind.DestinationChange||request.Kind==RequestKind.ViaStop;}}
 public bool PrepareProposal(){if(!ChangesRoute)return true;var zones=session.GetComponent<ServiceLawZones>();if(zones&&zones.Live&&zones.Live.Active)return false;
  if(Time.time<proposalUntil&&Vector3.Distance(proposalPosition,session.taxi.transform.position)<.25f)return proposal>=0;proposalUntil=Time.time+1;proposalPosition=session.taxi.transform.position;
  float best=float.MaxValue;proposal=-1;var jobs=session.jobs;
  for(int i=0;i<jobs.stops.Length;i++){if(i==jobs.current.drop||i==jobs.current.pickup)continue;float d;int f;float t;
   if(!jobs.PreviewPassengerChange(i,request.Kind==RequestKind.ViaStop,out d,out f,out t))continue;
   float rank=request.Kind==RequestKind.SafeStop?Vector3.Distance(session.taxi.transform.position,jobs.stops[i].Kerb):d;
   if(rank>=best)continue;best=rank;proposal=i;addedDistance=d;newFare=f;newRemaining=t;
  }return proposal>=0;
 }
 public bool Respond(bool accept){if(request.Resolved)return false;if(!accept){request.DeferSafety();resultUntil=Time.time+6;return true;}if(!SafeToRespond())return false;
  if(request.Kind==RequestKind.Eta)return RespondEta(0);
  var nav=session.jobs.nav;var kind=request.Kind;string response;
  float beforeDistance=nav.remaining,beforeEta=nav.estimatedSeconds;
  if(ChangesRoute){if(!PrepareProposal())return false;bool ok=kind==RequestKind.ViaStop?session.jobs.TrySetPassengerVia(proposal):session.jobs.TryChangeDestination(proposal);if(!ok)return false;response=(kind==RequestKind.ViaStop?"안전 경유 수락: ":"안전 목적지 변경: ")+KoreanText.Stop(session.jobs.stops[proposal].name)+" · 약정 "+newFare.ToString("N0")+"원 · 남은 기한 "+Mathf.CeilToInt(newRemaining)+"초 · 동일 운송";}
  else switch(kind){
   case RequestKind.Quiet:response="조용히 운행하겠습니다";break;
   case RequestKind.Eta:response="내비 예상 "+RoadNavigation.FormatEta(nav.estimatedSeconds)+" 안내 (교통에 따라 변경)";break;
   case RequestKind.Comfort:response="네, 편안히 모실게요. 수락 뒤 불필요한 급가속·급제동 각 -4점 (필요한 제동 제외)";break;
   case RequestKind.OnTime:response="신호를 지키면서 약속한 시간에 도착하도록 할게요. 남은 기한 "+Mathf.CeilToInt(session.jobs.trip.Remaining)+"초 · 기한 연장 없음";break;
   case RequestKind.LargeRoad:if(!nav.SetPassengerPreference(true,true))return false;expectedBroad=nav.LargeRoadShare();expectedDistance=nav.remaining;response="네, 가능한 큰길을 따라 모셔다드릴게요.";break;
   case RequestKind.ShortRoad:if(!nav.SetPassengerPreference(false,false))return false;expectedDistance=nav.remaining;response="네, 짧은 길로 안내해 드릴게요.";break;
   case RequestKind.Empathy:response="속상하셨겠어요. 편안히 말씀하시거나 쉬셔도 괜찮아요";break;
   case RequestKind.RouteAdvice:if(!nav.SetPassengerPreference(false,true))return false;adviceFastest=true;expectedDistance=nav.remaining;response="빠른 길 적용 · "+RoadNavigation.FormatEta(nav.estimatedSeconds)+" / "+Mathf.CeilToInt(nav.remaining)+"m 예상";break;
   case RequestKind.Interview:response="응원합니다. 편하게 준비하실 수 있게 운행할게요";break;
   case RequestKind.Music:response="음악을 바꿔 달라는 말씀이시죠? (프로토에서는 대화만 제공)";break;
   case RequestKind.FareHelp:response="약정 "+session.jobs.current.fare.ToString("N0")+"원 · 승객 만족도 요구 미달 시70%, 늦은 도착0원 · 할인·미수금 없음";break;
   case RequestKind.LocalChat:response="은계 주변을 다니고 있어요. 안전하게 모셔다드릴게요.";break;
   case RequestKind.LostItem:response="내리실 때 소지품을 함께 확인해 드릴게요.";break;
   default:response="죄송하지만 신호는 지켜야 해요. 안전하게 모셔다드릴게요.";break;
  }
  if(kind==RequestKind.LargeRoad||kind==RequestKind.ShortRoad||kind==RequestKind.RouteAdvice)response+=" · 거리 "+beforeDistance.ToString("0")+"→"+nav.remaining.ToString("0")+"m / ETA "+Mathf.CeilToInt(beforeEta)+"→"+RoadNavigation.FormatEta(nav.estimatedSeconds)+"";
  request.Accept(response);acceptedAt=Time.time;if(kind==RequestKind.Comfort){session.manners.ComfortAccepted=true;motionBefore=MotionCount();accelBefore=session.manners.accelEvents;brakeBefore=session.manners.brakeEvents;necessaryBefore=session.manners.necessaryBrakeEvents;}quietReply=kind==RequestKind.Quiet;travelled=broadTravel=0;sample=false;resultUntil=Time.time+6;return true;
 }
 public string AlternativeLabel {get{switch(request.Kind){case RequestKind.Empathy:case RequestKind.Interview:return "조용히 배려";case RequestKind.RouteAdvice:return "짧은 길";case RequestKind.LocalChat:return "안전 운행 안내";case RequestKind.LostItem:return "안전한 전달 절차 안내";case RequestKind.FareHelp:return "결제 방법은 하차 때 확인";default:return "";}}}
 public bool RespondAlternative(){if(request.Resolved||!SafeToRespond()||AlternativeLabel.Length==0)return false;string text;
  if(request.Kind==RequestKind.RouteAdvice){if(!session.jobs.nav.SetPassengerPreference(false,false))return false;adviceFastest=false;expectedDistance=session.jobs.nav.remaining;text="짧은 길 적용 · "+Mathf.CeilToInt(session.jobs.nav.remaining)+"m / "+RoadNavigation.FormatEta(session.jobs.nav.estimatedSeconds)+" 예상";}
  else text=request.Kind==RequestKind.LocalChat?"안전하게 모셔다드리겠습니다":request.Kind==RequestKind.LostItem?"분실물은 보관하고 안전한 전달·신고 절차를 안내하겠습니다":request.Kind==RequestKind.FareHelp?"하차 때 결제 방법을 확인하겠습니다. 약정 "+session.jobs.current.fare.ToString("N0")+"원 · 요구 점수 미달70% / 늦은 도착0원 · 할인 없음":"편하게 쉬시거나 준비하실 수 있도록 조용히 운행하겠습니다";
  quietReply=request.Kind==RequestKind.Empathy||request.Kind==RequestKind.Interview;request.Accept(text);acceptedAt=Time.time;travelled=broadTravel=0;sample=false;resultUntil=Time.time+6;return true;
 }
 public void Finish(){if(!session.business.useV2)return;session.manners.ComfortAccepted=false;
  if(request.Accepted){bool success=true;string reason="";
   switch(request.Kind){
    case RequestKind.Eta:if(etaSelected)reason="최초 내비 "+EtaChoice(etaNav)+" / 안내 "+RoadNavigation.FormatEta(etaPredicted)+" / 실제 "+Mathf.CeilToInt(Time.time-etaAt)+"초 · "+PassengerRequest.EtaComparison(etaPredicted,Time.time-etaAt)+" · 신호·교통 대기와 우회로 달라질 수 있음 (ETA 차이 감점 없음)";break;
    case RequestKind.Comfort:success=MotionCount()==motionBefore;reason="수락 뒤 급가속 "+(session.manners.accelEvents-accelBefore)+"회 / 급제동 "+(session.manners.brakeEvents-brakeBefore)+"회 (각 -4점) / 필요한 제동 제외 "+(session.manners.necessaryBrakeEvents-necessaryBefore)+"회 · "+(success?"편안한 운행":"급가감속·충돌 "+(MotionCount()-motionBefore)+"회 · "+session.manners.RideReasons);break;
    case RequestKind.OnTime:success=session.jobs.trip.Remaining>0;int violations=0;var events=session.law.ledger.Events;for(int i=lawBefore;i<events.Length;i++)if(events[i].Deduction>0)violations++;success&=violations==0;reason="도착 시 남은 시간 "+Mathf.CeilToInt(session.jobs.trip.Remaining)+"초 · 운전 위반 "+violations+"건";break;
    case RequestKind.LargeRoad:float share=travelled>1?broadTravel/travelled:0;success=share+.15f>=expectedBroad&&travelled>1;reason="실제 큰길 주행 "+(share*100).ToString("0")+"% / 안내 "+(expectedBroad*100).ToString("0")+"% (폭12m 이상)";break;
    case RequestKind.ShortRoad:success=travelled<=expectedDistance*1.25f+30&&travelled>1;reason="실제 이동 "+travelled.ToString("0")+"m / 수락 시 안내 "+expectedDistance.ToString("0")+"m · 우회 시 참고용, 추가 감점 없음";break;
    case RequestKind.SafeStop:case RequestKind.DestinationChange:success=session.jobs.At(proposal);reason="동일 운송으로 지정 안전 목적지 하차";break;
    case RequestKind.ViaStop:success=!session.jobs.PassengerViaPending;reason=success?"안전 경유 정차 후 원래 목적지 하차":"경유 정차 미완료";break;
    case RequestKind.LostItem:reason="후속 사건 "+observedTrip+":lost-item · 하차 뒤 지갑 발견 → 보관·신고 안내 기록 (텍스트 사건 · 물품·반환 경로 없음)";break;
    case RequestKind.RouteAdvice:reason=(adviceFastest?"빠른 길":"짧은 길")+" 적용 · 안내 "+expectedDistance.ToString("0")+"m / 실제 "+travelled.ToString("0")+"m · 수락 뒤 "+Mathf.CeilToInt(Time.time-acceptedAt)+"초 · 교통에 따라 변경";if(session.jobs.nav.PreferFastest!=adviceFastest||session.jobs.nav.PreferLargeRoad)request.CompleteUnknown(reason+" · 변경된 경로의 교통·안전 사유 불명");break;
    case RequestKind.RejectIllegal:if(!LawClear){request.CompleteUnknown("불법 요구는 안전하게 거절 · 실제 위반은 독립 운행 기록으로 평가");}reason="신호 위반 요구를 거절하고 실제 확정 위반 없이 운송";break;
   }
   if(reason.Length==0)reason="선택한 응대를 실제 대사로 안내: "+DialogueReply;
   if(!success&&(request.Kind==RequestKind.LargeRoad||request.Kind==RequestKind.ShortRoad||request.Kind==RequestKind.OnTime&&Violations()==0))request.CompleteUnknown(reason+" · 합법적 대기·우회와 불필요한 이탈을 구분할 관찰 부족");
   else request.CompleteGoal(success,reason);
  }
  int deduction=request.Finish();if(deduction>0)session.manners.Deduct("request:"+observedTrip,deduction,"조용한 운행 요청 미응답",0);
 }
 void Update(){if(!session.business.useV2||session.jobs.trip.State!=TripState.OnTrip||session.paused)return;
  bool safe=SafeToRespond();if(safe){stoppedSeconds+=Time.deltaTime;request.ObserveOpportunity(stoppedSeconds>=2);}else stoppedSeconds=0;
  Vector3 p=session.taxi.transform.position;if(request.Accepted&&sample){float d=Vector3.Distance(previous,p);if(d<Mathf.Max(2,session.taxi.body.velocity.magnitude*Time.deltaTime+1)){travelled+=d;if(session.jobs.nav.CurrentRoadWidth()>=12)broadTravel+=d;}}previous=p;sample=true;
  if(request.Kind==RequestKind.Eta){if(Input.GetKeyDown(KeyCode.Y))RespondEta(-1);if(Input.GetKeyDown(KeyCode.I))RespondEta(0);if(Input.GetKeyDown(KeyCode.O))RespondEta(1);}else {if(Input.GetKeyDown(KeyCode.I))RespondAlternative();if(Input.GetKeyDown(KeyCode.Y))Respond(true);}if(Input.GetKeyDown(KeyCode.U))Respond(false);
 }
 public string DialogueReply {get{if(request.SafetyDeferred)return "안전한 곳에 정차한 뒤 확인할게요.";string value=request.ResponseText();string prefix=request.Id+" · ";return value.StartsWith(prefix)?value.Substring(prefix.Length):value;}}
 static string EtaChoice(float value){return (value/60).ToString("0.0")+"분";}
 void OnGUI(){if(!session.business.useV2||session.jobs.trip.State!=TripState.OnTrip||session.paused)return;if(request.Resolved&&Time.time>resultUntil&&!etaSelected)return;
  string text=request.Resolved?"나\n“"+DialogueReply+"”":"손님\n“"+lines[(int)request.Kind]+"”";
  if(etaSelected)text="도착 예상 안내\n현재 내비 남은 "+RoadNavigation.FormatEta(session.jobs.nav.estimatedSeconds)+" · 최초 안내까지 "+RoadNavigation.FormatEta(Mathf.Max(0,etaPredicted-(Time.time-etaAt)))+"\n최초 안내 "+RoadNavigation.FormatEta(etaPredicted)+" 뒤 도착 · 교통에 따라 변동";
  if(!request.Resolved){if(ChangesRoute){if(!PrepareProposal())text+="\n안전한 변경 경로 준비 중 · 원래 운행 유지";else text+="\n"+KoreanText.Stop(session.jobs.stops[proposal].name)+" · 추가 "+addedDistance.ToString("0")+"m\n새 약정 "+newFare.ToString("N0")+"원 · 남은 기한 "+Mathf.CeilToInt(newRemaining)+"초";}
   if(AlternativeLabel.Length>0)text+="\nI "+AlternativeLabel;
   if(request.Kind==RequestKind.Eta){float eta=session.jobs.nav.estimatedSeconds;if(session.jobs.nav.path.Count<2||float.IsNaN(PassengerRequest.EtaSeconds(eta,0)))text+="\n현재 경로를 확인한 뒤 안내할게요";else text+="\nY 조금 빠르게 약 "+EtaChoice(PassengerRequest.EtaSeconds(eta,-1))+"\nI 내비 예상에 맞춰 약 "+EtaChoice(eta)+"\nO 조금 여유롭게 약 "+EtaChoice(PassengerRequest.EtaSeconds(eta,1))+"\n교통에 따라 달라질 수 있는 예상";text+="\nU 안전한 곳에서 확인할게요";}
   else text+="\nY "+(ChangesRoute?"네, 이 조건으로 변경할게요":request.Kind==RequestKind.RejectIllegal?"신호는 지켜야 해요":"네, 알겠습니다")+" / U 안전한 곳에서 확인할게요";}
  if(!SafeToRespond()&&!request.Resolved)text="손님\n“"+lines[(int)request.Kind]+"”\n안전하게 정차하면 답변할 수 있어요. 지금은 보류 · 감점 없음";
  if(!ChangesRoute)text+="\n조건: 운임·목적지 그대로";
  var style=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=14,wordWrap=true};style.normal.textColor=new Color(.1f,.16f,.2f);float width=Mathf.Min(440,Screen.width*.37f);float height=style.CalcHeight(new GUIContent(text),width-32)+24;float y=Mathf.Min(390,Screen.height-190-height);
  GUI.color=new Color(.94f,.97f,.91f,.97f);GUI.DrawTexture(new Rect(16,y,width,height),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(30,y+height-1,12,10),Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(new Rect(32,y+12,width-32,height-24),text,style);
 }
}
