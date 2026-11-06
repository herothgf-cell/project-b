using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable] public sealed class ContentPreviewSample {
 public int manner=100,required=70,fare=10000,delta,start=70,clean,boardingScore=100,comfort,etaOption;
 public bool onTime=true,unsafeDriving,requestSuccess=true;
 public float reference=120,actual=120,eta=120;
 public string requestKind="OnTime",lawCode="red-straight";
}
[Serializable] public sealed class ContentPreviewResult {
 public bool success;public string profileHash,error;public PassengerReward reward;public int total,requestDelta,lawPenalty;public float etaSeconds;public string lawReason;
}

// Early command adapter and a small status strip. It does not edit gameplay UI.
public sealed class ContentTuningProbe:MonoBehaviour {
 public static bool PreviewMode {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-contentPreview")>=0;}}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
 static void Preview(){
  if(!PreviewMode)return;
  string output=ContentTuningRuntime.Argument("-contentPreviewOutput");ContentPreviewResult result;
  try{
   string input=ContentTuningRuntime.Argument("-contentPreview");if(string.IsNullOrEmpty(input)||string.IsNullOrEmpty(output))throw new ArgumentException("-contentPreview input and -contentPreviewOutput output are required");
   if(new FileInfo(input).Length>1024*1024)throw new ArgumentException("Preview input exceeds 1 MiB");
   result=ParseAndCalculate(File.ReadAllText(input,new UTF8Encoding(false,true)));
  }catch(Exception e){result=new ContentPreviewResult{success=false,error=e.Message};}
  try{if(string.IsNullOrEmpty(output))throw new ArgumentException("Preview output path missing");output=Path.GetFullPath(output);Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,JsonUtility.ToJson(result,true),new UTF8Encoding(false));}
  catch(Exception e){Debug.LogError("CONTENT_PREVIEW_OUTPUT_ERROR "+e.Message);Application.Quit(2);return;}
  Debug.Log("CONTENT_PREVIEW_"+(result.success?"OK":"FAILED")+" "+(result.error??result.profileHash));Application.Quit(result.success?0:1);
 }
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Install(){if(!PreviewMode){var go=new GameObject("Content settings status");DontDestroyOnLoad(go);go.AddComponent<ContentTuningProbe>();}}
 static bool Known(string key,params string[] names){return Array.IndexOf(names,key)>=0;}
 public static ContentPreviewResult ParseAndCalculate(string input){
  var root=ContentJson.Object(ContentJson.Parse(input));foreach(string k in root.Keys)if(!Known(k,"profile","sample"))throw new ArgumentException("Unknown preview member "+k);
  if(!root.ContainsKey("profile")||!root.ContainsKey("sample"))throw new ArgumentException("Full profile and sample are required");var sample=ContentJson.Object(root["sample"]);
  foreach(string k in sample.Keys)if(!Known(k,"manner","required","fare","delta","onTime","start","clean","reference","actual","unsafeDriving","boardingScore","comfort","requestKind","requestSuccess","eta","etaOption","lawCode"))throw new ArgumentException("Unknown sample member "+k);
  var s=new ContentPreviewSample{manner=Int(sample,"manner",100,0,100),required=Int(sample,"required",70,1,100),fare=Int(sample,"fare",10000,0,10000000),delta=Int(sample,"delta",0,-100,100),onTime=Flag(sample,"onTime",true),start=Int(sample,"start",70,0,100),clean=Int(sample,"clean",0,0,100),reference=Number(sample,"reference",120,0,86400),actual=Number(sample,"actual",120,0,86400),unsafeDriving=Flag(sample,"unsafeDriving",false),boardingScore=Int(sample,"boardingScore",100,0,100),comfort=Int(sample,"comfort",0,0,100),requestKind=Text(sample,"requestKind","OnTime"),requestSuccess=Flag(sample,"requestSuccess",true),eta=Number(sample,"eta",120,0,86400),etaOption=Int(sample,"etaOption",0,-1,1),lawCode=Text(sample,"lawCode","red-straight")};
  return Calculate(ContentJson.Stringify(root["profile"]),s);
 }
 static int Int(Dictionary<string,object> d,string k,int fallback,int min,int max){object value;if(!d.TryGetValue(k,out value))return fallback;if(!(value is double))throw new ArgumentException("Expected integer sample "+k);double n=(double)value;if(double.IsNaN(n)||double.IsInfinity(n)||n<min||n>max||n!=Math.Truncate(n))throw new ArgumentException("Expected integer/range sample "+k);return (int)n;}
 static float Number(Dictionary<string,object> d,string k,float fallback,float min,float max){object value;if(!d.TryGetValue(k,out value))return fallback;if(!(value is double))throw new ArgumentException("Expected number sample "+k);double n=(double)value;if(double.IsNaN(n)||double.IsInfinity(n)||n<min||n>max)throw new ArgumentException("Sample range "+k);return (float)n;}
 static bool Flag(Dictionary<string,object> d,string k,bool fallback){object value;if(!d.TryGetValue(k,out value))return fallback;if(!(value is bool))throw new ArgumentException("Expected boolean sample "+k);return (bool)value;}
 static string Text(Dictionary<string,object> d,string k,string fallback){object value;if(!d.TryGetValue(k,out value))return fallback;if(!(value is string)||((string)value).Length>100)throw new ArgumentException("Expected short text sample "+k);return (string)value;}
 public static ContentPreviewResult Calculate(string profile,ContentPreviewSample sample){
  if(sample==null)throw new ArgumentException("Sample missing");string previous=ContentTuningRuntime.Snapshot();
  try{
   ContentTuningRuntime.Restore(profile);RequestKind kind;if(!Enum.TryParse(sample.requestKind,false,out kind)||!Enum.IsDefined(typeof(RequestKind),kind))throw new ArgumentException("Unsupported request kind");
   var reward=PassengerReward.CalculateCurrentFare(sample.manner,sample.required,sample.fare,sample.delta,sample.onTime,sample.start,sample.clean,sample.reference,sample.actual,sample.unsafeDriving,sample.boardingScore,sample.comfort);
   var law=LawSample(sample.lawCode);return new ContentPreviewResult{success=true,profileHash=(string)ContentJson.Object(ContentJson.Parse(ContentTuningRuntime.Snapshot()))["contentHash"],reward=reward,total=reward.Total,requestDelta=PassengerRequest.Delta(kind,sample.requestSuccess),etaSeconds=PassengerRequest.EtaSeconds(sample.eta,sample.etaOption),lawPenalty=law==null?0:law.Deduction,lawReason=law==null?"사건 없음":law.reason};
  }finally{ContentTuningRuntime.Restore(previous);}
 }
 static LawEvent LawSample(string code){
  var o=new RoadObservation{known=true,trafficClear=true,forward=SignalColor.Red,right=SignalColor.Red,pedestrian=PedestrianState.None};
  switch(code){
   case "none":return null;
   case "red-straight":return LawEvaluator.Signal("content-preview",1,o,false,SignalColor.Red);
   case "stop-line":return LawEvaluator.StopLine("content-preview",1,o);
   case "right-no-stop":return LawEvaluator.RightTurn("content-preview",1,o,false);
   case "dedicated-red":o.dedicated=true;return LawEvaluator.RightTurn("content-preview",1,o,true);
   case "pedestrian":o.forward=SignalColor.Green;o.pedestrian=PedestrianState.Crossing;return LawEvaluator.RightTurn("content-preview",1,o,true);
   case "insertion":return LawEvaluator.Exit("content-preview",1,true,true,true,false);
   case "interference":return LawEvaluator.Exit("content-preview",1,true,true,false,true);
   default:throw new ArgumentException("Unsupported lawCode; use none, red-straight, stop-line, right-no-stop, dedicated-red, pedestrian, insertion, interference");
  }
 }
}
