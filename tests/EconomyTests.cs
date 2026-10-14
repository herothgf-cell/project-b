using System;
class EconomyTests {
 static int checks;
 static void Check(bool value,string text){checks++;if(!value)throw new Exception(text);}
 static EconomySettings Config(){return new EconomySettings{days=2,target=10000,daySeconds=60,insurance=2000,food=3000,fuelPerKm=180,idlePerHour=1080};}
 static void Main(){
  var e=new EconomyLedger(Config());
  Check(e.Day==1&&e.CanAccept&&e.TotalNet==0,"fresh campaign");
  e.RecordFare(15000);e.Advance(60,1000);
  Check(e.State==BusinessState.Closing&&!e.CanAccept,"clock closes new work");
  Check(e.FuelCost==198,"distance and idle consumption");
  e.RecordFare(100);e.RecordFailure();
  Check(e.Settle(),"settle closing day");
  Check(e.Last.income==15100&&e.Last.rides==2&&e.Last.failedTrips==1,"daily receipts");
  Check(e.Last.net==9902&&e.TotalNet==9902&&e.RemainingGoal==98,"expenses before goal");
  Check(e.State==BusinessState.Summary,"gross income alone never wins");
  Check(!e.Settle()&&!e.RecordFare(1000),"closed day cannot charge or pay twice");
  var snapshot=e.Snapshot();var restored=EconomyLedger.Restore(snapshot);
  Check(restored.TotalNet==9902&&restored.State==BusinessState.Summary,"checkpoint restoration");
  Check(restored.NextDay()&&restored.Day==2&&restored.Income==0&&restored.FuelCost==0,"next day clean counters");
  Check(!restored.NextDay(),"cannot skip open day");
  restored.RecordFare(6000);restored.RequestClose();restored.Settle();
  Check(restored.State==BusinessState.Won&&restored.RemainingGoal==0&&!restored.NextDay(),"final-day net success");
  e.NextDay();e.RequestClose();e.Settle();
  Check(e.State==BusinessState.Lost&&e.TotalNet==4902&&!e.NextDay(),"deadline failure and fixed costs");
  var empty=new EconomyLedger(Config());empty.RequestClose();empty.Settle();
  Check(empty.Last.net==-5000&&empty.RemainingGoal==15000,"zero-fare day still costs money");
  var precision=new EconomyLedger(Config());for(int i=0;i<1000;i++)precision.Advance(0,.1);
  Check(precision.FuelCost==18,"fractional distance accumulates without per-frame rounding");
  precision.Advance(0,0);Check(precision.FuelCost==18,"paused zero advance");
  precision.Advance(double.NaN,100);precision.Advance(1,double.PositiveInfinity);
  Check(precision.FuelCost==18,"invalid telemetry ignored");
  precision.RequestClose();precision.Advance(10,100);
  Check(precision.FuelCost==39,"final passenger still consumes fuel after clock closes");
  precision.Settle();double metres=precision.Metres;precision.Advance(30,999);
  Check(precision.Metres==metres,"summary freezes fuel and time");
  var early=new EconomyLedger(Config());early.RecordFare(15000);early.RequestClose();early.Settle();
  Check(early.State==BusinessState.Won,"early net goal wins at settlement");
  snapshot.records[0].net++;
  bool invalid=false;try{EconomyLedger.Restore(snapshot);}catch(ArgumentException){invalid=true;}
  Check(invalid,"reject inconsistent checkpoint");
  Console.WriteLine("PASS: "+checks+" economy lifecycle checks");
 }
}
