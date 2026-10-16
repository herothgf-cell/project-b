using System;
using System.Reflection;
class PracticeConditionsTests {
 static void Check(bool v,string name){if(!v)throw new Exception(name);}
 static object Call(object o,string n,params object[] a){return o.GetType().GetMethod(n).Invoke(o,a);}
 static RoadObservation Observe(object o){return (RoadObservation)Call(o,"Observe");}
 static void Main(){try{
  Type t=typeof(LawLedger).Assembly.GetType("RightPracticeState");Check(t!=null,"missing arrival-gated practice state");
  for(int variant=1;variant<=6;variant++){
   var state=Activator.CreateInstance(t,new object[]{variant});Call(state,"Advance",180f,false);
   var o=Observe(state);if(variant==1)Check(o.forward==SignalColor.Red,"R1 stays red during slow approach");
   if(variant==3)Check(o.right==SignalColor.Red,"R3 waits for actual stopping point");
   if(variant>=5)Check(o.pedestrian!=PedestrianState.None,"pedestrian cannot expire before arrival");
   Call(state,"Advance",0f,true);Call(state,"Advance",180f,false);o=Observe(state);
   if(variant==1)Check(o.forward==SignalColor.Red,"R1 trains red turn without automatic green");
   if(variant==3)Check(o.right==SignalColor.Green,"R3 releases after local stop");
   if(variant>=5)Check(o.pedestrian==PedestrianState.None,"pedestrian completes crossing after stop");
   var fresh=Activator.CreateInstance(t,new object[]{variant});Check(variant<5||Observe(fresh).pedestrian!=PedestrianState.None,"retry restores starting conditions");
  }
  Console.WriteLine("PASS arrival-gated practice conditions and retry");
 }catch(Exception e){Console.WriteLine(e.Message);Environment.Exit(1);}}
}
