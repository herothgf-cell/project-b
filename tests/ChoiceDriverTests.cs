using System;
public static class ChoiceDriverTests {
 static int checks;static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
 public static int Main(){try{
  var normal=new DriverCondition{fatigue=10,satiety=80};var zero=normal.Copy();zero.choiceFatigueGrowthPercent=0;normal.Advance(60);zero.Advance(60);Check(normal.fatigue==zero.fatigue,"zero modifier preserves exact natural fatigue");
  var doubled=new DriverCondition{fatigue=10,satiety=80,choiceFatigueGrowthPercent=100};doubled.Advance(60);Check(Math.Abs(doubled.fatigue-18)<1e-8,"100 percent modifier doubles natural fatigue");
  var stopped=new DriverCondition{fatigue=10,satiety=80,choiceFatigueGrowthPercent=-100};stopped.Advance(60);Check(stopped.fatigue==10&&stopped.satiety<80,"minus100 stops fatigue growth without stopping hunger");
  var split=new DriverCondition{fatigue=10,satiety=80,choiceFatigueGrowthPercent=100};for(int i=0;i<60;i++)split.Advance(1);Check(Math.Abs(split.fatigue-doubled.fatigue)<1e-8,"natural multiplier is frame independent");
  Check(split.Copy().choiceFatigueGrowthPercent==100,"copy preserves effective multiplier");split.choiceFatigueGrowthPercent=double.NaN;bool rejected=false;try{split.Validate();}catch(ArgumentException){rejected=true;}Check(rejected,"nonfinite modifier rejected");
  Console.WriteLine("CHOICE_DRIVER_TESTS_OK checks="+checks);return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
