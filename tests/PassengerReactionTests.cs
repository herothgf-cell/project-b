using System;
using System.IO;
using System.Reflection;
class PassengerReactionTests {
 static int checks;
 static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
 static string Reaction(object state,string method,params object[] args){var m=state.GetType().GetMethod(method);Check(m!=null,"missing passenger reaction API: "+method);return (string)m.Invoke(state,args);}
 static void Main(string[] args){try{Run(args);}catch(Exception e){Console.WriteLine("FAIL "+e.Message);Environment.Exit(1);}}
 static void Run(string[] args){ContentTuning.ConfigureCatalog(File.ReadAllText(args[0]));
  var s=PassengerSituationState.Create("trip",0,0,0);s.Observe(1,false,true,true,false,0,0,0,1,false);
  string explain=Reaction(s,"ReactionForChoice",0),rest=Reaction(s,"ReactionForChoice",1);
  Check(explain.Contains("안전")&&!explain.Contains("속도를 줄였습니다"),"necessary brake explanation produces passenger acknowledgement");
  Check(explain!=rest&&rest.Contains("쉴게요"),"quiet choice has a different passenger reaction");
  Check(s.Pending==PassengerSituationKind.NecessaryBrake&&s.Answer==-1,"reading reaction does not apply the choice");
  s.LuggageLoaded=true;Check(Reaction(s,"ReactionForChoice",0).Contains("짐"),"loaded luggage acknowledgement");
  var food=PassengerBehaviorState.Create("food",PassengerType.Food);
  string sealedFood=Reaction(food,"ReactionForChoice",0),allowed=Reaction(food,"ReactionForChoice",1),cleanup=Reaction(food,"ReactionForChoice",2);
  Check(sealedFood.Contains("보관")&&allowed.Contains("먹")&&cleanup.Contains("정리")&&allowed!=cleanup,"food outcomes match selected options");
  Check(food.Choice==-1&&!food.ConsumptionOccurred,"reaction cannot change food permission");
  var luggage=PassengerBehaviorState.Create("luggage",PassengerType.Luggage);
  Check(Reaction(luggage,"ReactionForChoice",0).Contains("짐")&&Reaction(luggage,"ReactionForChoice",1).Contains("싣지"),"luggage accept and reject reactions differ");
  var temperature=PassengerBehaviorState.Create("temperature",PassengerType.General);temperature.PresetTraits=new[]{"body.temperatureSensitive"};temperature.Boarded=true;temperature.TemperaturePreference=0;
  Check(Reaction(temperature,"ReactionForChoice",0).Contains("부탁한 온도")&&Reaction(temperature,"ReactionForChoice",1).Contains("달라요")&&Reaction(temperature,"ReactionForChoice",2).Contains("지금 온도"),"temperature reactions distinguish agreement discomfort and unchanged setting");
  var umbrella=PassengerBehaviorState.Create("umbrella",PassengerType.General);umbrella.PresetTraits=new[]{"item.wetUmbrella"};Check(Reaction(umbrella,"ReactionForChoice",0).Contains("감싸")&&Reaction(umbrella,"ReactionForChoice",1).Contains("그대로"),"umbrella reactions reflect protection or unprotected storage");
  var edited=new PassengerRequest{Content=new PassengerContent(new[]{new PassengerOverride{id="request.Comfort.reply.Accept",value="기사 전용 개별 답변"}})};edited.Begin("custom",RequestKind.Comfort);edited.Accept();Check(edited.SpokenLine("fallback")=="기사 전용 개별 답변"&&!Reaction(edited,"ReactionLine").Contains("기사 전용"),"custom driver response remains authored driver content and is not relabeled as NPC");
  foreach(RequestKind kind in Enum.GetValues(typeof(RequestKind))){
   var r=new PassengerRequest();r.Begin("r"+kind,kind);r.Accept("기사 전용 수정 답변 · 적용 효과 6");
   string record=r.ResponseText();string line=Reaction(r,"ReactionLine");Check(line.Length>0&&!line.Contains("기사 전용")&&!line.Contains("효과"),"request response uses NPC voice "+kind);
   var restored=new PassengerRequest();restored.Restore(r.Snapshot());Check(Reaction(restored,"ReactionLine")==line,"reaction deterministic after snapshot "+kind);
   Check(r.ResponseText()==record,"driver response record preserved "+kind);
   var deferred=new PassengerRequest();deferred.Begin("defer"+kind,kind);deferred.DeferSafety();deferred.ApplyReply(PassengerReply.Defer);Check(Reaction(deferred,"ReactionLine").Contains("알겠"),"deferred passenger acknowledgement "+kind);
  }
  Check(PassengerPresentation.Speech("손님  “고맙습니다.”")=="고맙습니다.","speaker and outer curly quotes removed");
  Check(PassengerPresentation.Speech("승객: \"안녕하세요\"\n손님 · 「반갑습니다」")=="안녕하세요\n반갑습니다","outer quotes normalized per line");
  Check(PassengerPresentation.Speech("그분이 ‘천천히’ 가라고 했어요.")=="그분이 ‘천천히’ 가라고 했어요.","semantic inner quoted clause preserved");
  MovingScheduleChecks();FrequencyChecks();Console.WriteLine("PASSENGER_REACTIONS_OK checks="+checks);
 }
 static void MovingScheduleChecks(){
  var schedule=PassengerRequestSchedule.Restore(new PassengerRequestScheduleSnapshot{slots=new[]{new PassengerRequestSlot{kind=RequestKind.Comfort,threshold=.25f},new PassengerRequestSlot{kind=RequestKind.Empathy,threshold=.5f}}});
  Check(schedule.Tick(25,.3f,true,false)==0,"first scheduled request appears during continuous driving when dialogue is available");schedule.Responded();
  Check(schedule.Tick(19,.6f,true,false)==-1,"next request waits for twenty actual moving seconds");
  var restored=PassengerRequestSchedule.Restore(schedule.Capture());
  Check(restored.Tick(1,.6f,true,false)==1,"restored second request appears while driving without requiring a new stop");
  Check(restored.Tick(1,.7f,true,false)==-1,"pending request prevents simultaneous next popup");
 }
 static void FrequencyChecks(){
  Check(ContentTuning.Int("schedule.count0",-1)==15&&ContentTuning.Int("schedule.count1",-1)==60&&ContentTuning.Int("schedule.count2",-1)==25,"approved moderate request counts 15/60/25");
  Check(ContentTuning.Int("request.SafeStop.weight",-1)==3&&ContentTuning.Int("request.DestinationChange.weight",-1)==2&&ContentTuning.Int("request.ViaStop.weight",-1)==2,"approved activity request bias 3/2/2");
  string current=ContentTuning.CaptureJson();var baseline=ContentJson.Object(ContentJson.Parse(current));
  foreach(object item in ContentJson.Array(baseline["values"])){var row=ContentJson.Object(item);switch((string)row["id"]){case "schedule.count0":row["value"]="25";break;case "schedule.count2":row["value"]="15";break;case "request.SafeStop.weight":row["value"]="2";break;case "request.DestinationChange.weight":case "request.ViaStop.weight":row["value"]="1";break;}}
  baseline["contentHash"]=ContentTuning.ComputeHash(baseline);
  try{ContentTuning.UseJson(ContentJson.Stringify(baseline));Sample("before");ContentTuning.UseJson(current);Sample("after");}finally{ContentTuning.UseJson(current);}
 }
 static void Sample(string label){
  var kinds=new System.Collections.Generic.List<RequestKind>();foreach(RequestKind k in Enum.GetValues(typeof(RequestKind)))if(k!=RequestKind.LostItem&&ContentTuning.Flag("request."+k+".enabled",true)&&ContentTuning.Int("request."+k+".weight",1)>0)kinds.Add(k);
  int[] counts=new int[3];int total=0,activities=0,noneReleased=0,boardingReleased=0,stopsReleased=0,missed=0,movingReleased=0,movingMissed=0;
  for(int i=0;i<10000;i++){
   var schedule=PassengerRequestSchedule.Create("frequency-"+i,kinds.ToArray());var saved=schedule.Capture();counts[saved.slots.Length]++;total+=saved.slots.Length;
   foreach(var slot in saved.slots)if(slot.kind==RequestKind.SafeStop||slot.kind==RequestKind.DestinationChange||slot.kind==RequestKind.ViaStop)activities++;
   var noStop=PassengerRequestSchedule.Restore(schedule.Capture());noneReleased+=noStop.Tick(30,.6f,false,false)>=0?1:0;
   var moving=PassengerRequestSchedule.Restore(schedule.Capture());for(int t=0;t<85;t++)if(moving.Tick(t==0?0:1,t/100f,true,false)>=0){movingReleased++;moving.Responded();}moving.Tick(1,.85f,true,false);foreach(var slot in moving.Capture().slots)if(slot.closed&&!slot.presented)movingMissed++;
   var boarding=PassengerRequestSchedule.Restore(schedule.Capture());if(boarding.Tick(0,0,true,false)>=0){boardingReleased++;boarding.Responded();}boarding.Tick(50,.9f,false,false);
   // Controlled 100-second trip with boarding stop and further safe stops at20/40/60/80%.
   var stops=PassengerRequestSchedule.Restore(schedule.Capture());for(int t=0;t<=80;t++){bool safe=t%20==0;if(stops.Tick(t==0?0:1,t/100f,safe,false)>=0){stopsReleased++;stops.Responded();}}
   stops.Tick(5,.85f,false,false);foreach(var slot in stops.Capture().slots)if(slot.closed&&!slot.presented)missed++;
  }
  Check(noneReleased==0,"blocked dialogue never releases scheduled requests "+label);
  Check(total==movingReleased+movingMissed,"continuous driving accounts for every scheduled request "+label);
  Check(total==stopsReleased+missed,"controlled stop fixture accounts for every scheduled request "+label);
  Console.WriteLine("REQUEST_FREQUENCY_"+label+" seeds=10000 eligible="+kinds.Count+" count0/1/2="+string.Join("/",Array.ConvertAll(counts,x=>x.ToString()))+" scheduled="+total+" activitySlots="+activities+" dialogueBlockedReleased="+noneReleased+" boardingOnlyReleased="+boardingReleased+" stopsOnlyReleased="+stopsReleased+" stopsCutoffMissed="+missed+" continuousDrivingReleased="+movingReleased+" drivingCutoffMissed="+movingMissed);
 }
}
