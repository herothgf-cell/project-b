using UnityEngine;
public class DriveHUD : MonoBehaviour {
 public DriveSession session;public Texture2D tile;public bool HelpOpen {get;private set;}GUIStyle title,body,small;const int W=1024,H=1024;
 public void ToggleHelp(){HelpOpen=!HelpOpen;}
 Texture2D lightTile;public Texture2D LightTile {get{if(lightTile||!tile)return lightTile;lightTile=new Texture2D(W,H);var colors=tile.GetPixels();for(int i=0;i<colors.Length;i++){var c=colors[i];colors[i]=c.b>.4f&&c.r<.2f?new Color(.54f,.78f,.86f):c.r>.3f?new Color(.98f,.99f,.96f):new Color(.83f,.88f,.82f);}lightTile.SetPixels(colors);lightTile.Apply();return lightTile;}}
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
 void OnGUI(){if(PracticeSession.Enabled&&!session.paused)return;if(session.FullMapOpen||session.RideResultOpen||session.ChoiceOpen||session.PracticeMenuOpen||session.business.restartPrompt||session.jobs==null||session.business.ShowingSummary||session.business.preparing||session.business.taxPrompt||session.services.open||session.law.HistoryOpen)return;if(title==null){GUI.skin.font=KoreanText.Font;title=new GUIStyle(GUI.skin.label){fontSize=23,fontStyle=FontStyle.Bold};body=new GUIStyle(GUI.skin.label){fontSize=16};small=new GUIStyle(GUI.skin.label){fontSize=13};title.normal.textColor=body.normal.textColor=small.normal.textColor=Color.white;}
  CockpitUI.Draw(this);
  if(HelpOpen)DrawHelp();
  if(session.paused){
   float x=Screen.width/2f-240,y=Screen.height/2f-235;
   GUI.Box(new Rect(x,y,480,470),"일시정지 · 운행 지역");
   GUI.Label(new Rect(x+20,y+34,440,26),"현재: "+MapCatalog.Name(session.world.map.mapId),body);
   for(int i=0;i<MapCatalog.Ids.Length;i++){
    string id=MapCatalog.Ids[i];bool available=MapCatalog.Available(id);GUI.enabled=available;
    if(GUI.Button(new Rect(x+20,y+70+i*42,440,35),MapCatalog.Name(id)+(available?(MapCatalog.NextRegion==id?" · 다음 실행 지역":" · 다음 실행 시 선택"):" · 지도 자료·현장 검토 대기")))MapCatalog.SelectNext(id);
   }
   GUI.enabled=true;
   GUI.Label(new Rect(x+20,y+164,440,55),string.IsNullOrEmpty(MapCatalog.LockedRegion)?"지역 변경은 게임을 다시 실행할 때 적용됩니다.\n영업 마감(N) 후 종료하면 정산 결과가 저장됩니다.":"은계 전용 빌드 · 현재 지역으로 시작합니다.\n영업 마감(N) 후 종료하면 정산 결과가 저장됩니다.",small);
   GUI.enabled=!session.PracticeBusy;
   if(GUI.Button(new Rect(x+20,y+230,440,40),"계속 운전")){session.paused=false;Time.timeScale=1;}
   if(session.business.useV2&&GUI.Button(new Rect(x+20,y+282,440,40),session.business.ledger.Settings.days>1?"1일차부터 다시 시작":"새 하루 시작"))session.business.RequestRestart();
   if(GUI.Button(new Rect(x+20,y+334,215,40),"지도 편집"))session.GetComponent<RoadMapEditor>().Open();
   if(GUI.Button(new Rect(x+245,y+334,215,40),"운전 연습 선택"))session.GetComponent<PracticeSession>().OpenPicker();
   GUI.enabled=true;
   if(GUI.Button(new Rect(x+20,y+386,440,40),"게임 종료 (현재 운송까지 저장)"))Application.Quit();
   if(GUI.Button(new Rect(x+20,y+434,440,28),session.traffic.mitOptions.Label+" · 클릭하여 변경",KoreanText.Button(14)))session.traffic.mitOptions.SetEnabled(!session.traffic.mitOptions.Enabled);
  }
 }
 public string DrivingHelpText {get{return "수동: W 가속 · S 감속 / 정차 후 후진 · A/D 조향 · Space 제동\nQ/E 방향지시등 · G 비상등 · H 조향 보조 켜기/끄기\n보조: A/D 차로 변경 · Q/E 다음 교차로 회전 예약\n보조 중에도 가속·감속·제동과 신호 판단은 직접 합니다.\nC 시점 · B 뒤보기 · Z/X 좌우 보기 · V 코너 시선\nF 승하차 · M 전체 지도 · Tab 경로 변경 · +/- 미니맵 확대/축소\nF7 거점 · F8 기록 · F10 안내 · Esc 정지 · F11 전체화면";}}
 public static Rect HelpBounds(float width,float height){float scale=Mathf.Min(1,width/1600f,height/900f);return new Rect(width/2-300*scale,height/2-150*scale,600*scale,300*scale);}
 void DrawHelp(){
  int depth=GUI.depth;GUI.depth=-20;var matrix=GUI.matrix;float scale=CockpitUI.Scale;
  GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));var r=HelpBounds(Screen.width,Screen.height);r=new Rect(r.x/scale,r.y/scale,r.width/scale,r.height/scale);
  KoreanText.Panel(r);
  GUI.Label(new Rect(r.x+18,r.y+12,490,28),session.assisted.Active?"조향 보조 · H로 수동 전환":"수동 운전 · H로 조향 보조",body);
  if(GUI.Button(new Rect(r.x+516,r.y+10,66,30),"닫기",KoreanText.Button(14)))HelpOpen=false;
  GUI.Label(new Rect(r.x+18,r.y+48,564,36),session.assisted.Status,new GUIStyle(small){wordWrap=true});
  GUI.Label(new Rect(r.x+18,r.y+90,564,198),DrivingHelpText,new GUIStyle(body){wordWrap=true});
  GUI.matrix=matrix;GUI.depth=depth;
 }

 Vector2 Mini(Vector3 p,Rect r){return new Vector2(r.x+p.x/session.world.map.width*r.width,r.y+(1-p.z/session.world.map.depth)*r.height);}
 void Dot(Vector2 p,Color c,float size){GUI.color=c;GUI.DrawTexture(new Rect(p.x-size/2,p.y-size/2,size,size),Texture2D.whiteTexture);GUI.color=Color.white;}
 void Line(Vector2 a,Vector2 b,Color color,float width){var matrix=GUI.matrix;GUI.color=color;GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a);GUI.DrawTexture(new Rect(a.x,a.y,Vector2.Distance(a,b),width),Texture2D.whiteTexture);GUI.matrix=matrix;GUI.color=Color.white;}
}
