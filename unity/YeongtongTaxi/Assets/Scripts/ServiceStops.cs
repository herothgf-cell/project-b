using UnityEngine;

// Reuse validated right-side stopping locations and the same navigation graph.
public class ServiceStops : MonoBehaviour {
 public DriveSession session; public bool open; public string notice="";
 public readonly int[] indices={3,6,9};
 readonly string[] names={"임시 주유소","임시 식당","임시 주차장"};
 void Start(){if(session.business.ledger.Account==null){enabled=false;return;}
  for(int i=0;i<indices.Length;i++){indices[i]%=session.jobs.stops.Length;var label=new GameObject(names[i]);label.transform.position=session.jobs.stops[indices[i]].Passenger+Vector3.up*3.2f;var t=label.AddComponent<TextMesh>();t.text=names[i]+" · F7";t.fontSize=40;t.characterSize=.09f;t.anchor=TextAnchor.MiddleCenter;t.color=Color.yellow;KoreanText.Apply(t);label.AddComponent<FaceCamera>();}
 }
 void Update(){if(Input.GetKeyDown(KeyCode.F7))Toggle();}
 public void Toggle(){
  if(open){Close();return;}
  if(session.business.preparing||session.law.HistoryOpen||session.business.ShowingSummary||session.business.taxPrompt||session.jobs.Active||session.taxi.body.velocity.magnitude>=.56f){notice="빈차로 안전하게 정차한 뒤 F7";return;}
  open=true;session.paused=true;Time.timeScale=0;
 }
 void Close(){open=false;session.paused=false;Time.timeScale=1;}
 public bool Buy(int kind){
  var b=session.business;var a=b.ledger.Account;int day=b.ledger.Day;
  if(!open||session.jobs.Active||!session.jobs.At(indices[kind]))return false;
  bool ok=kind==0?a.Refuel(session.law.ids.Next("fuel"),50,day):kind==1?a.Meal(session.law.ids.Next("meal"),day):false;
  if(ok){b.ledger.Advance(10,0);notice="이용 완료 · 영업시간 10초 경과";Close();}else notice="현금·연료 잔량 또는 오늘 이용 여부를 확인하세요";return ok;
 }
 void OnGUI(){if(session.business.ledger.Account==null)return;
  var style=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=16,wordWrap=true};var button=new GUIStyle(GUI.skin.button){font=KoreanText.Font,fontSize=14,wordWrap=true};
  if(!open){GUI.Label(new Rect(420,94,700,24),"F7 거점·납부 · "+notice,style);return;}
  GUI.depth=-110;float x=(Screen.width-640)/2f,y=(Screen.height-450)/2f;GUI.color=new Color(.04f,.09f,.12f,1);GUI.DrawTexture(new Rect(x,y,640,450),Texture2D.whiteTexture);GUI.color=Color.white;
  var b=session.business;var a=b.ledger.Account;GUI.Label(new Rect(x+20,y+15,600,55),"거점·직접 납부 (메뉴 동안 시간 정지)\n현금 "+a.Cash.ToString("N0")+"원 · 미납 "+a.Debt.ToString("N0")+"원 · 연료 "+a.Fuel.ToString("0.0")+"%",style);
  for(int i=0;i<3;i++){int k=i;GUI.Label(new Rect(x+20,y+85+i*64,245,50),names[i]+" · "+session.jobs.stops[indices[i]].name,style);
   if(GUI.Button(new Rect(x+275,y+85+i*64,110,40),"길 안내",button)){session.jobs.nav.SetDestination(session.jobs.stops[indices[i]]);Close();}
   GUI.enabled=session.jobs.At(indices[i]);
   if(GUI.Button(new Rect(x+400,y+85+i*64,220,40),i==0?"50% 주유 (최대 2,000원)\n영업시간 10초":i==1?"식사 3,000원 / 10초":"주차·영업 마감 · 500원",button)){if(k==2){Close();b.RequestEndDay();}else Buy(k);}GUI.enabled=true;
  }
  if(GUI.Button(new Rect(x+20,y+292,290,38),a.Renewed?"보험 갱신 완료":"보험 갱신 · 6,000원",button))notice=a.Renew(session.law.ids.Next("insurance"),b.ledger.Day)?"3일차부터 갱신 보험 적용":"이미 갱신했거나 현금 부족";
  if(GUI.Button(new Rect(x+330,y+292,290,38),a.TaxPaid?"세금 납부 완료":"세금 납부 · 1,500원",button))notice=a.PayTax(session.law.ids.Next("tax"),b.ledger.Day)?"세금 납부 완료":"이미 납부했거나 현금 부족";
  GUI.Label(new Rect(x+20,y+338,600,46),notice+"\n보험: 2일차 만료 / 세금: 3일차 기한. 거점은 가상 배치입니다.",style);
  if(GUI.Button(new Rect(x+20,y+395,600,35),"취소·주행으로 돌아가기 (F7)",button))Close();
 }
}
