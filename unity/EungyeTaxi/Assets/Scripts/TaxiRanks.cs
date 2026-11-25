using System;
using System.Collections.Generic;
using UnityEngine;

// Taxi ranks (design 08 ch.5.1, decided 2026-11-25, Eungye single). Three game-only ranks on existing passenger
// stops (Resources/eungye-taxi-ranks.json). NPC taxis queue in the stop bay (0..3); a passenger arrives at an
// interval and leaves with the taxi at the head of the queue. The player joins by stopping, empty, behind the last
// taxi; at the head of the queue the next passenger waits for the player (F, as for any roadside passenger) and
// goes far (one of the three farthest stops). Leaving the bay loses the place. App calls are not affected.
public static class TaxiRankRules {
 [Serializable] public sealed class Rank {public string id,stopId,name,area,evidence;public float demand=1;}
 [Serializable] sealed class File {public int schema;public string mapId,note;public Rank[] ranks;}
 static Rank[] ranks;static bool loaded;
 static float F(string id,double fallback){return (float)ContentTuning.Number(id,fallback);}
 static int I(string id,int fallback){return ContentTuning.Int(id,fallback);}
 public static bool Enabled {get{return I("rank.enabled",1)!=0;}}
 public static float ArrivalSeconds {get{return Mathf.Clamp(F("rank.arrivalSeconds",40),5,600);}}
 public static int QueueMax {get{return Mathf.Clamp(I("rank.queueMax",3),0,6);}}
 public static float NpcArrivalSeconds {get{return Mathf.Clamp(F("rank.npcArrivalSeconds",55),5,600);}}
 public static int FarChoices {get{return Mathf.Clamp(I("rank.farChoices",3),1,12);}}
 public const float Slot=6.5f;
 public static Rank[] Ranks {get{if(!loaded){loaded=true;var a=Resources.Load<TextAsset>("eungye-taxi-ranks");Configure(a?a.text:null);}return ranks??new Rank[0];}}
 public static void Configure(string json){loaded=true;ranks=null;if(string.IsNullOrEmpty(json))return;var f=JsonUtility.FromJson<File>(json);if(f!=null&&f.schema==1&&f.ranks!=null)ranks=f.ranks;}
 // Seconds until the next passenger at a rank: the base interval over the rank's demand, +-30% by a stable roll.
 public static float NextArrival(float demand,int roll){return ArrivalSeconds/Mathf.Max(.2f,demand)*(.7f+.6f*(Mathf.Abs(roll)%1000)/1000f);}
 // The player's place: taxis ahead of the player's bay slot. Slot k lies k*Slot behind the stop point.
 public static int SlotOf(float behind){return Mathf.Max(0,Mathf.RoundToInt(behind/Slot));}
}

public sealed class TaxiRanks : MonoBehaviour {
 public DriveSession session;
 public sealed class Rank {public TaxiRankRules.Rank data;public int stop;public Vector3 kerb,dir;public readonly List<GameObject> queue=new List<GameObject>();public float nextPassenger,nextNpc;public bool passengerWaiting,playerHead;public int playerAhead=-1;public int departures,served;}
 public readonly List<Rank> ranks=new List<Rank>();
 int roll,day=-1;public string Status="";public Rank Near;
 public void Initialize(DriveSession owner){
  session=owner;ranks.Clear();if(!Single||!TaxiRankRules.Enabled)return;var j=session.jobs;if(j==null||j.Catalog==null)return;
  foreach(var r in TaxiRankRules.Ranks){int index=-1;for(int i=0;i<j.stops.Length;i++){var e=j.Catalog.Entry(i);if(e!=null&&e.StopId==r.stopId){index=i;break;}}if(index<0)continue;
   var s=j.stops[index];var d=s.Direction;d.y=0;if(d.sqrMagnitude<.01f)continue;var rank=new Rank{data=r,stop=index,kerb=s.Kerb,dir=d.normalized};rank.nextPassenger=Time.time+TaxiRankRules.NextArrival(r.demand,++roll*7919);rank.nextNpc=Time.time+5;ranks.Add(rank);
   for(int k=0;k<Mathf.Min(2,TaxiRankRules.QueueMax);k++)AddNpc(rank);}
  j.roadsideOverride=Override;
  Debug.Log("TAXI_RANKS ready="+ranks.Count);
 }
 bool Single {get{return session&&session.traffic&&session.traffic.EungyeSingle&&!session.simulationMatch&&!session.IsSimulationBot&&!PvpNativeLan.IsReplica(session)&&session.world&&session.world.map.hybrid;}}
 Rank RankAt(int stop){foreach(var r in ranks)if(r.stop==stop)return r;return null;}
 public bool IsRankStop(int stop){return RankAt(stop)!=null;}
 // Roadside waiting at a rank stop is decided here, not by the general roll.
 bool? Override(int stop){var r=RankAt(stop);if(r==null)return null;return r.passengerWaiting&&r.queue.Count==0;}
 Vector3 SlotPosition(Rank r,int k){return r.kerb-r.dir*TaxiRankRules.Slot*k;}
 void AddNpc(Rank r){
  if(r.queue.Count>=TaxiRankRules.QueueMax)return;int k=r.queue.Count;var p=SlotPosition(r,k);
  // Never on top of the player's taxi.
  if(session.taxi&&Vector3.Distance(session.taxi.transform.position,p)<4)return;
  var car=NpcTaxis.BuildModel("승강장 대기 택시 · "+r.data.name,transform);car.transform.SetPositionAndRotation(p,Quaternion.LookRotation(r.dir));r.queue.Add(car);
 }
 void Shift(Rank r){if(r.queue.Count==0)return;var front=r.queue[0];r.queue.RemoveAt(0);if(front){front.SetActive(false);Destroy(front);}for(int k=0;k<r.queue.Count;k++)if(r.queue[k])r.queue[k].transform.position=SlotPosition(r,k);r.departures++;}
 bool Empty {get{var j=session.jobs;return j&&j.current==null&&j.trip.State==TripState.Idle;}}
 void Update(){
  if(ranks.Count==0||!session.taxi||session.paused||Time.timeScale<=0)return;
  if(session.business&&session.business.ledger!=null&&session.business.ledger.Day!=day){if(day>=0)ResetForDay();day=session.business.ledger.Day;}
  var j=session.jobs;var taxi=session.taxi.transform.position;Near=null;Status="";
  foreach(var r in ranks){
   var rel=taxi-r.kerb;rel.y=0;float along=-Vector3.Dot(rel,r.dir),lateral=Mathf.Abs(Vector3.Dot(rel,TrafficSystem.Right(r.dir)));
   bool inBay=lateral<2.2f&&along>-3&&along<TaxiRankRules.Slot*(TaxiRankRules.QueueMax+1)&&session.taxi.Speed<1f&&Empty&&Vector3.Dot(session.taxi.transform.forward,r.dir)>.7f;
   // The player keeps a place while in the bay; ahead = taxis in slots in front of the player's slot.
   if(inBay){int slot=TaxiRankRules.SlotOf(along);r.playerAhead=Mathf.Min(slot,r.queue.Count);
    // Taxis behind the player are not ahead: trim the queue to the player's slot.
    while(r.queue.Count>r.playerAhead){var last=r.queue[r.queue.Count-1];r.queue.RemoveAt(r.queue.Count-1);if(last){last.SetActive(false);Destroy(last);}}}
   else if(r.playerAhead>=0&&(Mathf.Abs(along)>TaxiRankRules.Slot*(TaxiRankRules.QueueMax+2)||lateral>6||!Empty))r.playerAhead=-1;
   bool playerQueued=r.playerAhead>=0;
   if(Time.time>=r.nextPassenger&&!r.passengerWaiting){r.nextPassenger=Time.time+TaxiRankRules.NextArrival(r.data.demand,++roll*7919);
    if(r.queue.Count>0){Shift(r);}            // the head NPC taxi takes the passenger
    else{r.passengerWaiting=true;r.served++;j.SetForcedRoadsideDrop(r.stop,FarDrop(r.stop));j.SetRoadsideWaiting(r.stop,true);if(playerQueued)Card("승강장 · 손님이 왔어요 · 정차 칸에서 F");}}
   // Boarded by the player, or gone: the rank is free for the next passenger.
   if(r.passengerWaiting&&j.current!=null&&j.current.pickup==r.stop&&j.trip.State==TripState.OnTrip){r.passengerWaiting=false;r.playerAhead=-1;j.SetRoadsideWaiting(r.stop,false);Card("승강장 · 먼 손님을 태웠어요");}
   if(r.passengerWaiting&&!j.RoadsideWaitingAt(r.stop)){r.passengerWaiting=false;j.SetForcedRoadsideDrop(r.stop,-1);r.nextPassenger=Mathf.Max(r.nextPassenger,Time.time+5);}
   if(Time.time>=r.nextNpc){r.nextNpc=Time.time+TaxiRankRules.NpcArrivalSeconds*(.7f+.6f*UnityEngine.Random.value);if(!playerQueued&&!r.passengerWaiting)AddNpc(r);}
   // A waiting passenger with nobody in the queue is taken by the next NPC taxi after a while.
   if(r.passengerWaiting&&r.queue.Count==0&&!playerQueued&&Time.time>=r.nextPassenger-TaxiRankRules.ArrivalSeconds*.25f){j.TakenByOtherTaxi(r.stop);j.SetForcedRoadsideDrop(r.stop,-1);r.passengerWaiting=false;}
   if((taxi-r.kerb).sqrMagnitude<80*80){Near=r;Status=r.data.name+"(게임 전용) · "+(playerQueued?(r.playerAhead==0?(r.passengerWaiting?"손님 대기 중 · F로 태우기":"내 차례 · 다음 손님 약 "+Mathf.CeilToInt(Mathf.Max(0,r.nextPassenger-Time.time))+"초"):"앞 택시 "+r.playerAhead+"대"):"줄 "+r.queue.Count+"대 · 빈 차로 줄 끝에 서면 순서를 기다립니다");}
  }
 }
 // One of the farthest stops from the rank (excluding the rank and its opposite kerb).
 int FarDrop(int pickup){
  var j=session.jobs;var list=new List<KeyValuePair<float,int>>();
  for(int i=0;i<j.stops.Length;i++){if(i==pickup||i==j.Catalog.OppositeIndex(pickup))continue;float d=j.nav.Distance(j.stops[pickup],j.stops[i]);if(float.IsNaN(d)||float.IsInfinity(d))continue;list.Add(new KeyValuePair<float,int>(d,i));}
  if(list.Count==0)return -1;list.Sort((a,b)=>b.Key.CompareTo(a.Key));return list[(++roll)%Mathf.Min(TaxiRankRules.FarChoices,list.Count)].Value;
 }
 void Card(string text){var n=session.GetComponent<DriveNotifications>();if(n)n.Card(text);}
 public void ResetForDay(){foreach(var r in ranks){while(r.queue.Count>0)Shift(r);r.passengerWaiting=false;r.playerAhead=-1;r.nextPassenger=Time.time+TaxiRankRules.NextArrival(r.data.demand,++roll*7919);for(int k=0;k<Mathf.Min(2,TaxiRankRules.QueueMax);k++)AddNpc(r);}}
 void OnGUI(){
  if(Near==null||string.IsNullOrEmpty(Status)||session.paused||session.IsSimulationBot)return;
  float s=CockpitUI.Scale;var rect=new Rect(Screen.width*.5f-210*s,Screen.height-290*s,420*s,44*s);
  CasualUI.RoundedFill(rect,new Color(.1f,.12f,.16f,.88f));var style=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontSize=Mathf.RoundToInt(17*s),fontStyle=FontStyle.Bold,wordWrap=true};style.normal.textColor=new Color(1,.86f,.3f);if(KoreanText.Font)style.font=KoreanText.Font;
  GUI.Label(rect,Status,style);
 }
}
