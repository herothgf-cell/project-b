using System;
using System.Reflection;
public static class StopAlternativeTests {
 static int checks;
 static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
 static object Call(StopOccupancyPolicy p,string name,params object[] args){var m=typeof(StopOccupancyPolicy).GetMethod(name);Check(m!=null,"Missing safe alternative operation: "+name);return m.Invoke(p,args);}
 static T Field<T>(object p,string name){var f=p.GetType().GetField(name);Check(f!=null,"Missing persisted alternative field: "+name);return (T)f.GetValue(p);}
 public static int Main(){try{
  var p=new StopOccupancyPolicy();p.Begin("event","origin");
  Check(!(bool)Call(p,"AgreeAlternative","trip","wrong","alt",40f,true,true,true),"must match occupied original");
  Check(!(bool)Call(p,"AgreeAlternative","trip","origin","alt",151f,true,true,true),"walking range bound");
  Check(!(bool)Call(p,"AgreeAlternative","trip","origin","alt",40f,false,true,true),"route must be reachable");
  Check(!(bool)Call(p,"AgreeAlternative","trip","origin","alt",40f,true,false,true),"pedestrian path must be clear");
  Check(!(bool)Call(p,"AgreeAlternative","trip","origin","alt",40f,true,true,false),"bay must be empty");
  Check((bool)Call(p,"AgreeAlternative","trip","origin","alt",40f,true,true,true),"agree safe alternative");
  Call(p,"SetAlternativePickup",true);
  Check(p.Capture().version==2&&Field<string>(p.Capture(),"alternativeTrip")=="trip","versioned identity");
  Call(p,"AdvanceAlternative",10f,true);var s=p.Capture();Check(Math.Abs(Field<float>(s,"walkedMetres")-12)<.001,"real time pedestrian walking");
  Call(p,"AdvanceAlternative",10f,false);Check(Field<float>(p.Capture(),"walkedMetres")==12,"blocked pedestrian waits");
  Check(!(bool)Call(p,"AgreeAlternative","trip","origin","alt",40f,true,true,true),"repeat agreement never resets walking");
  var restored=StopOccupancyPolicy.Restore(s);Call(restored,"AdvanceAlternative",30f,true);
  Check(Field<float>(restored.Capture(),"walkedMetres")==40,"restore remaining walk completes without overrun");
  Call(restored,"BeginReturn");Check(Field<bool>(restored.Capture(),"alternativeReturning"),"cancel persists return state");
  Call(restored,"AdvanceReturn",10f,false);Check(Field<float>(restored.Capture(),"walkedMetres")==40,"blocked return waits");
  Call(restored,"AdvanceReturn",10f,true);var returning=StopOccupancyPolicy.Restore(restored.Capture());Check(Math.Abs(Field<float>(returning.Capture(),"walkedMetres")-28)<.001,"return progress survives restore");
  Call(returning,"AdvanceReturn",100f,true);Check(!returning.AlternativeActive,"return completes at original anchor");
  float protection=restored.Protection();restored.Protected("trip");Check(protection>0&&restored.Protection()==0,"deadline compensation stays once per event");
  var invalid=s.Copy();invalid.GetType().GetField("walkedMetres").SetValue(invalid,41f);bool failed=false;try{StopOccupancyPolicy.Restore(invalid);}catch(ArgumentException){failed=true;}Check(failed,"invalid saved walking rejected");
  Call(restored,"ClearAlternative");Check(!Field<bool>(restored.Capture(),"alternativeAccepted"),"new phase clears alternative");
  var legacy=StopOccupancyPolicy.Restore(new StopOccupancySnapshot{version=1});Check(!Field<bool>(legacy.Capture(),"alternativeAccepted"),"legacy neutral migration");
  Console.WriteLine("PASS StopAlternativeTests "+checks);return 0;
 }catch(Exception e){Console.Error.WriteLine("FAIL StopAlternativeTests "+e.GetBaseException().Message);return 1;}}
}
