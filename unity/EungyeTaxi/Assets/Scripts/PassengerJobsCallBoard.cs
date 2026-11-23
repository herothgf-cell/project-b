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
  string kind=CallBoardRules.Label(o.variety);
  string facts="픽업 "+Mathf.RoundToInt(CallPickupMetres(o))+"m · 약 "+RoadNavigation.FormatEta(CallEtaSeconds(o))+(DestinationHidden(o)?"":" · 하차지 수요 "+CallBoardRules.DemandLabel(DropDemandLevel(o)));
  return head+(kind.Length>0?"["+kind+"]\n":"")+KoreanText.Stop(stops[o.pickup].name)+"\n→ "+drop+"\n"+fare+" · 기한 "+RoadNavigation.FormatEta(o.seconds)+"\n"+facts;
 }
 // Called once per fresh board. Only freshly made ordinary calls change; scenario, reunion and the first
 // home dispatch keep their own rules. Three of the four characters rotate with the board seed.
 void ApplyOfferVariety(){
  if(!CallBoardSingle||!CallBoardRules.VarietyEnabled||FirstHomeDispatchPending)return;
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
