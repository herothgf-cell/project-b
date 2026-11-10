using UnityEngine;

// 내 공간 → 차고 tab (plan 22 §5). Same fixed 1200x720 frame and button rules as PersonalRoomHUD.
public static class VehicleGarageHUD {
 static GUIStyle text,small,title,button,bold;static string selected="lpg",confirm="";
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
 public static void Silhouette(Rect r,VehicleSpec v,int color,bool ornament){
  var c=v.colors[color==1?1:0];var paint=new Color(c[0],c[1],c[2]);float w=r.width*.86f*v.scaleZ,h=r.height*.36f*v.scaleY,x=r.center.x-w/2,y=r.yMax-h-r.height*.16f;
  CasualUI.RoundedFill(new Rect(x,y,w,h),paint);CasualUI.RoundedFill(new Rect(x+w*.2f,y-h*.62f,w*.55f,h*.72f),paint);
  CasualUI.RoundedFill(new Rect(x+w*.25f,y-h*.48f,w*.2f,h*.42f),new Color(.12f,.22f,.33f));CasualUI.RoundedFill(new Rect(x+w*.5f,y-h*.48f,w*.2f,h*.42f),new Color(.12f,.22f,.33f));
  CasualUI.RoundedFill(new Rect(x+w*.4f,y-h*.88f,w*.16f,h*.26f),new Color(1,.86f,.3f));
  float d=r.height*.25f;foreach(float at in new[]{.2f,.8f})CasualUI.RoundedFill(new Rect(x+w*at-d/2,y+h-d*.55f,d,d),new Color(.08f,.09f,.11f));
  if(ornament)CasualUI.RoundedFill(new Rect(x+w*.66f,y-h*.38f,h*.18f,h*.22f),new Color(.3f,.62f,.3f));
 }
 public static void Draw(DriveSession session){
  Init();var b=session.business;var a=b.ledger.Account;var owned=a.Vehicle;bool read=a.Room.finalized;
  CasualUI.Header(new Rect(15,15,830,55),read?"차고 · 완료 기록":"차고 · 내 택시",5);
  bool enabled=GUI.enabled;GUI.enabled=enabled&&confirm.Length==0;
  for(int i=0;i<VehicleCatalog.All.Length;i++){
   var v=VehicleCatalog.All[i];var r=new Rect(15+i*280,85,270,478);bool mine=owned.modelId==v.id;
   CasualUI.RoundedFill(r,mine?CasualUI.Soft:selected==v.id?CasualUI.Warm:CasualUI.Card);
   GUI.Label(new Rect(r.x+12,r.y+8,r.width-24,30),v.name+(mine?" · 내 차":""),title);
   Silhouette(new Rect(r.x+10,r.y+44,r.width-20,120),v,mine?owned.color:0,mine&&owned.ornamentOwned&&owned.ornamentPlaced);
   GUI.Label(new Rect(r.x+12,r.y+168,r.width-24,44),v.summary,small);
   var traits=Traits(v);for(int t=0;t<traits.Length;t++)GUI.Label(new Rect(r.x+12,r.y+214+t*34,r.width-24,34),traits[t],text);
   string price=v.id==VehicleCatalog.Starter?"시작 차량":"가격 "+Money(VehicleCatalog.Price(v.id));GUI.Label(new Rect(r.x+12,r.y+390,r.width-24,28),price,bold);
   if(!read&&!mine&&VehicleCatalog.Purchasable(v.id)&&GUI.Button(new Rect(r.x+12,r.y+425,r.width-24,40),selected==v.id?"견적 보는 중":"견적 보기",button))selected=v.id;
  }
  GUI.Label(new Rect(25,575,800,48),"차종마다 연료·트렁크·승차감·내구도·회전이 다릅니다. 운임과 속도는 같습니다.",text);
  GUI.Label(new Rect(25,628,800,52),"교체는 이번 3일 회차에 1번 · 현금만 · 다음 날부터 새 차로 운행 · 새 회차는 시작 차로 돌아갑니다. 산 외형·색·장식은 PVP '내 차'에 남습니다.",small);
  // Right panel: quote and decoration.
  CasualUI.Panel(new Rect(865,15,320,690));CasualUI.Header(new Rect(875,25,300,48),"견적·꾸미기",5);
  GUI.Label(new Rect(885,80,280,30),"현금 "+Money(a.Cash)+(a.Debt>0?" · 미납 "+Money(a.Debt):""),new GUIStyle(title){fontSize=20});
  int durability=b.ledger.Care.Damage.Durability;
  if(owned.modelId==VehicleCatalog.Starter&&!read){
   var q=b.ledger.QuoteVehicle(selected);var spec=VehicleCatalog.Get(selected);
   GUI.Label(new Rect(885,116,280,170),spec.name+"\n새 차 가격  "+Money(q.price)+"\n보상 판매  −"+Money(q.tradeIn)+"  (내구도 "+durability+")\n최종 지출  "+Money(q.spend)+"\n남는 현금  "+Money(q.cashAfter),text);
   if(!q.allowed)GUI.Label(new Rect(885,262,280,40),q.reason,new GUIStyle(small){normal={textColor=CasualUI.Danger}});
   bool can=GUI.enabled;GUI.enabled=can&&q.allowed;if(GUI.Button(new Rect(885,302,280,42),"이 차로 바꾸기",CasualUI.Action(17)))confirm=selected;GUI.enabled=can;
  }else GUI.Label(new Rect(885,116,280,120),owned.modelId==VehicleCatalog.Starter?"완료한 회차 기록입니다.":VehicleCatalog.Of(owned).name+" 보유 중\n"+(owned.purchasedDay<b.ledger.Settings.days?(owned.purchasedDay+1)+"일차부터 운행":"마지막 날 구매 · 소유만 기록")+"\n지출 "+Money(owned.paidVehicle),text);
  var mineSpec=VehicleCatalog.Of(owned);
  GUI.Label(new Rect(885,356,280,26),"색상",bold);
  for(int c=0;c<2;c++){var rgb=mineSpec.colors[c];var sw=new Rect(885+c*145,384,135,58);CasualUI.RoundedFill(sw,owned.color==c?CasualUI.Blue:CasualUI.Border);CasualUI.RoundedFill(new Rect(sw.x+4,sw.y+4,30,50),new Color(rgb[0],rgb[1],rgb[2]));
   bool have=c==0||owned.altColorOwned;string label=mineSpec.colorNames[c]+"\n"+(have?(owned.color==c?"사용 중":"바꾸기"):Money(VehicleCatalog.ColorPrice));
   if(!read&&GUI.Button(new Rect(sw.x+38,sw.y+4,sw.width-42,50),label,new GUIStyle(button){fontSize=13})){if(have)b.ChangeVehicle("color","",c);else b.ChangeVehicle("color-buy");}}
  GUI.Label(new Rect(885,452,280,26),"대시보드 장식 · 작은 화분",bold);
  if(!read){if(!owned.ornamentOwned){if(GUI.Button(new Rect(885,480,280,36),"구매 "+Money(VehicleCatalog.OrnamentPrice),button))b.ChangeVehicle("ornament-buy");}
   else if(GUI.Button(new Rect(885,480,280,36),owned.ornamentPlaced?"보관하기":"올려두기",button))b.ChangeVehicle("ornament");}
  else GUI.Label(new Rect(885,480,280,36),owned.ornamentOwned?"보유":"없음",text);
  GUI.Label(new Rect(885,522,280,64),b.SaveFailed?b.saveStatus:"차·꾸미기는 방 소품과 같은 개인 지출입니다. 영업 순수익과 따로 셉니다.",small);
  if(GUI.Button(new Rect(885,595,280,40),"정산으로 돌아가기",KoreanText.Button(16))){b.RoomOpen=false;b.GarageTab=false;b.ArchivedRoom=null;confirm="";}
  if(!read&&GUI.Button(new Rect(885,647,280,40),b.ledger.State==BusinessState.Completed?"이번 회차 마무리":"다음 날 준비",CasualUI.Action(18))){confirm="";b.GarageTab=false;if(b.ledger.State==BusinessState.Completed)b.ChangeRoom("finish");else b.BeginNextCampaignDay();}
  GUI.enabled=enabled;
  if(confirm.Length>0&&!read){
   var q=b.ledger.QuoteVehicle(confirm);var v=VehicleCatalog.Get(confirm);
   var shade=GUI.color;GUI.color=new Color(.07f,.15f,.29f,.42f);GUI.DrawTexture(new Rect(0,0,1200,720),Texture2D.whiteTexture);GUI.color=shade;
   CasualUI.Panel(new Rect(280,190,640,330));CasualUI.Header(new Rect(295,205,610,50),v.name+"로 바꿀까요?",5);
   GUI.Label(new Rect(305,266,590,140),"최종 지출 "+Money(q.spend)+" (새 차 "+Money(q.price)+" − 보상 판매 "+Money(q.tradeIn)+")\n남는 현금 "+Money(q.cashAfter)+" · 되돌릴 수 없음\n"+(b.ledger.State==BusinessState.Completed?"마지막 날이라 소유만 기록됩니다.":"다음 날부터 새 차로 운행합니다."),new GUIStyle(title){fontSize=20});
   GUI.enabled=q.allowed;if(GUI.Button(new Rect(305,420,280,50),"구매 확인",CasualUI.Action(18))){if(b.ChangeVehicle("buy",confirm))confirm="";}
   GUI.enabled=true;if(GUI.Button(new Rect(605,420,280,50),"취소",KoreanText.Button(16)))confirm="";
  }
 }
}
