using UnityEngine;

// The map's existing modal owns pause/input. Browsing these cached views never
// purchases a service or changes an accepted passenger's destination.
public partial class FullMapOverlay {
 public bool FacilityPlannerVisible {get;private set;}
 int facilityPlannerKind;Vector2 facilityPlannerScroll;
 static readonly int[] plannerKinds={0,1,3,4,5};
 static readonly string[] plannerLabels={"주유","식사","수면","세차","수리"};
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
  string travel=option.Reachable?"이동 약 "+Mathf.CeilToInt(option.TravelSeconds)+"초 / "+Mathf.CeilToInt(option.TravelMetres)+"m · 진입·출차 포함":"연결 경로·이동시간 확인 필요";
  string wait=option.Waiting>0?"대기 "+option.Waiting+"대 · 대기시간 미정":option.PhysicallyOccupied?"정차 공간 점유 · 대기시간 미정":"현재 대기 차량 없음";
  var quote=option.Quote;if(quote==null||!quote.Available)return travel+"\n"+wait+"\n현재 이용 견적 불가 · 현장 조건을 확인하세요";
  string price="이용 "+quote.UseSeconds.ToString("0")+"초 · "+quote.Price.ToString("N0")+"원"+(quote.NewDebt>0?" (미납 예상 "+quote.NewDebt.ToString("N0")+"원)":"");
  string effects="이용 직후 예상: 연료 "+quote.EndFuel.ToString("0.0")+"% · 포만 "+quote.EndSatiety.ToString("0")+" · 피로 "+quote.EndFatigue.ToString("0");
  if(option.Kind==4)effects+="\n청결 100 회복";else if(option.Kind==5)effects+="\n내구도 100 회복";
  return travel+"\n"+wait+"\n"+price+"\n"+effects;
 }
 void DrawFacilityPlannerSidebar(float x,float w,Rect viewport){
  var detail=new GUIStyle(text){fontSize=14,wordWrap=true};
  GUI.Label(new Rect(x,90,w,34),"생활시설 비교",heading);float top=132,cell=(w-12)/3;
  for(int i=0;i<plannerKinds.Length;i++){
   var old=GUI.backgroundColor;if(facilityPlannerKind==plannerKinds[i])GUI.backgroundColor=new Color(.35f,1,.8f);
   if(GUI.Button(new Rect(x+(i%3)*(cell+6),top+(i/3)*40,cell,34),plannerLabels[i],button)){facilityPlannerKind=plannerKinds[i];facilityPlannerScroll=Vector2.zero;}
   GUI.backgroundColor=old;
  }
  top+=84;string note="선택하면 입구까지 길안내만 시작합니다.\n결제는 현장 정차 후 F7에서 확정하세요.";
  if(session.jobs.trip.State==TripState.OnTrip)note="승객 운송 중 · 시설 정보를 비교할 수 있습니다.\n길안내는 운송을 마친 뒤 선택하세요.";
  else if(session.jobs.trip.State==TripState.ToPickup)note+="\n주유·세차·수리는 수락한 호출을 유지합니다.";
  float noteHeight=detail.CalcHeight(new GUIContent(note),w);GUI.Label(new Rect(x,top,w,noteHeight),note,detail);top+=noteHeight+10;
  var options=session.services.FacilityOptions(facilityPlannerKind);float inner=w-24,total=0;var heights=new float[options.Count];
  for(int i=0;i<options.Count;i++){var option=options[i];string blocked=FacilityGuideBlocked(option);float actionHeight=Mathf.Max(36,button.CalcHeight(new GUIContent(blocked.Length==0?"이 시설로 길안내 · 결제 없음":blocked),inner-16));heights[i]=text.CalcHeight(new GUIContent(option.Name),inner-16)+detail.CalcHeight(new GUIContent(FacilityPlannerDetails(option)),inner-16)+actionHeight+40;total+=heights[i]+10;}
  float visible=Mathf.Max(72,viewport.yMax-top-54);facilityPlannerScroll=GUI.BeginScrollView(new Rect(x,top,w,visible),facilityPlannerScroll,new Rect(0,0,w-20,Mathf.Max(visible,total)));
  if(options.Count==0)GUI.Label(new Rect(0,0,inner,80),"지원하는 시설이 없습니다.\n다른 서비스를 선택해 주세요.",detail);
  float y=0;for(int i=0;i<options.Count;i++){
   var option=options[i];float height=heights[i],nameHeight=text.CalcHeight(new GUIContent(option.Name),inner-16);KoreanText.Panel(new Rect(0,y,inner,height));
   GUI.Label(new Rect(8,y+8,inner-16,nameHeight),option.Name,text);
   string reason=FacilityGuideBlocked(option),caption=reason.Length==0?"이 시설로 길안내 · 결제 없음":reason;float actionHeight=Mathf.Max(36,button.CalcHeight(new GUIContent(caption),inner-16));
   GUI.Label(new Rect(8,y+nameHeight+12,inner-16,height-nameHeight-actionHeight-26),FacilityPlannerDetails(option),detail);
   GUI.enabled=!locationOpen&&reason.Length==0;
   if(GUI.Button(new Rect(8,y+height-actionHeight-8,inner-16,actionHeight),caption,button))GuidePlannerFacility(option);
   GUI.enabled=!locationOpen;y+=height+10;
  }
  GUI.EndScrollView();
  if(GUI.Button(new Rect(x,viewport.yMax-44,w,38),"호출·선택한 장소로 돌아가기",button))HideFacilityPlanner();
 }
}

public partial class ServiceStops {
 public bool OpenFacilityPlanner(int kind=0){
  if(pending>=0||session.jobs.trip.State==TripState.OnTrip)return false;var map=session.GetComponent<FullMapOverlay>();if(!map)return false;
  if(open)Close(false);if(!map.IsOpen&&!map.Open())return false;return map.ShowFacilityPlanner(kind);
 }
}
