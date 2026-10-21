using System.Collections.Generic;
using UnityEngine;

public class ServiceStops : MonoBehaviour {
 public DriveSession session;public bool open;public string notice="";public int pending=-1;public string destination="";
 float noticeUntil;int pendingFuel=-1;public float FuelAmount {get{return Mathf.Min(10,100-session.business.ledger.Account.Fuel);}}
 // Retained for meal/parking and old regional stop references. Eungye fuel uses actual pump bays.
 public readonly int[] indices={3,6,9};
 readonly string[] names={"임시 주유소","임시 식당","임시 주차장"};
 public IReadOnlyList<FuelStationLayout.FuelStation> Stations {get{return FuelStationLayout.For(session.world.map);}}
 public int FuelAtPump {get{var taxi=session.taxi;for(int i=0;i<Stations.Count;i++)if(Stations[i].CanRefuel(taxi.transform.position,taxi.transform.forward,taxi.body.velocity.magnitude))return i;return -1;}}
 public bool AtFuelPump(){return Stations.Count>0?FuelAtPump>=0:session.world.map.mapId!="eungye"&&session.jobs.At(indices[0]);}
 public bool CanUse(int kind){return kind>=0&&kind<=2&&!session.jobs.Active&&session.taxi.body.velocity.magnitude<.56f&&(kind==0?AtFuelPump():session.jobs.At(indices[kind]));}
 public string Quote(int kind){if(kind<0||kind>2)return "";var a=session.business.ledger.Account;return kind==0?"주유 "+FuelAmount.ToString("0.0")+"% · "+Mathf.CeilToInt(FuelAmount*session.business.settings.fuelUnitPrice).ToString("N0")+"원 / 10초":kind==1?"식사 3,000원 / 10초 (하루 1회)":"주차·영업 마감 · 하루 주차비 500원\n미식사 비용 등 하루 비용도 정산합니다";}
 void Start(){
  if(session.business.ledger.Account==null){enabled=false;return;}
  if(session.world.map.mapId=="eungye")indices[2]=0;
  for(int i=0;i<indices.Length;i++){
   indices[i]%=session.jobs.stops.Length;if(i==0&&Stations.Count>0)continue;
   var label=new GameObject(names[i]);label.transform.position=session.jobs.stops[indices[i]].Passenger+Vector3.up*3.2f;var t=label.AddComponent<TextMesh>();t.text=names[i]+" · F7";t.fontSize=40;t.characterSize=.09f;t.anchor=TextAnchor.MiddleCenter;t.color=Color.yellow;KoreanText.Apply(t);label.AddComponent<FaceCamera>();
  }
 }
 void Update(){if(noticeUntil>0&&Time.unscaledTime>=noticeUntil){notice="";noticeUntil=0;}if(Input.GetKeyDown(KeyCode.F7))Toggle();}
 public void Toggle(){
  if(open){Close();return;}
  if(session.FullMapOpen||session.PracticeMenuOpen||session.PracticeBusy||session.business.restartPrompt||PracticeSession.Enabled||session.business.preparing||session.law.HistoryOpen||session.business.ShowingSummary||session.business.taxPrompt||session.jobs.Active||session.taxi.body.velocity.magnitude>=.56f){notice="빈차로 안전하게 정차한 뒤 F7";return;}
  pending=-1;pendingFuel=-1;open=true;session.paused=true;Time.timeScale=0;
 }
 void Close(){pending=-1;pendingFuel=-1;open=false;session.paused=false;Time.timeScale=1;}
 public bool GuideFuel(int station){
  if(!open||station<0||station>=Stations.Count||session.jobs.Active)return false;
  session.jobs.nav.SetDestination(Stations[station].approach);destination=Stations[station].name+" · 입구 앞까지 내비/H 안내 · 내부는 H 해제 후 수동 진입";Close();return true;
 }
 public bool Guide(int kind){
  if(!open||kind<0||kind>2||session.jobs.Active)return false;
  if(kind==0&&Stations.Count>0){int closest=0;float best=float.MaxValue;for(int i=0;i<Stations.Count;i++){float distance=(Stations[i].entrance-session.taxi.transform.position).sqrMagnitude;if(distance<best){best=distance;closest=i;}}return GuideFuel(closest);}
  session.jobs.nav.SetDestination(session.jobs.stops[indices[kind]]);destination=names[kind]+" · 오른쪽 정차 화살표에서 F7";Close();return true;
 }
 public bool RequestAction(int kind){if(session.FullMapOpen||!open||!CanUse(kind))return false;pending=kind;pendingFuel=kind==0?FuelAtPump:-1;return true;}
 public bool ConfirmAction(bool accept){
  if(!open||pending<0)return false;int kind=pending,station=pendingFuel;pending=-1;pendingFuel=-1;
  if(!accept){notice="취소했습니다 · 결제/시간 차감 없음";noticeUntil=Time.unscaledTime+4;return false;}
  if(session.FullMapOpen||!CanUse(kind)||kind==0&&Stations.Count>0&&FuelAtPump!=station)return false;
  if(kind<2)return Buy(kind);Close();return session.business.RequestEndDay();
 }
 public bool Buy(int kind){
  if(kind<0||kind>1||session.FullMapOpen||!open||!CanUse(kind))return false;var b=session.business;var a=b.ledger.Account;int day=b.ledger.Day;
  bool ok=kind==0?a.Refuel(session.law.ids.Next("fuel"),FuelAmount,day):a.Meal(session.law.ids.Next("meal"),day);
  if(ok){b.ledger.Advance(10,0);notice="이용 완료 · 영업시간 10초 경과";noticeUntil=Time.unscaledTime+4;destination="";if(kind!=0)Close();}else notice="현금·연료 잔량 또는 오늘 이용 여부를 확인하세요";return ok;
 }
 void Row(float x,float y,string label,int kind,int station,GUIStyle style,GUIStyle button){
  GUI.Label(new Rect(x+20,y,330,51),label,style);
  if(GUI.Button(new Rect(x+360,y,105,42),"길 안내",button)){if(station>=0)GuideFuel(station);else Guide(kind);}
  GUI.enabled=CanUse(kind)&&(station<0||FuelAtPump==station)&&(kind!=0||FuelAmount>.001f);
  if(GUI.Button(new Rect(x+480,y,220,42),kind==0?"+"+FuelAmount.ToString("0.#")+"% 주유 · "+Mathf.CeilToInt(FuelAmount*session.business.settings.fuelUnitPrice)+"원":kind==1?"식사 비용 확인":"주차·영업 마감",button)){if(kind==0)Buy(0);else RequestAction(kind);}GUI.enabled=true;
 }
 void OnGUI(){
  if(session.business.ledger.Account==null||PracticeSession.Enabled)return;
  var style=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=16,wordWrap=true};var button=new GUIStyle(GUI.skin.button){font=KoreanText.Font,fontSize=14,wordWrap=true};
  if(!open){if((session.ShowHints||notice!=""||destination!="")&&!session.business.ShowingSummary&&!session.business.preparing&&!session.law.HistoryOpen)GUI.Label(new Rect(420,94,Mathf.Max(500,Screen.width-440),58),"F7 거점 · "+notice+(destination==""?"":"\n"+destination),style);return;}
  GUI.depth=-110;float height=Stations.Count>0?565:450,x=(Screen.width-720)/2f,y=(Screen.height-height)/2f;
  GUI.color=new Color(.04f,.09f,.12f,1);GUI.DrawTexture(new Rect(x,y,720,height),Texture2D.whiteTexture);GUI.color=Color.white;
  var b=session.business;var a=b.ledger.Account;GUI.Label(new Rect(x+20,y+15,680,55),"생활 거점 (메뉴 동안 시간 정지)\n현금 "+a.Cash.ToString("N0")+"원 · 미납 "+a.Debt.ToString("N0")+"원 · 연료 "+a.Fuel.ToString("0.0")+"%",style);
  if(pending>=0){GUI.Label(new Rect(x+30,y+110,660,160),Quote(pending)+"\n\n확정할 때만 결제합니다. 현금이 부족하면 실행하지 않습니다.",style);if(GUI.Button(new Rect(x+30,y+height-130,310,50),"확정",button))ConfirmAction(true);if(GUI.Button(new Rect(x+380,y+height-130,310,50),"취소",button))ConfirmAction(false);return;}
  int row=0;
  if(Stations.Count>0){for(int i=0;i<Stations.Count;i++)Row(x,y+85+row++*62,Stations[i].name,0,i,style,button);}else Row(x,y+85+row++*62,names[0]+" · "+session.jobs.stops[indices[0]].name,0,-1,style,button);
  for(int i=1;i<3;i++)Row(x,y+85+row++*62,names[i]+" · "+session.jobs.stops[indices[i]].name,i,-1,style,button);
  GUI.Label(new Rect(x+20,y+height-155,680,108),notice+"\n"+(Stations.Count>0?"주유소는 빈 부지에 배치한 게임시설입니다. 내비/H는 도로 입구 앞까지 안내합니다. H 해제 후 화살표를 따라 수동 진입 → 주유 구역에 같은 방향으로 정차 → F7 → 별도 출구. 도로변에서는 주유할 수 없습니다.":"주유소·식당·주차장은 임시 배치된 생활 거점입니다."),style);
  if(GUI.Button(new Rect(x+20,y+height-45,680,35),"취소·주행으로 돌아가기 (F7)",button))Close();
 }
}
