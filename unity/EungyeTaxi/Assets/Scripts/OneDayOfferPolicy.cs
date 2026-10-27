public static class OneDayOfferPolicy {
 // Directed road distances; no forced situation waypoints.
 public static float PickupSeconds(float distance){return System.Math.Max(150,distance/3+150);}
 // Candidate identity is saved with the offer. Never use randomized runtime string hashes.
 public static int Urgency(string candidate){uint hash=2166136261;foreach(char c in candidate??"")hash=unchecked((hash^c)*16777619);return (int)(hash%3);}
 public static string UrgencyLabel(int urgency){return urgency==0?"여유":urgency==2?"급함":"보통";}
 public static float TransportSeconds(float safeEta,float wait,int urgency){float factor=urgency==0?1.75f:urgency==2?1.2f:1.45f;return (float)System.Math.Ceiling(System.Math.Max(90,System.Math.Max(0,safeEta)*factor+System.Math.Max(0,wait)+45));}
 public static float Rank(float route,float pickup,float mealDistance,double elapsed,int course,int generation){
  if(elapsed<300||elapsed>=900)return route+pickup;
  return route*.35f+pickup+mealDistance*.5f+((course+generation)%9)*110;
 }
}
