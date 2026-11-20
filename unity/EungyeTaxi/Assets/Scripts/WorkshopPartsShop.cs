using System.Collections.Generic;
using UnityEngine;

public partial class ServiceStops {
 public bool WorkshopShopOpen {get;private set;}
 public bool WorkshopShopAvailable {get{return open&&pending<0&&session.business.SupportsCareer
  &&session.business.ledger.State==BusinessState.Open&&!session.FullMapOpen
  &&session.jobs.trip.State!=TripState.OnTrip&&!(session.damage&&session.damage.Disabled)
  &&!(session.accident&&session.accident.Locked)&&AtWorkshop();}}
 string workshopReceipt="";
 readonly Dictionary<string,Texture2D> workshopIcons=new Dictionary<string,Texture2D>();

 public bool OpenWorkshopShop(){
  if(!WorkshopShopAvailable)return false;
  workshopReceipt="";WorkshopShopOpen=true;return true;
 }
 void DrawWorkshopShop(GUIStyle text){
  var account=session.business.ledger.Account;
  CasualUI.Panel(new Rect(0,0,720,518));
  CasualUI.Header(new Rect(8,8,704,40),"공업사 차량 부품",12);
  GUI.Label(new Rect(23,54,675,32),VehicleCatalog.Of(account.ActiveVehicle).name+"  ·  현금 "+account.Cash.ToString("N0")+"원"
   +(account.Debt>0?"  ·  미납 "+account.Debt.ToString("N0")+"원":""),text);
  var heading=new GUIStyle(text){font=KoreanText.BoldFont,fontSize=18};
  var detail=new GUIStyle(text){fontSize=14};
  for(int i=0;i<WorkshopPart.All.Length;i++){
   var part=WorkshopPart.All[i];var fitted=account.PartVehicle(part);
   bool bought=account.OwnsPart(part);
   var action=!bought?WorkshopPartAction.Buy:fitted==account.ActiveVehicle?WorkshopPartAction.Remove:WorkshopPartAction.Equip;
   float y=90+84*i;
   CasualUI.RoundedFill(new Rect(15,y,690,78),CasualUI.Soft);
   Texture2D icon;
   if(!workshopIcons.TryGetValue(part.id,out icon))workshopIcons[part.id]=icon=Resources.Load<Texture2D>("WorkshopParts/"+part.id);
   if(icon)GUI.DrawTexture(new Rect(24,y+5,75,68),icon,ScaleMode.ScaleToFit,true);
   GUI.Label(new Rect(109,y+4,350,30),part.name,heading);
   GUI.Label(new Rect(109,y+35,353,35),part.description,detail);
   string status=!bought?"":fitted==null?"보관 중":VehicleCatalog.Of(fitted).name+" 장착 중";
   GUI.Label(new Rect(465,y+1,231,22),status,new GUIStyle(detail){alignment=TextAnchor.MiddleCenter});
   string caption=action==WorkshopPartAction.Buy?part.price.ToString("N0")+"원 구매":
    action==WorkshopPartAction.Remove?"분리하여 보관":fitted==null?"현재 차량에 장착":"현재 차량으로 옮겨 장착";
   bool canPay=bought||account.Debt==0&&account.Cash>=part.price;
   GUI.enabled=canPay;
   if(!canPay)caption=account.Debt!=0?"미납 정리 후 구매":(part.price-account.Cash).ToString("N0")+"원 부족";
   if(GUI.Button(new Rect(468,y+24,222,43),caption,CasualUI.Action(15))){
    bool changed=session.business.ChangeWorkshopPart(part.id,action);
    workshopReceipt=changed?part.name+(action==WorkshopPartAction.Buy?" 구매 · 보관함에 넣었습니다":action==WorkshopPartAction.Equip?" 장착 완료":" 분리 · 보관함에 넣었습니다")
     :"거래를 완료하지 못했습니다. 잔액과 저장 상태를 확인하세요.";
   }
   GUI.enabled=true;
  }
  GUI.Label(new Rect(22,429,674,31),workshopReceipt,detail);
  if(GUI.Button(new Rect(20,467,680,37),"공업사로 돌아가기",KoreanText.Button(15)))WorkshopShopOpen=false;
 }
}
