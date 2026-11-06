using System;
using System.Reflection;
using UnityEngine;

// Reflection keeps this acceptance test compilable against the pre-severity model.
public static class AccidentSeverityChecks {
 static int checks;
 static Type kindType,severityType,policyType,resolutionType;
 static void Check(bool value,string label){checks++;if(!value)throw new Exception("ACCIDENT_SEVERITY: "+label);}
 static object EnumValue(Type type,string value){return Enum.Parse(type,value);}
 static object Resolve(string kind,string severity){return policyType.GetMethod("Resolve").Invoke(null,new[]{EnumValue(kindType,kind),EnumValue(severityType,severity)});}
 static object Sample(string kind,int roll){return policyType.GetMethod("Sample").Invoke(null,new[]{EnumValue(kindType,kind),(object)roll});}
 static object Read(object value,string name){var p=value.GetType().GetProperty(name);Check(p!=null,"property "+name);return p.GetValue(value,null);}
 static void Reject(Action action,string label){bool rejected=false;try{action();}catch(ArgumentException){rejected=true;}catch(TargetInvocationException e){rejected=e.InnerException is ArgumentException;}Check(rejected,label);}
 static bool Begin(AccidentState state,string id,object resolution){return (bool)typeof(AccidentState).GetMethod("BeginResolved").Invoke(state,new[]{(object)id,"victim",resolution});}
 static EconomyLedger Ledger(int cash){return new EconomyLedger(new PrototypeSettings{startCash=cash,oneDay=true,days=3,daySeconds=1200,parking=0,initialFuel=50});}
 static bool Charge(EconomyLedger ledger,string id,int fee){return (bool)typeof(EconomyLedger).GetMethod("ChargeAccident",new[]{typeof(string),typeof(int)}).Invoke(ledger,new object[]{id,fee});}
 static bool Complete(EconomyLedger ledger,string id,double seconds){return (bool)typeof(EconomyLedger).GetMethod("CompleteAccident",new[]{typeof(string),typeof(double)}).Invoke(ledger,new object[]{id,seconds});}
 public static void Run(){
  checks=0;var assembly=typeof(AccidentState).Assembly;
  kindType=assembly.GetType("AccidentKind");severityType=assembly.GetType("AccidentSeverity");policyType=assembly.GetType("AccidentSeverityPolicy");resolutionType=assembly.GetType("AccidentResolution");
  Check(kindType!=null&&severityType!=null&&policyType!=null&&resolutionType!=null,"severity policy API exists");
  var names=new[]{"Minor","Moderate","Major"};int[][] fees={new[]{3000,8000,15000},new[]{1000,3000,6000},new[]{1000,3000,6000}};int[][] seconds={new[]{45,120,240},new[]{15,45,90},new[]{15,45,90}};int[][] penalties={new[]{10,25,40},new[]{3,8,15},new[]{3,8,15}};
  var kinds=new[]{"Person","Vehicle","Property"};
  for(int k=0;k<kinds.Length;k++)for(int s=0;s<names.Length;s++){
   var value=Resolve(kinds[k],names[s]);Check(Read(value,"Kind").ToString()==kinds[k]&&Read(value,"Severity").ToString()==names[s],"selected identity");
   Check(Convert.ToInt32(Read(value,"Fee"))==fees[k][s]&&Convert.ToDouble(Read(value,"Seconds"))==seconds[k][s]&&Convert.ToInt32(Read(value,"ScorePenalty"))==penalties[k][s],"approved band "+kinds[k]+names[s]);
   Check((bool)Read(value,"TowRequired")== (s==2),"major transfer only");
   if(s>0)Check(fees[k][s]>fees[k][s-1]&&seconds[k][s]>seconds[k][s-1]&&penalties[k][s]>penalties[k][s-1],"strictly increasing severity");
   if(k>0)Check(fees[k][s]<fees[0][s]&&seconds[k][s]<seconds[0][s]&&penalties[k][s]<penalties[0][s],"property below person");
  }
  foreach(var p in resolutionType.GetProperties())Check(p.GetSetMethod(false)==null,"immutable resolution "+p.Name);
  foreach(var kind in kinds){var counts=new int[3];for(int roll=0;roll<1000;roll++){var v=Sample(kind,roll);counts[Array.IndexOf(names,Read(v,"Severity").ToString())]++;}var expected=kind=="Person"?new[]{600,300,100}:new[]{700,250,50};for(int i=0;i<3;i++)Check(counts[i]==expected[i],"bounded sample counts "+kind+names[i]);}
  Reject(()=>Sample("Person",-1),"negative roll rejected");Reject(()=>Sample("Person",1000),"overflow roll rejected");
  Reject(()=>policyType.GetMethod("Resolve").Invoke(null,new[]{Enum.ToObject(kindType,99),EnumValue(severityType,"Minor")}),"unknown kind rejected");
  Reject(()=>policyType.GetMethod("Resolve").Invoke(null,new[]{EnumValue(kindType,"Person"),Enum.ToObject(severityType,99)}),"unknown severity rejected");
  var state=new AccidentState();Check(Begin(state,"chosen",Sample("Person",999)),"begin sampled event");Check(!Begin(state,"other",Resolve("Vehicle","Minor")),"active result cannot reroll");
  state.Validate();var copy=state.Copy();copy.Validate();Check((int)Read(copy,"Fee")==15000&&(double)Read(copy,"ProcessingDuration")==240&&(int)Read(copy,"ScorePenalty")==40,"copy retains chosen result");
  string json=JsonUtility.ToJson(copy);var restored=JsonUtility.FromJson<AccidentState>(json);restored.Validate();Check((int)Read(restored,"Fee")==15000&&Read(restored,"Severity").ToString()=="Major","JSON restore never rerolls");
  restored.MarkStopped(true);Check(restored.AdvanceProcessing(999)==240,"dynamic processing clamp");restored.chargeApplied=restored.outcomeApplied=true;Check(!restored.Complete(),"major cannot finish before transfer");typeof(AccidentState).GetField("workshopTransferred").SetValue(restored,true);Check(restored.Complete(),"major finishes after transfer");restored.Validate();
  foreach(string field in new[]{"selectedFee","selectedSeconds","selectedScorePenalty","resolutionVersion"}){var invalid=copy.Copy();var f=typeof(AccidentState).GetField(field);Check(f!=null,"persisted "+field);f.SetValue(invalid,Convert.ChangeType(-1,f.FieldType));Reject(()=>invalid.Validate(),"corrupt selected field rejected "+field);}
  var badTow=copy.Copy();typeof(AccidentState).GetField("selectedTowRequired").SetValue(badTow,false);Reject(()=>badTow.Validate(),"corrupt transfer policy rejected");
  foreach(string field in new[]{"accidentKind","accidentSeverity"}){var invalid=copy.Copy();var f=typeof(AccidentState).GetField(field);Check(f!=null,"persisted identity "+field);f.SetValue(invalid,Enum.ToObject(f.FieldType,99));Reject(()=>invalid.Validate(),"unknown stored identity rejected "+field);}
  var wrongTime=copy.Copy();wrongTime.remaining=45;Reject(()=>wrongTime.Validate(),"timer must use selected duration");
  var legacy=new AccidentState();Check(legacy.Begin("legacy","victim"),"legacy begin");legacy.Validate();Check((double)Read(legacy,"ProcessingDuration")==45&&(int)Read(legacy,"Fee")==3000&&!(bool)Read(legacy,"TowRequired"),"legacy 45/3000 preserved");
  var old=JsonUtility.FromJson<AccidentState>("{\"eventId\":\"old\",\"targetId\":\"victim\",\"phase\":3,\"elapsed\":10,\"remaining\":35,\"contactLatched\":true}");old.Validate();Check((double)Read(old,"ProcessingDuration")==45&&(int)Read(old,"Fee")==3000,"old pending snapshot preserved");
  var l=Ledger(1000);Check(Charge(l,"major",15000),"dynamic charge");Check(l.Account.Cash==0&&l.Account.Debt==14000&&l.Account.Expense(1,"accident")==15000,"insufficient cash becomes debt");Check(!Charge(l,"major",1000),"cannot recharge changed fee");Check(!Complete(l,"major",double.NaN)&&!Complete(l,"major",-1),"invalid duration cannot commit completion");Check(Complete(l,"major",240),"dynamic completion");Check(l.Elapsed==240&&l.Metrics.Capture().serviceSeconds==240,"single accounts chosen time once");Check(!Complete(l,"major",240)&&l.Elapsed==240,"completion replay no time");
  l=EconomyLedger.RestoreCampaign(l.Snapshot(),l.Account.Snapshot());Check(!Charge(l,"major",15000)&&!Complete(l,"major",240)&&l.Elapsed==240,"restored exactly once accounting");
  var pvp=Ledger(0);pvp.EnablePvpProfile();Charge(pvp,"partial",6000);pvp.AdvanceAccidentProcessing(3);Check(Complete(pvp,"partial",90)&&pvp.Elapsed==3,"PVP completion does not repeat host time");Check(!Complete(pvp,"partial",90)&&pvp.Account.Debt==6000,"PVP idempotence");
  var invalidFee=Ledger(1000);Check(!Charge(invalidFee,"zero",0)&&!Charge(invalidFee,"negative",-1)&&invalidFee.Account.Entries.Length==0,"invalid fee creates no entries");
  Debug.Log("ACCIDENT_SEVERITY_OK checks="+checks);
 }
}
