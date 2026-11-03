using System;
// Bound the playback cursor as well as the pose extrapolation. A delivery gap must
// not leave the cursor running far beyond the newest pose and prolong a freeze.
public static class PvpNativePlaybackClock {
 public static double Advance(double cursor,double newest,double age,double dt){
  double desired=newest+Math.Min(.2,Math.Max(0,age))-.1;
  if(Math.Abs(desired-cursor)>1)cursor=desired;
  else cursor+=Math.Max(0,dt)*Math.Max(.9,Math.Min(1.1,1+(desired-cursor)*2));
  return Math.Min(cursor,newest+.1);
 }
}
