using System;
using System.Collections;
using System.IO;
using UnityEngine;

// Explicit command-line fixture only. Uses UnifiedModeProbe's temporary save route.
// Poses verify UI/state integration, not automatic driving or manual play.
public sealed partial class BrightUIProbe:MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-brightUiCheck")>=0&&!FindObjectOfType<BrightUIProbe>()){var root=new GameObject("Bright UI acceptance");DontDestroyOnLoad(root);root.AddComponent<BrightUIProbe>();}}
 bool ok=true,done;int checks;float started;string output;DriveSession s;
 bool FullUiReview {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-fullUiReview")>=0;}}
 void Check(bool value,string text){checks++;ok&=value;Debug.Log("BRIGHT_UI "+(value?"PASS ":"FAIL ")+text);}
 void Awake(){started=Time.realtimeSinceStartup;var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-brightUiOutput");output=at>=0&&at+1<args.Length?args[at+1]:Path.GetFullPath("artifacts/ui-bright/player");Directory.CreateDirectory(output);Application.runInBackground=true;}
 void Update(){if(!done&&Time.realtimeSinceStartup-started>(FullUiReview?420:240)){done=true;Debug.LogError("BRIGHT_UI_TIMEOUT");Application.Quit(2);}}
 IEnumerator Start(){var run=Run();while(!done){object next;try{if(!run.MoveNext())yield break;next=run.Current;}catch(Exception e){done=true;Debug.LogError("BRIGHT_UI_EXCEPTION "+e);Application.Quit(2);yield break;}yield return next;}}
 IEnumerator Capture(string name){
  var path=Path.Combine(output,name+".png");bool rendered=false;
  for(int attempt=0;attempt<3&&!rendered;attempt++){
   yield return new WaitForSecondsRealtime(.5f);yield return new WaitForEndOfFrame();var frame=ScreenCapture.CaptureScreenshotAsTexture();
   try{int bright=0;for(int y=0;y<frame.height;y+=Mathf.Max(1,frame.height/20))for(int x=0;x<frame.width;x+=Mathf.Max(1,frame.width/20)){var p=frame.GetPixel(x,y);if(p.maxColorComponent>.15f)bright++;}rendered=bright>20;if(rendered)File.WriteAllBytes(path,frame.EncodeToPNG());}finally{Destroy(frame);}
  }
  Check(rendered&&File.Exists(path),"capture rendered frame "+name);
 }
 bool longDialoguePreview,carePreview;
 void OnGUI(){
  if(carePreview){var matrix=GUI.matrix;GUI.depth=-50;float scale=CockpitUI.Scale;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));float y=Screen.height/scale-238;CasualUI.Panel(new Rect(198,y+48,274,72));CockpitUI.DrawCareMessage(new Rect(210,y+55,250,58),"도착 완료 · 손님이 일어나지 않습니다 · 10초 대기 · 5초 뒤 F 다시 깨우기 / 30초 자동 하차 · 안전 정차 유지");GUI.matrix=matrix;return;}
  if(!longDialoguePreview)return;GUI.depth=-50;
  string line="긴 장소명과 사용자 대사가 줄바꿈되어도 모든 내용을 읽을 수 있어야 합니다. ";
  int selected=PassengerDialogueUI.Draw("가독성 검사 · 긴 대화",line+line+line+line,new[]{
   new PassengerDialogueUI.Option("Y","지금은 선택할 수 없는 응답",line+line,false),
   new PassengerDialogueUI.Option("I","긴 효과와 조건을 확인하는 응답",line+line),
   new PassengerDialogueUI.Option("O","아래쪽 선택지도 스크롤로 확인",line),
   new PassengerDialogueUI.Option("U","기존 운행 유지","추가 효과 없음")},"검사 전용 · 게임 진행에 반영하지 않음");
  if(selected==0)Check(false,"disabled long dialogue choice cannot be clicked");
 }
 IEnumerator ReviewSpeed(){
  var velocity=s.taxi.body.velocity;float timeScale=Time.timeScale;Time.timeScale=0;
  foreach(int width in new[]{1280,1920}){Screen.SetResolution(width,width*9/16,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.5f);foreach(float kmh in new[]{0f,35f,70f,140f,-35f}){s.taxi.body.velocity=s.taxi.transform.forward*(kmh/3.6f);Check(Mathf.Abs(s.taxi.Speed*3.6f-kmh)<.01f,"speed fixture "+kmh);yield return Capture("speed-"+width+"-"+kmh);}}
  s.taxi.body.velocity=velocity;Time.timeScale=timeScale;
 }
 IEnumerator ReviewDialogue(){
  foreach(int width in new[]{1280,1920}){Screen.SetResolution(width,width*9/16,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.5f);yield return Capture("dialogue-single-"+width);}
 }
 IEnumerator ReviewLongDialogue(){
  float timeScale=Time.timeScale;Time.timeScale=0;longDialoguePreview=true;
  foreach(int width in new[]{1280,1920}){
   Screen.SetResolution(width,width*9/16,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.5f);yield return Capture("dialogue-long-"+width);
   var scroll=typeof(PassengerDialogueUI).GetField("scroll",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);scroll.SetValue(null,new Vector2(0,100000));yield return Capture("dialogue-long-bottom-"+width);
   var position=(Vector2)scroll.GetValue(null);Check(position.y>0&&position.y<100000,"long dialogue scroll reaches and clamps to last choice "+width);scroll.SetValue(null,Vector2.zero);
  }
  // The isolated long-care fixture owns the shared card scroll; do not render a second real card into that state.
  longDialoguePreview=false;carePreview=true;var careHud=s.GetComponent<DriveHUD>();bool careHudEnabled=careHud.enabled;careHud.enabled=false;
  foreach(int width in new[]{1280,1920}){Screen.SetResolution(width,width*9/16,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.5f);yield return Capture("care-long-"+width);var field=typeof(CockpitUI).GetField("careScroll",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);field.SetValue(null,new Vector2(0,100000));yield return Capture("care-long-bottom-"+width);var position=(Vector2)field.GetValue(null);Check(position.y>0&&position.y<100000,"care instructions scroll to safe-stop footer "+width);field.SetValue(null,Vector2.zero);}
  carePreview=false;careHud.enabled=careHudEnabled;Time.timeScale=timeScale;
 }
 IEnumerator Ready(){s=null;while(!s||!s.StartupReady||s.business.IsRestoring){foreach(var item in FindObjectsOfType<DriveSession>())if(!item.IsSimulationBot)s=item;yield return null;}yield return null;}
 void Pose(MapStop stop){var t=s.taxi;t.body.velocity=t.body.angularVelocity=Vector3.zero;t.body.position=stop.Kerb+Vector3.up*.65f;t.body.rotation=Quaternion.LookRotation(stop.Direction);t.transform.SetPositionAndRotation(t.body.position,t.body.rotation);Physics.SyncTransforms();s.law.ResetObservation();s.manners.Suppress(2);}
 IEnumerator ReviewSingleMenus(){
  bool paused=s.paused;float timeScale=Time.timeScale;var hud=s.GetComponent<DriveHUD>();var practice=s.GetComponent<PracticeSession>();var settings=DrivingSettingsPanel.For(s);string preferenceKey=settings.PreferenceKey,tempKey="BrightUIReview."+Guid.NewGuid().ToString("N");
  int cash=s.business.ledger.Account.Cash,debt=s.business.ledger.Account.Debt,expense=s.business.ledger.Account.Expense(s.business.ledger.Day,null);double elapsed=s.business.ledger.Elapsed;
  try {
   foreach(int width in new[]{1280,1920}){
    Screen.SetResolution(width,width*9/16,FullScreenMode.Windowed);s.paused=true;Time.timeScale=0;yield return new WaitForSecondsRealtime(.5f);
    yield return Capture("02a-single-pause-"+width);
    settings.PreferenceKey=tempKey;settings.Open();Check(settings.IsOpen,"settings draft opens "+width);yield return Capture("02b-single-settings-"+width);settings.Cancel();settings.PreferenceKey=preferenceKey;Check(!settings.IsOpen&&!PlayerPrefs.HasKey(tempKey),"settings cancel writes no preferences "+width);
    // Help draws over the active cockpit; freeze only the clock for the capture.
    s.paused=false;hud.ToggleHelp();Check(hud.HelpOpen,"help opens "+width);yield return Capture("02c-single-help-"+width);hud.ToggleHelp();Check(!hud.HelpOpen,"help closes "+width);
    s.law.OpenHistory();Check(s.law.HistoryOpen,"history opens "+width);yield return Capture("02d-single-history-"+width);s.law.CloseHistory();s.paused=true;Time.timeScale=0;
    Check(practice.OpenPicker(),"practice picker opens without launching "+width);yield return Capture("02e-single-practice-picker-"+width);practice.CancelPicker();Check(!practice.PickerOpen&&!practice.Busy&&s.paused&&Time.timeScale==0,"practice cancel restores paused owner "+width);
    s.paused=false;Check(s.services.ToggleLifeHub(),"life hub opens "+width);yield return Capture("02f-single-life-hub-"+width);s.services.DismissPostTrip();Check(!s.services.PostTripVisible,"life hub dismisses "+width);
   }
   Check(s.business.ledger.Account.Cash==cash&&s.business.ledger.Account.Debt==debt&&s.business.ledger.Account.Expense(s.business.ledger.Day,null)==expense&&s.business.ledger.Elapsed==elapsed,"full menu browsing changes no payment or campaign time");
  } finally {
   settings.Cancel();settings.PreferenceKey=preferenceKey;if(hud.HelpOpen)hud.ToggleHelp();if(s.law.HistoryOpen)s.law.CloseHistory();practice.CancelPicker();s.services.DismissPostTrip();s.paused=paused;Time.timeScale=timeScale;
  }
  Check(s.paused==paused&&Time.timeScale==timeScale,"full menu review restores pause and clock");
 }
 IEnumerator ReviewSummaryMenus(){
  var b=s.business;bool paused=s.paused;float timeScale=Time.timeScale;int cash=b.ledger.Account.Cash,debt=b.ledger.Account.Debt,net=b.ledger.TotalNet;double elapsed=b.ledger.Elapsed;
  try {
   foreach(int width in new[]{1280,1920}){
    Screen.SetResolution(width,width*9/16,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.5f);
    if(b.RoomAvailable){b.RoomOpen=true;yield return Capture("07b-single-room-"+width);b.RoomOpen=false;}
    else Debug.Log("BRIGHT_UI_SKIP room unavailable for native campaign "+width);
    Check(b.RequestRestart(),"restart confirmation opens "+width);yield return Capture("07c-single-restart-"+width);b.ConfirmRestart(false);Check(!b.restartPrompt,"restart cancel retains campaign "+width);
   }
   Check(b.ledger.Account.Cash==cash&&b.ledger.Account.Debt==debt&&b.ledger.TotalNet==net&&b.ledger.Elapsed==elapsed&&b.ShowingSummary,"room/restart browsing preserves closed-day totals");
  } finally {b.RoomOpen=false;if(b.restartPrompt)b.ConfirmRestart(false);s.paused=paused;Time.timeScale=timeScale;}
  Check(s.paused==paused&&Time.timeScale==timeScale,"summary review restores pause and clock");
 }
 IEnumerator ReviewRepairAttention(string mode){
  var services=s.services;var damage=s.business.ledger.Care.Damage;var ledger=s.business.ledger;var map=s.GetComponent<FullMapOverlay>();
  int cash=ledger.Account.Cash,debt=ledger.Account.Debt,expense=ledger.Account.Expense(ledger.Day,null);float timeScale=Time.timeScale;var route=s.jobs.nav.hdRoute;
  Check(damage.Durability==100,"repair fixture starts undamaged "+mode);Time.timeScale=0;
  try {
   damage.ApplyAccidentLoss("ui-attention-51",49);Check(!services.RepairAttention,"no red dot at 51 "+mode);
   damage.ApplyAccidentLoss("ui-attention-50",1);Check(services.RepairAttention,"red dot starts at 50 "+mode);
   damage.ApplyAccidentLoss("ui-attention-21",29);Check(services.RepairAttention,"red dot at 21 "+mode);
   damage.ApplyAccidentLoss("ui-attention-0",21);Check(services.RepairAttention&&s.damage.Disabled,"zero retains attention and disabled handling "+mode);
   damage.Repair();Check(!services.RepairAttention,"completed repair clears red dot "+mode);damage.ApplyAccidentLoss("ui-attention-display",79);
   foreach(int width in new[]{1280,1920}){
    Screen.SetResolution(width,width*9/16,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.5f);
    Check(CockpitUI.LayoutFits(Screen.width,Screen.height),"attention HUD fits "+mode+width);yield return Capture("attention-"+mode+"-"+width);
    Check(services.ToggleLifeHub()&&services.PostTripVisible,"life hub opens "+mode+width);yield return Capture("life-hub-"+mode+"-"+width);
    Check(services.RepairAttention,"reading hub does not acknowledge repair "+mode+width);
    Check(services.OpenWorkshopFromHub()&&map.FacilityPlannerVisible&&!services.PostTripVisible,"workshop enters existing planner "+mode+width);Time.timeScale=0;yield return Capture("workshop-"+mode+"-"+width);map.Close();Time.timeScale=0;
    Check(services.RepairAttention&&s.jobs.nav.hdRoute==route,"browsing retains dot and selected route "+mode+width);
   }
   Check(ledger.Account.Cash==cash&&ledger.Account.Debt==debt&&ledger.Account.Expense(ledger.Day,null)==expense,"attention and browsing never purchase "+mode);
  } finally {if(map.IsOpen)map.Close();services.DismissPostTrip();damage.Repair();Time.timeScale=timeScale;}
  Check(!services.RepairAttention,"dot follows repaired state without reopen "+mode);
 }
 IEnumerator Run(){
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-fullUiMenuOnly")>=0){
   yield return new WaitForSecondsRealtime(2);
   foreach(int width in new[]{1280,1920}){Screen.SetResolution(width,width*9/16,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(1);Check(UnifiedModeBootstrap.Selected==UnifiedModeBootstrap.Mode.Menu&&!FindObjectOfType<DriveSession>(),"menu has no campaign session "+width);yield return Capture("mode-menu-"+width);}
   done=true;Debug.Log("FULL_UI_MENU_RESULT "+(ok?"PASS":"FAIL")+" checks="+checks);Application.Quit(ok?0:1);yield break;
  }
  Screen.SetResolution(1280,720,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(1);yield return Capture("01-mode-menu");
  FindObjectOfType<UnifiedModeBootstrap>().StartSingle();yield return Ready();
  Check(s.business.SavePath==UnifiedModeProbe.CampaignPath,"single isolated temporary campaign");
  yield return Capture("02-single-preparation");s.business.Depart();s.taxi.automated=true;s.taxi.autoBrake=true;if(Array.IndexOf(Environment.GetCommandLineArgs(),"-passengerActionReview")>=0){yield return ReviewPassengerActions();done=true;Debug.Log("PASSENGER_ACTION_PLAYER_RESULT "+(ok?"PASS":"FAIL")+" checks="+checks);Application.Quit(ok?0:2);yield break;}
  var map=s.GetComponent<FullMapOverlay>();
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-laneMovementReview")>=0){yield return ReviewLaneMovement();done=true;Debug.Log("LANE_MOVEMENT_PLAYER_RESULT "+(ok?"PASS":"FAIL")+" checks="+checks);Application.Quit(ok?0:2);yield break;}
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-roadviewSignalReview")>=0){yield return ReviewRoadview();done=true;Debug.Log("ROADVIEW_SIGNAL_PLAYER_RESULT "+(ok?"PASS":"FAIL")+" checks="+checks);Application.Quit(ok?0:2);yield break;}
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-signalCountdownOnly")>=0){
   foreach(var car in s.traffic.cars){car.Recycle();car.enabled=false;}
   var junction=s.traffic.junctions.Find(j=>j.signalized&&!j.scenarioSignal&&j.editorProgram==null);if(junction==null)throw new Exception("No timed signal fixture");
   var direction=s.traffic.AxisDirection(junction,0);var position=junction.center-direction*(junction.halfWidth[1]+22)+Vector3.up*.65f;
   s.taxi.body.velocity=s.taxi.body.angularVelocity=Vector3.zero;s.taxi.body.position=position;s.taxi.body.rotation=Quaternion.LookRotation(direction);s.taxi.transform.SetPositionAndRotation(position,s.taxi.body.rotation);Physics.SyncTransforms();s.law.ResetObservation();s.manners.Suppress(5);
   s.traffic.manualClock=true;s.traffic.manualTime=0;var readability=s.GetComponent<DrivingReadability>();readability.SelectSignal();Check(readability.SignalCountdownText.StartsWith("신호 변경까지 "),"native signal provides countdown text");
   foreach(int width in new[]{1280,1920}){Screen.SetResolution(width,width*9/16,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.5f);readability.SelectSignal();Check(readability.SignalCountdownText.EndsWith("초"),"countdown still available "+width);yield return Capture("signal-countdown-"+width);}
   done=true;Debug.Log("SIGNAL_COUNTDOWN_PLAYER_RESULT "+(ok?"PASS":"FAIL")+" checks="+checks);Application.Quit(ok?0:1);yield break;
  }
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-taxiArtReview")>=0){yield return ReviewTaxiArt();done=true;Debug.Log("TAXI_ART_PLAYER_RESULT "+(ok?"PASS":"FAIL")+" checks="+checks);Application.Quit(ok?0:2);yield break;}
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-lifeHubUiReview")>=0)yield return ReviewRepairAttention("single");
  if(FullUiReview)yield return ReviewSingleMenus();
  foreach(int width in new[]{1280,1920}){
   Screen.SetResolution(width,width*9/16,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.5f);
   Check(CockpitUI.LayoutFits(Screen.width,Screen.height),"HUD reference fits "+width);
   yield return Capture("03-single-hud-"+width);
   Check(map.Open(),"single map opens "+width);double elapsed=s.business.ledger.Elapsed;int first=Array.FindIndex(s.jobs.offers,o=>o!=null);if(first>=0)Check(map.SelectOffer(first),"single call preview selects without accepting");
   yield return Capture("04-single-map-"+width);Check(s.business.ledger.Elapsed==elapsed,"single map freezes timing "+width);
   int selected=Array.FindIndex(s.jobs.offers,o=>o!=null);Check(selected>=0,"forecast call available "+width);map.SelectOffer(selected);Check(map.SelectedOfferForecastText.Length>0,"selected forecast visible "+width);yield return Capture("04c-single-forecast-"+width);
   int expense=s.business.ledger.Account.Expense(s.business.ledger.Day,null);Check(map.ShowFacilityPlanner(0),"facility chooser opens "+width);yield return Capture("04d-single-facilities-"+width);Check(s.business.ledger.Account.Expense(s.business.ledger.Day,null)==expense&&s.business.ledger.Elapsed==elapsed,"facility browsing changes no payment/time "+width);map.Close();
  }
  if(FullUiReview)yield return ReviewSpeed();
  s.drivingCamera.SetFirstPerson(true);yield return Capture("04a-single-cockpit");s.drivingCamera.SetFirstPerson(false);
  var editor=s.GetComponent<RoadMapEditor>();editor.Open();yield return Capture("04b-editor-colors");editor.Close();
  // Normal native methods with controlled location, as in the existing receipt probe.
  foreach(var c in s.traffic.cars){c.Recycle();c.enabled=false;}
  var jobs=s.jobs;jobs.ClearForDay();jobs.ForcedPassengerType=PassengerType.General;jobs.ForcedRideCase=DrunkRideCase.Normal;var forecastRoute=jobs.nav.hd.Route(jobs.stops[0].laneEdge,jobs.stops[0].laneAt,jobs.stops[3].laneEdge,jobs.stops[3].laneAt);jobs.offers[0]=new JobOffer{candidateSeed="bright-ui-general",pickup=0,drop=3,fare=5000,seconds=2000,requiredScore=70,distance=jobs.nav.Distance(jobs.stops[0],jobs.stops[3]),pickupSeconds=600,timingPolicyVersion=1,safeRouteSeconds=OperationForecastAdapter.RouteSeconds(s,forecastRoute)};
  Pose(jobs.stops[0]);Check(jobs.Accept(0),"fixture native call acceptance");
  Check(CockpitPresentation.Destination("테스트",jobs.trip.State).Contains("승차 장소"),"pickup destination label identifies boarding stop");
  string passengerIdentity=PassengerDialogueUI.PortraitIdentity(s);int passengerPortrait=PassengerDialogueUI.PortraitIndex(passengerIdentity,null);
  yield return Capture("05c-single-pickup-ready");Check(jobs.Interact(),"fixture native boarding");
  Check(CockpitPresentation.Destination("테스트",jobs.trip.State).Contains("도착지"),"onboard destination label identifies dropoff");
  Check(PassengerDialogueUI.PortraitIdentity(s)==passengerIdentity,"boarding preserves portrait identity");
  var selectedRoute=jobs.nav.hdRoute;var cue=s.GetComponent<DrivingReadability>().TurnCue;Check(cue.Valid&&cue.Distance>=0,"single turn card reads live directed route");Check(jobs.nav.hdRoute==selectedRoute,"turn card never replaces selected route");yield return Capture("05a-single-navigation");
  var originalLaw=s.law.ledger;var notifications=s.GetComponent<DriveNotifications>();s.law.ledger=new LawLedger();notifications.RefreshScores();s.law.ledger.RecordIncident(LawEvaluator.StopLine("ui-notice",1,new RoadObservation{known=true,forward=SignalColor.Red}));notifications.RefreshScores();Check(notifications.VisibleTexts.Length>0,"actual score change notice visible");
  foreach(var size in new[]{new Vector2(1280,720),new Vector2(1600,900),new Vector2(1920,1080)})foreach(bool cockpit in new[]{false,true})Check(!DriveNotifications.NoticeBounds(size.x,size.y,cockpit).Overlaps(DrivingReadability.NavigationBounds(size.x,size.y,cockpit)),"notice clears navigation "+size+" cockpit="+cockpit);
  yield return Capture("05f-score-notice-below-navigation");s.law.ledger=originalLaw;notifications.RefreshScores();
  foreach(var resolution in new[]{new Vector2(1280,720),new Vector2(1600,900),new Vector2(1920,1080)}){var nr=DrivingReadability.NavigationBounds(resolution.x,resolution.y,false);float hs=UiReadability.HudScale(resolution.x,resolution.y);Check(Mathf.Abs(nr.center.x-resolution.x/2)<.1f&&Mathf.Abs(nr.width-527*hs)<.1f&&nr.height<140*hs,"compact centered navigation "+resolution);}
  string passengerName=PassengerPresentation.Name(passengerIdentity);var beforePassenger=jobs.CaptureLive();jobs.RestoreLive(beforePassenger);Check(passengerName==PassengerPresentation.Name(PassengerDialogueUI.PortraitIdentity(s)),"passenger name stable after actual save snapshot restore");Check(PassengerPresentation.Speech("손님  “반갑습니다.”")=="반갑습니다.","customer speaker label and outer quotes removed");yield return Capture("05d-single-named-passenger");s.drivingCamera.SetFirstPerson(true);yield return Capture("05e-single-named-first-person");s.drivingCamera.SetFirstPerson(false);
  Check(map.Open(),"active single map opens");yield return Capture("05b-single-active-map");map.Close();
  s.requests.BeginKind(RequestKind.Comfort);s.requests.AdvanceSafeStop(3);Check(!s.business.RequestEndDay()&&!string.IsNullOrEmpty(s.business.notice),"end shift refusal has visible feedback");yield return Capture("05-single-passenger-request");
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-hudDialogueReview")>=0)yield return ReviewDialogue();
  Check(PassengerDialogueUI.PortraitIdentity(s)==passengerIdentity,"request preserves portrait identity");s.requests.request.DeferSafety();
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-hudDialogueReview")>=0)yield return ReviewLongDialogue();
  Pose(jobs.stops[jobs.current.drop]);Check(jobs.Interact(),"fixture native arrival and one settlement");
  s.services.ObserveCompletedTrip();Check(!s.services.PostTripVisible,"completion never automatically opens life hub");var receipt=s.GetComponent<RideCompletionPanel>();if(!receipt.Open)receipt.ShowLast();int income=s.business.ledger.Income;
  Check(receipt.Open&&receipt.Receipt.Contains("실수령"),"real receipt fields available");Check(receipt.PortraitIndex==passengerPortrait,"settled receipt preserves passenger portrait");yield return Capture("06-single-receipt");
  int facilitiesCash=s.business.ledger.Account.Cash;var facilitiesRoute=jobs.nav.hdRoute;
  Check(receipt.TryOpenFacilities()&&s.services.PostTripVisible&&!receipt.Open,"receipt opens life hub");yield return Capture("06a-receipt-lifehub");
  Check(receipt.ShowLast()&&receipt.TryOpenFacilities()&&s.services.PostTripVisible,"receipt keeps existing life hub open");
  s.services.DismissPostTrip();Check(receipt.ShowLast()&&receipt.TryOpenFacilities()&&s.services.PostTripVisible,"receipt opens closed life hub");
  Check(s.business.ledger.Account.Cash==facilitiesCash&&jobs.nav.hdRoute==facilitiesRoute,"facility browsing changes neither cash nor route");s.services.DismissPostTrip();
  Check(receipt.ShowLast(),"receipt can reopen without payment");
  s.business.ledger.Care.Damage.ApplyAccidentLoss("receipt-disabled-fixture",100);
  Check(!receipt.CanOpenFacilities&&!receipt.TryOpenFacilities()&&receipt.Open,"disabled vehicle preserves receipt and repair flow");s.business.ledger.Care.Damage.Repair();receipt.Close();Check(s.business.ledger.Income==income,"closing receipt does not pay twice");
  s.business.ledger.RequestClose("UI screenshot fixture");yield return new WaitForSecondsRealtime(.5f);yield return Capture("07-single-day-summary");
  int cash=s.business.ledger.Account.Cash;int net=s.business.ledger.TotalNet;
  Check(!OneDayHUD.DayDetailsOpen,"day summary starts compact");OneDayHUD.ToggleDayDetails();yield return Capture("07a-single-day-details");
  Check(OneDayHUD.DayDetailsOpen&&s.business.ledger.Account.Cash==cash&&s.business.ledger.TotalNet==net,"expanding full day details changes no transactions");OneDayHUD.ToggleDayDetails();
  if(FullUiReview)yield return ReviewSummaryMenus();
  FindObjectOfType<UnifiedModeBootstrap>().ReturnToMenu();yield return new WaitForSecondsRealtime(1);
  FindObjectOfType<UnifiedModeBootstrap>().StartPvp();yield return new WaitForSecondsRealtime(1);yield return Capture("08-pvp-setup");
  if(FullUiReview)foreach(int width in new[]{1280,1920}){Screen.SetResolution(width,width*9/16,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.5f);yield return Capture("08a-pvp-setup-"+width);var setup=FindObjectOfType<Pvp.PvpPrototypePanel>();setup.ConditionSettingsOpen=true;yield return Capture("08b-pvp-settings-"+width);setup.ConditionSettingsOpen=false;}
  FindObjectOfType<Pvp.PvpBootstrap>().StartSimulation(true,180);yield return Ready();
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-lifeHubUiReview")>=0)yield return ReviewRepairAttention("pvp");
  Check(s.simulationMatch&&s.simulationMatch.Drivers.Count==4,"PVP four native sessions");Check(s.business.SavePath=="","PVP has no campaign save");
  map=s.GetComponent<FullMapOverlay>();
  foreach(int width in new[]{1280,1920}){
   Screen.SetResolution(width,width*9/16,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.5f);yield return Capture("09-pvp-hud-"+width);
   Check(map.Open(),"PVP map opens "+width);float elapsed=s.simulationMatch.Elapsed;int first=Array.FindIndex(s.jobs.offers,o=>o!=null);if(first>=0)Check(map.SelectOffer(first),"PVP call preview selects without accepting");yield return Capture("10-pvp-map-"+width);Check(s.simulationMatch.Elapsed>elapsed&&!s.paused,"PVP map keeps match running "+width);Check(map.ShowFacilityPlanner(1),"PVP meal chooser opens "+width);yield return Capture("10b-pvp-facilities-"+width);map.Close();
  }
  int available=Array.FindIndex(s.jobs.offers,o=>o!=null);s.jobs.ForcedPassengerType=PassengerType.General;s.jobs.ForcedRideCase=DrunkRideCase.Normal;Check(available>=0&&s.jobs.Accept(available),"PVP human accepts real shared call for navigation");yield return new WaitForSecondsRealtime(.2f);Check(s.GetComponent<DrivingReadability>().TurnCue.Valid,"PVP turn card reads live pickup route");yield return Capture("10a-pvp-navigation");
  Check(map.Open(),"active PVP map opens");yield return Capture("10b-pvp-active-map");map.Close();
  if(FullUiReview){longDialoguePreview=true;yield return Capture("10c-pvp-dialogue-standings");longDialoguePreview=false;}
  Pose(s.jobs.stops[s.jobs.current.pickup]);Check(s.jobs.Interact(),"PVP named fixture boards actual passenger");Check(PassengerPresentation.Name(PassengerDialogueUI.PortraitIdentity(s)).Length==3,"PVP passenger named");s.drivingCamera.SetFirstPerson(true);yield return Capture("10d-pvp-named-first-person");s.drivingCamera.SetFirstPerson(false);
  s.simulationMatch.Finish();yield return Capture("11-pvp-results");Check(Time.timeScale==0,"PVP final result pauses match");
  done=true;Debug.Log("BRIGHT_UI_RESULT "+(ok?"PASS":"FAIL")+" checks="+checks);Application.Quit(ok?0:1);
 }
}
