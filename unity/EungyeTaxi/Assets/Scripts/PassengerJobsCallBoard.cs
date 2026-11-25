using UnityEngine;

// 08 chapter 3 (2026-11-23, Eungye single only): judgement material on each call card and a call board that
// mixes calls of different character. PvP boards and shared physics are untouched. Every number is a content
// setting (offer.*); new JobOffer fields default to "ordinary", so older saves load unchanged.
public static class CallBoardRules {
 public const int Ordinary=0,Near=1,Rush=2,Luggage=3,Hidden=4,OutOfTown=7,Emergency=8;
 static float F(string id,double fallback){return (float)ContentTuning.Number(id,fallback);}
 public static bool VarietyEnabled {get{return ContentTuning.Int("offer.variety.enabled",1)!=0;}}
 public static float NearMaxPickup {get{return F("offer.near.maxPickupMetres",450);}}
 public static float NearMaxRide {get{return F("offer.near.maxRideMetres",1000);}}
 public static float RushDeadline {get{return F("offer.rush.deadlineMultiplier",.75);}}
 public static float RushFare {get{return F("offer.rush.fareMultiplier",1.4);}}
 public static int LuggageFareBonus {get{return ContentTuning.Int("offer.luggage.fareBonus",800);}}
 public static float PickupMetresPerSecond {get{return F("offer.eta.pickupMetresPerSecond",4);}}
 public static string Label(int variety){return variety==Emergency?"응급 손님 · "+EmergencyRules.HospitalName+" 방면 · 기한 짧음":variety==OutOfTown?OutOfTownRules.Label+" · 시외 구간 "+OutOfTownRules.Minutes+"분 · 고액":variety==Near?"근거리 · 무난":variety==Rush?"급한 손님 · 기한 짧음 · 고액":variety==Luggage?"큰 짐 · 트렁크 필요":variety==Hidden?"길가 손님 · 목적지는 타면 알려 줌":"";}
 public static string DemandLabel(int level){return level>=2?"높음":level==1?"보통":"낮음";}
 // Short tag for the map card; Label stays the full sentence used by captions and checks.
 public static string ShortLabel(int variety){return variety==Emergency?"응급 손님":variety==OutOfTown?OutOfTownRules.Label:variety==Near?"근거리":variety==Rush?"급한 손님":variety==Luggage?"큰 짐":variety==Hidden?"길가 손님":"";}
}

public partial class PassengerJobs {
 bool CallBoardSingle {get{return !session.simulationMatch&&session.business&&session.business.ledger!=null&&session.business.ledger.OneDay;}}
 // A hidden destination stays hidden until the passenger is aboard.
 public bool DestinationHidden(JobOffer o){return o!=null&&o.variety==CallBoardRules.Hidden&&!(o==current&&trip.State==TripState.OnTrip);}
 public float CallPickupMetres(JobOffer o){return o==null?float.PositiveInfinity:PickupDistance(o.pickup);}
 // Pickup drive at the board's 4 m/s convention, then the safe ride time and expected waits.
 public float CallEtaSeconds(JobOffer o){if(o==null)return float.PositiveInfinity;float ride=o.safeRouteSeconds>0?o.safeRouteSeconds:o.distance/8f;return CallPickupMetres(o)/Mathf.Max(.5f,CallBoardRules.PickupMetresPerSecond)+ride+o.waitSeconds;}
 // Passenger demand around the drop-off for the current phase: preferred region = high, neutral = normal, else low.
 public int DropDemandLevel(JobOffer o){
  if(o==null)return 1;string region=DemandRegion(o.drop);if(region=="neutral")return 1;
  int phase=PassengerDemandPolicy.Phase(session.business.ledger.Elapsed,session.business.ledger.Settings.daySeconds,false);
  return PassengerDemandPolicy.Weight(region,phase,false)>=3?2:0;
 }
 // 2026-11-24 test feedback (map sidebar had too much text): the same facts as CallCardCaption, split into
 // short fields the map draws beside icons. Empty fields are not drawn.
 public sealed class CallCard {public string number="",pickup="",drop="",fare="",deadline="",distance="",tags="";}
 public CallCard CallCardParts(int index,int drivingScore){
  var o=offers[index];
  var card=new CallCard{number=(index+1).ToString(),pickup=KoreanText.Stop(stops[o.pickup].name),drop=DestinationHidden(o)?"타면 알려 줌":KoreanText.Stop(stops[o.drop].name),
   fare=PassengerReward.AdjustedFare(o.fare,drivingScore).ToString("N0")+"원"};
  var tags=new System.Collections.Generic.List<string>();
  if(o.reunionKind!=0)tags.Add("실습 끝난 학생");
  if(CallBoardSingle){
   card.deadline=RoadNavigation.FormatEta(o.seconds);
   card.distance=Mathf.RoundToInt(CallPickupMetres(o))+"m·"+RoadNavigation.FormatEta(CallEtaSeconds(o));
   string kind=IsLastCall(o)?IdleTimeRules.Label(o.variety):CallBoardRules.ShortLabel(o.variety);if(kind.Length>0)tags.Add(kind);
   if(!DestinationHidden(o)){int demand=DropDemandLevel(o);if(demand!=1)tags.Add("하차지 수요 "+CallBoardRules.DemandLabel(demand));}
   if(IsLastCall(o)&&Finite(HomeReturnMetres(o)))tags.Add("하차 후 숙소 "+Mathf.RoundToInt(HomeReturnMetres(o))+"m");
   if(o.variety==CallBoardRules.OutOfTown)tags.Add(OutOfTownCardLine(o));
   if(o.variety==CallBoardRules.Emergency)tags.Add(EmergencyCardLine(o));
   string mission=MissionCardLine(o).Trim();if(mission.Length>0)tags.Add(mission);
  }
  card.tags=string.Join(" · ",tags.ToArray());return card;
 }
 public string CallCardCaption(int index,int drivingScore){
  var o=offers[index];
  string head=(index+1)+(o.reunionKind!=0?"  실습 끝난 학생 · ":"  ");
  string drop=DestinationHidden(o)?"목적지 비공개 (탑승 후 안내)":KoreanText.Stop(stops[o.drop].name);
  string fare="예상 운임 "+PassengerReward.AdjustedFare(o.fare,drivingScore).ToString("N0")+"원";
  if(!CallBoardSingle)return head+KoreanText.Stop(stops[o.pickup].name)+"\n→ "+drop+"\n"+fare;
  string kind=IsLastCall(o)?IdleTimeRules.Label(o.variety):CallBoardRules.Label(o.variety);
  string home=IsLastCall(o)&&Finite(HomeReturnMetres(o))?"\n하차 후 덕성프라자까지 약 "+Mathf.RoundToInt(HomeReturnMetres(o))+"m":"";
  string facts="픽업 "+Mathf.RoundToInt(CallPickupMetres(o))+"m · 약 "+RoadNavigation.FormatEta(CallEtaSeconds(o))+(DestinationHidden(o)?"":" · 하차지 수요 "+CallBoardRules.DemandLabel(DropDemandLevel(o)));
  string outOfTown=o.variety==CallBoardRules.OutOfTown?"\n"+OutOfTownCardLine(o):"";
  return head+(kind.Length>0?"["+kind+"]\n":"")+KoreanText.Stop(stops[o.pickup].name)+"\n→ "+drop+"\n"+fare+" · 기한 "+RoadNavigation.FormatEta(o.seconds)+"\n"+facts+home+outOfTown+MissionCardLine(o);
 }
 // Called once per fresh board. Only freshly made ordinary calls change; scenario, reunion and the first
 // home dispatch keep their own rules. Three of the four characters rotate with the board seed.
 void ApplyOfferVariety(){
  if(!CallBoardSingle||!CallBoardRules.VarietyEnabled||FirstHomeDispatchPending||System.Array.Exists(offers,IsLastCall))return;
  var pool=new System.Collections.Generic.List<JobOffer>();
  foreach(var o in offers)if(o!=null&&o.variety==CallBoardRules.Ordinary&&string.IsNullOrEmpty(o.scenarioId)&&o.reunionKind==0)pool.Add(o);
  if(pool.Count==0)return;
  // Out-of-town call first (decided 2026-11-24): at most one board slot, then the usual characters.
  if(OutOfTownDue()&&GoodDrivingRules.Roll(pool[0].candidateSeed+":outoftown")<OutOfTownRules.BoardChance){var last=pool[pool.Count-1];if(MakeOutOfTown(last))pool.Remove(last);}
  if(pool.Count>0&&EmergencyBlocked()==""&&GoodDrivingRules.Roll(pool[0].candidateSeed+":emergency")<EmergencyRules.BoardChance){var first=pool[0];if(MakeEmergency(first))pool.Remove(first);}
  if(pool.Count==0)return;
  bool trunk=session.business.ledger.Account==null||session.business.ledger.Account.VehicleSpec.carriesLuggage;
  var kinds=new System.Collections.Generic.List<int>{CallBoardRules.Near,CallBoardRules.Rush,CallBoardRules.Luggage,CallBoardRules.Hidden};
  if(!trunk)kinds.Remove(CallBoardRules.Luggage);
  var order=new System.Collections.Generic.List<int>(Rotate(kinds,GoodDrivingRules.Roll(pool[0].candidateSeed+":variety")%kinds.Count));
  // Each ordinary call takes the next character it qualifies for; a character is used once per board.
  foreach(var pick in pool){
   int kind=-1;foreach(int k in order)if(k!=CallBoardRules.Near||CallPickupMetres(pick)<=CallBoardRules.NearMaxPickup&&pick.distance<=CallBoardRules.NearMaxRide){kind=k;break;}
   if(kind<0)continue;order.Remove(kind);pick.variety=kind;
   if(kind==CallBoardRules.Rush){pick.urgency=2;pick.seconds=Mathf.Max(60,Mathf.Ceil(pick.seconds*CallBoardRules.RushDeadline));pick.fare=Mathf.CeilToInt(pick.fare*CallBoardRules.RushFare/100f)*100;}
   else if(kind==CallBoardRules.Luggage)pick.fare+=Mathf.Max(0,CallBoardRules.LuggageFareBonus)/100*100;
  }
 }
 static System.Collections.Generic.IEnumerable<int> Rotate(System.Collections.Generic.List<int> list,int start){for(int i=0;i<list.Count;i++)yield return list[(start+i)%list.Count];}
 // A "big luggage" call always boards a luggage passenger when the taxi has a trunk.
 PassengerType? VarietyPassengerType(){return current!=null&&current.variety==CallBoardRules.Luggage&&(session.business.ledger.Account==null||session.business.ledger.Account.VehicleSpec.carriesLuggage)?PassengerType.Luggage:(PassengerType?)null;}
}

// 08 chapter 4 stage 2 (Eungye single): a "while stopped" summary in the drive feed and a last call before
// clocking off. Nothing here interrupts driving; the summary only replaces the feed's context line.
public static class IdleTimeRules {
 public const int LastHome=5,LastFar=6;
 static float F(string id,double fallback){return (float)ContentTuning.Number(id,fallback);}
 public static bool StopBreakEnabled {get{return ContentTuning.Int("stopBreak.enabled",1)!=0;}}
 public static float StopBreakDelay {get{return F("stopBreak.delaySeconds",1.5);}}
 public static float StopBreakPreviewMetres {get{return F("stopBreak.previewMetres",800);}}
 public static bool LastCallEnabled {get{return ContentTuning.Int("lastCall.enabled",1)!=0;}}
 public static float LastCallWindow {get{return F("lastCall.windowSeconds",240);}}
 public static float LastCallHomeMaxRide {get{return F("lastCall.homeMaxRideMetres",1200);}}
 public static float LastCallFarFare {get{return F("lastCall.farFareMultiplier",1.3);}}
 public static float LastCallFarMinReturn {get{return F("lastCall.farMinReturnMetres",1500);}}
 public static string Label(int variety){return variety==LastHome?"마지막 한 콜 · 숙소 방향":variety==LastFar?"마지막 한 콜 · 반대 방향 고액":"";}
}

public partial class PassengerJobs {
 // ---- while stopped ----
 float breakStoppedSince=-1;string breakKey="",breakPreview="";
 int DemandLevelAt(int stop){string region=DemandRegion(stop);if(region=="neutral")return 1;int phase=PassengerDemandPolicy.Phase(session.business.ledger.Elapsed,session.business.ledger.Settings.daySeconds,false);return PassengerDemandPolicy.Weight(region,phase,false)>=3?2:0;}
 string NextCallPreview(){
  string key=session.law.tripId+":"+current.drop;if(key==breakKey)return breakPreview;breakKey=key;
  var near=new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<float,int>>();
  var all=new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<float,int>>();
  for(int i=0;i<stops.Length;i++){if(i==current.drop||ShopOnly(i))continue;float d=nav.Distance(stops[current.drop],stops[i]);if(!Finite(d))continue;var pair=new System.Collections.Generic.KeyValuePair<float,int>(d,i);all.Add(pair);if(d<=IdleTimeRules.StopBreakPreviewMetres)near.Add(pair);}
  // No stop inside the radius: still show the two nearest so the preview never goes blank.
  if(near.Count==0)near=all;
  near.Sort((a,b)=>a.Key.CompareTo(b.Key));
  var parts=new System.Collections.Generic.List<string>();for(int k=0;k<near.Count&&k<2;k++)parts.Add(KoreanText.Stop(stops[near[k].Value].name)+" "+Mathf.RoundToInt(near[k].Key)+"m(수요 "+CallBoardRules.DemandLabel(DemandLevelAt(near[k].Value))+")");
  breakPreview=parts.Count==0?"":"하차 후 가까운 승차 지점 · "+string.Join(" · ",parts);return breakPreview;
 }
 // Shown only after the taxi has stood still for a moment with a passenger aboard (red light, queue).
 public string StopBreakSummary(){
  if(!CallBoardSingle||!IdleTimeRules.StopBreakEnabled||trip.State!=TripState.OnTrip||trip.ArrivalLocked||current==null||session.paused||!session.taxi){breakStoppedSince=-1;return null;}
  if(session.taxi.body.velocity.magnitude>.5f){breakStoppedSince=-1;return null;}
  if(breakStoppedSince<0)breakStoppedSince=Time.time;if(Time.time-breakStoppedSince<IdleTimeRules.StopBreakDelay)return null;
  var lines=new System.Collections.Generic.List<string>();
  if(session.requests&&session.requests.HasPendingChoice)lines.Add("손님 요청에 답할 수 있어요 · 요청 창에서 선택");
  if(chainedOffer!=null)lines.Add("다음 호출 예약됨 · "+KoreanText.Stop(stops[chainedOffer.pickup].name));
  else if(ChainWindow&&ChainCandidates().Length>0)lines.Add("M에서 하차 근처의 다음 호출을 미리 잡을 수 있어요");
  if(!DestinationHidden(current)){string preview=NextCallPreview();if(preview.Length>0)lines.Add(preview);}
  return lines.Count==0?null:"정차 중 · "+string.Join("\n",lines);
 }
 // ---- last call before clocking off ----
 bool LastCallWindowOpen {get{return CallBoardSingle&&IdleTimeRules.LastCallEnabled&&session.services&&session.services.HasHomeBase&&session.business.ledger.SecondsLeft>0&&session.business.ledger.SecondsLeft<=IdleTimeRules.LastCallWindow&&!FirstHomeDispatchPending;}}
 static bool IsLastCall(JobOffer o){return o!=null&&(o.variety==IdleTimeRules.LastHome||o.variety==IdleTimeRules.LastFar);}
 public bool LastCallBoard {get{return LastCallWindowOpen&&System.Array.Exists(offers,IsLastCall);}}
 bool LastCallDue(){return LastCallWindowOpen&&!System.Array.Exists(offers,IsLastCall);}
 public float HomeReturnMetres(JobOffer o){var home=session.services?session.services.HomeApproach:null;return o==null||home==null?float.NaN:nav.Distance(stops[o.drop],home);}
 // Two calls: the shortest ride ending closest to the home garage, and the ride ending farthest from it
 // with a higher fare. Going home at once stays available from the call list.
 bool ApplyLastCall(){
  if(!LastCallWindowOpen)return false;
  // Home direction: least total driving before reaching home (pickup + ride + return), ride within the cap.
  // Far: among drops at least farMinReturnMetres from home, the highest fare; otherwise the farthest drop.
  JobOffer near=null,far=null,farthest=null;float bestNear=float.PositiveInfinity,bestFare=-1,bestBack=-1;
  foreach(var c in OrdinaryCandidates()){if(c==null)continue;float back=HomeReturnMetres(c);if(!Finite(back))continue;
   float total=CallPickupMetres(c)+c.distance+back;
   if(c.distance<=IdleTimeRules.LastCallHomeMaxRide&&total<bestNear){near=c;bestNear=total;}
   if(back>=IdleTimeRules.LastCallFarMinReturn&&c.fare>bestFare){far=c;bestFare=c.fare;}
   if(back>bestBack){farthest=c;bestBack=back;}}
  if(far==null)far=farthest;
  if(near==null&&far==null)return false;if(far==near)far=null;
  if(near!=null)near.variety=IdleTimeRules.LastHome;
  if(far!=null){far.variety=IdleTimeRules.LastFar;far.fare=Mathf.CeilToInt(far.fare*IdleTimeRules.LastCallFarFare/100f)*100;}
  for(int i=0;i<offers.Length;i++)offers[i]=null;int slot=0;if(near!=null)offers[slot++]=near;if(far!=null)offers[slot]=far;
  return true;
 }
 public bool GoHomeNow(){return session.services&&session.services.GuideAfterTrip(2);}
}

// Out-of-town call, method A (design 08 ch.5.6 item 3, decided 2026-11-24, Eungye single): the passenger is
// taken to an edge stop of the map; there a 3 s blackout passes the out-of-town leg (there and back empty) on
// the shift clock, fuel and fatigue in one step. The fare is paid for the shift time it uses, not the distance:
// (ride + out-of-town minutes) x usual income per minute x multiplier. Once a day, only when the shift is long
// enough that the leg uses at most 35% of it (20-minute shifts and longer at the default 6 minutes).
public static class OutOfTownRules {
 static float F(string id,double fallback){return (float)ContentTuning.Number(id,fallback);}
 static int I(string id,int fallback){return ContentTuning.Int(id,fallback);}
 public static bool Enabled {get{return I("offer.outOfTown.enabled",1)!=0;}}
 public static int DailyMax {get{return Mathf.Max(0,I("offer.outOfTown.dailyMax",1));}}
 public static int Minutes {get{return Mathf.Clamp(I("offer.outOfTown.minutes",6),1,60);}}
 public static float MaxShiftShare {get{return Mathf.Clamp(F("offer.outOfTown.maxShiftShare",.35),.05f,1);}}
 public static float FareMultiplier {get{return Mathf.Clamp(F("offer.outOfTown.fareMultiplier",1.15),.5f,5);}}
 // Usual income per minute of shift. Measured from the one Eungye day in the play-test save (11,700 won in
 // 231.6 s = about 3,030 won/min, 2026-11-24); a small sample, so it is a setting.
 public static int PerMinuteWon {get{return Mathf.Clamp(I("offer.outOfTown.perMinuteWon",3000),100,100000);}}
 public static float MinSecondsMargin {get{return Mathf.Max(0,F("offer.outOfTown.minSecondsMargin",90));}}
 public static float FadeSeconds {get{return Mathf.Clamp(F("offer.outOfTown.fadeSeconds",3),.5f,10);}}
 public static int BoardChance {get{return Mathf.Clamp(I("offer.outOfTown.boardChancePercent",35),0,100);}}
 public static int EdgeStop {get{return Mathf.Max(0,I("offer.outOfTown.edgeStop",1));}}
 public static float Kmh {get{return Mathf.Clamp(F("offer.outOfTown.kmh",50),10,120);}}
 public static string Direction="북쪽";
 public static string Label {get{return "시외("+Direction+")";}}
 public static float LegSeconds {get{return Minutes*60f;}}
 public static float LegMetres {get{return LegSeconds*Kmh/3.6f;}}
 public static bool ShiftAllows(float daySeconds){return Enabled&&LegSeconds<=daySeconds*MaxShiftShare;}
 public static int Fare(float rideSeconds){double minutes=Mathf.Max(0,rideSeconds)/60.0+Minutes;return (int)System.Math.Ceiling(minutes*PerMinuteWon*FareMultiplier/100.0)*100;}
 // Enough shift left for pickup, ride, the leg and a margin.
 public static bool TimeAllows(double secondsLeft,float pickupAndRide){return secondsLeft>=pickupAndRide+LegSeconds+MinSecondsMargin;}
 public static string DirectionOf(Vector3 stop,float width,float depth){var d=new Vector2(stop.x-width*.5f,stop.z-depth*.5f);return Mathf.Abs(d.x)>Mathf.Abs(d.y)?(d.x>0?"동쪽":"서쪽"):(d.y>0?"북쪽":"남쪽");}
}

public partial class PassengerJobs {
 int outOfTownDay=-1,outOfTownDone;
 public int OutOfTownDoneToday {get{return session.business.ledger.Day==outOfTownDay?outOfTownDone:0;}}
 public void CountOutOfTown(){int day=session.business.ledger.Day;if(day!=outOfTownDay){outOfTownDay=day;outOfTownDone=0;}outOfTownDone++;}
 int EdgeStopIndex {get{int i=OutOfTownRules.EdgeStop;return i<stops.Length?i:-1;}}
 // Why no out-of-town call is offered now; "" = may be offered.
 public string OutOfTownBlocked(){
  if(!CallBoardSingle)return "싱글 영업 아님";var l=session.business.ledger;
  if(!OutOfTownRules.ShiftAllows(l.Settings.daySeconds))return "영업 시간이 짧음";
  if(OutOfTownDoneToday>=OutOfTownRules.DailyMax)return "하루 한도";
  if(EdgeStopIndex<0)return "가장자리 지점 없음";
  if(LastCallWindowOpen||FirstHomeDispatchPending)return "마감 직전·첫 출차";
  return "";
 }
 bool OutOfTownDue(){return OutOfTownBlocked()=="";}
 // Turns an ordinary call into the out-of-town call: same pickup, the edge stop as drop, fare by shift time.
 public string OutOfTownReject="";
 public bool MakeOutOfTown(JobOffer o){
  int edge=EdgeStopIndex;if(o==null||edge<0||o.pickup==edge){OutOfTownReject="같은 지점";return false;}
  float distance=nav.Distance(stops[o.pickup],stops[edge]);if(!Finite(distance)||distance<200){OutOfTownReject="거리 "+distance;return false;}
  float ride=distance/8f;var l=session.business.ledger;float pickup=CallPickupMetres(o);if(!Finite(pickup))pickup=0;float before=pickup/Mathf.Max(.5f,CallBoardRules.PickupMetresPerSecond)+ride+o.waitSeconds;
  if(!OutOfTownRules.TimeAllows(l.SecondsLeft,before)){OutOfTownReject="시간 left="+l.SecondsLeft.ToString("0")+" need="+(before+OutOfTownRules.LegSeconds+OutOfTownRules.MinSecondsMargin).ToString("0");return false;}
  // Fuel for the ride and the whole leg, with 5 points to spare.
  if(l.Account!=null&&l.Account.Fuel<l.Account.FuelRequired(ride+OutOfTownRules.LegSeconds,distance+OutOfTownRules.LegMetres)+5){OutOfTownReject="연료 "+l.Account.Fuel.ToString("0.0")+" need "+l.Account.FuelRequired(ride+OutOfTownRules.LegSeconds,distance+OutOfTownRules.LegMetres).ToString("0.0");return false;}
  OutOfTownRules.Direction=OutOfTownRules.DirectionOf(stops[edge].V(),session.world.map.width,session.world.map.depth);
  o.drop=edge;Stamp(o);o.distance=distance;o.safeRouteSeconds=ride;o.seconds=Mathf.Ceil((ride+45+o.waitSeconds)*1.35f);
  o.variety=CallBoardRules.OutOfTown;o.urgency=0;o.mission=MissionRules.None;o.missionBonus=0;o.fare=OutOfTownRules.Fare(ride);
  return true;
 }
 public string OutOfTownCardLine(JobOffer o){
  float total=CallEtaSeconds(o)+OutOfTownRules.LegSeconds;float left=(float)session.business.ledger.SecondsLeft-total;
  return "시외 구간 "+OutOfTownRules.Minutes+"분(빈 차 복귀 포함) · 끝나면 남는 영업 약 "+Mathf.Max(0,Mathf.FloorToInt(left/60f))+"분";
 }
}

// Emergency passenger (design 08 ch.5.3, approved 2026-11-25, Eungye single): rarely (at most once a day) an
// ordinary call becomes "병원으로 빨리요". The nearest emergency room is real (신천연합병원 응급실, from
// Resources/eungye-care-points.json) and lies just west of the map: the passenger is driven to the stop nearest
// the hospital, then a short blackout covers the rest of the way and the empty return. The deadline is tight;
// law and accident rules are unchanged (no exemption for hurrying).
public static class EmergencyRules {
 static float F(string id,double fallback){return (float)ContentTuning.Number(id,fallback);}
 static int I(string id,int fallback){return ContentTuning.Int(id,fallback);}
 public static bool Enabled {get{return I("emergency.enabled",1)!=0;}}
 public static int DailyMax {get{return Mathf.Max(0,I("emergency.dailyMax",1));}}
 public static int BoardChance {get{return Mathf.Clamp(I("emergency.boardChancePercent",12),0,100);}}
 public static float DeadlineFactor {get{return Mathf.Clamp(F("emergency.deadlineFactor",1.15),.8f,3);}}
 public static float FareMultiplier {get{return Mathf.Clamp(F("emergency.fareMultiplier",1.3),.5f,5);}}
 public static float LegKmh {get{return Mathf.Clamp(F("emergency.legKmh",40),10,100);}}
 public static string HospitalName {get{var h=LostItemRules.Hospital;return h!=null?h.name:"병원";}}
 // Remaining way from the drop stop to the hospital (straight line x1.3) and back, at the leg speed.
 public static float LegSeconds(float straightMetres){return Mathf.Max(30,straightMetres*1.3f*2/(LegKmh/3.6f));}
}

public partial class PassengerJobs {
 int emergencyDay=-1,emergencyDone;
 public int EmergencyDoneToday {get{return session.business.ledger.Day==emergencyDay?emergencyDone:0;}}
 public void CountEmergency(){int day=session.business.ledger.Day;if(day!=emergencyDay){emergencyDay=day;emergencyDone=0;}emergencyDone++;}
 // The stop nearest the hospital.
 public int HospitalStop {get{var h=LostItemRules.Hospital;if(h==null)return -1;int best=-1;float bd=float.MaxValue;for(int i=0;i<stops.Length;i++){if(ShopOnly(i))continue;var d=stops[i].Kerb-h.V();d.y=0;if(d.sqrMagnitude<bd){bd=d.sqrMagnitude;best=i;}}return best;}}
 public float HospitalLegMetres {get{var h=LostItemRules.Hospital;int s=HospitalStop;if(h==null||s<0)return 0;var d=stops[s].Kerb-h.V();d.y=0;return d.magnitude;}}
 public string EmergencyBlocked(){if(!CallBoardSingle)return "싱글 영업 아님";if(!EmergencyRules.Enabled)return "꺼짐";if(EmergencyDoneToday>=EmergencyRules.DailyMax)return "하루 한도";if(HospitalStop<0)return "병원 없음";if(LastCallWindowOpen||FirstHomeDispatchPending)return "마감 직전·첫 출차";return "";}
 public bool MakeEmergency(JobOffer o){
  int drop=HospitalStop;if(o==null||drop<0||o.pickup==drop)return false;
  float distance=nav.Distance(stops[o.pickup],stops[drop]);if(!Finite(distance)||distance<300)return false;
  float ride=distance/8f;float leg=EmergencyRules.LegSeconds(HospitalLegMetres);var l=session.business.ledger;
  float pickup=CallPickupMetres(o);if(!Finite(pickup))pickup=0;if(l.SecondsLeft<pickup/Mathf.Max(.5f,CallBoardRules.PickupMetresPerSecond)+ride+leg+60)return false;
  o.drop=drop;Stamp(o);o.distance=distance;o.safeRouteSeconds=ride;o.seconds=Mathf.Ceil((ride+30+o.waitSeconds)*EmergencyRules.DeadlineFactor);
  o.variety=CallBoardRules.Emergency;o.urgency=2;o.mission=MissionRules.None;o.missionBonus=0;
  o.fare=(int)System.Math.Ceiling((ride+leg*.5f)/60f*OutOfTownRules.PerMinuteWon*EmergencyRules.FareMultiplier/100.0)*100;
  return true;
 }
 public string EmergencyCardLine(JobOffer o){return EmergencyRules.HospitalName+"까지 지도 밖 약 "+Mathf.RoundToInt(HospitalLegMetres/100f)*100+"m · 서둘러도 법규 판정은 그대로";}
}
