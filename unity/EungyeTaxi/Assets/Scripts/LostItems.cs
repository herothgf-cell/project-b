using System;
using UnityEngine;

// Lost items (design 08 ch.5.3, approved 2026-11-25, Eungye single). After a paid drop-off an item may stay on the
// back seat (umbrella, shopping bag, wallet). With the taxi empty the driver chooses:
//  1 return it: the owner waits at the drop stop for a while; arriving there pays a thank-you (its own ledger line,
//    counted with tips);
//  2 hand it to the police: 은행파출소 (real place, Resources/eungye-care-points.json); a small thank-you;
//  3 leave it: the next passenger minds the item (-3 comfort, once); it is handed in at the end of the day.
public static class LostItemRules {
 static float F(string id,double fallback){return (float)ContentTuning.Number(id,fallback);}
 static int I(string id,int fallback){return ContentTuning.Int(id,fallback);}
 public static bool Enabled {get{return I("lostItem.enabled",1)!=0;}}
 public static int ChancePercent {get{return Mathf.Clamp(I("lostItem.chancePercent",12),0,100);}}
 public static float OwnerWaitSeconds {get{return Mathf.Clamp(F("lostItem.ownerWaitSeconds",240),30,1800);}}
 public static int PoliceReward {get{return Mathf.Max(0,I("lostItem.policeReward",300));}}
 public static int LeavePenalty {get{return Mathf.Clamp(I("lostItem.leavePenalty",3),0,20);}}
 public static readonly string[] Names={"우산","쇼핑백","지갑"};
 public static int Reward(int item){return item==2?Mathf.Max(0,I("lostItem.reward.wallet",3000)):item==1?Mathf.Max(0,I("lostItem.reward.bag",1000)):Mathf.Max(0,I("lostItem.reward.umbrella",500));}
 // Stable rolls per trip: whether an item stays (drunk or sleepy passengers twice as often) and which.
 public static bool Left(string trip,bool careless){return GoodDrivingRules.Roll(trip+":lost")<ChancePercent*(careless?2:1);}
 public static int Which(string trip){return GoodDrivingRules.Roll(trip+":lost:item")%3;}
 [Serializable] public sealed class Place {public string name,address,phone,evidence;public float x,z;public Vector3 V(){return new Vector3(x,0,z);}}
 [Serializable] sealed class File {public int schema;public string mapId,note;public Place police,hospital;}
 static File data;static bool loaded;
 static File Data {get{if(!loaded){loaded=true;var a=Resources.Load<TextAsset>("eungye-care-points");if(a)data=JsonUtility.FromJson<File>(a.text);}return data;}}
 public static Place Police {get{return Data!=null?Data.police:null;}}
 public static Place Hospital {get{return Data!=null?Data.hospital:null;}}
}

public sealed class LostItems : MonoBehaviour {
 public DriveSession session;
 public const int None=0,Pending=1,Returning=2,ToPolice=3,Left=4;
 public int State,Item;public string Trip="",OwnerStop="";int ownerIndex=-1;float ownerUntil;int seenResults;int day=-1;bool penalised;
 public int Returned,Handed,LeftCount;public string LastOutcome="";
 bool Single {get{return session&&session.traffic&&session.traffic.EungyeSingle&&!session.simulationMatch&&!session.IsSimulationBot&&!PvpNativeLan.IsReplica(session)&&session.business&&session.business.enabledCampaign;}}
 // Not carrying and not on the way to a pickup (a finished trip may still show its state until the next call).
 bool Empty {get{var j=session.jobs;return j&&j.trip.State!=TripState.OnTrip&&j.trip.State!=TripState.ToPickup;}}
 void Card(string text){var n=session.GetComponent<DriveNotifications>();if(n)n.Card(text);}
 public Vector3 PolicePoint {get{var p=LostItemRules.Police;return p==null?Vector3.zero:p.V();}}
 void Update(){
  if(!Single||!LostItemRules.Enabled||session.business.ledger==null)return;var b=session.business;var j=session.jobs;
  if(b.ledger.Day!=day){if(day>=0&&State!=None){LastOutcome="하루 마감 · 파출소 처리";Reset();}day=b.ledger.Day;seenResults=b.passengers.Count;}
  if(b.passengers.Count<seenResults)seenResults=b.passengers.Count;
  if(b.passengers.Count>seenResults){var r=b.passengers[b.passengers.Count-1];seenResults=b.passengers.Count;if(State==None&&r!=null&&r.paid>0&&!string.IsNullOrEmpty(r.tripId)&&LostItemRules.Left(r.tripId,r.drunk||r.wakeDelayed))Begin(r.tripId,r.dropStopId,LostItemRules.Which(r.tripId));}
  if(State==Pending&&Empty){int pick=NumericChoiceInput.Read(3,i=>true);if(pick>=0)Choose(pick+1);}
  if(State==Returning){
   if(Time.time>ownerUntil){State=ToPolice;Card("분실물 · 주인이 떠났어요 · 은행파출소에 맡겨 주세요");GuideToPolice();}
   else if(ownerIndex>=0&&Empty&&session.taxi.Speed<1&&Vector3.Distance(session.taxi.transform.position,j.stops[ownerIndex].Kerb)<9){Pay("lost:"+Trip,LostItemRules.Reward(Item));Returned++;LastOutcome="돌려줌";Card("분실물 · "+LostItemRules.Names[Item]+"을 돌려드렸어요 · “정말 고마워요!” · +"+LostItemRules.Reward(Item).ToString("N0")+"원");Reset();}
  }
  if(State==ToPolice&&Empty&&session.taxi.Speed<1&&LostItemRules.Police!=null&&(Vector3.Distance(Flat(session.taxi.transform.position),Flat(PoliceRoad))<12||Vector3.Distance(Flat(session.taxi.transform.position),PolicePoint)<20)){Pay("lost-police:"+Trip,LostItemRules.PoliceReward);Handed++;LastOutcome="파출소";Card("분실물 · "+LostItemRules.Police.name+"에 맡겼어요 · +"+LostItemRules.PoliceReward.ToString("N0")+"원");Reset();}
  // Left on the seat: the next passenger minds it, once.
  if(State==Left&&!penalised&&j.trip.State==TripState.OnTrip&&j.current!=null){penalised=true;if(session.manners)session.manners.Deduct("lostitem",LostItemRules.LeavePenalty,"뒷좌석 물건 · 손님이 불편해해요",0);}
 }
 static Vector3 Flat(Vector3 v){v.y=0;return v;}
 public void Begin(string trip,string dropStopId,int item){
  State=Pending;Trip=trip;Item=Mathf.Clamp(item,0,2);OwnerStop=dropStopId;ownerIndex=-1;penalised=false;
  var j=session.jobs;if(j.Catalog!=null)for(int i=0;i<j.stops.Length;i++){var e=j.Catalog.Entry(i);if(e!=null&&e.StopId==dropStopId){ownerIndex=i;break;}}
  Card("분실물 · 뒷좌석에 "+LostItemRules.Names[Item]+"이 있어요");Debug.Log("LOST_ITEM "+LostItemRules.Names[Item]+" trip="+trip+" owner stop="+ownerIndex);
 }
 public void Choose(int choice){
  if(State!=Pending)return;var j=session.jobs;
  if(choice==1&&ownerIndex>=0){State=Returning;ownerUntil=Time.time+LostItemRules.OwnerWaitSeconds;var s=j.stops[ownerIndex];if(j.nav&&s.laneEdge>=0)j.nav.SetHdDestination(s.laneEdge,s.laneAt);Card("분실물 · 주인이 "+KoreanText.Stop(s.name)+"에서 "+Mathf.RoundToInt(LostItemRules.OwnerWaitSeconds/60)+"분 기다려요");}
  else if(choice==2||choice==1){State=ToPolice;GuideToPolice();Card("분실물 · "+(LostItemRules.Police!=null?LostItemRules.Police.name:"파출소")+"에 맡기러 가요");}
  else{State=Left;LeftCount++;LastOutcome="그냥 둠";Card("분실물 · 그냥 두면 다음 손님이 불편해해요 · 마감 때 파출소로 보냄");}
 }
 // The road point nearest the police box: where the taxi stops to hand the item in.
 public Vector3 PoliceRoad {get{var j=session.jobs;if(j==null||j.nav==null||j.nav.hd==null||LostItemRules.Police==null)return PolicePoint;var g=j.nav.hd;int best=-1;float bd=float.MaxValue,bat=0;for(int i=0;i<g.edges.Length;i++){if(g.edges[i].change!=0||g.edges[i].turn!=0||g.edges[i].TraversalDisabled)continue;float err;float at=g.paths[i].Project(PolicePoint,out err);if(err<bd){bd=err;best=i;bat=at;}}return best<0?PolicePoint:g.paths[best].Position(bat);}}
 void GuideToPolice(){var j=session.jobs;if(!j.nav||j.nav.hd==null||LostItemRules.Police==null)return;var road=PoliceRoad;float at;int edge=j.nav.hd.Nearest(road,Vector3.zero,out at);if(edge>=0)j.nav.SetHdDestination(edge,at);}
 void Pay(string id,int amount){if(amount>0)session.business.RecordReward(id,amount);session.business.SaveCheckpoint();}
 void Reset(){State=None;Trip="";ownerIndex=-1;penalised=false;}
 void OnGUI(){
  if(State!=Pending||!Empty||session.IsSimulationBot||session.paused)return;
  var options=new PassengerDialogueUI.Option[3];
  options[0]=new PassengerDialogueUI.Option("1","돌려주러 간다",ownerIndex>=0?KoreanText.Stop(session.jobs.stops[ownerIndex].name)+"에서 "+Mathf.RoundToInt(LostItemRules.OwnerWaitSeconds/60)+"분 기다림 · +"+LostItemRules.Reward(Item).ToString("N0")+"원":"주인 위치 모름");
  options[1]=new PassengerDialogueUI.Option("2",(LostItemRules.Police!=null?LostItemRules.Police.name:"파출소")+"에 맡긴다","+"+LostItemRules.PoliceReward.ToString("N0")+"원");
  options[2]=new PassengerDialogueUI.Option("3","그냥 둔다","다음 손님 만족 −"+LostItemRules.LeavePenalty);
  int selected=PassengerDialogueUI.Draw("분실물","뒷좌석에 "+LostItemRules.Names[Item]+"이 남아 있어요.",options,"",PassengerDialogueUI.PortraitIdentity(session));
  if(selected>=0&&NumericChoiceInput.TryConsume(Time.frameCount))Choose(selected+1);
 }
}
