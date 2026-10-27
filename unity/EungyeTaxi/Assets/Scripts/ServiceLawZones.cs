using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public sealed class ServiceLawZonesSnapshot {public string scenarioId,assignedId,lastTitle;public bool pickupPhase,completed,hasObservation,hasFeedback;public int sequence;public PracticeFeedback lastFeedback;public ConfusionLiveSnapshot observation;}
public sealed class ServiceLawZone {
 public string id;public int variant,incoming,connector,outgoing;public TrafficJunction junction;
 public string Title {get{return ConfusionPracticeState.Title(variant);}}
 public string MapTitle {get{switch(variant){case 8:return "적색 점멸\n정지 후 진행";case 9:return "황색 점멸\n주변 확인";case 10:return "녹색\n출구 정체";case 11:return "녹색\n출구 여유";case 12:return "비보호 좌회전\n적색 대기";case 13:return "비보호 좌회전\n대향차 양보";default:return "비보호 좌회전\n대향차 없음";}}}
}
// Persistent places in the normal Eungye world. Situations observe ordinary trips; they never add routing waypoints.
public sealed class ServiceLawZones : MonoBehaviour {
 public DriveSession session;
 public readonly List<ServiceLawZone> zones=new List<ServiceLawZone>();
 public readonly List<PrototypeScenario> scenarios=new List<PrototypeScenario>();
 public PrototypeScenario CurrentScenario {get;private set;}public bool PickupPhase {get;private set;}
 public ServiceLawZone Assigned {get;private set;}
 public ConfusionPractice Live {get;private set;}
 public PracticeFeedback LastFeedback {get;private set;}
 public string LastTitle {get;private set;}public bool Completed {get;private set;}
 public bool Ready {get;private set;}int sequence;float toastUntil;bool details;Vector2 scroll;
 public ServiceLawZonesSnapshot CaptureLive(){var state=Live?Live.CaptureLive():null;return new ServiceLawZonesSnapshot{scenarioId=CurrentScenario==null?null:CurrentScenario.id,assignedId=Assigned==null?null:Assigned.id,lastTitle=LastTitle,pickupPhase=PickupPhase,completed=Completed,sequence=sequence,lastFeedback=LastFeedback,hasFeedback=LastFeedback!=null,hasObservation=state!=null,observation=state};}
 public void PrepareForRestore(){if(Live&&Live.Active)Live.Stop();var choice=GetComponent<ScenarioChoiceSituation>();if(choice)choice.Stop();Assigned=null;Completed=false;}
 public void RestoreLive(ServiceLawZonesSnapshot s){if(s==null)return;if(!Ready||s.sequence<0)throw new ArgumentException("Invalid live service zones");CurrentScenario=string.IsNullOrEmpty(s.scenarioId)?null:scenarios.Find(x=>x.id==s.scenarioId);Assigned=string.IsNullOrEmpty(s.assignedId)?null:zones.Find(x=>x.id==s.assignedId);if(!string.IsNullOrEmpty(s.scenarioId)&&CurrentScenario==null||!string.IsNullOrEmpty(s.assignedId)&&Assigned==null)throw new ArgumentException("Saved service zone removed");PickupPhase=s.pickupPhase;Completed=s.completed;sequence=s.sequence;LastTitle=s.lastTitle;LastFeedback=s.hasFeedback?s.lastFeedback:null;toastUntil=Time.time+(LastFeedback==null?0:12);if(s.hasObservation)Live.RestoreLive(s.observation,Assigned);}
 public bool Owns(TrafficJunction j){return j.editorProgram==null&&Live&&Live.Owns(j);}
 public bool RequiredBraking(){
  if(!Live||!Live.Active)return false;var taxi=session.taxi;var front=taxi.transform.position+taxi.transform.forward*2;var q=front-Live.Junction.center;float gap=Live.StopAlong-Vector3.Dot(q,Live.Direction),side=Vector3.Dot(q,TrafficSystem.Right(Live.Direction));
  if(Live.State.Left&&Live.State.Oncoming&&Vector3.Distance(front,Live.Junction.center)<Live.Junction.StopDistance(Live.Axis)+5)return true;
  return !Live.State.Entered&&Vector3.Dot(taxi.transform.forward,Live.Direction)>.8f&&side>.15f&&side<Live.Junction.halfWidth[Live.Axis]&&gap>=-2&&gap<=Mathf.Max(5,taxi.body.velocity.magnitude*2+4)&&(Live.State.Blocked||Live.State.Oncoming||Live.State.Signal==SignalColor.Red);
 }
 void Start(){
  if(PracticeSession.Enabled||!session.world.map.hybrid||session.world.map.mapId!="eungye")return;
  Register();Ready=zones.Count>0;session.jobs.Generate();
 }
 public void Register(){
  if(zones.Count>0)return;
  var hd=session.jobs.nav.hd;scenarios.AddRange(PrototypeScenario.Build(hd,session.jobs.stops));
  foreach(var course in scenarios){if(course.variant==0||zones.Exists(z=>z.connector==course.connector))continue;
   int k=course.route.edges.IndexOf(course.connector);var j=session.traffic.junctions.Find(x=>x.name==hd.edges[course.connector].ruleId);if(j==null||!j.signalized)continue;
   var zone=new ServiceLawZone{id="eungye-law-"+course.variant+"-"+course.connector,variant=course.variant,junction=j,incoming=course.route.edges[k-1],connector=course.connector,outgoing=course.route.edges[k+1]};zones.Add(zone);CreateSign(zone);
  }
  var obj=new GameObject("은계 영업 도로 상황 운용");obj.transform.SetParent(transform);Live=obj.AddComponent<ConfusionPractice>();Live.session=session;
 }
 void CreateSign(ServiceLawZone zone){
  var hd=session.jobs.nav.hd;var path=hd.paths[zone.incoming];var point=path.Position(Mathf.Max(0,path.length-40));var dir=path.Tangent(Mathf.Max(0,path.length-40));
  var obj=new GameObject(zone.id+" · 창작 상황 구역");obj.transform.SetParent(transform);obj.transform.position=point+TrafficSystem.Right(dir)*(zone.junction.halfWidth[0]+4)+Vector3.up*3;
  var t=obj.AddComponent<TextMesh>();t.fontSize=38;t.characterSize=.09f;t.anchor=TextAnchor.MiddleCenter;t.color=new Color(1,.75f,.25f);t.text="운행 상황 구역 · 창작\n"+zone.Title;KoreanText.Apply(t);obj.AddComponent<FaceCamera>();
 }
 public ServiceLawZone ZoneFor(JobOffer offer){return offer!=null&&offer.zoneIndex>=0&&offer.zoneIndex<zones.Count?zones[offer.zoneIndex]:null;}
 public string OfferSituation(JobOffer offer){var zone=ZoneFor(offer);return zone==null?"일반 운송 · 지정 상황 구역 없음":"경유 상황: "+zone.Title;}
 public void ConfigureOffer(JobOffer offer){
  if(!Ready||!session.jobs.stops[offer.pickup].graphStop||!session.jobs.stops[offer.drop].graphStop)return;
  var from=session.jobs.stops[offer.pickup];var to=session.jobs.stops[offer.drop];var hd=session.jobs.nav.hd;
  for(int k=0;k<zones.Count;k++){
   int index=(sequence+k)%zones.Count;var zone=zones[index];var route=hd.Route(from.laneEdge,from.laneAt,to.laneEdge,to.laneAt);
   if(!PrototypeScenario.Valid(route,zone.connector))continue;
   offer.zoneIndex=index;offer.distance=route.path.length;offer.waitSeconds=35;
   foreach(int e in route.edges)if(hd.edges[e].id.StartsWith("join:"))foreach(var j in session.traffic.junctions)if(j.signalized&&j.name==hd.edges[e].ruleId){offer.waitSeconds+=15;break;}
   sequence=(index+1)%zones.Count;return;
  }
 }
 public bool ConfigureScenario(JobOffer offer,PrototypeScenario scenario){
  if(!Ready||scenario==null)return false;var hd=session.jobs.nav.hd;var stops=session.jobs.stops;
  var p=stops[scenario.pickup];var d=stops[scenario.drop];float at;int start=hd.Nearest(session.taxi.transform.position,session.taxi.transform.forward,out at);
  var pickupRoute=hd.Route(start,at,p.laneEdge,p.laneAt);
  bool alreadyAtPickup=start==p.laneEdge&&Mathf.Abs(at-p.laneAt)<.25f;
  if(pickupRoute==null&&!alreadyAtPickup)return false;
  var delivery=scenario.route;if(delivery==null)return false;
  int zone=zones.FindIndex(z=>z.connector==scenario.connector);if(scenario.variant>0&&zone<0)return false;
  offer.pickup=scenario.pickup;offer.drop=scenario.drop;offer.scenarioId=scenario.id;offer.zoneIndex=zone;
  offer.distance=delivery.path.length;offer.waitSeconds=50;
  offer.pickupSeconds=Mathf.Max(180,(alreadyAtPickup?0:pickupRoute.path.length)/4+120);return true;
 }
 public void BeginPickup(JobOffer offer){
  EndTrip();CurrentScenario=scenarios.Find(s=>s.id==offer.scenarioId);PickupPhase=false;Completed=false;LastFeedback=null;LastTitle="";session.jobs.nav.RequireVia(-1);
 }
 public void BeginTrip(JobOffer offer){
  EndTrip();CurrentScenario=scenarios.Find(s=>s.id==offer.scenarioId);PickupPhase=false;Assigned=ZoneFor(offer);Completed=false;LastFeedback=null;LastTitle="";
  session.jobs.nav.ObserveEdge(Assigned==null?-1:Assigned.connector);
  if(CurrentScenario!=null&&(CurrentScenario.id=="PS08"||CurrentScenario.id=="PS09")){
   var course=GetComponent<ScenarioChoiceSituation>();if(!course)course=gameObject.AddComponent<ScenarioChoiceSituation>();course.session=session;course.Begin(CurrentScenario);session.jobs.nav.ObserveEdge(CurrentScenario.connector);
  }
 }
 public void RecordSkippedDelivery(){var choice=GetComponent<ScenarioChoiceSituation>();if((Assigned==null||Completed)&&(choice==null||choice.Course==null||choice.Completed))return;
  var e=new LawEvent{recoveryPolicyVersion=1,id=session.law.ids.Next("unvisited"),day=session.law.Day,tripId=session.law.tripId,occupied=true,family="상황 미체험",scenario=CurrentScenario==null?"":CurrentScenario.id,reason="목적지 정상 하차 · 추천 상황 미체험 / 실제 관찰한 운행 기록은 유지"};e.Add("상황 미체험",LawVerdict.NoOpportunity,0);session.law.ledger.Record(e);
 }
 public void EndTrip(){
  if(Live&&Live.Active){if(Live.State.Entered&&!Live.State.Finished){Live.RecordObservation();LastFeedback=Live.Feedback();LastTitle=Assigned==null?"상황 미완료":Assigned.Title;}Live.Stop();}session.jobs.nav.ObserveEdge(-1);Assigned=null;details=false;PickupPhase=false;CurrentScenario=null;var course=GetComponent<ScenarioChoiceSituation>();if(course)course.Stop();
 }
 void Update(){
  if(!Ready)return;
  if(session.jobs.trip.State!=TripState.OnTrip&&!(PickupPhase&&session.jobs.trip.State==TripState.ToPickup)){if(Assigned!=null)EndTrip();return;}
  if(Assigned==null||Completed||session.paused||session.PracticeMenuOpen||session.business.ShowingSummary)return;
  if(!Live.Active&&session.jobs.nav.PassedObservedEdge==Assigned.connector){Completed=true;LastTitle=Assigned.Title;LastFeedback=new PracticeFeedback{outcome="구역 통과 · 상황 관찰 보류",good="안내한 상황 구역을 통과했습니다.",issues="주변 교통 때문에 안전한 상황 배치를 보류했습니다. 법규 준수로 자동 채점하지 않습니다.",next="F8에서 운송 기록과 법규 관찰을 구분해 확인하세요."};toastUntil=Time.time+12;
   var e=new ConfusionPracticeState(Assigned.variant){Known=false}.Event(session.law.ids.Next("confusion"),session.law.Day);e.junction=Assigned.junction.name;e.tripId=session.law.tripId;e.occupied=session.jobs.trip.State==TripState.OnTrip;e.seconds=(float)session.business.ledger.Elapsed;e.reason=LastFeedback.Details();session.law.ledger.Record(e);return;}
  if(!Live.Active){
   float error;var path=session.jobs.nav.hd.paths[Assigned.incoming];float at=path.Project(session.taxi.transform.position,out error);float gap=Vector3.Distance(session.taxi.transform.position,Assigned.junction.center);
   bool direction=Vector3.Dot(session.taxi.transform.forward,path.Tangent(at))>.7f;
   float routeError,progress=session.jobs.nav.ProjectVehicle(out routeError),begin=0;bool correctVisit=false;var route=session.jobs.nav.hdRoute;
   if(route!=null)for(int k=0;k<route.edges.Count;k++){if(route.edges[k]==Assigned.connector&&begin-progress>=-1&&begin-progress<140){correctVisit=true;break;}begin=route.ends[k];}
   if(correctVisit&&direction&&error<3&&gap>45&&!GetComponent<RightTurnSituation>().Active&&!GetComponent<ExitQueueSituation>().Active){
    bool occupied=false;foreach(var c in session.traffic.cars)if(c.Visible&&Vector3.Distance(c.transform.position,Assigned.junction.center)<Assigned.junction.StopDistance(0)+5)occupied=true;
    if(!occupied&&Live.CanStartInService(Assigned))Live.StartInService(Assigned);
   }
  }else if(Live.State.Finished){
   LastFeedback=Live.Feedback();LastTitle=Assigned.Title;Completed=true;toastUntil=Time.time+12;session.jobs.nav.RequireVia(-1);Live.Stop();
  }
 }
 public void ToggleDetails(){if(LastFeedback!=null)details=!details;}
 public void BeginAlternative(){if(Live&&Live.Active)Live.Stop();Assigned=null;Completed=false;}
 public void CompleteAlternative(){Completed=true;LastTitle="출구 우회";LastFeedback=new PracticeFeedback{outcome="우회 경로 통과",good="선택한 대체 연결도로를 실제로 통과했습니다.",issues="원래 대기열 합류는 평가 기회 없음입니다.",next="목적지까지 안내 차로를 따라 진행하세요."};toastUntil=Time.time+12;}
 Vector2 situationScroll;public bool TipExpanded {get;private set;}=true;
 public void ToggleTip(){TipExpanded=!TipExpanded;}
 void OnGUI(){
  if(PracticeSession.Enabled||session.FullMapOpen||session.PracticeMenuOpen||session.ChoiceOpen||session.RideResultOpen||session.business.preparing||session.business.restartPrompt||session.business.ShowingSummary||session.business.taxPrompt||session.law.HistoryOpen||session.services.open||session.GetComponent<DriveHUD>().HelpOpen)return;
  var text=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=15,wordWrap=true};var button=new GUIStyle(GUI.skin.button){font=KoreanText.Font,fontSize=14};
  if(details&&LastFeedback!=null){
   float w=Mathf.Min(650,Screen.width-40),h=Mathf.Min(450,Screen.height-40),x=(Screen.width-w)/2,y=(Screen.height-h)/2;Panel(new Rect(x,y,w,h));GUI.Label(new Rect(x+18,y+15,w-36,55),LastTitle+" · 이번 운송 법규 결과\n"+LastFeedback.outcome,text);
   string content=LastFeedback.Details();float ch=Mathf.Max(250,text.CalcHeight(new GUIContent(content),w-60));scroll=GUI.BeginScrollView(new Rect(x+18,y+80,w-36,h-150),scroll,new Rect(0,0,w-56,ch));GUI.Label(new Rect(0,0,w-60,ch),content,text);GUI.EndScrollView();
   if(GUI.Button(new Rect(x+18,y+h-54,w-36,36),"닫기 · 운행은 계속됩니다",button))details=false;return;
  }
  string tip=null;bool result=false;
  if(Assigned!=null&&!Completed){float distance=Vector3.Distance(session.taxi.transform.position,Assigned.junction.center);if(distance<260)tip=Assigned.Title+"\n창작 구역 · 약 "+distance.ToString("0")+"m\n"+(Live.Active?Live.Progress:ConfusionPracticeState.Guidance(Assigned.variant));}
  if(tip==null&&LastFeedback!=null&&(session.paused||session.business.ShowingSummary||session.taxi.body.velocity.magnitude<.5f||Time.time<toastUntil)){tip=LastFeedback.outcome;result=true;}
  string service=session.services.TipText;
  if(!string.IsNullOrEmpty(service))tip=(tip==null?"":tip+"\n\n")+service;
  if(!string.IsNullOrEmpty(session.services.notice))tip=session.services.notice+(tip==null?"":"\n\n"+tip);
  if(tip!=null){float scale=CockpitUI.Scale;bool expanded=TipExpanded;var r=CockpitUI.SituationBounds(Screen.width,Screen.height,result,session.jobs.Active);if(!expanded)r.height=38*scale;Panel(r);
   GUI.Label(new Rect(r.x+10*scale,r.y+3*scale,120*scale,30*scale),"TIP",new GUIStyle(text){font=KoreanText.BoldFont,fontSize=Mathf.RoundToInt(17*scale)});
   if(GUI.Button(new Rect(r.xMax-104*scale,r.y+5*scale,94*scale,28*scale),TipExpanded?"숨기기 ▲":"펼치기 ▼",KoreanText.Button(Mathf.RoundToInt(13*scale))))ToggleTip();
   if(expanded){float footer=result?36*scale:0;var body=new Rect(r.x,r.y+36*scale,r.width,r.height-36*scale-footer);situationScroll=DrawSituationCard(body,tip,situationScroll);
    if(result){GUI.enabled=session.paused||session.business.ShowingSummary||session.taxi.body.velocity.magnitude<.5f;if(GUI.Button(new Rect(r.x+10*scale,r.yMax-34*scale,r.width-20*scale,28*scale),"운전 평가 · 정차 시 확인",KoreanText.Button(Mathf.RoundToInt(13*scale))))ToggleDetails();GUI.enabled=true;}
   }
  }

 }
 public static Vector2 DrawSituationCard(Rect r,string content,Vector2 position){Panel(r);var text=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=15,wordWrap=true};float ch=Mathf.Max(r.height-20,text.CalcHeight(new GUIContent(content),r.width-36)+8);position=GUI.BeginScrollView(new Rect(r.x+10,r.y+8,r.width-20,r.height-16),position,new Rect(0,0,r.width-36,ch));GUI.Label(new Rect(0,0,r.width-36,ch),content,text);GUI.EndScrollView();return position;}
 static void Panel(Rect r){GUI.color=new Color(.025f,.055f,.075f,1);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
}
