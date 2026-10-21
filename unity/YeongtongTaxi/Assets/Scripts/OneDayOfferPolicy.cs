public static class OneDayOfferPolicy {
 // Rank existing routes only: deadlines, fares and scenario triggers are unchanged.
 public static float Rank(float route,float pickup,float mealDistance,double elapsed,int course,int generation){
  if(elapsed<300||elapsed>=900)return route+pickup;
  return route*.35f+pickup+mealDistance*.5f+((course+generation)%9)*110;
 }
}
