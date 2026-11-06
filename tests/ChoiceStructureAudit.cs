using System;
using System.IO;
using System.Collections.Generic;
public static class ChoiceStructureAudit {
 static int checks;
 static void Check(bool value,string label){if(!value)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
 static PassengerRequest Accepted(RequestKind kind,PassengerReply reply){var r=new PassengerRequest();r.Begin("audit-"+kind+reply,kind);r.Accept("test",reply);return r;}
 static string Change(string id,string value,bool add){var p=ContentJson.Object(ContentJson.Parse(ContentTuning.CaptureJson()));var rows=ContentJson.Array(p["values"]);bool found=false;foreach(object x in rows){var row=ContentJson.Object(x);if((string)row["id"]==id){row["value"]=value;found=true;}}if(add&&!found)rows.Add(new Dictionary<string,object>{{"id",id},{"value",value}});p["contentHash"]=ContentTuning.ComputeHash(p);return ContentJson.Stringify(p);}
 static void Rejected(string json,string label){bool rejected=false;try{ContentTuning.ValidateJson(json);}catch(ArgumentException){rejected=true;}Check(rejected,label);}
 public static int Main(string[] args){try{
  ContentTuning.ConfigureCatalog(File.ReadAllText(args[0]));
  foreach(PassengerReply reply in new[]{PassengerReply.Accept,PassengerReply.Alternative}){
   var r=Accepted(RequestKind.Empathy,reply);Check(r.SatisfactionDelta==0,"Empathy "+reply+" immediate satisfaction remains 0");
   Check(r.Snapshot().reply==reply,"selected reply is persisted "+reply);
   r.CompleteGoal(true,"answered");Check(r.SatisfactionDelta==5,"Empathy "+reply+" shared completion +5");
  }
  var unanswered=new PassengerRequest();unanswered.Begin("unanswered",RequestKind.Empathy);unanswered.ObserveOpportunity(true);unanswered.Finish();Check(unanswered.SatisfactionDelta==-5,"Empathy unanswered opportunity applies -5");
  var deferred=new PassengerRequest();deferred.Begin("deferred",RequestKind.Empathy);deferred.DeferSafety();deferred.CompleteGoal(true,"ignored");deferred.Finish();Check(deferred.SatisfactionDelta==0,"Empathy safety defer stays 0");
  foreach(PassengerReply reply in new[]{PassengerReply.EtaFaster,PassengerReply.EtaNormal,PassengerReply.EtaRelaxed}){var r=Accepted(RequestKind.Eta,reply);r.CompleteGoal(true,"ETA explanation");Check(r.SatisfactionDelta==0,"ETA "+reply+" completion stays 0");}
  Rejected(Change("request.Eta.success","5",true),"ETA satisfaction override is rejected by actual catalog/profile validation");
  Rejected(Change("request.Empathy.reply.Accept.satisfaction","5",true),"independent per-reply score key is unsupported");
  ContentTuning.UseJson(Change("request.Empathy.success","11",false));
  foreach(PassengerReply reply in new[]{PassengerReply.Accept,PassengerReply.Alternative}){var r=Accepted(RequestKind.Empathy,reply);r.CompleteGoal(true,"answered");Check(r.SatisfactionDelta==11,"editing request score changes both replies "+reply);}
  ContentTuning.UseJson(Change("request.Empathy.success","0",false));var zero=Accepted(RequestKind.Empathy,PassengerReply.Accept);zero.CompleteGoal(true,"answered");Check(zero.SatisfactionDelta==0,"zero score means no change");
  Console.WriteLine("CHOICE_STRUCTURE_AUDIT_OK checks="+checks);return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
