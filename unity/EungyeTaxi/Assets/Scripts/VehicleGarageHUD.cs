using UnityEngine;

// 내 공간 → 차고 tab (plan 22 §5). Same fixed 1200x720 frame and button rules as PersonalRoomHUD.
public static class VehicleGarageHUD {
 static GUIStyle text,small,title,button,bold;static string selected="lpg",confirm="",paintModel="";
 public static bool Confirming {get{return confirm.Length>0;}}
 static string Money(int n){return n.ToString("N0")+"원";}
 static void Init(){if(text!=null)return;text=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=16,wordWrap=true};small=new GUIStyle(text){fontSize=13};bold=new GUIStyle(text){fontStyle=FontStyle.Bold};title=new GUIStyle(text){fontSize=23,fontStyle=FontStyle.Bold};button=KoreanText.Button(15);}
 public static string[] Traits(VehicleSpec v){
  return new[]{
   "연료 소모  "+(v.fuelFactor==1?"보통":(v.fuelFactor<1?"적음 ":"많음 ")+"("+System.Math.Round(v.fuelFactor*100)+"%)"),
   "트렁크  "+(v.carriesLuggage?(v.id=="lpg"?"큼 · 큰 짐 손님 가능":"보통 · 큰 짐 손님 가능"):"작음 · 큰 짐 손님 호출 안 받음"),
   "승차감  "+(v.comfortFactor>1?"좋음 · 급가감속 판정이 덜 민감":"보통"),
   "내구도 감소  "+(v.wearFactor<1?"느림 · 충돌 충격 "+System.Math.Round(v.wearFactor*100)+"%":"빠름(기본)"),
   "회전 반경  "+(v.steerFactor<1?"큼 · 최대 조향 "+System.Math.Round(v.steerFactor*100)+"%":"보통")
  };
 }
 // Side profile: proportions and paint follow the same spec as the 3D variant.
 public static void Silhouette(Rect r,VehicleSpec v,int color,bool ornament){VehicleIcon.Draw(r,v,color,ornament);}
 public static void Draw(DriveSession session){
  Init();var b=session.business;var a=b.ledger.Account;bool keep=a.FatherCarGarage,read=a.Room.finalized;
  var owned=a.OwnedVehicle(paintModel)??a.Vehicle;
  var active=a.ActiveVehicle;string scheduled=a.PendingVehicleModel;
  if(read)confirm="";
  CasualUI.Header(new Rect(15,15,830,55),read?"차고 · 완료 기록":"차고 · 내 택시",5);
  bool enabled=GUI.enabled;GUI.enabled=enabled&&confirm.Length==0;
  for(int i=0;i<VehicleCatalog.All.Length;i++){
   var v=VehicleCatalog.All[i];var r=new Rect(15+i*280,85,270,478);var car=a.OwnedVehicle(v.id);bool mine=car!=null;
   CasualUI.RoundedFill(r,mine?CasualUI.Soft:selected==v.id?CasualUI.Warm:CasualUI.Card);
   GUI.Label(new Rect(r.x+12,r.y+8,r.width-24,30),v.name+(mine?" · 내 차":""),title);
   Silhouette(new Rect(r.x+10,r.y+44,r.width-20,120),v,mine?car.color:0,mine&&car.ornamentOwned&&car.ornamentPlaced);
   GUI.Label(new Rect(r.x+12,r.y+168,r.width-24,44),v.summary+"\n"+VehicleFeel.Describe(v.id),small);
   var traits=Traits(v);for(int t=0;t<traits.Length;t++)GUI.Label(new Rect(r.x+12,r.y+214+t*34,r.width-24,34),traits[t],text);
   string price=mine&&keep?(scheduled==v.id?"다음 날 운행 예정":active.modelId==v.id?"오늘 운행 차량":"숙소 주차장 보관")
    :v.id==VehicleCatalog.Starter?"시작 차량":"가격 "+Money(VehicleCatalog.Price(v.id));
   GUI.Label(new Rect(r.x+12,r.y+390,r.width-24,28),price,bold);
   if(!read&&keep&&mine){
    if(GUI.Button(new Rect(r.x+12,r.y+425,94,40),owned==car?"꾸미는 중":"꾸미기",button))paintModel=v.id;
    bool can=GUI.enabled;GUI.enabled=can&&a.Vehicle.modelId!=VehicleCatalog.Starter&&scheduled!=v.id&&(active.modelId!=v.id||scheduled.Length>0);
    if(GUI.Button(new Rect(r.x+116,r.y+425,r.width-128,40),scheduled==v.id?"선택됨":"다음 날 운행",button))b.ChangeVehicle("select",v.id);
    GUI.enabled=can;
   }else if(!read&&!mine&&a.Vehicle.modelId==VehicleCatalog.Starter&&VehicleCatalog.Purchasable(v.id)&&GUI.Button(new Rect(r.x+12,r.y+425,r.width-24,40),selected==v.id?"견적 보는 중":"견적 보기",button))selected=v.id;
  }
  GUI.Label(new Rect(25,575,800,48),"차종마다 연료·트렁크·승차감·내구도·회전이 다릅니다. 운임과 속도는 같습니다.",text);
  GUI.Label(new Rect(25,628,800,52),keep?"새 차 구매 1회 · 숙소 주차장 보관 무료 · 선택한 차는 다음 날부터 운행합니다. 구매한 외형·색·장식은 PVP '내 차'에 남습니다."
    :b.ledger.IsContinuousCareer?"교체는 경력에서 1번 · 현금만 · 다음 날부터 새 차로 운행합니다. 산 외형·색·장식은 PVP '내 차'에 남습니다.":"교체는 이번 3일 회차에 1번 · 현금만 · 다음 날부터 새 차로 운행 · 새 회차는 시작 차로 돌아갑니다. 산 외형·색·장식은 PVP '내 차'에 남습니다.",small);
  // Right panel: quote and decoration.
  CasualUI.Panel(new Rect(865,15,320,690));CasualUI.Header(new Rect(875,25,300,48),"견적·꾸미기",5);
  GUI.Label(new Rect(885,80,280,30),"현금 "+Money(a.Cash)+(a.Debt>0?" · 미납 "+Money(a.Debt):""),new GUIStyle(title){fontSize=20});
  int durability=b.ledger.Care.Damage.Durability;
  if(a.Vehicle.modelId==VehicleCatalog.Starter&&!read){
   var q=b.ledger.QuoteVehicle(selected);var spec=VehicleCatalog.Get(selected);
   string trade=keep?"아버지의 택시는 숙소에 보관\n":"보상 판매  −"+Money(q.tradeIn)+"  (내구도 "+durability+")\n";
   GUI.Label(new Rect(885,116,280,170),spec.name+"\n새 차 가격  "+Money(q.price)+"\n"+trade+"최종 지출  "+Money(q.spend)+"\n남는 현금  "+Money(q.cashAfter),text);
   if(!q.allowed)GUI.Label(new Rect(885,262,280,40),q.reason,new GUIStyle(small){normal={textColor=CasualUI.Danger}});
   bool can=GUI.enabled;GUI.enabled=can&&q.allowed;if(GUI.Button(new Rect(885,302,280,42),"이 차로 바꾸기",CasualUI.Action(17)))confirm=selected;GUI.enabled=can;
  }else if(keep){
   string next=scheduled.Length==0?active.modelId:scheduled;
   GUI.Label(new Rect(885,116,280,120),"오늘 운행  "+VehicleCatalog.Of(active).name+"\n다음 날 운행  "+VehicleCatalog.Get(next).name+"\n새 차 지출  "+Money(a.Vehicle.paidVehicle),text);
  }else GUI.Label(new Rect(885,116,280,120),owned.modelId==VehicleCatalog.Starter?"완료한 회차 기록입니다.":VehicleCatalog.Of(owned).name+" 보유 중\n"+(owned.purchasedDay<b.ledger.DayLimit?(owned.purchasedDay+1)+"일차부터 운행":"마지막 날 구매 · 소유만 기록")+"\n지출 "+Money(owned.paidVehicle),text);
  var mineSpec=VehicleCatalog.Of(owned);
  GUI.Label(new Rect(885,356,280,26),keep?mineSpec.name+" · 색상":"색상",bold);
  for(int c=0;c<2;c++){var rgb=mineSpec.colors[c];var sw=new Rect(885+c*145,384,135,58);CasualUI.RoundedFill(sw,owned.color==c?CasualUI.Blue:CasualUI.Border);CasualUI.RoundedFill(new Rect(sw.x+4,sw.y+4,30,50),new Color(rgb[0],rgb[1],rgb[2]));
   bool have=c==0||owned.altColorOwned;string label=mineSpec.colorNames[c]+"\n"+(have?(owned.color==c?"사용 중":"바꾸기"):Money(VehicleCatalog.ColorPrice));
   if(!read&&GUI.Button(new Rect(sw.x+38,sw.y+4,sw.width-42,50),label,new GUIStyle(button){fontSize=13})){if(have)b.ChangeVehicle("color",owned.modelId,c);else b.ChangeVehicle("color-buy",owned.modelId);}}
  var ornamentCar=a.Vehicle;
  GUI.Label(new Rect(885,452,280,26),"대시보드 장식 · 작은 화분",bold);
  if(ornamentCar!=owned)GUI.Label(new Rect(885,480,280,36),VehicleCatalog.Of(ornamentCar).name+"에서 설정",small);
  else if(!read){if(!ornamentCar.ornamentOwned){if(GUI.Button(new Rect(885,480,280,36),"구매 "+Money(VehicleCatalog.OrnamentPrice),button))b.ChangeVehicle("ornament-buy");}
   else if(GUI.Button(new Rect(885,480,280,36),ornamentCar.ornamentPlaced?"보관하기":"올려두기",button))b.ChangeVehicle("ornament");}
  else GUI.Label(new Rect(885,480,280,36),ornamentCar.ornamentOwned?"보유":"없음",text);
  GUI.Label(new Rect(885,522,280,64),b.SaveFailed?b.saveStatus:"차·꾸미기는 방 소품과 같은 개인 지출입니다. 영업 순수익과 따로 셉니다.",small);
  if(GUI.Button(new Rect(885,595,280,40),"정산으로 돌아가기",KoreanText.Button(16))){b.RoomOpen=false;b.GarageTab=false;b.ArchivedRoom=null;confirm="";}
  if(!read&&GUI.Button(new Rect(885,647,280,40),b.ledger.State==BusinessState.Completed?"이번 회차 마무리":"다음 날 준비",CasualUI.Action(18))){confirm="";b.GarageTab=false;if(b.ledger.State==BusinessState.Completed)b.ChangeRoom("finish");else b.BeginNextCampaignDay();}
  GUI.enabled=enabled;
  if(confirm.Length>0&&!read){
   var q=b.ledger.QuoteVehicle(confirm);var v=VehicleCatalog.Get(confirm);
   var shade=GUI.color;GUI.color=new Color(.07f,.15f,.29f,.42f);GUI.DrawTexture(new Rect(0,0,1200,720),Texture2D.whiteTexture);GUI.color=shade;
   if(keep){DrawKeepConfirmation(b,a,q,v,enabled);return;}
   CasualUI.Panel(new Rect(280,190,640,330));CasualUI.Header(new Rect(295,205,610,50),v.name+"로 바꿀까요?",5);
   GUI.Label(new Rect(305,266,590,140),"최종 지출 "+Money(q.spend)+" (새 차 "+Money(q.price)+" − 보상 판매 "+Money(q.tradeIn)+")\n남는 현금 "+Money(q.cashAfter)+" · 되돌릴 수 없음\n"+(b.ledger.State==BusinessState.Completed?"마지막 날이라 소유만 기록됩니다.":"다음 날부터 새 차로 운행합니다."),new GUIStyle(title){fontSize=20});
   GUI.enabled=q.allowed;if(GUI.Button(new Rect(305,420,280,50),"구매 확인",CasualUI.Action(18))){if(b.ChangeVehicle("buy",confirm))confirm="";}
   GUI.enabled=enabled;if(GUI.Button(new Rect(605,420,280,50),"취소",KoreanText.Button(16)))confirm="";
  }
 }
 static void DrawKeepConfirmation(BusinessProgress b,CampaignAccount a,VehicleQuote q,VehicleSpec v,bool enabled){
  CasualUI.Panel(new Rect(290,176,620,370));
  CasualUI.Header(new Rect(300,186,600,47),"아버지의 택시는 숙소 주차장에 둘게요",5);
  var father=a.OwnedVehicle(VehicleCatalog.Starter);
  CasualUI.RoundedFill(new Rect(320,249,177,86),CasualUI.Soft);
  Silhouette(new Rect(326,253,165,76),VehicleCatalog.Standard,father.color,false);
  GUI.Label(new Rect(518,246,357,24),"판매하지 않고 보관합니다",bold);
  GUI.Label(new Rect(518,275,357,23),"보상 판매가 없어 새 차 값을 모두 냅니다",small);
  GUI.Label(new Rect(518,296,357,45),v.name+"  "+Money(q.spend)+"\n남는 현금  "+Money(q.cashAfter),text);
  CasualUI.RoundedFill(new Rect(320,352,560,87),CasualUI.Warm);
  GUI.Label(new Rect(334,359,534,72),"보관한 차는\n· 숙소 주차장에 늘 서 있어요\n· 차고에서 다시 고르면 다음 날부터 그 차로 영업",text);
  if(b.SaveFailed)GUI.Label(new Rect(320,440,560,31),b.saveStatus,small);
  GUI.enabled=enabled&&q.allowed;
  if(GUI.Button(new Rect(320,471,270,49),"이 차로 바꾸기",CasualUI.Action(18))&&b.ChangeVehicle("buy",v.id)){paintModel=v.id;confirm="";}
  GUI.enabled=enabled;
  if(GUI.Button(new Rect(610,471,270,49),"다시 생각하기",button))confirm="";
 }
}
