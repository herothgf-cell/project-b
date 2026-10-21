using System;
class MitTrafficFollowingTests {
 static void Check(bool v,string why){if(!v)throw new Exception(why);}
 static void Main(){
  Check(MitTrafficFollowing.Limit(false,7,3,2,1,true)==7,"OFF preserves baseline exactly");
  Check(MitTrafficFollowing.Limit(true,7,3,2,1,true)<7,"ON slows for same-direction leader");
  Check(MitTrafficFollowing.Limit(true,7,3,2,.8f,true)==7,"crossing traffic not speed matched");
  Check(MitTrafficFollowing.Limit(true,7,3,2,1,false)==7,"generic obstacle uses baseline");
  Check(MitTrafficFollowing.Limit(true,7,20,2,1,true)==7,"distant leader does not constrain");
  Check(MitTrafficFollowing.Limit(true,0,3,5,1,true)==0,"red signal stop wins");
  Check(MitTrafficFollowing.Limit(true,.05f,3,0,1,true)==.05f,"baseline tighter cap wins");
  Check(MitTrafficFollowing.Limit(true,7,3,0,1,true)>0,"stationary leader does not latch follower stopped");
  for(int gap=0;gap<30;gap++)for(int speed=0;speed<15;speed++){
   float baseline=TrafficRules.SafeSpeed(gap,9);
   float value=MitTrafficFollowing.Limit(true,baseline,gap,speed,1,true);
   Check(value>=0&&value<=baseline,"never exceeds safety envelope");
  }
  Console.WriteLine("PASS: MIT following OFF/ON, direction, distance, signals, restart and 450 safety bounds");
 }
}
