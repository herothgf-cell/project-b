using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable] public sealed class ContentPreviewSample {
 public int manner=100,required=70,fare=10000,delta,start=70,clean,boardingScore=100,comfort,etaOption;
 public bool onTime=true,unsafeDriving,requestSuccess=true;
 public float reference=120,actual=120,eta=120;
 public string requestKind="OnTime",lawCode="red-straight",choiceId="";
 public float fatigue,seconds;public int outcome=1;
}
[Serializable] public sealed class ContentChoicePreviewResult {
 public string choiceId;public ChoiceEffectDelta immediate,during,end;public double satisfaction,fatigue,fatigueGrowthPercent;public int satisfactionDelta,requestEndDelta,requestEndAppliedDelta,requestEndCap,motionPenaltyDelta;
}
[Serializable] public sealed class ContentPreviewResult {
 public ContentChoicePreviewResult choice;
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
  foreach(string k in sample.Keys)if(!Known(k,"manner","required","fare","delta","onTime","start","clean","reference","actual","unsafeDriving","boardingScore","comfort","requestKind","requestSuccess","eta","etaOption","lawCode","choiceId","fatigue","seconds","outcome"))throw new ArgumentException("Unknown sample member "+k);
  var s=new ContentPreviewSample{manner=Int(sample,"manner",100,0,100),required=Int(sample,"required",70,1,100),fare=Int(sample,"fare",10000,0,10000000),delta=Int(sample,"delta",0,-100,100),onTime=Flag(sample,"onTime",true),start=Int(sample,"start",70,0,100),clean=Int(sample,"clean",0,0,100),reference=Number(sample,"reference",120,0,86400),actual=Number(sample,"actual",120,0,86400),unsafeDriving=Flag(sample,"unsafeDriving",false),boardingScore=Int(sample,"boardingScore",100,0,100),comfort=Int(sample,"comfort",0,0,100),requestKind=Text(sample,"requestKind","OnTime"),requestSuccess=Flag(sample,"requestSuccess",true),eta=Number(sample,"eta",120,0,86400),etaOption=Int(sample,"etaOption",0,-1,1),lawCode=Text(sample,"lawCode","red-straight"),choiceId=Text(sample,"choiceId",""),fatigue=Number(sample,"fatigue",0,0,100),seconds=Number(sample,"seconds",0,0,86400),outcome=Int(sample,"outcome",1,-1,1)};
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
   var choice=CalculateChoice(sample);var law=LawSample(sample.lawCode);return new ContentPreviewResult{choice=choice,success=true,profileHash=(string)ContentJson.Object(ContentJson.Parse(ContentTuningRuntime.Snapshot()))["contentHash"],reward=reward,total=reward.Total,requestDelta=PassengerRequest.Delta(kind,sample.requestSuccess),etaSeconds=PassengerRequest.EtaSeconds(sample.eta,sample.etaOption),lawPenalty=law==null?0:law.Deduction,lawReason=law==null?"사건 없음":law.reason};
  }finally{ContentTuningRuntime.Restore(previous);}
 }
 static double ClampStat(double x){return Math.Max(0,Math.Min(100,x));}
 static ContentChoicePreviewResult CalculateChoice(ContentPreviewSample sample){
  if(string.IsNullOrEmpty(sample.choiceId))return null;
  if(!ChoiceEffectDefinition.Enabled)throw new ArgumentException("Choice effects require a version 2 profile");
  if(sample.choiceId.Length>100||float.IsNaN(sample.fatigue)||float.IsInfinity(sample.fatigue)||sample.fatigue<0||sample.fatigue>100||float.IsNaN(sample.seconds)||float.IsInfinity(sample.seconds)||sample.seconds<0||sample.seconds>86400||sample.outcome< -1||sample.outcome>1)throw new ArgumentException("Invalid choice preview sample");
  var ledger=new ChoiceEffectLedger();double satisfaction=sample.start,fatigue=sample.fatigue;
  var immediate=ledger.Select("preview:choice",sample.choiceId,satisfaction,fatigue);satisfaction=ClampStat(satisfaction+immediate.satisfaction);fatigue=ClampStat(fatigue+immediate.fatigue);
  var during=ledger.Advance(sample.seconds,satisfaction,fatigue);satisfaction=ClampStat(satisfaction+during.satisfaction);fatigue=ClampStat(fatigue+during.fatigue);
  int motion=ledger.MotionPenaltyDelta;double growth=ledger.FatigueGrowthPercent;
  var end=ledger.Complete("preview:choice",sample.outcome,satisfaction,fatigue);satisfaction=ClampStat(satisfaction+end.satisfaction);fatigue=ClampStat(fatigue+end.fatigue);ledger.Validate();
  int cap=ContentTuning.Int("common.requestCap",10),applied=Math.Max(-cap,Math.Min(cap,ledger.RequestEndDelta));satisfaction=ClampStat(sample.start+ledger.SatisfactionDelta+applied);
  return new ContentChoicePreviewResult{choiceId=sample.choiceId,immediate=immediate,during=during,end=end,satisfaction=satisfaction,fatigue=fatigue,satisfactionDelta=ledger.SatisfactionDelta,requestEndDelta=ledger.RequestEndDelta,requestEndAppliedDelta=applied,requestEndCap=cap,motionPenaltyDelta=motion,fatigueGrowthPercent=growth};
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
