using System.Collections.Generic;
using UnityEngine;

public partial class FullMapOverlay {
 public sealed class Place {public string name,description;public Vector3 position;public MapStop approach;public int glyph;public bool roadPoint;}
 public sealed class PaintedSegment {public Vector3 a,b;public Color color;public bool passenger;}
 public readonly List<PaintedSegment> RoutePaint=new List<PaintedSegment>();
 List<Place> cachedPlaces;MapData placesMap;float nextPlaces;
 public JobOffer PreviewOffer {get{return selectedOffer;}}
 List<Vector3> pickupPath=new List<Vector3>(),ridePath=new List<Vector3>();JobOffer cachedRide;
 public int PickupPreviewPointCount {get{return pickupPath.Count;}}
 public void ShowCalls(){selectedPlace=null;if(selectedOffer!=null&&SelectedOffer>=0&&!session.jobs.Active)SelectOffer(SelectedOffer);else {previewPath.Clear();RefreshRoutePaint(true);}}
 List<Vector3> previewPath=new List<Vector3>();Place selectedPlace;float nextPaint;
 public int PreviewPointCount {get{return previewPath.Count;}}
 public string SelectedPlaceName {get{return selectedPlace==null?"":selectedPlace.name;}}
 public List<Place> Places(){
  var map=session.world.map;if(cachedPlaces!=null&&placesMap==map&&Time.unscaledTime<nextPlaces)return cachedPlaces;placesMap=map;nextPlaces=Time.unscaledTime+1;var result=new List<Place>();
  foreach(var station in session.services.Stations)result.Add(new Place{name=station.name,position=station.pump,approach=station.approach,glyph=8,description="주유소\n입구로 진입해 주유 구역에 정차 후 F7\n연료를 최대 10%씩 추가합니다.\n"+session.services.Quote(0)});
  if(ProjectMapScene.Active(map)){foreach(var o in map.projectScene.objects)if(o.enabled&&o.IsService)result.Add(ServicePlace(o.name,o.Position,ProjectMapScene.ServiceStop(map,o),o.kind));}
  else {if(session.services.HasService(1))result.Add(ServicePlace("기사 식당",session.services.MealPosition,session.services.MealStop,"restaurant"));if(session.services.HasService(2))result.Add(ServicePlace("주차장",session.services.ParkingStop.V(),session.services.StopFor(2),"parking"));var r=session.services.RestArea;if(r!=null)result.Add(ServicePlace("휴식 주차",r.pump,r.approach,"rest"));}
  cachedPlaces=result;return result;
 }
 Place ServicePlace(string name,Vector3 p,MapStop stop,string kind){return new Place{name=name,position=p,approach=stop,glyph=kind=="restaurant"?9:kind=="rest"?11:10,description=kind=="restaurant"?"기사 식당\n제육백반 / 된장찌개 정식 / 소고기국밥\n각 3,000원 · 식사 50초 · 포만감 +70\n입구로 진입해 식당 앞 주차 칸에 정차 후 F7":kind=="rest"?"휴식 주차 구역\n무료 수면 · 100초\n주차 구역에 정차 후 F7":"생활 거점 주차장\n차량을 주차한 뒤 F7\n휴식 또는 하루 영업을 마칠 수 있습니다."};}
 public bool SelectPlace(Place place){if(!IsOpen||place==null||place.approach==null&&!place.roadPoint)return false;selectedPlace=place;selectedOffer=null;SelectedOffer=-1;previewPath=place.roadPoint?session.jobs.nav.PreviewPoint(place.position):session.jobs.nav.PreviewTo(place.approach);FitRoutes(previewPath);RefreshRoutePaint(true);return true;}
 public bool SelectRoadPoint(Vector3 point){if(!IsOpen)return false;var path=session.jobs.nav.PreviewPoint(point);if(path.Count<2)return false;var end=path[path.Count-1];MapStop stop=null;var graph=session.jobs.nav.hd;if(graph!=null){float at;int edge=graph.Nearest(end,end-path[path.Count-2],out at);var dir=graph.paths[edge].Tangent(at);stop=new MapStop{name="지도 목적지",x=end.x,z=end.z,dx=dir.x,dz=dir.z,graphStop=true,laneEdge=edge,laneAt=at};}return SelectPlace(new Place{name="선택한 도로 지점",position=end,approach=stop,glyph=3,roadPoint=stop==null,description="선택 위치 인근의 연결된 차로까지 안내합니다.\n정차 허용 장소를 뜻하지 않습니다. 주변 표지와 도로 상황을 확인하세요."});}

 public bool StartPlaceGuidance(){if(!IsOpen||previousPause||selectedPlace==null||previewPath.Count<2||session.jobs.Active)return false;var place=selectedPlace;Close();if(place.roadPoint)session.jobs.nav.SetDestination(place.position);else session.jobs.nav.SetDestination(place.approach);session.services.destination=place.name+" · 입구 앞까지 길안내";selectedPlace=null;previewPath.Clear();RefreshRoutePaint(true);return true;}
 public static Color CongestionColor(float delay){return delay>=6?new Color(.95f,.18f,.15f):delay>=2?new Color(1,.65f,.05f):new Color(.15f,.85f,.4f);}
 public void FitRoutes(params List<Vector3>[] paths){float minX=float.MaxValue,minZ=float.MaxValue,maxX=float.MinValue,maxZ=float.MinValue;foreach(var path in paths)foreach(var p in path){minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);minZ=Mathf.Min(minZ,p.z);maxZ=Mathf.Max(maxZ,p.z);}if(minX==float.MaxValue)return;float span=Mathf.Max(session.world.map.width,session.world.map.depth);center=new Vector2(.5f+((minX+maxX)/2-session.world.map.width/2)/span,.5f-((minZ+maxZ)/2-session.world.map.depth/2)/span);ZoomLevel=Mathf.Clamp(span/(Mathf.Max(maxX-minX,maxZ-minZ)+160),1,8);ClampCenter();}
 void PaintPath(List<Vector3> path,bool passenger){float traveled=0;for(int i=1;i<path.Count;i++){var a=path[i-1];var b=path[i];float length=Vector3.Distance(a,b);if(length<.001f)continue;float delay=session.roadConditions==null?0:session.roadConditions.ExtraSeconds(a,b,(b-a).normalized,3.2f);var color=CongestionColor(delay);if(!passenger)RoutePaint.Add(new PaintedSegment{a=a,b=b,color=color});else {float d=0;while(d<length){float phase=Mathf.Repeat(traveled+d,26);float step=Mathf.Min(length-d,(phase<16?16:26)-phase);step=Mathf.Max(.001f,step);if(phase<16)RoutePaint.Add(new PaintedSegment{a=Vector3.Lerp(a,b,d/length),b=Vector3.Lerp(a,b,Mathf.Min(length,d+step)/length),color=color,passenger=true});d+=step;}}traveled+=length;}}

 public void RefreshRoutePaint(bool force=false){if(session==null||session.jobs==null||!force&&Time.unscaledTime<nextPaint)return;nextPaint=Time.unscaledTime+1;var jobs=session.jobs;if(selectedOffer!=null&&(jobs.Active||SelectedOffer<0||SelectedOffer>=jobs.offers.Length||jobs.offers[SelectedOffer]!=selectedOffer)){selectedOffer=null;SelectedOffer=-1;}RoutePaint.Clear();
  if(IsOpen&&selectedPlace!=null){PaintPath(previewPath,false);return;}
  if(!jobs.Active&&selectedOffer!=null){PaintPath(pickupPath,false);PaintPath(previewPath,true);return;}
  PaintPath(jobs.nav.path,jobs.trip.State==TripState.OnTrip);
  if(jobs.trip.State==TripState.ToPickup&&jobs.current!=null){if(cachedRide!=jobs.current){cachedRide=jobs.current;ridePath=jobs.nav.Find(jobs.stops[jobs.current.pickup],jobs.stops[jobs.current.drop]);}PaintPath(ridePath,true);}else cachedRide=null;
 }

 Vector2 facilityScroll;
 public string ApproachSummary {get{var jobs=session.jobs;var p=jobs.Active?jobs.nav.path:pickupPath;float d=0;for(int i=1;i<p.Count;i++)d+=Vector3.Distance(p[i-1],p[i]);return p.Count<2?"승차 경로를 찾지 못했습니다":"내 차 → 승차 · "+d.ToString("0")+"m · 하늘색 실선";}}
 void DrawSidebar(float x,float w,Rect viewport){var jobs=session.jobs;GUI.Label(new Rect(x,90,w,34),"주변 호출 보기 (M)",heading);float top=132;
  if(selectedPlace!=null){float height=Mathf.Min(270,viewport.height*.44f);KoreanText.Panel(new Rect(x,top,w,height));var detail=selectedPlace.name+"\n\n"+selectedPlace.description;float length=0;for(int i=1;i<previewPath.Count;i++)length+=Vector3.Distance(previewPath[i-1],previewPath[i]);float body=height-100;float ch=Mathf.Max(body,text.CalcHeight(new GUIContent(detail),w-36)+12);facilityScroll=GUI.BeginScrollView(new Rect(x+8,top+8,w-16,body),facilityScroll,new Rect(0,0,w-36,ch));GUI.Label(new Rect(0,0,w-36,ch),detail,text);GUI.EndScrollView();GUI.Label(new Rect(x+8,top+height-90,w-16,32),previewPath.Count<2?"연결 경로 없음":"입구까지 약 "+length.ToString("0")+"m",new GUIStyle(text){font=KoreanText.BoldFont,fontSize=15});GUI.enabled=!locationOpen&&!previousPause&&!jobs.Active&&previewPath.Count>1;if(GUI.Button(new Rect(x+8,top+height-48,w-16,40),jobs.Active?"운송 완료 후 길안내 가능":"시설 길안내 시작",button))StartPlaceGuidance();GUI.enabled=!locationOpen;top+=height+12;}
  else {GUI.Label(new Rect(x,top,w,50),selectedOffer!=null||jobs.trip.State==TripState.ToPickup?ApproachSummary:"번호를 선택하면 승차·운송 경로를 표시합니다",text);top+=54;}
  GUI.Label(new Rect(x,top,w,28),"호출 목록 · 점선은 승객 운송 경로",new GUIStyle(text){font=KoreanText.BoldFont,fontSize=14});top+=32;
  float listHeight=Mathf.Max(90,viewport.yMax-top-60),rw=w-24;float content=0;var rows=new System.Collections.Generic.List<string>();var indices=new System.Collections.Generic.List<int>();var heights=new System.Collections.Generic.List<float>();
  if(!jobs.Active)for(int i=0;i<jobs.offers.Length;i++){var o=jobs.offers[i];if(o==null)continue;var caption=(i+1)+"  "+KoreanText.Stop(jobs.stops[o.pickup].name)+"\n→ "+KoreanText.Stop(jobs.stops[o.drop].name)+"\n"+o.fare.ToString("N0")+"원 · 기한 "+RoadNavigation.FormatEta(o.seconds);float height=Mathf.Max(104,button.CalcHeight(new GUIContent(caption),rw-20)+24);rows.Add(caption);indices.Add(i);heights.Add(height);content+=height+10;}
  var chosen=jobs.Active?jobs.current:selectedOffer;float foot=content;if(chosen!=null)content+=100;if(jobs.Active)content+=44;
  filterScroll=GUI.BeginScrollView(new Rect(x,top,w,listHeight),filterScroll,new Rect(0,0,w-20,Mathf.Max(listHeight,content)));float y=0;
  for(int k=0;k<rows.Count;k++){int index=indices[k];var old=GUI.backgroundColor;if(index==SelectedOffer)GUI.backgroundColor=new Color(.35f,1,.8f);if(GUI.Button(new Rect(0,y,rw,heights[k]),rows[k],new GUIStyle(button){alignment=TextAnchor.MiddleLeft,padding=new RectOffset(10,10,4,4)}))SelectOffer(index);GUI.backgroundColor=old;y+=heights[k]+10;}
  if(chosen!=null){if(GUI.Button(new Rect(0,foot,rw,42),"승차 · "+KoreanText.Stop(jobs.stops[chosen.pickup].name),button))FocusStop(chosen.pickup);if(GUI.Button(new Rect(0,foot+50,rw,42),"하차 · "+KoreanText.Stop(jobs.stops[chosen.drop].name),button))FocusStop(chosen.drop);}
  if(jobs.Active)GUI.Label(new Rect(0,foot+100,rw,44),"운송 중 · 신규 호출은 운송 완료 후",text);GUI.EndScrollView();GUI.enabled=!locationOpen&&!previousPause&&!jobs.Active&&selectedPlace==null&&selectedOffer!=null&&session.business.CanAccept;if(GUI.Button(new Rect(x,viewport.yMax-50,w,44),"선택한 호출 수락",button))AcceptSelected();GUI.enabled=true;
 }
}
