using UnityEngine;
public class DriveHUD : MonoBehaviour {
 public DriveSession session;public Texture2D tile;GUIStyle title,body,small;const int W=1024,H=1024;
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
 public string OfferText(int index){var jobs=session.jobs;var o=jobs.offers[index];var zones=GetComponent<ServiceLawZones>();return (index+1)+"  "+KoreanText.Stop(jobs.stops[o.pickup].name)+" → "+KoreanText.Stop(jobs.stops[o.drop].name)+"    "+o.distance.ToString("0")+"m · "+o.fare.ToString("N0")+"원 · "+o.seconds.ToString("0")+"초 · 요구 "+o.requiredScore+"점\n"+(zones?zones.OfferSituation(o):"일반 운송 · 지정 상황 구역 없음");}
 public string MannerCaption {get{return session.jobs.trip.State==TripState.OnTrip?"승객 매너 "+session.manners.ledger.Score+"점 / 요구 "+session.jobs.current.requiredScore+"점":session.jobs.trip.State==TripState.ToPickup?"승객 매너 · 탑승 전 / 요구 "+session.jobs.current.requiredScore+"점":"승객 매너 · 탑승 시 100점으로 시작";}}
 public string LawCaption {get{int day=session.business.ledger.Day,valid=session.law.ledger.ValidForDay(day);return "오늘 법규 "+session.law.ledger.Score(day)+"점 · "+(valid==0?"아직 유효 평가 없음":"유효 "+valid+"건")+" · F8";}}
 void Gauge(Rect r,float value,Color color,int requirement=-1){GUI.color=new Color(.13f,.18f,.21f);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=color;GUI.DrawTexture(new Rect(r.x,r.y,r.width*Mathf.Clamp01(value/100),r.height),Texture2D.whiteTexture);if(requirement>=0){GUI.color=Color.white;GUI.DrawTexture(new Rect(r.x+r.width*Mathf.Clamp01(requirement/100f)-1,r.y-2,2,r.height+4),Texture2D.whiteTexture);}GUI.color=Color.white;}
 void OnGUI(){if(PracticeSession.Enabled&&!session.paused)return;if(session.PracticeMenuOpen||session.business.restartPrompt||session.jobs==null||session.business.ShowingSummary||session.business.preparing||session.business.taxPrompt||session.services.open||session.law.HistoryOpen)return;if(title==null){GUI.skin.font=KoreanText.Font;title=new GUIStyle(GUI.skin.label){fontSize=23,fontStyle=FontStyle.Bold};body=new GUIStyle(GUI.skin.label){fontSize=16};small=new GUIStyle(GUI.skin.label){fontSize=13};title.normal.textColor=body.normal.textColor=small.normal.textColor=Color.white;}
  var jobs=session.jobs;GUI.Box(new Rect(16,16,380,176),GUIContent.none);GUI.Label(new Rect(28,22,360,35),RoadRulesTrial.Enabled?"가상 도로 규칙 시험장":MapCatalog.Name(session.world.map.mapId),title);
  GUI.Label(new Rect(28,54,355,25),Mathf.Abs(session.taxi.Speed*3.6f).ToString("00")+" km/h   "+(session.taxi.Speed<-.2f?"R":"D")+"   수입 "+(session.business.enabledCampaign?session.business.ledger.Income:jobs.balance).ToString("N0")+"원",body);
  GUI.Label(new Rect(28,80,355,23),(session.drivingCamera.FirstPerson?"1인칭":"3인칭")+" · "+(session.assisted.Active?"보조":"수동")+"   방향지시등: "+(session.signals.direction<0?"좌측":session.signals.direction>0?"우측":"꺼짐"),small);
  bool occupied=jobs.trip.State==TripState.OnTrip;GUI.Label(new Rect(28,103,355,20),MannerCaption,small);Gauge(new Rect(28,124,355,6),occupied?session.manners.ledger.Score:0,occupied?(session.manners.ledger.Score>=jobs.current.requiredScore?new Color(.2f,.85f,.55f):new Color(1,.5f,.2f)):Color.gray,occupied?jobs.current.requiredScore:-1);
  GUI.Label(new Rect(28,132,355,20),LawCaption,small);Gauge(new Rect(28,153,355,6),session.law.ledger.Score(session.business.ledger.Day),session.law.ledger.ValidForDay(session.business.ledger.Day)==0?Color.gray:new Color(.35f,.7f,1));
  GUI.Label(new Rect(28,164,355,24),Time.time<session.manners.eventUntil?session.manners.lastEvent:"지도·시설 현장 규제 미검증 · 법규와 매너 별도",new GUIStyle(small){fontSize=11,wordWrap=true});
  float navY=session.assist.Visible?Screen.height-50:Screen.height-205;float navWidth=Mathf.Min(600,Screen.width-832),navX=Screen.width-navWidth-16;
  if(session.assist.Visible){GUI.Box(new Rect(navX,navY,navWidth,38),GUIContent.none);GUI.Label(new Rect(navX+8,navY+4,navWidth-16,30),jobs.nav.active?jobs.nav.instruction:"교통 상황을 확인하세요",small);}
  else if(jobs.nav.active){
   GUI.Box(new Rect(navX,navY,navWidth,110),GUIContent.none);
   GUI.Label(new Rect(navX+12,navY+6,navWidth-24,28),jobs.nav.instruction,body);
   GUI.Label(new Rect(navX+12,navY+30,navWidth-24,28),jobs.nav.PreferenceLabel+" · "+jobs.nav.remaining.ToString("0")+"m · 약 "+Mathf.CeilToInt(jobs.nav.estimatedSeconds)+"초",body);
   GUI.enabled=!session.paused;
   if(GUI.Button(new Rect(navX+12,navY+57,navWidth-24,26),"Tab: "+(jobs.nav.PreferFastest?"짧은 길":"빠른 길")+" 선택 · "+jobs.nav.alternativeDistance.ToString("0")+"m / 약 "+Mathf.CeilToInt(jobs.nav.alternativeSeconds)+"초",KoreanText.Button(13)))jobs.nav.TogglePreference();
   GUI.enabled=true;
   GUI.Label(new Rect(navX+12,navY+84,navWidth-24,25),string.IsNullOrEmpty(jobs.nav.rerouteNotice)?"정체·정차 차량을 반영한 예상 시간":jobs.nav.rerouteNotice,small);
  }else if(!RoadRulesTrial.Enabled&&!HdRouteTrial.Enabled){GUI.Box(new Rect(navX,navY,navWidth,58),GUIContent.none);GUI.Label(new Rect(navX+12,navY+5,navWidth-24,48),session.roadConditions.Notice,new GUIStyle(small){wordWrap=true});}
  if(session.assist.Visible)GUI.Label(new Rect(Screen.width*.745f,Screen.height*.69f-26,Screen.width*.24f,25),(session.assist.Mode=="REAR CAMERA"?"후방 카메라":session.assist.Mode=="LEFT CAMERA"?"좌측 카메라":"우측 카메라"),body);
  float assistY=session.business.enabledCampaign?330:198;
  GUI.Box(new Rect(16,assistY,380,62),GUIContent.none);GUI.Label(new Rect(28,assistY+3,365,26),session.assisted.Status,small);
  GUI.Label(new Rect(28,assistY+29,365,24),session.assisted.Active?"A/D 차로 · Q/E 회전 · 가능 "+session.assisted.AvailableTurns+" · H 수동":"수동: 직접 조향 / H 보조 → Q/E 회전 · W 가속",small);
  if(!RoadRulesTrial.Enabled&&!HdRouteTrial.Enabled){
  float y=Screen.height-211;
  if(jobs.trip.State==TripState.Idle){float offersY=Screen.height-284;GUI.Box(new Rect(16,offersY,790,214),GUIContent.none);GUI.Label(new Rect(28,offersY+5,760,25),"호출 선택 1·2·3 / 길가 손님 옆 F · M 전체 구역 이름",body);
   var offerStyle=new GUIStyle(GUI.skin.button){font=KoreanText.Font,fontSize=14,alignment=TextAnchor.MiddleLeft,wordWrap=true,padding=new RectOffset(10,10,3,3)};
   GUI.enabled=session.business.CanAccept&&!session.paused;for(int i=0;i<3;i++)if(GUI.Button(new Rect(28,offersY+35+i*58,762,54),OfferText(i),offerStyle))jobs.Accept(i);GUI.enabled=true;
  }else{GUI.Box(new Rect(16,y+28,790,115),GUIContent.none);string state=jobs.trip.State==TripState.ToPickup?"승객에게 이동 중":jobs.trip.State==TripState.OnTrip?"승객 운송 중":(jobs.trip.State==TripState.Complete?"운송 완료":"운송 실패");GUI.Label(new Rect(28,y+34,760,36),state+"    요금: "+jobs.current.fare.ToString("N0")+"원"+(jobs.trip.State==TripState.OnTrip?"    남은 시간: "+Mathf.CeilToInt(jobs.trip.Remaining)+"초":""),title);GUI.Label(new Rect(28,y+72,760,26),jobs.message,body);GUI.Label(new Rect(28,y+101,760,24),"오른쪽 연석 · 화살표와 같은 방향 · 시속 2km 미만 · F 승하차",small);}
  }
  GUI.Box(new Rect(16,Screen.height-59,790,48),GUIContent.none);GUI.Label(new Rect(28,Screen.height-56,760,22),"WASD 운전 · Space 제동 · F 승하차 · Q/E 깜빡이 · C 시점 · R 복귀 · Esc 정지 · F11 전체화면",small);GUI.Label(new Rect(28,Screen.height-34,760,22),session.drivingCamera.FirstPerson?"G 전체지도 · Z/X 좌우 보기 · V 코너 시선 · B 뒤보기":"G 전체지도 · F7 거점 · F8 기록 · F10 안내 · B 뒤보기 / 후진·깜빡이: 보조 카메라",small);
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
   if(session.business.useV2&&GUI.Button(new Rect(x+20,y+282,440,40),"1일차부터 다시 시작"))session.business.RequestRestart();
   if(GUI.Button(new Rect(x+20,y+334,440,40),"운전 연습 선택 · 출구 정체 / 우회전"))session.GetComponent<PracticeSession>().OpenPicker();
   GUI.enabled=true;
   if(GUI.Button(new Rect(x+20,y+386,440,40),"게임 종료 (완료한 정산까지 저장)"))Application.Quit();
  }
 }
 Vector2 Mini(Vector3 p,Rect r){return new Vector2(r.x+p.x/session.world.map.width*r.width,r.y+(1-p.z/session.world.map.depth)*r.height);}
 void Dot(Vector2 p,Color c,float size){GUI.color=c;GUI.DrawTexture(new Rect(p.x-size/2,p.y-size/2,size,size),Texture2D.whiteTexture);GUI.color=Color.white;}
 void Line(Vector2 a,Vector2 b,Color color,float width){var matrix=GUI.matrix;GUI.color=color;GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a);GUI.DrawTexture(new Rect(a.x,a.y,Vector2.Distance(a,b),width),Texture2D.whiteTexture);GUI.matrix=matrix;GUI.color=Color.white;}
}
