using System;
[Serializable] public sealed class StopOccupancySnapshot {
 public int version=1,sequence,day;public string eventId,stopId,attemptedTrip,protectedTrip,campaign;
 public float remaining,exitMetres;public double nextAt;public bool active,compensated;
 public StopOccupancySnapshot Copy(){return (StopOccupancySnapshot)MemberwiseClone();}
}
public sealed class StopOccupancyPolicy {
 StopOccupancySnapshot state=new StopOccupancySnapshot();
 public bool Blocking {get{return state.active&&(state.remaining>0||state.exitMetres<7);}}
 public bool Active {get{return state.active;}}
 public bool Begin(string eventId,string stopId){if(state.active||string.IsNullOrEmpty(eventId)||string.IsNullOrEmpty(stopId))return false;state.eventId=eventId;state.stopId=stopId;state.remaining=30;state.exitMetres=0;state.active=true;state.compensated=false;state.protectedTrip=null;return true;}
 public void Advance(float seconds,bool exitClear){if(!state.active||float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<0)return;float after=Math.Max(0,seconds-state.remaining);state.remaining=Math.Max(0,state.remaining-seconds);if(state.remaining==0&&exitClear)state.exitMetres=Math.Min(7,state.exitMetres+after*1.5f);}
 public float Protection(){return !Blocking||state.compensated?0:Math.Min(90,state.remaining+(7-state.exitMetres)/1.5f+10);}
 public void Protected(string trip){state.compensated=true;state.protectedTrip=trip;}
 public void Removed(){state.active=false;}
 public StopOccupancySnapshot Capture(){return state.Copy();}
 public static StopOccupancyPolicy Restore(StopOccupancySnapshot s){var p=new StopOccupancyPolicy();if(s==null||s.version==0)return p;if(s.version!=1||float.IsNaN(s.remaining)||float.IsInfinity(s.remaining)||s.remaining<0||s.remaining>30||float.IsNaN(s.exitMetres)||s.exitMetres<0||s.exitMetres>7||s.active&&(string.IsNullOrEmpty(s.eventId)||string.IsNullOrEmpty(s.stopId)))throw new ArgumentException("Invalid occupancy snapshot");p.state=s.Copy();return p;}
}
