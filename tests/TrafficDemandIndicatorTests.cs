using System;
public static class TrafficDemandIndicatorTests {
 static int failures,checks;
 static void Check(bool value,string name){checks++;if(!value){failures++;Console.WriteLine("FAIL "+name);}}
 public static int Main(){
  Check(PassengerDemandPolicy.Phase(1200)==3,"overtime keeps even supply instead of permanent evening bias");
  Check(PassengerDemandPolicy.Phase(299,1200,false)==0&&PassengerDemandPolicy.Phase(300,1200,false)==1&&PassengerDemandPolicy.Phase(900,1200,false)==2,"campaign08–20 phase boundaries");
  Check(PassengerDemandPolicy.Phase(99,300,true)==0&&PassengerDemandPolicy.Phase(100,300,true)==1&&PassengerDemandPolicy.Phase(200,300,true)==2,"PVP relative thirds independent of match length");
  Check(PassengerDemandPolicy.Phase(double.NaN,1200,false)==3&&PassengerDemandPolicy.Phase(1,0,false)==3,"invalid clocks use neutral demand");
  Check(PassengerDemandPolicy.Weight("residential",0,false)==3&&PassengerDemandPolicy.Weight("office",0,true)==3,"morning residential to office");
  Check(PassengerDemandPolicy.Weight("commercial",1,true)==3&&PassengerDemandPolicy.Weight("residential",2,true)==3,"day commercial and evening home destinations");
  Check(PassengerDemandPolicy.Weight("office",3,false)==1&&PassengerDemandPolicy.Weight("commercial",3,true)==1,"overtime even both ends");
  double saved=PassengerDemandPolicy.OfferPriority("trip17","EG-GAME-03","EG-GAME-01",0);
  Check(saved==PassengerDemandPolicy.OfferPriority("trip17","EG-GAME-03","EG-GAME-01",0),"same generation identity repeats exactly");
  Check(Math.Abs(saved*9-PassengerDemandPolicy.OfferPriority("trip17","EG-GAME-03","EG-GAME-01",3))<1e-9,"origin destination bias bounded nine and no altered seed");
  int a=0,b=0;for(int i=0;i<2000;i++){if(PassengerDemandPolicy.OfferPriority("s"+i,"EG-GAME-03","EG-GAME-01",0)<PassengerDemandPolicy.OfferPriority("s"+i,"EG-GAME-01","EG-GAME-03",0))a++;if(PassengerDemandPolicy.OfferPriority("s"+i,"EG-GAME-03","EG-GAME-01",2)<PassengerDemandPolicy.OfferPriority("s"+i,"EG-GAME-01","EG-GAME-03",2))b++;}
  Check(a>1600&&b<1100,"observed seeded selection shifts morning direction without mandatory outcome");
  var calm=TrafficDriverProfile.ForIndex(0);var normal=TrafficDriverProfile.ForIndex(1);var impatient=TrafficDriverProfile.ForIndex(2);
  Check(calm.Kind==TrafficTemperament.Calm&&normal.Kind==TrafficTemperament.Normal&&impatient.Kind==TrafficTemperament.Impatient,"stable visible profile assignment");
  Check(calm.Acceleration<normal.Acceleration&&normal.Acceleration<impatient.Acceleration,"observable acceleration differs");
  Check(calm.LimitSpeed(8,9)<normal.LimitSpeed(8,9)&&normal.LimitSpeed(8,9)<=impatient.LimitSpeed(8,9),"visible safe headway differs");
  foreach(var p in new[]{calm,normal,impatient}){Check(p.LimitSpeed(0,9)==0&&p.LimitSpeed(100,4)<=4,"every profile stops and respects cruise/turn/legal cap");Check(p.LimitSpeed(2,9)<=TrafficRules.SafeSpeed(2,9),"no profile weakens braking envelope");}
  foreach(var p in new[]{calm,normal,impatient})Check(p.LimitSpeed(.2f,9)>0,"every profile can reach the actual stop line for full-stop evidence");
  Check(!calm.LaneChangeReady(5,0)&&impatient.LaneChangeReady(5,0),"actual lane change completion cooldown differs");
  IndicatorTests();
  Console.WriteLine("Traffic demand/profile checks="+checks+" failures="+failures);return failures==0?0:1;
 } static void IndicatorTests(){
  var p=new ManualIndicatorPolicy();p.Begin(1,350,0,0);
  Check(!p.Observe(4,10,.5,0,1,.1,false,false),"wrap turn alone does not cancel");
  Check(!p.Observe(4,30,.5,1,2,.1,false,false),"observed40degree turn needs steering return");
  Check(!p.Observe(4,30,0,2,3,.5,false,false),"straight less than1second keeps indicator");
  var saved=p.Capture();var restored=new ManualIndicatorPolicy();restored.Restore(saved);
  Check(restored.Observe(4,30,0,3,4,.5,false,false),"save restore finishes remaining straight interval once");
  Check(!restored.Observe(4,30,0,4,5,2,false,false),"completed cancellation emits once");
  p.Begin(-1,10,0,0);p.Observe(4,350,-.5,0,1,.1,false,false);p.Observe(4,330,-.5,-1,2,.1,false,false);
  Check(!p.Observe(0,330,0,-1,2,30,false,false),"red waiting never times out");
  Check(!p.Observe(4,330,0,-2,3,.5,false,false)&&p.Observe(4,330,0,-3,4,.5,false,false),"left wrap40degree turn cancels after moving straight");
  p.Begin(1,0,0,0);p.Observe(-4,50,.5,0,-1,2,false,false);
  Check(!p.Observe(4,50,0,0,0,2,false,false),"reverse rotation is not a forward turn");
  p.Begin(1,0,0,0);p.Observe(4,20,.5,1,1,.1,false,false);
  Check(!p.Observe(4,20,0,2,2,10,false,false),"subthreshold turn does not timeout cancel");
  p.Begin(1,0,0,0);p.Observe(4,40,.5,1,2,.1,false,false);
  Check(!p.Observe(4,40,0,2,3,2,true,false)&&!p.Observe(4,40,0,3,4,2,false,false),"hazards override and clear pending manual cancellation");
  p.Begin(1,0,0,0);p.Observe(4,40,.5,1,2,.1,false,false);
  Check(!p.Observe(4,40,0,2,3,2,false,true)&&!p.Observe(4,40,0,3,4,2,false,false),"assisted controller owns indicators");
  p.Begin(1,0,0,0);p.Observe(4,10,.3,1,3,.1,false,false);p.Observe(4,0,0,2.5,7,.1,false,false);
  Check(p.Observe(4,0,0,3,10,.9,false,false),"observed lanechange excursion displacement and heading return cancel");
  p.Begin(1,0,0,0);p.Observe(4,0,0,3,8,2,false,false);
  Check(!p.Observe(4,0,0,4,10,2,false,false),"sideways displacement alone is not lanechange evidence");
  p.Begin(1,0,0,0);p.Observe(4,40,.5,1,2,.1,false,false);p.Reset();
  Check(!p.Observe(4,40,0,2,3,2,false,false),"explicit reset removes stale completion evidence");
  restored.Restore(null);Check(!restored.Observe(4,40,0,2,3,2,false,false),"legacy save neutral migration");
 }
}
