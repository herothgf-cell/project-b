using UnityEngine;

// Presentation only; condition never feeds taxi controls or passenger manners.
public static class OneDayHUD {
 static GUIStyle text,small,title,button;
 static void Styles(){if(text!=null)return;text=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=18,wordWrap=true};small=new GUIStyle(text){fontSize=14};title=new GUIStyle(text){fontSize=25,fontStyle=FontStyle.Bold};button=new GUIStyle(GUI.skin.button){font=KoreanText.Font,fontSize=16,wordWrap=true};}
 static string Money(int n){return n.ToString("N0")+"원";}
 static string TimeText(double s){int n=Mathf.CeilToInt((float)s);return n/60+":"+(n%60).ToString("00");}
 static void Panel(Rect r){GUI.color=new Color(.035f,.08f,.11f,.97f);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
 static string State(DriverCondition d){return d.fatigue>=100?"피로 한계 · 수면 필요":d.fatigue>=70?"휴식 권장":d.satiety<25?"배고픔 · 피로 증가 빨라짐":"컨디션 양호";}
 public static void Draw(DriveSession s){Styles();var b=s.business;var e=b.ledger;var d=e.Driver;if(d==null)return;
  GUI.depth=-35;
  if(!b.preparing&&!b.ShowingSummary&&!b.restartPrompt){
   Panel(new Rect(16,202,380,120));int minute=480+(int)(System.Math.Min(1200,e.Elapsed)*.6);
   GUI.Label(new Rect(26,205,250,24),e.State==BusinessState.Closing?"마감 후 +"+TimeText(e.Elapsed-1200)+" · 운송 중":minute/60+":"+(minute%60).ToString("00")+" · 남은 "+TimeText(e.SecondsLeft),text);
   GUI.enabled=e.State==BusinessState.Open&&!s.paused&&s.jobs.trip.State!=TripState.OnTrip;if(GUI.Button(new Rect(282,207,104,25),"N 퇴근",button))b.RequestEndDay();GUI.enabled=true;
   GUI.Label(new Rect(26,233,360,22),"포만감 "+d.satiety.ToString("0")+" / 피로 "+d.fatigue.ToString("0")+" · "+State(d),small);
   GUI.Label(new Rect(26,256,360,22),"운임 "+Money(e.Income)+" · 예상 순수익 "+Money(e.ProjectedNet),small);
   GUI.Label(new Rect(26,279,360,22),"현금 "+Money(e.Account.Cash)+" · 미납 "+Money(e.Account.Debt)+" · 연료 "+e.Account.Fuel.ToString("0")+"%",small);
   GUI.Label(new Rect(26,301,360,20),e.SecondsLeft<=60?"마감 임박 · 탑승 승객은 목적지까지 운송":"F7 식당·주차장 안내 / 식사 50초 · 수면 100초",small);
   if(!string.IsNullOrEmpty(b.notice)){Panel(new Rect(410,300,Mathf.Max(330,Screen.width-820),58));GUI.Label(new Rect(420,305,Mathf.Max(310,Screen.width-840),50),b.notice,small);}return;
  }
  var saved=GUI.matrix;float scale=Mathf.Min(1,Screen.width/800f,Screen.height/730f);GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,Vector3.one*scale);float x=(Screen.width/scale-720)/2,y=(Screen.height/scale-680)/2;GUI.depth=-150;Panel(new Rect(x,y,720,680));
  if(b.restartPrompt){GUI.Label(new Rect(x+25,y+25,670,55),"같은 조건으로 새 하루를 시작할까요?",title);GUI.Label(new Rect(x+25,y+100,670,210),"현금 10,000원 · 연료 60% · 포만감 80 · 피로 10\n운행 점수 100점으로 새 회차를 시작합니다.\n이전 완료 결과는 보존하고 진행 중인 회차는 비교에서 제외합니다.\n저장 실패나 취소 시 현재 상태를 유지합니다.",text);GUI.Label(new Rect(x+25,y+360,670,65),b.saveStatus,small);if(GUI.Button(new Rect(x+25,y+590,325,55),"새 하루 시작",button))b.ConfirmRestart(true);if(GUI.Button(new Rect(x+370,y+590,325,55),"취소",button))b.ConfirmRestart(false);}
  else if(b.preparing){GUI.Label(new Rect(x+25,y+25,670,45),"하루 영업 준비 · 08:00~20:00",title);GUI.Label(new Rect(x+25,y+90,670,120),"실직 후 택시로 생계를 이어가는 운전자입니다.\n20분의 예산 안에서 운송·식사·수면을 선택하세요.\n현금 10,000원 · 연료 60% · 포만감 80 · 피로 10",text);GUI.Label(new Rect(x+25,y+225,670,110),"식사 3,000원 / 50초 · 수면 무료 / 100초 · 마감 주차 500원\n배고프면 피로가 빨리 쌓이고, 피로 100에서는 신규 승차가 제한됩니다.\n조작 성능과 매너·운행 점수는 컨디션으로 바뀌지 않습니다.",text);GUI.Label(new Rect(x+25,y+365,670,80),"WASD 운전 · F 승하차 · H 조향 보조 · Q/E 깜빡이\nF7 거점 · F8 기록 · M 전체지도 · Esc 일시정지\n종료 후에는 하루 시작/정산 경계로 복원됩니다.",small);if(GUI.Button(new Rect(x+25,y+470,670,45),"운전 연습 선택",button))s.GetComponent<PracticeSession>().OpenPicker();if(GUI.Button(new Rect(x+25,y+590,325,55),"영업 시작",button))b.Depart();if(GUI.Button(new Rect(x+370,y+590,325,55),"새 하루 시작",button))b.RequestRestart();}
  else {var r=e.Last;GUI.Label(new Rect(x+25,y+20,670,45),"하루 영업 결과 · "+r.endReason,title);GUI.Label(new Rect(x+25,y+80,670,55),"순수익 "+Money(r.net)+" · 운송 "+r.rides+"건 · 실패 "+r.failedTrips+"건",title);GUI.Label(new Rect(x+25,y+145,670,90),"운임 "+Money(r.income)+" − 주유·지원 "+Money(r.fuel)+" − 식사 "+Money(r.food)+" − 주차 "+Money(e.Account.Expense(1,"parking"))+"\n현금 "+Money(e.Account.Cash)+" · 미납 "+Money(e.Account.Debt)+" (비용에 포함)",text);GUI.Label(new Rect(x+25,y+245,670,125),"영업 예산 경과 "+TimeText(r.elapsed)+" · 마지막 운송 추가 "+TimeText(System.Math.Max(0,r.elapsed-1200))+"\n식사 "+d.meals+"회 / "+d.mealSeconds+"초 · 수면 "+d.sleeps+"회 / "+d.sleepSeconds+"초\n종료 포만감 "+d.satiety.ToString("0")+" · 피로 "+d.fatigue.ToString("0")+" · 연료 "+e.Account.Fuel.ToString("0.0")+"%\n실제 경과(메뉴 포함) "+TimeText(r.realSeconds),text);
   CompletedDay prior=null;for(int i=b.completedDays.Count-1;i>=0;i--){var p=b.completedDays[i];if(p.id!=s.law.ids.campaign&&p.configuration==b.ComparisonVersion&&!b.ComparisonVersion.EndsWith("mixed")){prior=p;break;}}
   GUI.Label(new Rect(x+25,y+380,670,80),prior==null?"같은 설정의 이전 완료 결과 없음": "이전 순수익 "+Money(prior.result.net)+" · 차이 "+Money(r.net-prior.result.net)+"\n이전 영업 "+TimeText(prior.result.elapsed)+" · 연료 "+prior.fuel.ToString("0")+"% · 피로 "+prior.result.driver.fatigue.ToString("0"),text);GUI.Label(new Rect(x+25,y+470,670,50),"운행 점수 "+s.law.ledger.TotalScore+" · 승객 매너와 별도\n"+b.saveStatus,small);if(b.SaveFailed&&GUI.Button(new Rect(x+25,y+530,670,40),"저장 재시도",button))b.SaveCheckpoint();if(GUI.Button(new Rect(x+25,y+590,325,55),"같은 조건으로 새 하루",button))b.RequestRestart();if(GUI.Button(new Rect(x+370,y+590,325,55),"운행·승객 기록",button))s.law.OpenHistory();}
  GUI.matrix=saved;
 }
}
