using UnityEngine;
public class DriveHUD : MonoBehaviour {
 public DriveSession session;Texture2D tile;GUIStyle title,body,small;const int W=240,H=280;
 void Start(){tile=new Texture2D(W,H);var pixels=new Color[W*H];for(int i=0;i<pixels.Length;i++)pixels[i]=new Color(.10f,.15f,.17f);tile.SetPixels(pixels);
  foreach(var road in session.world.map.roads)for(int i=1;i<road.points.Length;i++){var a=road.points[i-1].V();var b=road.points[i].V();float len=Vector3.Distance(a,b);for(float d=0;d<=len;d+=1){var p=Vector3.Lerp(a,b,len>0?d/len:0);int x=Mathf.RoundToInt(p.x/session.world.map.width*(W-1)),y=Mathf.RoundToInt(p.z/session.world.map.depth*(H-1));for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)if(x+dx>=0&&x+dx<W&&y+dy>=0&&y+dy<H)tile.SetPixel(x+dx,y+dy,new Color(.43f,.49f,.50f));}}
  tile.Apply();
 }
 void OnGUI(){if(session.jobs==null||session.business.ShowingSummary)return;if(title==null){GUI.skin.font=KoreanText.Font;title=new GUIStyle(GUI.skin.label){fontSize=23,fontStyle=FontStyle.Bold};body=new GUIStyle(GUI.skin.label){fontSize=16};small=new GUIStyle(GUI.skin.label){fontSize=13};title.normal.textColor=body.normal.textColor=small.normal.textColor=Color.white;}
  var jobs=session.jobs;GUI.Box(new Rect(16,16,380,176),GUIContent.none);GUI.Label(new Rect(28,22,360,35),"영통 택시 · 영업 운행",title);
  GUI.Label(new Rect(28,61,355,27),Mathf.Abs(session.taxi.Speed*3.6f).ToString("00")+" km/h   "+(session.taxi.Speed<-.2f?"R":"D")+"   수입 "+(session.business.enabledCampaign?session.business.ledger.Income:jobs.balance).ToString("N0")+"원",body);
  GUI.Label(new Rect(28,89,355,24),(session.drivingCamera.FirstPerson?"1인칭":"3인칭")+"   방향지시등: "+(session.signals.direction<0?"좌측":session.signals.direction>0?"우측":"꺼짐"),body);
  GUI.Label(new Rect(28,119,355,24),"매너 "+session.manners.ledger.Score+"점"+(jobs.trip.State!=TripState.Idle?" / 승객 요구 "+jobs.current.requiredScore+"점":" · 승객 탑승 시 100점"),small);
  GUI.Label(new Rect(28,148,355,26),Time.time<session.manners.eventUntil?session.manners.lastEvent:session.taxi.CornerAssisting?"코너 감속 보조 · 급회전 전 미리 감속하세요":"게임용 판정 · 신호·충돌·승차감·도로 경계",small);
  float navY=session.drivingCamera.FirstPerson?110:16;
  if(jobs.nav.active){GUI.Box(new Rect(410,navY,Screen.width-680,116),GUIContent.none);GUI.Label(new Rect(424,navY+8,Screen.width-706,30),jobs.nav.instruction,body);GUI.Label(new Rect(424,navY+43,Screen.width-706,30),jobs.nav.remaining.ToString("0")+"m · "+(jobs.trip.State==TripState.ToPickup?"픽업 지점":"목적지"),body);GUI.Label(new Rect(424,navY+74,Screen.width-706,25),"경로를 벗어나면 자동으로 다시 안내합니다",small);}
  TrafficJunction nearby=null;float nearest=45;foreach(var j in session.traffic.junctions){float d=Vector3.Distance(session.taxi.transform.position,j.center);if(d<nearest){nearest=d;nearby=j;}}
  if(nearby!=null){int axis=Mathf.Abs(Vector3.Dot(session.taxi.transform.forward,session.traffic.mainDirection))>.7f?0:1;var straight=session.traffic.Signal(nearby,axis);var left=session.traffic.LeftSignal(nearby,axis);float row=jobs.nav.active?navY+122:navY;GUI.Box(new Rect(410,row,Screen.width-680,30),GUIContent.none);GUI.Label(new Rect(420,row+2,Screen.width-700,26),nearby.name+"  직진 "+(straight==SignalColor.Green?"진행":straight==SignalColor.Yellow?"황색":"정지")+" / ← 좌회전 "+(left==SignalColor.Green?"진행":"대기"),small);}
  var rect=new Rect(Screen.width-248,16,232,271);GUI.DrawTexture(rect,tile);
  for(int i=1;i<jobs.nav.path.Count;i++)Line(Mini(jobs.nav.path[i-1],rect),Mini(jobs.nav.path[i],rect),Color.cyan,3);
  for(int i=0;i<jobs.stops.Length;i++){if(!jobs.people[i].activeSelf)continue;Dot(Mini(jobs.stops[i].V(),rect),Color.cyan,5);}
  foreach(var car in session.traffic.cars){var p=Mini(car.transform.position,rect);if(rect.Contains(p))Dot(p,Color.white,4);}
  if(jobs.nav.active)Dot(Mini(jobs.nav.destination,rect),new Color(1,.4f,.1f),9);Dot(Mini(session.taxi.transform.position,rect),Color.yellow,8);
  GUI.Label(new Rect(Screen.width-248,289,232,25),"노랑: 내 택시 / 하늘색: 손님",small);
  if(session.assist.Visible)GUI.Label(new Rect(Screen.width*.745f,Screen.height*.69f-26,Screen.width*.24f,25),(session.assist.Mode=="REAR CAMERA"?"후방 카메라":session.assist.Mode=="LEFT CAMERA"?"좌측 카메라":"우측 카메라"),body);
  float assistY=session.business.enabledCampaign?330:198;
  GUI.Box(new Rect(16,assistY,380,62),GUIContent.none);GUI.Label(new Rect(28,assistY+3,365,26),session.assisted.Status,small);
  GUI.Label(new Rect(28,assistY+29,365,24),session.assisted.Active?"A/D 차로 · Q/E 회전 · 가능 "+session.assisted.AvailableTurns+" · H 수동":"H 보조 운전 · 차로에 맞춰 켜세요",small);
  float y=Screen.height-211;
  if(jobs.trip.State==TripState.Idle){GUI.Box(new Rect(16,y,790,145),GUIContent.none);GUI.Label(new Rect(28,y+5,760,25),"호출 선택 1·2·3 / 길가 손님 옆 정차 후 F로 탑승",body);
   GUI.enabled=session.business.CanAccept&&!session.paused;for(int i=0;i<3;i++){var o=jobs.offers[i];string text=(i+1)+"  "+KoreanText.Stop(jobs.stops[o.pickup].name)+" > "+KoreanText.Stop(jobs.stops[o.drop].name)+"    "+o.distance.ToString("0")+" m    "+o.fare.ToString("N0")+"원    "+o.seconds.ToString("0")+"초    요구 "+o.requiredScore+"점";if(GUI.Button(new Rect(28,y+35+i*33,762,29),text))jobs.Accept(i);}GUI.enabled=true;
  }else{GUI.Box(new Rect(16,y+28,790,115),GUIContent.none);string state=jobs.trip.State==TripState.ToPickup?"승객에게 이동 중":jobs.trip.State==TripState.OnTrip?"승객 운송 중":(jobs.trip.State==TripState.Complete?"운송 완료":"운송 실패");GUI.Label(new Rect(28,y+34,760,36),state+"    요금: "+jobs.current.fare.ToString("N0")+"원"+(jobs.trip.State==TripState.OnTrip?"    남은 시간: "+Mathf.CeilToInt(jobs.trip.Remaining)+"초":""),title);GUI.Label(new Rect(28,y+72,760,26),jobs.message,body);GUI.Label(new Rect(28,y+101,760,24),"오른쪽 연석 · 화살표와 같은 방향 · 시속 2km 미만 · F 승하차",small);}
  GUI.Box(new Rect(16,Screen.height-59,790,48),GUIContent.none);GUI.Label(new Rect(28,Screen.height-56,760,22),"WASD 운전 · Space 제동 · F 승하차 · Q/E 깜빡이 · C 시점 · R 복귀 · Esc 정지",small);GUI.Label(new Rect(28,Screen.height-34,760,22),session.drivingCamera.FirstPerson?"Z/X 또는 오른쪽 스틱: 좌우 보기 · V: 코너 시선 보정 · B: 뒤보기":"후진·깜빡이: 자동 보조 카메라 / B를 누르는 동안 뒤보기",small);
  if(session.paused){GUI.Box(new Rect(Screen.width/2-160,Screen.height/2-70,320,140),"일시정지");if(GUI.Button(new Rect(Screen.width/2-130,Screen.height/2-30,260,35),"계속 운전")){session.paused=false;Time.timeScale=1;}if(GUI.Button(new Rect(Screen.width/2-130,Screen.height/2+15,260,35),"게임 종료 (완료한 정산까지 저장)"))Application.Quit();}
 }
 Vector2 Mini(Vector3 p,Rect r){return new Vector2(r.x+p.x/session.world.map.width*r.width,r.y+(1-p.z/session.world.map.depth)*r.height);}
 void Dot(Vector2 p,Color c,float size){GUI.color=c;GUI.DrawTexture(new Rect(p.x-size/2,p.y-size/2,size,size),Texture2D.whiteTexture);GUI.color=Color.white;}
 void Line(Vector2 a,Vector2 b,Color color,float width){var matrix=GUI.matrix;GUI.color=color;GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a);GUI.DrawTexture(new Rect(a.x,a.y,Vector2.Distance(a,b),width),Texture2D.whiteTexture);GUI.matrix=matrix;GUI.color=Color.white;}
}
