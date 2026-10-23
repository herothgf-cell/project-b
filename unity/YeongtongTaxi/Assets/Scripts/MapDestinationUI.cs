using System.Collections.Generic;
using UnityEngine;

public partial class FullMapOverlay {
 public sealed class Place {public string name,description;public Vector3 position;public MapStop approach;public int glyph;public bool roadPoint;}
 public sealed class PaintedSegment {public Vector3 a,b;public Color color;}
 public readonly List<PaintedSegment> RoutePaint=new List<PaintedSegment>();
 List<Place> cachedPlaces;MapData placesMap;float nextPlaces;
 public JobOffer PreviewOffer {get{return selectedOffer;}}
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
 public bool SelectPlace(Place place){if(!IsOpen||place==null||place.approach==null&&!place.roadPoint)return false;selectedPlace=place;selectedOffer=null;SelectedOffer=-1;previewPath=place.roadPoint?session.jobs.nav.PreviewPoint(place.position):session.jobs.nav.PreviewTo(place.approach);FocusWorld(place.position);RefreshRoutePaint(true);return true;}
 public bool SelectRoadPoint(Vector3 point){if(!IsOpen)return false;var path=session.jobs.nav.PreviewPoint(point);if(path.Count<2)return false;var end=path[path.Count-1];MapStop stop=null;var graph=session.jobs.nav.hd;if(graph!=null){float at;int edge=graph.Nearest(end,end-path[path.Count-2],out at);var dir=graph.paths[edge].Tangent(at);stop=new MapStop{name="지도 목적지",x=end.x,z=end.z,dx=dir.x,dz=dir.z,graphStop=true,laneEdge=edge,laneAt=at};}return SelectPlace(new Place{name="선택한 도로 지점",position=end,approach=stop,glyph=3,roadPoint=stop==null,description="선택 위치 인근의 연결된 차로까지 안내합니다.\n정차 허용 장소를 뜻하지 않습니다. 주변 표지와 도로 상황을 확인하세요."});}

 public bool StartPlaceGuidance(){if(!IsOpen||previousPause||selectedPlace==null||previewPath.Count<2||session.jobs.Active)return false;var place=selectedPlace;Close();if(place.roadPoint)session.jobs.nav.SetDestination(place.position);else session.jobs.nav.SetDestination(place.approach);session.services.destination=place.name+" · 입구 앞까지 길안내";selectedPlace=null;previewPath.Clear();RefreshRoutePaint(true);return true;}
 public static Color CongestionColor(float delay){return delay>=6?new Color(.95f,.18f,.15f):delay>=2?new Color(1,.65f,.05f):new Color(.15f,.85f,.4f);}
 public void RefreshRoutePaint(bool force=false){if(session==null||session.jobs==null||!force&&Time.unscaledTime<nextPaint)return;nextPaint=Time.unscaledTime+1;if(selectedOffer!=null&&(session.jobs.Active||SelectedOffer<0||SelectedOffer>=session.jobs.offers.Length||session.jobs.offers[SelectedOffer]!=selectedOffer)){selectedOffer=null;SelectedOffer=-1;}RoutePaint.Clear();var path=IsOpen&&selectedPlace!=null||!session.jobs.Active&&selectedOffer!=null?previewPath:session.jobs.nav.path;for(int i=1;i<path.Count;i++){var a=path[i-1];var b=path[i];float delay=session.roadConditions==null?0:session.roadConditions.ExtraSeconds(a,b,(b-a).normalized,3.2f);RoutePaint.Add(new PaintedSegment{a=a,b=b,color=CongestionColor(delay)});}}
 void DrawPlaceDetails(float x,float w,Rect viewport){GUI.enabled=!locationOpen;GUI.Label(new Rect(x,140,w,42),selectedPlace.name,heading);GUI.Label(new Rect(x,190,w,210),selectedPlace.description,text);float length=0;for(int i=1;i<previewPath.Count;i++)length+=Vector3.Distance(previewPath[i-1],previewPath[i]);GUI.Label(new Rect(x,410,w,85),previewPath.Count<2?"연결된 경로를 찾지 못했습니다.":"입구까지 약 "+length.ToString("0")+"m\n경로: 초록 원활 · 주황 서행 · 빨강 정체",text);if(session.jobs.Active)GUI.Label(new Rect(x,510,w,70),"운송 중에는 시설 정보를 확인할 수 있습니다. 길안내는 운송을 마친 뒤 시작하세요.",text);if(GUI.Button(new Rect(x,viewport.yMax-104,w,42),"주변 호출 보기",button)){selectedPlace=null;previewPath.Clear();RefreshRoutePaint(true);}GUI.enabled=!locationOpen&&!previousPause&&!session.jobs.Active&&previewPath.Count>1;if(GUI.Button(new Rect(x,viewport.yMax-50,w,44),"길안내 시작",button))StartPlaceGuidance();GUI.enabled=true;}
}
