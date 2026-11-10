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
 // Absent/null in pre-garage saves: migrated to the starter taxi.
 public VehicleState Vehicle;
}
// Actual cash movements and accrued debt are separate. Payment of debt never creates a second cost.
public class CampaignAccount {
 readonly PrototypeSettings settings;
 readonly List<AccountEntry> entries=new List<AccountEntry>();
 public int Cash {get;private set;}
 public int Debt {get;private set;}
 public float Fuel {get;private set;}
 public PersonalRoomState Room {get;private set;}
 public VehicleState Vehicle {get;private set;}
 public VehicleSpec VehicleSpec {get{return VehicleCatalog.Of(Vehicle);}}
 // Vehicle trade: one replacement per campaign, cash only, shares the personal-expense ledger.
 public bool BuyVehicle(string model,int day,int durability){
  if(day<1||day>settings.days||Room.finalized)return false;var q=VehicleCatalog.Quote(Vehicle,model,Cash,Debt,durability);if(!q.allowed)return false;
  if(!Post("vehicle:"+model,"personal-purchase",day,-q.spend,0))return false;
  // The dashboard ornament moves to the new car; a colour bought for the traded car stays on record.
  var old=Vehicle;Vehicle=new VehicleState{modelId=model,purchasedDay=day,paidVehicle=q.spend,ornamentOwned=old.ornamentOwned,ornamentPlaced=old.ornamentPlaced,paidOrnament=old.paidOrnament,tradedColorModel=old.altColorOwned?old.modelId:"",paidTradedColor=old.altColorOwned?old.paidColor:0};return true;
 }
 public bool BuyVehicleColor(int day){
  int price=VehicleCatalog.ColorPrice;if(day<1||day>settings.days||Room.finalized||Debt!=0||Vehicle.altColorOwned||Cash<price||price<=0)return false;
  if(!Post("vehicle-color:"+Vehicle.modelId,"personal-purchase",day,-price,0))return false;Vehicle.altColorOwned=true;Vehicle.color=1;Vehicle.paidColor=price;return true;
 }
 public bool BuyVehicleOrnament(int day){
  int price=VehicleCatalog.OrnamentPrice;if(day<1||day>settings.days||Room.finalized||Debt!=0||Vehicle.ornamentOwned||Cash<price||price<=0)return false;
  if(!Post("vehicle-ornament","personal-purchase",day,-price,0))return false;Vehicle.ornamentOwned=true;Vehicle.ornamentPlaced=true;Vehicle.paidOrnament=price;return true;
 }
 public bool SelectVehicleColor(int color){if(Room.finalized||color<0||color>1||color==1&&!Vehicle.altColorOwned)return false;Vehicle.color=color;return true;}
 public bool ToggleOrnament(){if(Room.finalized||!Vehicle.ornamentOwned)return false;Vehicle.ornamentPlaced=!Vehicle.ornamentPlaced;return true;}
 public int PersonalSpent {get{int total=0;foreach(var e in entries)if(e.kind=="personal-purchase")total-=e.cash;return total;}}
 public int PersonalExpense(int day){int total=0;foreach(var e in entries)if(e.kind=="personal-purchase"&&e.day==day)total-=e.cash;return total;}
 public int Net {get{return Cash-settings.startCash-Debt+PersonalSpent;}}
 public static int RoomPrice(string item){return item=="lamp"?5000:item=="rug"?8000:0;}
 public bool BuyRoomItem(string item,int style,int day){
  int price=RoomPrice(item);if(price==0||day<1||day>settings.days||style<0||style>1||Debt!=0||Room.finalized||Cash<price||item=="lamp"&&Room.lampOwned||item=="rug"&&Room.rugOwned)return false;
  if(!Post("room:"+item,"personal-purchase",day,-price,0))return false;
  if(item=="lamp"){Room.lampOwned=true;Room.lampPlaced=true;Room.lampOn=true;Room.lampStyle=style;}else{Room.rugOwned=true;Room.rugPlaced=true;Room.rugStyle=style;}
  if(Room.wished==item)Room.wished="";return true;
 }
 public bool Renewed {get{return HasKind("insurance");}}
 public bool TaxPaid {get{return HasKind("tax-payment");}}
 public bool GoalMet {get{return Net>=settings.target;}}
 public int PendingDayCost(int day){return (settings.oneDay||Ate(day)?0:settings.meal)+(!Has("parking:"+day)?settings.parking:0);}
 public void SetDaySeconds(float seconds){settings.daySeconds=seconds;}
 public CampaignAccount(PrototypeSettings s){settings=s;Cash=s.startCash;Fuel=s.initialFuel;Room=new PersonalRoomState();Vehicle=new VehicleState();}
 bool Has(string id){return entries.Exists(e=>e.id==id);}
 bool HasKind(string kind){return entries.Exists(e=>e.kind==kind);}
 public bool Ate(int day){return entries.Exists(e=>e.kind=="meal"&&e.day==day);}
 public bool InsuranceValid(int day){return true;}
 bool Post(string id,string kind,int day,int cash,int debt){
  if(string.IsNullOrEmpty(id)||Has(id)||Cash+cash<0||Debt+debt<0)return false;
  entries.Add(new AccountEntry{id=id,kind=kind,day=day,cash=cash,debt=debt});Cash+=cash;Debt+=debt;return true;
 }
 public bool Fare(string trip,int amount,int day=1){return amount>0&&Post("fare:"+trip,"fare",day,amount,0);}
 public bool Refuel(string id,float quantity,int day=1){
  if(float.IsNaN(quantity)||float.IsInfinity(quantity)||quantity<=0)return false;
  float actual=Math.Min(quantity,100-Fuel);int cost=(int)Math.Ceiling(actual*settings.fuelUnitPrice-.00001);
  if(actual<=0||!Post(id,"fuel",day,-cost,0))return false;Fuel+=actual;return true;
 }
 public void Consume(double seconds,double metres){
  if(double.IsNaN(seconds)||double.IsInfinity(seconds)||double.IsNaN(metres)||double.IsInfinity(metres)||seconds<0||metres<0)return;
  // Starter factor is exactly 1, preserving the pre-vehicle consumption.
  double factor=VehicleSpec.fuelFactor;
  Fuel=Math.Max(0,Fuel-(float)((metres/1000*settings.fuelPerKm+seconds/60*settings.idlePerMinute)*factor));
 }
 public bool Meal(string id,int day){if(settings.oneDay){if(string.IsNullOrEmpty(id)||Has(id))return false;Mandatory(id,"meal",day,settings.meal);return true;}return !Ate(day)&&Post(id,"meal",day,-settings.meal,0);}
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
 void Mandatory(string id,string kind,int day,int cost){int paid=Math.Min(Cash,cost);Post(id,kind,day,-paid,cost-paid);}
 public bool RecoverFuel(string id,int day){if(Fuel>0||Has(id))return false;Mandatory(id,"fuel-recovery",day,1800);Fuel=20;return true;}
 public bool CloseDay(int day){
  string id="settlement:"+day;if(Has(id))return false;
  if(!settings.oneDay&&!Ate(day))Mandatory("meal-fallback:"+day,"meal",day,settings.meal);
  Mandatory("parking:"+day,"parking",day,settings.parking);
  return Post(id,"settlement",day,0,0);
 }
 public int Expense(int day,string kind){int cost=0;foreach(var e in entries)if(e.day==day&&(kind==null||e.kind==kind)&&e.kind!="fare"&&e.kind!="personal-purchase")cost+=e.debt-e.cash;return cost;}
 public AccountEntry[] Entries {get{return entries.ConvertAll(e=>e.Copy()).ToArray();}}
 public AccountSnapshot Snapshot(){return new AccountSnapshot{settings=settings,fuel=Fuel,entries=Entries,Room=Room.Copy(),Vehicle=Vehicle.Copy()};}
 static bool ExcludedCost(string kind){return kind=="insurance"||kind=="tax-due"||kind=="tax-payment";}
 public static CampaignAccount Restore(AccountSnapshot snapshot){
  if(snapshot==null||(snapshot.version!=2&&snapshot.version!=3)||snapshot.settings==null||snapshot.entries==null||float.IsNaN(snapshot.fuel)||snapshot.fuel<0||snapshot.fuel>100)throw new ArgumentException("Invalid account checkpoint");
  // Validate the original sequence first: removing old costs must not make a corrupt
  // checkpoint look solvent or hide duplicate transaction identities.
  var checkedAccount=new CampaignAccount(snapshot.settings);
  foreach(var e in snapshot.entries)if(e==null||e.day<1||!checkedAccount.Post(e.id,e.kind,e.day,e.cash,e.debt))throw new ArgumentException("Invalid transaction");
  var a=new CampaignAccount(snapshot.settings.WithCurrentOperationBalance());
  foreach(var e in snapshot.entries)if(!ExcludedCost(e.kind)&&!a.Post(e.id,e.kind,e.day,e.cash,e.debt))throw new ArgumentException("Invalid migrated transaction");
  // No refund transaction is added. Normalized snapshots no longer contain the
  // removed costs, so later loads cannot refund or forgive them again.
  a.Room=snapshot.Room==null?new PersonalRoomState():snapshot.Room.Copy();
  if(a.Room.wished==null)a.Room.wished="";
  if(a.Room.lampStyle<0||a.Room.lampStyle>1||a.Room.rugStyle<0||a.Room.rugStyle>1||a.Room.wished!=""&&a.Room.wished!="lamp"&&a.Room.wished!="rug"||a.Room.lampPlaced&&!a.Room.lampOwned||a.Room.rugPlaced&&!a.Room.rugOwned||a.Room.lampOn&&!a.Room.lampOwned)throw new ArgumentException("Invalid room placement");
  // Old saves (no vehicle member, or JsonUtility's materialized blank) migrate to the starter taxi.
  var v=snapshot.Vehicle==null?new VehicleState():snapshot.Vehicle.Copy();if(string.IsNullOrEmpty(v.modelId))v.modelId=VehicleCatalog.Starter;a.Vehicle=v;
  if(VehicleCatalog.Get(v.modelId)==null||v.color<0||v.color>1||v.color==1&&!v.altColorOwned||v.ornamentPlaced&&!v.ornamentOwned||v.paidVehicle<0||v.paidColor<0||v.paidOrnament<0)throw new ArgumentException("Invalid vehicle state");
  if(v.tradedColorModel==null)v.tradedColorModel="";
  if(v.tradedColorModel.Length>0&&(v.tradedColorModel!=VehicleCatalog.Starter||v.modelId==VehicleCatalog.Starter||v.paidTradedColor<=0)||v.tradedColorModel.Length==0&&v.paidTradedColor!=0)throw new ArgumentException("Invalid traded vehicle colour");
  bool lamp=false,rug=false,vehicle=false,color=false,ornament=false,traded=false;
  foreach(var e in a.entries)if(e.kind=="personal-purchase"){
   if(e.debt!=0||e.day>a.settings.days)throw new ArgumentException("Invalid personal transaction");
   if(e.id=="room:lamp"&&e.cash==-5000&&!lamp)lamp=true;
   else if(e.id=="room:rug"&&e.cash==-8000&&!rug)rug=true;
   else if(e.id=="vehicle:"+v.modelId&&VehicleCatalog.Purchasable(v.modelId)&&e.cash==-v.paidVehicle&&e.day==v.purchasedDay&&!vehicle)vehicle=true;
   else if(e.id=="vehicle-color:"+v.modelId&&e.cash==-v.paidColor&&e.cash<0&&!color)color=true;
   else if(v.tradedColorModel.Length>0&&e.id=="vehicle-color:"+v.tradedColorModel&&e.cash==-v.paidTradedColor&&!traded)traded=true;
   else if(e.id=="vehicle-ornament"&&e.cash==-v.paidOrnament&&e.cash<0&&!ornament)ornament=true;
   else throw new ArgumentException("Invalid personal purchase");
  }
  if(a.Room.lampOwned!=lamp||a.Room.rugOwned!=rug)throw new ArgumentException("Room ownership does not match purchases");
  if(vehicle!=(v.modelId!=VehicleCatalog.Starter)||color!=v.altColorOwned||ornament!=v.ornamentOwned||traded!=(v.tradedColorModel.Length>0))throw new ArgumentException("Vehicle ownership does not match purchases");
  a.Fuel=snapshot.fuel;return a;
 }
}
