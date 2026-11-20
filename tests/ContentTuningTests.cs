using System;
using System.IO;
using System.Collections.Generic;
public static class ContentTuningTests {
 static int count;
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;}
 static void Reject(string json){string before=ContentTuning.CaptureJson();bool threw=false;try{ContentTuning.UseJson(json);}catch(ArgumentException){threw=true;}Check(threw,"invalid profile rejected");Check(before==ContentTuning.CaptureJson(),"failed install is atomic");}
 static string Change(string id,string value){var changes=new Dictionary<string,string>();changes.Add(id,value);return Changes(changes);}
 static string Changes(Dictionary<string,string> changes){var p=Profile();foreach(object o in ContentJson.Array(p["values"])){var row=ContentJson.Object(o);string value;if(changes.TryGetValue((string)row["id"],out value))row["value"]=value;}return Seal(p);}
 static Dictionary<string,object> Profile(){return ContentJson.Object(ContentJson.Parse(ContentTuning.CaptureJson()));}
 static string Seal(Dictionary<string,object> p){p["contentHash"]=ContentTuning.ComputeHash(p);return ContentJson.Stringify(p);}
 static string Schedule(string seed){var s=PassengerRequestSchedule.Create(seed,(RequestKind[])Enum.GetValues(typeof(RequestKind))).Capture();string result="";foreach(var row in s.slots)result+=row.kind+":"+row.threshold.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+";";return result;}
 static void SignalWaitSchedule(){
  var schedule=PassengerRequestSchedule.Restore(new PassengerRequestScheduleSnapshot{slots=new[]{
   new PassengerRequestSlot{kind=RequestKind.Comfort,threshold=.25f},
   new PassengerRequestSlot{kind=RequestKind.Empathy,threshold=.5f}}});
  Check(schedule.Tick(0,.1f,true,false)==-1,"ordinary short stop keeps progress and movement gates");
  Check(schedule.Tick(0,.1f,false,false,true)==-1,"long signal wait respects dialogue availability");
  Check(schedule.Tick(0,.1f,true,false,true)==0,"long signal wait releases an already assigned request");
  Check(schedule.Tick(0,.1f,true,false,true)==-1,"pending request prevents overlap at signal");
  schedule.Responded();
  Check(schedule.Tick(0,.1f,true,false)==-1,"normal movement gap still applies after the wait");
  Check(schedule.Tick(0,1,true,false,true)==-1,"long signal wait respects arrival cutoff");
 }
 static void MainTests(string[] args){
  // Capture the existing pure fallback calculations before a catalog is configured.
  var seeds=new string[300];for(int i=0;i<seeds.Length;i++)seeds[i]=Schedule("seed"+i);
  var fallbackReward=PassengerReward.CalculateCurrentFare(93,85,11300,5,true,76,3,100,85,false,83,0);
  ContentTuning.ConfigureCatalog(File.ReadAllText(args[0]));ContentTuning.UseDefaults();
  count+=PassengerStoryTests.Run();
 count+=RadioScheduleTests.Run();
  Check(ContentTuning.Int("home.rent.daily",-1)==1500,"trial home rent comes from the catalog");
  Check(ContentTuning.Number("home.returnTrip.weight",-1)==3,"homebound demand has the specified default weight");
  var home = new CampaignAccount(new PrototypeSettings{oneDay=true,homeBase=true,startCash=900,parking=500});
  Check(home.PendingDayCost(1)==900&&home.CloseDay(1),"projected home costs match available cash");
  Check(home.Expense(1,"home-rent")==400&&home.Debt==0&&home.Cash==0,"shortfall cannot produce rent debt");
  Check(!home.CloseDay(1)&&CampaignAccount.Restore(home.Snapshot()).Expense(1,"home-rent")==400,"settlement and restore preserve one rent charge");
  var oldAccount=new CampaignAccount(PrototypeSettings.ForThreeDays());
  oldAccount.CloseDay(1);
  var migrated=CampaignAccount.Restore(oldAccount.Snapshot());
  string oldWireProfile=ContentTuning.CapturePvpJson();
  Check(!oldWireProfile.Contains("home.rent.daily")&&!oldWireProfile.Contains("home.returnTrip.weight"),"v19 host profile excludes single-player keys");
  ContentTuning.UseJson(oldWireProfile);
  migrated.UseHomeBase();
  Check(migrated.Expense(1,"home-rent")==0&&migrated.CloseDay(2)&&migrated.Expense(2,"home-rent")==1500,"legacy completed day is not charged retroactively");
  Check(ContentTuning.Number("home.returnTrip.weight",-1)==3,"older save profile uses catalog default without changing its hash");
  ContentTuning.UseDefaults();
  var career=new EconomyLedger(PrototypeSettings.ForThreeDays());
  for(int i=1;i<=3;i++){
   Check(career.RecordFare("career-"+i,i*6000),"record career fixture fare");
   career.RequestClose();Check(career.Settle(),"settle career fixture day");
   if(i==1)Check(career.Account.BuyRoomItem("lamp",1,i),"career fixture owns a lamp");
   if(i<3)Check(career.NextDay(),"intro advances normally");
  }
  Check(career.State==BusinessState.Completed&&!career.NextDay()&&career.RecentNetMedian()==11500,"completed intro and recent-three median");
  Check(career.PurchaseVehicle("lpg")&&career.BuyVehicleColor(),"car and color owned before continuation");
  int priorMoney=career.Account.Cash;career.Account.Room.finalized=true;
  Check(career.ContinueCareer()&&career.Day==4&&!career.Account.Room.finalized,"completed archive continues into day four");
  Check(career.Account.Cash==priorMoney&&career.Account.Room.lampOwned&&career.Account.Vehicle.altColorOwned,"continuation preserves balance and ownership");
  Check(career.Account.SetCareerGoal("car","hybrid"),"career car goal accepts an existing candidate");
  career.RequestClose();career.Settle();
  Check(career.NextDay()&&career.Day==5,"unbounded next-day progression");
  var carried=EconomyLedger.RestoreCampaign(career.Snapshot(),career.Account.Snapshot());
  Check(carried.IsContinuousCareer&&carried.Day==5&&carried.Account.CareerGoal=="car"&&carried.Account.CareerVehicleGoal=="hybrid","day five and goal survive restore");
  var cabin=new CabinControlsState();
  Check(ContentTuning.Number("cabin.meter.missedFareRate",-1)==.5,"cabin missed fare uses the catalog rate");
  var story=new FatherStoryState();
  Check(!story.Ask(1,0,"trip-one")&&story.Ask(2,0,"trip-two")&&!story.Ask(2,1,"other"),"father question at most once per day from day two");
  Check(story.Question()=="원래 이 일 하셨어요?"&&story.Reply(1)=="아버지가 하시던 일이에요.","father question and reply use authored Korean content");
  Check(story.Answer("trip-two",1)&&!story.Answer("trip-two",0)&&story.Satisfaction("trip-two")==1&&story.Satisfaction("other")==0,"only one answer applies its satisfaction once per ride");
  story.Validate(2);story.NeighborhoodProgress(2);Check(!story.HasPage(1),"two neighborhood stars do not reveal the second page");
  story.NeighborhoodProgress(3);story.FridayOperated();Check(story.HasPage(1)&&story.HasPage(2),"future neighborhood and Friday conditions unlock distinct pages");
  foreach(var seed in new[]{"eungye-a","eungye-b","eungye-c"})for(int day=1;day<=30;day++){
   var current=DailyConditions.ForDay(seed,day);
   Check(current==DailyConditions.ForDay(seed,day),"same saved daily seed and date");
   for(int earlier=Math.Max(1,day-4);earlier<day;earlier++)Check(current!=DailyConditions.ForDay(seed,earlier),"no duplicate in the last four cards");
  }
  Check(DailyConditions.DemandMultiplier(DailyConditionKind.Rain,"office","residential",0)==1.3,"rain on all routes");
  Check(DailyConditions.DemandMultiplier(DailyConditionKind.Market,"commercial","office",2)==2&&DailyConditions.DemandMultiplier(DailyConditionKind.Market,"commercial","office",1)==1,"market only evening commercial origin");
  Check(DailyConditions.DemandMultiplier(DailyConditionKind.Friday,"commercial","residential",2)==2&&DailyConditions.DemandMultiplier(DailyConditionKind.Friday,"commercial","residential",3)==2&&DailyConditions.DemandMultiplier(DailyConditionKind.Friday,"residential","commercial",3)==1,"Friday only late commercial to residential");
  Check(PassengerDemandPolicy.Weight("commercial","residential",2,2)==18,"condition stacks with normal movement demand");
  Check(!DailyConditions.IsRaining(DailyConditionKind.Rain,399,1200)&&DailyConditions.IsRaining(DailyConditionKind.Rain,400,1200)&&!DailyConditions.IsRaining(DailyConditionKind.Market,600,1200),"rain starts at noon on rainy card only");
  Check(DailyConditions.PoolMultiplier(DailyConditionKind.Friday,PassengerType.Sleeping)>1&&DailyConditions.PoolMultiplier(DailyConditionKind.Market,PassengerType.Food)>1&&DailyConditions.PoolMultiplier(DailyConditionKind.Construction,PassengerType.Hurried)>1,"passenger weight per condition");
  RainPolicy.RuntimeRain=true;Check(RainPolicy.Enabled&&RainPolicy.BoardingDirt==2,"card override reuses configured rain penalty");
  RainPolicy.RuntimeRain=null;
  var logbook=new CampaignLogbook();var logRide=new PassengerResult{outcome="운송 완료",passengerPresetId="general",pickupStopId="EG-GAME-01",dropStopId="EG-GAME-02",storyId="test-story",storyLine="첫 결말",rideDistance=3200};
  logbook.RecordPreset("general");logbook.RecordPreset("general");
  Check(logbook.PresetCount==1&&logbook.HasPreset("general"),"first passenger preset unlocks once");
  for(int i=0;i<25;i++){
   logbook.ObserveRide(logRide,i==0,false);
   if(i==1)Check(logbook.NeighborhoodStars("EG-GAME-01")==0,"neighborhood stays locked until trip three");
   if(i==2)Check(logbook.NeighborhoodStars("EG-GAME-01")==1,"three endpoint visits reveal neighborhood");
   if(i==9)Check(logbook.NeighborhoodStars("EG-GAME-02")==2,"ten visits reveal waiting spot");
  }
  Check(logbook.NeighborhoodStars("EG-GAME-01")==3&&logbook.StarTotal==6&&logbook.StarredNeighborhoods==2,"twenty-five visits reach third star");
  Check(logbook.EndingCount==1&&logbook.EndingLine("test-story",0)=="첫 결말","duplicate ending preserves the first actual dialogue");
  logRide.storyEnding=1;logbook.ObserveRide(logRide,false,false);logRide.storyEnding=2;logbook.ObserveRide(logRide,false,true);
  Check(logbook.EndingCount==3&&logbook.HasAchievement(8)&&logbook.HasAchievement(10),"three different outcomes and Friday ride earn records");
  logbook.RecordStoryAssignment(1,"test-story");logbook.RecordStoryAssignment(1,"test-story");logbook.RecordStoryAssignment(2,"test-story");
  Check(logbook.StoryAssignments(1)==0&&logbook.StoryAssignments(2)==1&&logbook.HasStoryAssignment(2,"test-story"),"assignments reset on day boundary and deduplicate");
  logbook.ObserveSettlement(true);logbook.ObserveSettlement(false);logbook.ObserveSettlement(true);
  Check(logbook.HasAchievement(2)&&!logbook.HasAchievement(9),"a violation breaks the clean-day streak");
  logbook.ObserveSettlement(true);logbook.ObserveSettlement(true);logbook.Validate(5);
  Check(logbook.HasAchievement(9)&&logbook.HasAchievement(3)&&logbook.HasAchievement(7),"clean streak, longest distance and ride count are tracked");
  Check(!cabin.ToggleMeter(false)&&cabin.Payable(6300)==3150,"missed meter discounts fare without cancelling it");
  Check(cabin.ToggleMeter(true)&&!cabin.ToggleMeter(true)&&cabin.Payable(6300)==6300,"meter stays on until arrival");
  Check(cabin.NewCallBlock(false).Length>0&&cabin.ToggleMeter(false)&&cabin.NewCallBlock(false).Length==0,"stale meter must turn off before the next call");
  cabin.Vacancy=2;
  Check(cabin.NewCallBlock(false).Length==0,"legacy off-duty save can take a new call");
  cabin.Temperature=CabinControlsState.Cool;
  Check(cabin.Target==20&&cabin.Comfort==CabinControlsState.Cool,"old cooling save starts at its cool temperature");
  cabin.AdjustTemperature(1);cabin.AdjustTemperature(1);cabin.AdjustTemperature(1);cabin.AdjustTemperature(1);
  Check(cabin.Target==24&&cabin.Actual==20&&cabin.ClimateLabel=="난방","raising the setpoint requests heat without instantly warming the cabin");
  cabin.AdvanceClimate(12);
  Check(cabin.Actual==24&&cabin.Comfort==CabinControlsState.Warm,"heat reaches warm passenger preference");
  cabin.WindowOpen=true;cabin.AdvanceClimate(12);
  Check(cabin.Actual==23,"open window exchanges heat with outside air");
  cabin.WindowOpen=false;cabin.AdvanceClimate(12);
  Check(cabin.Actual==24,"closing window lets cabin reach setpoint again");
  while(cabin.AdjustTemperature(-1)){}
  Check(cabin.Target==18&&cabin.ClimateLabel=="냉방","cooling stops at minimum setpoint");
  while(cabin.AdjustTemperature(1)){}
  Check(cabin.Target==28,"warming stops at maximum setpoint");
  Check(ContentTuning.Int("law.centreLine.penalty",-1)==1,"centreline default is independently one law point");
  ContentTuning.UseJson(Change("law.centreLine.penalty","7"));
  Check(ContentTuning.Int("law.centreLine.penalty",-1)==7&&ContentTuning.Int("law.wrongWay.penalty",-1)==10&&ContentTuning.Int("manner.wrongWay.penalty",-1)==10,"centreline tuning preserves wrong-way and manner penalties");
  var beforeCentreLine=Profile();ContentJson.Array(beforeCentreLine["values"]).RemoveAll(o=>(string)ContentJson.Object(o)["id"]=="law.centreLine.penalty");
  ContentTuning.UseJson(ContentTuning.UpgradeJson(Seal(beforeCentreLine)));
  Check(ContentTuning.Int("law.centreLine.penalty",-1)==1,"legacy profile gains centreline default");
  ContentTuning.UseDefaults();
  foreach(string key in new[]{"flashStop","permissiveTurn","blockedExit"}){
   ContentTuning.UseDefaults();ContentTuning.UseJson(Change("law."+key+".penalty","37"));
   var scene=new ConfusionPracticeState(key=="flashStop"?8:key=="permissiveTurn"?12:10){Entered=true,Finished=true,CorrectRoute=true,EntryRed=true,EntryBlocked=true};
   Check(scene.Event("edited",1).Deduction==37,"edited scenario penalty "+key);
  }
  foreach(AccidentKind kind in Enum.GetValues(typeof(AccidentKind)))foreach(AccidentSeverity severity in Enum.GetValues(typeof(AccidentSeverity))){
   ContentTuning.UseDefaults();ContentTuning.UseJson(Change("law."+(kind==AccidentKind.Person?"person":"vehicle")+severity+".penalty","38"));
   var resolution=AccidentSeverityPolicy.Resolve(kind,severity);Check(resolution.ScorePenalty==38,"edited accident penalty "+kind+severity);
   var stored=new AccidentState();stored.BeginResolved("configured","target",resolution);
   ContentTuning.UseDefaults();stored.Copy().Validate();Check(stored.ScorePenalty==38,"captured accident penalty retained after profile change and restore");
   stored.selectedScorePenalty=101;bool invalidStoredPenalty=false;try{stored.Validate();}catch(ArgumentException){invalidStoredPenalty=true;}Check(invalidStoredPenalty,"out of range stored penalty rejected");
  }
  ContentTuning.UseDefaults();
  string baseline=ContentTuning.CaptureJson();ContentTuning.ValidateJson(baseline);
  ContentTuning.UseJson(Change("request.Quiet.unanswered","2"));var unansweredSigned=new PassengerRequest();unansweredSigned.Begin("signed-unanswered",RequestKind.Quiet);unansweredSigned.ObserveOpportunity(true);Check(unansweredSigned.Finish()==2,"signed editor minus two retains actual M deduction magnitude two");Check(unansweredSigned.Finish()==0,"unanswered M deduction cannot apply twice");ContentTuning.UseJson(Change("request.Quiet.unanswered","0"));unansweredSigned=new PassengerRequest();unansweredSigned.Begin("zero-unanswered",RequestKind.Quiet);unansweredSigned.ObserveOpportunity(true);Check(unansweredSigned.Finish()==0,"zero unanswered input has no M deduction");Reject(Change("request.Quiet.unanswered","-2"));ContentTuning.UseDefaults();ContentTuning.UseJson(Change("request.Empathy.unanswered","-2"));unansweredSigned=new PassengerRequest();unansweredSigned.Begin("signed-empathy",RequestKind.Empathy);unansweredSigned.ObserveOpportunity(true);Check(unansweredSigned.Finish()==0&&unansweredSigned.SatisfactionDelta==-2,"already signed unanswered satisfaction remains minus two");ContentTuning.UseDefaults();
  Check(ContentTuning.SchemaVersion==2,"default choice profile schema2");
  var legacy=Profile();legacy["schemaVersion"]=1.0;var legacyRows=ContentJson.Array(legacy["values"]);for(int n=legacyRows.Count-1;n>=0;n--){var row=ContentJson.Object(legacyRows[n]);string key=(string)row["id"];if(key.StartsWith("choice."))legacyRows.RemoveAt(n);else if(key=="passenger.presets")row["value"]=File.ReadAllText(args[2]);else if(key=="request.Empathy.success")row["value"]="11";else if(key=="effect.food.satisfaction")row["value"]="9";else if(key=="manner.comfortMotion.penalty")row["value"]="8";else if(key=="manner.motion.penalty")row["value"]="3";}
  string legacyJson=Seal(legacy);ContentTuning.UseJson(legacyJson);Check(ContentTuning.SchemaVersion==1,"snapshot retains schema1");string upgraded=ContentTuning.UpgradeJson(legacyJson);Check(ContentTuning.SchemaVersion==1,"upgrade does not mutate active profile");ContentTuning.UseJson(upgraded);Check(ContentTuning.SchemaVersion==2&&ContentTuning.Int("choice.request.Empathy.Accept.end.success.satisfaction",0)==11&&ContentTuning.Int("choice.type.Food.1.immediate.satisfaction",0)==9&&ContentTuning.Int("choice.request.Comfort.Accept.during.motionPenaltyDelta",0)==5,"upgrade preserves custom legacy values");File.WriteAllText(args[1]+".migrated.json",upgraded);File.WriteAllText(args[1]+".legacy.json",legacyJson);Reject(legacyJson.Replace("\"revision\":1","\"revision\":2"));ContentTuning.UseDefaults();
  ContentTuning.ValidateJson(File.ReadAllText(Path.Combine(Path.GetDirectoryName(args[0]),"default-profile.json")));
  var legacySchedule=new Dictionary<string,string>();foreach(var row in ContentJson.Object(ContentJson.Parse(File.ReadAllText(args[3]))))legacySchedule.Add(row.Key,(string)row.Value);ContentTuning.UseJson(Changes(legacySchedule));
  for(int i=0;i<seeds.Length;i++)Check(seeds[i]==Schedule("seed"+i),"fixed-seed legacy schedule equivalence "+i);ContentTuning.UseDefaults();
  var configuredReward=PassengerReward.CalculateCurrentFare(93,85,11300,5,true,76,3,100,85,false,83,0);
  Check(fallbackReward.Total==configuredReward.Total&&fallbackReward.satisfaction==configuredReward.satisfaction,"default fare equivalence");
  Check(PassengerRequest.Delta(RequestKind.Comfort,true)==8,"default request");
  ContentTuning.UseJson(Change("request.Comfort.success","3"));Check(PassengerRequest.Delta(RequestKind.Comfort,true)==3,"changed request actual");
  var req=new PassengerRequest();req.Begin("request",RequestKind.Comfort);req.Accept();req.CompleteGoal(true,"test");Check(req.SatisfactionDelta==3,"changed completed request");
  ContentTuning.UseDefaults();ContentTuning.UseJson(Change("request.Comfort.reply.Accept","수정 실제 기사 답변"));var spoken=new PassengerRequest();spoken.Begin("spoken",RequestKind.Comfort);spoken.Accept("기존 상세 안내");Check(spoken.ResponseText().Contains("수정 실제 기사 답변")&&spoken.SpokenLine("기존 화면 답변")=="수정 실제 기사 답변","edited reply is actual stored and spoken response");var restoredSpoken=new PassengerRequest();restoredSpoken.Restore(spoken.Snapshot());Check(restoredSpoken.SpokenLine("이전 화면 답변")=="수정 실제 기사 답변","edited actual reply survives snapshot");ContentTuning.UseDefaults();spoken=new PassengerRequest();spoken.Begin("default-spoken",RequestKind.Comfort);spoken.Accept("기존 상세 안내");Check(spoken.ResponseText().Contains("기존 상세 안내")&&spoken.SpokenLine("기존 화면 답변")=="기존 화면 답변","default actual response unchanged");ContentTuning.UseDefaults();ContentTuning.UseJson(Change("effect.luggage.prepareSeconds","24"));
  var b=PassengerBehaviorState.Create("work",PassengerType.Luggage);b.Respond(0,true);Check(b.PreparationRemaining==24,"changed work time");b.Validate();
  Check(b.ChoiceEffects[0].Contains("24초"),"effect text shares work setting");b.Advance(10,true,false,0,100);b.Validate();Check(b.PreparationRemaining==14&&!b.BoardingReady,"real work gate uses changed duration");
  ContentTuning.UseJson(Change("effect.food.crumbs","9"));var food=PassengerBehaviorState.Create("food",PassengerType.Food);food.Respond(1,true);food.Board();food.Advance(30,false,true,.6f,100);Check(food.DirtEvents[0].Amount==9,"actual dirt");Check(food.ChoiceEffects[1].Contains("-9"),"food effect generated from setting");
  ContentTuning.UseDefaults();ContentTuning.UseJson(Change("common.deadlineMultiplier","2"));Check(OneDayOfferPolicy.TransportSeconds(100,0,1)==304,"deadline setting actual");
  ContentTuning.UseDefaults();ContentTuning.UseJson(Change("law.redStraight.penalty","7"));var observed=new RoadObservation{known=true,forward=SignalColor.Red};Check(LawEvaluator.Signal("law",1,observed,false,SignalColor.Red,new PrototypeSettings()).Deduction==7,"actual law penalty");
  ContentTuning.UseDefaults();ContentTuning.UseJson(Change("law.redStraight.penalty","0"));var zeroViolation=LawEvaluator.Signal("zero-law",1,new RoadObservation{known=true,forward=SignalColor.Red},false,SignalColor.Red);Check(zeroViolation.Deduction==0&&zeroViolation.HasViolation,"zero point event retains violation verdict");var zeroEvents=new[]{zeroViolation};Check(LawLedger.ViolationCount(zeroEvents,0)==1&&LawLedger.ViolationCount(zeroEvents,1)==0,"semantic violation count respects boarding boundary");var onTimeRequest=new PassengerRequest();onTimeRequest.Begin("zero-ontime",RequestKind.OnTime);onTimeRequest.Accept();onTimeRequest.CompleteGoal(LawLedger.ViolationCount(zeroEvents,0)==0,"zero-cost law event");Check(onTimeRequest.SatisfactionDelta==-10,"zero-point violation prevents on-time request success");var zeroUnsafe=PassengerReward.CalculateCurrentFare(100,70,5000,0,true,65,0,100,70,LawLedger.ViolationCount(zeroEvents,0)>0,70,0);Check(zeroUnsafe.timeDelta==0&&zeroUnsafe.bonus==0,"zero-point violation prevents early arrival gain and resulting bonus");var mixedZero=new LawEvent{recoveryPolicyVersion=1,id="mixed-zero",day=1};mixedZero.Add("good",LawVerdict.Compliant,0);mixedZero.Add("bad",LawVerdict.Violation,0);Check(mixedZero.HasViolation&&LawLedger.ScoreAfter(70,mixedZero)==70,"zero-point mixed violation never earns compliant recovery");ContentTuning.UseDefaults();ContentTuning.UseDefaults();ContentTuning.UseJson(Change("manner.collision.penalty","3"));var m=new MannerLedger();m.Penalize("collision",10,0,0);Check(m.Score==97,"actual manner penalty");
  ContentTuning.UseDefaults();ContentTuning.UseJson(Change("manner.comfortMotion.penalty","17"));Check(PassengerRequestChoices.For(RequestKind.Comfort)[0].effect.Contains("감점 +1점")&&!PassengerRequestChoices.For(RequestKind.Comfort)[0].effect.Contains("-17"),"legacy comfort penalty does not mutate independent choice description");ContentTuning.UseJson(Change("choice.request.Comfort.Accept.during.motionPenaltyDelta","7"));Check(PassengerRequestChoices.For(RequestKind.Comfort)[0].effect.Contains("감점 +7점"),"comfort effect text uses independent motion extra");ContentTuning.UseDefaults();ContentTuning.UseJson(Change("manner.rightStop.penalty","3"));var stopM=new MannerLedger();stopM.Penalize("redmovement:stop",4,0,0);Check(stopM.Score==97&&MannerLedger.Penalty("redmovement:stop",4)==3,"right-stop M and shown amount use same semantic source");ContentTuning.UseDefaults();ContentTuning.UseJson(Change("common.fareMultiplier","2"));var reward=PassengerReward.CalculateCurrentFare(100,70,5000,0,true,70,0,100,100,false,70,0);Check(reward.baseFare==10000,"actual fare factor");
  Check(PassengerReward.CalculateWithDrivingScore(100,70,5000,0,true,70,0,100,100,false,70,0).baseFare==5000,"legacy policy3 untouched");Check(PassengerReward.CalculateDetailed(100,70,5000,0,0,true,70,0,100,100,false).baseFare==5000,"legacy policy2 untouched");
  ContentTuning.UseDefaults();ContentTuning.UseJson(Change("common.timeCap","0"));reward=PassengerReward.CalculateCurrentFare(100,70,5000,0,true,65,0,100,70,false,70,0);Check(reward.timeDelta==0&&reward.satisfaction==65&&reward.baseFare==4640,"changed arrival score/fare consistent");
  ContentTuning.UseDefaults();ContentTuning.UseJson(Change("common.bonusMultiplier","2"));reward=PassengerReward.CalculateCurrentFare(100,70,5000,0,true,70,0,100,100,false,70,0);Check(reward.bonus==200,"bonus factor actual");
  ContentTuning.UseDefaults();var voice=PassengerBehaviorState.Create("voice",PassengerType.Comfort);voice.Respond(0,true);voice.Board();voice.Advance(35,false,true,.5f,100);voice.SetRepresentativeContext(PassengerRepresentativeStatus.Accepted,"actual");ContentTuning.UseJson(Change("type.Comfort.followup."+voice.Variant,"수정 후속 대사"));Check(voice.Dialogue.Contains("수정 후속 대사"),"accepted representative follows tuned dialogue");ContentTuning.UseDefaults();ContentTuning.UseJson(Change("type.Food.choice.0","새 기사 답변"));Check(PassengerBehaviorState.Create("voice-food",PassengerType.Food).ChoiceLabels[0]=="새 기사 답변","actual typed choice override");ContentTuning.UseDefaults();var draw=new Dictionary<string,string>{{"schedule.count0","0"},{"schedule.count1","100"},{"schedule.count2","0"}};foreach(RequestKind kind in Enum.GetValues(typeof(RequestKind)))if(kind!=RequestKind.LostItem)draw["request."+kind+".weight"]=kind==RequestKind.Quiet?"10":"0";ContentTuning.UseJson(Changes(draw));for(int i=0;i<50;i++){var slots=PassengerRequestSchedule.Create("draw"+i,(RequestKind[])Enum.GetValues(typeof(RequestKind))).Capture().slots;Check(slots.Length==1&&slots[0].kind==RequestKind.Quiet,"actual count and weighted draw "+i);}
  ContentTuning.UseDefaults();ContentTuning.UseJson(Change("request.Quiet.enabled","false"));for(int i=0;i<100;i++)foreach(var slot in PassengerRequestSchedule.Create("disabled"+i,(RequestKind[])Enum.GetValues(typeof(RequestKind))).Capture().slots)Check(slot.kind!=RequestKind.Quiet,"disabled request omitted");
  ContentTuning.UseDefaults();ContentTuning.UseJson(Changes(new Dictionary<string,string>{{"schedule.count0","0"},{"schedule.count1","100"},{"schedule.count2","0"},{"schedule.progressMin","0.2506"},{"schedule.progressMax","0.2512"}}));for(int i=0;i<100;i++){var fractional=PassengerRequestSchedule.Create("fractional"+i,(RequestKind[])Enum.GetValues(typeof(RequestKind))).Capture();foreach(var slot in fractional.slots)Check(slot.threshold==0||slot.threshold<=.2512001f,"fractional threshold bounded");PassengerRequestSchedule.Restore(fractional);}ContentTuning.UseDefaults();ContentTuning.UseDefaults();ContentTuning.UseJson(Change("effect.errand.seconds","45"));var errand=PassengerErrandState.Agree("trip","via","dest");Check(errand.Remaining==45,"errand actual agreement");errand.Start(true);errand.Advance(20,true,false);errand.Validate();Check(errand.Remaining==25,"errand changed timer restores");
  ContentTuning.UseDefaults();ContentTuning.UseJson(Change("law.recovery.points","8"));var recovered=new LawEvent{recoveryPolicyVersion=1,id="good",day=1};recovered.Add("observed",LawVerdict.Compliant,0);Check(LawLedger.ScoreAfter(50,recovered)==58,"configured law recovery");ContentTuning.UseDefaults();Check(LawLedger.ScoreAfter(50,recovered)==58,"past recovery recorded unchanged");ContentTuning.UseJson(Change("law.slowTurn.limitKmh","20"));var slow=new SlowRightTurnState();slow.Observe(1,15,true,true,false);Check(slow.Verdict==SlowRightTurnVerdict.Compliant,"actual slow turn threshold");ContentTuning.UseDefaults();var catalogRoot=ContentJson.Object(ContentJson.Parse(File.ReadAllText(args[0])));foreach(object item in ContentJson.Array(catalogRoot["entries"])){var entry=ContentJson.Object(item);string entryId=(string)entry["id"];if(entryId=="request.LargeRoad.failure"||entryId=="request.ShortRoad.failure"||entryId=="request.Quiet.failure"||entryId=="request.Eta.success"||entryId=="request.Music.unanswered"||entryId=="request.LostItem.success")Check(!(bool)entry["editable"],"unreachable/neutral outcome is read-only "+entryId);}ContentTuning.UseDefaults();Reject(baseline.Replace("\"schemaVersion\":2","\"schemaVersion\":3"));Reject(baseline.Replace("\"revision\":1","\"revision\":2"));Reject(baseline.Replace("\"schemaVersion\":2","\"schemaVersion\":2,\"schemaVersion\":2"));Reject(baseline.Replace("\"schemaVersion\":2","\"schemaVersion\":2,\"unknown\":1"));
  Reject(Change("effect.food.spillPercent","101"));Reject(Change("effect.food.spillPercent","2.5"));Reject(Change("effect.food.spillPercent","NaN"));Reject(Change("request.Quiet.enabled","yes"));Reject(Change("schedule.progressMin","0.9"));Reject(Change("schedule.count0","21"));Reject(Change("common.required.easy","90"));Reject(Change("effect.luggage.prepareSeconds","-1"));Reject(Change("request.Quiet.prompt",new string('a',4001)));
  var invalid=Profile();var rows=ContentJson.Array(invalid["values"]);rows.RemoveAll(o=>(string)ContentJson.Object(o)["id"]=="common.required.easy");Reject(Seal(invalid));invalid=Profile();rows=ContentJson.Array(invalid["values"]);rows.Add(rows[0]);Reject(ContentJson.Stringify(invalid));invalid=Profile();ContentJson.Object(ContentJson.Array(invalid["values"])[0])["unknown"]=true;Reject(Seal(invalid));invalid=Profile();ContentJson.Object(ContentJson.Array(invalid["values"])[0])["value"]=3.0;Reject(ContentJson.Stringify(invalid));
  foreach(string bad in new[]{"{} trailing","{\"a\":1,}","[1,]","\"\\ud800\"","{\"a\":01}"}){bool threw=false;try{ContentJson.Parse(bad);}catch(ArgumentException){threw=true;}Check(threw,"strict JSON grammar");}
  ContentTuning.UseJson(Change("request.Quiet.prompt","한글\n\"é😀"));Check(ContentTuning.Text("request.Quiet.prompt","")=="한글\n\"é😀","Unicode text");string snap=ContentTuning.CaptureJson();ContentTuning.UseDefaults();ContentTuning.UseJson(snap);Check(ContentTuning.CaptureJson()==snap,"snapshot independent");
  File.WriteAllText(args[1],snap,new System.Text.UTF8Encoding(false));Console.WriteLine("ContentTuningTests passed "+count);
 }
 static void NaturalFatigueGuidanceTests(){
  double[] satieties={80,20,5},singleRates={4,6,8},pvpRates={5,7.5,10};
  for(int i=0;i<satieties.Length;i++){
   var single=new DriverCondition{satiety=satieties[i],fatigue=20};single.Advance(60);Check(Math.Abs(single.fatigue-20-singleRates[i])<.000001,"documented single natural fatigue rate "+i);
   var pvp=new DriverCondition{satiety=satieties[i],fatigue=20};pvp.Advance(60,false,PvpConditionRules.FatigueMultiplier);Check(Math.Abs(pvp.fatigue-20-pvpRates[i])<.000001&&PvpConditionRules.FatiguePerMinute(satieties[i])==pvpRates[i],"documented PVP natural fatigue rate "+i);
  }
  foreach(double percent in new double[]{-100,-50,0,50}){var driver=new DriverCondition{satiety=80,fatigue=20,choiceFatigueGrowthPercent=percent};driver.Advance(60);Check(Math.Abs(driver.fatigue-20-4*(1+percent/100))<.000001,"documented natural fatigue percent example "+percent);}
  var boundary=new DriverCondition{satiety=27,fatigue=20};boundary.Advance(60);Check(Math.Abs(boundary.fatigue-25.2)<.000001,"natural fatigue changes at hunger boundary within a minute");
  var resting=new DriverCondition{satiety=80,fatigue=20};resting.Advance(60,true);Check(resting.fatigue==20,"rest skips natural fatigue in guidance");
  ContentTuning.UseDefaults();ContentTuning.UseJson(Change("law.redStraight.penalty","2"));var law=LawEvaluator.Signal("signed-law",1,new RoadObservation{known=true,forward=SignalColor.Red},false,SignalColor.Red);Check(law.Deduction==2&&LawLedger.ScoreAfter(70,law)==68,"signed UI minus two retains actual law deduction two");
  ContentTuning.UseDefaults();ContentTuning.UseJson(Change("manner.collision.penalty","2"));var manner=new MannerLedger();manner.Penalize("collision",10,0,0);Check(manner.Score==98,"signed UI minus two retains actual driving deduction two");ContentTuning.UseDefaults();
 }

 static void DrunkSettingsTests(){
 ContentTuning.UseDefaults();string baseline=ContentTuning.CaptureJson();
 var changes=new Dictionary<string,string>{{"type.Drunk.spawnPercent","100"},{"type.Drunk.dailyMax","3"},{"type.Drunk.forceEnabled","false"},{"type.Drunk.sleepPercent","100"},{"type.Drunk.vomitPercent","0"}};
 ContentTuning.UseJson(Changes(changes));var care=new VehicleCare();int drunks=0;for(int i=0;i<12;i++){var t=care.ReservePassenger("cap"+i,new PassengerType[0]);if(t==PassengerType.Drunk)drunks++;Check(care.CommitPassenger("cap"+i,t),"editable cap commit");care=VehicleCare.Restore(care.Snapshot());}Check(drunks==3,"editable daily cap3 and restored count");
 for(int i=0;i<50;i++){var r=DrunkRideState.CreateAssignedDrunk("prob"+i);Check(r.SleepPlanned&&!r.VomitPlanned,"sleep100 vomit0 independently");}
 ContentTuning.UseDefaults();ContentTuning.UseJson(Changes(new Dictionary<string,string>{{"type.Drunk.dailyMax","0"},{"type.Drunk.spawnPercent","100"}}));care=new VehicleCare();for(int i=0;i<8;i++){var t=care.ReservePassenger("off"+i,new PassengerType[0]);Check(t==PassengerType.General&&care.CommitPassenger("off"+i,t),"cap0 overrides chance and force");}
 ContentTuning.UseDefaults();ContentTuning.UseJson(Changes(new Dictionary<string,string>{{"type.Drunk.spawnPercent","0"},{"type.Drunk.forceEnabled","false"}}));care=new VehicleCare();for(int i=0;i<10;i++){var t=care.ReservePassenger("off2"+i,new PassengerType[0]);Check(t==PassengerType.General&&care.CommitPassenger("off2"+i,t),"zero chance force off");}
 ContentTuning.UseDefaults();ContentTuning.UseJson(Changes(new Dictionary<string,string>{{"type.Drunk.spawnPercent","0"},{"type.Drunk.forceAfterMisses","5"},{"type.Drunk.sleepPercent","0"},{"type.Drunk.vomitPercent","100"}}));care=new VehicleCare();for(int i=0;i<5;i++){var t=care.ReservePassenger("five"+i,new PassengerType[0]);Check(t==PassengerType.General&&care.CommitPassenger("five"+i,t),"no forced before five");care=VehicleCare.Restore(care.Snapshot());}Check(care.ReservePassenger("forced",new PassengerType[0])==PassengerType.Drunk,"force after configurable five");var planned=DrunkRideState.CreateAssignedDrunk("planned");Check(!planned.SleepPlanned&&planned.VomitPlanned,"sleep0 vomit100");ContentTuning.UseJson(baseline);Check(!planned.SleepPlanned&&planned.VomitPlanned,"existing ride not rerolled");
 var old=Profile();ContentJson.Array(old["values"]).RemoveAll(o=>((string)ContentJson.Object(o)["id"]).StartsWith("type.Drunk.")&&!((string)ContentJson.Object(o)["id"]).Contains(".intro.")&&!((string)ContentJson.Object(o)["id"]).Contains(".choice.")&&!((string)ContentJson.Object(o)["id"]).Contains(".followup."));string legacy=Seal(old);ContentTuning.UseJson(legacy);Check(ContentTuning.CaptureJson()==legacy,"old snapshot remains exact");string upgraded=ContentTuning.UpgradeJson(legacy);ContentTuning.UseJson(upgraded);Check(ContentTuning.Int("type.Drunk.dailyMax",-1)==2,"old profile upgrades default without reward changes");Reject(Change("type.Drunk.spawnPercent","101"));Reject(Change("type.Drunk.forceAfterMisses","0"));ContentTuning.UseDefaults();
 }
 public static int Main(string[] args){try{MainTests(args);SignalWaitSchedule();NaturalFatigueGuidanceTests();DrunkSettingsTests();Check(ContentTuning.OverrideText("request.Quiet.reply.Accept","dynamic fallback")=="조용히 모시겠습니다.","promoted authored reply is spoken even at release default");Check(ContentTuning.OverrideText("request.OnTime.reply.Accept","dynamic route details")=="dynamic route details","unchanged dynamic replies retain details");Console.WriteLine("Signed penalties and fatigue guidance total "+count);return 0;}catch(Exception e){Console.Error.WriteLine(e.ToString());return 1;}}
}
