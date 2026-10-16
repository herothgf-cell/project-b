using UnityEngine;
public class DriveHUD : MonoBehaviour {
 public DriveSession session;Texture2D tile;GUIStyle title,body,small;const int W=240,H=280;
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
 void OnGUI(){if(session.jobs==null||session.business.ShowingSummary||session.business.preparing||session.business.taxPrompt||session.services.open||session.law.HistoryOpen)return;if(title==null){GUI.skin.font=KoreanText.Font;title=new GUIStyle(GUI.skin.label){fontSize=23,fontStyle=FontStyle.Bold};body=new GUIStyle(GUI.skin.label){fontSize=16};small=new GUIStyle(GUI.skin.label){fontSize=13};title.normal.textColor=body.normal.textColor=small.normal.textColor=Color.white;}
  var jobs=session.jobs;GUI.Box(new Rect(16,16,380,176),GUIContent.none);GUI.Label(new Rect(28,22,360,35),RoadRulesTrial.Enabled?"가상 도로 규칙 시험장":MapCatalog.Name(session.world.map.mapId),title);
  GUI.Label(new Rect(28,61,355,27),Mathf.Abs(session.taxi.Speed*3.6f).ToString("00")+" km/h   "+(session.taxi.Speed<-.2f?"R":"D")+"   수입 "+(session.business.enabledCampaign?session.business.ledger.Income:jobs.balance).ToString("N0")+"원",body);
  GUI.Label(new Rect(28,89,355,24),(session.drivingCamera.FirstPerson?"1인칭":"3인칭")+" · "+(session.assisted.Active?"보조":"수동")+"   방향지시등: "+(session.signals.direction<0?"좌측":session.signals.direction>0?"우측":"꺼짐"),body);
  GUI.Label(new Rect(28,119,355,24),"매너 "+session.manners.ledger.Score+"점"+(jobs.trip.State!=TripState.Idle?" / 승객 요구 "+jobs.current.requiredScore+"점":" · 승객 탑승 시 100점"),small);
  GUI.Label(new Rect(28,148,355,26),Time.time<session.manners.eventUntil?session.manners.lastEvent:session.taxi.CornerAssisting?"코너 감속 보조 · 급회전 전 미리 감속하세요":RoadRulesTrial.Enabled?"가상 지도 · 실제 장소를 재현한 도로가 아닙니다":"지도 형상 기반 · 실제 통행 규제 미검증",small);
  float navY=session.assist.Visible?Screen.height-50:Screen.height-205;float navWidth=Mathf.Min(600,Screen.width-832),navX=Screen.width-navWidth-16;
  if(session.assist.Visible){GUI.Box(new Rect(navX,navY,navWidth,38),GUIContent.none);GUI.Label(new Rect(navX+8,navY+4,navWidth-16,30),jobs.nav.active?jobs.nav.instruction:"교통 상황을 확인하세요",small);}
  else if(jobs.nav.active){
   GUI.Box(new Rect(navX,navY,navWidth,110),GUIContent.none);
   GUI.Label(new Rect(navX+12,navY+6,navWidth-24,28),jobs.nav.instruction,body);
   GUI.Label(new Rect(navX+12,navY+30,navWidth-24,28),jobs.nav.PreferenceLabel+" · "+jobs.nav.remaining.ToString("0")+"m · 약 "+Mathf.CeilToInt(jobs.nav.estimatedSeconds)+"초",body);
   GUI.enabled=!session.paused;
   if(GUI.Button(new Rect(navX+12,navY+57,navWidth-24,26),"Tab: "+(jobs.nav.PreferFastest?"짧은 길":"빠른 길")+" 선택 · "+jobs.nav.alternativeDistance.ToString("0")+"m / 약 "+Mathf.CeilToInt(jobs.nav.alternativeSeconds)+"초"))jobs.nav.TogglePreference();
   GUI.enabled=true;
   GUI.Label(new Rect(navX+12,navY+84,navWidth-24,25),string.IsNullOrEmpty(jobs.nav.rerouteNotice)?"정체·정차 차량을 반영한 예상 시간":jobs.nav.rerouteNotice,small);
  }else if(!RoadRulesTrial.Enabled&&!HdRouteTrial.Enabled){GUI.Box(new Rect(navX,navY,navWidth,58),GUIContent.none);GUI.Label(new Rect(navX+12,navY+5,navWidth-24,48),session.roadConditions.Notice,new GUIStyle(small){wordWrap=true});}
  TrafficJunction nearby=null;float nearest=45;foreach(var j in session.traffic.junctions){if(!j.signalized)continue;float d=Vector3.Distance(session.taxi.transform.position,j.center);if(d<nearest){nearest=d;nearby=j;}}
  if(nearby!=null&&!session.assist.Visible){int axis=Mathf.Abs(Vector3.Dot(session.taxi.transform.forward,session.traffic.AxisDirection(nearby,0)))>.7f?0:1;int sign=session.traffic.Sign(session.taxi.transform.forward,axis,nearby);var straight=session.traffic.Signal(nearby,axis,sign);var left=session.traffic.LeftSignal(nearby,axis,sign);bool clear=session.traffic.YieldClear(nearby,session.taxi.transform.forward,-1,session.taxi.gameObject);float row=jobs.nav.active?navY+116:navY+64;GUI.Box(new Rect(navX,row,navWidth,30),GUIContent.none);GUI.Label(new Rect(navX+10,row+2,navWidth-20,26),nearby.name+"  직진 "+(straight==SignalColor.Green?"진행":straight==SignalColor.Yellow?"황색":"정지")+(nearby.leftTurnMode==LeftTurnMode.Permissive?" / 비보호 ← "+(left!=SignalColor.Green?"대기":clear?"맞은편 확인":"맞은편 차량 양보"):" / 보호 ← "+(left==SignalColor.Green?"진행":"대기")),small);}
  var rect=new Rect(Screen.width-248,16,232,271);GUI.DrawTexture(rect,tile);
  if(session.world.map.hybrid)foreach(var r in session.world.map.junctionRules){if(r.signalMode==0&&!r.crosswalk&&!r.stopLine)continue;var p=Mini(r.V(),rect);if(!rect.Contains(p))continue;Dot(p,r.facilityStatus=="verified"?Color.green:new Color(1,.65f,.15f),7);Dot(p,new Color(.1f,.15f,.16f),3);}
  for(int i=1;i<jobs.nav.alternativePath.Count;i++)Line(Mini(jobs.nav.alternativePath[i-1],rect),Mini(jobs.nav.alternativePath[i],rect),new Color(1,.55f,.1f),4);
  for(int i=1;i<jobs.nav.path.Count;i++)Line(Mini(jobs.nav.path[i-1],rect),Mini(jobs.nav.path[i],rect),Color.cyan,3);
  for(int i=0;i<jobs.stops.Length;i++){if(!jobs.people[i].activeSelf)continue;Dot(Mini(jobs.stops[i].V(),rect),Color.cyan,5);}
  if(session.business.useV2)foreach(int index in session.services.indices)Dot(Mini(jobs.stops[index].V(),rect),new Color(1,.35f,1),8);
  foreach(var incident in session.roadConditions.incidents)Dot(Mini(incident.position,rect),Color.red,8);
  if(session.world.map.landmarks!=null)foreach(var landmark in session.world.map.landmarks){var p=Mini(landmark.V(),rect);if(rect.Contains(p))Dot(p,new Color(.3f,1,.45f),5);}
  foreach(var car in session.traffic.cars){var p=Mini(car.transform.position,rect);if(rect.Contains(p))Dot(p,Color.white,4);}
  if(jobs.nav.active)Dot(Mini(jobs.nav.destination,rect),new Color(1,.4f,.1f),9);Dot(Mini(session.taxi.transform.position,rect),Color.yellow,8);
  GUI.Label(new Rect(Screen.width-248,289,232,25),"노랑: 택시 / 자홍: 거점 / 빨강: 정체",small);
  if(session.world.map.hybrid)GUI.Label(new Rect(Screen.width-248,307,240,22),"시설 □ 주황: 임시 / 초록: 현장 확인",small);
  if(session.assist.Visible)GUI.Label(new Rect(Screen.width*.745f,Screen.height*.69f-26,Screen.width*.24f,25),(session.assist.Mode=="REAR CAMERA"?"후방 카메라":session.assist.Mode=="LEFT CAMERA"?"좌측 카메라":"우측 카메라"),body);
  float assistY=session.business.enabledCampaign?330:198;
  GUI.Box(new Rect(16,assistY,380,62),GUIContent.none);GUI.Label(new Rect(28,assistY+3,365,26),session.assisted.Status,small);
  GUI.Label(new Rect(28,assistY+29,365,24),session.assisted.Active?"A/D 차로 · Q/E 회전 · 가능 "+session.assisted.AvailableTurns+" · H 수동":"수동: 직접 조향 / H 보조 → Q/E 회전 · W 가속",small);
  if(!RoadRulesTrial.Enabled&&!HdRouteTrial.Enabled){
  float y=Screen.height-211;
  if(jobs.trip.State==TripState.Idle){GUI.Box(new Rect(16,y,790,145),GUIContent.none);GUI.Label(new Rect(28,y+5,760,25),"호출 선택 1·2·3 / 길가 손님 옆 정차 후 F로 탑승",body);
   GUI.enabled=session.business.CanAccept&&!session.paused;for(int i=0;i<3;i++){var o=jobs.offers[i];string text=(i+1)+"  "+KoreanText.Stop(jobs.stops[o.pickup].name)+" > "+KoreanText.Stop(jobs.stops[o.drop].name)+"    "+o.distance.ToString("0")+" m    "+o.fare.ToString("N0")+"원    "+o.seconds.ToString("0")+"초    요구 "+o.requiredScore+"점";if(GUI.Button(new Rect(28,y+35+i*33,762,29),text))jobs.Accept(i);}GUI.enabled=true;
  }else{GUI.Box(new Rect(16,y+28,790,115),GUIContent.none);string state=jobs.trip.State==TripState.ToPickup?"승객에게 이동 중":jobs.trip.State==TripState.OnTrip?"승객 운송 중":(jobs.trip.State==TripState.Complete?"운송 완료":"운송 실패");GUI.Label(new Rect(28,y+34,760,36),state+"    요금: "+jobs.current.fare.ToString("N0")+"원"+(jobs.trip.State==TripState.OnTrip?"    남은 시간: "+Mathf.CeilToInt(jobs.trip.Remaining)+"초":""),title);GUI.Label(new Rect(28,y+72,760,26),jobs.message,body);GUI.Label(new Rect(28,y+101,760,24),"오른쪽 연석 · 화살표와 같은 방향 · 시속 2km 미만 · F 승하차",small);}
  }
  GUI.Box(new Rect(16,Screen.height-59,790,48),GUIContent.none);GUI.Label(new Rect(28,Screen.height-56,760,22),"WASD 운전 · Space 제동 · F 승하차 · Q/E 깜빡이 · C 시점 · R 복귀 · Esc 정지 · F11 전체화면",small);GUI.Label(new Rect(28,Screen.height-34,760,22),session.drivingCamera.FirstPerson?"Z/X 또는 오른쪽 스틱: 좌우 보기 · V: 코너 시선 보정 · B: 뒤보기":"F7 거점 · F8 기록 · F10 안내 · B 뒤보기 / 후진·깜빡이: 보조 카메라",small);
  if(session.paused){
   float x=Screen.width/2f-240,y=Screen.height/2f-180;
   GUI.Box(new Rect(x,y,480,360),"일시정지 · 운행 지역");
   GUI.Label(new Rect(x+20,y+34,440,26),"현재: "+MapCatalog.Name(session.world.map.mapId),body);
   for(int i=0;i<MapCatalog.Ids.Length;i++){
    string id=MapCatalog.Ids[i];bool available=MapCatalog.Available(id);GUI.enabled=available;
    if(GUI.Button(new Rect(x+20,y+70+i*42,440,35),MapCatalog.Name(id)+(available?(MapCatalog.NextRegion==id?" · 다음 실행 지역":" · 다음 실행 시 선택"):" · 지도 자료·현장 검토 대기")))MapCatalog.SelectNext(id);
   }
   GUI.enabled=true;
   GUI.Label(new Rect(x+20,y+164,440,55),string.IsNullOrEmpty(MapCatalog.LockedRegion)?"지역 변경은 게임을 다시 실행할 때 적용됩니다.\n영업 마감(N) 후 종료하면 정산 결과가 저장됩니다.":"은계 전용 빌드 · 현재 지역으로 시작합니다.\n영업 마감(N) 후 종료하면 정산 결과가 저장됩니다.",small);
   if(GUI.Button(new Rect(x+20,y+230,440,40),"계속 운전")){session.paused=false;Time.timeScale=1;}
   if(GUI.Button(new Rect(x+20,y+282,440,40),"게임 종료 (완료한 정산까지 저장)"))Application.Quit();
  }
 }
 Vector2 Mini(Vector3 p,Rect r){return new Vector2(r.x+p.x/session.world.map.width*r.width,r.y+(1-p.z/session.world.map.depth)*r.height);}
 void Dot(Vector2 p,Color c,float size){GUI.color=c;GUI.DrawTexture(new Rect(p.x-size/2,p.y-size/2,size,size),Texture2D.whiteTexture);GUI.color=Color.white;}
 void Line(Vector2 a,Vector2 b,Color color,float width){var matrix=GUI.matrix;GUI.color=color;GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a);GUI.DrawTexture(new Rect(a.x,a.y,Vector2.Distance(a,b),width),Texture2D.whiteTexture);GUI.matrix=matrix;GUI.color=Color.white;}
}
