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
 float FacilityCardHeight(FacilityOption option,float width){
  var item=Refreshment.For(option.Kind);bool quoted=option.Quote!=null&&option.Quote.Available;
  string reason=FacilityGuideBlocked(option);float action=reason.Length==0?40:Mathf.Max(40,button.CalcHeight(new GUIContent(reason),width-16));
  return 46+64+(item!=null?26:0)+(quoted?52:28)+action+12;
 }
 void DrawFacilityCard(Rect r,FacilityOption option,bool nearest){
  var bold=new GUIStyle(BoldLine){fontSize=MapFont(15)};var plain=PlainLine;var small=new GUIStyle(PlainLine){fontSize=MapFont(12)};small.normal.textColor=CasualUI.Muted;
  CasualUI.Panel(r);var tip=FacilityPlannerDetails(option);GUI.Label(r,new GUIContent("",tip));
  // Name band. The badge sits inside it so every card keeps the same rhythm.
  var band=new Rect(r.x+4,r.y+4,r.width-8,38);CasualUI.RoundedFill(band,nearest?CasualUI.Green:CasualUI.Blue);
  float badge=nearest?86:0;var nameStyle=new GUIStyle(bold){fontSize=MapFont(16)};nameStyle.normal.textColor=Color.white;
  GUI.Label(new Rect(band.x+10,band.y,band.width-20-badge,band.height),CasualUI.PlainName(option.Name),nameStyle);
  if(nearest){var b=new Rect(band.xMax-badge-4,band.y+7,badge,24);CasualUI.RoundedFill(b,Color.white);var bs=new GUIStyle(small){alignment=TextAnchor.MiddleCenter,font=KoreanText.BoldFont};bs.normal.textColor=CasualUI.Green;GUI.Label(b,"가장 가까움",bs);}
  float y=r.y+46,cx=r.x+10,cw=r.width-20;
  // Facts row: distance/time, queue, price.
  string travel=option.Reachable?(option.TravelMetres/1000f).ToString("0.0")+"km · "+Mathf.Max(1,Mathf.RoundToInt(option.TravelSeconds/60f))+"분":"경로 없음";
  string wait=!option.QueueKnown?"확인 중":option.Waiting>0?"대기 "+option.Waiting:option.PhysicallyOccupied?"이용 중":"대기 0";
  var quote=option.Quote;bool quoted=quote!=null&&quote.Available;
  // Row 1: how far and how long. Row 2: price (bold) and the queue.
  Icon(new Rect(cx,y,cw*.62f,28),3,CasualUI.Blue,new GUIContent(travel,"입구까지 거리 · 진입·출차 포함 이동 시간"),bold);
  Icon(new Rect(cx+cw*.62f,y,cw*.38f,28),1,option.Waiting>0||option.PhysicallyOccupied?CasualUI.Amber:CasualUI.Muted,new GUIContent(wait,"지금 기다리는 차량"),plain);
  y+=30;
  string price=quoted?(quote.Price==0?"무료":quote.Price.ToString("N0")+"원"):"견적 불가";
  Icon(new Rect(cx,y,cw*.62f,28),4,CasualUI.Amber,new GUIContent(price,quoted&&quote.NewDebt>0?"미납 "+quote.NewDebt.ToString("N0")+"원 발생":"이용 요금"),bold);
  if(quoted)Icon(new Rect(cx+cw*.62f,y,cw*.38f,28),18,CasualUI.Muted,new GUIContent("이용 "+quote.UseSeconds.ToString("0")+"초","시설 이용에 걸리는 시간"),plain);
  y+=34;
  var item=Refreshment.For(option.Kind);if(item!=null){GUI.Label(new Rect(cx,y,cw,22),item.Menu,small);y+=26;}
  if(quoted){
   var account=session.business.ledger.Account;var driver=session.business.ledger.Driver;float col=(cw-16)/3;
   Gauge(new Rect(cx,y,col,46),"연료",account!=null?(float)account.Fuel:(float)quote.EndFuel,(float)quote.EndFuel,false);
   Gauge(new Rect(cx+col+8,y,col,46),"포만",driver!=null?(float)driver.satiety:(float)quote.EndSatiety,(float)quote.EndSatiety,false);
   Gauge(new Rect(cx+(col+8)*2,y,col,46),"피로",driver!=null?(float)driver.fatigue:(float)quote.EndFatigue,(float)quote.EndFatigue,true);
   y+=52;
  } else {var warn=new GUIStyle(plain);warn.normal.textColor=CasualUI.Danger;GUI.Label(new Rect(cx,y,cw,24),"지금은 이용할 수 없음",warn);y+=28;}
  string reason=FacilityGuideBlocked(option),caption=reason.Length==0?"길 안내":reason;float actionHeight=r.yMax-y-8;
  GUI.enabled=!locationOpen&&reason.Length==0;
  if(GUI.Button(new Rect(cx-2,y,cw+4,actionHeight),new GUIContent(caption,"입구까지 길 안내만 합니다 · 결제는 도착 후 F"),nearest&&reason.Length==0?CasualUI.Action(button.fontSize):button))GuidePlannerFacility(option);
  GUI.enabled=!locationOpen;
 }
 // One gauge: label and the value after use, a bar of the value now (pale) and after (solid), and the change.
 // For fatigue lower is better, so a fall is drawn green.
 void Gauge(Rect r,string label,float now,float after,bool lowerIsBetter){
  var style=new GUIStyle(PlainLine){fontSize=MapFont(13)};float delta=after-now;bool better=lowerIsBetter?delta<-.5f:delta>.5f,worse=lowerIsBetter?delta>.5f:delta<-.5f;
  GUI.Label(new Rect(r.x,r.y,r.width,22),label+" "+after.ToString("0"),style);
  if(Mathf.Abs(delta)>=.5f){var d=new GUIStyle(style){alignment=TextAnchor.MiddleRight,font=KoreanText.BoldFont};d.normal.textColor=better?CasualUI.Green:worse?CasualUI.Danger:CasualUI.Muted;GUI.Label(new Rect(r.x,r.y,r.width,22),(delta>0?"+":"")+delta.ToString("0"),d);}
  var bar=new Rect(r.x,r.y+26,r.width,10);CasualUI.RoundedFill(bar,CasualUI.Track);
  float a=Mathf.Clamp01(now/100f),b=Mathf.Clamp01(after/100f);var fill=better?CasualUI.Green:worse?CasualUI.Danger:CasualUI.Blue;
  if(Mathf.Max(a,b)>0)CasualUI.RoundedFill(new Rect(bar.x,bar.y,bar.width*Mathf.Max(a,b),bar.height),new Color(fill.r,fill.g,fill.b,.3f));
  if(Mathf.Min(a,b)>0)CasualUI.RoundedFill(new Rect(bar.x,bar.y,bar.width*Mathf.Min(a,b),bar.height),fill);
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
  // 2026-11-24 play-test round 2: the cards were paragraphs. Each card is now a name band, one row of
  // icon facts (distance, wait, price) and three gauges showing now -> after for fuel, satiety and fatigue.
  // The nearest reachable option gets a badge and the filled button; the old sentence stays as hover text.
  int nearest=-1;for(int i=0;i<options.Count;i++)if(options[i].Reachable&&options[i].Quote!=null&&options[i].Quote.Available&&(nearest<0||options[i].TravelSeconds<options[nearest].TravelSeconds))nearest=i;
  for(int i=0;i<options.Count;i++)heights[i]=FacilityCardHeight(options[i],inner);foreach(var h in heights)total+=h+10;
  float visible=Mathf.Max(72,viewport.yMax-top-54);facilityPlannerScroll=GUI.BeginScrollView(new Rect(x,top,w,visible),facilityPlannerScroll,new Rect(0,0,w-20,Mathf.Max(visible,total)));
  if(options.Count==0)GUI.Label(new Rect(0,0,inner,80),"지원하는 시설이 없습니다.\n다른 서비스를 선택해 주세요.",detail);
  float y=0;for(int i=0;i<options.Count;i++){DrawFacilityCard(new Rect(0,y,inner,heights[i]),options[i],i==nearest);y+=heights[i]+10;}
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
