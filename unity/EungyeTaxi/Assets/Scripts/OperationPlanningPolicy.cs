using System;
using System.Collections.Generic;

// Immutable views: nothing here owns or advances a live account/condition.
public sealed class OperationForecast {
 public bool Available {get;private set;} public double EndFuel {get;private set;} public double EndSatiety {get;private set;} public double EndFatigue {get;private set;}
 public double TravelSeconds {get;private set;} public double TravelMetres {get;private set;} public bool Overtime {get;private set;} public bool MatchEndsBeforeArrival {get;private set;}
 public string Text {get{return !Available?"상태 예측 불가 · 연결 경로/예상시간 확인 필요":"도착 예상 연료 "+EndFuel.ToString("0.0")+"% · 포만 "+EndSatiety.ToString("0")+" · 피로 "+EndFatigue.ToString("0")+(Overtime?" · 영업 마감 초과":"")+(MatchEndsBeforeArrival?" · 경기 종료 전 도착 어려움":"")+" (교통·대기·우회에 따라 변동)";}}
 static bool Finite(double n){return !double.IsNaN(n)&&!double.IsInfinity(n)&&n>=0;}
 public static OperationForecast Calculate(CampaignAccount account,DriverCondition driver,double seconds,double metres,bool campaignDay,double secondsLeft,bool match,double matchLeft){
  if(account==null||driver==null||!Finite(seconds)||!Finite(metres)||!Finite(secondsLeft)||match&&!Finite(matchLeft))return new OperationForecast();
  var copy=CampaignAccount.Restore(account.Snapshot());copy.Consume(seconds,metres);var condition=driver==null?null:driver.Copy();
  bool overtime=campaignDay&&!match&&seconds>=secondsLeft;
  if(condition!=null){condition.Advance(seconds);if(overtime&&secondsLeft>0)condition.fatigue=100;}
  return new OperationForecast{Available=true,EndFuel=copy.Fuel,EndSatiety=condition==null?0:condition.satiety,EndFatigue=condition==null?0:condition.fatigue,TravelSeconds=seconds,TravelMetres=metres,Overtime=overtime,MatchEndsBeforeArrival=match&&seconds>=matchLeft};
 }
}
public sealed class OperationServiceQuote {
 public bool Available {get;private set;} public double TravelSeconds {get;private set;} public double UseSeconds {get;private set;} public int Price {get;private set;} public int NewDebt {get;private set;}
 public double EndFuel {get;private set;} public double EndSatiety {get;private set;} public double EndFatigue {get;private set;}
 public static OperationServiceQuote Calculate(EconomyLedger ledger,int kind,double seconds,double metres){
  if(ledger==null||ledger.Account==null||double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<0||double.IsNaN(metres)||double.IsInfinity(metres)||metres<0)return new OperationServiceQuote();
  var clone=EconomyLedger.RestoreCampaign(ledger.Snapshot(),ledger.Account.Snapshot());clone.Advance(seconds,metres);
  int before=clone.Account.Expense(clone.Day,null),debt=clone.Account.Debt;double time=clone.Elapsed;bool done=false;
  if(kind==0){done=clone.Account.Refuel("forecast:fuel",Math.Min(10,100-clone.Account.Fuel),clone.Day);if(done)clone.AdvanceService(10);}
  else if(kind==1){if(clone.OneDay)done=clone.UseMeal("forecast:meal");else {done=clone.Account.Meal("forecast:meal",clone.Day);if(done)clone.AdvanceService(10);}}
  else if(kind==3&&clone.OneDay)done=clone.UseSleep("forecast:sleep");else if(kind==4)done=clone.UseWash("forecast:wash");else if(kind==5)done=clone.UseRepair("forecast:repair");
  return new OperationServiceQuote{Available=done,TravelSeconds=seconds,UseSeconds=clone.Elapsed-time,Price=clone.Account.Expense(clone.Day,null)-before,NewDebt=clone.Account.Debt-debt,EndFuel=clone.Account.Fuel,EndSatiety=clone.Driver==null?0:clone.Driver.satiety,EndFatigue=clone.Driver==null?0:clone.Driver.fatigue};
 }
}
public sealed class FacilityAdmissionQueue {
 sealed class Ticket {public string site;public int owner;public bool admitted;}
 readonly List<Ticket> tickets=new List<Ticket>();
 public void Request(string site,int owner,double now){if(string.IsNullOrEmpty(site))return;foreach(var t in tickets)if(t.owner==owner){if(t.site==site)return;break;}Release(owner);tickets.Add(new Ticket{site=site,owner=owner});}
 public int Position(string site,int owner){int place=0;foreach(var t in tickets)if(t.site==site){place++;if(t.owner==owner)return place;}return 0;}
 public int Count(string site){int count=0;foreach(var t in tickets)if(t.site==site)count++;return count;}
 public bool IsAdmitted(int owner){foreach(var t in tickets)if(t.owner==owner)return t.admitted;return false;}
 public bool TryAdmit(string site,int owner,bool clear){foreach(var t in tickets)if(t.site==site){if(t.owner!=owner||!clear)return false;t.admitted=true;return true;}return false;}
 public void Release(int owner){tickets.RemoveAll(delegate(Ticket t){return t.owner==owner;});}
 public void RemoveAbsent(Predicate<int> alive){tickets.RemoveAll(delegate(Ticket t){return !alive(t.owner);});}
}
public static class BotServiceDecision {
 public static int Needed(float fuel,double fatigue,double satiety,int cleanliness,bool needsWash,int durability){
  if(durability<=20)return 5;if(fuel<8)return 0;if(needsWash||cleanliness<30)return 4;if(fatigue>=90)return 3;
  if(satiety<25)return 1;if(cleanliness<45)return 4;if(fatigue>=55)return 3;if(fuel<25)return 0;if(durability<45)return 5;return -1;
 }
 static double SafeSeconds(DriverCondition driver,double maximum){double low=0,high=maximum;for(int i=0;i<24;i++){double mid=(low+high)/2;var c=driver.Copy();c.Advance(mid);if(c.fatigue>=100)high=mid;else low=mid;}return low;}
 public static bool Worthwhile(int kind,double travel,double remaining,double use,double wait,int cost,int nextFare,double nextTripSeconds,double satiety,double fatigue,bool critical){
  if(double.IsNaN(travel)||double.IsInfinity(travel)||travel<0||double.IsNaN(wait)||double.IsInfinity(wait)||wait<0)return false;
  if(critical)return true;double overhead=travel+use+wait;if(remaining<=overhead+Math.Max(20,nextTripSeconds))return false;
  double rate=Math.Max(0,nextFare)/Math.Max(30,nextTripSeconds),available=remaining-overhead;
  if(kind!=1&&kind!=3)return cost+overhead*rate<Math.Max(nextFare,rate*available);
  var before=new DriverCondition{satiety=satiety,fatigue=fatigue};before.Advance(travel+wait);var after=before.Copy();if(kind==1)after.Meal();else after.Sleep();
  double gained=SafeSeconds(after,available)-Math.Max(0,SafeSeconds(before,remaining-travel-wait)-use);
  return gained*rate>cost+overhead*rate;
 }
}
