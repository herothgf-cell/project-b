using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;

// Run with -executeMethod ContentTuningChecks.Run. No scene or user save is opened.
public static class ContentTuningChecks {
 static int checks;
 static void Check(bool value,string reason){checks++;if(!value)throw new Exception("CONTENT_TUNING "+reason);}
 static void Reject(Action run,string reason){bool rejected=false;try{run();}catch(ArgumentException){rejected=true;}Check(rejected,reason);}
 static string Variant(string baseline){var p=ContentJson.Object(ContentJson.Parse(baseline));p["id"]="runtime-check";p["revision"]=(double)p["revision"]+1;foreach(var value in ContentJson.Array(p["values"])){var row=ContentJson.Object(value);if((string)row["id"]=="common.fareMultiplier")row["value"]="2.0";if((string)row["id"]=="law.redStraight.penalty")row["value"]="37";}p["contentHash"]=ContentTuning.ComputeHash(p);return ContentJson.Stringify(p);}
 public static void Run(){
  checks=0;ContentTuningRuntime.Initialize();string original=ContentTuningRuntime.Snapshot();
  string folder=Path.Combine(Path.GetTempPath(),"taxi-content-check-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
  try{
   ContentTuningRuntime.Restore(null);string baseline=ContentTuningRuntime.Snapshot(),variant=Variant(baseline),path=Path.Combine(folder,"active-profile.json");
   File.WriteAllText(path,variant);ContentTuningRuntime.LoadActivePath(path);Check(ContentTuningRuntime.Snapshot()==ContentTuning.CaptureJson()&&ContentTuningRuntime.Snapshot().Contains("runtime-check"),"new session reads complete active profile");
   File.WriteAllText(path,"{invalid");Check(ContentTuningRuntime.Snapshot().Contains("runtime-check"),"disk editing cannot mutate running session");
   ContentTuningRuntime.LoadActivePath(path);Check(ContentTuningRuntime.Snapshot()==baseline&&ContentTuningRuntime.Status.Contains("오류"),"invalid active atomically falls back with visible error");
   File.WriteAllText(path,variant);ContentTuningRuntime.Restore(baseline);Check(ContentTuningRuntime.Snapshot()==baseline,"saved snapshot wins over newer disk");
   ContentTuningRuntime.Restore(variant);ContentTuningRuntime.Restore(null);Check(ContentTuningRuntime.Snapshot()==baseline,"legacy null restores baseline");
   ContentTuningRuntime.Restore(variant);Reject(()=>ContentTuningRuntime.Restore("{}"),"invalid saved snapshot rejected");Check(ContentTuningRuntime.Snapshot().Contains("runtime-check"),"failed restore does not leak globals");
   Reject(()=>ContentTuningRuntime.RestoreAndValidate(baseline,()=>{throw new ArgumentException("bad saved passenger");}),"invalid passenger rejects candidate");Check(ContentTuningRuntime.Snapshot().Contains("runtime-check"),"failed passenger validation restores previous whole profile");
   ContentTuningRuntime.ValidateHostSnapshot(variant,null);Reject(()=>ContentTuningRuntime.ValidateHostSnapshot("{}",null),"LAN malformed snapshot rejected");Reject(()=>ContentTuningRuntime.ValidateHostSnapshot(variant,baseline),"LAN mid-room profile mutation rejected");Reject(()=>ContentTuningRuntime.ValidateHostSnapshot(new string('x',ContentTuningRuntime.MaximumProfileBytes+1),null),"LAN oversized snapshot rejected");
   var unsupported=ContentJson.Object(ContentJson.Parse(variant));unsupported["schemaVersion"]=2d;Reject(()=>ContentTuningRuntime.ValidateHostSnapshot(ContentJson.Stringify(unsupported),null),"LAN unsupported schema rejected before world construction");unsupported["schemaVersion"]=1d;unsupported["contentHash"]=new string('0',64);Reject(()=>ContentTuningRuntime.ValidateHostSnapshot(ContentJson.Stringify(unsupported),null),"LAN modified hash rejected");
   string isolated=ContentTuningRuntime.IsolatedSavePath(Path.Combine(folder,"test-session"),"campaign.json");Check(Path.GetDirectoryName(isolated)==Path.Combine(folder,"test-session")&&Path.GetFileName(isolated)=="campaign.json","test save rooted in explicit isolated folder");
   string requested=ContentTuningRuntime.Argument("-contentProfile");Check(ContentTuningRuntime.ActivePath==(ContentTuningRuntime.TestMode&&!string.IsNullOrEmpty(requested)?Path.GetFullPath(requested):Path.GetFullPath(Path.Combine(Application.dataPath,"..","content","active-profile.json"))),"arbitrary profile path is honored only by explicit test launch");
   if(ContentTuningRuntime.TestMode)Check(Path.GetDirectoryName(ContentTuningRuntime.CampaignPath("campaign.json"))==ContentTuningRuntime.TestDirectory,"test campaign and sidecar journals share isolated directory");
#if UNITY_EDITOR
   var c=BusinessProgress.ParseCheckpoint(JsonUtility.ToJson(new CampaignCheckpoint{contentProfileJson=variant}));Check(c.contentProfileJson==variant,"checkpoint carries complete standalone profile");Check(BusinessProgress.ParseCheckpoint("{\"version\":3}").contentProfileJson==null,"legacy absent checkpoint profile remains null");
   var room=JsonUtility.FromJson<PvpNativeRoom>(JsonUtility.ToJson(new PvpNativeRoom{contentProfileJson=variant}));Check(room.contentProfileJson==variant&&PvpNativeLan.Protocol=="native-city-lan-v4","LAN full snapshot and incompatible protocol");
#endif
#if UNITY_EDITOR || CONTENT_RUNTIME_PREVIEW_CHECKS
   var sample=new ContentPreviewSample{manner=96,required=70,fare=10000,delta=5,onTime=true,start=74,clean=4,reference=120,actual=100,boardingScore=90,comfort=2,requestKind="OnTime",requestSuccess=true,eta=120,etaOption=1,lawCode="red-straight"};
   var result=ContentTuningProbe.Calculate(variant,sample);var expected=PassengerReward.CalculateCurrentFare(sample.manner,sample.required,sample.fare,sample.delta,sample.onTime,sample.start,sample.clean,sample.reference,sample.actual,sample.unsafeDriving,sample.boardingScore,sample.comfort);Check(result.success&&result.reward.Total==expected.Total&&result.requestDelta==PassengerRequest.Delta(RequestKind.OnTime,true),"preview uses actual C# fare and request calculations");
   var defaultPreview=ContentTuningProbe.Calculate(baseline,sample);Check(result.total!=defaultPreview.total&&result.lawPenalty==37,"modified profile changes actual preview fare and law event");Check(ContentTuningRuntime.Snapshot().Contains("runtime-check"),"temporary preview profile cannot change session snapshot");
   Reject(()=>ContentTuningProbe.ParseAndCalculate("{\"profile\":"+variant+",\"sample\":{\"fare\":10000.00001}}"),"fractional sample integer cannot round into valid fare");
   Reject(()=>ContentTuningProbe.ParseAndCalculate("{\"profile\":"+variant+",\"sample\":{\"unknown\":1}}"),"unknown preview sample member rejected");
   Reject(()=>ContentTuningProbe.ParseAndCalculate("{\"profile\":"+variant.Replace("\"schemaVersion\":1","\"schemaVersion\":2")+",\"sample\":{}}"),"preview validates full original profile schema");
   Reject(()=>ContentTuningProbe.ParseAndCalculate("{\"profile\":"+variant+",\"sample\":{\"lawCode\":\"imaginary\"}}"),"unsupported actual law calculation rejected");
#endif
   Debug.Log("CONTENT_TUNING_RUNTIME_CHECKS_OK checks="+checks);
  }finally{ContentTuningRuntime.Restore(original);Directory.Delete(folder,true);}
 }
}
