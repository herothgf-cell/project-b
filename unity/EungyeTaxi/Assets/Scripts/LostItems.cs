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
 public int OwnerIndex {get{return ownerIndex;}}
 public float OwnerSecondsLeft {get{return Mathf.Max(0,ownerUntil-Time.time);}}
 public Vector3 PolicePoint {get{var p=LostItemRules.Police;return p==null?Vector3.zero:p.V();}}
 void Update(){
  if(!signs&&session.jobs&&session.jobs.nav&&session.jobs.nav.hd!=null)BuildSigns();
  if(!Single||!LostItemRules.Enabled||session.business.ledger==null)return;var b=session.business;var j=session.jobs;
  if(b.ledger.Day!=day){if(day>=0&&State!=None){LastOutcome="하루 마감 · 파출소 처리";Reset();}day=b.ledger.Day;seenResults=b.passengers.Count;}
  if(b.passengers.Count<seenResults)seenResults=b.passengers.Count;
  if(b.passengers.Count>seenResults){var r=b.passengers[b.passengers.Count-1];seenResults=b.passengers.Count;if(State==None&&r!=null&&r.paid>0&&!string.IsNullOrEmpty(r.tripId)&&LostItemRules.Left(r.tripId,r.drunk||r.wakeDelayed))Begin(r.tripId,r.dropStopId,LostItemRules.Which(r.tripId));}
  if(State==Pending&&Empty){int pick=NumericChoiceInput.Read(3,i=>true);if(pick>=0)Choose(pick+1);}
  if(State==Returning){
   if(Time.time>ownerUntil){State=ToPolice;Card("분실물 · 주인이 떠났어요 · 은행파출소에 맡겨 주세요");GuideToPolice();}
   else if(ownerIndex>=0&&Empty&&session.taxi.Speed<1&&Vector3.Distance(session.taxi.transform.position,j.stops[ownerIndex].Kerb)<9){Pay("lost:"+Trip,LostItemRules.Reward(Item));Returned++;LastOutcome="돌려줌";Card("분실물 · "+LostItemRules.Names[Item]+"을 돌려드렸어요 · “정말 고마워요!” · +"+LostItemRules.Reward(Item).ToString("N0")+"원");Reset();}
  }
  // Play feedback round 7: the police box works like the shops. Stopped in front of it with an item aboard, F opens a
  // small desk menu and the driver chooses to hand it in (Enter) or not (Esc); nothing happens on arrival by itself.
  if(AtPolice&&HasItem&&State!=Pending&&!PoliceMenuOpen&&!session.paused&&Input.GetKeyDown(KeyCode.F))OpenPoliceMenu();
  if(PoliceMenuOpen&&(!AtPolice||!HasItem))PoliceMenuOpen=false;
  // Play feedback round 7: an accident (or a ride taken meanwhile) clears the route, which looked like a cancelled
  // errand while it was still on. With the taxi empty again and nothing guided, the errand's route comes back.
  if((State==Returning||State==ToPolice)&&Empty&&j.nav&&!j.nav.HasDestination&&!(session.accident&&session.accident.Locked)&&Time.time>=reguideAt){reguideAt=Time.time+2;Reguide();}
  // Left on the seat: the next passenger minds it, once.
  if(State==Left&&!penalised&&j.trip.State==TripState.OnTrip&&j.current!=null){penalised=true;if(session.manners)session.manners.Deduct("lostitem",LostItemRules.LeavePenalty,"뒷좌석 물건 · 손님이 불편해해요",0);}
 }
 float reguideAt;public int Reguided;
 public bool PoliceMenuOpen;
 public bool HasItem {get{return State==Pending||State==Returning||State==ToPolice||State==Left;}}
 public bool AtPolice {get{if(!Single||LostItemRules.Police==null||!session.taxi||!Empty||session.taxi.Speed>=1)return false;var p=Flat(session.taxi.transform.position);return Vector3.Distance(p,Flat(PoliceRoad))<12||Vector3.Distance(p,PolicePoint)<20;}}
 public void OpenPoliceMenu(){if(AtPolice&&HasItem)PoliceMenuOpen=true;}
 public bool HandIn(){
  if(!AtPolice||!HasItem)return false;PoliceMenuOpen=false;
  Pay("lost-police:"+Trip,LostItemRules.PoliceReward);Handed++;LastOutcome="파출소";Card("분실물 · "+LostItemRules.Police.name+"에 맡겼어요 · +"+LostItemRules.PoliceReward.ToString("N0")+"원");Reset();return true;
 }
 // While the choice dialog (Pending) is open it owns the screen; the desk shows once a choice is made.
 void PoliceGUI(){
  if(!Single||session.paused||session.FullMapOpen||!AtPolice||!HasItem||State==Pending)return;
  var prior=GUI.matrix;float scale=CockpitUI.Scale;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
  try{
   float w=Screen.width/scale,h=Screen.height/scale;
   if(!PoliceMenuOpen){var bar=new Rect(w/2-150,h-320,300,46);CasualUI.RoundedFill(bar,new Color(.10f,.20f,.45f,.94f));if(GUI.Button(bar,GUIContent.none,GUIStyle.none))OpenPoliceMenu();KeyHints.Cap(new Rect(bar.x+12,bar.y+9,28,28),"F",true);GUI.Label(new Rect(bar.x+50,bar.y,bar.width-60,bar.height),LostItemRules.Police.name+" · 분실물 맡기기",DrivingHudArt.Text(17,Color.white,true));return;}
   var r=new Rect(w/2-230,h/2-150,460,300);CasualUI.Panel(r);
   var head=new Rect(r.x,r.y,r.width,58);CasualUI.RoundedFill(head,new Color(.10f,.20f,.45f));HudGlyph.Draw(new Rect(head.x+16,head.y+12,34,34),21,Color.white);GUI.Label(new Rect(head.x+60,head.y,head.width-70,head.height),LostItemRules.Police.name+" · 분실물 신고",DrivingHudArt.Text(20,Color.white,true));
   float y=r.y+74;
   GUI.Label(new Rect(r.x+24,y,r.width-48,30),"맡길 물건 · "+LostItemRules.Names[Item],DrivingHudArt.Text(18,CasualUI.Ink,true));y+=36;
   GUI.Label(new Rect(r.x+24,y,r.width-48,28),"감사 사례 +"+LostItemRules.PoliceReward.ToString("N0")+"원",DrivingHudArt.Text(16,CasualUI.Green,true));y+=32;
   string note=State==Returning?"주인이 아직 기다리는 중이에요. 직접 돌려주면 사례가 더 커요.":"맡기면 파출소가 주인을 찾아 줘요.";
   GUI.Label(new Rect(r.x+24,y,r.width-48,44),note,new GUIStyle(DrivingHudArt.Text(14,CasualUI.Muted,false)){wordWrap=true});
   var style=KoreanText.Button(17);
   if(KeyHints.Button(new Rect(r.x+24,r.yMax-66,(r.width-60)*.6f,48),"맡기기","Enter",style)||KeyHints.Enter())HandIn();
   else if(KeyHints.Button(new Rect(r.x+36+(r.width-60)*.6f,r.yMax-66,(r.width-60)*.4f,48),"닫기","Esc",style)||Event.current.type==EventType.KeyDown&&Event.current.keyCode==KeyCode.Escape){PoliceMenuOpen=false;Event.current.Use();}
  }finally{GUI.matrix=prior;}
 }
 void Reguide(){
  var j=session.jobs;Reguided++;
  if(State==Returning&&ownerIndex>=0){var s=j.stops[ownerIndex];if(s.laneEdge>=0)j.nav.SetHdDestination(s.laneEdge,s.laneAt);Card("분실물 · 돌려주기 안내를 다시 이어요 · "+KoreanText.Stop(s.name));}
  else if(State==ToPolice){GuideToPolice();Card("분실물 · "+(LostItemRules.Police!=null?LostItemRules.Police.name:"파출소")+" 안내를 다시 이어요");}
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
 Vector3 policeRoad;object policeGraph;
 public Vector3 PoliceRoad {get{var j=session.jobs;if(j==null||j.nav==null||j.nav.hd==null||LostItemRules.Police==null)return PolicePoint;if(policeGraph==(object)j.nav.hd)return policeRoad;policeGraph=j.nav.hd;policeRoad=FindPoliceRoad(j.nav.hd);return policeRoad;}}
 Vector3 FindPoliceRoad(HdRoadGraph g){int best=-1;float bd=float.MaxValue,bat=0;for(int i=0;i<g.edges.Length;i++){if(g.edges[i].change!=0||g.edges[i].turn!=0||g.edges[i].TraversalDisabled)continue;float err;float at=g.paths[i].Project(PolicePoint,out err);if(err<bd){bd=err;best=i;bat=at;}}return best<0?PolicePoint:g.paths[best].Position(bat);}
 void GuideToPolice(){var j=session.jobs;if(!j.nav||j.nav.hd==null||LostItemRules.Police==null)return;var road=PoliceRoad;float at;int edge=j.nav.hd.Nearest(road,Vector3.zero,out at);if(edge>=0)j.nav.SetHdDestination(edge,at);session.services.destination=LostItemRules.Police.name+" · 앞 도로에 정차";}
 // Life facilities (play feedback 2026-11-26): the police box and the emergency-room side stop show on the maps and
 // in the life hub, with a sign at the police box, so a lost item can be found and handed in.
 public bool PlacesShown {get{return Single&&LostItemRules.Police!=null;}}
 public MapStop PoliceStop {get{var j=session.jobs;if(j==null||j.nav==null||j.nav.hd==null||LostItemRules.Police==null)return null;var road=PoliceRoad;float at;int edge=j.nav.hd.Nearest(road,Vector3.zero,out at);if(edge<0)return null;var p=j.nav.hd.paths[edge];var pos=p.Position(at);var dir=p.Tangent(at);return new MapStop{name=LostItemRules.Police.name,x=pos.x,z=pos.z,dx=dir.x,dz=dir.z,graphStop=true,laneEdge=edge,laneAt=at};}}
 public MapStop HospitalApproach {get{var j=session.jobs;int i=j?j.HospitalStop:-1;return i<0?null:j.stops[i];}}
 public bool GuidePolice(){if(!PlacesShown||session.jobs.trip.State==TripState.OnTrip)return false;var stop=PoliceStop;if(stop==null)return false;session.jobs.nav.SetDestination(stop);session.services.destination=LostItemRules.Police.name+" · 앞 도로에 정차";return true;}
 public bool GuideHospital(){if(!PlacesShown||session.jobs.trip.State==TripState.OnTrip)return false;var stop=HospitalApproach;if(stop==null)return false;session.jobs.nav.SetDestination(stop);session.services.destination=EmergencyRules.HospitalName+" · 앞 정차 칸";return true;}
 bool signs;
 void BuildSigns(){
  signs=true;if(!PlacesShown)return;
  DressPolice();
  var road=Flat(PoliceRoad);var toward=PolicePoint-road;Sign(road+(toward.sqrMagnitude>1?toward.normalized*Mathf.Min(6,toward.magnitude):Vector3.zero),LostItemRules.Police.name,new Color(.15f,.3f,.75f));
  // The emergency room is in the map since 2026-11-26: its own sign at the door, beside the drop stop.
  var h=HospitalApproach;var hp=LostItemRules.Hospital;if(h!=null&&hp!=null)Sign(Vector3.Lerp(h.Kerb,hp.V(),.55f),"+ 응급실 · "+EmergencyRules.HospitalName,new Color(.8f,.15f,.15f));
 }
 // Play feedback round 7: the police box looks like a facility: a blue name board on the face toward the road, a red
 // and blue light bar on the roof, a paved walk from the kerb to the door and a painted stopping box at the kerb.
 void DressPolice(){
  var map=session.world?session.world.map:null;if(map==null||map.buildings==null||LostItemRules.Police==null)return;
  MapBuilding building=null;foreach(var b in map.buildings)if(b!=null&&b.name==LostItemRules.Police.name){building=b;break;}
  var road=Flat(PoliceRoad);var centre=PolicePoint;
  if(building!=null&&building.points!=null&&building.points.Length>0){float x=0,z=0;foreach(var p in building.points){x+=p.x;z+=p.z;}centre=new Vector3(x/building.points.Length,0,z/building.points.Length);}
  var toBuilding=centre-road;toBuilding.y=0;if(toBuilding.sqrMagnitude<1)return;float gap=toBuilding.magnitude;var dir=toBuilding/gap;
  var root=new GameObject("파출소 꾸밈 · "+LostItemRules.Police.name).transform;root.position=road;root.rotation=Quaternion.LookRotation(dir);
  float ground=Floor(road+dir*Mathf.Min(gap,6)),roof=Roof(centre,ground);
  var blue=WorldBuilder.Mat("파출소 청색",new Color(.08f,.2f,.5f));var white=WorldBuilder.Mat("파출소 백색",new Color(.95f,.96f,.98f));
  var walk=WorldBuilder.Mat("파출소 보도",new Color(.72f,.70f,.66f));var paint=WorldBuilder.Mat("파출소 정차 표시",new Color(.95f,.8f,.18f));
  // Walk from the kerb (3 m in from the lane point) to the building face.
  float face=Mathf.Max(4,gap-5);
  WorldBuilder.Box("출입 보도",new Vector3(0,ground-road.y+.06f,(3+face)*.5f),new Vector3(2.2f,.08f,Mathf.Max(.5f,face-3)),walk,root,false);
  // Stopping box on the road at the kerb.
  var stopBox=new GameObject("파출소 정차 칸").transform;stopBox.SetParent(root,false);stopBox.localPosition=new Vector3(0,.06f,1.6f);stopBox.localRotation=Quaternion.Euler(0,90,0);
  foreach(float x in new[]{-1.3f,1.3f})WorldBuilder.Box("정차 칸",new Vector3(x,0,0),new Vector3(.12f,.02f,6),paint,stopBox,false);
  foreach(float z in new[]{-3f,3f})WorldBuilder.Box("정차 선",new Vector3(0,0,z),new Vector3(2.8f,.02f,.12f),paint,stopBox,false);
  // Name board and light bar on the building.
  var board=new GameObject("파출소 간판").transform;board.SetParent(root,false);board.localPosition=new Vector3(0,ground-road.y+Mathf.Min(roof-ground-.6f,3.4f),face-.15f);board.localRotation=Quaternion.identity;
  WorldBuilder.Box("간판 바탕",Vector3.zero,new Vector3(4.2f,.9f,.12f),blue,board,false);WorldBuilder.Box("간판 띠",new Vector3(0,-.52f,0),new Vector3(4.2f,.14f,.12f),white,board,false);
  var label=new GameObject("간판 글자");label.transform.SetParent(board,false);label.transform.localPosition=new Vector3(0,.02f,-.08f);
  var t=label.AddComponent<TextMesh>();t.text=LostItemRules.Police.name+"  POLICE";t.fontSize=56;t.characterSize=.06f;t.anchor=TextAnchor.MiddleCenter;t.color=Color.white;KoreanText.Apply(t);KoreanText.Fit(t,3.9f,.62f);
  var bar=new GameObject("경광등").transform;bar.SetParent(root,false);bar.localPosition=new Vector3(0,roof-road.y+.18f,face+2);
  WorldBuilder.Box("경광등 적색",new Vector3(-.45f,0,0),new Vector3(.8f,.3f,.35f),WorldBuilder.Mat("경광등 적색",new Color(.9f,.12f,.12f)),bar,false);
  WorldBuilder.Box("경광등 청색",new Vector3(.45f,0,0),new Vector3(.8f,.3f,.35f),WorldBuilder.Mat("경광등 청색",new Color(.12f,.3f,.95f)),bar,false);
 }
 static float Floor(Vector3 p){RaycastHit hit;return Physics.Raycast(p+Vector3.up*20,Vector3.down,out hit,40,~(1<<8),QueryTriggerInteraction.Ignore)?hit.point.y:p.y;}
 static float Roof(Vector3 p,float ground){RaycastHit hit;return Physics.Raycast(new Vector3(p.x,ground+60,p.z),Vector3.down,out hit,80,~(1<<8),QueryTriggerInteraction.Ignore)&&hit.point.y>ground+2?hit.point.y:ground+4;}
 void Sign(Vector3 at,string text,Color color){
  var root=new GameObject("생활시설 표지 · "+text);root.transform.position=new Vector3(at.x,0,at.z);
  WorldBuilder.Box("기둥",new Vector3(0,1.6f,0),new Vector3(.12f,3.2f,.12f),WorldBuilder.Mat("Facility sign pole",new Color(.55f,.57f,.6f)),root.transform,false);
  var board=WorldBuilder.Box("판",new Vector3(0,3.4f,0),new Vector3(.12f,.9f,2.6f),WorldBuilder.Mat("Facility sign "+text,color),root.transform,false);
  var label=new GameObject("시설 이름");label.transform.SetParent(root.transform,false);label.transform.localPosition=new Vector3(0,4.3f,0);var t=label.AddComponent<TextMesh>();t.text=text;t.fontSize=48;t.characterSize=.07f;t.anchor=TextAnchor.MiddleCenter;t.color=Color.white;KoreanText.Apply(t);label.AddComponent<FaceCamera>().hideBeyond=120;
 }
 void Pay(string id,int amount){if(amount>0)session.business.RecordReward(id,amount);session.business.SaveCheckpoint();}
 void Reset(){State=None;Trip="";ownerIndex=-1;penalised=false;}
 void OnGUI(){
  PoliceGUI();
  if(State!=Pending||!Empty||session.IsSimulationBot||session.paused)return;
  var options=new PassengerDialogueUI.Option[3];
  options[0]=new PassengerDialogueUI.Option("1","돌려주러 간다",ownerIndex>=0?KoreanText.Stop(session.jobs.stops[ownerIndex].name)+"에서 "+Mathf.RoundToInt(LostItemRules.OwnerWaitSeconds/60)+"분 기다림 · +"+LostItemRules.Reward(Item).ToString("N0")+"원":"주인 위치 모름");
  options[1]=new PassengerDialogueUI.Option("2",(LostItemRules.Police!=null?LostItemRules.Police.name:"파출소")+"에 맡긴다","+"+LostItemRules.PoliceReward.ToString("N0")+"원");
  options[2]=new PassengerDialogueUI.Option("3","그냥 둔다","다음 손님 만족 −"+LostItemRules.LeavePenalty);
  int selected=PassengerDialogueUI.Draw("분실물","뒷좌석에 "+LostItemRules.Names[Item]+"이 남아 있어요.",options,"",PassengerDialogueUI.PortraitIdentity(session));
  if(selected>=0&&NumericChoiceInput.TryConsume(Time.frameCount))Choose(selected+1);
 }
}
