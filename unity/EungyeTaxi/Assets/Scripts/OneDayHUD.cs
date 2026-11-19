using UnityEngine;

// Modern daily operation UI, shared by the one-day fixture and the three-day prototype.
public static class OneDayHUD {
 static GUIStyle text,small,title,button,preparationNumber,preparationCaption,preparationDelta,preparationAmount,preparationAction;
 static bool showDayDetails;static Vector2 detailScroll;
 public static bool DayDetailsOpen {get{return showDayDetails;}}
 public static void ToggleDayDetails(){showDayDetails=!showDayDetails;}
 static void Styles(){if(text!=null)return;text=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=18,wordWrap=true};small=new GUIStyle(text){fontSize=14};title=new GUIStyle(text){fontSize=25,fontStyle=FontStyle.Bold};button=KoreanText.Button(16);}
 static string Money(int n){return n.ToString("N0")+"원";}
 static string Clock(double value){int n=Mathf.CeilToInt((float)System.Math.Max(0,value));return n/60+":"+(n%60).ToString("00");}
 static void Panel(Rect r){CasualUI.Panel(r);}
 static void PreparationStyles(){
  if(preparationNumber!=null)return;
  preparationNumber=new GUIStyle(title){fontSize=26,alignment=TextAnchor.MiddleCenter,wordWrap=false};
  preparationCaption=new GUIStyle(text){fontSize=17,alignment=TextAnchor.MiddleCenter,wordWrap=false};
  preparationDelta=new GUIStyle(small){fontSize=15,alignment=TextAnchor.MiddleCenter,wordWrap=false};
  preparationAmount=new GUIStyle(text){fontSize=17,alignment=TextAnchor.MiddleRight,wordWrap=false};
  preparationAction=CasualUI.Action(20);
 }
 // Single-mode shift length selector; locked once the day has started.
 public static void DrawShiftLength(Rect r,BusinessProgress b){
  var label=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=17,alignment=TextAnchor.MiddleLeft,wordWrap=false};label.normal.textColor=CasualUI.Ink;
  GUI.Label(new Rect(r.x,r.y,110,r.height),"영업 시간",label);
  var choices=PrototypeSettings.ShiftMinuteChoices;int current=b.ShiftMinutes;float w=(r.width-110-6*(choices.Length-1))/choices.Length;
  bool old=GUI.enabled;GUI.enabled=old&&b.CanChangeShiftLength;
  for(int i=0;i<choices.Length;i++){int m=choices[i];if(GUI.Button(new Rect(r.x+110+i*(w+6),r.y,w,r.height),m+"분",m==current?CasualUI.Action(16):KoreanText.Button(16))&&m!=current)b.SetShiftLength(m);}
  GUI.enabled=old;
 }
 static void DrawPreparation(DriveSession session){
  PreparationStyles();var view=DayPreparationPresentation.Create(session.business.ledger);bool career=session.business.SupportsCareer;bool beside=career&&Screen.width>=1100;float extra=career&&!beside?110:0;var bounds=DayPreparationPresentation.PanelBounds(Screen.width,Screen.height,career&&!beside);float scale=bounds.width/760f;
  var saved=GUI.matrix;var oldColor=GUI.color;GUI.depth=-150;GUI.color=new Color(0,0,0,.30f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=oldColor;
  GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,Vector3.one*scale);float x=bounds.x/scale,y=bounds.y/scale;Panel(new Rect(x,y,760,640+extra));
  CasualUI.Header(new Rect(x+20,y+16,720,54),view.title,4);
  GUI.Label(new Rect(x+30,y+72,700,26),"현재 상태",text);
  for(int i=0;i<view.stats.Length;i++){
   var stat=view.stats[i];var card=new Rect(x+30+i*178,y+108,166,135);CasualUI.RoundedFill(card,CasualUI.Soft);
   HudGlyph.Draw(new Rect(card.center.x-12,card.y+12,24,24),stat.glyph,CasualUI.Blue);
   GUI.Label(new Rect(card.x+6,card.y+39,card.width-12,26),stat.label,preparationCaption);
   preparationNumber.fontSize=26;while(preparationNumber.fontSize>18&&preparationNumber.CalcSize(new GUIContent(stat.valueText)).x>card.width-12)preparationNumber.fontSize--;
   GUI.Label(new Rect(card.x+6,card.y+65,card.width-12,34),stat.valueText,preparationNumber);
   Color delta=CasualUI.Muted;if(stat.hasDelta&&stat.delta!=0)delta=(stat.id=="fatigue"?stat.delta<0:stat.delta>0)?CasualUI.Green:CasualUI.Danger;
   preparationDelta.normal.textColor=delta;GUI.Label(new Rect(card.x+6,card.y+103,card.width-12,25),stat.deltaText,preparationDelta);
  }
  GUI.Label(new Rect(x+30,y+252,700,25),view.comparisonSource,small);
  GUI.Label(new Rect(x+30,y+296,350,32),"이용 비용",text);preparationAmount.normal.textColor=CasualUI.Muted;GUI.Label(new Rect(x+380,y+296,350,30),view.expenseSource,preparationAmount);
  CasualUI.RoundedFill(new Rect(x+30,y+338,700,126),CasualUI.Soft);
  for(int i=0;i<view.expenses.Length;i++){
   var row=view.expenses[i];int column=i<4?0:1,index=i<4?i:i-4;float rowX=x+44+column*350,rowY=y+347+index*28;
   GUI.Label(new Rect(rowX,rowY,180,27),row.label,text);preparationAmount.normal.textColor=CasualUI.Ink;GUI.Label(new Rect(rowX+172,rowY,144,27),Money(row.amount),preparationAmount);
  }
  if(session.business.ledger.Settings.days==3){var room=session.business.ledger.Account.Room;string chosen=room.wished=="lamp"?"찜한 탁상 조명":room.wished=="rug"?"찜한 작은 러그":room.lampOwned||room.rugOwned?"내 공간의 물건이 기다리고 있어요":"";if(chosen.Length>0)GUI.Label(new Rect(x+30,y+278,700,24),chosen,small);}
  CasualUI.Divider(new Rect(x+30,y+472,700,1));
  GUI.Label(new Rect(x+30,y+478,340,32),"오늘의 이용 비용",text);preparationAmount.normal.textColor=CasualUI.Ink;GUI.Label(new Rect(x+390,y+476,340,36),Money(view.totalExpense),new GUIStyle(preparationAmount){fontSize=26,fontStyle=FontStyle.Bold});
  if(career)DrawCareerGoal(beside?new Rect(x+780,y,Mathf.Min(320,Screen.width/scale-x-795),255):new Rect(x+30,y+522,700,100),session.business);
  DrawShiftLength(new Rect(x+30,y+522+extra,700,40),session.business);
  if(GUI.Button(new Rect(x+30,y+574+extra,700,50),"영업 시작",preparationAction))session.business.Depart();
  GUI.matrix=saved;
 }
 static string GoalName(CampaignAccount account){return account.CareerGoal=="car"?"아버지 차를, 내 차로":account.CareerGoal=="home"?"이 집을 내 집답게":"일단, 오늘을 버티자";}
 static int GoalPrice(CampaignAccount account){return account.CareerGoal=="car"?VehicleCatalog.Price(account.CareerVehicleGoal):account.CareerGoal=="home"?ContentTuning.Int("home.upgrade.price",0):0;}
 static bool OwnsGoalCar(CampaignAccount account){return account.CareerGoal=="car"&&account.Vehicle.modelId==account.CareerVehicleGoal;}
 static float GoalProgress(int cash,int price){return price<=0?0:Mathf.Clamp01((float)cash/price);}
 public static void DrawCareerGoal(Rect rect,BusinessProgress business){
  Styles();var ledger=business.ledger;var account=ledger.Account;int price=GoalPrice(account);
  bool side=rect.height>=200;
  CasualUI.Panel(rect);
  if(side)CasualUI.Header(new Rect(rect.x+7,rect.y+7,rect.width-14,38),"내 목표",5,CasualUI.Green);
  GUI.Label(new Rect(rect.x+12,rect.y+(side?52:5),rect.width-24,22),side?GoalName(account):"목표 · "+GoalName(account),small);
  if(price>0){
   bool achieved=OwnsGoalCar(account);int left=Mathf.Max(0,price-account.Cash);double median=ledger.RecentNetMedian();
   string target=account.CareerGoal=="car"?VehicleCatalog.Get(account.CareerVehicleGoal).name:"넓은 호실";
   string forecast=left>0&&median>0?"오늘 평균이면 약 "+System.Math.Ceiling(left/median)+"일":"";
   string balance=achieved?target+" · 보유 중":target+"까지 "+Money(left);
   GUI.Label(new Rect(rect.x+12,rect.y+(side?78:27),rect.width-24,22),!side&&forecast.Length>0?balance+" · "+forecast:balance,small);
   CasualUI.Bar(new Rect(rect.x+12,rect.y+(side?108:51),rect.width-24,side?14:8),achieved?1:GoalProgress(account.Cash,price),CasualUI.Green);
   if(side)GUI.Label(new Rect(rect.x+12,rect.y+130,rect.width-24,36),Money(account.Cash)+" / "+Money(price)+(forecast.Length==0?"":"\n"+forecast),small);
  }
  string[] names={"하이브리드 세단","넓은 LPG 택시","이 집을 내 집답게","일단 버티자"};
  string[] goals={"car","car","home","none"};string[] models={VehicleCatalog.Hybrid,VehicleCatalog.WideLpg,"",""};
  int columns=side?2:4;float width=(rect.width-20-(columns-1)*5)/columns;
  for(int i=0;i<names.Length;i++){
   var choice=new Rect(rect.x+10+(i%columns)*(width+5),rect.y+(side?174:65)+(i/columns)*33,width,27);
   if(GUI.Button(choice,names[i],KoreanText.Button(11)))business.ChooseCareerGoal(goals[i],models[i]);
  }
 }
 static void DrawCareerResult(Rect rect,DriveSession session){
  var business=session.business;var ledger=business.ledger;var account=ledger.Account;int price=GoalPrice(account);
  GUI.Label(new Rect(rect.x,rect.y,rect.width,20),"목표 · "+GoalName(account),small);
  if(price>0){
   bool achieved=OwnsGoalCar(account);
   float before=achieved&&account.Vehicle.purchasedDay<ledger.Day?1:GoalProgress(account.CashBeforeDay(ledger.Day),price);
   float after=achieved?1:GoalProgress(account.Cash,price);
   CasualUI.Bar(new Rect(rect.x,rect.y+23,rect.width-122,11),after,CasualUI.Green);
   if(before<after&&before>0)CasualUI.RoundedFill(new Rect(rect.x,rect.y+23,(rect.width-122)*before,11),CasualUI.Blue);
   GUI.Label(new Rect(rect.xMax-116,rect.y+16,116,26),Mathf.RoundToInt(before*100)+" → "+Mathf.RoundToInt(after*100)+"%",small);
  }
  float longest=0;foreach(var ride in business.passengers)if(ride.day==ledger.Day&&!ride.cancelledForFatigue&&ride.paid>0)longest=Mathf.Max(longest,ride.rideDistance);
  int lastViolation=0;foreach(var entry in session.law.ledger.Events)if(entry.HasViolation&&entry.day<=ledger.Day)lastViolation=Mathf.Max(lastViolation,entry.day);
  GUI.Label(new Rect(rect.x,rect.y+40,rect.width,62),"오늘의 기록 · 최장 운송 "+(longest>0?(longest/1000).ToString("0.0")+"km":"—")+"\n무위반 연속 "+(ledger.Day-lastViolation)+"일\n완료 운송 "+ledger.Last.rides+"건",small);
 }
 public static void Draw(DriveSession s){if(s.RideResultOpen)return;Styles();var b=s.business;var e=b.ledger;var d=e.Driver;if(d==null)return;bool multi=e.Settings.days>1;GUI.depth=-35;if(b.RoomOpen&&b.RoomAvailable){PersonalRoomHUD.Draw(s);return;}
  if(!b.preparing&&!b.ShowingSummary&&!b.restartPrompt&&!b.ResumeBlocked){
   return; // Live operation is composed by CockpitUI.

  }
  if(b.preparing&&!b.restartPrompt&&!b.ResumeBlocked){showDayDetails=false;if(DayPreparationPresentation.ShouldShow(e.Day,b.preparing,b.restartPrompt,b.ResumeBlocked))DrawPreparation(s);else s.GetComponent<DriveHUD>().DrawSingleMenu();return;}
  bool careerResult=b.SupportsCareer&&b.ShowingSummary&&!b.restartPrompt&&!b.ResumeBlocked;
  float extra=careerResult?105:0,height=680+extra;
  var saved=GUI.matrix;float scale=Mathf.Min(1,Screen.width/800f,Screen.height/(height+50));GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,Vector3.one*scale);float x=(Screen.width/scale-720)/2,y=(Screen.height/scale-height)/2;GUI.depth=-150;Panel(new Rect(x,y,720,height));
  if(b.restartPrompt&&b.ResumeBlocked){CasualUI.Header(new Rect(x+20,y+18,680,64),"원본 보관 후 새로 시작할까요?",1,CasualUI.Amber);GUI.Label(new Rect(x+25,y+115,670,210),"현재 저장을 이어갈 수 없어 영업을 멈췄습니다.\n새로 시작하면 현재 진행 대신 현금 10,000원 · 연료 60%의 1일차로 시작합니다.\n\n기존 원본과 백업은 저장 폴더 안에 별도로 보관합니다. 보관하지 못하면 새로 시작하지 않습니다.\n취소하면 기존 파일과 현재 상태를 유지합니다.",text);GUI.Label(new Rect(x+25,y+360,670,75),b.saveStatus,small);if(GUI.Button(new Rect(x+25,y+590,325,55),"원본 보관 후 새로 시작",CasualUI.Action(16)))b.ConfirmRestart(true);if(GUI.Button(new Rect(x+370,y+590,325,55),"돌아가기",button))b.ConfirmRestart(false);}
  else if(b.restartPrompt){CasualUI.Header(new Rect(x+20,y+18,680,64),b.SupportsCareer?"새 경력을 시작할까요?":multi?"새 캠페인을 시작할까요?":"새 하루를 시작할까요?",4);GUI.Label(new Rect(x+25,y+100,670,210),b.SupportsCareer?"현금 10,000원 · 연료 60% · 포만감 80 · 피로 10\n별도 슬롯에서 1일차와 운전 점수 100점부터 시작합니다.\n이전 경력은 저장된 경력 목록에서 다시 열 수 있습니다.":"현금 10,000원 · 연료 60% · 포만감 80 · 피로 10\n1일차와 운전 점수 100점부터 시작합니다.\n이전 완료 결과와 방 기록은 보존합니다. 새 회차의 물건·배치는 초기화됩니다. 취소하면 현재 진행을 유지합니다.",text);GUI.Label(new Rect(x+25,y+360,670,65),b.saveStatus,small);if(GUI.Button(new Rect(x+25,y+590,325,55),b.SupportsCareer?"새 경력":multi?"새 3일 시작":"새 하루 시작",CasualUI.Action(18)))b.ConfirmRestart(true);if(GUI.Button(new Rect(x+370,y+590,325,55),"취소",button))b.ConfirmRestart(false);}
  else if(b.ResumeBlocked){CasualUI.Header(new Rect(x+20,y+18,680,64),"저장 파일을 읽지 못했어요",1,CasualUI.Amber);GUI.Label(new Rect(x+25,y+110,670,160),"기존 진행은 파일에 보존되어 있으며 영업은 멈춰 있습니다.\n저장과 맞는 지도·버전에서 이어가거나, 원본을 별도 보관한 뒤 새 캠페인을 선택할 수 있습니다.\n새 캠페인은 다음 화면에서 확인한 뒤 시작합니다.",text);GUI.Label(new Rect(x+25,y+290,670,60),b.saveStatus,small);GUI.Label(new Rect(x+25,y+365,670,180),"복원 오류\n"+b.LoadFailureDetail,small);if(GUI.Button(new Rect(x+25,y+590,670,55),"복구 방법 보기",CasualUI.Action(18)))b.RequestRestart();}
  else {
   var r=e.Last;bool complete=e.State==BusinessState.Completed;
   CasualUI.Header(new Rect(x+20,y+16,680,54),complete&&multi?e.Settings.days+"일 영업을 마쳤어요!":e.Day+"일차 · 영업 마감",6,CasualUI.Green);
   GUI.Label(new Rect(x+28,y+80,180,28),complete&&multi?"누적 순수익":"오늘 순수익",small);
   var netStyle=new GUIStyle(title){fontSize=35,alignment=TextAnchor.MiddleRight};netStyle.normal.textColor=CasualUI.Green;
   GUI.Label(new Rect(x+215,y+76,480,48),Money(complete&&multi?e.TotalNet:r.net),netStyle);
   CasualUI.Panel(new Rect(x+25,y+134,670,82),CasualUI.Soft);
   GUI.Label(new Rect(x+42,y+142,320,66),"수입  "+Money(complete?e.TotalIncome:r.income)+"\n지출  "+Money(complete?e.TotalExpenses:r.income-r.net),text);
   GUI.Label(new Rect(x+375,y+142,300,66),"잔액  "+Money(e.Account.Cash)+"\n미납  "+Money(e.Account.Debt),text);
   GUI.Label(new Rect(x+25,y+234,670,30),"운송 "+r.rides+"건 · 미완료 "+r.failedTrips+"건 · 영업 "+Clock(r.elapsed),text);
   GUI.Label(new Rect(x+25,y+274,670,30),"운전 점수 "+s.law.ledger.TotalScore+" · 차량 청결도 "+e.Care.Cleanliness+" · 내구도 "+e.Care.Damage.Durability,text);
   GUI.Label(new Rect(x+25,y+310,670,28),"포만감 "+d.satiety.ToString("0")+" · 기사 피로도 "+d.fatigue.ToString("0"),text);
   if(GUI.Button(new Rect(x+25,y+352,670,36),showDayDetails?"상세 내역 닫기":"상세 내역 보기",button))ToggleDayDetails();
   if(showDayDetails){
    string rows="";foreach(var day in e.Snapshot().records)rows+=day.day+"일차 · 수입 "+Money(day.income)+" · 순수익 "+Money(day.net)+" · 운송 "+day.rides+"건\n";
    string content=rows+"\n"+OperationMetrics.Describe(r.metrics)+"\n세차 "+Money(r.wash)+" · 수리 "+Money(r.repair)+" · 견인 "+Money(r.tow)+"\n식사 "+d.meals+"회 · 수면 "+d.sleeps+"회\n누적 수입 "+Money(e.TotalIncome)+" · 지출 "+Money(e.TotalExpenses)+" · 순수익 "+Money(e.TotalNet)+"\n"+b.saveStatus;
    float ch=Mathf.Max(120,small.CalcHeight(new GUIContent(content),625)+12);
    detailScroll=GUI.BeginScrollView(new Rect(x+25,y+398,670,e.Account.HasHomeBase?65:80),detailScroll,new Rect(0,0,640,ch));GUI.Label(new Rect(0,0,625,ch),content,small);GUI.EndScrollView();
   }else if(!complete&&multi){
    GUI.Label(new Rect(x+25,y+407,670,65),"누적 순수익  "+Money(e.TotalNet)+"\n다음 날 · 피로 "+System.Math.Max(0,d.fatigue-b.settings.nightlyFatigueRecovery).ToString("0")+" · 포만감 "+d.satiety.ToString("0"),small);
   }else {
    CompletedDay previous=null;for(int i=b.completedDays.Count-1;i>=0;i--){var candidate=b.completedDays[i];if(candidate.id!=s.law.ids.campaign&&candidate.days==e.Settings.days&&candidate.configuration==b.ComparisonVersion&&!candidate.configuration.EndsWith("mixed")){previous=candidate;break;}}
    GUI.Label(new Rect(x+25,y+407,670,65),previous==null?"이전 완료 기록 없음":"이전 순수익 "+Money(previous.totalNet)+" · 차이 "+Money(e.TotalNet-previous.totalNet),small);
   }
   if(careerResult)DrawCareerResult(new Rect(x+25,y+466,670,100),s);
   if(e.Account.HasHomeBase)GUI.Label(new Rect(x+25,y+466+extra,670,24),HomeBasePresentation.RentLabel+"  −"+Money(e.Account.Expense(e.Day,"home-rent")),small);
   if(multi&&GUI.Button(new Rect(x+25,y+492+extra,325,40),e.Account.HasHomeBase?HomeBasePresentation.Name:"내 공간 · 방/차고",button)){b.ArchivedRoom=null;b.GarageTab=false;b.RoomOpen=true;}
   CompletedDay roomRecord=null;for(int ri=b.completedDays.Count-1;ri>=0;ri--)if(b.completedDays[ri].id!=s.law.ids.campaign&&b.completedDays[ri].room!=null){roomRecord=b.completedDays[ri];break;}if(multi&&roomRecord!=null&&GUI.Button(new Rect(x+370,y+492+extra,325,40),"이전 완료 방 보기",button)){b.ArchivedRoom=roomRecord.room;b.RoomOpen=true;}
   GUI.enabled=!b.SaveFailed;
   if(b.CanContinueCareer){
    if(GUI.Button(new Rect(x+370,y+590+extra,325,55),"계속하기",CasualUI.Action(18)))b.BeginContinuousCareer();
    if(GUI.Button(new Rect(x+370,y+540+extra,325,40),"새 경력",button))b.RequestRestart();
   }
   else if(!complete&&multi){if(GUI.Button(new Rect(x+370,y+590+extra,325,55),(e.Day+1)+"일차 준비",CasualUI.Action(18)))b.BeginNextCampaignDay();}
   else if(multi&&!e.Account.Room.finalized){if(GUI.Button(new Rect(x+370,y+590,325,55),"이번 회차 마무리",CasualUI.Action(18)))b.ChangeRoom("finish");}
   else if(GUI.Button(new Rect(x+370,y+590,325,55),multi?"새 캠페인":"새 하루",CasualUI.Action(18)))b.RequestRestart();
   GUI.enabled=true;
   if(GUI.Button(new Rect(x+25,y+590+extra,325,55),"운행 기록",button))s.law.OpenHistory();
   if(b.SaveFailed){GUI.Label(new Rect(x+25,y+540+extra,470,35),b.saveStatus,small);if(GUI.Button(new Rect(x+505,y+540+extra,190,35),"저장 재시도",button))b.SaveCheckpoint();}
  }
  GUI.matrix=saved;
 }
}
