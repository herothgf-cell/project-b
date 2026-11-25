using UnityEngine;

// 08 chapter 3 (2026-11-24, Eungye single): chained dispatch. Near the drop-off the driver may reserve one nearby
// call from the map. The current trip's arrival, fare and receipt are untouched; the reserved call is accepted
// right after the drop-off result clears, so its pickup deadline is computed from the drop-off point.
public static class ChainRules {
 static float F(string id,double fallback){return (float)ContentTuning.Number(id,fallback);}
 public static bool Enabled {get{return ContentTuning.Int("chain.enabled",1)!=0;}}
 public static float PreviewMetres {get{return F("chain.previewMetres",400);}}
 public static float RadiusMetres {get{return F("chain.radiusMetres",600);}}
 public static int MaxOffers {get{return Mathf.Clamp(ContentTuning.Int("chain.maxOffers",2),1,4);}}
}

public partial class PassengerJobs {
 JobOffer chainedOffer;string chainKey="";JobOffer[] chainCache=new JobOffer[0];
 public JobOffer ChainedOffer {get{return chainedOffer;}}
 // Open while the passenger is aboard and the taxi is within previewMetres of the drop-off.
 public bool ChainWindow {get{
  if(!CallBoardSingle||!ChainRules.Enabled||current==null||current.variety==CallBoardRules.OutOfTown||current.variety==CallBoardRules.Emergency||trip.State!=TripState.OnTrip||!session.taxi)return false;
  var drop=EffectiveStop(current.drop);var d=session.taxi.transform.position-drop.Kerb;d.y=0;return d.magnitude<=ChainRules.PreviewMetres;
 }}
 public JobOffer[] ChainCandidates(){
  if(!ChainWindow)return new JobOffer[0];
  string key=session.law.tripId+":"+current.drop+":"+completed;if(key==chainKey)return chainCache;chainKey=key;
  var near=new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<float,int>>();
  for(int p=0;p<stops.Length;p++){if(p==current.drop||StopOccupied(p)||ShopOnly(p)||Time.time<cooldown[p])continue;float d=nav.Distance(stops[current.drop],stops[p]);if(Finite(d)&&d<=ChainRules.RadiusMetres)near.Add(new System.Collections.Generic.KeyValuePair<float,int>(d,p));}
  near.Sort((a,b)=>a.Key.CompareTo(b.Key));
  var list=new System.Collections.Generic.List<JobOffer>();int n=stops.Length;
  foreach(var pair in near){if(list.Count>=ChainRules.MaxOffers)break;int p=pair.Value;
   int drop=(p+3+completed*7)%n;for(int step=0;step<n&&(drop==p||drop==current.drop||ShopOnly(drop));step++)drop=(drop+1)%n;
   var o=Make(p,drop);if(o!=null&&Finite(o.distance))list.Add(o);}
  chainCache=list.ToArray();return chainCache;
 }
 public float ChainPickupMetres(JobOffer o){return o==null||current==null?float.NaN:nav.Distance(stops[current.drop],stops[o.pickup]);}
 public bool ReserveChain(JobOffer o){if(!ChainWindow||o==null||System.Array.IndexOf(chainCache,o)<0)return false;chainedOffer=o;return true;}
 public void CancelChain(){chainedOffer=null;}
 // Called once the drop-off result has cleared and the board was regenerated.
 void StartChained(){
  if(chainedOffer==null)return;var o=chainedOffer;chainedOffer=null;
  if(!CallBoardSingle||trip.State!=TripState.Idle)return;
  offers[0]=o;if(!Accept(0))message="미리 잡은 호출을 시작하지 못했어요 · "+message;
 }
 public string ChainCaption(JobOffer o){
  return KoreanText.Stop(stops[o.pickup].name)+" → "+KoreanText.Stop(stops[o.drop].name)+"\n하차 지점에서 "+Mathf.RoundToInt(ChainPickupMetres(o))+"m · 예상 운임 "+PassengerReward.AdjustedFare(o.fare,session.law.ledger.TotalScore).ToString("N0")+"원";
 }
 public float ChainContentHeight {get{if(!ChainWindow)return 0;int rows=ChainCandidates().Length;return rows==0?0:40+rows*74+(chainedOffer!=null?48:0);}}
}
