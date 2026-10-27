using System;
class SevereHungerTests {
 static void Check(bool b,string s){if(!b)throw new Exception(s);}
 static void Near(double a,double b,string s){Check(Math.Abs(a-b)<.000001,s+" actual="+a);}
 static void Main(){try{
  var d=new DriverCondition{satiety=9,fatigue=0};d.Advance(60);Near(d.fatigue,8,"severe hunger adds8 fatigue per minute");
  d=new DriverCondition{satiety=10,fatigue=0};d.Advance(12);Near(d.fatigue,1.6,"crossing10 at segment start uses2x");
  d=new DriverCondition{satiety=11,fatigue=0};d.Advance(24);Near(d.fatigue,2.8,"12seconds1.5x plus12seconds2x");
  d=new DriverCondition{satiety=26,fatigue=0};var fine=d.Copy();d.Advance(204);for(int i=0;i<2040;i++)fine.Advance(.1);Near(d.fatigue,20.4,"all three hunger bands integrate");Near(d.fatigue,fine.fatigue,"frame partition invariance");
  d=new DriverCondition{satiety=0,fatigue=50};d.Advance(100,true);Near(d.fatigue,50,"sleeping never accumulates hunger fatigue");
  d=new DriverCondition{satiety=0,fatigue=99};d.Advance(60);Near(d.fatigue,100,"fatigue caps100");
  Console.WriteLine("SEVERE_HUNGER_OK checks=8");
 }catch(Exception e){Console.WriteLine(e);Environment.Exit(1);}}
}
