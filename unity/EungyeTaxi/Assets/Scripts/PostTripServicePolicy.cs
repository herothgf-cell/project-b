using System;

// Advisory only: choosing a service never changes the driver's ability to accept a fare.
public static class PostTripServicePolicy {
 public static int Recommend(double fuel,double satiety,double fatigue,double secondsLeft,float[] distance){
  double[] need={fuel<=15?2+(15-fuel)/10:0,satiety<25?1+(25-satiety)/25:0,secondsLeft<=180?1.5:0,fatigue>=70?1+(fatigue-70)/30:0};
  int best=-1;double score=0;
  for(int i=0;i<need.Length;i++){
   if(distance==null||i>=distance.Length||float.IsInfinity(distance[i])||float.IsNaN(distance[i]))continue;
   double candidate=need[i]/(1+Math.Max(0,distance[i])/1000);
   if(candidate>score){score=candidate;best=i;}
  }
  return best;
 }
}
