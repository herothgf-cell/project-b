using System;
class FareTripTests {
 static void Check(bool c,string m){if(!c)throw new Exception(m);}
 static void Main(){try{Run();}catch(Exception ex){Console.WriteLine(ex.Message);Environment.Exit(1);}}
 static void Run(){
  var t=new FareTrip();Check(!t.Board(),"cannot board without job");
  Check(t.Accept(5000,60),"accept");Check(!t.Accept(9999,60),"reject concurrent job");
  t.Tick(100);Check(t.Remaining==60,"pickup travel does not consume passenger deadline");
  Check(t.Board(),"board");t.Tick(20);Check(t.Remaining==40,"countdown");
  Check(t.Complete()==5000,"pay once");Check(t.Complete()==0,"no duplicate payment");
  t.Clear();t.Accept(8000,2);t.Board();t.Tick(2);
  Check(t.State==TripState.OnTrip&&t.Remaining==0,"deadline keeps passenger onboard until destination");
  t.Tick(120);Check(t.State==TripState.OnTrip&&t.Remaining==0,"late passenger remains onboard during closing wait");
  Check(t.Complete()==0&&t.State==TripState.Failed,"late arrival ends ride for zero fare");
  Check(t.Complete()==0&&t.State==TripState.Failed,"late arrival cannot finish or pay twice");
  t.Clear();t.Accept(5000,60,180);t.Tick(0);Check(t.PickupRemaining==180,"paused pickup deadline");t.Tick(179);Check(t.State==TripState.ToPickup&&t.Remaining==60,"pickup timer separate from fare timer");t.Tick(1);Check(t.PickupExpired&&t.State==TripState.Failed&&!t.Board()&&t.Complete()==0,"pickup expiry no boarding or fare");
  t.Clear();t.Accept(5000,60,180);t.Tick(30);t.Board();t.Tick(20);Check(t.Remaining==40&&t.PickupRemaining==150,"boarding freezes pickup timer");Check(t.Revise(5300,80)&&t.Fare==5300&&t.Remaining==80,"request revise retains occupied state");Check(!t.Revise(5000,90),"no reduction of agreed fare");t.Tick(80);Check(!t.Revise(6000,100),"expired ride cannot regain paid time");
  Console.WriteLine("PASS: fare lifecycle, pickup deadline, request repricing and late passenger retention");
 }
}
