using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class ServiceStops : MonoBehaviour {
 bool Pvp {get{return session&&session.business&&session.business.SimulationSession;}}
 public bool CanContinueConditionService(int kind){return Pvp&&session.business.ledger.State==BusinessState.Open&&!session.jobs.Active&&(PvpNativeLan.IsReplica(session)?Mathf.Abs(session.taxi.Speed):session.taxi.body.velocity.magnitude)<.56f&&(kind==1?AtRestaurant():kind==3&&(AtRestArea()||AtParking()));}
 public void FinishConditionService(int kind,bool completed){notice=completed?(kind==1?"식사 완료 · "+PvpConditionRules.MealCost.ToString("N0")+"원 · 포만 +60":"짧은 휴식 완료 · 피로 −40 · 졸음 해제"):"이용 중단 · 비용/회복 없음";noticeUntil=Time.unscaledTime+5;if(completed)CompleteServiceNavigation();Close();session.business.SaveCheckpoint();}
 public bool PostTripVisible {get{return postTrip;}}
 public static Rect PostTripBounds(float width,float height,bool pvp){float scale=UiReadability.HudScale(width,height);return new Rect(width/2-310*scale,200*scale,620*scale,174*scale);}
 // Presentation follows the replicated/current care state; reading/opening does not acknowledge it.
 public bool RepairAttention {get{return session&&session.business&&session.business.ledger!=null&&session.business.ledger.Care.Damage.Durability<=50;}}
 public bool AttentionFor(int kind){if(!session||!session.business||session.business.ledger==null)return false;var e=session.business.ledger;return kind==5?RepairAttention:kind==0?e.Account.Fuel<=25:kind==1?e.Driver.satiety<=25:(kind==2||kind==3)&&e.Driver.fatigue>=75;}
 public bool LifeHubAttention {get{return AttentionFor(0)||AttentionFor(1)||AttentionFor(2)||RepairAttention;}}
 public bool CanOpenLifeHub {get{return session&&session.business&&session.jobs&&!session.jobs.Active&&!session.paused&&!session.PracticeBusy&&!session.PracticeMenuOpen&&!session.FullMapOpen&&!session.ChoiceOpen&&!session.business.preparing&&!session.business.ShowingSummary&&!session.business.restartPrompt&&!session.business.ResumeBlocked&&!session.law.HistoryOpen;}}
 public bool ToggleLifeHub(){if(postTrip){DismissPostTrip();return true;}if(!CanOpenLifeHub)return false;postTrip=true;RefreshPostTrip();nextPostTripRefresh=Time.unscaledTime+2;return true;}
 public bool OpenWorkshopFromHub(){if(!postTrip||!CanOpenLifeHub||!OpenFacilityPlanner(5))return false;DismissPostTrip();return true;}
 public DriveSession session;public bool open;public string notice="";public int pending=-1;public string destination="";
 float noticeUntil;int pendingFuel=-1;public float FuelAmount {get{return Mathf.Min(10,100-session.business.ledger.Account.Fuel);}}
 static readonly string[] mealMenu={"제육백반","된장찌개 정식","소고기국밥"};
 public int SelectedMeal {get;private set;}
 public void ApplyNetworkView(bool isOpen,int pendingAction,string message,string target,int meal,int fuel){open=isOpen;pending=pendingAction;notice=message??"";destination=target??"";SelectedMeal=Mathf.Clamp(meal,0,mealMenu.Length-1);pendingFuel=fuel;}
 public string MealName {get{return mealMenu[SelectedMeal];}}
 public bool SelectMeal(int index){if(PvpNativeLan.Route(session,"service-meal",index))return true;if(!open||pending!=1||index<0||index>=mealMenu.Length)return false;SelectedMeal=index;return true;}
 // Legacy regional references; Eungye meal has a dedicated stop. Retained for meal/parking and old regional stop references. Eungye fuel uses actual pump bays.
 public readonly int[] indices={3,6,9};
 readonly string[] names={"임시 주유소","임시 식당","임시 주차장"};
 int observedCompleted,observedFailed;bool postTrip;int recommended=-1,postTripFuel=-1;float nextPostTripRefresh;
 readonly float[] postTripDistances=new float[4];
 public bool PostTripOpen {get{return postTrip;}}
 public int PostTripRecommendation {get{return recommended;}}
 public IReadOnlyList<FuelStationLayout.FuelStation> Stations {get{return FuelStationLayout.For(session.world.map);}}
 public int FuelAtPump {get{var taxi=session.taxi;for(int i=0;i<Stations.Count;i++)if(Stations[i].CanRefuel(taxi.transform.position,taxi.transform.forward,(PvpNativeLan.IsReplica(session)?Mathf.Abs(taxi.Speed):taxi.body.velocity.magnitude)))return i;return -1;}}
 public FuelStationLayout.FuelStation RestArea {get{var map=session.world.map;if(ProjectMapScene.Active(map)){var o=Nearest("rest");return o==null?null:FuelStationLayout.FromObject(map,o);}return FuelStationLayout.Rest(map);}}
 ProjectMapObject Nearest(string kind){var position=session.taxi?session.taxi.transform.position:Vector3.zero;return ProjectMapScene.Services(session.world.map,kind).OrderBy(o=>(o.Position-position).sqrMagnitude).FirstOrDefault();}
 public bool HasService(int kind){return !ProjectMapScene.Active(session.world.map)||kind==0||Nearest(kind==1?"restaurant":kind==3?"rest":"parking")!=null||kind==3&&Nearest("parking")!=null;}
 public MapStop ParkingStop {get{var map=session.world.map;if(ProjectMapScene.Active(map)){var o=Nearest("parking");if(o!=null)return ProjectMapScene.ParkingStop(o);}return session.jobs.stops[indices[2]];}}
 public bool AtParking(){var t=session.taxi;var map=session.world.map;if(ProjectMapScene.Active(map))return ProjectMapScene.Services(map,"parking").Any(o=>ProjectMapScene.CanUse(o,t.transform.position,t.transform.forward,(PvpNativeLan.IsReplica(session)?Mathf.Abs(t.Speed):t.body.velocity.magnitude)));return session.jobs.At(indices[2]);}
 static MapStop Unavailable(string name){return new MapStop{name=name+" 없음",x=-10000,z=-10000,dx=0,dz=1,width=3,laneEdge=-1,graphStop=true};}
 MapData mealMap;MapStop mealStop;
 public MapStop MealStop {get{var map=session.world.map;if(ProjectMapScene.Active(map)){var o=Nearest("restaurant");return o==null?Unavailable("식당"):ProjectMapScene.ServiceStop(map,o);}if(!ReferenceEquals(mealMap,map)||mealStop==null){mealMap=map;mealStop=CreateMealStop(map);}return mealStop;}}
 public MapStop StopFor(int kind){if(kind==1)return MealStop;if((kind==2||kind==3)&&ProjectMapScene.Active(session.world.map)){var o=Nearest("parking");return o==null?Unavailable("주차장"):ProjectMapScene.ServiceStop(session.world.map,o);}return session.jobs.stops[indices[kind==3?2:kind]];}
 public static MapStop CreateMealStop(MapData map){
  if(ProjectMapScene.Active(map)){var o=ProjectMapScene.Services(map,"restaurant").FirstOrDefault();return o==null?Unavailable("식당"):ProjectMapScene.ServiceStop(map,o);}
  if(map.mapId!="eungye"||!map.HasHd)return map.stops[6%map.stops.Length];
  return RestaurantSiteLayout.For(map).approach;
 }
 public RestaurantSiteLayout.Site Restaurant {get{var map=session.world.map;if(ProjectMapScene.Active(map)){var o=Nearest("restaurant");return o==null?null:RestaurantSiteLayout.FromObject(map,o);}return RestaurantSiteLayout.For(map);}}
 public Vector3 MealPosition {get{return Restaurant!=null?Restaurant.parking:MealStop.V();}}
 public MapStop MealParkingStop {get{return Restaurant!=null?Restaurant.ParkingStop:MealStop;}}
 public bool AtRestaurant(){var t=session.taxi;if(ProjectMapScene.Active(session.world.map))return ProjectMapScene.Services(session.world.map,"restaurant").Any(o=>ProjectMapScene.CanUse(o,t.transform.position,t.transform.forward,(PvpNativeLan.IsReplica(session)?Mathf.Abs(t.Speed):t.body.velocity.magnitude)));return Restaurant!=null?Restaurant.CanDine(t.transform.position,t.transform.forward,(PvpNativeLan.IsReplica(session)?Mathf.Abs(t.Speed):t.body.velocity.magnitude)):MealStop.CanStop(t.transform.position,t.transform.forward,(PvpNativeLan.IsReplica(session)?Mathf.Abs(t.Speed):t.body.velocity.magnitude));}
 public bool AtRestArea(){if(ProjectMapScene.Active(session.world.map)){var taxi=session.taxi;return ProjectMapScene.Services(session.world.map,"rest").Any(o=>ProjectMapScene.CanUse(o,taxi.transform.position,taxi.transform.forward,(PvpNativeLan.IsReplica(session)?Mathf.Abs(taxi.Speed):taxi.body.velocity.magnitude)));}var r=RestArea;var t=session.taxi;return r!=null&&r.CanRefuel(t.transform.position,t.transform.forward,(PvpNativeLan.IsReplica(session)?Mathf.Abs(t.Speed):t.body.velocity.magnitude));}
 public bool AtEndDayHub(){return AtParking()||AtRestArea();}
 public bool AtFuelPump(){return Stations.Count>0?FuelAtPump>=0:session.world.map.mapId!="eungye"&&session.jobs.At(indices[0]);}
 public IReadOnlyList<WorkshopLayout.Site> Workshops {get{return WorkshopLayout.For(session.world.map);}}
 public bool AtWorkshop(){var t=session.taxi;foreach(var site in Workshops)if(site.CanRepair(t.transform.position,t.transform.forward,PvpNativeLan.IsReplica(session)?Mathf.Abs(t.Speed):t.body.velocity.magnitude))return true;return false;}
 public bool AtRepairPump(){if(Workshops.Count>0)return AtWorkshop();int pump=FuelAtPump;return pump>=0?Stations[pump].SupportsRepair:Stations.Count==0&&AtFuelPump();} public bool FuelMenuOnly {get{return AtFuelPump();}}
 public int[] LocalActions {get{if(AtWorkshop()||session.damage&&session.damage.AtRepairBay)return new[]{5};int pump=FuelAtPump;if(pump>=0){var actions=new List<int>{0};if(Stations[pump].SupportsWash)actions.Add(4);if(Workshops.Count==0&&Stations[pump].SupportsRepair)actions.Add(5);return actions.ToArray();}if(AtFuelPump())return new[]{0,4,5};if(AtRestaurant())return Pvp&&AtEndDayHub()?new[]{1,3}:new[]{1};if(AtEndDayHub())return Pvp?new[]{3}:session.business.ledger.OneDay?new[]{3,2}:new[]{2};return new int[0];}}
 public string TipText {get{string text=destination;if(QueueStatus.Length>0)text+=(text.Length==0?"":"\n")+QueueStatus;if(session.ShowHints)text+=(text.Length==0?"":"\n")+"F7 현재 시설 이용 · M 지도에서 거점 안내";return text;}}
 public bool CanUse(int kind){if(session.accident&&session.accident.Locked)return false;if(session.simulationMatch&&session.simulationMatch.ServiceLocked(session))return false;if(kind==5)return session.damage&&session.damage.CanRepair;if(Pvp&&(kind==1||kind==3))return CanContinueConditionService(kind)&&session.business.ledger.Account.Cash>=(kind==1?PvpConditionRules.MealCost:PvpConditionRules.RestCost);if(session.business.ledger.Driver!=null&&session.business.ledger.Driver.drowsiness.Blackout)return false;if(kind==0||kind==4){int pump=FuelAtPump;bool wash=kind!=4||(pump>=0?Stations[pump].SupportsWash:Stations.Count==0);return wash&&session.business.ledger.State==BusinessState.Open&&session.jobs.trip.State!=TripState.OnTrip&&(PvpNativeLan.IsReplica(session)?Mathf.Abs(session.taxi.Speed):session.taxi.body.velocity.magnitude)<.56f&&AtFuelPump();}bool daily=session.business.ledger.OneDay;return kind>=1&&kind<=(daily?3:2)&&session.business.ledger.State==BusinessState.Open&&(daily?session.jobs.trip.State!=TripState.OnTrip:!session.jobs.Active)&&(PvpNativeLan.IsReplica(session)?Mathf.Abs(session.taxi.Speed):session.taxi.body.velocity.magnitude)<.56f&&(kind==1?AtRestaurant():kind==3?(AtRestArea()||AtParking()):AtEndDayHub());}
 public string Quote(int kind){if(Pvp&&(kind==1||kind==3)){var preview=session.business.ledger.PreviewConditionService(kind==3);return (kind==1?"간편식 "+PvpConditionRules.MealCost.ToString("N0")+"원 · "+PvpConditionRules.MealSeconds+"초 · 포만 +60":"짧은 휴식 "+PvpConditionRules.RestCost.ToString("N0")+"원 · "+PvpConditionRules.RestSeconds+"초 · 피로 −40 · 졸음 해제")+"\n완료 예상 포만 "+preview.satiety.ToString("0")+" · 피로 "+preview.fatigue.ToString("0")+"\n공통 경기 시계 계속 · 완료 시 비용/회복 1회 · 중단 시 비용/회복 없음";}if(kind==5)return session.damage.RepairQuote;if(kind==4)return "세차·실내 청소 2,000원 · 30초 (부족분은 미납)\n청결도 100 회복 · 엔진 정지\n취소 시 비용/시간/청결 변화 없음";if(kind<0||kind>3)return "";var b=session.business;var a=b.ledger.Account;if(b.ledger.OneDay&&(kind==1||kind==3)){var preview=b.ledger.PreviewConditionService(kind==3);return (kind==1?"식사 3,000원 · 50초 (부족분은 미납)":"수면 무료 · 100초")+"\n포만감 "+preview.satiety.ToString("0")+" · 피로 "+preview.fatigue.ToString("0")+" 예상\n미탑승 호출은 취소됩니다. 엔진 정지 · 점수 변화 없음";}return kind==0?"주유 "+FuelAmount.ToString("0.0")+"% · "+Mathf.CeilToInt(FuelAmount*session.business.settings.fuelUnitPrice).ToString("N0")+"원 / 10초":kind==1?"식사 3,000원 / 10초 (하루 1회)":"무료 수면 100초 · 피로 −40 후 영업 종료 · 하루 주차비 500원";}
 void Start(){
  if(session.business.ledger.Account==null){enabled=false;return;}
  observedCompleted=session.jobs.completed;observedFailed=session.jobs.failed;if(session.IsSimulationBot)return;
  if(session.world.map.mapId=="eungye")indices[2]=0;
  for(int i=0;i<indices.Length;i++){
   indices[i]%=session.jobs.stops.Length;if(ProjectMapScene.Active(session.world.map)&&i>0)continue;if(i==0&&Stations.Count>0||i==1&&Restaurant!=null)continue;
   var label=new GameObject(i==1?MealStop.name:names[i]);label.transform.position=StopFor(i).Passenger+Vector3.up*3.2f;var t=label.AddComponent<TextMesh>();t.text=(i==1?"식당 · 식사":names[i])+" · F7";t.fontSize=40;t.characterSize=.09f;t.anchor=TextAnchor.MiddleCenter;t.color=Color.yellow;KoreanText.Apply(t);label.AddComponent<FaceCamera>();
  }
 }
 void Update(){if(PvpNativeLan.IsReplica(session)){ObserveCompletedTrip();if(postTrip&&(session.jobs.Active||session.business.ledger.Care.Damage.Disabled))DismissPostTrip();if(postTrip&&Time.unscaledTime>=nextPostTripRefresh){RefreshPostTrip();nextPostTripRefresh=Time.unscaledTime+2;}if((open||!PvpNativeLan.UiBlocked(session))&&Input.GetKeyDown(KeyCode.F7))Toggle();return;}if(noticeUntil>0&&Time.unscaledTime>=noticeUntil){notice="";noticeUntil=0;}
  TickFacilityQueue();ObserveCompletedTrip();
  if(postTrip&&(session.business.ledger.Care.Damage.Disabled||session.jobs.Active||session.business.ShowingSummary||session.business.preparing||session.business.ledger.State!=BusinessState.Open))DismissPostTrip();
  if(postTrip&&Time.unscaledTime>=nextPostTripRefresh){RefreshPostTrip();nextPostTripRefresh=Time.unscaledTime+2;}
  if(!PvpNativeLan.IsRemoteHuman(session)&&Input.GetKeyDown(KeyCode.F7))Toggle();
 }
 public void ObserveCompletedTrip(){
  int count=session.jobs.completed;bool deliveredLate=session.jobs.failed>observedFailed&&session.jobs.trip.State==TripState.Failed&&session.jobs.current!=null&&session.jobs.At(session.jobs.current.drop);observedFailed=session.jobs.failed;if(count==observedCompleted&&!deliveredLate)return;
  bool finished=count>observedCompleted||deliveredLate;observedCompleted=count;
  if(!finished||PracticeSession.Enabled||!session.business.ledger.OneDay||session.business.ledger.State!=BusinessState.Open)return;
  RefreshPostTrip();nextPostTripRefresh=Time.unscaledTime+2;
 }
 float ServiceDistance(MapStop stop){return PassengerJobs.DirectedDistance(session.jobs.nav.hd,session.taxi.transform.position,session.taxi.transform.forward,stop);}
 void RefreshPostTrip(){
  postTripFuel=-1;postTripDistances[0]=float.PositiveInfinity;
  for(int i=0;i<Stations.Count;i++){float d=ServiceDistance(Stations[i].approach);if(d<postTripDistances[0]){postTripDistances[0]=d;postTripFuel=i;}}
  if(Stations.Count==0&&session.world.map.mapId!="eungye")postTripDistances[0]=ServiceDistance(session.jobs.stops[indices[0]]);
  postTripDistances[1]=HasService(1)?ServiceDistance(MealStop):float.PositiveInfinity;
  postTripDistances[2]=HasService(2)?ServiceDistance(StopFor(2)):float.PositiveInfinity;
  postTripDistances[3]=RestArea==null?postTripDistances[2]:Mathf.Min(postTripDistances[2],ServiceDistance(RestArea.approach));
  var ledger=session.business.ledger;recommended=ledger.Care.NeedsWash&&!float.IsInfinity(postTripDistances[0])?0:PostTripServicePolicy.Recommend(ledger.Account.Fuel,ledger.Driver.satiety,ledger.Driver.fatigue,ledger.SecondsLeft,postTripDistances);
 }
 public void DismissPostTrip(){postTrip=false;}
 public bool GuideAfterTrip(int kind){
  if(PvpNativeLan.Route(session,"guide-after-trip",kind)){DismissPostTrip();return true;}
  if(kind<0||kind>3||session.jobs.Active||session.paused||session.business.ledger.State!=BusinessState.Open)return false;
  RefreshPostTrip();if(float.IsInfinity(postTripDistances[kind]))return false;
  ResetFacilityQueueIntent();
  // Explicit route choice only; no popup, payment, rest or shift closing is performed here.
  if(kind==0&&postTripFuel>=0){session.jobs.nav.SetDestination(Stations[postTripFuel].approach);destination=Stations[postTripFuel].name+" · 입구부터 수동 진입 · 정차 후 F7";}
  else if(kind==3&&RestArea!=null&&ServiceDistance(RestArea.approach)<postTripDistances[2]){session.jobs.nav.SetDestination(RestArea.approach);destination="동부 휴식 주차구역 · 입구부터 수동 진입 · 정차 후 F7";}
  else {int stop=kind==3?2:kind;session.jobs.nav.SetDestination(StopFor(stop));destination=stop==1&&Restaurant!=null?"식당 입구 앞까지 안내 · H 해제 후 주차 칸에 진입 · 정차 후 F7":(kind==3?"수면 · ":"")+names[stop]+" · 오른쪽 정차 화살표에서 F7";}
  DismissPostTrip();return true;
 }
 void DrawPostTrip(GUIStyle style,GUIStyle button){
  if(!postTrip||session.paused||session.FullMapOpen||session.PracticeMenuOpen||session.PracticeBusy||session.ChoiceOpen||session.business.restartPrompt||session.business.taxPrompt||session.business.ShowingSummary||session.business.preparing||session.law.HistoryOpen)return;
  var previous=GUI.matrix;float scale=CockpitUI.Scale;GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
  try {
   var bounds=PostTripBounds(Screen.width,Screen.height,Pvp);float x=bounds.x/scale,y=bounds.y/scale,width=bounds.width/scale;GUI.depth=-20;CasualUI.Panel(new Rect(x,y,width,174));
   string reason=RepairAttention?"내구도 "+session.business.ledger.Care.Damage.Durability+" · 수리 권장":recommended==0?(session.jobs.NeedsWash?"세차 필요":"주유 권장"):recommended==1?"식사 권장":recommended==3?"수면 권장":recommended==2?"마감 준비":"현재 컨디션 양호";
   CasualUI.Header(new Rect(x+8,y+6,width-16,34),"생활거점 · "+reason,0);
   string[] labels={session.jobs.NeedsWash?"주유·세차":"주유","식사","귀가 주차장","수면","공업사","닫기"};int[] glyphs={8,9,10,11,12,19};float cell=(width-20)/3;
   for(int i=0;i<6;i++){
    var rect=new Rect(x+10+(i%3)*cell,y+48+(i/3)*61,cell-6,54);
    GUI.enabled=i<4?!session.jobs.Active&&!float.IsInfinity(postTripDistances[i]):true;
    string label=labels[i];if(i<4){float d=postTripDistances[i];label+="\n"+(float.IsInfinity(d)?"경로 없음":d>=1000?(d/1000).ToString("0.0")+"km":d.ToString("0")+"m");}
    var rowButton=KoreanText.Button(UiReadability.Font(15));rowButton.padding=new RectOffset(36,10,0,0);
    if(GUI.Button(rect,label,rowButton)){if(i<4)GuideAfterTrip(i);else if(i==4){OpenWorkshopFromHub();}else DismissPostTrip();}
    HudGlyph.Draw(new Rect(rect.x+8,rect.y+17,20,20),glyphs[i],CasualUI.Blue);
    if(i<5&&AttentionFor(i==4?5:i))CasualUI.AttentionDot(new Rect(rect.xMax-12,rect.y-3,12,12));
   }
   GUI.enabled=true;
  } finally {GUI.matrix=previous;GUI.enabled=true;}
 }
 public void Toggle(){if(session.SettingsOpen)return;
  if(session.accident&&session.accident.Locked||session.damage&&session.damage.PendingServiceRemaining>0)return;
  if(PvpNativeLan.Route(session,"toggle-service"))return;
  if(QueueWaiting){CancelQueue();notice="시설 대기 취소 · 결제/서비스 시간 변화 없음";noticeUntil=Time.unscaledTime+5;return;}
  if(open){Close();return;}
  if(session.FullMapOpen||session.PracticeMenuOpen||session.PracticeBusy||session.business.restartPrompt||PracticeSession.Enabled||session.business.preparing||session.law.HistoryOpen||session.business.ShowingSummary||session.business.taxPrompt||(session.business.ledger.OneDay?session.jobs.trip.State==TripState.OnTrip:session.jobs.Active)||(PvpNativeLan.IsReplica(session)?Mathf.Abs(session.taxi.Speed):session.taxi.body.velocity.magnitude)>=.56f){notice="빈차로 안전하게 정차한 뒤 F7";noticeUntil=Time.unscaledTime+4;return;}
  if(LocalActions.Length==0&&session.damage&&session.damage.Damage.Durability<100&&Workshops.Count>0){OpenFacilityPlanner(5);return;}
  if(LocalActions.Length==0){notice="현재 시설의 정차 구역이 아닙니다 · M 지도에서 생활 거점을 선택하세요";noticeUntil=Time.unscaledTime+5;return;}
  pending=-1;pendingFuel=-1;open=true;if(Pvp){session.assisted.SetActive(false);session.paused=false;Time.timeScale=1;}else{session.paused=true;Time.timeScale=0;}
 }
 public void Close(bool resumePickup=true){if(PvpNativeLan.Route(session,"close-service",resumePickup?1:0))return;if(Pvp&&session.simulationMatch&&session.simulationMatch.CancelConditionService(session)){notice="이용 중단 · 소비한 시간 유지 · 비용/회복 없음";noticeUntil=Time.unscaledTime+5;}pending=-1;pendingFuel=-1;open=false;DismissPostTrip();session.paused=Pvp&&session.simulationMatch&&session.simulationMatch.Finished;Time.timeScale=session.paused?0:1;if(resumePickup){session.jobs.ResumePickupNavigation();if(session.jobs.trip.State==TripState.ToPickup)destination="";}}
 // The facility approach is a temporary destination, not a trip destination.
 // Completing a service must retire its route as well as its HUD caption.
 public void CompleteServiceNavigation(){destination="";var jobs=session.jobs;if(jobs.trip.State==TripState.ToPickup&&jobs.current!=null)jobs.ResumePickupNavigation();else if(!jobs.Active)jobs.nav.Clear();}
 public bool GuideFuel(int station){
  if(PvpNativeLan.Route(session,"guide-fuel",station))return true;
  if(!open||station<0||station>=Stations.Count||session.jobs.trip.State==TripState.OnTrip)return false;
  ResetFacilityQueueIntent();
  session.jobs.nav.SetDestination(Stations[station].approach);destination=Stations[station].name+" · 입구 앞까지 내비/H 안내 · 내부는 H 해제 후 수동 진입";Close(false);return true;
 }
 public bool Guide(int kind){
  if(PvpNativeLan.Route(session,"guide",kind))return true;
  if(!open||kind<0||kind>3||!HasService(kind)||session.jobs.trip.State==TripState.OnTrip)return false;
  ResetFacilityQueueIntent();
  if(session.jobs.Active&&kind!=0){if(!session.business.ledger.OneDay)return false;session.jobs.ClearForDay();}
  if(kind==3&&RestArea!=null){var taxi=session.taxi;var graph=session.jobs.nav.hd;float restDistance=PassengerJobs.DirectedDistance(graph,taxi.transform.position,taxi.transform.forward,RestArea.approach);float baseDistance=PassengerJobs.DirectedDistance(graph,taxi.transform.position,taxi.transform.forward,StopFor(2));if(restDistance<baseDistance){session.jobs.nav.SetDestination(RestArea.approach);destination="동부 휴식 주차구역 · 입구부터 수동 진입 · 파란 정차선에서 F7";Close();return true;}}
  if(kind==3)kind=2;
  if(kind==0&&Stations.Count>0){int closest=0;float best=float.MaxValue;for(int i=0;i<Stations.Count;i++){float distance=(Stations[i].entrance-session.taxi.transform.position).sqrMagnitude;if(distance<best){best=distance;closest=i;}}return GuideFuel(closest);}
  session.jobs.nav.SetDestination(StopFor(kind));destination=kind==1&&Restaurant!=null?"식당 입구 앞까지 안내 · H 해제 후 주차 칸에 진입 · 정차 후 F7":names[kind]+" · 오른쪽 정차 화살표에서 F7";Close();return true;
 }
 public bool RequestAction(int kind){if(PvpNativeLan.Route(session,"service-request",kind))return true;if(session.FullMapOpen||!open||!CanUse(kind))return false;pending=kind;pendingFuel=kind==0||kind==4||kind==5?FuelAtPump:-1;return true;}
 public bool ConfirmAction(bool accept){
  if(PvpNativeLan.Route(session,"service-confirm",accept?1:0))return true;
  if(!open||pending<0)return false;int kind=pending,station=pendingFuel;pending=-1;pendingFuel=-1;
  if(!accept){notice="취소했습니다 · 결제/시간 차감 없음";noticeUntil=Time.unscaledTime+4;return false;}
  if(session.FullMapOpen||!CanUse(kind)||(kind==0||kind==4||kind==5)&&Stations.Count>0&&FuelAtPump!=station)return false;
  if(kind<2||kind==3||kind==4||kind==5)return Buy(kind);Close();return session.business.RequestEndDay();
 }
 public bool Buy(int kind){
  if(PvpNativeLan.Route(session,"service-buy",kind))return true;
  if(kind<0||kind==2||kind>5||session.FullMapOpen||!open||!CanUse(kind))return false;var b=session.business;var a=b.ledger.Account;int day=b.ledger.Day;
  if(!AdmitPurchase(kind))return false;
  if(Pvp&&(kind==1||kind==3)){bool started=session.simulationMatch&&session.simulationMatch.BeginConditionService(session,kind);if(started){notice=(kind==1?"식사":"짧은 휴식")+" 진행 중 · F7로 중단";noticeUntil=0;}return started;}
  if(kind==5){if(!session.damage.Repair())return false;notice=session.damage.PendingServiceRemaining>0?"공업사 수리 진행 중 · 완료 시 결제":"전량 수리 완료 · 내구도 100";noticeUntil=Time.unscaledTime+5;CompleteServiceNavigation();Close();b.SaveCheckpoint();return true;}
  if(kind==4){if(!b.ledger.UseWash(session.law.ids.Next("wash"))){notice="서비스를 이용할 수 없습니다";noticeUntil=Time.unscaledTime+5;return false;}session.jobs.AdvancePickupWait(30);notice="세차·실내 청소 완료 · 청결 100 / 30초 경과 · 2,000원";noticeUntil=Time.unscaledTime+5;CompleteServiceNavigation();Close();b.SaveCheckpoint();return true;}
  if(b.ledger.OneDay&&(kind==1||kind==3)){int seconds=kind==1?50:100;double beforeFatigue=b.ledger.Driver.fatigue;bool done=kind==1?b.ledger.UseMeal(session.law.ids.Next("meal")):b.ledger.UseSleep(session.law.ids.Next("sleep"));if(!done){notice="서비스를 이용할 수 없습니다";noticeUntil=Time.unscaledTime+5;return false;}if(session.jobs.Active)session.jobs.ClearForDay();CompleteServiceNavigation();b.SaveCheckpoint();notice=kind==1?"식사 완료 · 50초 경과":"수면 완료 · "+seconds+"초 경과 · 피로 "+beforeFatigue.ToString("0")+" → "+b.ledger.Driver.fatigue.ToString("0");noticeUntil=Time.unscaledTime+4;Close();return true;}
  bool ok=kind==0?a.Refuel(session.law.ids.Next("fuel"),FuelAmount,day):a.Meal(session.law.ids.Next("meal"),day);
  if(ok){b.ledger.AdvanceService(10);session.jobs.AdvancePickupWait(10);CompleteServiceNavigation();b.SaveCheckpoint();notice="이용 완료 · 영업시간 10초 경과";noticeUntil=Time.unscaledTime+4;if(kind!=0||PvpSimulationMode.Active)Close();}else notice="현금·연료 잔량 또는 오늘 이용 여부를 확인하세요";return ok;
 }
 void Row(float x,float y,string label,int kind,int station,GUIStyle style,GUIStyle button){
  CasualUI.RoundedFill(new Rect(x+12,y-4,696,56),CasualUI.Soft);
  GUI.Label(new Rect(x+20,y,420,51),label,style);
  GUI.enabled=CanUse(kind)&&(station<0||FuelAtPump==station)&&(kind!=0||FuelAmount>.001f);
  if(GUI.Button(new Rect(x+480,y,220,42),kind==0?"+"+FuelAmount.ToString("0.#")+"% 주유 · "+Mathf.CeilToInt(FuelAmount*session.business.settings.fuelUnitPrice)+"원":kind==5?"전량 수리":kind==4?"세차·실내 청소 · 2,000원":kind==1?"식사하기":kind==3?(Pvp?"휴식 · "+PvpConditionRules.RestSeconds+"초":"수면 · 100초"):"휴식·영업 종료",button)){if(kind==0)Buy(0);else RequestAction(kind);}GUI.enabled=true;
 }
 void OnGUI(){
  if(session.IsSimulationBot||PvpNativeLan.IsRemoteHuman(session))return;
  if(session.business.ledger.Account==null||PracticeSession.Enabled)return;
  var style=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=16,wordWrap=true};var button=CasualUI.Action(14);
  style.normal.textColor=CasualUI.Ink;
  if(!open){if(!session.paused&&!session.business.restartPrompt&&!session.GetComponent<DriveHUD>().HelpOpen&&!session.FullMapOpen&&!session.ChoiceOpen&&!session.RideResultOpen&&!session.PracticeMenuOpen&&!session.business.ShowingSummary&&!session.business.preparing&&!session.law.HistoryOpen)DrawPostTrip(style,button);return;}

  int pump=FuelAtPump;var actions=LocalActions;bool fuelOnly=FuelMenuOnly,restaurant=System.Array.IndexOf(actions,1)>=0;GUI.depth=-110;float height=pending==1?525:pending>=0?430:350+Mathf.Max(0,actions.Length-1)*62,x=(Screen.width-720)/2f,y=(Screen.height-height)/2f;
  CasualUI.Backdrop();CasualUI.Panel(new Rect(x,y,720,height));
  CasualUI.Header(new Rect(x+8,y+8,704,40),(fuelOnly?(pump>=0?Stations[pump].ServiceLabel:"주유·세차"):restaurant?"기사 식당":"주차·휴식")+(Pvp?" · 경기 진행 중":" · 일시정지"),3);
  var b=session.business;var a=b.ledger.Account;GUI.Label(new Rect(x+20,y+55,680,28),"현금 "+a.Cash.ToString("N0")+"원 · 미납 "+a.Debt.ToString("N0")+"원 · 연료 "+a.Fuel.ToString("0.0")+"% · 청결 "+b.ledger.Care.Cleanliness.ToString("0"),style);
  if(Pvp&&session.simulationMatch&&session.simulationMatch.ConditionServiceActive(session)){GUI.Label(new Rect(x+30,y+100,660,130),(session.simulationMatch.ConditionServiceKind(session)==1?"식사":"짧은 휴식")+" · 남은 "+session.simulationMatch.ServiceRemaining(session).ToString("0.0")+"초\n경기는 계속 진행됩니다.\n중단 시 결제·회복 없음 (경과 시간 유지)",style);if(GUI.Button(new Rect(x+20,y+height-45,680,35),"이용 중단 F7",button))Close();return;}
  if(pending>=0){CasualUI.RoundedFill(new Rect(x+20,y+98,680,176),CasualUI.Soft);GUI.Label(new Rect(x+30,y+110,660,160),(pending==1?MealName+" · ":"")+Quote(pending)+(Pvp?"\n\n이용 완료 후 결제·회복됩니다.":"\n\n확정 후 결제됩니다.")+(session.jobs.trip.State==TripState.ToPickup?" 이용 시간만큼 승객 대기시간이 줄어듭니다.":""),style);
   if(pending==1){for(int i=0;i<mealMenu.Length;i++)if(GUI.Button(new Rect(x+30+i*220,y+285,210,48),(SelectedMeal==i?"✓ ":"")+mealMenu[i],button))SelectMeal(i);GUI.Label(new Rect(x+30,y+340,660,50),Pvp?"모든 메뉴 "+PvpConditionRules.MealCost.ToString("N0")+"원 · "+PvpConditionRules.MealSeconds+"초 · 포만감 +60 (최대 100)":"모든 메뉴 3,000원 · 50초 · 포만감 +70 (최대 100)",style);}
   if(GUI.Button(new Rect(x+30,y+height-130,310,50),"이 견적으로 이용",button))ConfirmAction(true);if(GUI.Button(new Rect(x+380,y+height-130,310,50),"취소",KoreanText.Button(14)))ConfirmAction(false);return;}
  int row=0;
  foreach(int kind in actions){string label=kind==0?(pump>=0?Stations[pump].name:names[0]):kind==4?"세차·실내 청소\n청결 100 회복 / 30초":kind==5?"자동차 공업사 · 전량 수리":kind==1?"식당 · "+MealStop.name:kind==3?(Pvp?"휴식 "+PvpConditionRules.RestCost.ToString("N0")+"원 · "+PvpConditionRules.RestSeconds+"초 / 피로 −40":"무료 수면 · 100초 / 피로 −40"):"현재 거점에서 휴식·영업 종료";Row(x,y+85+row++*62,label,kind,pump,style,button);}
  string pickupWarning=session.jobs.trip.State==TripState.ToPickup?(fuelOnly?"호출 유지 · 남은 대기 "+session.jobs.trip.PickupRemaining.ToString("0")+"초 · 이용 시간 차감":"식사·수면 확정 시 미탑승 호출 취소")+"\n":"";
  CasualUI.RoundedFill(new Rect(x+12,y+height-158,696,112),new Color(1,.95f,.83f));
  GUI.Label(new Rect(x+20,y+height-155,680,108),pickupWarning+notice+"\n"+(fuelOnly?"이용 후 출구로 나가세요.":restaurant?"다른 시설 찾기: M 지도":"다른 시설 찾기: M 지도"),new GUIStyle(style){fontSize=14});
  if(GUI.Button(new Rect(x+20,y+height-45,430,35),"닫기  F7",KoreanText.Button(14)))Close();
  if(GUI.Button(new Rect(x+460,y+height-45,240,35),"다른 시설·견적 비교",button))OpenFacilityPlanner(restaurant?1:fuelOnly?0:3);
 }
}
