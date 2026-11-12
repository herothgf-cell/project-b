public struct OneDayPickup {
 public int Index,Cell;public float X,Z;
 public OneDayPickup(int index,int cell,float x,float z){Index=index;Cell=cell;X=x;Z=z;}
}
public static class OneDayOfferPolicy {
 // Bound route work independently of catalog size; rank only within geographic strata.
 public const int OrdinaryPickupLimit=12;
 public const float PickupSeparation=120;
 public static int PickupCell(float x,float z,float width,float depth){
  int col=System.Math.Max(0,System.Math.Min(2,(int)(x/System.Math.Max(1,width)*3)));
  int row=System.Math.Max(0,System.Math.Min(2,(int)(z/System.Math.Max(1,depth)*3)));
  return row*3+col;
 }
 // Pool order already includes demand, travel cost and the last completed trip.
 // Prefer a new pickup within the same spatial pass; if legal alternatives are
 // scarce, retain the existing distance/cell relaxation and ranked fallback.
 public static int SpreadPickup(OneDayPickup[] pool,OneDayPickup[] selected,int[] previous,bool rotate,int generation){
  for(int pass=0;pass<3;pass++){
   int best=-1,bestCell=10;bool bestRecent=false;
   for(int i=0;i<pool.Length;i++){
    var item=pool[i];bool same=false,near=false,cellUsed=false;
    foreach(var prior in selected){same|=prior.Index==item.Index;float dx=prior.X-item.X,dz=prior.Z-item.Z;near|=dx*dx+dz*dz<PickupSeparation*PickupSeparation;cellUsed|=prior.Cell==item.Cell;}
    if(same||pass<2&&near||pass==0&&cellUsed)continue;
    bool recent=previous!=null&&System.Array.IndexOf(previous,item.Index)>=0;
    int cell=(item.Cell-generation%9+9)%9;
    if(best<0||bestRecent&&!recent||bestRecent==recent&&rotate&&cell<bestCell){best=i;bestCell=cell;bestRecent=recent;}
   }
   if(best>=0)return best;
  }
  return -1;
 }
 // Directed road distances; no forced situation waypoints.
 public static float PickupSeconds(float distance){return .8f*System.Math.Max(150,distance/3+150);}
 // Candidate identity is saved with the offer. Never use randomized runtime string hashes.
 public static int Urgency(string candidate){uint hash=2166136261;foreach(char c in candidate??"")hash=unchecked((hash^c)*16777619);return (int)(hash%3);}
 public static string UrgencyLabel(int urgency){return urgency==0?"여유":urgency==2?"급함":"보통";}
 public static float TransportSeconds(float safeEta,float wait,int urgency){double factor=urgency==0?1.75:urgency==2?1.2:1.45;return .8f*(float)System.Math.Ceiling(System.Math.Max(90,(System.Math.Max(0,safeEta)*factor+System.Math.Max(0,wait)+45)*ContentTuning.Number("common.deadlineMultiplier",1)+ContentTuning.Number("common.deadlineSlack",0)));}
 public static float Rank(float route,float pickup,float mealDistance,double elapsed,int course,int generation){
  if(elapsed<300||elapsed>=900)return route+pickup;
  return route*.35f+pickup+mealDistance*.5f+((course+generation)%9)*110;
 }
}
