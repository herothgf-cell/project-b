using UnityEngine;

// Modern daily operation UI, shared by the one-day fixture and the three-day prototype.
public static class OneDayHUD {
 static GUIStyle text,small,title,button;
 static void Styles(){if(text!=null)return;text=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=18,wordWrap=true};small=new GUIStyle(text){fontSize=14};title=new GUIStyle(text){fontSize=25,fontStyle=FontStyle.Bold};button=KoreanText.Button(16);}
 static string Money(int n){return n.ToString("N0")+"원";}
 static string Clock(double value){int n=Mathf.CeilToInt((float)System.Math.Max(0,value));return n/60+":"+(n%60).ToString("00");}
 static void Panel(Rect r){GUI.color=new Color(.035f,.08f,.11f,.98f);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
 public static void Draw(DriveSession s){if(s.RideResultOpen)return;Styles();var b=s.business;var e=b.ledger;var d=e.Driver;if(d==null)return;bool multi=e.Settings.days>1;GUI.depth=-35;
  if(!b.preparing&&!b.ShowingSummary&&!b.restartPrompt){
   Panel(new Rect(16,202,380,126));int minute=480+(int)(System.Math.Min(e.Settings.daySeconds,e.Elapsed)*720/e.Settings.daySeconds);
   GUI.Label(new Rect(26,205,252,25),e.Day+"/"+e.Settings.days+"일 · "+(e.State==BusinessState.Closing?"마감 후 +"+Clock(e.Elapsed-e.Settings.daySeconds):minute/60+":"+(minute%60).ToString("00")+" · 남은 "+Clock(e.SecondsLeft)),new GUIStyle(text){fontSize=16});
   GUI.enabled=e.State==BusinessState.Open&&!s.paused&&s.jobs.trip.State!=TripState.OnTrip;if(GUI.Button(new Rect(282,205,104,25),"N 퇴근",button))b.RequestEndDay();GUI.enabled=true;
   DriverStatusGauges.Draw(new Rect(26,234,360,52),d,e.Account);
   GUI.Label(new Rect(26,290,360,18),"오늘 수입 "+Money(e.Income)+" · 누적 순수익 "+Money(e.TotalNet),new GUIStyle(small){fontSize=12});
   GUI.Label(new Rect(26,309,360,18),"현금 "+Money(e.Account.Cash)+" · 미납 "+Money(e.Account.Debt),new GUIStyle(small){fontSize=12});return;
  }
  var saved=GUI.matrix;float scale=Mathf.Min(1,Screen.width/800f,Screen.height/730f);GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,Vector3.one*scale);float x=(Screen.width/scale-720)/2,y=(Screen.height/scale-680)/2;GUI.depth=-150;Panel(new Rect(x,y,720,680));
  if(b.restartPrompt){GUI.Label(new Rect(x+25,y+25,670,55),multi?"새 3일 캠페인을 시작할까요?":"새 하루를 시작할까요?",title);GUI.Label(new Rect(x+25,y+100,670,210),"현금 10,000원 · 연료 60% · 포만감 80 · 피로 10\n1일차와 운행 점수 100점부터 시작합니다.\n이전 완료 결과는 보존합니다. 취소하면 현재 진행을 유지합니다.",text);GUI.Label(new Rect(x+25,y+360,670,65),b.saveStatus,small);if(GUI.Button(new Rect(x+25,y+590,325,55),multi?"새 3일 시작":"새 하루 시작",button))b.ConfirmRestart(true);if(GUI.Button(new Rect(x+370,y+590,325,55),"취소",button))b.ConfirmRestart(false);}
  else if(b.preparing){GUI.Label(new Rect(x+25,y+25,670,45),e.Day+" / "+e.Settings.days+"일차 영업 준비",title);GUI.Label(new Rect(x+25,y+90,670,145),(multi?"3일 동안 얼마를 벌 수 있을까요?\n하루 20분씩 운송·식사·휴식을 선택합니다.\n":"20분의 예산 안에서 운송·식사·휴식을 선택하세요.\n")+"현금 "+Money(e.Account.Cash)+" · 연료 "+e.Account.Fuel.ToString("0")+"%\n포만감 "+d.satiety.ToString("0")+" · 피로 "+d.fatigue.ToString("0"),text);GUI.Label(new Rect(x+25,y+250,670,115),"식사 3,000원 / 50초 · 수면 무료 / 100초 · 마감 주차 500원\n밤사이 수면: 피로 60 회복 · 포만감은 이어집니다.\n현금·연료·운행 점수는 다음 날에도 이어집니다.",text);GUI.Label(new Rect(x+25,y+385,670,80),"기본은 수동 운전 · H 조향 보조 · 가속/제동은 직접\nF 승하차 · G 비상등 · M 전체지도 · 도움말 버튼\n운행 점수와 승객 만족도는 별도입니다.",small);if(GUI.Button(new Rect(x+25,y+485,670,40),"운전 연습 선택",button))s.GetComponent<PracticeSession>().OpenPicker();if(GUI.Button(new Rect(x+25,y+590,325,55),e.Day+"일차 영업 시작",button))b.Depart();if(GUI.Button(new Rect(x+370,y+590,325,55),multi?"1일차부터 새로 시작":"새 하루 시작",button))b.RequestRestart();}
  else {var r=e.Last;bool complete=e.State==BusinessState.Completed;GUI.Label(new Rect(x+25,y+20,670,45),complete&&multi?"3일 캠페인 완료!":e.Day+"일차 영업 결과",title);
   GUI.Label(new Rect(x+25,y+82,670,45),complete&&multi?"3일간 획득한 순수익  "+Money(e.TotalNet):"오늘 순수익  "+Money(r.net),title);
   GUI.Label(new Rect(x+25,y+145,670,85),"누적 총수입 "+Money(e.TotalIncome)+" − 총지출 "+Money(e.TotalExpenses)+"\n누적 순수익 "+Money(e.TotalNet)+" · 현금 "+Money(e.Account.Cash)+" · 미납 "+Money(e.Account.Debt),text);
   string rows="";foreach(var day in e.Snapshot().records)rows+=day.day+"일차   수입 "+Money(day.income)+"   순수익 "+Money(day.net)+"   운송 "+day.rides+"건\n";
   GUI.Label(new Rect(x+25,y+245,670,120),rows,text);GUI.Label(new Rect(x+25,y+375,670,100),"오늘 운송 "+r.rides+"건 · 실패 "+r.failedTrips+"건 · 경과 "+Clock(r.elapsed)+"\n식사 "+d.meals+"회 / 수면 "+d.sleeps+"회 · 종료 포만감 "+d.satiety.ToString("0")+" · 피로 "+d.fatigue.ToString("0")+"\n운행 점수 "+s.law.ledger.TotalScore+"점 · "+b.saveStatus,small);
   if(!complete&&multi){GUI.Label(new Rect(x+25,y+490,670,60),"다음 날 수면 후 피로 "+System.Math.Max(0,d.fatigue-b.settings.nightlyFatigueRecovery).ToString("0")+" · 포만감 "+d.satiety.ToString("0")+"로 시작합니다.",text);GUI.enabled=!b.SaveFailed;if(GUI.Button(new Rect(x+25,y+590,325,55),(e.Day+1)+"일차 준비",button))b.BeginNextCampaignDay();GUI.enabled=true;}
   else {CompletedDay previous=null;for(int i=b.completedDays.Count-1;i>=0;i--){var candidate=b.completedDays[i];if(candidate.id!=s.law.ids.campaign&&candidate.days==e.Settings.days&&candidate.configuration==b.ComparisonVersion&&!candidate.configuration.EndsWith("mixed")){previous=candidate;break;}}
    GUI.Label(new Rect(x+25,y+490,670,50),previous==null?"같은 설정의 이전 완료 결과 없음":"이전 누적 순수익 "+Money(previous.totalNet)+" · 차이 "+Money(e.TotalNet-previous.totalNet),small);
    if(GUI.Button(new Rect(x+25,y+590,325,55),multi?"새 3일 캠페인":"새 하루 시작",button))b.RequestRestart();}
   if(GUI.Button(new Rect(x+370,y+590,325,55),"운행·승객 기록",button))s.law.OpenHistory();if(b.SaveFailed&&GUI.Button(new Rect(x+25,y+540,670,35),"저장 재시도",button))b.SaveCheckpoint();
  }
  GUI.matrix=saved;
 }
}
