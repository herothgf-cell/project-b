public static class OneDayOfferPolicy {
 // Directed road distances; no forced situation waypoints.
 public static float PickupSeconds(float distance){return System.Math.Max(180,distance/3+180);}
 public static float Rank(float route,float pickup,float mealDistance,double elapsed,int course,int generation){
  if(elapsed<300||elapsed>=900)return route+pickup;
  return route*.35f+pickup+mealDistance*.5f+((course+generation)%9)*110;
 }
}
