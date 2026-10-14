using System;
using System.Collections.Generic;

public enum BusinessState { Open, Closing, Summary, Won, Lost }
[Serializable] public class EconomySettings {
 public int days=7,target=100000,insurance=2000,food=3000;
 public float daySeconds=600,fuelPerKm=180,idlePerHour=1080;
 public bool Valid {get{return days>=1&&days<=30&&target>0&&target<=10000000&&daySeconds>=1&&daySeconds<=7200&&insurance>=0&&insurance<=100000&&food>=0&&food<=100000&&fuelPerKm>=0&&fuelPerKm<=10000&&idlePerHour>=0&&idlePerHour<=10000;}}
}
[Serializable] public class DayRecord {
 public int day,income,rides,failedTrips,fuel,insurance,food,net;
 public float metres;
 public DayRecord Copy(){return (DayRecord)MemberwiseClone();}
}
[Serializable] public class EconomySnapshot {
 public int version=1;
 public EconomySettings settings;
 public DayRecord[] records;
}

// Engine-independent accounting. Only settlement commits a day's net earnings.
public class EconomyLedger {
 public EconomySettings Settings {get;private set;}
 public BusinessState State {get;private set;}
 public int Day {get;private set;}
 public int Income {get;private set;}
 public int Rides {get;private set;}
 public int FailedTrips {get;private set;}
 public int TotalNet {get;private set;}
 public double Elapsed {get;private set;}
 public double Metres {get;private set;}
 double fuel;
 readonly List<DayRecord> records=new List<DayRecord>();
 public DayRecord Last {get{return records.Count==0?null:records[records.Count-1];}}
 public bool CanAccept {get{return State==BusinessState.Open;}}
 public bool Operating {get{return State==BusinessState.Open||State==BusinessState.Closing;}}
 public bool ShowingSummary {get{return !Operating;}}
 public int FuelCost {get{return (int)Math.Ceiling(Math.Max(0,fuel-0.000001));}}
 public int ProjectedNet {get{return Income-FuelCost-Settings.insurance-Settings.food;}}
 public int RemainingGoal {get{return Math.Max(0,Settings.target-TotalNet);}}
 public double SecondsLeft {get{return Math.Max(0,Settings.daySeconds-Elapsed);}}
 public EconomyLedger(EconomySettings settings){if(settings==null||!settings.Valid)throw new ArgumentException("Invalid economy settings");Settings=settings;Day=1;State=BusinessState.Open;}
 public bool RecordFare(int amount){if(!Operating||amount<=0)return false;Income+=amount;Rides++;return true;}
 public void RecordFailure(){if(Operating)FailedTrips++;}
 public void Advance(double seconds,double metres){
  if(!Operating||double.IsNaN(seconds)||double.IsInfinity(seconds)||double.IsNaN(metres)||double.IsInfinity(metres)||seconds<0||metres<0)return;
  Elapsed+=seconds;Metres+=metres;fuel+=metres/1000*Settings.fuelPerKm+seconds/3600*Settings.idlePerHour;
  if(Elapsed>=Settings.daySeconds)RequestClose();
 }
 public void RequestClose(){if(State==BusinessState.Open)State=BusinessState.Closing;}
 public bool Settle(){
  if(State!=BusinessState.Closing)return false;
  var record=new DayRecord{day=Day,income=Income,rides=Rides,failedTrips=FailedTrips,metres=(float)Metres,fuel=FuelCost,insurance=Settings.insurance,food=Settings.food,net=ProjectedNet};
  records.Add(record);TotalNet+=record.net;State=Outcome();return true;
 }
 BusinessState Outcome(){return TotalNet>=Settings.target?BusinessState.Won:Day>=Settings.days?BusinessState.Lost:BusinessState.Summary;}
 public bool NextDay(){if(State!=BusinessState.Summary)return false;Day++;Income=Rides=FailedTrips=0;Elapsed=Metres=fuel=0;State=BusinessState.Open;return true;}
 public EconomySnapshot Snapshot(){return new EconomySnapshot{settings=Settings,records=records.ConvertAll(r=>r.Copy()).ToArray()};}
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
