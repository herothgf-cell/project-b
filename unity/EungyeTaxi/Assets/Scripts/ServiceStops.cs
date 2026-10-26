using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ServiceStops : MonoBehaviour {
 public bool PostTripVisible {get{return postTrip;}}
 public DriveSession session;public bool open;public string notice="";public int pending=-1;public string destination="";
 float noticeUntil;int pendingFuel=-1;public float FuelAmount {get{return Mathf.Min(10,100-session.business.ledger.Account.Fuel);}}
 static readonly string[] mealMenu={"제육백반","된장찌개 정식","소고기국밥"};
 public int SelectedMeal {get;private set;}
 public string MealName {get{return mealMenu[SelectedMeal];}}
 public bool SelectMeal(int index){if(!open||pending!=1||index<0||index>=mealMenu.Length)return false;SelectedMeal=index;return true;}
 // Legacy regional references; Eungye meal has a dedicated stop. Retained for meal/parking and old regional stop references. Eungye fuel uses actual pump bays.
 public readonly int[] indices={3,6,9};
 readonly string[] names={"임시 주유소","임시 식당","임시 주차장"};
 int observedCompleted,observedFailed;bool postTrip;int recommended=-1,postTripFuel=-1;float nextPostTripRefresh;
 readonly float[] postTripDistances=new float[4];
 public bool PostTripOpen {get{return postTrip;}}
 public int PostTripRecommendation {get{return recommended;}}
 public IReadOnlyList<FuelStationLayout.FuelStation> Stations {get{return FuelStationLayout.For(session.world.map);}}
 public int FuelAtPump {get{var taxi=session.taxi;for(int i=0;i<Stations.Count;i++)if(Stations[i].CanRefuel(taxi.transform.position,taxi.transform.forward,taxi.body.velocity.magnitude))return i;return -1;}}
 public FuelStationLayout.FuelStation RestArea {get{var map=session.world.map;if(ProjectMapScene.Active(map)){var o=Nearest("rest");return o==null?null:FuelStationLayout.FromObject(map,o);}return FuelStationLayout.Rest(map);}}
 ProjectMapObject Nearest(string kind){var position=session.taxi?session.taxi.transform.position:Vector3.zero;return ProjectMapScene.Services(session.world.map,kind).OrderBy(o=>(o.Position-position).sqrMagnitude).FirstOrDefault();}
 public bool HasService(int kind){return !ProjectMapScene.Active(session.world.map)||kind==0||Nearest(kind==1?"restaurant":kind==3?"rest":"parking")!=null||kind==3&&Nearest("parking")!=null;}
 public MapStop ParkingStop {get{var map=session.world.map;if(ProjectMapScene.Active(map)){var o=Nearest("parking");if(o!=null)return ProjectMapScene.ParkingStop(o);}return session.jobs.stops[indices[2]];}}
 public bool AtParking(){var t=session.taxi;var map=session.world.map;if(ProjectMapScene.Active(map))return ProjectMapScene.Services(map,"parking").Any(o=>ProjectMapScene.CanUse(o,t.transform.position,t.transform.forward,t.body.velocity.magnitude));return session.jobs.At(indices[2]);}
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
 public bool AtRestaurant(){var t=session.taxi;if(ProjectMapScene.Active(session.world.map))return ProjectMapScene.Services(session.world.map,"restaurant").Any(o=>ProjectMapScene.CanUse(o,t.transform.position,t.transform.forward,t.body.velocity.magnitude));return Restaurant!=null?Restaurant.CanDine(t.transform.position,t.transform.forward,t.body.velocity.magnitude):MealStop.CanStop(t.transform.position,t.transform.forward,t.body.velocity.magnitude);}
 public bool AtRestArea(){if(ProjectMapScene.Active(session.world.map)){var taxi=session.taxi;return ProjectMapScene.Services(session.world.map,"rest").Any(o=>ProjectMapScene.CanUse(o,taxi.transform.position,taxi.transform.forward,taxi.body.velocity.magnitude));}var r=RestArea;var t=session.taxi;return r!=null&&r.CanRefuel(t.transform.position,t.transform.forward,t.body.velocity.magnitude);}
 public bool AtEndDayHub(){return AtParking()||AtRestArea();}
 public bool AtFuelPump(){return Stations.Count>0?FuelAtPump>=0:session.world.map.mapId!="eungye"&&session.jobs.At(indices[0]);}
 public bool CanUse(int kind){if(kind==4)return session.business.ledger.State==BusinessState.Open&&!session.jobs.Active&&session.taxi.body.velocity.magnitude<.56f&&AtFuelPump();bool daily=session.business.ledger.OneDay;return kind>=0&&kind<=(daily?3:2)&&session.business.ledger.State==BusinessState.Open&&(daily&&kind>0?session.jobs.trip.State!=TripState.OnTrip:!session.jobs.Active)&&session.taxi.body.velocity.magnitude<.56f&&(kind==0?AtFuelPump():kind==1?AtRestaurant():kind==3?(AtRestArea()||AtParking()):AtEndDayHub());}
 public string Quote(int kind){if(kind==4)return "세차·실내 청소 2,000원 · 30초 (부족분은 미납)\n청결도 100 회복 · 엔진 정지\n취소 시 비용/시간/청결 변화 없음";if(kind<0||kind>3)return "";var b=session.business;var a=b.ledger.Account;if(b.ledger.OneDay&&(kind==1||kind==3)){var preview=b.ledger.PreviewConditionService(kind==3);return (kind==1?"식사 3,000원 · 50초 (부족분은 미납)":"수면 무료 · 100초")+"\n포만감 "+preview.satiety.ToString("0")+" · 피로 "+preview.fatigue.ToString("0")+" 예상\n미탑승 호출은 취소됩니다. 엔진 정지 · 점수 변화 없음";}return kind==0?"주유 "+FuelAmount.ToString("0.0")+"% · "+Mathf.CeilToInt(FuelAmount*session.business.settings.fuelUnitPrice).ToString("N0")+"원 / 10초":kind==1?"식사 3,000원 / 10초 (하루 1회)":"무료 수면 100초 · 피로 −40 후 영업 종료 · 하루 주차비 500원";}
 void Start(){
  if(session.business.ledger.Account==null){enabled=false;return;}
  observedCompleted=session.jobs.completed;observedFailed=session.jobs.failed;
  if(session.world.map.mapId=="eungye")indices[2]=0;
  for(int i=0;i<indices.Length;i++){
   indices[i]%=session.jobs.stops.Length;if(ProjectMapScene.Active(session.world.map)&&i>0)continue;if(i==0&&Stations.Count>0||i==1&&Restaurant!=null)continue;
   var label=new GameObject(i==1?MealStop.name:names[i]);label.transform.position=StopFor(i).Passenger+Vector3.up*3.2f;var t=label.AddComponent<TextMesh>();t.text=(i==1?"식당 · 식사":names[i])+" · F7";t.fontSize=40;t.characterSize=.09f;t.anchor=TextAnchor.MiddleCenter;t.color=Color.yellow;KoreanText.Apply(t);label.AddComponent<FaceCamera>();
  }
 }
 void Update(){if(noticeUntil>0&&Time.unscaledTime>=noticeUntil){notice="";noticeUntil=0;}
  ObserveCompletedTrip();
  if(postTrip&&(session.jobs.Active||session.business.ShowingSummary||session.business.preparing||session.business.ledger.State!=BusinessState.Open))DismissPostTrip();
  if(postTrip&&Time.unscaledTime>=nextPostTripRefresh){RefreshPostTrip();nextPostTripRefresh=Time.unscaledTime+2;}
  if(Input.GetKeyDown(KeyCode.F7))Toggle();
 }
 public void ObserveCompletedTrip(){
  int count=session.jobs.completed;bool deliveredLate=session.jobs.failed>observedFailed&&session.jobs.trip.State==TripState.Failed&&session.jobs.current!=null&&session.jobs.At(session.jobs.current.drop);observedFailed=session.jobs.failed;if(count==observedCompleted&&!deliveredLate)return;
  bool finished=count>observedCompleted||deliveredLate;observedCompleted=count;
  if(!finished||PracticeSession.Enabled||!session.business.ledger.OneDay||session.business.ledger.State!=BusinessState.Open)return;
  postTrip=true;RefreshPostTrip();nextPostTripRefresh=Time.unscaledTime+2;
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
  if(!postTrip||kind<0||kind>3||session.jobs.Active||session.paused||session.business.ledger.State!=BusinessState.Open)return false;
  RefreshPostTrip();if(float.IsInfinity(postTripDistances[kind]))return false;
  // Explicit route choice only; no popup, payment, rest or shift closing is performed here.
  if(kind==0&&postTripFuel>=0){session.jobs.nav.SetDestination(Stations[postTripFuel].approach);destination=Stations[postTripFuel].name+" · 입구부터 수동 진입 · 정차 후 F7";}
  else if(kind==3&&RestArea!=null&&ServiceDistance(RestArea.approach)<postTripDistances[2]){session.jobs.nav.SetDestination(RestArea.approach);destination="동부 휴식 주차구역 · 입구부터 수동 진입 · 정차 후 F7";}
  else {int stop=kind==3?2:kind;session.jobs.nav.SetDestination(StopFor(stop));destination=stop==1&&Restaurant!=null?"식당 입구 앞까지 안내 · H 해제 후 주차 칸에 진입 · 정차 후 F7":(kind==3?"수면 · ":"")+names[stop]+" · 오른쪽 정차 화살표에서 F7";}
  DismissPostTrip();return true;
 }
 void DrawPostTrip(GUIStyle style,GUIStyle button){
  if(!postTrip||session.paused||session.FullMapOpen||session.PracticeMenuOpen||session.PracticeBusy||session.ChoiceOpen||session.business.restartPrompt||session.business.taxPrompt||session.business.ShowingSummary||session.business.preparing||session.law.HistoryOpen)return;
  float scale=CockpitUI.Scale,width=610*scale,x=365*scale,y=110*scale;
  GUI.depth=-20;GUI.color=new Color(.04f,.09f,.12f,1);GUI.DrawTexture(new Rect(x,y,width,80),Texture2D.whiteTexture);GUI.color=Color.white;
  string[] labels={session.jobs.NeedsWash?"주유소(세차 필요)":"주유","식사","귀가 주차장","수면"};
  string reason=recommended==0?(session.jobs.NeedsWash?"청결 30 미만 · 세차 필요":"연료 15% 이하"):recommended==1?"포만감 25 미만":recommended==3?"피로 70 이상":recommended==2?"마감까지 3분 이내":"현재 컨디션 양호";
  GUI.Label(new Rect(x+10,y+2,width-20,28),"운송 후 생활 거점 · "+(recommended<0?"계속 영업 가능":labels[recommended]+" 권장"),new GUIStyle(style){fontSize=14});
  float cell=(width-20)/5;
  for(int i=0;i<4;i++){
   GUI.enabled=!session.jobs.Active&&!float.IsInfinity(postTripDistances[i]);
   string distance=float.IsInfinity(postTripDistances[i])?"경로 없음":postTripDistances[i]>=1000?(postTripDistances[i]/1000).ToString("0.0")+"km":postTripDistances[i].ToString("0")+"m";
   if(GUI.Button(new Rect(x+10+i*cell,y+30,cell-5,32),(recommended==i?"★ ":"")+labels[i]+" · "+distance,button))GuideAfterTrip(i);
  }
  GUI.enabled=true;if(GUI.Button(new Rect(x+10+4*cell,y+30,cell-5,32),"계속 영업 · 닫기",button))DismissPostTrip();
 }
 public void Toggle(){
  if(open){Close();return;}
  if(session.FullMapOpen||session.PracticeMenuOpen||session.PracticeBusy||session.business.restartPrompt||PracticeSession.Enabled||session.business.preparing||session.law.HistoryOpen||session.business.ShowingSummary||session.business.taxPrompt||(session.business.ledger.OneDay?session.jobs.trip.State==TripState.OnTrip:session.jobs.Active)||session.taxi.body.velocity.magnitude>=.56f){notice="빈차로 안전하게 정차한 뒤 F7";noticeUntil=Time.unscaledTime+4;return;}
  pending=-1;pendingFuel=-1;open=true;session.paused=true;Time.timeScale=0;
 }
 void Close(){pending=-1;pendingFuel=-1;open=false;DismissPostTrip();session.paused=false;Time.timeScale=1;}
 public bool GuideFuel(int station){
  if(!open||station<0||station>=Stations.Count||session.jobs.Active)return false;
  session.jobs.nav.SetDestination(Stations[station].approach);destination=Stations[station].name+" · 입구 앞까지 내비/H 안내 · 내부는 H 해제 후 수동 진입";Close();return true;
 }
 public bool Guide(int kind){
  if(!open||kind<0||kind>3||!HasService(kind)||session.jobs.trip.State==TripState.OnTrip)return false;
  if(session.jobs.Active){if(!session.business.ledger.OneDay)return false;session.jobs.ClearForDay();}
  if(kind==3&&RestArea!=null){var taxi=session.taxi;var graph=session.jobs.nav.hd;float restDistance=PassengerJobs.DirectedDistance(graph,taxi.transform.position,taxi.transform.forward,RestArea.approach);float baseDistance=PassengerJobs.DirectedDistance(graph,taxi.transform.position,taxi.transform.forward,StopFor(2));if(restDistance<baseDistance){session.jobs.nav.SetDestination(RestArea.approach);destination="동부 휴식 주차구역 · 입구부터 수동 진입 · 파란 정차선에서 F7";Close();return true;}}
  if(kind==3)kind=2;
  if(kind==0&&Stations.Count>0){int closest=0;float best=float.MaxValue;for(int i=0;i<Stations.Count;i++){float distance=(Stations[i].entrance-session.taxi.transform.position).sqrMagnitude;if(distance<best){best=distance;closest=i;}}return GuideFuel(closest);}
  session.jobs.nav.SetDestination(StopFor(kind));destination=kind==1&&Restaurant!=null?"식당 입구 앞까지 안내 · H 해제 후 주차 칸에 진입 · 정차 후 F7":names[kind]+" · 오른쪽 정차 화살표에서 F7";Close();return true;
 }
 public bool RequestAction(int kind){if(session.FullMapOpen||!open||!CanUse(kind))return false;pending=kind;pendingFuel=kind==0||kind==4?FuelAtPump:-1;return true;}
 public bool ConfirmAction(bool accept){
  if(!open||pending<0)return false;int kind=pending,station=pendingFuel;pending=-1;pendingFuel=-1;
  if(!accept){notice="취소했습니다 · 결제/시간 차감 없음";noticeUntil=Time.unscaledTime+4;return false;}
  if(session.FullMapOpen||!CanUse(kind)||(kind==0||kind==4)&&Stations.Count>0&&FuelAtPump!=station)return false;
  if(kind<2||kind==3||kind==4)return Buy(kind);Close();return session.business.RequestEndDay();
 }
 public bool Buy(int kind){
  if(kind<0||kind==2||kind>4||session.FullMapOpen||!open||!CanUse(kind))return false;var b=session.business;var a=b.ledger.Account;int day=b.ledger.Day;
  if(kind==4){if(!b.ledger.UseWash(session.law.ids.Next("wash"))){notice="서비스를 이용할 수 없습니다";noticeUntil=Time.unscaledTime+5;return false;}notice="세차·실내 청소 완료 · 청결 100 / 30초 경과 · 2,000원";noticeUntil=Time.unscaledTime+5;destination="";b.SaveCheckpoint();Close();return true;}
  if(b.ledger.OneDay&&(kind==1||kind==3)){int seconds=kind==1?50:100;bool done=kind==1?b.ledger.UseMeal(session.law.ids.Next("meal")):b.ledger.UseSleep(session.law.ids.Next("sleep"));if(!done){notice="서비스를 이용할 수 없습니다";noticeUntil=Time.unscaledTime+5;return false;}if(session.jobs.Active)session.jobs.ClearForDay();b.SaveCheckpoint();notice=(kind==1?"식사":"수면")+" 완료 · "+seconds+"초 경과";noticeUntil=Time.unscaledTime+4;Close();return true;}
  bool ok=kind==0?a.Refuel(session.law.ids.Next("fuel"),FuelAmount,day):a.Meal(session.law.ids.Next("meal"),day);
  if(ok){b.ledger.Advance(10,0);b.SaveCheckpoint();notice="이용 완료 · 영업시간 10초 경과";noticeUntil=Time.unscaledTime+4;destination="";if(kind!=0)Close();}else notice="현금·연료 잔량 또는 오늘 이용 여부를 확인하세요";return ok;
 }
 void Row(float x,float y,string label,int kind,int station,GUIStyle style,GUIStyle button){
  GUI.Label(new Rect(x+20,y,330,51),label,style);
  GUI.enabled=HasService(kind==4?0:kind);if(GUI.Button(new Rect(x+360,y,105,42),"길 안내",button)){if(station>=0)GuideFuel(station);else Guide(kind==4?0:kind);}
  GUI.enabled=CanUse(kind)&&(station<0||FuelAtPump==station)&&(kind!=0||FuelAmount>.001f);
  if(GUI.Button(new Rect(x+480,y,220,42),kind==0?"+"+FuelAmount.ToString("0.#")+"% 주유 · "+Mathf.CeilToInt(FuelAmount*session.business.settings.fuelUnitPrice)+"원":kind==4?"세차·실내 청소 · 2,000원":kind==1?"식사 비용 확인":kind==3?"수면 · 100초":"휴식·영업 종료",button)){if(kind==0)Buy(0);else RequestAction(kind);}GUI.enabled=true;
 }
 void OnGUI(){
  if(session.business.ledger.Account==null||PracticeSession.Enabled)return;
  var style=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=16,wordWrap=true};var button=new GUIStyle(GUI.skin.button){font=KoreanText.Font,fontSize=14,wordWrap=true};
  style.normal.textColor=Color.white;
  if(!open){if(!session.paused&&!session.business.restartPrompt&&!session.GetComponent<DriveHUD>().HelpOpen&&!session.FullMapOpen&&!session.ChoiceOpen&&!session.RideResultOpen&&!session.PracticeMenuOpen&&!session.business.ShowingSummary&&!session.business.preparing&&!session.law.HistoryOpen){if(session.ShowHints||notice!=""||destination!=""){var hint=CockpitUI.ServiceHintBounds(Screen.width,Screen.height);KoreanText.Panel(hint);var hintStyle=new GUIStyle(style){font=KoreanText.BoldFont,fontSize=Mathf.Max(13,Mathf.RoundToInt(16*CockpitUI.Scale)),alignment=TextAnchor.UpperLeft};var msg="F7 생활 거점"+(notice==""?"":"\n"+notice)+(destination==""?"":"\n"+destination);GUI.Label(new Rect(hint.x+10,hint.y+8,hint.width-20,hint.height-16),msg,hintStyle);}DrawPostTrip(style,button);}return;}

  GUI.depth=-110;float height=Stations.Count>0?687:572,x=(Screen.width-720)/2f,y=(Screen.height-height)/2f;
  GUI.color=new Color(.04f,.09f,.12f,1);GUI.DrawTexture(new Rect(x,y,720,height),Texture2D.whiteTexture);GUI.color=Color.white;
  var b=session.business;var a=b.ledger.Account;GUI.Label(new Rect(x+20,y+15,680,55),"생활 거점 (메뉴 동안 시간 정지)\n현금 "+a.Cash.ToString("N0")+"원 · 미납 "+a.Debt.ToString("N0")+"원 · 연료 "+a.Fuel.ToString("0.0")+"%",style);
  if(pending>=0){GUI.Label(new Rect(x+30,y+110,660,160),(pending==1?MealName+" · ":"")+Quote(pending)+"\n\n확정할 때만 처리합니다. 식사 부족금은 생활비 미납으로 기록합니다.",style);
   if(pending==1){for(int i=0;i<mealMenu.Length;i++)if(GUI.Button(new Rect(x+30+i*220,y+285,210,48),(SelectedMeal==i?"✓ ":"")+mealMenu[i],button))SelectMeal(i);GUI.Label(new Rect(x+30,y+340,660,50),"기사식당 프로토 메뉴 · 모두 3,000원 / 50초 / 포만감 +70 (최대 100)",style);}
   if(GUI.Button(new Rect(x+30,y+height-130,310,50),"확정",button))ConfirmAction(true);if(GUI.Button(new Rect(x+380,y+height-130,310,50),"취소",button))ConfirmAction(false);return;}
  int row=0;
  if(Stations.Count>0){for(int i=0;i<Stations.Count;i++)Row(x,y+85+row++*62,Stations[i].name,0,i,style,button);}else Row(x,y+85+row++*62,names[0]+" · "+session.jobs.stops[indices[0]].name,0,-1,style,button);
  Row(x,y+85+row++*62,"주유소 세차·실내 청소\n청결 100 회복 / 30초",4,-1,style,button);
  for(int i=1;i<3;i++)Row(x,y+85+row++*62,(i==1?"식당":"주차장·휴식 거점")+" · "+StopFor(i).name,i,-1,style,button);
  if(b.ledger.OneDay)Row(x,y+85+row++*62,"수면 · 기존 주차장 / 동부 휴식구역\n무료 / 피로 −40",3,-1,style,button);
  GUI.Label(new Rect(x+20,y+height-155,680,108),notice+"\n"+(Stations.Count>0?"주유소는 빈 부지에 배치한 게임시설입니다. 내비/H는 도로 입구 앞까지 안내합니다. H 해제 후 화살표를 따라 수동 진입 → 주유 구역에 같은 방향으로 정차 → F7 → 별도 출구. 도로변에서는 주유할 수 없습니다.":"주유소·식당·주차장은 임시 배치된 생활 거점입니다."),new GUIStyle(style){fontSize=14});
  if(GUI.Button(new Rect(x+20,y+height-45,680,35),"취소·주행으로 돌아가기 (F7)",button))Close();
 }
}
