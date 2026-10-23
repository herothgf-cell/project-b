using UnityEngine;
// Shared 1600x900 reference layout; small windows scale rather than overlap controls.
public static class CockpitUI {
 public static float Scale {get{return Mathf.Min(1,Screen.width/1600f,Screen.height/900f);}}
 public static bool LayoutFits(float w,float h){float s=Mathf.Min(1,w/1600,h/900);return w/s>=1599.99f&&h/s>=899.99f;}
 static GUIStyle text,small,large,title;
 static void Styles(){if(text!=null)return;text=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=18,wordWrap=true};small=new GUIStyle(text){fontSize=13};large=new GUIStyle(text){fontSize=76,fontStyle=FontStyle.Bold};title=new GUIStyle(text){fontSize=24,fontStyle=FontStyle.Bold};text.normal.textColor=small.normal.textColor=large.normal.textColor=title.normal.textColor=Color.white;}
 public static string Clock(double seconds){int n=Mathf.CeilToInt((float)System.Math.Max(0,seconds));return n/60+":"+(n%60).ToString("00");}
 static void Panel(Rect r){GUI.color=new Color(.045f,.10f,.14f,.95f);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
 static void Gauge(Rect r,int score,Color c){GUI.color=new Color(.2f,.29f,.32f);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=c;GUI.DrawTexture(new Rect(r.x,r.y,r.width*Mathf.Clamp01(score/100f),r.height),Texture2D.whiteTexture);GUI.color=Color.white;}
 public static void Draw(DriveHUD hud){Styles();var s=hud.session;var e=s.business.ledger;var jobs=s.jobs;var previous=GUI.matrix;float scale=Scale;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));float w=Screen.width/scale,h=Screen.height/scale;
  Panel(new Rect(24,20,790,86));GUI.Label(new Rect(42,35,250,40),MapCatalog.Name(s.world.map.mapId),title);
  int minute=480+(int)(System.Math.Min(e.Settings.daySeconds,e.Elapsed)*720/e.Settings.daySeconds);
  GUI.Label(new Rect(302,33,190,28),minute/60+":"+(minute%60).ToString("00")+" · "+e.Day+" / "+e.Settings.days+"일",text);GUI.Label(new Rect(302,64,190,25),e.State==BusinessState.Closing?"퇴근 대기":"남은 "+Clock(e.SecondsLeft),small);
  if(GUI.Button(new Rect(510,40,84,42),"정보",KoreanText.Button(15)))s.law.OpenHistory();
  GUI.enabled=!s.paused&&e.State==BusinessState.Open&&jobs.trip.State!=TripState.OnTrip;if(GUI.Button(new Rect(606,40,84,42),"퇴근",KoreanText.Button(15)))s.business.RequestEndDay();GUI.enabled=true;
  if(GUI.Button(new Rect(702,40,94,42),"도움말",KoreanText.Button(15)))hud.ToggleHelp();
  Panel(new Rect(w-620,20,280,112));HudGlyph.Draw(new Rect(w-604,36,24,24),4,Color.white);GUI.Label(new Rect(w-570,34,210,23),"현재 잔액",small);GUI.Label(new Rect(w-604,60,254,38),(e.Account==null?jobs.balance:e.Account.Cash).ToString("N0")+"원",title);HudGlyph.Draw(new Rect(w-604,103,18,18),5,new Color(.4f,.9f,.7f));GUI.Label(new Rect(w-578,100,222,24),"누적 순수익 "+e.TotalNet.ToString("+ #,0;- #,0;0"),small);
  float y=h-220;Panel(new Rect(24,y,320,196));GUI.Label(new Rect(42,y+10,280,25),(s.taxi.Speed<-.2f?"R":"D")+" · "+(s.assisted.Active?"조향 보조":"수동 운전")+" · "+(s.signals.Hazards?"비상등":s.signals.direction<0?"좌측 점멸":s.signals.direction>0?"우측 점멸":""),small);GUI.Label(new Rect(40,y+25,230,95),Mathf.Abs(s.taxi.Speed*3.6f).ToString("000"),large);GUI.Label(new Rect(258,y+83,72,28),"km/h",small);DriverStatusGauges.Draw(new Rect(40,y+132,288,52),e.Driver,e.Account);
  Panel(new Rect(w-312,h-166,288,142));bool occupied=jobs.trip.State==TripState.OnTrip;
  HudGlyph.Draw(new Rect(w-296,h-151,28,28),6,new Color(.3f,.85f,.6f));GUI.Label(new Rect(w-256,h-155,216,25),"승객 만족도  "+(occupied?s.manners.ledger.Score.ToString():"대기"),text);Gauge(new Rect(w-256,h-122,216,8),occupied?s.manners.ledger.Score:0,new Color(.3f,.85f,.6f));
  HudGlyph.Draw(new Rect(w-296,h-98,28,28),7,new Color(.4f,.7f,1));GUI.Label(new Rect(w-256,h-103,216,30),"운행 점수  "+s.law.ledger.TotalScore,text);Gauge(new Rect(w-256,h-61,216,8),s.law.ledger.TotalScore,new Color(.4f,.7f,1));
  Panel(new Rect(24,405,320,jobs.Active?100:50));if(jobs.Active){bool pickup=jobs.trip.State==TripState.ToPickup;GUI.Label(new Rect(38,414,292,44),(pickup?"승차":"하차")+" · "+KoreanText.Stop(jobs.stops[pickup?jobs.current.pickup:jobs.current.drop].name),text);GUI.Label(new Rect(38,463,292,36),(pickup?"대기 "+Clock(jobs.trip.PickupRemaining):"남은 "+Clock(jobs.trip.Remaining))+" · "+jobs.current.fare.ToString("N0")+"원",small);}else if(GUI.Button(new Rect(38,414,292,32),"M 주변 호출 보기",KoreanText.Button(16)))s.GetComponent<FullMapOverlay>().Open();

  GUI.matrix=previous;
 }
}
