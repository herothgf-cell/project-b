using UnityEngine;
// Shared 1600x900 reference layout; small windows scale rather than overlap controls.
public static class CockpitUI {
 public static float Scale {get{return UiReadability.HudScale(Screen.width,Screen.height);}}
 public static bool LayoutFits(float w,float h){float s=UiReadability.HudScale(w,h);return w/s>=1599.99f&&h/s>=899.99f;}
 static GUIStyle text,small,large,title;
 static void Styles(){if(text!=null)return;text=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=18,wordWrap=true};small=new GUIStyle(text){fontSize=14};large=new GUIStyle(text){font=KoreanText.BoldFont,fontSize=64,fontStyle=FontStyle.Bold};title=new GUIStyle(text){fontSize=24,fontStyle=FontStyle.Bold};text.normal.textColor=small.normal.textColor=large.normal.textColor=title.normal.textColor=CasualUI.Ink;}
 public static Rect JobBounds(float w,float h,bool active){float s=UiReadability.HudScale(w,h);return new Rect(24*s,334*s,320*s,(active?168:50)*s);}
 // TIP uses the left column between the current job and the speedometer.
 public static Rect SituationBounds(float w,float h,bool result=false,bool activeJob=true){float s=UiReadability.HudScale(w,h);float top=JobBounds(w,h,activeJob).yMax+12*s;return new Rect(24*s,top,320*s,Mathf.Min((result?118:220)*s,h-276*s-top));}
 public static Rect ServiceHintBounds(float w,float h){float s=UiReadability.HudScale(w,h);return new Rect(365*s,200*s,410*s,110*s);}
 public static Rect SlidingTipBounds(float w,float h,bool result,bool active,float visible){var r=SituationBounds(w,h,result,active);float s=UiReadability.HudScale(w,h);r.x=Mathf.Lerp(-r.width,r.x,Mathf.Clamp01(visible));return r;}
 public static string Clock(double seconds){int n=Mathf.CeilToInt((float)System.Math.Max(0,seconds));return n/60+":"+(n%60).ToString("00");}
 static void Panel(Rect r){CasualUI.Panel(r);}
 static void Gauge(Rect r,int score,Color c){CasualUI.Bar(r,score/100f,c);}
 public static void Draw(DriveHUD hud){Styles();text.fontSize=UiReadability.Font(18);small.fontSize=UiReadability.Font(14);title.fontSize=UiReadability.Font(24);var s=hud.session;var e=s.business.ledger;var jobs=s.jobs;var previous=GUI.matrix;float scale=Scale;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));float w=Screen.width/scale,h=Screen.height/scale;
  Panel(new Rect(24,20,875,86));GUI.Label(new Rect(42,35,250,40),(s.simulationMatch?"PVP 도시":MapCatalog.Name(s.world.map.mapId)),new GUIStyle(title){font=KoreanText.Font,fontStyle=FontStyle.Normal});
  var match=s.simulationMatch;
  int minute=480+(int)(e.Elapsed*720/e.Settings.daySeconds);
  if(match){GUI.Label(new Rect(302,33,190,28),"남은 경기 "+Clock(match.Remaining),text);GUI.Label(new Rect(302,64,190,25),(match.ServiceRemaining(s)>0?"시설 이용 "+Clock(match.ServiceRemaining(s)):PvpNativeLan.RunningOnline?"LAN · "+(PvpNativeLan.Instance.IsHost?"호스트":"참가자"):"나 + 봇 3대 · 오프라인"),small);}else {GUI.Label(new Rect(302,33,190,28),(minute/60%24)+":"+(minute%60).ToString("00")+" · "+e.Day+" / "+e.Settings.days+"일",text);GUI.Label(new Rect(302,64,190,25),e.State==BusinessState.Closing?"퇴근 대기":e.Elapsed>=e.Settings.daySeconds?"초과 영업 +"+Clock(e.Elapsed-e.Settings.daySeconds):"남은 "+Clock(e.SecondsLeft),small);}
  if(GUI.Button(new Rect(500,40,76,42),"기록",KoreanText.Button(UiReadability.Font(15))))s.law.OpenHistory();
  GUI.enabled=s.services.CanOpenLifeHub;if(GUI.Button(new Rect(584,40,104,42),"생활거점",KoreanText.Button(UiReadability.Font(15))))s.services.ToggleLifeHub();GUI.enabled=true;
  GUI.enabled=!match&&!s.paused&&e.State==BusinessState.Open&&jobs.trip.State!=TripState.OnTrip;if(GUI.Button(new Rect(696,40,76,42),match?"경기 중":"퇴근",KoreanText.Button(UiReadability.Font(15))))s.business.RequestEndDay();GUI.enabled=true;
  if(GUI.Button(new Rect(780,40,100,42),"도움말",KoreanText.Button(UiReadability.Font(15))))hud.ToggleHelp();
  if(match){Panel(new Rect(w-560,20,220,264));GUI.Label(new Rect(w-544,28,194,26),"수입 − 시설비",small);for(int i=0;i<match.Drivers.Count;i++){float row=58+i*52;var rowStyle=new GUIStyle(text){wordWrap=false,normal={textColor=PvpDriverPresentation.Color(i)}};string name=PvpDriverPresentation.Name(i)+"  "+match.Score(i).ToString("N0")+"원";GUI.Label(new Rect(w-544,row,194,28),new GUIContent(UiReadability.Ellipsis(name,rowStyle,194),name),rowStyle);GUI.Label(new Rect(w-544,row+27,194,25),PvpDriverPresentation.Status(match,i),new GUIStyle(small){wordWrap=false});}}else {Panel(new Rect(w-620,20,280,112));HudGlyph.Draw(new Rect(w-604,36,24,24),4,CasualUI.Amber);GUI.Label(new Rect(w-570,34,210,23),"잔액",small);GUI.Label(new Rect(w-604,60,254,38),(e.Account==null?jobs.balance:e.Account.Cash).ToString("N0")+"원",title);HudGlyph.Draw(new Rect(w-604,103,18,18),5,CasualUI.Green);GUI.Label(new Rect(w-578,100,222,24),"누적 순수익 "+e.TotalNet.ToString("+ #,0;- #,0;0"),small);}
  float y=h-258;Panel(new Rect(24,y,420,234));CockpitPresentation.DrawDriveState(new Rect(42,y+10,382,30),s);GUI.Label(new Rect(40,y+28,230,95),Mathf.Abs(s.taxi.Speed*3.6f).ToString("0"),large);GUI.Label(new Rect(258,y+83,72,28),"km/h",small);DriverStatusGauges.Draw(new Rect(40,y+112,388,112),e.Driver,e.Account,e.Care.Cleanliness,e.Care.Damage.Durability);
  Panel(new Rect(w-312,h-186,288,162));bool occupied=jobs.HasPassengerScore;var satisfaction=jobs.ScoreView;
  HudGlyph.Draw(new Rect(w-296,h-171,28,28),6,CasualUI.Green);GUI.Label(new Rect(w-256,h-175,216,30),occupied?"만족도 "+satisfaction.value+" · 요구 "+satisfaction.required:"승객 없음",small);Gauge(new Rect(w-256,h-142,216,8),occupied?satisfaction.value:0,CasualUI.Green);
  if(occupied){float threshold=Mathf.Clamp01(satisfaction.required/100f);GUI.color=CasualUI.Ink;GUI.DrawTexture(new Rect(w-256+216*threshold-1,h-145,2,14),Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(new Rect(w-256,h-133,216,20),"승차 "+satisfaction.start+" · 청결 +"+satisfaction.clean,new GUIStyle(small){fontSize=UiReadability.Font(12)});}
  HudGlyph.Draw(new Rect(w-296,h-98,28,28),7,CasualUI.Blue);GUI.Label(new Rect(w-256,h-103,216,30),"운전 점수  "+s.law.ledger.TotalScore,text);Gauge(new Rect(w-256,h-61,216,8),s.law.ledger.TotalScore,CasualUI.Blue);
  var readability=s.GetComponent<DrivingReadability>();float jobY=readability&&readability.SignalTarget!=null?334:278;Panel(new Rect(24,jobY,320,jobs.Active?168:50));
  string taxiStatus=CockpitPresentation.TaxiStatus(jobs.trip.State);
  if(jobs.Active){bool pickup=jobs.trip.State==TripState.ToPickup;
   string destination=CockpitPresentation.Destination(KoreanText.Stop(jobs.stops[pickup?jobs.current.pickup:jobs.current.drop].name));
   var destinationStyle=new GUIStyle(text){fontSize=UiReadability.Font(16),wordWrap=true};float titleWidth=taxiStatus.Length>0?222:292;
   GUI.Label(new Rect(38,jobY+8,titleWidth,48),new GUIContent(destination,destination),destinationStyle);
   if(taxiStatus.Length>0){CasualUI.RoundedFill(new Rect(268,jobY+12,60,28),CasualUI.Soft);GUI.Label(new Rect(268,jobY+12,60,28),taxiStatus,new GUIStyle(small){alignment=TextAnchor.MiddleCenter});}
   CockpitPresentation.DrawDeadline(new Rect(38,jobY+62,292,40),pickup?jobs.trip.PickupRemaining:jobs.trip.Remaining,text);
   int fare=pickup?PassengerReward.AdjustedFare(jobs.current.fare,s.law.ledger.TotalScore):jobs.DrivingFareCaptured?PassengerReward.AdjustedFare(jobs.current.fare,jobs.BoardingDrivingScore):jobs.current.fare;
   GUI.Label(new Rect(38,jobY+107,292,28),"예상 운임 비용",small);GUI.Label(new Rect(38,jobY+133,292,29),fare.ToString("N0")+"원",text);
  }else {GUI.Label(new Rect(38,jobY+12,64,30),taxiStatus,new GUIStyle(text){font=KoreanText.BoldFont});if(GUI.Button(new Rect(105,jobY+9,225,32),"호출 보기 (M)",KoreanText.Button(UiReadability.Font(16)))){var map=s.GetComponent<FullMapOverlay>();if(map.Open())map.ShowCalls();}}

  // Separate care speech from the request choices (which occupy at most the bottom 254px).
  string careDialogue=jobs.VisibleCareDialogue;if(careDialogue.Length>0){var careArea=new Rect((w-540)/2,h-350,540,72);Panel(careArea);GUI.Label(new Rect(careArea.x+12,careArea.y+8,516,60),careDialogue,text);}
  GUI.matrix=previous;
 }
}

