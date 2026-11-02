using System;
public static class SlowRightTurnTests {
 static int count;
 static void Check(bool value,string why){count++;if(!value)throw new Exception(why);}
 static Type Kind;
 static object New(){return Activator.CreateInstance(Kind);}
 static void Observe(object state,float dt,float speed,bool turning,bool known,bool necessary){Kind.GetMethod("Observe").Invoke(state,new object[]{dt,speed,turning,known,necessary});}
 static string Verdict(object state){return Kind.GetProperty("Verdict").GetValue(state,null).ToString();}
 public static int Main(){try{Kind=typeof(SlowRightTurnTests).Assembly.GetType("SlowRightTurnState");Check(Kind!=null,"missing explicitly scoped game slow-turn observation");
  var straight=New();Observe(straight,2,50,false,true,false);Check(Verdict(straight)=="NoOpportunity","straight approach is not right-turn speeding");
  var slow=New();Observe(slow,1,10,true,true,false);Check(Verdict(slow)=="Compliant","observable slow right turn");
  var spike=New();Observe(spike,.2f,30,true,true,false);Observe(spike,.8f,8,true,true,false);Check(Verdict(spike)=="Compliant","short sensor spike is not sustained violation");
  var fast=New();Observe(fast,.4f,20,true,true,false);Check(Verdict(fast)=="NoOpportunity","insufficient observed time");Observe(fast,.4f,20,true,true,false);Check(Verdict(fast)=="Violation","sustained game limit exceeded");
  var stopped=New();Observe(stopped,.4f,20,true,true,false);Observe(stopped,.1f,0,false,true,false);Observe(stopped,.4f,20,true,true,false);Check(Verdict(stopped)=="Compliant","stopping breaks continuous excess");
  var interrupted=New();Observe(interrupted,.4f,20,true,true,false);Observe(interrupted,.1f,20,true,false,false);var restored=Kind.GetMethod("Copy").Invoke(interrupted,null);Kind.GetMethod("Validate").Invoke(restored,null);Observe(restored,.4f,20,true,true,false);Check(Verdict(restored)=="Compliant","unknown gap remains interrupted after checkpoint copy");
  foreach(float bad in new[]{float.NaN,float.PositiveInfinity,-1f}){var invalid=New();Observe(invalid,.4f,20,true,true,false);Observe(invalid,.1f,bad,true,true,false);Observe(invalid,.4f,20,true,true,false);Check(Verdict(invalid)=="Compliant","invalid speed breaks continuous excess");}
  foreach(float bad in new[]{float.NaN,float.PositiveInfinity,-1f,0f}){var invalid=New();Observe(invalid,.4f,20,true,true,false);Observe(invalid,bad,20,true,true,false);Observe(invalid,.4f,20,true,true,false);Check(Verdict(invalid)=="Compliant","invalid duration breaks continuous excess");}
  Observe(fast,.1f,0,false,true,false);Check(Verdict(fast)=="Violation","interruption preserves a latched violation");
  var safe=New();Observe(safe,1,20,true,true,true);Check(Verdict(safe)=="Deferred","necessary safety avoidance not auto-blamed");
  Observe(safe,.1f,0,false,true,false);Check(Verdict(safe)=="Deferred","interruption preserves necessary safety avoidance");
  var unknown=New();Observe(unknown,1,20,true,false,false);Check(Verdict(unknown)=="NoOpportunity","unknown approach not scored");
  var copy=Kind.GetMethod("Copy").Invoke(fast,null);Check(Verdict(copy)=="Violation","mid-turn checkpoint retains evidence");Observe(copy,0,20,true,true,false);Check(Verdict(copy)=="Violation","pause cannot create new evidence");
  Console.WriteLine("PASS SlowRightTurnTests "+count);return 0;
 }catch(Exception e){Console.Error.WriteLine("FAIL SlowRightTurnTests "+e.GetBaseException().Message);return 1;}}
}
