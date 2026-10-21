using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ServiceLawZone {
 public string id;public int variant,incoming,connector,outgoing;public TrafficJunction junction;
 public string Title {get{return ConfusionPracticeState.Title(variant);}}
 public string MapTitle {get{switch(variant){case 8:return "적색 점멸\n정지 후 진행";case 9:return "황색 점멸\n주변 확인";case 10:return "녹색\n출구 정체";case 11:return "녹색\n출구 여유";case 12:return "비보호 좌회전\n적색 대기";case 13:return "비보호 좌회전\n대향차 양보";default:return "비보호 좌회전\n대향차 없음";}}}
}
// Persistent places in the normal Eungye world. Trips route through a connector, never a new scene/process.
public sealed class ServiceLawZones : MonoBehaviour {
 public DriveSession session;
 public readonly List<ServiceLawZone> zones=new List<ServiceLawZone>();
 public ServiceLawZone Assigned {get;private set;}
 public ConfusionPractice Live {get;private set;}
 public PracticeFeedback LastFeedback {get;private set;}
 public string LastTitle {get;private set;}public bool Completed {get;private set;}
 public bool Ready {get;private set;}int sequence;float toastUntil;bool details;Vector2 scroll;
 public bool Owns(TrafficJunction j){return Live&&Live.Owns(j);}
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
  var hd=session.jobs.nav.hd;var occupied=new HashSet<string>();
  for(int variant=8;variant<=14;variant++){
   ServiceLawZone best=null;float score=float.MaxValue;
   foreach(var j in session.traffic.junctions){if(!j.signalized||occupied.Contains(j.name))continue;
    for(int c=0;c<hd.edges.Length;c++){
     var e=hd.edges[c];if(e.disabled||e.change!=0||e.ruleId!=j.name||e.turn!=(variant>=12?-1:0))continue;
     int outgoing=-1;foreach(int n in e.next)if(!hd.edges[n].disabled&&hd.edges[n].change==0&&hd.paths[n].length>32){outgoing=n;break;}if(outgoing<0)continue;
     for(int b=0;b<hd.edges.Length;b++){
      if(hd.edges[b].disabled||hd.edges[b].change!=0||hd.edges[b].number!=1||hd.paths[b].length<65||Array.IndexOf(hd.edges[b].next,c)<0)continue;
      float d=Vector3.Distance(j.center,new Vector3(575,0,257));if(d>=score)continue;
      var r=hd.Route(b,Mathf.Max(0,hd.paths[b].length-75),outgoing,25);if(r==null||r.edges.Count!=3||r.edges[1]!=c)continue;
      if(!ReachableOffer(hd,c))continue;
      best=new ServiceLawZone{id="eungye-law-"+variant,variant=variant,junction=j,incoming=b,connector=c,outgoing=outgoing};score=d;
     }
    }
   }
   if(best==null)continue;zones.Add(best);occupied.Add(best.junction.name);CreateSign(best);
  }
  var obj=new GameObject("은계 영업 도로 상황 운용");obj.transform.SetParent(transform);Live=obj.AddComponent<ConfusionPractice>();Live.session=session;
 }
 bool ReachableOffer(HdRoadGraph hd,int connector){
  var stops=session.jobs.stops;
  for(int p=0;p<stops.Length;p++){
   if(!stops[p].graphStop)continue;var first=hd.Route(stops[p].laneEdge,stops[p].laneAt,connector,0);if(first==null||first.path.length<85)continue;
   for(int offset=3;offset<=5;offset++){var to=stops[(p+offset)%stops.Length];if(to.graphStop&&hd.Route(connector,hd.paths[connector].length,to.laneEdge,to.laneAt)!=null)return true;}
  }return false;
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
   int index=(sequence+k)%zones.Count;var zone=zones[index];var approach=hd.Route(from.laneEdge,from.laneAt,zone.connector,0);
   if(approach==null||approach.path.length<85)continue;
   var route=hd.Via(from.laneEdge,from.laneAt,zone.connector,to.laneEdge,to.laneAt);if(route==null)continue;
   offer.zoneIndex=index;offer.distance=route.path.length;offer.waitSeconds=35;
   foreach(int e in route.edges)if(hd.edges[e].id.StartsWith("join:"))foreach(var j in session.traffic.junctions)if(j.signalized&&j.name==hd.edges[e].ruleId){offer.waitSeconds+=15;break;}
   sequence=(index+1)%zones.Count;return;
  }
 }
 public void BeginTrip(JobOffer offer){
  EndTrip();Assigned=ZoneFor(offer);Completed=false;LastFeedback=null;LastTitle="";
  if(Assigned!=null)session.jobs.nav.RequireVia(Assigned.connector);
 }
 public void EndTrip(){
  if(Live&&Live.Active){if(Live.State.Entered&&!Live.State.Finished){Live.RecordObservation();LastFeedback=Live.Feedback();LastTitle=Assigned==null?"상황 미완료":Assigned.Title;}Live.Stop();}Assigned=null;details=false;
 }
 void Update(){
  if(!Ready)return;
  if(session.jobs.trip.State!=TripState.OnTrip){if(Assigned!=null)EndTrip();return;}
  if(Assigned==null||Completed||session.paused||session.PracticeMenuOpen||session.business.ShowingSummary)return;
  if(!Live.Active&&session.jobs.nav.PassedRequiredVia==Assigned.connector){Completed=true;LastTitle=Assigned.Title;LastFeedback=new PracticeFeedback{outcome="구역 통과 · 상황 관찰 보류",good="안내한 상황 구역을 통과했습니다.",issues="주변 교통 때문에 안전한 상황 배치를 보류했습니다. 법규 준수로 자동 채점하지 않습니다.",next="F8에서 운송 기록과 법규 관찰을 구분해 확인하세요."};toastUntil=Time.time+12;
   var e=new ConfusionPracticeState(Assigned.variant){Known=false}.Event(session.law.ids.Next("confusion"),session.law.Day);e.junction=Assigned.junction.name;e.tripId=session.law.tripId;e.occupied=true;e.seconds=(float)session.business.ledger.Elapsed;e.reason=LastFeedback.Details();session.law.ledger.Record(e);return;}
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
 void OnGUI(){
  if(!Ready||session.PracticeMenuOpen||session.business.preparing||session.business.restartPrompt||session.law.HistoryOpen||session.services.open)return;
  var text=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=15,wordWrap=true};var button=new GUIStyle(GUI.skin.button){font=KoreanText.Font,fontSize=14};
  if(details&&LastFeedback!=null){
   float w=Mathf.Min(650,Screen.width-40),h=Mathf.Min(450,Screen.height-40),x=(Screen.width-w)/2,y=(Screen.height-h)/2;Panel(new Rect(x,y,w,h));GUI.Label(new Rect(x+18,y+15,w-36,55),LastTitle+" · 이번 운송 법규 결과\n"+LastFeedback.outcome,text);
   string content=LastFeedback.Details();float ch=Mathf.Max(250,text.CalcHeight(new GUIContent(content),w-60));scroll=GUI.BeginScrollView(new Rect(x+18,y+80,w-36,h-150),scroll,new Rect(0,0,w-56,ch));GUI.Label(new Rect(0,0,w-60,ch),content,text);GUI.EndScrollView();
   if(GUI.Button(new Rect(x+18,y+h-54,w-36,36),"닫기 · 운행은 계속됩니다",button))details=false;return;
  }
  if(Assigned!=null&&!Completed){float distance=Vector3.Distance(session.taxi.transform.position,Assigned.junction.center);if(distance<260){float y=Screen.height-355;Panel(new Rect(16,y,430,170));GUI.Label(new Rect(28,y+10,406,148),"운송 중 상황 · "+Assigned.Title+"\n창작 구역 · 약 "+distance.ToString("0")+"m\n"+(Live.Active?Live.Progress:ConfusionPracticeState.Guidance(Assigned.variant)),text);}}
  if(LastFeedback!=null&&(session.paused||session.business.ShowingSummary||session.taxi.body.velocity.magnitude<.5f||Time.time<toastUntil)){
   float y=Screen.height-405;Panel(new Rect(16,y,430,70));GUI.Label(new Rect(28,y+4,405,30),LastFeedback.outcome,text);
   GUI.enabled=session.paused||session.business.ShowingSummary||session.taxi.body.velocity.magnitude<.5f;if(GUI.Button(new Rect(28,y+36,405,28),"운전 평가 결과 · 잘한 점 / 보완 위치 (정차 시)",button))ToggleDetails();GUI.enabled=true;
  }
 }
 static void Panel(Rect r){GUI.color=new Color(.025f,.055f,.075f,.96f);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
}
