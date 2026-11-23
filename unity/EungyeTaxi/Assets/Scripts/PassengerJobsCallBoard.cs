using UnityEngine;

// 08 chapter 3 (2026-11-23, Eungye single only): judgement material on each call card and a call board that
// mixes calls of different character. PvP boards and shared physics are untouched. Every number is a content
// setting (offer.*); new JobOffer fields default to "ordinary", so older saves load unchanged.
public static class CallBoardRules {
 public const int Ordinary=0,Near=1,Rush=2,Luggage=3,Hidden=4;
 static float F(string id,double fallback){return (float)ContentTuning.Number(id,fallback);}
 public static bool VarietyEnabled {get{return ContentTuning.Int("offer.variety.enabled",1)!=0;}}
 public static float NearMaxPickup {get{return F("offer.near.maxPickupMetres",450);}}
 public static float NearMaxRide {get{return F("offer.near.maxRideMetres",1000);}}
 public static float RushDeadline {get{return F("offer.rush.deadlineMultiplier",.75);}}
 public static float RushFare {get{return F("offer.rush.fareMultiplier",1.4);}}
 public static int LuggageFareBonus {get{return ContentTuning.Int("offer.luggage.fareBonus",800);}}
 public static float PickupMetresPerSecond {get{return F("offer.eta.pickupMetresPerSecond",4);}}
 public static string Label(int variety){return variety==Near?"근거리 · 무난":variety==Rush?"급한 손님 · 기한 짧음 · 고액":variety==Luggage?"큰 짐 · 트렁크 필요":variety==Hidden?"길가 손님 · 목적지는 타면 알려 줌":"";}
 public static string DemandLabel(int level){return level>=2?"높음":level==1?"보통":"낮음";}
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
 public string CallCardCaption(int index,int drivingScore){
  var o=offers[index];
  string head=(index+1)+(o.reunionKind!=0?"  실습 끝난 학생 · ":"  ");
  string drop=DestinationHidden(o)?"목적지 비공개 (탑승 후 안내)":KoreanText.Stop(stops[o.drop].name);
  string fare="예상 운임 "+PassengerReward.AdjustedFare(o.fare,drivingScore).ToString("N0")+"원";
  if(!CallBoardSingle)return head+KoreanText.Stop(stops[o.pickup].name)+"\n→ "+drop+"\n"+fare;
  string kind=IsLastCall(o)?IdleTimeRules.Label(o.variety):CallBoardRules.Label(o.variety);
  string home=IsLastCall(o)&&Finite(HomeReturnMetres(o))?"\n하차 후 덕성프라자까지 약 "+Mathf.RoundToInt(HomeReturnMetres(o))+"m":"";
  string facts="픽업 "+Mathf.RoundToInt(CallPickupMetres(o))+"m · 약 "+RoadNavigation.FormatEta(CallEtaSeconds(o))+(DestinationHidden(o)?"":" · 하차지 수요 "+CallBoardRules.DemandLabel(DropDemandLevel(o)));
  return head+(kind.Length>0?"["+kind+"]\n":"")+KoreanText.Stop(stops[o.pickup].name)+"\n→ "+drop+"\n"+fare+" · 기한 "+RoadNavigation.FormatEta(o.seconds)+"\n"+facts+home+MissionCardLine(o);
 }
 // Called once per fresh board. Only freshly made ordinary calls change; scenario, reunion and the first
 // home dispatch keep their own rules. Three of the four characters rotate with the board seed.
 void ApplyOfferVariety(){
  if(!CallBoardSingle||!CallBoardRules.VarietyEnabled||FirstHomeDispatchPending||System.Array.Exists(offers,IsLastCall))return;
  var pool=new System.Collections.Generic.List<JobOffer>();
  foreach(var o in offers)if(o!=null&&o.variety==CallBoardRules.Ordinary&&string.IsNullOrEmpty(o.scenarioId)&&o.reunionKind==0)pool.Add(o);
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
  for(int i=0;i<stops.Length;i++){if(i==current.drop)continue;float d=nav.Distance(stops[current.drop],stops[i]);if(Finite(d)&&d<=IdleTimeRules.StopBreakPreviewMetres)near.Add(new System.Collections.Generic.KeyValuePair<float,int>(d,i));}
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
