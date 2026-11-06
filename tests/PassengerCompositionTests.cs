using System;
using System.IO;
using System.Collections.Generic;
class PassengerCompositionTests {
 static int n;
 static void Check(bool value,string name){n++;if(!value)throw new Exception(name);}
 static void Reject(Action action,string name){bool rejected=false;try{action();}catch(ArgumentException){rejected=true;}Check(rejected,name);}
 static string Preset(string traits,string pool="Drunk",string id="combo"){return "{\"version\":1,\"presets\":[{\"id\":\""+id+"\",\"name\":\"조합 승객\",\"enabled\":true,\"weight\":1,\"pool\":\""+pool+"\",\"traits\":["+traits+"],\"intro\":\"안녕하세요\"}]}";}
 static void Install(string value){var profile=ContentJson.Object(ContentJson.Parse(ContentTuning.CaptureJson()));bool found=false;foreach(var o in ContentJson.Array(profile["values"])){var row=ContentJson.Object(o);if((string)row["id"]=="passenger.presets"){row["value"]=value;found=true;}}if(!found)ContentJson.Array(profile["values"]).Add(new Dictionary<string,object>{{"id","passenger.presets"},{"value",value}});profile["contentHash"]=ContentTuning.ComputeHash(profile);ContentTuning.UseJson(ContentJson.Stringify(profile));}
 static void Main(string[] args){try{Run(args);Console.WriteLine("PASS PassengerCompositionTests "+n);}catch(Exception e){Console.WriteLine("FAIL "+e.Message);Environment.Exit(1);}}
 static void Run(string[] args){
  Check(typeof(PassengerBehaviorState).GetMethod("CreateConfigured")!=null,"Configured presets must produce executable passenger behavior");
  foreach(object fixture in ContentJson.Array(ContentJson.Object(ContentJson.Parse(File.ReadAllText(args[1])))["cases"])){var row=ContentJson.Object(fixture);bool valid=true;try{PassengerPresetCatalog.Parse(ContentJson.Stringify(row["data"]));}catch(ArgumentException){valid=false;}Check(valid==(bool)row["valid"],"shared validator fixture "+row["name"]);}
  Reject(()=>PassengerPresetCatalog.Parse(Preset("","Food").Replace("조합 승객","   ")),"blank preset name rejected");
  ContentTuning.ConfigureCatalog(File.ReadAllText(args[0]));
  var baseline=ContentTuning.CaptureJson();
  Install(File.ReadAllText(args[2]));
  foreach(PassengerType type in Enum.GetValues(typeof(PassengerType))){var a=PassengerBehaviorState.Create("default",type);var b=PassengerBehaviorState.CreateConfigured("default",type,null,null);Check(a.Type==b.Type&&a.Variant==b.Variant&&a.Dialogue==b.Dialogue&&a.Quiet==b.Quiet,"preserved legacy twelve fixture matches old behavior "+type);}
  var care=new VehicleCare();care.ReservePassenger("first-offer",new PassengerType[0]);care=VehicleCare.Restore(care.Snapshot());care.ReservePassenger("different-offer",new PassengerType[0]);Check(care.PassengerSelectionSeed=="first-offer","cancel/resume preserves selection seed");
  // Removing a newly optional key preserves a legacy profile's exact canonical snapshot.
  var old=ContentJson.Object(ContentJson.Parse(baseline));ContentJson.Array(old["values"]).RemoveAll(o=>(string)ContentJson.Object(o)["id"]=="passenger.presets");old["contentHash"]=ContentTuning.ComputeHash(old);string legacy=ContentJson.Stringify(old);ContentTuning.UseJson(legacy);Check(ContentTuning.CaptureJson()==legacy,"legacy snapshot has no injected defaults");
  foreach(PassengerType type in Enum.GetValues(typeof(PassengerType))){var a=PassengerBehaviorState.Create("same",type);var b=PassengerBehaviorState.CreateConfigured("same",type,null,null);Check(a.Type==b.Type&&a.Variant==b.Variant&&a.Dialogue==b.Dialogue,"missing key retains legacy "+type);}
  Install(Preset("\"body.drunk\",\"speech.quiet\",\"item.luggage\""));
  var p=PassengerBehaviorState.CreateConfigured("case",PassengerType.Drunk,null,null);Check(p.PresetId=="combo"&&p.Type==PassengerType.Luggage&&p.Pool==PassengerType.Drunk&&p.IsDrunk,"independent pool drunk and luggage action");Check(p.Quiet&&!p.BoardingReady&&p.Dialogue.Contains("안녕하세요")&&p.Dialogue.Contains(PassengerBehaviorState.Create("case",PassengerType.Luggage).Dialogue),"intro retains action warning");Check(p.Respond(0,true)&&p.Quiet,"quiet survives luggage response");p.Advance(10,true,false,0,100);Check(p.Board(),"actual loading completes");p.BindEffectTrip("trip");var situation=PassengerSituationState.Create("trip",0,0,0);situation.ApplyPhysicalObservation(p,1,0,0);Check(situation.Observe(1,false,true,true,p.Quiet,1,1,0,0,false)&&situation.OffersReSecure,"explicit quiet retains physical luggage safety response");Check(situation.Respond(0,true)&&situation.ReSecureRemaining>0,"quiet luggage can really resecure");p=p.Copy();p.Validate();Check(p.PresetId=="combo"&&p.Quiet,"composition survives copy");Check(p.Arrive()&&!p.Finish(true),"unloading required");p.Advance(10,true,false,1,100);Check(p.Finish(true),"unloading completes");
  Install(Preset("\"body.sensitive\",\"item.food\"","Comfort"));p=PassengerBehaviorState.CreateConfigured("meal",PassengerType.Comfort,null,null);Check(p.Type==PassengerType.Food&&p.RepresentativeType==PassengerType.Comfort,"food retains comfort request");p.Board();p.Respond(1,true);p.Advance(25,false,true,.6f,100);Check(p.ConsumptionOccurred,"food action really executes alongside comfort");
  Install(Preset("\"purpose.errand\",\"item.muddy\"","Errand"));p=PassengerBehaviorState.CreateConfigured("errand",PassengerType.Errand,null,null);Check(p.Type==PassengerType.MuddyLuggage&&p.RepresentativeType==PassengerType.Errand,"mud retains via request");p.Respond(1,true);p.Board();p.BindEffectTrip("mud-trip");p.Arrive();p.Finish(true);Check(p.DirtEvents.Length==1&&p.Copy().DirtEvents[0].Id=="mud-trip:mud","mud applies exactly once on actual exit");
  var copy=p.Copy();copy.PresetTraits[0]="purpose.hurried";Check(p.PresetTraits[0]=="purpose.errand","snapshot does not alias traits");Reject(()=>copy.Validate(),"snapshot rejects altered module association");
  Reject(()=>PassengerPresetCatalog.Parse(Preset("\"body.drunk\",\"body.sleepy\"")),"double wake rejected");
  Check(PassengerPresetCatalog.Parse(Preset("\"item.food\",\"body.sleepy\"","Food"))[0].ActionModule=="Food","sleeping food is executable");
  Reject(()=>PassengerPresetCatalog.Parse(Preset("\"purpose.errand\",\"body.sensitive\"","Errand")),"two representative requests rejected");
  Reject(()=>PassengerPresetCatalog.Parse(Preset("\"item.luggage\"","General")),"required rest cannot contain work");
  Reject(()=>PassengerPresetCatalog.Parse(Preset("\"body.drunk\"","Food")),"drunk cannot bypass its pool");
  Reject(()=>PassengerPresetCatalog.Parse(Preset("\"item.food\",\"item.food\"","Food")),"duplicate traits rejected");
  Reject(()=>PassengerPresetCatalog.Parse(Preset("\"unknown\"","Food")),"unknown trait rejected");
  Reject(()=>PassengerPresetCatalog.Parse(Preset("","Food","UPPER")),"invalid id rejected");
  Reject(()=>PassengerPresetCatalog.Parse(Preset("","Food").Replace("\"weight\":1","\"weight\":1.5")),"fraction rejected");
  Reject(()=>PassengerPresetCatalog.Parse(Preset("","Food").Replace("\"intro\":\"안녕하세요\"","\"intro\":\""+new string('x',241)+"\"")),"intro limit");
  Install("{\"version\":1,\"presets\":[]}");p=PassengerBehaviorState.CreateConfigured("none",PassengerType.Drunk,null,null);Check(p.Type==PassengerType.General&&p.Pool==PassengerType.General&&!p.IsDrunk,"empty pool immutable ordinary fallback");
  Install(Preset("\"body.drunk\",\"item.luggage\""));p=PassengerBehaviorState.CreateConfigured("again",PassengerType.Drunk,null,"first-offer");Check(p.PresetId=="combo","stable candidate selection");
  Install(Preset("\"purpose.errand\",\"item.muddy\"","Luggage"));p=PassengerBehaviorState.CreateConfigured("unavailable",PassengerType.Luggage,new PassengerType[]{PassengerType.Food},null);Check(p.Type==PassengerType.General,"unavailable request falls back without dropping selected trait");
  ContentTuning.UseJson(baseline);
 }
}
