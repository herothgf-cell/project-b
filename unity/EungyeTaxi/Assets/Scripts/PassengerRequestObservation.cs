using UnityEngine;
[System.Serializable] public class PassengerRequestObservationSnapshot {
 public string systemRouteReason;
 public PassengerRequestSnapshot request;public string observedTrip,selectedEffect;public float stoppedSeconds,travelled,broadTravel,expectedDistance,expectedBroad,etaAge,etaPredicted,etaNav,acceptedAge,addedDistance,newRemaining;public int motionBefore,lawBefore,accelBefore,brakeBefore,necessaryBefore,proposal,newFare;public bool etaSelected,quietReply,adviceFastest,preferLarge,preferFast;
}
// Disabled components owned and ticked by PassengerRequestUI: one observation per promise.
public class PassengerRequestObservation : MonoBehaviour {
 public string SystemRouteReason="";
 public DriveSession session;public readonly PassengerRequest request=new PassengerRequest();
 string observedTrip="";float stoppedSeconds,resultUntil,travelled,broadTravel,expectedDistance,expectedBroad,etaAt,etaPredicted,etaNav,acceptedAt;int motionBefore,lawBefore,accelBefore,brakeBefore,necessaryBefore;Vector3 previous;bool sample,etaSelected,quietReply,adviceFastest;
 public bool SuppressFollowupDialogue {get{return quietReply;}}
 string selectedEffect="";PassengerRequestChoice[] cachedChoices;RequestKind cachedKind;
 int proposal=-1;float proposalUntil;Vector3 proposalPosition;float addedDistance,newRemaining;int newFare;
 public PassengerRequestObservationSnapshot CaptureLive(){return new PassengerRequestObservationSnapshot{systemRouteReason=SystemRouteReason,request=request.Snapshot(),observedTrip=observedTrip,selectedEffect=selectedEffect,stoppedSeconds=stoppedSeconds,travelled=travelled,broadTravel=broadTravel,expectedDistance=expectedDistance,expectedBroad=expectedBroad,etaAge=Time.time-etaAt,etaPredicted=etaPredicted,etaNav=etaNav,acceptedAge=Time.time-acceptedAt,addedDistance=addedDistance,newRemaining=newRemaining,motionBefore=motionBefore,lawBefore=lawBefore,accelBefore=accelBefore,brakeBefore=brakeBefore,necessaryBefore=necessaryBefore,proposal=proposal,newFare=newFare,etaSelected=etaSelected,quietReply=quietReply,adviceFastest=adviceFastest,preferLarge=session.jobs.nav.PreferLargeRoad,preferFast=session.jobs.nav.PreferFastest};}
 public void RestoreLive(PassengerRequestObservationSnapshot s){SystemRouteReason=s.systemRouteReason??"";request.Restore(s.request);observedTrip=s.observedTrip;selectedEffect=s.selectedEffect;stoppedSeconds=0;travelled=s.travelled;broadTravel=s.broadTravel;expectedDistance=s.expectedDistance;expectedBroad=s.expectedBroad;etaAt=Time.time-s.etaAge;etaPredicted=s.etaPredicted;etaNav=s.etaNav;acceptedAt=Time.time-s.acceptedAge;addedDistance=s.addedDistance;newRemaining=s.newRemaining;motionBefore=s.motionBefore;lawBefore=s.lawBefore;accelBefore=s.accelBefore;brakeBefore=s.brakeBefore;necessaryBefore=s.necessaryBefore;proposal=s.proposal;newFare=s.newFare;etaSelected=s.etaSelected;quietReply=s.quietReply;adviceFastest=s.adviceFastest;sample=false;}
 static readonly string[] lines={"조용히 운행해 주세요.","도착까지 얼마나 걸릴까요?","차멀미가 심해요. 부드럽게 운전해 주세요.","급해요. 규칙을 지키며 제시간에 도착해 주세요.","골목 대신 큰길로 가 주세요.","가능한 짧은 길로 가 주세요.","가까운 안전한 승하차 지점에 내려 주세요.","오늘 속상한 일이 있었어요.","어느 길이 좋을까요? 빠른 길을 추천해 주세요.","오늘 면접인데 잘할 수 있을까요?","라디오 음악을 바꿔 주세요. (텍스트 연출)","택시비가 부족한데 요금을 알려 주세요.","이 동네에서 오래 일하셨어요?","다른 승하차 지점으로 목적지를 바꿔 주세요.","안전한 승하차 지점에 잠깐 들러 주세요.","하차 후 분실물 발견 시 보관·신고 절차를 부탁드려요.","빨간불이어도 그냥 가 주세요."};
 public static string UpcomingTitle(int completed){return lines[Mathf.Abs(completed)%lines.Length];}
 public void BeginKind(RequestKind kind){selectedEffect="";observedTrip=session.law.tripId;
  if(!request.Begin(observedTrip,kind))return;stoppedSeconds=travelled=broadTravel=0;resultUntil=0;proposal=-1;proposalUntil=0;sample=etaSelected=quietReply=false;
  motionBefore=MotionCount();lawBefore=session.law.ledger.Events.Length;
  // Eligibility can disappear between boarding assignment and a later stop.
  // Keep the assigned kind stable across saves; never silently redraw it.
  if(kind==RequestKind.LargeRoad&&!session.jobs.nav.HasLargeRoadAlternative())request.DeferSafety();
 }
 int MotionCount(){var m=session.manners;return m.accelEvents+m.brakeEvents+m.collisionEvents;}
 int Violations(){int count=0;var events=session.law.ledger.Events;for(int i=lawBefore;i<events.Length;i++)if(events[i].Deduction>0)count++;return count;}
 public bool LawClear {get{return Violations()==0;}}
 public bool RespondEta(int option){if(request.Kind!=RequestKind.Eta||request.Resolved||!SafeToRespond()||session.jobs.nav.path.Count<2)return false;float eta=session.jobs.nav.estimatedSeconds;float predicted=PassengerRequest.EtaSeconds(eta,option);if(float.IsNaN(predicted))return false;etaNav=eta;etaPredicted=predicted;etaAt=Time.time;etaSelected=true;request.Accept("내비 예상 "+EtaChoice(eta)+" / 안내 예상 "+EtaChoice(predicted)+" 뒤 도착 · 교통에 따라 달라질 수 있는 예상");resultUntil=Time.time+8;return true;}
 public bool SafeToRespond(){return session.requests.SafeToRespond();}
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
  if(ChangesRoute){if(!PrepareProposal())return false;bool ok=kind==RequestKind.ViaStop?session.jobs.TrySetPassengerVia(proposal):session.jobs.TryChangeDestination(proposal);if(!ok)return false;response=(kind==RequestKind.ViaStop?"안전 경유 수락: ":"안전 목적지 변경: ")+KoreanText.Stop(session.jobs.stops[proposal].name)+" · "+session.jobs.FareAgreementText(newFare)+" · 남은 기한 "+Mathf.CeilToInt(newRemaining)+"초 · 동일 운송";}
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
   case RequestKind.FareHelp:response=session.jobs.FareAgreementText(session.jobs.current.fare)+" · 승객 만족도 요구 미달 시 만족도/요구치 비율 지급 · 늦은 도착0원";break;
   case RequestKind.LocalChat:response="은계 주변을 다니고 있어요. 안전하게 모셔다드릴게요.";break;
   case RequestKind.LostItem:response="하차 후 분실물을 발견하면 보관·신고 절차를 안내하겠습니다. (텍스트 사건)";break;
   default:response="죄송하지만 신호는 지켜야 해요. 안전하게 모셔다드릴게요.";break;
  }
  if(kind==RequestKind.LargeRoad||kind==RequestKind.ShortRoad||kind==RequestKind.RouteAdvice)response+=" · 거리 "+beforeDistance.ToString("0")+"→"+nav.remaining.ToString("0")+"m / ETA "+Mathf.CeilToInt(beforeEta)+"→"+RoadNavigation.FormatEta(nav.estimatedSeconds)+"";
  request.Accept(response);acceptedAt=Time.time;if(kind==RequestKind.Comfort){session.manners.ComfortAccepted=true;motionBefore=MotionCount();accelBefore=session.manners.accelEvents;brakeBefore=session.manners.brakeEvents;necessaryBefore=session.manners.necessaryBrakeEvents;}quietReply=kind==RequestKind.Quiet;travelled=broadTravel=0;sample=false;resultUntil=Time.time+6;return true;
 }
 public string AlternativeLabel {get{switch(request.Kind){case RequestKind.Empathy:case RequestKind.Interview:return "조용히 배려";case RequestKind.RouteAdvice:return "짧은 길";case RequestKind.LocalChat:return "안전 운행 안내";case RequestKind.LostItem:return "안전한 전달 절차 안내";case RequestKind.FareHelp:return "결제 방법은 하차 때 확인";default:return "";}}}
 public bool RespondAlternative(){if(request.Resolved||!SafeToRespond()||AlternativeLabel.Length==0)return false;string text;
  if(request.Kind==RequestKind.RouteAdvice){if(!session.jobs.nav.SetPassengerPreference(false,false))return false;adviceFastest=false;expectedDistance=session.jobs.nav.remaining;text="짧은 길 적용 · "+Mathf.CeilToInt(session.jobs.nav.remaining)+"m / "+RoadNavigation.FormatEta(session.jobs.nav.estimatedSeconds)+" 예상";}
  else text=request.Kind==RequestKind.LocalChat?"안전하게 모셔다드리겠습니다":request.Kind==RequestKind.LostItem?"분실물은 보관하고 안전한 전달·신고 절차를 안내하겠습니다":request.Kind==RequestKind.FareHelp?"하차 때 결제 방법을 확인하겠습니다. "+session.jobs.FareAgreementText(session.jobs.current.fare)+" · 승객 만족도 요구 미달 시 만족도/요구치 비율 지급 / 늦은 도착0원":"편하게 쉬시거나 준비하실 수 있도록 조용히 운행하겠습니다";
  quietReply=request.Kind==RequestKind.Empathy||request.Kind==RequestKind.Interview;request.Accept(text);acceptedAt=Time.time;travelled=broadTravel=0;sample=false;resultUntil=Time.time+6;return true;
 }
 public void Finish(){if(!session.business.useV2)return;
  if(request.Accepted){bool success=true;string reason="";
   if(!string.IsNullOrEmpty(SystemRouteReason)&&PassengerRequestSchedule.Route(request.Kind))request.CompleteUnknown(SystemRouteReason+" · 시스템 강제 대기/우회는 요청 미이행에서 제외");
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
  int deduction=request.Finish();var notices=session.GetComponent<DriveNotifications>();if(notices)notices.RefreshScores();if(deduction>0)session.manners.Deduct("request:"+observedTrip,deduction,"조용한 운행 요청 미응답",0);
 }
 public void Observe(bool input){if(!session.business.useV2||session.jobs.trip.State!=TripState.OnTrip||session.jobs.trip.ArrivalLocked||session.paused)return;
  bool safe=input&&SafeToRespond();if(safe){stoppedSeconds+=Time.deltaTime;request.ObserveOpportunity(stoppedSeconds>=2);}else stoppedSeconds=0;
  Vector3 p=session.taxi.transform.position;if(request.Accepted&&sample){float d=Vector3.Distance(previous,p);if(d<Mathf.Max(2,session.taxi.body.velocity.magnitude*Time.deltaTime+1)){travelled+=d;if(session.jobs.nav.CurrentRoadWidth()>=12)broadTravel+=d;}}previous=p;sample=true;
  if(input&&Input.GetKeyDown(KeyCode.Y))session.requests.Choose(request.Kind==RequestKind.Eta?PassengerReply.EtaFaster:PassengerReply.Accept);
  if(input&&Input.GetKeyDown(KeyCode.I))session.requests.Choose(request.Kind==RequestKind.Eta?PassengerReply.EtaNormal:PassengerReply.Alternative);
  if(input&&Input.GetKeyDown(KeyCode.O)&&request.Kind==RequestKind.Eta)session.requests.Choose(PassengerReply.EtaRelaxed);
  if(input&&Input.GetKeyDown(KeyCode.U))session.requests.Choose(PassengerReply.Defer);
 }
 public string DialogueReply {get{if(request.SafetyDeferred)return ChangesRoute?"이번에는 원래 목적지로 안전하게 모시겠습니다.":"안전한 곳에 정차한 뒤 확인할게요.";string value=request.ResponseText();string prefix=request.Id+" · ";return value.StartsWith(prefix)?value.Substring(prefix.Length):value;}}
 static string EtaChoice(float value){return (value/60).ToString("0.0")+"분";}
 public PassengerRequestChoice[] ReplyChoices {get{if(cachedChoices==null||cachedKind!=request.Kind){cachedKind=request.Kind;cachedChoices=PassengerRequestChoices.For(cachedKind);}return cachedChoices;}}
 public bool CanChoose(PassengerReply reply){
  if(request.Resolved||session.jobs.trip.State!=TripState.OnTrip||session.jobs.trip.ArrivalLocked||session.paused||Time.timeScale==0)return false;
  bool offered=false;foreach(var c in ReplyChoices)if(c.reply==reply)offered=true;if(!offered)return false;
  if(!SafeToRespond())return false;
  if(reply==PassengerReply.Defer)return true;
  if(request.Kind==RequestKind.Eta)return session.jobs.nav.path.Count>=2&&!float.IsNaN(PassengerRequest.EtaSeconds(session.jobs.nav.estimatedSeconds,0));
  return !ChangesRoute||PrepareProposal();
 }
 public bool Choose(PassengerReply reply){if(!CanChoose(reply))return false;bool applied;
  switch(reply){case PassengerReply.EtaFaster:applied=RespondEta(-1);break;case PassengerReply.EtaNormal:applied=RespondEta(0);break;case PassengerReply.EtaRelaxed:applied=RespondEta(1);break;case PassengerReply.Alternative:applied=RespondAlternative();break;default:applied=Respond(reply!=PassengerReply.Defer);break;}
  if(applied){foreach(var c in ReplyChoices)if(c.reply==reply)selectedEffect=c.effect;}return applied;
 }
 public string ChoiceText(PassengerRequestChoice choice){string label=choice.label;
  if(request.Kind==RequestKind.Eta&&choice.reply!=PassengerReply.Defer){int option=choice.reply==PassengerReply.EtaFaster?-1:choice.reply==PassengerReply.EtaRelaxed?1:0;float seconds=PassengerRequest.EtaSeconds(session.jobs.nav.estimatedSeconds,option);label+=" · "+(float.IsNaN(seconds)?"경로 확인 중":EtaChoice(seconds));}
  return label+"\n"+choice.effect;
 }
 public static Rect ReplyButtonRect(Rect area,int index){float w=(area.width-8)/2;return new Rect(area.x+(index%2)*(w+8),area.y+(index/2)*49,w,45);}
 public bool PanelVisible {get{return !request.Resolved||Time.time<=resultUntil||etaSelected;}}
 public void Draw(){if(!session.business.useV2||session.jobs.trip.State!=TripState.OnTrip||session.jobs.trip.ArrivalLocked||session.paused)return;if(request.Resolved&&Time.time>resultUntil&&!etaSelected)return;
  string text=request.Resolved?"나  “"+DialogueReply+"”":"손님  “"+lines[(int)request.Kind]+"”";
  if(etaSelected)text="도착 예상 안내 · 교통에 따라 변동\n현재 남은 "+RoadNavigation.FormatEta(session.jobs.nav.estimatedSeconds)+" · 최초 안내까지 "+RoadNavigation.FormatEta(Mathf.Max(0,etaPredicted-(Time.time-etaAt)));
  if(!request.Resolved){if(ChangesRoute){if(!PrepareProposal())text+="\n안전한 변경 후보 없음 · 원래 운행 유지 가능";else text+="\n"+KoreanText.Stop(session.jobs.stops[proposal].name)+" · 추가 "+addedDistance.ToString("0")+"m · "+session.jobs.FareAgreementText(newFare)+" / 기한 "+Mathf.CeilToInt(newRemaining)+"초";}
   if(!SafeToRespond())text+="\n안전하게 정차하면 답변을 선택할 수 있어요.";
  }else if(selectedEffect.Length>0)text+="\n적용: "+selectedEffect;
  var priorMatrix=GUI.matrix;float scale=CockpitUI.Scale;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));float vw=Screen.width/scale,vh=Screen.height/scale;
  var style=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=15,wordWrap=true};style.normal.textColor=new Color(.1f,.16f,.2f);
  float width=540,textHeight=Mathf.Min(132,style.CalcHeight(new GUIContent(text),width-24));int rows=request.Resolved?0:(ReplyChoices.Length+1)/2;float height=textHeight+24+rows*49;
  // Keep the reply beside the road view and above the passenger destination card.
  float y=vh-24-height,x=(vw-width)/2;var panel=new Rect(x,y,width,height);
  GUI.color=new Color(.94f,.97f,.91f,1);GUI.DrawTexture(panel,Texture2D.whiteTexture);GUI.DrawTexture(new Rect(x+14,y+height-1,12,8),Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(new Rect(x+12,y+8,width-24,textHeight),text,style);
  if(!request.Resolved){var button=new GUIStyle(GUI.skin.button){font=KoreanText.Font,fontSize=11,wordWrap=true,padding=new RectOffset(4,4,2,2)};var area=new Rect(x+8,y+textHeight+18,width-16,rows*49);
   for(int i=0;i<ReplyChoices.Length;i++){var choice=ReplyChoices[i];bool enabled=GUI.enabled;GUI.enabled=enabled&&CanChoose(choice.reply);if(GUI.Button(ReplyButtonRect(area,i),ChoiceText(choice),button))session.requests.Choose(choice.reply);GUI.enabled=enabled;}
  }
  GUI.matrix=priorMatrix;
 }
}
