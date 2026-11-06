using System;
using System.IO;
class RainSettingsTests {
 static int n;static void Check(bool ok,string why){if(!ok)throw new Exception(why);n++;}
 static void Main(string[] args){ContentTuning.ConfigureCatalog(File.ReadAllText(args[0]));ContentTuning.UseDefaults();string before=ContentTuning.CaptureJson();var draft=RainSettings.Capture();draft.enabled=true;draft.boardingDirt=4;draft.brakeMultiplier=.82f;
 Check(!RainPolicy.Enabled,"draft does not mutate live");string json=draft.Profile(before);ContentTuning.ValidateJson(json);ContentTuning.UseJson(json);Check(RainPolicy.Enabled&&RainPolicy.BoardingDirt==4,"weather applied");Check(Math.Abs(RainPolicy.BrakeMultiplier(10)-.82)<.001,"brake applied");
 var old=ContentJson.Object(ContentJson.Parse(before));var next=ContentJson.Object(ContentJson.Parse(json));var a=ContentJson.Array(old["values"]);var b=ContentJson.Array(next["values"]);foreach(var x in a){var r=ContentJson.Object(x);if(((string)r["id"]).StartsWith("weather."))continue;bool found=false;foreach(var y in b){var q=ContentJson.Object(y);if((string)q["id"]==(string)r["id"]){found=true;Check((string)q["value"]==(string)r["value"],"unrelated value preserved");break;}}Check(found,"unrelated id retained");}
 var copy=draft.Copy();copy.intensity=.2f;Check(draft.intensity!=copy.intensity,"draft independent");draft.intensity=float.NaN;bool rejected=false;try{draft.Profile(before);}catch(ArgumentException){rejected=true;}Check(rejected,"invalid values rejected");
 ContentTuning.UseJson(before);Check(!RainPolicy.Enabled,"rollback dry");Console.WriteLine("RAIN_SETTINGS_OK checks="+n);
 }
}
