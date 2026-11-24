using System;
using System.Collections.Generic;

[Serializable] public class AccountEntry {
 public string id,kind;public int day,cash,debt;
 public AccountEntry Copy(){return (AccountEntry)MemberwiseClone();}
}
[Serializable] public class PersonalRoomState {
 public bool lampOwned,rugOwned,lampPlaced,rugPlaced,lampOn,finalized;
 public int lampStyle,rugStyle;
 public string wished="";
 public PersonalRoomState Copy(){return (PersonalRoomState)MemberwiseClone();}
}
[Serializable] public class AccountSnapshot {
 public int version=3;public PrototypeSettings settings;public float fuel;
 public AccountEntry[] entries;
 public PersonalRoomState Room;
 // Account versions 2–4 recorded only the current purchase.
 public VehicleState Vehicle;
 public VehicleState[] ownedVehicles;
 public string activeVehicleModel,pendingVehicleModel;
 public string careerGoal,careerVehicleGoal;
}
// Actual cash movements and accrued debt are separate. Payment of debt never creates a second cost.
public class CampaignAccount {
 readonly PrototypeSettings settings;
 public bool IsContinuousCareer {get{return settings.continuousCareer;}}
 public string CareerGoal {get;private set;}
 public string CareerVehicleGoal {get;private set;}
 public bool SetCareerGoal(string goal,string vehicle){
  if(goal!="none"&&goal!="home"&&goal!="car"||goal=="car"&&!VehicleCatalog.Purchasable(vehicle))return false;
  CareerGoal=goal;CareerVehicleGoal=goal=="car"?vehicle:"";return true;
 }
 public void ContinueCareer(){settings.continuousCareer=true;Room.finalized=false;}
 public int CashBeforeDay(int day){int cash=settings.startCash;foreach(var entry in entries)if(entry.day<day)cash+=entry.cash;return cash;}
 readonly List<AccountEntry> entries=new List<AccountEntry>();
 public int Cash {get;private set;}
 public int Debt {get;private set;}
 public float Fuel {get;private set;}
 public PersonalRoomState Room {get;private set;}
 // The replacement record stays stable when the player takes the starter back out.
 public VehicleState Vehicle {get;private set;}
 VehicleState fatherVehicle;
 string activeVehicleModel;
 public string PendingVehicleModel {get;private set;}
 public bool FatherCarGarage {get{return HasHomeBase&&settings.oneDay&&settings.days==3;}}
 public VehicleState OwnedVehicle(string model){
  if(model==Vehicle.modelId)return Vehicle;
  return model==VehicleCatalog.Starter?fatherVehicle:null;
 }
 public VehicleState ActiveVehicle {get{return OwnedVehicle(activeVehicleModel)??Vehicle;}}
 public VehicleSpec VehicleSpec {get{return VehicleCatalog.Of(ActiveVehicle);}}
 public double RideComfort {get{return VehicleSpec.comfortFactor*(WorkshopPart.Damper.Fitted(ActiveVehicle)?1.10:1);}}
 public bool OwnsPart(WorkshopPart part){return part!=null&&Has(part.receipt);}
 public VehicleState PartVehicle(WorkshopPart part){
  if(part==null)return null;
  return part.Fitted(Vehicle)?Vehicle:part.Fitted(fatherVehicle)?fatherVehicle:null;
 }
 public bool ChangeWorkshopPart(string id,WorkshopPartAction action,int day){
  var part=WorkshopPart.Get(id);
  if(part==null||!FatherCarGarage||day<1||day>settings.DayLimit||Has("settlement:"+day)||Room.finalized)return false;
  if(action==WorkshopPartAction.Buy)
   return Debt==0&&Cash>=part.price&&Post(part.receipt,"personal-purchase",day,-part.price,0);
  if(!OwnsPart(part))return false;
  var previous=PartVehicle(part);var current=ActiveVehicle;
  if(action==WorkshopPartAction.Remove){
   if(previous!=current)return false;
   current.equippedParts&=~part.bit;return true;
  }
  if(action!=WorkshopPartAction.Equip||previous==current)return false;
  if(previous!=null)previous.equippedParts&=~part.bit;
  current.equippedParts|=part.bit;return true;
 }
 public VehicleQuote QuoteVehicle(string model,int durability){return VehicleCatalog.Quote(Vehicle,model,Cash,Debt,durability,FatherCarGarage);}
 // A return to the active car cancels a pending selection without posting a transaction.
 public bool ScheduleVehicle(string model,int day){
  if(!FatherCarGarage||day<1||day>settings.DayLimit||!Has("settlement:"+day)||Room.finalized||OwnedVehicle(model)==null)return false;
  string next=model==ActiveVehicle.modelId?"":model;
  if(next==PendingVehicleModel)return false;
  PendingVehicleModel=next;return true;
 }
 internal bool ActivatePendingVehicle(){
  if(string.IsNullOrEmpty(PendingVehicleModel))return false;
  activeVehicleModel=PendingVehicleModel;PendingVehicleModel="";return true;
 }
 void RestoreFatherVehicle(){
  if(!FatherCarGarage||Vehicle.modelId==VehicleCatalog.Starter||fatherVehicle!=null)return;
  fatherVehicle=new VehicleState();
  if(Vehicle.tradedColorModel==VehicleCatalog.Starter){
   fatherVehicle.altColorOwned=true;fatherVehicle.paidColor=Vehicle.paidTradedColor;
   Vehicle.tradedColorModel="";Vehicle.paidTradedColor=0;
  }
 }
 public bool BuyVehicle(string model,int day,int durability){
  if(day<1||day>settings.DayLimit||Room.finalized||FatherCarGarage&&!Has("settlement:"+day))return false;
  var q=QuoteVehicle(model,durability);if(!q.allowed)return false;
  if(!Post("vehicle:"+model,"personal-purchase",day,-q.spend,0))return false;
  var old=Vehicle;
  Vehicle=new VehicleState{modelId=model,purchasedDay=day,paidVehicle=q.spend,ornamentOwned=old.ornamentOwned,ornamentPlaced=old.ornamentPlaced,paidOrnament=old.paidOrnament};
  if(FatherCarGarage){
   fatherVehicle=old;old.ornamentOwned=old.ornamentPlaced=false;old.paidOrnament=0;
   activeVehicleModel=old.modelId;PendingVehicleModel=model;
  }else{
   Vehicle.tradedColorModel=old.altColorOwned?old.modelId:"";Vehicle.paidTradedColor=old.altColorOwned?old.paidColor:0;
  }
  return true;
 }
 public bool BuyVehicleColor(int day,string model=""){
  var car=string.IsNullOrEmpty(model)?Vehicle:OwnedVehicle(model);
  int price=VehicleCatalog.ColorPrice;if(day<1||day>settings.DayLimit||Room.finalized||Debt!=0||car==null||car.altColorOwned||Cash<price||price<=0)return false;
  if(!Post("vehicle-color:"+car.modelId,"personal-purchase",day,-price,0))return false;car.altColorOwned=true;car.color=1;car.paidColor=price;return true;
 }
 public bool BuyVehicleOrnament(int day){
  int price=VehicleCatalog.OrnamentPrice;if(day<1||day>settings.DayLimit||Room.finalized||Debt!=0||Vehicle.ornamentOwned||Cash<price||price<=0)return false;
  if(!Post("vehicle-ornament","personal-purchase",day,-price,0))return false;Vehicle.ornamentOwned=true;Vehicle.ornamentPlaced=true;Vehicle.paidOrnament=price;return true;
 }
 public bool SelectVehicleColor(int color,string model=""){
  var car=string.IsNullOrEmpty(model)?Vehicle:OwnedVehicle(model);
  if(Room.finalized||car==null||color<0||color>1||color==1&&!car.altColorOwned)return false;car.color=color;return true;
 }
 public bool ToggleOrnament(){if(Room.finalized||!Vehicle.ornamentOwned)return false;Vehicle.ornamentPlaced=!Vehicle.ornamentPlaced;return true;}
 public int PersonalSpent {get{int total=0;foreach(var e in entries)if(e.kind=="personal-purchase")total-=e.cash;return total;}}
 public int PersonalExpense(int day){int total=0;foreach(var e in entries)if(e.kind=="personal-purchase"&&e.day==day)total-=e.cash;return total;}
 public int Net {get{return Cash-settings.startCash-Debt+PersonalSpent;}}
 public static int RoomPrice(string item){return item=="lamp"?5000:item=="rug"?8000:0;}
 public bool BuyRoomItem(string item,int style,int day){
  int price=RoomPrice(item);if(price==0||day<1||day>settings.DayLimit||style<0||style>1||Debt!=0||Room.finalized||Cash<price||item=="lamp"&&Room.lampOwned||item=="rug"&&Room.rugOwned)return false;
  if(!Post("room:"+item,"personal-purchase",day,-price,0))return false;
  if(item=="lamp"){Room.lampOwned=true;Room.lampPlaced=true;Room.lampOn=true;Room.lampStyle=style;}else{Room.rugOwned=true;Room.rugPlaced=true;Room.rugStyle=style;}
  if(Room.wished==item)Room.wished="";return true;
 }
 public bool Renewed {get{return HasKind("insurance");}}
 public bool TaxPaid {get{return HasKind("tax-payment");}}
 public bool GoalMet {get{return Net>=settings.target;}}
 public bool HasHomeBase {get{return settings.homeBase;}}
 public void UseHomeBase(){settings.homeBase=true;RestoreFatherVehicle();}
 int RentCost(int available){return HasHomeBase?Math.Min(Math.Max(0,available),ContentTuning.Int("home.rent.daily",0)):0;}
 public int PendingDayCost(int day){
  if(Has("settlement:"+day))return 0;
  int operatingCost=(settings.oneDay||Ate(day)?0:settings.meal)+(!Has("parking:"+day)?settings.parking:0);
  return operatingCost+RentCost(Cash-operatingCost);
 }
 public void SetDaySeconds(float seconds){settings.daySeconds=seconds;}
 public CampaignAccount(PrototypeSettings s){settings=s;Cash=s.startCash;Fuel=s.initialFuel;Room=new PersonalRoomState();Vehicle=new VehicleState();CareerGoal="none";CareerVehicleGoal="";PendingVehicleModel="";}
 bool Has(string id){return entries.Exists(e=>e.id==id);}
 bool HasKind(string kind){return entries.Exists(e=>e.kind==kind);}
 public bool Ate(int day){return entries.Exists(e=>e.kind=="meal"&&e.day==day);}
 public bool InsuranceValid(int day){return true;}
 bool Post(string id,string kind,int day,int cash,int debt){
  if(string.IsNullOrEmpty(id)||Has(id)||Cash+cash<0||Debt+debt<0)return false;
  entries.Add(new AccountEntry{id=id,kind=kind,day=day,cash=cash,debt=debt});Cash+=cash;Debt+=debt;return true;
 }
 public bool Fare(string trip,int amount,int day=1){return amount>0&&Post("fare:"+trip,"fare",day,amount,0);}
 public bool Tip(string trip,int amount,int day=1){return amount>0&&!string.IsNullOrEmpty(trip)&&Post("tip:"+trip,"tip",day,amount,0);}
 // Temptation reward (design 08 4.1): its own line, counted with tips (kind "tip") so day totals need no new row.
 public bool Hurry(string trip,int amount,int day=1){return amount>0&&!string.IsNullOrEmpty(trip)&&Post("hurry:"+trip,"tip",day,amount,0);}
 public bool Mission(string trip,int amount,int day=1){return amount>0&&!string.IsNullOrEmpty(trip)&&Post("mission:"+trip,"mission",day,amount,0);}
 public int Missions(int day){int total=0;foreach(var e in entries)if(e.kind=="mission"&&e.day==day)total+=e.cash;return total;}
 public int Tips(int day){int total=0;foreach(var e in entries)if(e.kind=="tip"&&e.day==day)total+=e.cash;return total;}
 public bool Refuel(string id,float quantity,int day=1){
  if(float.IsNaN(quantity)||float.IsInfinity(quantity)||quantity<=0)return false;
  float actual=Math.Min(quantity,100-Fuel);int cost=(int)Math.Ceiling(actual*settings.fuelUnitPrice-.00001);
  if(actual<=0||!Post(id,"fuel",day,-cost,0))return false;Fuel+=actual;return true;
 }
 public float FuelRequired(double seconds,double metres){
  double distanceFuel=metres/1000*settings.fuelPerKm;
  if(WorkshopPart.Filter.Fitted(ActiveVehicle))distanceFuel*=.9;
  return (float)((distanceFuel+seconds/60*settings.idlePerMinute)*VehicleSpec.fuelFactor);
 }
 public void Consume(double seconds,double metres){
  if(double.IsNaN(seconds)||double.IsInfinity(seconds)||double.IsNaN(metres)||double.IsInfinity(metres)||seconds<0||metres<0)return;
  Fuel=Math.Max(0,Fuel-FuelRequired(seconds,metres));
 }
 public bool Meal(string id,int day){if(settings.oneDay){if(string.IsNullOrEmpty(id)||Has(id))return false;Mandatory(id,"meal",day,settings.meal);return true;}return !Ate(day)&&Post(id,"meal",day,-settings.meal,0);}
 internal bool BuyRefreshment(string id,int day,int price){return day>=1&&price>0&&Post(id,"meal",day,-price,0);}
 // PVP purchases keep the native meal expense kind and require actual cash.
 public bool PvpRest(string id,int day){return settings.oneDay&&day>=1&&Post(id,"rest",day,-PvpConditionRules.RestCost,0);}
 public bool PvpMeal(string id,int day){return settings.oneDay&&day>=1&&Post(id,"meal",day,-PvpConditionRules.MealCost,0);}
 public bool Wash(string id,int day){if(day<1||string.IsNullOrEmpty(id)||Has(id))return false;Mandatory(id,"wash",day,2000);return true;}
 public bool Repair(string id,int day,int lost){if(day<1||lost<1||lost>100||string.IsNullOrEmpty(id)||Has(id))return false;Mandatory(id,"repair",day,lost*200);return true;}
 public bool Tow(string id,int day){if(day<1||string.IsNullOrEmpty(id)||Has(id))return false;Mandatory(id,"tow",day,3000);return true;}
 public bool Accident(string id,int day){return Accident(id,day,3000);}
 public bool Accident(string id,int day,int fee){if(day<1||fee<=0||string.IsNullOrWhiteSpace(id)||Has("accident:"+id))return false;Mandatory("accident:"+id,"accident",day,fee);return true;}
 // Completion persists independently of per-day service identities, including across day changes.
 public bool CompleteAccident(string id,int day){return !string.IsNullOrWhiteSpace(id)&&entries.Exists(e=>e.id=="accident:"+id&&e.kind=="accident"&&e.day==day)&&Post("accident-complete:"+id,"accident-completion",day,0,0);}
 // Compatibility entry points cannot reactivate excluded prototype features.
 public bool Renew(string id,int day=1){return false;}
 public bool PayTax(string id,int day=1){return false;}
 public bool Repay(string id,int amount,int day){
  return amount>0&&amount<=Debt&&Post(id,"debt-payment",day,-amount,-amount);
 }
 // Speed camera fine (2026-11-24): paid from cash, any shortfall as unpaid like other mandatory costs. Once per id.
 public bool Fine(string id,int amount,int day=1){if(amount<=0||string.IsNullOrEmpty(id)||Has(id))return false;Mandatory(id,"fine",day,amount);return Has(id);}
 void Mandatory(string id,string kind,int day,int cost){int paid=Math.Min(Cash,cost);Post(id,kind,day,-paid,cost-paid);}
 public bool RecoverFuel(string id,int day){if(Fuel>0||Has(id))return false;Mandatory(id,"fuel-recovery",day,1800);Fuel=20;return true;}
 public bool CloseDay(int day){
  string id="settlement:"+day;if(Has(id))return false;
  if(!settings.oneDay&&!Ate(day))Mandatory("meal-fallback:"+day,"meal",day,settings.meal);
  Mandatory("parking:"+day,"parking",day,settings.parking);
  if(HasHomeBase)Post("home-rent:"+day,"home-rent",day,-RentCost(Cash),0);
  return Post(id,"settlement",day,0,0);
 }
 public int Expense(int day,string kind){int cost=0;foreach(var e in entries)if(e.day==day&&(kind==null||e.kind==kind)&&e.kind!="fare"&&e.kind!="personal-purchase")cost+=e.debt-e.cash;return cost;}
 public AccountEntry[] Entries {get{return entries.ConvertAll(e=>e.Copy()).ToArray();}}
 public AccountSnapshot Snapshot(){
  var snapshot=new AccountSnapshot{version=HasHomeBase?4:3,settings=settings,fuel=Fuel,entries=Entries,Room=Room.Copy(),careerGoal=CareerGoal,careerVehicleGoal=CareerVehicleGoal};
  if(!FatherCarGarage){snapshot.Vehicle=Vehicle.Copy();return snapshot;}
  snapshot.version=5;
  snapshot.ownedVehicles=fatherVehicle==null?new[]{Vehicle.Copy()}:new[]{fatherVehicle.Copy(),Vehicle.Copy()};
  snapshot.activeVehicleModel=ActiveVehicle.modelId;snapshot.pendingVehicleModel=PendingVehicleModel;
  return snapshot;
 }
 static bool ExcludedCost(string kind){return kind=="insurance"||kind=="tax-due"||kind=="tax-payment";}
 static bool ValidVehicle(VehicleState v){
  return v!=null&&VehicleCatalog.Get(v.modelId)!=null&&v.color>=0&&v.color<=1&&(v.color==0||v.altColorOwned)&&(!v.ornamentPlaced||v.ornamentOwned)&&v.paidVehicle>=0&&v.paidColor>=0&&v.paidOrnament>=0;
 }
 void RestoreOwnedVehicles(AccountSnapshot snapshot){
  if(!FatherCarGarage||snapshot.ownedVehicles==null||snapshot.ownedVehicles.Length<1||snapshot.ownedVehicles.Length>2)throw new ArgumentException("Invalid garage ownership");
  VehicleState starter=null,replacement=null;
  foreach(var car in snapshot.ownedVehicles){
   if(!ValidVehicle(car)||!string.IsNullOrEmpty(car.tradedColorModel)||car.paidTradedColor!=0)throw new ArgumentException("Invalid garage vehicle");
   if(car.modelId==VehicleCatalog.Starter&&starter==null&&car.purchasedDay==0&&car.paidVehicle==0)starter=car.Copy();
   else if(VehicleCatalog.Purchasable(car.modelId)&&replacement==null)replacement=car.Copy();
   else throw new ArgumentException("Invalid garage models");
  }
  if(starter==null||replacement!=null&&(starter.ornamentOwned||starter.paidOrnament!=0))throw new ArgumentException("Invalid starter ownership");
  Vehicle=replacement??starter;fatherVehicle=replacement!=null?starter:null;
  activeVehicleModel=snapshot.activeVehicleModel;PendingVehicleModel=snapshot.pendingVehicleModel??"";
  if(OwnedVehicle(activeVehicleModel)==null||PendingVehicleModel.Length>0&&(PendingVehicleModel==activeVehicleModel||OwnedVehicle(PendingVehicleModel)==null))throw new ArgumentException("Invalid garage selection");
 }
 public static CampaignAccount Restore(AccountSnapshot snapshot){
  if(snapshot==null||snapshot.version<2||snapshot.version>5||snapshot.settings==null||snapshot.entries==null||float.IsNaN(snapshot.fuel)||snapshot.fuel<0||snapshot.fuel>100)throw new ArgumentException("Invalid account checkpoint");
  // Validate the original sequence first: removing old costs must not make a corrupt
  // checkpoint look solvent or hide duplicate transaction identities.
  var checkedAccount=new CampaignAccount(snapshot.settings);
  foreach(var e in snapshot.entries){
   if(e==null||e.day<1||!checkedAccount.Post(e.id,e.kind,e.day,e.cash,e.debt))throw new ArgumentException("Invalid transaction");
   if(e.kind=="home-rent"&&(snapshot.version<4||!snapshot.settings.homeBase||e.id!="home-rent:"+e.day||e.debt!=0||e.cash>0))throw new ArgumentException("Invalid home rent transaction");
  }
  var a=new CampaignAccount(snapshot.settings.WithCurrentOperationBalance());
  foreach(var e in snapshot.entries)if(!ExcludedCost(e.kind)&&!a.Post(e.id,e.kind,e.day,e.cash,e.debt))throw new ArgumentException("Invalid migrated transaction");
  // No refund transaction is added. Normalized snapshots no longer contain the
  // removed costs, so later loads cannot refund or forgive them again.
  a.Room=snapshot.Room==null?new PersonalRoomState():snapshot.Room.Copy();
  if(a.Room.wished==null)a.Room.wished="";
  if(!a.SetCareerGoal(string.IsNullOrEmpty(snapshot.careerGoal)?"none":snapshot.careerGoal,snapshot.careerVehicleGoal))throw new ArgumentException("Invalid career goal");
  if(a.IsContinuousCareer&&(!a.settings.oneDay||a.settings.days!=3||a.Room.finalized))throw new ArgumentException("Invalid continuous career");
  if(a.Room.lampStyle<0||a.Room.lampStyle>1||a.Room.rugStyle<0||a.Room.rugStyle>1||a.Room.wished!=""&&a.Room.wished!="lamp"&&a.Room.wished!="rug"||a.Room.lampPlaced&&!a.Room.lampOwned||a.Room.rugPlaced&&!a.Room.rugOwned||a.Room.lampOn&&!a.Room.lampOwned)throw new ArgumentException("Invalid room placement");
  // Old saves (no vehicle member, or JsonUtility's materialized blank) migrate to the starter taxi.
  if(snapshot.version==5)a.RestoreOwnedVehicles(snapshot);
  else{
   var old=snapshot.Vehicle==null?new VehicleState():snapshot.Vehicle.Copy();if(string.IsNullOrEmpty(old.modelId))old.modelId=VehicleCatalog.Starter;
   a.Vehicle=old;
  }
  var v=a.Vehicle;var father=a.fatherVehicle;
  if(!ValidVehicle(v))throw new ArgumentException("Invalid vehicle state");
  if(v.tradedColorModel==null)v.tradedColorModel="";
  if(v.tradedColorModel.Length>0&&(v.tradedColorModel!=VehicleCatalog.Starter||v.modelId==VehicleCatalog.Starter||v.paidTradedColor<=0)||v.tradedColorModel.Length==0&&v.paidTradedColor!=0)throw new ArgumentException("Invalid traded vehicle colour");
  bool lamp=false,rug=false,vehicle=false,color=false,ornament=false,traded=false,fatherColor=false;
  int purchasedParts=0;
  foreach(var e in a.entries)if(e.kind=="personal-purchase"){
   if(e.debt!=0||e.day>a.settings.DayLimit)throw new ArgumentException("Invalid personal transaction");
   if(e.id=="room:lamp"&&e.cash==-5000&&!lamp)lamp=true;
   else if(e.id=="room:rug"&&e.cash==-8000&&!rug)rug=true;
   else if(e.id=="vehicle:"+v.modelId&&VehicleCatalog.Purchasable(v.modelId)&&e.cash==-v.paidVehicle&&e.day==v.purchasedDay&&!vehicle)vehicle=true;
   else if(e.id=="vehicle-color:"+v.modelId&&e.cash==-v.paidColor&&e.cash<0&&!color)color=true;
   else if(v.tradedColorModel.Length>0&&e.id=="vehicle-color:"+v.tradedColorModel&&e.cash==-v.paidTradedColor&&!traded)traded=true;
   else if(father!=null&&e.id=="vehicle-color:"+father.modelId&&e.cash==-father.paidColor&&e.cash<0&&!fatherColor)fatherColor=true;
   else if(e.id=="vehicle-ornament"&&e.cash==-v.paidOrnament&&e.cash<0&&!ornament)ornament=true;
   else if(e.id.StartsWith("workshop-part:",StringComparison.Ordinal)){
    var part=Array.Find(WorkshopPart.All,p=>p.receipt==e.id);
    if(snapshot.version!=5||part==null||e.cash!=-part.price)throw new ArgumentException("Invalid workshop part receipt");
    purchasedParts|=part.bit;
   }
   else throw new ArgumentException("Invalid personal purchase");
  }
  if(a.Room.lampOwned!=lamp||a.Room.rugOwned!=rug)throw new ArgumentException("Room ownership does not match purchases");
  int fatherParts=father==null?0:father.equippedParts;
  if(((v.equippedParts|fatherParts)&~purchasedParts)!=0||(v.equippedParts&fatherParts)!=0)
   throw new ArgumentException("Invalid equipped workshop parts");
  if(vehicle!=(v.modelId!=VehicleCatalog.Starter)||color!=v.altColorOwned||ornament!=v.ornamentOwned||traded!=(v.tradedColorModel.Length>0)||fatherColor!=(father!=null&&father.altColorOwned))throw new ArgumentException("Vehicle ownership does not match purchases");
  a.RestoreFatherVehicle();
  a.Fuel=snapshot.fuel;return a;
 }
}
