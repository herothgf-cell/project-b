using System;
using System.Collections.Generic;

public enum BusinessState { Open, Closing, Summary, Won, Lost, Completed }
[Serializable] public class EconomySettings {
 public int days=7,target=100000,insurance=2000,food=3000;
 public float daySeconds=600,fuelPerKm=180,idlePerHour=1080;
 public bool Valid {get{return days>=1&&days<=30&&target>0&&target<=10000000&&daySeconds>=1&&daySeconds<=7200&&insurance>=0&&insurance<=100000&&food>=0&&food<=100000&&fuelPerKm>=0&&fuelPerKm<=10000&&idlePerHour>=0&&idlePerHour<=10000;}}
}
[Serializable] public class DayRecord {
 public OperationMetricsSnapshot metrics;
 public double elapsed,realSeconds; public string endReason; public DriverCondition driver;
 public int day,income,rides,failedTrips,fuel,insurance,food,wash,repair,tow,accident,net;
 public float metres;
 public DayRecord Copy(){var c=(DayRecord)MemberwiseClone();c.driver=driver==null?null:driver.Copy();c.metrics=metrics==null?null:metrics.Copy();return c;}
}
[Serializable] public class EconomySnapshot {
 public OperationMetricsSnapshot metrics;
 public int version=1;
 public EconomySettings settings;
 public DayRecord[] records;
 public int currentDay;public bool dayOpen;
 public VehicleCareSnapshot care;
 public bool hasActive;public ActiveDaySnapshot active;
}

[Serializable] public class ActiveDaySnapshot {public BusinessState state;public int income,rides,failedTrips;public double elapsed,metres,fuel;public string endReason;public DriverCondition driver;public string[] services;}

// Engine-independent accounting. Only settlement commits a day's net earnings.
public class EconomyLedger {
 OperationMetrics metrics=new OperationMetrics();public OperationMetrics Metrics {get{return metrics;}private set{metrics=value;}}
 public OperationMetricsSnapshot TotalMetrics {get{var total=new OperationMetricsSnapshot{captured=true};foreach(var r in records)AddMetrics(total,r.metrics);if(Operating)AddMetrics(total,Metrics.Capture());return total;}}
 static void AddMetrics(OperationMetricsSnapshot total,OperationMetricsSnapshot value){if(value==null||!value.captured)return;total.emptyMetres+=value.emptyMetres;total.occupiedMetres+=value.occupiedMetres;total.emptyMovingSeconds+=value.emptyMovingSeconds;total.emptyWaitingSeconds+=value.emptyWaitingSeconds;total.serviceSeconds+=value.serviceSeconds;}
 public void AdvanceOperation(double seconds,double metres,bool occupied){if(!Operating)return;double before=Elapsed;Advance(seconds,metres);Care.Advance(Elapsed-before);Metrics.Observe(metres,seconds,occupied,false);}
 public void AdvanceService(double seconds){if(!Operating)return;Advance(seconds,0);Metrics.Observe(0,seconds,false,true);}
 public VehicleCare Care {get;private set;}
 public bool OneDay {get;private set;}
 // Runtime-only profile: no static setting or campaign checkpoint can carry it.
 public bool PvpProfile {get;private set;}
 public int PvpMeals {get;private set;}
 public int PvpRests {get;private set;}
 public void ApplyNetworkPvpCounters(int meals,int rests){if(!PvpProfile||meals<0||rests<0)throw new ArgumentException("invalid PVP counters");PvpMeals=meals;PvpRests=rests;}
 public bool EnablePvpProfile(){if(!OneDay||!Operating||Account==null||Driver==null)return false;PvpProfile=true;return true;}
 public DriverCondition Driver {get;private set;}
 public string EndReason {get;private set;}
 readonly HashSet<string> services=new HashSet<string>();
 public CampaignAccount Account {get;private set;}
 public EconomySettings Settings {get;private set;}
 public BusinessState State {get;private set;}
 public int Day {get;private set;}
 public int Income {get;private set;}
 public int Rides {get;private set;}
 public int FailedTrips {get;private set;}
 public int TotalIncome {get{int total=0;foreach(var record in records)total+=record.income;return total+(Operating?Income:0);}}
 public int TotalExpenses {get{return TotalIncome-TotalNet;}}
 double nightlyFatigueRecovery;
 int legacyTotal; public int TotalNet {get{return Account==null?legacyTotal:Account.Net;}private set{legacyTotal=value;}}
 public double Elapsed {get;private set;}
 public double Metres {get;private set;}
 double fuel;
 readonly List<DayRecord> records=new List<DayRecord>();
 public DayRecord Last {get{return records.Count==0?null:records[records.Count-1];}}
 public bool CanAccept {get{return State==BusinessState.Open&&!Care.Damage.Disabled&&(Driver==null||Driver.CanAccept);}}
 public bool Operating {get{return State==BusinessState.Open||State==BusinessState.Closing;}}
 public bool ShowingSummary {get{return !Operating;}}
 public int FuelCost {get{return Account==null?(int)Math.Ceiling(Math.Max(0,fuel-0.000001)):Account.Expense(Day,"fuel")+Account.Expense(Day,"fuel-recovery");}}
 public int ProjectedNet {get{return Account==null?Income-FuelCost-Settings.insurance-Settings.food:Income-Account.Expense(Day,null)-Account.PendingDayCost(Day);}}
 public int RemainingGoal {get{return Math.Max(0,Settings.target-TotalNet);}}
 public double SecondsLeft {get{return Math.Max(0,Settings.daySeconds-Elapsed);}}
 public EconomyLedger(EconomySettings settings){if(settings==null||!settings.Valid)throw new ArgumentException("Invalid economy settings");Settings=settings;Day=1;Care=new VehicleCare();State=BusinessState.Open;}
 public EconomyLedger(PrototypeSettings settings):this(new EconomySettings{days=settings.days,target=settings.target,daySeconds=settings.daySeconds,insurance=0,food=0,fuelPerKm=0,idlePerHour=0}){Account=new CampaignAccount(settings);OneDay=settings.oneDay;nightlyFatigueRecovery=settings.nightlyFatigueRecovery;if(OneDay)Driver=new DriverCondition();}
 public bool RecordFare(string trip,int amount){if(!Operating||Account==null||!Account.Fare(trip,amount,Day))return false;Income+=amount;Rides++;return true;}
 public bool RecordFare(int amount){if(!Operating||amount<=0)return false;Income+=amount;Rides++;return true;}
 public void RecordFailure(){if(Operating)FailedTrips++;}
 public void Advance(double seconds,double metres){
  if(!Operating||double.IsNaN(seconds)||double.IsInfinity(seconds)||double.IsNaN(metres)||double.IsInfinity(metres)||seconds<0||metres<0)return;
  double before=Elapsed;Elapsed+=seconds;Metres+=metres;fuel+=metres/1000*Settings.fuelPerKm+seconds/3600*Settings.idlePerHour;
  if(Account!=null)Account.Consume(seconds,metres);
  if(Driver!=null)Driver.Advance(seconds,false,PvpProfile?PvpConditionRules.FatigueMultiplier:1);
  BudgetCrossed(before);
 }
 public DriverCondition PreviewConditionService(bool sleeping){
  if(Driver==null)return null;
  if(PvpProfile)return sleeping?PvpConditionRules.PreviewRest(Driver):PvpConditionRules.PreviewMeal(Driver);
  var next=Driver.Copy();bool crosses=Elapsed<Settings.daySeconds&&Elapsed+(sleeping?100:50)>=Settings.daySeconds;
  if(sleeping){if(crosses)next.fatigue=100;next.Sleep();}
  else {next.Meal();if(crosses)next.fatigue=100;}
  return next;
 }
 public bool UseMeal(string id){if(PvpProfile)return false;if(!OneDay||State!=BusinessState.Open||services.Contains(id)||!Account.Meal(id,Day))return false;services.Add(id);Driver=PreviewConditionService(false);ServiceTime(50);return true;}
 public bool UseSleep(string id){if(PvpProfile)return false;if(!OneDay||State!=BusinessState.Open||Driver.drowsiness.Blackout||string.IsNullOrEmpty(id)||!services.Add(id))return false;var next=PreviewConditionService(true);ServiceTime(100);Driver=next;return true;}
 // Scheduler owns duration and cancellation. Only this path accounts for a pending
 // condition service frame; it must replace normal operation advancement.
 public void AdvancePvpConditionService(double seconds,bool resting){
  if(!PvpProfile||State!=BusinessState.Open||!Finite(seconds)||seconds<=0)return;
  Elapsed+=seconds;Driver.Advance(seconds,resting,PvpConditionRules.FatigueMultiplier);
  Metrics.Observe(0,seconds,false,true);
 }
 public bool CompletePvpMeal(string id){
  if(!PvpProfile||State!=BusinessState.Open||string.IsNullOrEmpty(id)||services.Contains(id)||!Account.PvpMeal(id,Day))return false;
  services.Add(id);Driver.satiety=Math.Min(100,Driver.satiety+PvpConditionRules.MealSatiety);PvpMeals++;return true;
 }
 public bool CompletePvpRest(string id){
  if(!PvpProfile||State!=BusinessState.Open||string.IsNullOrEmpty(id)||services.Contains(id)||!Account.PvpRest(id,Day))return false;
  services.Add(id);
  Driver.fatigue=Math.Max(0,Driver.fatigue-PvpConditionRules.RestFatigue);
  if(Driver.drowsiness!=null)Driver.drowsiness.Recover();PvpRests++;return true;
 }
 public bool UseEmergencySleep(string id){return EmergencySleepCore(id,true);}
 public bool UseEmergencySleepInMatch(string id){return EmergencySleepCore(id,false);}
 bool EmergencySleepCore(string id,bool closeDay){if(PvpProfile&&closeDay)return false;if(!OneDay||State!=BusinessState.Open||Driver.drowsiness==null||!Driver.drowsiness.Blackout||string.IsNullOrEmpty(id)||!services.Add(id))return false;var next=Driver.Copy();if(!PvpProfile&&Elapsed<Settings.daySeconds&&Elapsed+100>=Settings.daySeconds)next.fatigue=100;next.EmergencySleep();ServiceTime(100);Driver=next;if(closeDay)RequestClose("졸음 운전 위험 · 당일 영업 종료");return true;}
 public bool ChargeAccident(string id){return ChargeAccident(id,3000);}
 public bool ChargeAccident(string id,int fee){return Operating&&Account!=null&&Account.Accident(id,Day,fee);}
 public bool CompleteAccident(string id){return CompleteAccident(id,45);}
 public bool CompleteAccident(string id,double duration){
  if(!Finite(duration)||duration<=0||!Operating||Account==null||!Account.CompleteAccident(id,Day))return false;
  if(!PvpProfile){AdvanceAccidentCondition(duration);ServiceTime(duration);}return true;
 }
 // Replace normal operation advancement for each host processing frame; the caller
 // clamps seconds to remaining processing/match time. No fuel or natural dirt accrues.
 public void AdvanceAccidentProcessing(double seconds){
  if(!PvpProfile||!Operating||!Finite(seconds)||seconds<=0)return;
  AdvanceAccidentCondition(seconds);ServiceTime(seconds);
 }
 // Exposure accrues only after fatigue reaches100, including hunger-rate changes
 // and the single-player operating budget. Preserve prior exposure; never recover here.
 void AdvanceAccidentCondition(double seconds){
  if(Driver==null)return;
  double multiplier=PvpProfile?PvpConditionRules.FatigueMultiplier:1;
  double normal=Math.Min(seconds,Math.Max(0,(Driver.satiety-25)*12));
  double hungry=Math.Min(seconds-normal,Math.Max(0,(Math.Min(Driver.satiety,25)-10)*12));
  double[] spans={normal,hungry,seconds-normal-hungry};double[] rates={multiplier/15,multiplier/10,multiplier*2/15};
  double deficit=Math.Max(0,100-Driver.fatigue),before=0,reachesAt=seconds;
  for(int i=0;i<spans.Length;i++){
   double gain=spans[i]*rates[i];
   if(deficit<=gain){reachesAt=before+deficit/rates[i];break;}
   deficit-=gain;before+=spans[i];
  }
  if(!PvpProfile&&OneDay&&Elapsed<Settings.daySeconds&&Elapsed+seconds>=Settings.daySeconds)reachesAt=Math.Min(reachesAt,Settings.daySeconds-Elapsed);
  Driver.Advance(seconds,false,multiplier);
  if(Driver.drowsiness!=null&&seconds>reachesAt)Driver.drowsiness.Advance(seconds-reachesAt,true,100,false);
 }
 public bool UseWash(string id){if(Account==null||State!=BusinessState.Open||string.IsNullOrEmpty(id)||services.Contains(id)||!Account.Wash(id,Day))return false;services.Add(id);Care.Wash();if(Driver!=null)Driver.Advance(30,false,PvpProfile?PvpConditionRules.FatigueMultiplier:1);ServiceTime(30);return true;}
 // Only after a settlement (summary/completed). Trade-in uses the current durability; the
 // replacement car starts repaired and clean. The caller persists the whole ledger once.
 public VehicleQuote QuoteVehicle(string model){return Account==null?null:VehicleCatalog.Quote(Account.Vehicle,model,Account.Cash,Account.Debt,Care.Damage.Durability);}
 public bool PurchaseVehicle(string model){
  if(Account==null||Settings.days!=3||PvpProfile||Operating||Last==null)return false;
  if(!Account.BuyVehicle(model,Day,Care.Damage.Durability))return false;Care.Damage.Repair();Care.Wash();return true;
 }
 public bool BuyVehicleColor(){return Account!=null&&!PvpProfile&&!Operating&&Last!=null&&Account.BuyVehicleColor(Day);}
 public bool BuyVehicleOrnament(){return Account!=null&&!PvpProfile&&!Operating&&Last!=null&&Account.BuyVehicleOrnament(Day);}
 public int RepairCost {get{return Care.Damage.RepairCost;}}
 public double RepairSeconds {get{return Care.Damage.RepairSeconds;}}
 // Host scheduler owns elapsed time and cancellation; incomplete optional services
 // never call these commit methods. Completion changes only cost and vehicle state.
 public bool CompletePvpRepair(string id){
  if(!PvpProfile||State!=BusinessState.Open||Account==null||string.IsNullOrWhiteSpace(id)||services.Contains(id)||Care.Damage.Durability==100||!Account.Repair(id,Day,100-Care.Damage.Durability))return false;
  services.Add(id);Care.Damage.Repair();return true;
 }
 public bool CompletePvpTow(string id,string bayId){
  if(!PvpProfile||State!=BusinessState.Open||Account==null||string.IsNullOrWhiteSpace(id)||string.IsNullOrWhiteSpace(bayId)||services.Contains(id)||!Care.Damage.Disabled||!string.IsNullOrEmpty(Care.Damage.RepairBayId)||!Account.Tow(id,Day))return false;
  services.Add(id);Care.Damage.EnterRepairBay(bayId);return true;
 }
 public bool UseRepair(string id){
  if(!Operating||Account==null||string.IsNullOrEmpty(id)||services.Contains(id)||Care.Damage.Durability==100)return false;
  double seconds=RepairSeconds;if(!Account.Repair(id,Day,100-Care.Damage.Durability))return false;
  services.Add(id);Care.Damage.Repair();if(Driver!=null)Driver.Advance(seconds,false,PvpProfile?PvpConditionRules.FatigueMultiplier:1);ServiceTime(seconds);return true;
 }
 public bool UseTow(string id,string bayId){
  if(!Operating||Account==null||string.IsNullOrEmpty(id)||string.IsNullOrWhiteSpace(bayId)||services.Contains(id)||!Care.Damage.Disabled||!string.IsNullOrEmpty(Care.Damage.RepairBayId)||!Account.Tow(id,Day))return false;
  services.Add(id);Care.Damage.EnterRepairBay(bayId);if(Driver!=null)Driver.Advance(30,false,PvpProfile?PvpConditionRules.FatigueMultiplier:1);ServiceTime(30);return true;
 }
 void ServiceTime(double seconds){double before=Elapsed;Elapsed+=seconds;Metrics.Observe(0,seconds,false,true);BudgetCrossed(before);}
 void BudgetCrossed(double before){if(PvpProfile)return;if(before<Settings.daySeconds&&Elapsed>=Settings.daySeconds){if(OneDay)Driver.fatigue=100;else RequestClose("시간 종료");}}
 public bool SetDaySeconds(float seconds){if(PvpProfile||!OneDay||Account==null||!PrototypeSettings.IsShiftChoice(seconds)||State!=BusinessState.Open||Elapsed>0)return false;Settings.daySeconds=seconds;Account.SetDaySeconds(seconds);return true;}
 // Developer test: may change today's length mid-shift. Shortening past the elapsed time applies the normal end-of-shift fatigue.
 public bool DevSetDaySeconds(float seconds){if(PvpProfile||!OneDay||Account==null||!PrototypeSettings.IsShiftChoice(seconds)||State!=BusinessState.Open)return false;double old=Settings.daySeconds;Settings.daySeconds=seconds;Account.SetDaySeconds(seconds);if(Driver!=null&&Elapsed<old&&Elapsed>=seconds)Driver.fatigue=100;return true;}
 public void RequestClose(){RequestClose("조기 퇴근");}
 public void RequestClose(string reason){if(State==BusinessState.Open){State=BusinessState.Closing;EndReason=reason;}}
 public bool Settle(){
  if(State!=BusinessState.Closing)return false;
  var record=new DayRecord{metrics=Metrics.Capture(),day=Day,income=Income,rides=Rides,failedTrips=FailedTrips,metres=(float)Metres,fuel=FuelCost,insurance=Settings.insurance,food=Settings.food,net=ProjectedNet};
  if(Account!=null){Account.CloseDay(Day);record.food=Account.Expense(Day,"meal");record.insurance=Account.Expense(Day,"insurance");record.net=Income-Account.Expense(Day,null);}
  record.wash=Account==null?0:Account.Expense(Day,"wash");record.repair=Account==null?0:Account.Expense(Day,"repair");record.tow=Account==null?0:Account.Expense(Day,"tow");record.accident=Account==null?0:Account.Expense(Day,"accident");record.elapsed=Elapsed;record.endReason=EndReason;record.driver=Driver==null?null:Driver.Copy();records.Add(record);if(Account==null)TotalNet+=record.net;State=Outcome();return true;
 }
 BusinessState Outcome(){if(OneDay)return Day>=Settings.days?BusinessState.Completed:BusinessState.Summary;return (Account==null?TotalNet>=Settings.target:Account.GoalMet)?BusinessState.Won:Day>=Settings.days?BusinessState.Lost:BusinessState.Summary;}
 public bool DepartureAllowed {get{return Account==null||Account.InsuranceValid(Day+1);}}
 public void RefreshSummary(){if(Account==null||Operating||Last==null)return;Last.insurance=Account.Expense(Day,"insurance");Last.food=Account.Expense(Day,"meal");Last.fuel=FuelCost;Last.wash=Account.Expense(Day,"wash");Last.repair=Account.Expense(Day,"repair");Last.tow=Account.Expense(Day,"tow");Last.accident=Account.Expense(Day,"accident");Last.net=Income-Account.Expense(Day,null);State=Outcome();}
 public bool NextDay(){if(State!=BusinessState.Summary)return false;PvpProfile=false;PvpMeals=PvpRests=0;Day++;Metrics=new OperationMetrics();Care.BeginDay(Day);Income=Rides=FailedTrips=0;Elapsed=Metres=fuel=0;EndReason=null;services.Clear();if(Driver!=null){Driver=new DriverCondition{satiety=Driver.satiety,fatigue=Math.Max(0,Driver.fatigue-nightlyFatigueRecovery)};}State=BusinessState.Open;return true;}
 public EconomySnapshot Snapshot(){return new EconomySnapshot{metrics=Metrics.Capture(),settings=Settings,records=records.ConvertAll(r=>r.Copy()).ToArray(),currentDay=Day,dayOpen=Operating,care=Care.Snapshot(),hasActive=Operating,active=Operating?new ActiveDaySnapshot{state=State,income=Income,rides=Rides,failedTrips=FailedTrips,elapsed=Elapsed,metres=Metres,fuel=fuel,endReason=EndReason,driver=Driver==null?null:Driver.Copy(),services=new List<string>(services).ToArray()}:null};}
 public static EconomyLedger RestoreCampaign(EconomySnapshot days,AccountSnapshot account){
  var a=CampaignAccount.Restore(account);var result=new EconomyLedger(a.Snapshot().settings);result.Account=a;
  if(days==null||days.records==null||days.records.Length>result.Settings.days)throw new ArgumentException("Invalid campaign days");
  foreach(var r in days.records){
   if(result.OneDay&&(r==null||double.IsNaN(r.elapsed)||double.IsInfinity(r.elapsed)||r.elapsed<0||double.IsNaN(r.realSeconds)||double.IsInfinity(r.realSeconds)||r.realSeconds<0))throw new ArgumentException("Invalid day time");
   if(r==null||r.day!=result.records.Count+1||r.income<0||r.rides<0||r.failedTrips<0||r.metres<0||float.IsNaN(r.metres)||float.IsInfinity(r.metres))throw new ArgumentException("Invalid campaign sequence");
   var normalized=r.Copy();normalized.insurance=0;normalized.wash=a.Expense(r.day,"wash");normalized.repair=a.Expense(r.day,"repair");normalized.tow=a.Expense(r.day,"tow");normalized.accident=a.Expense(r.day,"accident");normalized.food=a.Expense(r.day,"meal");normalized.fuel=a.Expense(r.day,"fuel")+a.Expense(r.day,"fuel-recovery");normalized.net=r.income-a.Expense(r.day,null);result.records.Add(normalized);
  }
  if(result.Last!=null){var r=result.Last;result.Day=r.day;result.Income=r.income;result.Rides=r.rides;result.FailedTrips=r.failedTrips;result.Metres=r.metres;result.Elapsed=r.elapsed;result.EndReason=r.endReason;if(result.OneDay){if(r.driver==null)throw new ArgumentException("Missing driver checkpoint");r.driver.Validate();result.Driver=r.driver.Copy();}result.State=result.Outcome();}
  if(days.dayOpen){if(days.currentDay!=result.records.Count+1||days.currentDay>result.Settings.days)throw new ArgumentException("Invalid campaign start boundary");if(result.Last!=null&&!result.NextDay())throw new ArgumentException("Cannot reopen completed campaign");}
  if(days.hasActive){var v=days.active;if(v==null)throw new ArgumentException("Missing active day");if(!days.dayOpen||v.state!=BusinessState.Open&&v.state!=BusinessState.Closing||v.income<0||v.rides<0||v.failedTrips<0||!Finite(v.elapsed)||!Finite(v.metres)||!Finite(v.fuel)||v.elapsed<0||v.metres<0||v.fuel<0||v.services==null)throw new ArgumentException("Invalid active day");if(result.OneDay){if(v.driver==null)throw new ArgumentException("Missing active condition");v.driver.Validate();result.Driver=v.driver.Copy();}result.State=v.state;result.Income=v.income;result.Rides=v.rides;result.FailedTrips=v.failedTrips;result.Elapsed=v.elapsed;result.Metres=v.metres;result.fuel=v.fuel;result.EndReason=v.endReason;foreach(var id in v.services)if(string.IsNullOrEmpty(id)||!result.services.Add(id))throw new ArgumentException("Invalid service identity");}
  result.Metrics=OperationMetrics.Restore(days.metrics,result.Elapsed);result.Care=VehicleCare.Restore(days.care);result.Care.BeginDay(result.Day);return result;
 }
 static bool Finite(double value){return !double.IsNaN(value)&&!double.IsInfinity(value);}
 public static EconomyLedger Restore(EconomySnapshot snapshot){
  if(snapshot==null||snapshot.version!=1||snapshot.records==null)throw new ArgumentException("Invalid checkpoint");
  var result=new EconomyLedger(snapshot.settings);
  if(snapshot.records.Length>snapshot.settings.days)throw new ArgumentException("Too many days");
  foreach(var r in snapshot.records){
   if(r==null||r.day!=result.records.Count+1||r.income<0||r.rides<0||r.failedTrips<0||r.metres<0||float.IsNaN(r.metres)||float.IsInfinity(r.metres)||r.fuel<0||r.wash<0||r.repair<0||r.tow<0||r.accident<0||r.insurance!=snapshot.settings.insurance||r.food!=snapshot.settings.food||(long)r.income-r.fuel-r.insurance-r.food-r.wash-r.repair-r.tow-r.accident!=r.net||result.TotalNet>=snapshot.settings.target)throw new ArgumentException("Inconsistent checkpoint");
   result.records.Add(r.Copy());result.TotalNet+=r.net;
  }
  if(result.Last!=null){var r=result.Last;result.Day=r.day;result.Income=r.income;result.Rides=r.rides;result.FailedTrips=r.failedTrips;result.Metres=r.metres;result.fuel=r.fuel;result.State=result.Outcome();}
  result.Care=VehicleCare.Restore(snapshot.care);result.Care.BeginDay(result.Day);
  return result;
 }
}
