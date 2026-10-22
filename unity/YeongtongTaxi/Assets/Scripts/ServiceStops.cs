using System.Collections.Generic;
using UnityEngine;

public class ServiceStops : MonoBehaviour {
 public bool PostTripVisible {get{return postTrip;}}
 public DriveSession session;public bool open;public string notice="";public int pending=-1;public string destination="";
 float noticeUntil;int pendingFuel=-1;public float FuelAmount {get{return Mathf.Min(10,100-session.business.ledger.Account.Fuel);}}
 // Legacy regional references; Eungye meal has a dedicated stop. Retained for meal/parking and old regional stop references. Eungye fuel uses actual pump bays.
 public readonly int[] indices={3,6,9};
 readonly string[] names={"임시 주유소","임시 식당","임시 주차장"};
 int observedCompleted,observedFailed;bool postTrip;int recommended=-1,postTripFuel=-1;float nextPostTripRefresh;
 readonly float[] postTripDistances=new float[4];
 public bool PostTripOpen {get{return postTrip;}}
 public int PostTripRecommendation {get{return recommended;}}
 public IReadOnlyList<FuelStationLayout.FuelStation> Stations {get{return FuelStationLayout.For(session.world.map);}}
 public int FuelAtPump {get{var taxi=session.taxi;for(int i=0;i<Stations.Count;i++)if(Stations[i].CanRefuel(taxi.transform.position,taxi.transform.forward,taxi.body.velocity.magnitude))return i;return -1;}}
 public FuelStationLayout.FuelStation RestArea {get{return FuelStationLayout.Rest(session.world.map);}}
 MapData mealMap;MapStop mealStop;
 public MapStop MealStop {get{var map=session.world.map;if(!ReferenceEquals(mealMap,map)||mealStop==null){mealMap=map;mealStop=CreateMealStop(map);}return mealStop;}}
 public MapStop StopFor(int kind){return kind==1?MealStop:session.jobs.stops[indices[kind==3?2:kind]];}
 public static MapStop CreateMealStop(MapData map){
  if(map.mapId!="eungye"||!map.HasHd)return map.stops[6%map.stops.Length];
  var graph=new HdRoadGraph(map);var target=new Vector3(688,0,326);int best=-1;float bestAt=0,error=float.MaxValue;
  for(int i=0;i<graph.edges.Length;i++){var e=graph.edges[i];var path=graph.paths[i];if(e.disabled||!e.id.StartsWith("local:")||e.turn!=0||e.change!=0||path.length<35)continue;float ignored;float at=Mathf.Clamp(path.Project(target,out ignored),12,path.length-12);float d=(path.Position(at)-target).sqrMagnitude;if(d<error){error=d;best=i;bestAt=at;}}
  if(best<0||error>80*80)throw new System.InvalidOperationException("Restaurant approach missing from south Eungye road graph");
  var pos=graph.paths[best].Position(bestAt);var dir=graph.paths[best].Tangent(bestAt);return new MapStop{name="은계 남부 식당 (게임시설)",x=pos.x,z=pos.z,elevation=pos.y,dx=dir.x,dz=dir.z,width=3,graphStop=true,laneEdge=best,laneAt=bestAt,hasPassengerAnchor=true,passengerAnchor=pos+TrafficSystem.Right(dir)*6+Vector3.up*.2f};
 }
 public bool AtRestArea(){var r=RestArea;var t=session.taxi;return r!=null&&r.CanRefuel(t.transform.position,t.transform.forward,t.body.velocity.magnitude);}
 public bool AtFuelPump(){return Stations.Count>0?FuelAtPump>=0:session.world.map.mapId!="eungye"&&session.jobs.At(indices[0]);}
 public bool CanUse(int kind){bool daily=session.business.ledger.OneDay;return kind>=0&&kind<=(daily?3:2)&&session.business.ledger.State==BusinessState.Open&&(daily&&kind>0?session.jobs.trip.State!=TripState.OnTrip:!session.jobs.Active)&&session.taxi.body.velocity.magnitude<.56f&&(kind==0?AtFuelPump():kind==3?(AtRestArea()||session.jobs.At(indices[2])):StopFor(kind).CanStop(session.taxi.transform.position,session.taxi.transform.forward,session.taxi.body.velocity.magnitude));}
 public string Quote(int kind){if(kind<0||kind>3)return "";var b=session.business;var a=b.ledger.Account;if(b.ledger.OneDay&&(kind==1||kind==3)){var preview=b.ledger.Driver.Copy();if(kind==1)preview.Meal();else preview.Sleep();return (kind==1?"식사 3,000원 · 50초 (부족분은 미납)":"수면 무료 · 100초")+"\n포만감 "+preview.satiety.ToString("0")+" · 피로 "+preview.fatigue.ToString("0")+" 예상\n미탑승 호출은 취소됩니다. 엔진 정지 · 점수 변화 없음";}return kind==0?"주유 "+FuelAmount.ToString("0.0")+"% · "+Mathf.CeilToInt(FuelAmount*session.business.settings.fuelUnitPrice).ToString("N0")+"원 / 10초":kind==1?"식사 3,000원 / 10초 (하루 1회)":"주차·영업 마감 · 하루 주차비 500원";}
 void Start(){
  if(session.business.ledger.Account==null){enabled=false;return;}
  observedCompleted=session.jobs.completed;observedFailed=session.jobs.failed;
  if(session.world.map.mapId=="eungye")indices[2]=0;
  for(int i=0;i<indices.Length;i++){
   indices[i]%=session.jobs.stops.Length;if(i==0&&Stations.Count>0)continue;
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
  postTripDistances[1]=ServiceDistance(MealStop);
  postTripDistances[2]=ServiceDistance(session.jobs.stops[indices[2]]);
  postTripDistances[3]=RestArea==null?postTripDistances[2]:Mathf.Min(postTripDistances[2],ServiceDistance(RestArea.approach));
  var ledger=session.business.ledger;recommended=PostTripServicePolicy.Recommend(ledger.Account.Fuel,ledger.Driver.satiety,ledger.Driver.fatigue,ledger.SecondsLeft,postTripDistances);
 }
 public void DismissPostTrip(){postTrip=false;}
 public bool GuideAfterTrip(int kind){
  if(!postTrip||kind<0||kind>3||session.jobs.Active||session.paused||session.business.ledger.State!=BusinessState.Open)return false;
  RefreshPostTrip();if(float.IsInfinity(postTripDistances[kind]))return false;
  // Explicit route choice only; no popup, payment, rest or shift closing is performed here.
  if(kind==0&&postTripFuel>=0){session.jobs.nav.SetDestination(Stations[postTripFuel].approach);destination=Stations[postTripFuel].name+" · 입구부터 수동 진입 · 정차 후 F7";}
  else if(kind==3&&RestArea!=null&&ServiceDistance(RestArea.approach)<postTripDistances[2]){session.jobs.nav.SetDestination(RestArea.approach);destination="동부 휴식 주차구역 · 입구부터 수동 진입 · 정차 후 F7";}
  else {int stop=kind==3?2:kind;session.jobs.nav.SetDestination(StopFor(stop));destination=(kind==3?"수면 · ":"")+names[stop]+" · 오른쪽 정차 화살표에서 F7";}
  DismissPostTrip();return true;
 }
 void DrawPostTrip(GUIStyle style,GUIStyle button){
  if(!postTrip||session.paused||session.FullMapOpen||session.PracticeMenuOpen||session.PracticeBusy||session.ChoiceOpen||session.business.restartPrompt||session.business.taxPrompt||session.business.ShowingSummary||session.business.preparing||session.law.HistoryOpen)return;
  float width=Mathf.Min(790,Screen.width-32),x=16,y=Mathf.Max(326,Screen.height-394);
  GUI.depth=-20;GUI.color=new Color(.04f,.09f,.12f,.96f);GUI.DrawTexture(new Rect(x,y,width,70),Texture2D.whiteTexture);GUI.color=Color.white;
  string[] labels={"주유","식사","귀가 주차장","수면"};
  string reason=recommended==0?"연료 15% 이하":recommended==1?"포만감 25 미만":recommended==3?"피로 70 이상":recommended==2?"마감까지 3분 이내":"현재 컨디션 양호";
  GUI.Label(new Rect(x+10,y+2,width-20,24),"운송 후 생활 거점 · "+(recommended<0?"계속 영업 가능":labels[recommended]+" 권장")+" ("+reason+") · 선택 시에만 길 안내",style);
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
  if(session.FullMapOpen||session.PracticeMenuOpen||session.PracticeBusy||session.business.restartPrompt||PracticeSession.Enabled||session.business.preparing||session.law.HistoryOpen||session.business.ShowingSummary||session.business.taxPrompt||(session.business.ledger.OneDay?session.jobs.trip.State==TripState.OnTrip:session.jobs.Active)||session.taxi.body.velocity.magnitude>=.56f){notice="빈차로 안전하게 정차한 뒤 F7";return;}
  pending=-1;pendingFuel=-1;open=true;session.paused=true;Time.timeScale=0;
 }
 void Close(){pending=-1;pendingFuel=-1;open=false;DismissPostTrip();session.paused=false;Time.timeScale=1;}
 public bool GuideFuel(int station){
  if(!open||station<0||station>=Stations.Count||session.jobs.Active)return false;
  session.jobs.nav.SetDestination(Stations[station].approach);destination=Stations[station].name+" · 입구 앞까지 내비/H 안내 · 내부는 H 해제 후 수동 진입";Close();return true;
 }
 public bool Guide(int kind){
  if(!open||kind<0||kind>3||session.jobs.trip.State==TripState.OnTrip)return false;
  if(session.jobs.Active){if(!session.business.ledger.OneDay)return false;session.jobs.ClearForDay();}
  if(kind==3&&RestArea!=null){var taxi=session.taxi;var graph=session.jobs.nav.hd;float restDistance=PassengerJobs.DirectedDistance(graph,taxi.transform.position,taxi.transform.forward,RestArea.approach);float baseDistance=PassengerJobs.DirectedDistance(graph,taxi.transform.position,taxi.transform.forward,session.jobs.stops[indices[2]]);if(restDistance<baseDistance){session.jobs.nav.SetDestination(RestArea.approach);destination="동부 휴식 주차구역 · 입구부터 수동 진입 · 파란 정차선에서 F7";Close();return true;}}
  if(kind==3)kind=2;
  if(kind==0&&Stations.Count>0){int closest=0;float best=float.MaxValue;for(int i=0;i<Stations.Count;i++){float distance=(Stations[i].entrance-session.taxi.transform.position).sqrMagnitude;if(distance<best){best=distance;closest=i;}}return GuideFuel(closest);}
  session.jobs.nav.SetDestination(StopFor(kind));destination=names[kind]+" · 오른쪽 정차 화살표에서 F7";Close();return true;
 }
 public bool RequestAction(int kind){if(session.FullMapOpen||!open||!CanUse(kind))return false;pending=kind;pendingFuel=kind==0?FuelAtPump:-1;return true;}
 public bool ConfirmAction(bool accept){
  if(!open||pending<0)return false;int kind=pending,station=pendingFuel;pending=-1;pendingFuel=-1;
  if(!accept){notice="취소했습니다 · 결제/시간 차감 없음";noticeUntil=Time.unscaledTime+4;return false;}
  if(session.FullMapOpen||!CanUse(kind)||kind==0&&Stations.Count>0&&FuelAtPump!=station)return false;
  if(kind<2||kind==3)return Buy(kind);Close();return session.business.RequestEndDay();
 }
 public bool Buy(int kind){
  if(kind<0||kind==2||kind>3||session.FullMapOpen||!open||!CanUse(kind))return false;var b=session.business;var a=b.ledger.Account;int day=b.ledger.Day;
  if(b.ledger.OneDay&&(kind==1||kind==3)){int seconds=kind==1?50:100;bool done=kind==1?b.ledger.UseMeal(session.law.ids.Next("meal")):b.ledger.UseSleep(session.law.ids.Next("sleep"));if(!done){notice="남은 영업시간 부족 · 식사 50초 / 수면 100초 필요";noticeUntil=Time.unscaledTime+5;return false;}if(session.jobs.Active)session.jobs.ClearForDay();notice=(kind==1?"식사":"수면")+" 완료 · "+seconds+"초 경과";noticeUntil=Time.unscaledTime+4;Close();return true;}
  bool ok=kind==0?a.Refuel(session.law.ids.Next("fuel"),FuelAmount,day):a.Meal(session.law.ids.Next("meal"),day);
  if(ok){b.ledger.Advance(10,0);notice="이용 완료 · 영업시간 10초 경과";noticeUntil=Time.unscaledTime+4;destination="";if(kind!=0)Close();}else notice="현금·연료 잔량 또는 오늘 이용 여부를 확인하세요";return ok;
 }
 void Row(float x,float y,string label,int kind,int station,GUIStyle style,GUIStyle button){
  GUI.Label(new Rect(x+20,y,330,51),label,style);
  if(GUI.Button(new Rect(x+360,y,105,42),"길 안내",button)){if(station>=0)GuideFuel(station);else Guide(kind);}
  GUI.enabled=CanUse(kind)&&(station<0||FuelAtPump==station)&&(kind!=0||FuelAmount>.001f);
  if(GUI.Button(new Rect(x+480,y,220,42),kind==0?"+"+FuelAmount.ToString("0.#")+"% 주유 · "+Mathf.CeilToInt(FuelAmount*session.business.settings.fuelUnitPrice)+"원":kind==1?"식사 비용 확인":kind==3?"수면 · 100초":"주차·영업 마감",button)){if(kind==0)Buy(0);else RequestAction(kind);}GUI.enabled=true;
 }
 void OnGUI(){
  if(session.business.ledger.Account==null||PracticeSession.Enabled)return;
  var style=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=16,wordWrap=true};var button=new GUIStyle(GUI.skin.button){font=KoreanText.Font,fontSize=14,wordWrap=true};
  if(!open){if((session.ShowHints||notice!=""||destination!="")&&!session.business.ShowingSummary&&!session.business.preparing&&!session.law.HistoryOpen)GUI.Label(new Rect(420,94,Mathf.Max(500,Screen.width-440),58),"F7 거점 · "+notice+(destination==""?"":"\n"+destination),style);DrawPostTrip(style,button);return;}
  GUI.depth=-110;float height=Stations.Count>0?625:510,x=(Screen.width-720)/2f,y=(Screen.height-height)/2f;
  GUI.color=new Color(.04f,.09f,.12f,1);GUI.DrawTexture(new Rect(x,y,720,height),Texture2D.whiteTexture);GUI.color=Color.white;
  var b=session.business;var a=b.ledger.Account;GUI.Label(new Rect(x+20,y+15,680,55),"생활 거점 (메뉴 동안 시간 정지)\n현금 "+a.Cash.ToString("N0")+"원 · 미납 "+a.Debt.ToString("N0")+"원 · 연료 "+a.Fuel.ToString("0.0")+"%",style);
  if(pending>=0){GUI.Label(new Rect(x+30,y+110,660,160),Quote(pending)+"\n\n확정할 때만 처리합니다. 식사 부족금은 생활비 미납으로 기록합니다.",style);if(GUI.Button(new Rect(x+30,y+height-130,310,50),"확정",button))ConfirmAction(true);if(GUI.Button(new Rect(x+380,y+height-130,310,50),"취소",button))ConfirmAction(false);return;}
  int row=0;
  if(Stations.Count>0){for(int i=0;i<Stations.Count;i++)Row(x,y+85+row++*62,Stations[i].name,0,i,style,button);}else Row(x,y+85+row++*62,names[0]+" · "+session.jobs.stops[indices[0]].name,0,-1,style,button);
  for(int i=1;i<3;i++)Row(x,y+85+row++*62,(i==1?"식당":names[i])+" · "+StopFor(i).name,i,-1,style,button);
  if(b.ledger.OneDay)Row(x,y+85+row++*62,"수면 · 기존 주차장 / 동부 휴식구역\n무료 / 피로 −40",3,-1,style,button);
  GUI.Label(new Rect(x+20,y+height-155,680,108),notice+"\n"+(Stations.Count>0?"주유소는 빈 부지에 배치한 게임시설입니다. 내비/H는 도로 입구 앞까지 안내합니다. H 해제 후 화살표를 따라 수동 진입 → 주유 구역에 같은 방향으로 정차 → F7 → 별도 출구. 도로변에서는 주유할 수 없습니다.":"주유소·식당·주차장은 임시 배치된 생활 거점입니다."),style);
  if(GUI.Button(new Rect(x+20,y+height-45,680,35),"취소·주행으로 돌아가기 (F7)",button))Close();
 }
}
