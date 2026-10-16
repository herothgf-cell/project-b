using System;
using System.Collections.Generic;

[Serializable] public class AccountEntry {
 public string id,kind;public int day,cash,debt;
 public AccountEntry Copy(){return (AccountEntry)MemberwiseClone();}
}
[Serializable] public class AccountSnapshot {
 public int version=2;public PrototypeSettings settings;public float fuel;
 public AccountEntry[] entries;
}
// Actual cash movements and accrued debt are separate. Payment of debt never creates a second cost.
public class CampaignAccount {
 readonly PrototypeSettings settings;
 readonly List<AccountEntry> entries=new List<AccountEntry>();
 public int Cash {get;private set;}
 public int Debt {get;private set;}
 public float Fuel {get;private set;}
 public int Net {get{return Cash-settings.startCash-Debt;}}
 public bool Renewed {get{return HasKind("insurance");}}
 public bool TaxPaid {get{return HasKind("tax-payment");}}
 public bool GoalMet {get{return Net>=settings.target&&Renewed&&TaxPaid;}}
 public int PendingDayCost(int day){return (Ate(day)?0:settings.meal)+(!Has("parking:"+day)?settings.parking:0)+(day>=settings.taxDueDay&&!TaxPaid&&!HasKind("tax-due")?settings.tax:0);}
 public CampaignAccount(PrototypeSettings s){settings=s;Cash=s.startCash;Fuel=s.initialFuel;}
 bool Has(string id){return entries.Exists(e=>e.id==id);}
 bool HasKind(string kind){return entries.Exists(e=>e.kind==kind);}
 public bool Ate(int day){return entries.Exists(e=>e.kind=="meal"&&e.day==day);}
 public bool InsuranceValid(int day){return day<=settings.insuranceExpiryDay||Renewed;}
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
  Fuel=Math.Max(0,Fuel-(float)(metres/1000*settings.fuelPerKm+seconds/60*settings.idlePerMinute));
 }
 public bool Meal(string id,int day){return !Ate(day)&&Post(id,"meal",day,-settings.meal,0);}
 public bool Renew(string id,int day=1){return !Renewed&&Post(id,"insurance",day,-settings.insurance,0);}
 public bool PayTax(string id,int day=1){return !TaxPaid&&Post(id,"tax-payment",day,-settings.tax,HasKind("tax-due")?-settings.tax:0);}
 public bool Repay(string id,int amount,int day){
  // Keep the specifically tracked tax debt for the dedicated tax-payment action.
  int available=Debt-(HasKind("tax-due")&&!TaxPaid?settings.tax:0);
  return amount>0&&amount<=available&&Post(id,"debt-payment",day,-amount,-amount);
 }
 void Mandatory(string id,string kind,int day,int cost){int paid=Math.Min(Cash,cost);Post(id,kind,day,-paid,cost-paid);}
 public bool RecoverFuel(string id,int day){if(Fuel>0||Has(id))return false;Mandatory(id,"fuel-recovery",day,1800);Fuel=20;return true;}
 public bool CloseDay(int day){
  string id="settlement:"+day;if(Has(id))return false;
  if(!Ate(day))Mandatory("meal-fallback:"+day,"meal",day,settings.meal);
  Mandatory("parking:"+day,"parking",day,settings.parking);
  if(day>=settings.taxDueDay&&!TaxPaid&&!HasKind("tax-due"))Post("tax-due","tax-due",day,0,settings.tax);
  return Post(id,"settlement",day,0,0);
 }
 public int Expense(int day,string kind){int cost=0;foreach(var e in entries)if(e.day==day&&(kind==null||e.kind==kind)&&e.kind!="fare")cost+=e.debt-e.cash;return cost;}
 public AccountEntry[] Entries {get{return entries.ConvertAll(e=>e.Copy()).ToArray();}}
 public AccountSnapshot Snapshot(){return new AccountSnapshot{settings=settings,fuel=Fuel,entries=Entries};}
 public static CampaignAccount Restore(AccountSnapshot snapshot){
  if(snapshot==null||snapshot.version!=2||snapshot.settings==null||snapshot.entries==null||float.IsNaN(snapshot.fuel)||snapshot.fuel<0||snapshot.fuel>100)throw new ArgumentException("Invalid account checkpoint");
  var a=new CampaignAccount(snapshot.settings);foreach(var e in snapshot.entries){if(e==null||e.day<1||!a.Post(e.id,e.kind,e.day,e.cash,e.debt))throw new ArgumentException("Invalid transaction");}a.Fuel=snapshot.fuel;return a;
 }
}
