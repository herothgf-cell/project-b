using UnityEngine;

// Reuse validated right-side stopping locations and the shared navigation graph.
public class ServiceStops : MonoBehaviour {
 public DriveSession session; public bool open; public string notice=""; public int pending=-1;public string destination="";
 float noticeUntil;
 public readonly int[] indices={3,6,9};
 readonly string[] names={"임시 주유소","임시 식당","임시 주차장"};
 public string Quote(int kind){if(kind<0||kind>2)return "";var a=session.business.ledger.Account;return kind==0?"주유 "+Mathf.Min(50,100-a.Fuel).ToString("0.0")+"% · "+Mathf.CeilToInt(Mathf.Min(50,100-a.Fuel)*session.business.settings.fuelUnitPrice).ToString("N0")+"원 / 10초":kind==1?"식사 3,000원 / 10초 (하루 1회)":"주차·영업 마감 · 하루 주차비 500원\n미식사 비용 등 하루 비용도 정산합니다";}
 void Start(){if(session.business.ledger.Account==null){enabled=false;return;}
  // Temporary facility: Eungye stop 0 uses the wider national-road carriageway.
  // Keep the original service locations for the other regions.
  if(session.world.map.mapId=="eungye")indices[2]=0;
  for(int i=0;i<indices.Length;i++){indices[i]%=session.jobs.stops.Length;var label=new GameObject(names[i]);label.transform.position=session.jobs.stops[indices[i]].Passenger+Vector3.up*3.2f;var t=label.AddComponent<TextMesh>();t.text=names[i]+" · F7";t.fontSize=40;t.characterSize=.09f;t.anchor=TextAnchor.MiddleCenter;t.color=Color.yellow;KoreanText.Apply(t);label.AddComponent<FaceCamera>();}
 }
 void Update(){if(noticeUntil>0&&Time.unscaledTime>=noticeUntil){notice="";noticeUntil=0;}if(Input.GetKeyDown(KeyCode.F7))Toggle();}
 public void Toggle(){
  if(open){Close();return;}
  if(session.FullMapOpen||session.PracticeMenuOpen||session.PracticeBusy||session.business.restartPrompt||PracticeSession.Enabled||session.business.preparing||session.law.HistoryOpen||session.business.ShowingSummary||session.business.taxPrompt||session.jobs.Active||session.taxi.body.velocity.magnitude>=.56f){notice="빈차로 안전하게 정차한 뒤 F7";return;}
  pending=-1;open=true;session.paused=true;Time.timeScale=0;
 }
 void Close(){pending=-1;open=false;session.paused=false;Time.timeScale=1;}
 public bool Guide(int kind){if(!open||kind<0||kind>2||session.jobs.Active)return false;session.jobs.nav.SetDestination(session.jobs.stops[indices[kind]]);destination=names[kind]+" · 오른쪽 정차 화살표에서 F7";Close();return true;}
 public bool RequestAction(int kind){if(session.FullMapOpen||!open||kind<0||kind>2||session.jobs.Active||!session.jobs.At(indices[kind]))return false;pending=kind;return true;}
 public bool ConfirmAction(bool accept){if(!open||pending<0)return false;int kind=pending;pending=-1;if(!accept){notice="취소했습니다 · 결제/시간 차감 없음";noticeUntil=Time.unscaledTime+4;return false;}
  if(session.FullMapOpen||kind>2||session.jobs.Active||session.taxi.body.velocity.magnitude>=.56f||!session.jobs.At(indices[kind]))return false;
  if(kind<2)return Buy(kind);
  if(kind==2){Close();return session.business.RequestEndDay();}
  return false;
 }
 public bool Buy(int kind){
  if(kind<0||kind>1)return false;var b=session.business;var a=b.ledger.Account;int day=b.ledger.Day;
  if(!open||session.jobs.Active||!session.jobs.At(indices[kind]))return false;
  bool ok=kind==0?a.Refuel(session.law.ids.Next("fuel"),50,day):a.Meal(session.law.ids.Next("meal"),day);
  if(ok){b.ledger.Advance(10,0);notice="이용 완료 · 영업시간 10초 경과";noticeUntil=Time.unscaledTime+4;destination="";Close();}else notice="현금·연료 잔량 또는 오늘 이용 여부를 확인하세요";return ok;
 }
 void OnGUI(){if(session.business.ledger.Account==null||PracticeSession.Enabled)return;
  var style=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=16,wordWrap=true};var button=new GUIStyle(GUI.skin.button){font=KoreanText.Font,fontSize=14,wordWrap=true};
  if(!open){if((session.ShowHints||notice!=""||destination!="")&&!session.business.ShowingSummary&&!session.business.preparing&&!session.law.HistoryOpen)GUI.Label(new Rect(420,94,650,40),"F7 거점 · "+notice+(destination==""?"":"\n"+destination),style);return;}
  GUI.depth=-110;float x=(Screen.width-640)/2f,y=(Screen.height-450)/2f;GUI.color=new Color(.04f,.09f,.12f,1);GUI.DrawTexture(new Rect(x,y,640,450),Texture2D.whiteTexture);GUI.color=Color.white;
  var b=session.business;var a=b.ledger.Account;GUI.Label(new Rect(x+20,y+15,600,55),"생활 거점 (메뉴 동안 시간 정지)\n현금 "+a.Cash.ToString("N0")+"원 · 미납 "+a.Debt.ToString("N0")+"원 · 연료 "+a.Fuel.ToString("0.0")+"%",style);
  if(pending>=0){GUI.Label(new Rect(x+30,y+110,580,150),Quote(pending)+"\n\n확정할 때만 결제합니다. 현금이 부족하면 실행하지 않습니다.",style);if(GUI.Button(new Rect(x+30,y+320,280,50),"확정",button))ConfirmAction(true);if(GUI.Button(new Rect(x+330,y+320,280,50),"취소",button))ConfirmAction(false);return;}
  for(int i=0;i<3;i++){GUI.Label(new Rect(x+20,y+85+i*64,245,50),names[i]+" · "+session.jobs.stops[indices[i]].name,style);
   if(GUI.Button(new Rect(x+275,y+85+i*64,110,40),"길 안내",button))Guide(i);
   GUI.enabled=session.jobs.At(indices[i]);if(GUI.Button(new Rect(x+400,y+85+i*64,220,40),i==0?"주유 비용 확인":i==1?"식사 비용 확인":"주차·영업 마감",button))RequestAction(i);GUI.enabled=true;
  }
  GUI.Label(new Rect(x+20,y+310,600,60),notice+"\n주유소·식당·주차장은 임시 배치된 생활 거점입니다.",style);
  if(GUI.Button(new Rect(x+20,y+395,600,35),"취소·주행으로 돌아가기 (F7)",button))Close();
 }
}
