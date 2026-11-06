using UnityEngine;
public class DriveHUD : MonoBehaviour {
 public DriveSession session;public Texture2D tile;public bool HelpOpen {get;private set;}GUIStyle title,body,small;const int W=1024,H=1024;
 public void ToggleHelp(){HelpOpen=!HelpOpen;}
 Texture2D lightTile;public Texture2D LightTile {get{if(!lightTile&&session&&session.world&&session.world.map!=null)lightTile=MapPresentation.Create(session.world.map,2048);return lightTile;}}
 void OnDestroy(){if(tile)Destroy(tile);if(lightTile)Destroy(lightTile);}
 void Start(){tile=new Texture2D(W,H);var pixels=new Color[W*H];for(int i=0;i<pixels.Length;i++)pixels[i]=new Color(.10f,.15f,.17f);tile.SetPixels(pixels);
  if(session.world.map.areas!=null)foreach(var area in session.world.map.areas){
   if(area.kind!="water"||area.points==null||area.points.Length<3)continue;
   float minX=float.MaxValue,maxX=0,minZ=float.MaxValue,maxZ=0;foreach(var p in area.points){minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);minZ=Mathf.Min(minZ,p.z);maxZ=Mathf.Max(maxZ,p.z);}
   for(int x=Mathf.Max(0,Mathf.FloorToInt(minX/session.world.map.width*W));x<Mathf.Min(W,Mathf.CeilToInt(maxX/session.world.map.width*W));x++)
   for(int y=Mathf.Max(0,Mathf.FloorToInt(minZ/session.world.map.depth*H));y<Mathf.Min(H,Mathf.CeilToInt(maxZ/session.world.map.depth*H));y++){
    float px=(x+.5f)/W*session.world.map.width,pz=(y+.5f)/H*session.world.map.depth;bool inside=false;
    for(int i=0,j=area.points.Length-1;i<area.points.Length;j=i++){var a=area.points[i];var b=area.points[j];if((a.z>pz)!=(b.z>pz)&&px<(b.x-a.x)*(pz-a.z)/(b.z-a.z)+a.x)inside=!inside;}
    if(inside)tile.SetPixel(x,y,new Color(.12f,.37f,.51f));
   }
  }
  foreach(var road in session.world.map.roads)for(int i=1;i<road.points.Length;i++){var a=road.points[i-1].V();var b=road.points[i].V();float len=Vector3.Distance(a,b);for(float d=0;d<=len;d+=1){var p=Vector3.Lerp(a,b,len>0?d/len:0);int x=Mathf.RoundToInt(p.x/session.world.map.width*(W-1)),y=Mathf.RoundToInt(p.z/session.world.map.depth*(H-1));for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)if(x+dx>=0&&x+dx<W&&y+dy>=0&&y+dy<H)tile.SetPixel(x+dx,y+dy,new Color(.43f,.49f,.50f));}}
  tile.Apply();
 }
 public string OfferText(int index){var jobs=session.jobs;var o=jobs.offers[index];if(o==null)return "현재 접근 가능한 호출 없음";return (index+1)+"  "+KoreanText.Stop(jobs.stops[o.pickup].name)+" → "+KoreanText.Stop(jobs.stops[o.drop].name)+"    "+o.distance.ToString("0")+"m · "+jobs.FareAgreementText(o.fare,true)+" · "+o.seconds.ToString("0")+"초 · 요구 "+o.requiredScore+"점\n"+jobs.FuelHint(o);}
 public string MannerCaption {get{return session.jobs.HasPassengerScore?session.jobs.ScoreView.Caption:session.jobs.trip.State==TripState.ToPickup?"승객 만족도 · 탑승 전 / 요구 "+session.jobs.current.requiredScore+"점":"승객 만족도 · 탑승 후 표시";}}
 public string LawCaption {get{int valid=0;foreach(var e in session.law.ledger.Events)foreach(var r in e.rules)if(r.verdict==LawVerdict.Compliant||r.verdict==LawVerdict.Violation)valid++;return "운전 점수 "+session.law.ledger.TotalScore+"점 · "+(valid==0?"아직 유효 평가 없음":"유효 "+valid+"건")+" · F8";}}
 void Gauge(Rect r,float value,Color color,int requirement=-1){GUI.color=new Color(.13f,.18f,.21f);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=color;GUI.DrawTexture(new Rect(r.x,r.y,r.width*Mathf.Clamp01(value/100),r.height),Texture2D.whiteTexture);if(requirement>=0){GUI.color=Color.white;GUI.DrawTexture(new Rect(r.x+r.width*Mathf.Clamp01(requirement/100f)-1,r.y-2,2,r.height+4),Texture2D.whiteTexture);}GUI.color=Color.white;}
 void OnGUI(){if(PracticeSession.Enabled&&!session.paused)return;if(session.FullMapOpen||session.RideResultOpen||session.ChoiceOpen||session.PracticeMenuOpen||session.business.restartPrompt||session.jobs==null||session.business.ShowingSummary||session.business.preparing||session.business.taxPrompt||session.services.open||session.law.HistoryOpen)return;if(title==null){GUI.skin.font=KoreanText.Font;title=new GUIStyle(GUI.skin.label){fontSize=23,fontStyle=FontStyle.Bold};body=new GUIStyle(GUI.skin.label){fontSize=16};small=new GUIStyle(GUI.skin.label){fontSize=13};title.normal.textColor=body.normal.textColor=small.normal.textColor=CasualUI.Ink;}
  body.fontSize=UiReadability.Font(16);small.fontSize=UiReadability.Font(13);CockpitUI.Draw(this);
  if(HelpOpen)DrawHelp();
  if(session.paused){
   if(session.simulationMatch&&session.simulationMatch.Finished||PvpNativeLan.RunningOnline)return;
   var settings=DrivingSettingsPanel.For(session);if(settings.IsOpen){settings.Draw();return;}
   if(session.simulationMatch){
    if(session.simulationMatch.Finished)return;
    float px=Screen.width/2f-240,py=Screen.height/2f-130;
    GUI.depth=-150;GUI.Box(new Rect(px,py,480,260),"일시정지 · 경기와 차량이 멈췄습니다");
    if(GUI.Button(new Rect(px+20,py+55,440,45),"계속하기")){session.paused=false;Time.timeScale=1;}
    if(GUI.Button(new Rect(px+20,py+115,440,45),"설정",KoreanText.Button(18)))settings.Open();
    if(GUI.Button(new Rect(px+20,py+175,440,45),"경기 나가기"))session.simulationMatch.ReturnToMenu();
    return;
   }
   DrawSingleMenu();
  }
 }
 public void DrawSingleMenu(){
  var settings=DrivingSettingsPanel.For(session);if(settings.IsOpen){settings.Draw();return;}
  var b=session.business;bool restart=b.useV2&&b.HasCampaignToContinue;
  float height=restart?360:308,x=Screen.width/2f-240,y=(Screen.height-height)/2;
  GUI.depth=-150;GUI.Box(new Rect(x,y,480,height),b.preparing?"영업 준비":"일시정지");
  bool enabled=GUI.enabled;GUI.enabled=enabled&&!session.PracticeBusy&&!b.IsRestoring;
  if(GUI.Button(new Rect(x+20,y+52,440,44),b.MenuPrimaryLabel,KoreanText.Button(18)))b.ContinueFromMenu();
  float row=y+104;
  if(restart){if(GUI.Button(new Rect(x+20,row,440,40),"처음부터 시작",KoreanText.Button(16)))b.RequestRestart();row+=52;}
  if(GUI.Button(new Rect(x+20,row,440,40),"운전 연습 선택",KoreanText.Button(16)))session.GetComponent<PracticeSession>().OpenPicker();row+=52;
  GUI.enabled=enabled;
  if(GUI.Button(new Rect(x+20,row,440,40),"설정",KoreanText.Button(16)))settings.Open();row+=52;
  if(GUI.Button(new Rect(x+20,row,440,40),"게임 종료",KoreanText.Button(16)))Application.Quit();row+=52;
  if(GUI.Button(new Rect(x+20,row,440,28),session.traffic.mitOptions.Label+" · 클릭하여 변경",KoreanText.Button(14)))session.traffic.mitOptions.SetEnabled(!session.traffic.mitOptions.Enabled);
 }
 public string DrivingHelpText {get{return "수동: W/↑ 가속 · S/↓ 감속 · A/D 또는 ←/→ 조향\n멈춘 뒤 S를 놓았다 다시 누르면 후진 · Space 긴급제동\nQ/E 방향지시등 · G 비상등 · H 조향 보조 켜기/끄기\n보조: ←/→ 차로 변경 · Q/E 다음 교차로 회전 예약\n보조 중에도 가속·감속·제동과 신호 판단은 직접 합니다.\nC 시점 · B 뒤보기 · Z/X 좌우 보기 · V 코너 시선\nF 승하차 · M 전체 지도 · Tab 경로 변경 · +/- 미니맵 확대/축소\nF7 거점 · F8 기록 · F10 안내 · Esc 정지 · F11 전체화면";}}
 public static Rect HelpBounds(float width,float height){float scale=UiReadability.HudScale(width,height);return new Rect(width/2-360*scale,height/2-195*scale,720*scale,390*scale);}
 void DrawHelp(){
  int depth=GUI.depth;GUI.depth=-20;var matrix=GUI.matrix;float scale=CockpitUI.Scale;
  GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));var r=HelpBounds(Screen.width,Screen.height);r=new Rect(r.x/scale,r.y/scale,r.width/scale,r.height/scale);
  KoreanText.Panel(r);
  GUI.Label(new Rect(r.x+18,r.y+12,584,32),session.assisted.Active?"조향 보조 · H로 수동 전환":"수동 운전 · H로 조향 보조",body);
  if(GUI.Button(new Rect(r.x+618,r.y+10,84,36),"닫기",KoreanText.Button(14)))HelpOpen=false;
  GUI.Label(new Rect(r.x+18,r.y+52,684,54),session.assisted.Status,new GUIStyle(small){wordWrap=true});
  GUI.Label(new Rect(r.x+18,r.y+112,684,254),DrivingHelpText,new GUIStyle(body){wordWrap=true});
  GUI.Label(new Rect(r.x+18,r.y+366,464,23),"지도 자료: © OpenStreetMap contributors · ODbL",new GUIStyle(small){fontSize=12});
  if(GUI.Button(new Rect(r.x+492,r.y+365,210,24),"지도 출처·라이선스",KoreanText.Button(12)))Application.OpenURL("https://www.openstreetmap.org/copyright");
  GUI.matrix=matrix;GUI.depth=depth;
 }

 Vector2 Mini(Vector3 p,Rect r){return new Vector2(r.x+p.x/session.world.map.width*r.width,r.y+(1-p.z/session.world.map.depth)*r.height);}
 void Dot(Vector2 p,Color c,float size){GUI.color=c;GUI.DrawTexture(new Rect(p.x-size/2,p.y-size/2,size,size),Texture2D.whiteTexture);GUI.color=Color.white;}
 void Line(Vector2 a,Vector2 b,Color color,float width){var matrix=GUI.matrix;GUI.color=color;GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a);GUI.DrawTexture(new Rect(a.x,a.y,Vector2.Distance(a,b),width),Texture2D.whiteTexture);GUI.matrix=matrix;GUI.color=Color.white;}
}
