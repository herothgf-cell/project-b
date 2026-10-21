using System;
using System.Collections.Generic;

public enum BusinessState { Open, Closing, Summary, Won, Lost, Completed }
[Serializable] public class EconomySettings {
 public int days=7,target=100000,insurance=2000,food=3000;
 public float daySeconds=600,fuelPerKm=180,idlePerHour=1080;
 public bool Valid {get{return days>=1&&days<=30&&target>0&&target<=10000000&&daySeconds>=1&&daySeconds<=7200&&insurance>=0&&insurance<=100000&&food>=0&&food<=100000&&fuelPerKm>=0&&fuelPerKm<=10000&&idlePerHour>=0&&idlePerHour<=10000;}}
}
[Serializable] public class DayRecord {
 public double elapsed,realSeconds; public string endReason; public DriverCondition driver;
 public int day,income,rides,failedTrips,fuel,insurance,food,net;
 public float metres;
 public DayRecord Copy(){var c=(DayRecord)MemberwiseClone();c.driver=driver==null?null:driver.Copy();return c;}
}
[Serializable] public class EconomySnapshot {
 public int version=1;
 public EconomySettings settings;
 public DayRecord[] records;
}

// Engine-independent accounting. Only settlement commits a day's net earnings.
public class EconomyLedger {
 public bool OneDay {get;private set;}
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
 int legacyTotal; public int TotalNet {get{return Account==null?legacyTotal:Account.Net;}private set{legacyTotal=value;}}
 public double Elapsed {get;private set;}
 public double Metres {get;private set;}
 double fuel;
 readonly List<DayRecord> records=new List<DayRecord>();
 public DayRecord Last {get{return records.Count==0?null:records[records.Count-1];}}
 public bool CanAccept {get{return State==BusinessState.Open&&(Driver==null||Driver.CanAccept);}}
 public bool Operating {get{return State==BusinessState.Open||State==BusinessState.Closing;}}
 public bool ShowingSummary {get{return !Operating;}}
 public int FuelCost {get{return Account==null?(int)Math.Ceiling(Math.Max(0,fuel-0.000001)):Account.Expense(Day,"fuel")+Account.Expense(Day,"fuel-recovery");}}
 public int ProjectedNet {get{return Account==null?Income-FuelCost-Settings.insurance-Settings.food:Income-Account.Expense(Day,null)-Account.PendingDayCost(Day);}}
 public int RemainingGoal {get{return Math.Max(0,Settings.target-TotalNet);}}
 public double SecondsLeft {get{return Math.Max(0,Settings.daySeconds-Elapsed);}}
 public EconomyLedger(EconomySettings settings){if(settings==null||!settings.Valid)throw new ArgumentException("Invalid economy settings");Settings=settings;Day=1;State=BusinessState.Open;}
 public EconomyLedger(PrototypeSettings settings):this(new EconomySettings{days=settings.days,target=settings.target,daySeconds=settings.daySeconds,insurance=0,food=0,fuelPerKm=0,idlePerHour=0}){Account=new CampaignAccount(settings);OneDay=settings.oneDay;if(OneDay)Driver=new DriverCondition();}
 public bool RecordFare(string trip,int amount){if(!Operating||Account==null||!Account.Fare(trip,amount,Day))return false;Income+=amount;Rides++;return true;}
 public bool RecordFare(int amount){if(!Operating||amount<=0)return false;Income+=amount;Rides++;return true;}
 public void RecordFailure(){if(Operating)FailedTrips++;}
 public void Advance(double seconds,double metres){
  if(!Operating||double.IsNaN(seconds)||double.IsInfinity(seconds)||double.IsNaN(metres)||double.IsInfinity(metres)||seconds<0||metres<0)return;
  Elapsed+=seconds;Metres+=metres;fuel+=metres/1000*Settings.fuelPerKm+seconds/3600*Settings.idlePerHour;
  if(Account!=null)Account.Consume(seconds,metres);
  if(Driver!=null)Driver.Advance(seconds);
  if(Elapsed>=Settings.daySeconds)RequestClose("시간 종료");
 }
 public bool UseMeal(string id){if(!OneDay||State!=BusinessState.Open||SecondsLeft<50||services.Contains(id)||!Account.Meal(id,Day))return false;services.Add(id);Driver.Meal();ServiceTime(50);return true;}
 public bool UseSleep(string id){if(!OneDay||State!=BusinessState.Open||SecondsLeft<100||string.IsNullOrEmpty(id)||!services.Add(id))return false;Driver.Sleep();ServiceTime(100);return true;}
 void ServiceTime(double seconds){Elapsed+=seconds;if(Elapsed>=Settings.daySeconds)RequestClose("시간 종료");}
 public void RequestClose(){RequestClose("조기 퇴근");}
 public void RequestClose(string reason){if(State==BusinessState.Open){State=BusinessState.Closing;EndReason=reason;}}
 public bool Settle(){
  if(State!=BusinessState.Closing)return false;
  var record=new DayRecord{day=Day,income=Income,rides=Rides,failedTrips=FailedTrips,metres=(float)Metres,fuel=FuelCost,insurance=Settings.insurance,food=Settings.food,net=ProjectedNet};
  if(Account!=null){Account.CloseDay(Day);record.food=Account.Expense(Day,"meal");record.insurance=Account.Expense(Day,"insurance");record.net=Income-Account.Expense(Day,null);}
  record.elapsed=Elapsed;record.endReason=EndReason;record.driver=Driver==null?null:Driver.Copy();records.Add(record);if(Account==null)TotalNet+=record.net;State=Outcome();return true;
 }
 BusinessState Outcome(){if(OneDay)return BusinessState.Completed;return (Account==null?TotalNet>=Settings.target:Account.GoalMet)?BusinessState.Won:Day>=Settings.days?BusinessState.Lost:BusinessState.Summary;}
 public bool DepartureAllowed {get{return Account==null||Account.InsuranceValid(Day+1);}}
 public void RefreshSummary(){if(Account==null||Operating||Last==null)return;Last.insurance=Account.Expense(Day,"insurance");Last.food=Account.Expense(Day,"meal");Last.fuel=FuelCost;Last.net=Income-Account.Expense(Day,null);State=Outcome();}
 public bool NextDay(){if(State!=BusinessState.Summary)return false;Day++;Income=Rides=FailedTrips=0;Elapsed=Metres=fuel=0;State=BusinessState.Open;return true;}
 public EconomySnapshot Snapshot(){return new EconomySnapshot{settings=Settings,records=records.ConvertAll(r=>r.Copy()).ToArray()};}
 public static EconomyLedger RestoreCampaign(EconomySnapshot days,AccountSnapshot account){
  var a=CampaignAccount.Restore(account);var result=new EconomyLedger(a.Snapshot().settings);result.Account=a;
  if(days==null||days.records==null||days.records.Length>result.Settings.days)throw new ArgumentException("Invalid campaign days");
  foreach(var r in days.records){
   if(result.OneDay&&(r==null||double.IsNaN(r.elapsed)||double.IsInfinity(r.elapsed)||r.elapsed<0||double.IsNaN(r.realSeconds)||double.IsInfinity(r.realSeconds)||r.realSeconds<0))throw new ArgumentException("Invalid day time");
   if(r==null||r.day!=result.records.Count+1||r.income<0||r.rides<0||r.failedTrips<0||r.metres<0||float.IsNaN(r.metres)||float.IsInfinity(r.metres))throw new ArgumentException("Invalid campaign sequence");
   var normalized=r.Copy();normalized.insurance=0;normalized.food=a.Expense(r.day,"meal");normalized.fuel=a.Expense(r.day,"fuel")+a.Expense(r.day,"fuel-recovery");normalized.net=r.income-a.Expense(r.day,null);result.records.Add(normalized);
  }
  if(result.Last!=null){var r=result.Last;result.Day=r.day;result.Income=r.income;result.Rides=r.rides;result.FailedTrips=r.failedTrips;result.Metres=r.metres;result.Elapsed=r.elapsed;result.EndReason=r.endReason;if(result.OneDay){if(r.driver==null)throw new ArgumentException("Missing driver checkpoint");r.driver.Validate();result.Driver=r.driver.Copy();}result.State=result.Outcome();}return result;
 }
 public static EconomyLedger Restore(EconomySnapshot snapshot){
  if(snapshot==null||snapshot.version!=1||snapshot.records==null)throw new ArgumentException("Invalid checkpoint");
  var result=new EconomyLedger(snapshot.settings);
  if(snapshot.records.Length>snapshot.settings.days)throw new ArgumentException("Too many days");
  foreach(var r in snapshot.records){
   if(r==null||r.day!=result.records.Count+1||r.income<0||r.rides<0||r.failedTrips<0||r.metres<0||float.IsNaN(r.metres)||float.IsInfinity(r.metres)||r.fuel<0||r.insurance!=snapshot.settings.insurance||r.food!=snapshot.settings.food||(long)r.income-r.fuel-r.insurance-r.food!=r.net||result.TotalNet>=snapshot.settings.target)throw new ArgumentException("Inconsistent checkpoint");
   result.records.Add(r.Copy());result.TotalNet+=r.net;
  }
  if(result.Last!=null){var r=result.Last;result.Day=r.day;result.Income=r.income;result.Rides=r.rides;result.FailedTrips=r.failedTrips;result.Metres=r.metres;result.fuel=r.fuel;result.State=result.Outcome();}
  return result;
 }
}
