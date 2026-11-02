using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public sealed class FacilityOption {
 public string Id,Name;public int Kind;public MapStop Approach;public Vector3 Bay,Direction;public Vector3[] Entry,Departure;
 public float TravelMetres,TravelSeconds;public int Waiting;public bool PhysicallyOccupied;public OperationServiceQuote Quote;
 public bool Reachable {get{return !float.IsInfinity(TravelSeconds)&&!float.IsNaN(TravelSeconds);}}
 public string Text {get{return Name+" · "+(!Reachable?"경로/예상시간 없음":"이동 약 "+Mathf.CeilToInt(TravelSeconds)+"초 / "+Mathf.CeilToInt(TravelMetres)+"m")+" · "+(Waiting>0?"대기 "+Waiting+"대 (시간 미정)":PhysicallyOccupied?"정차 공간 점유 · 대기시간 미정":"대기 차량 없음")+(Quote!=null&&Quote.Available?" · 이용 "+Quote.UseSeconds.ToString("0")+"초 / "+Quote.Price.ToString("N0")+"원"+(Quote.NewDebt>0?" (미납 "+Quote.NewDebt.ToString("N0")+"원)":""):" · 현재 견적 불가");}}
}

public partial class ServiceStops {
 sealed class Registry {public readonly FacilityAdmissionQueue queue=new FacilityAdmissionQueue();public readonly Dictionary<int,ServiceStops> members=new Dictionary<int,ServiceStops>();}
 static readonly ConditionalWeakTable<object,Registry> registries=new ConditionalWeakTable<object,Registry>();
 Registry registry;string queuedSite;Vector3 queuedBay,queuedApproach;int queuedAction=-1;float grantedAt,nextEntranceScan;bool observedBay;
 sealed class OptionCache {public float until;public object route;public List<FacilityOption> options;}
 readonly Dictionary<int,OptionCache> optionCaches=new Dictionary<int,OptionCache>();
 readonly Collider[] facilityColliders=new Collider[32];
 public bool QueueWaiting {get{return queuedSite!=null&&registry!=null&&!registry.queue.IsAdmitted(GetInstanceID());}}
 public string QueueStatus {get{return queuedSite==null?"":(QueueWaiting?"시설 대기 "+registry.queue.Position(queuedSite,GetInstanceID())+"번째 · 입장 전 결제/서비스 시간 없음 · F7 대기 취소":"시설 이용/출차 중 · 다음 차량은 출차 후 입장")+" · 대체 시설은 M 지도";}}
 Registry SharedRegistry(){if(registry==null){object key=session.simulationMatch?(object)session.simulationMatch:this;registry=registries.GetOrCreateValue(key);registry.members[GetInstanceID()]=this;}return registry;}
 static string SiteId(Vector3 bay){return bay.x.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+":"+bay.z.ToString("F1",System.Globalization.CultureInfo.InvariantCulture);}
 bool BayClear(Vector3 bay){int count=Physics.OverlapBoxNonAlloc(bay+Vector3.up*.8f,new Vector3(2.3f,.8f,4.2f),facilityColliders,Quaternion.identity,(1<<8)|(1<<9),QueryTriggerInteraction.Ignore);if(count>=facilityColliders.Length)return false;for(int i=0;i<count;i++)if(facilityColliders[i]&&!facilityColliders[i].transform.IsChildOf(session.taxi.transform))return false;return true;}
 public bool CanEnterFacility(FacilityOption option){
  if(option==null||!option.Reachable)return false;var r=SharedRegistry();if(queuedSite!=option.Id){CancelQueue();queuedSite=option.Id;queuedBay=option.Bay;queuedApproach=option.Approach.V();observedBay=false;grantedAt=Time.time;}
  r.queue.Request(queuedSite,GetInstanceID(),Time.time);bool already=r.queue.IsAdmitted(GetInstanceID());bool admitted=r.queue.TryAdmit(queuedSite,GetInstanceID(),BayClear(queuedBay));if(admitted&&!already)grantedAt=Time.time;return admitted;
 }
 public void CancelQueue(){if(registry!=null)registry.queue.Release(GetInstanceID());queuedSite=null;queuedAction=-1;observedBay=false;grantedAt=0;}
 void OnDestroy(){CancelQueue();if(registry!=null)registry.members.Remove(GetInstanceID());}
 public void TickFacilityQueue(){
  if(!session||!session.business||session.business.ledger==null)return;var r=SharedRegistry();r.queue.RemoveAbsent(delegate(int id){ServiceStops service;return r.members.TryGetValue(id,out service)&&service&&service.session&&(!service.session.simulationMatch||!service.session.simulationMatch.Finished);});
  if(queuedSite==null&&!session.IsSimulationBot&&session.jobs.trip.State!=TripState.OnTrip&&Time.time>=nextEntranceScan){nextEntranceScan=Time.time+.5f;ObservePlayerEntrance();}
  foreach(var member in r.members.Values)if(member)member.ReconcileFacilityTicket();
  if(queuedSite==null)return;
  bool locked=session.simulationMatch&&session.simulationMatch.ServiceLocked(session);
  if(!session.IsSimulationBot&&queuedAction>=0&&!session.paused&&!locked){int action=queuedAction;if(!CanUse(action)){if(session.taxi.body.velocity.magnitude>.56f)CancelQueue();return;}if(r.queue.TryAdmit(queuedSite,GetInstanceID(),BayClear(queuedBay))){queuedAction=-1;open=true;Buy(action);open=false;}}
 }
 void ReconcileFacilityTicket(){
  if(queuedSite==null)return;if(!session||session.simulationMatch&&session.simulationMatch.Finished||session.jobs.Active&&session.jobs.trip.State==TripState.OnTrip){CancelQueue();return;}
  float d=Vector3.Distance(session.taxi.transform.position,queuedBay);if(d<5)observedBay=true;
  bool locked=session.simulationMatch&&session.simulationMatch.ServiceLocked(session);
  if(!locked&&(observedBay&&d>9||!observedBay&&Vector3.Distance(session.taxi.transform.position,queuedApproach)>90)){CancelQueue();return;}
  if(!locked&&(session.damage&&session.damage.Disabled||session.business.ledger.Driver!=null&&session.business.ledger.Driver.drowsiness.Blackout)){CancelQueue();return;}
  if(!locked&&!observedBay&&registry.queue.IsAdmitted(GetInstanceID())&&Time.time-grantedAt>60)CancelQueue();
 }
 void ObservePlayerEntrance(){
  var position=session.taxi.transform.position;bool nearby=false;
  foreach(var station in Stations)if((position-station.approach.V()).sqrMagnitude<35*35||(position-station.pump).sqrMagnitude<35*35){nearby=true;break;}
  if(!nearby&&ProjectMapScene.Active(session.world.map)){foreach(string type in new[]{"restaurant","rest","parking"})foreach(var obj in ProjectMapScene.Services(session.world.map,type))if((position-obj.Position).sqrMagnitude<45*45){nearby=true;break;}}
  else if(!nearby){nearby=(position-MealPosition).sqrMagnitude<45*45||(position-ParkingStop.Kerb).sqrMagnitude<35*35;var rest=RestArea;if(rest!=null)nearby|=(position-rest.pump).sqrMagnitude<35*35;}
  if(!nearby)return;
  foreach(int kind in new[]{0,1,3})foreach(var site in FacilityOptions(kind)){
   if(!site.Reachable)continue;
   // Only the explicitly selected facility route queues on the public road.
   // Passing a driveway, or waiting at a nearby signal, creates no ticket.
   bool guided=destination.Length>0&&Vector3.Distance(position,site.Approach.V())<15&&(Vector3.Distance(session.jobs.nav.destination,site.Approach.Kerb)<3||Vector3.Distance(session.jobs.nav.destination,site.Approach.V())<3);
   bool driveway=false;for(int i=1;i<site.Entry.Length;i++){Vector3 a=site.Entry[i-1],d=site.Entry[i]-a;if(Vector3.Distance(site.Entry[i],site.Approach.V())<8)continue;float t=Mathf.Clamp01(Vector3.Dot(position-a,d)/Mathf.Max(.001f,d.sqrMagnitude));if((position-a-d*t).sqrMagnitude<9){driveway=true;break;}}
   if(guided||driveway){CanEnterFacility(site);return;}
  }
 }
 bool AdmitPurchase(int action){
  var options=FacilityOptions(action);FacilityOption here=null;foreach(var option in options)if(Vector3.Distance(session.taxi.transform.position,option.Bay)<5&&(here==null||Vector3.Distance(session.taxi.transform.position,option.Bay)<Vector3.Distance(session.taxi.transform.position,here.Bay)))here=option;
  if(here==null)return false;if(CanEnterFacility(here))return true;
  if(!session.IsSimulationBot){queuedAction=action;notice="시설이 사용 중입니다 · "+QueueStatus;noticeUntil=Time.unscaledTime+8;Close(false);}return false;
 }
 FacilityOption Option(int kind,string name,MapStop approach,Vector3 bay,Vector3 direction,Vector3[] entry,Vector3[] departure){
  float metres,seconds=OperationForecastAdapter.DirectedSeconds(session,approach,out metres);if(Vector3.Distance(session.taxi.transform.position,bay)<5){seconds=0;metres=0;}var r=SharedRegistry();string id=SiteId(bay);return new FacilityOption{Id=id,Name=name,Kind=kind,Approach=approach,Bay=bay,Direction=direction,Entry=entry,Departure=departure,TravelMetres=metres,TravelSeconds=seconds,Waiting=r.queue.Count(id)-(r.queue.Position(id,GetInstanceID())>0?1:0),PhysicallyOccupied=!BayClear(bay),Quote=OperationServiceQuote.Calculate(session.business.ledger,kind,seconds,metres)};
 }
 void AddDrive(List<FacilityOption> list,int kind,string name,MapStop approach,Vector3 bay,Vector3 direction,Vector3[] drive){if(drive==null||drive.Length<2)return;int split=0;float best=float.PositiveInfinity;for(int i=0;i<drive.Length;i++){float d=(drive[i]-bay).sqrMagnitude;if(d<best){best=d;split=i;}}var enter=new List<Vector3>();for(int i=0;i<split;i++)enter.Add(drive[i]);enter.Add(bay);var leave=new List<Vector3>{bay};for(int i=split+1;i<drive.Length;i++)leave.Add(drive[i]);if(enter.Count<2)enter.Insert(0,approach.V());if(leave.Count<2)return;list.Add(Option(kind,name,approach,bay,direction,enter.ToArray(),leave.ToArray()));}
 public List<FacilityOption> FacilityOptions(int kind){
  OptionCache cached;if(optionCaches.TryGetValue(kind,out cached)&&Time.unscaledTime<cached.until&&ReferenceEquals(cached.route,session.jobs.nav.hdRoute))return cached.options;
  var list=new List<FacilityOption>();var map=session.world.map;
  if(kind==0||kind==4||kind==5){foreach(var site in Stations){if(kind==4&&!site.SupportsWash||kind==5&&!site.SupportsRepair)continue;AddDrive(list,kind,site.name,site.approach,site.pump,site.direction,site.drivePath);}if(Stations.Count==0&&map.mapId!="eungye"){var stop=StopFor(0);list.Add(Option(kind,stop.name,stop,stop.Kerb,stop.Direction,new[]{stop.V(),stop.Kerb},new[]{stop.Kerb,stop.Kerb+stop.Direction*12}));}}
  else if(kind==1){if(ProjectMapScene.Active(map)){foreach(var obj in ProjectMapScene.Services(map,"restaurant")){var site=RestaurantSiteLayout.FromObject(map,obj);list.Add(Option(kind,obj.name,site.approach,site.parking,site.direction,site.enter,site.leave));}}else if(Restaurant!=null){var site=Restaurant;list.Add(Option(kind,MealStop.name,site.approach,site.parking,site.direction,site.enter,site.leave));}else {var stop=MealStop;list.Add(Option(kind,stop.name,stop,stop.Kerb,stop.Direction,new[]{stop.V(),stop.Kerb},new[]{stop.Kerb,stop.Kerb+stop.Direction*12}));}}
  else if(kind==3||kind==2){if(ProjectMapScene.Active(map)){foreach(string type in new[]{"rest","parking"})foreach(var obj in ProjectMapScene.Services(map,type)){if(type=="rest"){var site=FuelStationLayout.FromObject(map,obj);AddDrive(list,kind,obj.name,site.approach,site.pump,site.direction,site.drivePath);continue;}var access=Array.Find(map.roads,road=>road.id==obj.id+":access");if(access==null||access.points==null||access.points.Length<4)continue;var points=new List<Vector3>();foreach(var p in access.points)points.Add(p.V());points.Insert(points.Count/2,obj.Position);float at;int edge=session.jobs.nav.hd.Nearest(points[0],obj.Direction,out at);if(edge<0)continue;at=Mathf.Max(0,at-4);var path=session.jobs.nav.hd.paths[edge];var p0=path.Position(at);var heading=path.Tangent(at);var stop=new MapStop{name=obj.name,x=p0.x,z=p0.z,elevation=p0.y,dx=heading.x,dz=heading.z,width=3,graphStop=true,laneEdge=edge,laneAt=at};AddDrive(list,kind,obj.name,stop,obj.Position,obj.Direction,points.ToArray());}}
   else {var rest=RestArea;if(rest!=null)AddDrive(list,kind,"휴식 주차장",rest.approach,rest.pump,rest.direction,rest.drivePath);var stop=ParkingStop;list.Add(Option(kind,stop.name,stop,stop.Kerb,stop.Direction,new[]{stop.V(),stop.Kerb},new[]{stop.Kerb,stop.Kerb+stop.Direction*12}));}}
  list.Sort(delegate(FacilityOption a,FacilityOption b){return a.TravelSeconds.CompareTo(b.TravelSeconds);});optionCaches[kind]=new OptionCache{until=Time.unscaledTime+1,route=session.jobs.nav.hdRoute,options=list};return list;
 }
 public bool GuideFacility(FacilityOption option){if(option==null||!option.Reachable||session.jobs.trip.State==TripState.OnTrip)return false;CancelQueue();session.jobs.nav.SetDestination(option.Approach);destination=option.Text+" · 내부 수동 진입 · 정차 후 F7";if(open)Close(false);return true;}
}
