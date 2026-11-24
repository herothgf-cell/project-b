using UnityEngine;

// The map's existing modal owns pause/input. Browsing these cached views never
// purchases a service or changes an accepted passenger's destination.
public partial class FullMapOverlay {
 public bool FacilityPlannerVisible {get;private set;}
 int facilityPlannerKind;Vector2 facilityPlannerScroll;
 static readonly int[] plannerKinds={0,1,3,4,5,Refreshment.Snack};
 static readonly string[] plannerLabels={"주유","식사","수면","세차","수리","간식·커피"};
 public string SelectedOfferForecastText {get{
  if(selectedOffer==null||SelectedOffer<0||SelectedOffer>=session.jobs.offers.Length||session.jobs.offers[SelectedOffer]!=selectedOffer)return "";
  var forecast=OperationForecastAdapter.ForSelection(session,selectedOffer);
  return "선택한 호출 · 픽업+운송 후 예상\n"+(forecast.Available?"총 이동 약 "+RoadNavigation.FormatEta((float)forecast.TravelSeconds)+" / "+forecast.TravelMetres.ToString("0")+"m\n":"")+forecast.Text;
 }}
 public bool ShowFacilityPlanner(int kind=0){
  if(!IsOpen)return false;facilityPlannerKind=System.Array.IndexOf(plannerKinds,kind)>=0?kind:0;facilityPlannerScroll=Vector2.zero;FacilityPlannerVisible=true;return true;
 }
 public void HideFacilityPlanner(){FacilityPlannerVisible=false;}
 string FacilityGuideBlocked(FacilityOption option){
  if(!IsOpen)return "먼저 지도를 열어 주세요";
  if(session.simulationMatch&&session.simulationMatch.Finished)return "경기가 종료되었습니다";
  if(previousPause)return "일시정지를 해제한 뒤 길안내를 시작하세요";
  if(option==null||!option.Reachable)return "연결 경로·예상시간 확인 필요";
  if(session.jobs.trip.State==TripState.OnTrip)return "운송 중 · 시설 정보만 확인할 수 있습니다";
  if(session.jobs.Active&&option.Kind!=0&&option.Kind!=4&&option.Kind!=5)return "승차 호출 진행 중 · 주유·세차·수리 길안내만 가능";
  return "";
 }
 public bool GuidePlannerFacility(FacilityOption option){
  if(FacilityGuideBlocked(option).Length>0||!session.services.GuideFacility(option))return false;
  selectedPlace=null;selectedOffer=null;SelectedOffer=-1;previewPath.Clear();FacilityPlannerVisible=false;Close();RefreshRoutePaint(true);return true;
 }
 static string FacilityPlannerDetails(FacilityOption option){
  // 2026-11-24 text reduction: one line each for the trip, the cost and the result.
  string travel=option.Reachable?(option.TravelMetres/1000f).ToString("0.0")+"km · "+Mathf.Max(1,Mathf.RoundToInt(option.TravelSeconds/60f))+"분":"경로 없음";
  string wait=!option.QueueKnown?"대기 확인 중":option.Waiting>0?"대기 "+option.Waiting+"대":option.PhysicallyOccupied?"이용 중":"대기 0";
  var quote=option.Quote;if(quote==null||!quote.Available)return travel+"   "+wait+"\n지금은 이용할 수 없음";
  string price=quote.UseSeconds.ToString("0")+"초 · "+quote.Price.ToString("N0")+"원"+(quote.NewDebt>0?" (미납 "+quote.NewDebt.ToString("N0")+"원)":"");
  string effects="연료 "+quote.EndFuel.ToString("0")+" · 포만 "+quote.EndSatiety.ToString("0")+" · 피로 "+quote.EndFatigue.ToString("0");
  if(option.Kind==4)effects+=" · 청결 100";else if(option.Kind==5)effects+=" · 내구 100";
  var item=Refreshment.For(option.Kind);
  return (item!=null?item.Menu+"\n":"")+travel+"   "+wait+"\n"+price+"\n"+effects;
 }
 void DrawFacilityPlannerSidebar(float x,float w,Rect viewport){
  var detail=new GUIStyle(text){fontSize=MapFont(14),wordWrap=true};
  CasualUI.Header(new Rect(x,86,w,38),"생활시설",3);CasualUI.Info(new Rect(x+w-34,93,24,24),"고르면 입구까지 길 안내만 시작합니다. 결제는 도착해서 정차한 뒤 F로 합니다.",true);float top=132,cell=(w-12)/3;
  for(int i=0;i<plannerKinds.Length;i++){
   if(plannerKinds[i]==Refreshment.Snack&&session.services.RefreshmentShops.Count==0)continue;
   var tab=facilityPlannerKind==plannerKinds[i]?CasualUI.Action(button.fontSize):button;
   string label=plannerKinds[i]==5&&session.business.SupportsCareer?"수리·부품":plannerLabels[i];
   if(GUI.Button(new Rect(x+(i%3)*(cell+6),top+(i/3)*40,cell,34),label,tab)){facilityPlannerKind=plannerKinds[i];facilityPlannerScroll=Vector2.zero;}
  }
  top+=84;string note=session.jobs.trip.State==TripState.OnTrip?"운송을 마친 뒤 길 안내를 받을 수 있어요":"";
  if(note.Length>0){float noteHeight=detail.CalcHeight(new GUIContent(note),w);GUI.Label(new Rect(x,top,w,noteHeight),note,detail);top+=noteHeight+10;}
  var options=session.services.FacilityOptions(facilityPlannerKind);float inner=w-24,total=0;var heights=new float[options.Count];
  for(int i=0;i<options.Count;i++){var option=options[i];string blocked=FacilityGuideBlocked(option);float actionHeight=Mathf.Max(36,button.CalcHeight(new GUIContent(blocked.Length==0?"길 안내":blocked),inner-16));heights[i]=text.CalcHeight(new GUIContent(CasualUI.PlainName(option.Name)),inner-16)+detail.CalcHeight(new GUIContent(FacilityPlannerDetails(option)),inner-16)+actionHeight+46;total+=heights[i]+10;}
  float visible=Mathf.Max(72,viewport.yMax-top-54);facilityPlannerScroll=GUI.BeginScrollView(new Rect(x,top,w,visible),facilityPlannerScroll,new Rect(0,0,w-20,Mathf.Max(visible,total)));
  if(options.Count==0)GUI.Label(new Rect(0,0,inner,80),"지원하는 시설이 없습니다.\n다른 서비스를 선택해 주세요.",detail);
  float y=0;for(int i=0;i<options.Count;i++){
   var option=options[i];float height=heights[i],nameHeight=text.CalcHeight(new GUIContent(CasualUI.PlainName(option.Name)),inner-16);CasualUI.Panel(new Rect(0,y,inner,height));
   CasualUI.RoundedFill(new Rect(4,y+4,inner-8,nameHeight+10),CasualUI.Blue);
   var nameStyle=new GUIStyle(text);nameStyle.normal.textColor=Color.white;GUI.Label(new Rect(8,y+8,inner-16,nameHeight),CasualUI.PlainName(option.Name),nameStyle);
   string reason=FacilityGuideBlocked(option),caption=reason.Length==0?"길 안내":reason;float actionHeight=Mathf.Max(36,button.CalcHeight(new GUIContent(caption),inner-16));
   GUI.Label(new Rect(8,y+nameHeight+18,inner-16,height-nameHeight-actionHeight-32),FacilityPlannerDetails(option),detail);
   GUI.enabled=!locationOpen&&reason.Length==0;
   if(GUI.Button(new Rect(8,y+height-actionHeight-8,inner-16,actionHeight),caption,button))GuidePlannerFacility(option);
   GUI.enabled=!locationOpen;y+=height+10;
  }
  GUI.EndScrollView();
  if(GUI.Button(new Rect(x,viewport.yMax-44,w,38),"호출 목록으로",button))HideFacilityPlanner();
  CasualUI.FlushTooltip(x+w);
 }
}

public partial class ServiceStops {
 public bool OpenFacilityPlanner(int kind=0){
  if(pending>=0||session.jobs.trip.State==TripState.OnTrip)return false;var map=session.GetComponent<FullMapOverlay>();if(!map)return false;
  if(open)Close(false);if(!map.IsOpen&&!map.Open())return false;return map.ShowFacilityPlanner(kind);
 }
}
