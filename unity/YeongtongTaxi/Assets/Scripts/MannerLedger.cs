using System;
using System.Collections.Generic;
public class MannerLedger {
 public int Score {get;private set;}
 public MannerLedger(){Score=100;}
 readonly Dictionary<string,float> last=new Dictionary<string,float>();
 public bool Penalize(string key,int points,float now,float cooldown){float at;if(last.TryGetValue(key,out at)&&now-at<cooldown)return false;last[key]=now;Score=Math.Max(0,Score-points);return true;}
 public void Reset(){Score=100;last.Clear();}
 public static int Payment(int fare,int score,int required){return score>=required?fare:(int)((long)fare*7/10);}
 public static int MotionPenalty(bool comfortAccepted){return comfortAccepted?4:2;}
}
