using System.Collections.Generic;
using UnityEngine;

public partial class FullMapOverlay {
 public sealed class Place {public string name,description,capabilityLabel;public Vector3 position;public MapStop approach;public int glyph;public int plannerKind=-1;public bool roadPoint,home;}
 public sealed class PaintedSegment {public Vector3 a,b;public Color color;public bool passenger,retained;}
 public readonly List<PaintedSegment> RoutePaint=new List<PaintedSegment>();
 List<Place> cachedPlaces;MapData placesMap;float nextPlaces;
 public JobOffer PreviewOffer {get{return selectedOffer;}}
 List<Vector3> pickupPath=new List<Vector3>(),ridePath=new List<Vector3>();JobOffer cachedRide;
 public int PickupPreviewPointCount {get{return pickupPath.Count;}}
 public void ShowCalls(){if(session.jobs.Active&&RestoreTripRoute())return;selectedPlace=null;if(selectedOffer!=null&&SelectedOffer>=0&&!session.jobs.Active)SelectOffer(SelectedOffer);else {previewPath.Clear();RefreshRoutePaint(true);}}
 List<Vector3> previewPath=new List<Vector3>();Place selectedPlace;float nextPaint;
 public int PreviewPointCount {get{return previewPath.Count;}}
 public string SelectedPlaceName {get{return selectedPlace==null?"":selectedPlace.name;}}
 public int[] ActiveStopPins {get{var jobs=session.jobs;if(!jobs.Active||jobs.current==null)return new int[0];return jobs.PassengerViaPending?new[]{jobs.current.pickup,jobs.current.drop,jobs.PassengerViaStop}:new[]{jobs.current.pickup,jobs.current.drop};}}
 public bool RestoreTripRoute(bool resumePickup=true){var jobs=session.jobs;if(!IsOpen||!jobs.Active||jobs.current==null)return false;if(resumePickup&&jobs.trip.State==TripState.ToPickup){jobs.ResumePickupNavigation();session.services.destination="";}selectedPlace=null;selectedOffer=null;SelectedOffer=-1;acceptFeedback="";previewPath.Clear();RefreshRoutePaint(true);if(jobs.trip.State==TripState.ToPickup)FitRoutes(jobs.nav.DisplayPath,ridePath);else FitRoutes(jobs.nav.DisplayPath);return true;}
 public List<Place> Places(){
  var map=session.world.map;if(cachedPlaces!=null&&placesMap==map&&Time.unscaledTime<nextPlaces)return cachedPlaces;placesMap=map;nextPlaces=Time.unscaledTime+1;var result=new List<Place>();
  if(session.services.HasHomeBase){result.Add(new Place{name=HomeBasePresentation.Name,capabilityLabel=HomeBasePresentation.Name,description="기사 숙소 주차장\n"+HomeBasePresentation.EntryGuide,position=session.services.ParkingStop.Kerb,approach=session.services.HomeApproach,glyph=0,home=true});}
  foreach(var station in session.services.Stations)result.Add(new Place{name=station.name+" · "+station.ServiceLabel,capabilityLabel=station.ServiceLabel,position=station.pump,approach=station.approach,glyph=8,description=station.ServiceLabel+"\n"+session.services.Quote(0)+(station.SupportsWash?"\n\n"+session.services.Quote(4):"")});
  bool sellsParts=session.business.SupportsCareer;
  foreach(var site in session.services.Workshops)result.Add(new Place{name=site.name,capabilityLabel=sellsParts?"공업사 · 수리/견인/부품":"공업사 · 수리/견인",position=site.repair,approach=site.approach,glyph=12,description="정비 1칸 · 견인 2칸 · 대기 1칸\n"+(sellsParts?"부품 판매·장착\n":"")+session.services.Quote(5)});
  if(ProjectMapScene.Active(map)){foreach(var o in map.projectScene.objects)if(o.enabled&&o.IsService&&!(session.services.HasHomeBase&&HomeBasePresentation.IsFacility(map,o)))result.Add(ServicePlace(CasualUI.PlainName(o.name),o.Position,ProjectMapScene.ServiceStop(map,o),o.kind));}
  else {if(session.services.HasService(1))result.Add(ServicePlace("기사 식당",session.services.MealPosition,session.services.MealStop,"restaurant"));if(session.services.HasService(2)&&!session.services.HasHomeBase)result.Add(ServicePlace("주차장",session.services.ParkingStop.V(),session.services.StopFor(2),"parking"));var r=session.services.RestArea;if(r!=null)result.Add(ServicePlace("휴식 주차",r.pump,r.approach,"rest"));}
  foreach(var shop in session.services.RefreshmentShops){
   var item=shop.Item;result.Add(new Place{name=item.Shop,capabilityLabel=item.Shop,position=shop.Stop.Kerb,approach=shop.Stop,glyph=item.Kind==Refreshment.Snack?9:20,plannerKind=Refreshment.Snack,description=item.Terms});
  }
  if(session.simulationMatch&&session.simulationMatch.SharedCalls!=null)foreach(var offer in session.simulationMatch.SharedCalls.Roadside)if(offer!=null){var stop=session.jobs.stops[offer.pickup];result.Add(new Place{name="길가 승객 · "+KoreanText.Stop(stop.name),position=stop.Passenger,approach=stop,glyph=1,description="공유 호출 목록 밖의 길가 승객\n같은 방향 연석에 빈차로 정차 후 F\n다른 택시와 선점 경쟁 · "+session.jobs.DemandHint(offer)});}
  cachedPlaces=result;return result;
 }
 Place ServicePlace(string name,Vector3 p,MapStop stop,string kind){return new Place{name=name,position=p,approach=stop,glyph=kind=="restaurant"?9:kind=="rest"?11:10,description=kind=="restaurant"?"기사 식당\n3,000원 · 50초 · 포만 +70":kind=="rest"?(session.services.HasHomeBase?"휴식 주차\n짧은 휴식 · 퇴근은 숙소에서":"휴식 주차\n무료 수면 · 100초"):"주차장\n휴식 · 퇴근"};}
 public bool SelectPlace(Place place){if(!IsOpen||place==null||place.approach==null&&!place.roadPoint)return false;HideFacilityPlanner();selectedPlace=place;selectedOffer=null;SelectedOffer=-1;acceptFeedback="";previewPath=place.roadPoint?session.jobs.nav.PreviewPoint(place.position):session.jobs.nav.PreviewTo(place.approach);FitRoutes(previewPath);RefreshRoutePaint(true);return true;}
 public bool SelectRoadPoint(Vector3 point){if(!IsOpen)return false;var path=session.jobs.nav.PreviewPoint(point);if(path.Count<2)return false;var end=path[path.Count-1];MapStop stop=null;var graph=session.jobs.nav.hd;if(graph!=null){float at;int edge=graph.Nearest(end,end-path[path.Count-2],out at);if(edge<0)return false;var dir=graph.paths[edge].Tangent(at);stop=new MapStop{name="지도 목적지",x=end.x,z=end.z,dx=dir.x,dz=dir.z,graphStop=true,laneEdge=edge,laneAt=at};}return SelectPlace(new Place{name="선택한 도로 지점",position=end,approach=stop,glyph=3,roadPoint=stop==null,description="선택 위치 인근의 연결된 차로까지 안내합니다.\n정차 허용 장소를 뜻하지 않습니다. 주변 표지와 도로 상황을 확인하세요."});}

 bool SelectedPickupFuel {get{return selectedPlace!=null&&(selectedPlace.glyph==8||selectedPlace.glyph==12)&&session.jobs.trip.State==TripState.ToPickup;}}
 public bool StartPlaceGuidance(){if(!IsOpen||previousPause||selectedPlace==null||previewPath.Count<2||session.jobs.Active&&!SelectedPickupFuel)return false;var place=selectedPlace;Close();if(place.roadPoint)session.jobs.nav.SetDestination(place.position);else session.jobs.nav.SetDestination(place.approach);session.services.destination=place.home?HomeBasePresentation.EntryGuide:place.name+(place.plannerKind==Refreshment.Snack?" · 오른쪽 정차 칸에서 F":" · 입구 앞까지 길안내");selectedPlace=null;previewPath.Clear();RefreshRoutePaint(true);return true;}
 public static Color CongestionColor(float delay){return delay>=6?new Color(.95f,.18f,.15f):delay>=2?new Color(1,.65f,.05f):new Color(.15f,.85f,.4f);}
 public void FitRoutes(params List<Vector3>[] paths){float minX=float.MaxValue,minZ=float.MaxValue,maxX=float.MinValue,maxZ=float.MinValue;foreach(var path in paths)foreach(var p in path){minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);minZ=Mathf.Min(minZ,p.z);maxZ=Mathf.Max(maxZ,p.z);}if(minX==float.MaxValue)return;float span=Mathf.Max(session.world.map.width,session.world.map.depth);center=new Vector2(.5f+((minX+maxX)/2-session.world.map.width/2)/span,.5f-((minZ+maxZ)/2-session.world.map.depth/2)/span);float extent=Mathf.Max(1,Mathf.Max(maxX-minX,maxZ-minZ));float visible=Mathf.Max(.2f,1-100/Mathf.Min(MapRect.width,MapRect.height));ZoomLevel=Mathf.Clamp(span*visible/extent,1,8);ClampCenter();}
 void PaintPath(List<Vector3> path,bool passenger){float traveled=0;for(int i=1;i<path.Count;i++){var a=path[i-1];var b=path[i];float length=Vector3.Distance(a,b);if(length<.001f)continue;float delay=session.roadConditions==null?0:session.roadConditions.ExtraSeconds(a,b,(b-a).normalized,3.2f);var color=CongestionColor(delay);if(!passenger)RoutePaint.Add(new PaintedSegment{a=a,b=b,color=color});else {float d=0;while(d<length){float phase=Mathf.Repeat(traveled+d,26);float step=Mathf.Min(length-d,(phase<16?16:26)-phase);step=Mathf.Max(.001f,step);if(phase<16)RoutePaint.Add(new PaintedSegment{a=Vector3.Lerp(a,b,d/length),b=Vector3.Lerp(a,b,Mathf.Min(length,d+step)/length),color=color,passenger=true});d+=step;}}traveled+=length;}}

 public void RefreshRoutePaint(bool force=false){if(session==null||session.jobs==null||!force&&Time.unscaledTime<nextPaint)return;nextPaint=Time.unscaledTime+1;var jobs=session.jobs;if(selectedOffer!=null&&(jobs.Active||SelectedOffer<0||SelectedOffer>=jobs.offers.Length||jobs.offers[SelectedOffer]!=selectedOffer)){if(!jobs.Active)acceptFeedback="호출이 갱신됐습니다 · 목록에서 다시 선택하세요";selectedOffer=null;SelectedOffer=-1;}RoutePaint.Clear();
  if(IsOpen&&selectedPlace!=null){PaintPath(previewPath,false);return;}
  if(!jobs.Active&&selectedOffer!=null){PaintPath(pickupPath,false);PaintPath(previewPath,true);return;}
  int firstSegment=RoutePaint.Count;PaintPath(jobs.nav.ShownPath,jobs.trip.State==TripState.OnTrip);
  if(jobs.nav.RetainingPreviousRoute)for(int i=firstSegment;i<RoutePaint.Count;i++)RoutePaint[i].retained=true;
  if(jobs.trip.State==TripState.ToPickup&&jobs.current!=null){if(cachedRide!=jobs.current){cachedRide=jobs.current;ridePath=jobs.nav.Find(jobs.stops[jobs.current.pickup],jobs.stops[jobs.current.drop]);}PaintPath(ridePath,true);}else cachedRide=null;
 }

 public string ApproachSummary {get{if(session.jobs.Active&&session.jobs.trip.State==TripState.ToPickup&&StopArrivalGuidance.For(session).Ready)return "승차 지점 도착 · 지도 닫고 F";if(session.jobs.nav.RetainingPreviousRoute&&session.jobs.Active)return "경로 재탐색 중 · 흐린 선은 이전 경로입니다";var jobs=session.jobs;var p=jobs.Active?jobs.nav.DisplayPath:pickupPath;if(p.Count==1){if(jobs.Active)return "승차 지점 근처 · 정차 위치와 방향을 확인하세요";if(selectedOffer!=null&&selectedPickupReachable)return "승차 지점 도착 · 호출 수락 후 정차하여 F";}float d=0;for(int i=1;i<p.Count;i++)d+=Vector3.Distance(p[i-1],p[i]);return p.Count<2?"승차 경로를 찾지 못했습니다":"내 차 → 승차 · "+d.ToString("0")+"m · 하늘색 실선";}}
 // 2026-11-24 chained dispatch (Eungye single): reserve one nearby call while approaching the drop-off.
 void DrawChain(float y,float rw){
  var jobs=session.jobs;if(!jobs.ChainWindow)return;var list=jobs.ChainCandidates();if(list.Length==0)return;
  Icon(new Rect(0,y,rw,32),18,CasualUI.Blue,new GUIContent("다음 호출 예약","하차 지점 근처 호출을 미리 잡으면 하차 후 바로 시작합니다"),BoldLine);y+=40;
  foreach(var o in list){bool reserved=jobs.ChainedOffer==o;var old=GUI.backgroundColor;if(reserved)GUI.backgroundColor=new Color(.72f,.92f,.78f);
   var r=new Rect(0,y,rw,66);if(GUI.Button(r,GUIContent.none,button)&&!reserved)jobs.ReserveChain(o);GUI.backgroundColor=old;
   GUI.Label(new Rect(r.x+10,r.y+6,rw-20,24),(reserved?"예약됨 · ":"")+KoreanText.Stop(jobs.stops[o.pickup].name)+" → "+KoreanText.Stop(jobs.stops[o.drop].name),BoldLine);
   float half=(rw-20)/2;
   Icon(new Rect(r.x+10,r.y+34,half,24),3,CasualUI.Muted,new GUIContent(Mathf.RoundToInt(jobs.ChainPickupMetres(o))+"m","하차 지점에서 승차 지점까지"),PlainLine);
   Icon(new Rect(r.x+10+half,r.y+34,half,24),4,CasualUI.Amber,new GUIContent(PassengerReward.AdjustedFare(o.fare,session.law.ledger.TotalScore).ToString("N0")+"원","예상 운임"),PlainLine);
   y+=74;}
  if(jobs.ChainedOffer!=null&&GUI.Button(new Rect(0,y,rw,40),"예약 취소",button))jobs.CancelChain();
 }
 // 2026-11-24 test feedback: the sidebar was sentences on top of sentences. Each fact is now an icon and a
 // few words on one line; the full sentence is the hover text.
 GUIStyle PlainLine {get{return new GUIStyle(text){wordWrap=false,alignment=TextAnchor.MiddleLeft,clipping=TextClipping.Clip,fontSize=MapFont(14)};}}
 GUIStyle BoldLine {get{return new GUIStyle(PlainLine){font=KoreanText.BoldFont};}}
 static void Icon(Rect r,int glyph,Color tint,GUIContent label,GUIStyle style){float size=Mathf.Min(20,r.height);HudGlyph.Draw(new Rect(r.x,r.y+(r.height-size)/2,size,size),glyph,tint);GUI.Label(new Rect(r.x+size+6,r.y,r.width-size-6,r.height),label,style);}
 // A stop row: coloured dot, role, name. The whole row focuses the stop on the map.
 bool StopRow(Rect r,Color color,string role,string name,bool enabled=true){
  bool hit=GUI.Button(r,new GUIContent("",role+" · "+name),button)&&enabled;
  HudGlyph.Draw(new Rect(r.x+10,r.y+(r.height-16)/2,16,16),15,color);
  GUI.Label(new Rect(r.x+34,r.y,40,r.height),role,BoldLine);GUI.Label(new Rect(r.x+76,r.y,r.width-84,r.height),name,PlainLine);return hit;
 }
 float CallCardHeight(PassengerJobs.CallCard card,float rw){float tags=card.tags.Length==0?0:new GUIStyle(text){fontSize=MapFont(12)}.CalcHeight(new GUIContent(card.tags),rw-20)+2;return 88+tags;}
 void DrawCallCard(Rect r,PassengerJobs.CallCard card,bool selected,int index){
  var old=GUI.backgroundColor;if(selected)GUI.backgroundColor=new Color(.72f,.88f,1);if(GUI.Button(r,GUIContent.none,button))SelectOffer(index);GUI.backgroundColor=old;
  HudGlyph.Draw(new Rect(r.x+8,r.y+8,24,24),15,MapPresentation.Pickup);GUI.Label(new Rect(r.x+8,r.y+8,24,24),card.number,new GUIStyle(BoldLine){alignment=TextAnchor.MiddleCenter,normal={textColor=Color.white}});
  GUI.Label(new Rect(r.x+40,r.y+6,r.width-48,24),card.pickup,BoldLine);
  GUI.Label(new Rect(r.x+40,r.y+30,r.width-48,22),"→ "+card.drop,PlainLine);
  float cx=r.x+10,cw=r.width-20;bool facts=card.deadline.Length>0;
  Icon(new Rect(cx,r.y+58,facts?cw*.33f:cw,22),4,CasualUI.Amber,new GUIContent(card.fare,"예상 운임"),BoldLine);
  if(facts){Icon(new Rect(cx+cw*.33f,r.y+58,cw*.19f,22),18,CasualUI.Muted,new GUIContent(card.deadline,"운송 기한"),PlainLine);
   Icon(new Rect(cx+cw*.52f,r.y+58,cw*.48f,22),3,CasualUI.Muted,new GUIContent(card.distance,"승차 지점까지 거리 · 운송을 마칠 때까지 예상 시간"),PlainLine);}
  if(card.tags.Length>0){var small=new GUIStyle(text){fontSize=MapFont(12)};small.normal.textColor=CasualUI.Muted;GUI.Label(new Rect(cx,r.y+84,cw,r.height-86),card.tags,small);}
 }
 void DrawSidebar(float x,float w,Rect viewport){if(FacilityPlannerVisible){DrawFacilityPlannerSidebar(x,w,viewport);return;}var jobs=session.jobs;CasualUI.Header(new Rect(x,86,w,40),session.simulationMatch?"공용 호출":"주변 호출",1);float top=132;
  if(session.simulationMatch)Icon(new Rect(x,top,w,26),1,CasualUI.Blue,new GUIContent("먼저 수락한 차가 운송","4대가 함께 선택하는 호출입니다. 먼저 수락한 차량이 운송합니다."),PlainLine);
  else Icon(new Rect(x,top,w,26),5,CasualUI.Blue,new GUIContent(PassengerDemandPolicy.Short(session.business.ledger.Elapsed),PassengerDemandPolicy.Description(session.business.ledger.Elapsed)),PlainLine);
  top+=32;
  bool placeFacility=selectedPlace!=null&&!selectedPlace.roadPoint;int plannerKind=placeFacility?(selectedPlace.plannerKind>=0?selectedPlace.plannerKind:selectedPlace.glyph==12?5:selectedPlace.glyph==9?1:selectedPlace.glyph==10||selectedPlace.glyph==11?3:0):0;
  if(GUI.Button(new Rect(x,top,w,36),new GUIContent(placeFacility?"이 시설 견적 비교":"생활시설 비교","이동·대기 시간과 견적을 비교합니다"),button))ShowFacilityPlanner(plannerKind);HudGlyph.Draw(new Rect(x+12,top+8,20,20),9,CasualUI.Blue);top+=44;
  if(selectedPlace!=null){float nameHeight=text.CalcHeight(new GUIContent(selectedPlace.name),w-16);float height=nameHeight+100;KoreanText.Panel(new Rect(x,top,w,height));GUI.Label(new Rect(x+8,top+8,w-16,nameHeight),selectedPlace.name,text);float length=0;for(int i=1;i<previewPath.Count;i++)length+=Vector3.Distance(previewPath[i-1],previewPath[i]);GUI.Label(new Rect(x+8,top+nameHeight+14,w-16,30),previewPath.Count<2?"연결 경로 없음 · 차로/출구로 이동하세요":"입구까지 약 "+length.ToString("0")+"m",new GUIStyle(text){fontSize=MapFont(14)});bool restore=jobs.Active&&!SelectedPickupFuel;GUI.enabled=!locationOpen&&(restore||!previousPause&&previewPath.Count>1);if(GUI.Button(new Rect(x+8,top+height-48,w-16,40),restore?"운송 경로로 돌아가기":SelectedPickupFuel?"호출 유지 · 주유소 길안내":"시설 길안내 시작",button)){if(restore)RestoreTripRoute();else StartPlaceGuidance();}GUI.enabled=!locationOpen;top+=height+12;}
  else {bool approach=selectedOffer!=null||jobs.trip.State==TripState.ToPickup;string state=approach?ApproachSummary:jobs.Active?"운송 중 · 하차 위치 확인":"호출을 선택하세요";Icon(new Rect(x,top,w,26),approach?3:1,approach?MapPresentation.Pickup:jobs.Active?MapPresentation.Passenger:CasualUI.Muted,new GUIContent(state,state),PlainLine);top+=34;}
  if(!jobs.Active&&jobs.LastCallBoard){if(GUI.Button(new Rect(x,top,w,40),"즉시 귀가 · 덕성프라자로 안내",CasualUI.Action(MapFont(14)))){Close();jobs.GoHomeNow();}top+=46;}
  GUI.Label(new Rect(x,top,w,28),jobs.Active?"현재 운송":"호출 목록",BoldLine);top+=32;
  float listHeight=Mathf.Max(72,viewport.yMax-top-124),rw=w-24;float content=0;var cards=new System.Collections.Generic.List<PassengerJobs.CallCard>();var indices=new System.Collections.Generic.List<int>();var heights=new System.Collections.Generic.List<float>();
  if(!jobs.Active)for(int i=0;i<jobs.offers.Length;i++){var o=jobs.offers[i];if(o==null)continue;var card=jobs.CallCardParts(i,session.law.ledger.TotalScore);float height=CallCardHeight(card,rw);cards.Add(card);indices.Add(i);heights.Add(height);content+=height+10;}
  var chosen=jobs.Active?jobs.current:selectedOffer;float foot=content;bool via=jobs.Active&&jobs.PassengerViaPending;if(chosen!=null)content+=88+(via?44:0);if(jobs.Active)content+=jobs.ChainContentHeight;
  filterScroll=GUI.BeginScrollView(new Rect(x,top,w,listHeight),filterScroll,new Rect(0,0,w-20,Mathf.Max(listHeight,content)));float y=0;
  for(int n=0;n<cards.Count;n++){DrawCallCard(new Rect(0,y,rw,heights[n]),cards[n],indices[n]==SelectedOffer,indices[n]);y+=heights[n]+10;}
  if(chosen!=null){bool hidden=jobs.DestinationHidden(chosen);
   if(StopRow(new Rect(0,foot,rw,38),MapPresentation.Pickup,"승차",KoreanText.Stop(jobs.stops[chosen.pickup].name)))FocusStop(chosen.pickup);
   if(StopRow(new Rect(0,foot+44,rw,38),MapPresentation.Passenger,"하차",hidden?"탑승 후 공개":KoreanText.Stop(jobs.stops[chosen.drop].name),!hidden))FocusStop(chosen.drop);
   if(via&&StopRow(new Rect(0,foot+88,rw,38),new Color(1,.8f,.2f),"경유",KoreanText.Stop(jobs.stops[jobs.PassengerViaStop].name)))FocusStop(jobs.PassengerViaStop);
   if(jobs.Active)DrawChain(foot+(via?132:88)+4,rw);}
  GUI.EndScrollView();
  string reason=AcceptBlockedReason;string status=!string.IsNullOrEmpty(acceptFeedback)?acceptFeedback:!string.IsNullOrEmpty(reason)?reason:"선택 "+(SelectedOffer+1)+" · "+KoreanText.Stop(jobs.stops[selectedOffer.pickup].name)+"\n→ "+(jobs.DestinationHidden(selectedOffer)?"목적지 비공개":KoreanText.Stop(jobs.stops[selectedOffer.drop].name));
  KoreanText.Panel(new Rect(x,viewport.yMax-118,w,62));bool blocked=!string.IsNullOrEmpty(reason);if(blocked)HudGlyph.Draw(new Rect(x+10,viewport.yMax-98,22,22),2,CasualUI.Amber);
  GUI.Label(new Rect(x+(blocked?40:10),viewport.yMax-116,w-(blocked?50:20),58),status,new GUIStyle(text){fontSize=MapFont(14),alignment=TextAnchor.MiddleLeft,normal={textColor=blocked?CasualUI.Amber:CasualUI.Ink}});
  // Keep a blocked action clickable so its rejection is visible instead of silently disabled.
  GUI.enabled=!locationOpen;if(GUI.Button(LogicalAcceptButtonBounds,"호출 수락",CasualUI.Action(MapFont(15)))){AcceptButtonClickCount++;AcceptSelected();}GUI.enabled=true;
 }
}
