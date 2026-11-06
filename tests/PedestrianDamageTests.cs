using System;
using System.Reflection;

public static class PedestrianDamageTests {
 static int checks;
 static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static MethodInfo Required(Type t,string name){var m=t.GetMethod(name);Check(m!=null,"Missing "+t.Name+"."+name);return m;}
 static object Invoke(object o,string method,params object[] args){return Required(o.GetType(),method).Invoke(o,args);}
 static int Field(object o,string n){var f=o.GetType().GetField(n);Check(f!=null,"Missing field "+n);return (int)f.GetValue(o);}
 static AccidentState Person(string id,AccidentSeverity severity,int roll){var s=new AccidentState();Check(s.BeginResolved(id,"waiting:0",AccidentSeverityPolicy.Resolve(AccidentKind.Person,severity)),"new event");Check((bool)Invoke(s,"SelectPedestrianDamage",roll),"select damage once");return s;}
 static VehicleDamage Clone(VehicleDamage damage){return VehicleDamage.Restore(damage.Snapshot());}
 public static void Main(){try{Run();Console.WriteLine("PedestrianDamageTests PASS "+checks+" checks");}catch(Exception e){Console.WriteLine("PedestrianDamageTests FAIL "+e.GetBaseException());Environment.ExitCode=1;}}
 public static void Run(){
  var eligibility=typeof(AccidentState).GetField("pedestrianDamageEligible");Check(eligibility!=null,"new incidents need an eligibility marker distinct from saved old physical incidents");var oldPhysical=new AccidentState();oldPhysical.BeginResolved("old-physical","wall",AccidentSeverityPolicy.Resolve(AccidentKind.Property,AccidentSeverity.Moderate));eligibility.SetValue(oldPhysical,false);var promoted=AccidentSeverityPolicy.Resolve(AccidentKind.Person,AccidentSeverity.Moderate);oldPhysical.accidentKind=promoted.Kind;oldPhysical.selectedFee=promoted.Fee;oldPhysical.selectedSeconds=promoted.Seconds;oldPhysical.selectedScorePenalty=promoted.ScorePenalty;oldPhysical.selectedTowRequired=promoted.TowRequired;oldPhysical.remaining=promoted.Seconds;Check(!(bool)Invoke(oldPhysical,"SelectPedestrianDamage",999),"legacy physical to person promotion never adds new damage");oldPhysical.Validate();
  var sample=Required(typeof(AccidentSeverityPolicy),"SamplePedestrianDamage");
  for(int level=0;level<3;level++){int max=new[]{2,5,10}[level];var bins=new int[max+1];for(int roll=0;roll<1000;roll++){int n=(int)sample.Invoke(null,new object[]{(AccidentSeverity)level,roll});Check(n>=0&&n<=max,"bounded damage");bins[n]++;}foreach(int count in bins)Check(count>=1000/(max+1)&&count<=1000/(max+1)+1,"uniform integer buckets");Check((int)sample.Invoke(null,new object[]{(AccidentSeverity)level,0})==0&&(int)sample.Invoke(null,new object[]{(AccidentSeverity)level,999})==max,"zero/max included");}
  var state=Person("positive",AccidentSeverity.Major,999);Check(Field(state,"selectedDamage")==10&&!(bool)Invoke(state,"SelectPedestrianDamage",0),"cannot reroll");var car=new VehicleDamage();Check((int)Invoke(state,"ApplyVehicleDamage",car)==10&&car.Durability==90,"actual damage");Check((int)Invoke(state,"ApplyVehicleDamage",car)==0&&car.Durability==90,"same state idempotent");state.Validate();
  var replay=Person("positive",AccidentSeverity.Major,999);car=Clone(car);Check((int)Invoke(replay,"ApplyVehicleDamage",car)==0&&car.Durability==90,"snapshot ledger deduplicates event without applied flag");
  var repair=typeof(VehicleDamage).GetMethod("Repair",BindingFlags.NonPublic|BindingFlags.Instance);repair.Invoke(car,null);replay=Person("positive",AccidentSeverity.Major,999);Check((int)Invoke(replay,"ApplyVehicleDamage",car)==0&&car.Durability==100,"repair cannot reapply old incident");
  var zero=Person("zero",AccidentSeverity.Minor,0);Check((int)Invoke(zero,"ApplyVehicleDamage",car)==0&&car.Durability==100,"zero damage still resolves");zero.Validate();
  car.Impact(15);car.Advance(1,false);car.Impact(15);Check(car.Durability==0,"disabled fixture");var disabled=Person("disabled",AccidentSeverity.Major,999);Check((int)Invoke(disabled,"ApplyVehicleDamage",car)==0&&car.Durability==0,"never negative");
  car=new VehicleDamage();Invoke(car,"ApplyAccidentLoss","wear",99);var last=Person("last",AccidentSeverity.Moderate,999);Check((int)Invoke(last,"ApplyVehicleDamage",car)==1&&car.Durability==0&&Field(last,"appliedDamage")==1,"actual loss capped at remaining durability");
  var legacy=new AccidentState();legacy.BeginResolved("old","waiting:0",AccidentSeverityPolicy.Resolve(AccidentKind.Person,AccidentSeverity.Major));car=new VehicleDamage();Check((int)Invoke(legacy,"ApplyVehicleDamage",car)==0&&car.Durability==100,"existing pending severity event has no retroactive damage");legacy.Validate();
  var wall=new AccidentState();wall.BeginResolved("wall","wall",AccidentSeverityPolicy.Resolve(AccidentKind.Property,AccidentSeverity.Major));Check(!(bool)Invoke(wall,"SelectPedestrianDamage",999)&&(int)Invoke(wall,"ApplyVehicleDamage",car)==0,"physical vehicle/property damage stays separate");
  var copy=state.Copy();Check(Field(copy,"selectedDamage")==10&&Field(copy,"appliedDamage")==10,"copy retains selected and actual damage");copy.Validate();
 }
}
