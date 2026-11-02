using System;
[Serializable] public sealed class StopOccupancySnapshot {
 public int version=1,sequence,day;public string eventId,stopId,attemptedTrip,protectedTrip,campaign;
 public float remaining,exitMetres;public double nextAt;public bool active,compensated;
 public bool alternativeAccepted,alternativeReturning,alternativePickup;public string alternativeId,alternativeTrip;public float walkingMetres,walkedMetres;
 public StopOccupancySnapshot Copy(){return (StopOccupancySnapshot)MemberwiseClone();}
}
public sealed class StopOccupancyPolicy {
 StopOccupancySnapshot state=new StopOccupancySnapshot();
 public bool Blocking {get{return state.active&&(state.remaining>0||state.exitMetres<7);}}
 public bool Active {get{return state.active;}}
 public bool AlternativeActive {get{return state.alternativeAccepted;}}
 public bool AlternativeReady {get{return state.alternativeAccepted&&!state.alternativeReturning&&state.walkedMetres>=state.walkingMetres;}}
 public bool AgreeAlternative(string trip,string original,string alternative,float walkingMetres,bool reachable,bool pathClear,bool bayClear){
  if(!Blocking||state.alternativeAccepted||string.IsNullOrEmpty(trip)||original!=state.stopId||string.IsNullOrEmpty(alternative)||alternative==original||float.IsNaN(walkingMetres)||float.IsInfinity(walkingMetres)||walkingMetres<=0||walkingMetres>150||!reachable||!pathClear||!bayClear)return false;
  state.version=2;state.alternativeAccepted=true;state.alternativeId=alternative;state.alternativeTrip=trip;state.walkingMetres=walkingMetres;state.walkedMetres=0;return true;
 }
 public void AdvanceAlternative(float seconds,bool pathClear){if(!state.alternativeAccepted||state.alternativeReturning||!pathClear||float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<=0)return;state.walkedMetres=Math.Min(state.walkingMetres,state.walkedMetres+seconds*1.2f);}
 public void BeginReturn(){if(state.alternativeAccepted)state.alternativeReturning=true;}
 public void SetAlternativePickup(bool pickup){if(!state.alternativeAccepted)return;state.alternativePickup=pickup;if(!pickup)state.walkedMetres=state.walkingMetres;}
 public void AdvanceReturn(float seconds,bool pathClear){if(!state.alternativeAccepted||!state.alternativeReturning||!pathClear||float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<=0)return;state.walkedMetres=Math.Max(0,state.walkedMetres-seconds*1.2f);if(state.walkedMetres<=0)ClearAlternative();}
 public void ClearAlternative(){state.alternativeAccepted=state.alternativeReturning=state.alternativePickup=false;state.alternativeId=state.alternativeTrip=null;state.walkingMetres=state.walkedMetres=0;}
 public bool Begin(string eventId,string stopId){if(state.active||string.IsNullOrEmpty(eventId)||string.IsNullOrEmpty(stopId))return false;state.eventId=eventId;state.stopId=stopId;state.remaining=30;state.exitMetres=0;state.active=true;state.compensated=false;state.protectedTrip=null;return true;}
 public void Advance(float seconds,bool exitClear){if(!state.active||float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<0)return;float after=Math.Max(0,seconds-state.remaining);state.remaining=Math.Max(0,state.remaining-seconds);if(state.remaining==0&&exitClear)state.exitMetres=Math.Min(7,state.exitMetres+after*1.5f);}
 public float Protection(){return !Blocking||state.compensated?0:Math.Min(90,state.remaining+(7-state.exitMetres)/1.5f+10);}
 public void Protected(string trip){state.compensated=true;state.protectedTrip=trip;}
 public void Removed(){state.active=false;}
 public StopOccupancySnapshot Capture(){return state.Copy();}
 public static StopOccupancyPolicy Restore(StopOccupancySnapshot s){var p=new StopOccupancyPolicy();if(s==null||s.version==0)return p;if((s.version!=1&&s.version!=2)||float.IsNaN(s.remaining)||float.IsInfinity(s.remaining)||s.remaining<0||s.remaining>30||float.IsNaN(s.exitMetres)||s.exitMetres<0||s.exitMetres>7||s.active&&(string.IsNullOrEmpty(s.eventId)||string.IsNullOrEmpty(s.stopId)))throw new ArgumentException("Invalid occupancy snapshot");
  if(float.IsNaN(s.walkingMetres)||float.IsInfinity(s.walkingMetres)||float.IsNaN(s.walkedMetres)||float.IsInfinity(s.walkedMetres)||s.walkingMetres<0||s.walkedMetres<0||s.walkedMetres>s.walkingMetres||s.walkingMetres>150||s.alternativeAccepted&&(s.version!=2||string.IsNullOrEmpty(s.alternativeTrip)||string.IsNullOrEmpty(s.stopId)||string.IsNullOrEmpty(s.alternativeId)||s.stopId==s.alternativeId||s.walkingMetres<=0||s.alternativeReturning&&!s.alternativePickup)||!s.alternativeAccepted&&(s.alternativePickup||s.alternativeReturning||s.walkingMetres!=0||s.walkedMetres!=0||!string.IsNullOrEmpty(s.alternativeId)||!string.IsNullOrEmpty(s.alternativeTrip)))throw new ArgumentException("Invalid alternative stop snapshot");
  p.state=s.Copy();return p;}
}
