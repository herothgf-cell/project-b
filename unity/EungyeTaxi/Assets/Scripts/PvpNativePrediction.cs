using System;using System.Collections.Generic;using UnityEngine;

// Ordered, bounded control history. The server receives controls only, never poses.
public sealed class PvpNativeInputQueue {
 readonly Queue<PvpNativeDrive> queued=new Queue<PvpNativeDrive>();public int Received {get;private set;}public int Applied {get;private set;}
 public int Count {get{return queued.Count;}}
 public bool Enqueue(PvpNativeDrive input){
  if(input==null||input.sequence<=Received||input.sequence-Received>256||!Finite(input.throttle)||!Finite(input.steer))return false;
  var copy=new PvpNativeDrive{sequence=input.sequence,throttle=Mathf.Clamp(input.throttle,-1,1),steer=Mathf.Clamp(input.steer,-1,1),brake=input.brake,blocked=input.blocked,steeringGain=DrivingSettings.Bound(input.steeringGain,.5f,1.5f,1),steeringResponse=DrivingSettings.Bound(input.steeringResponse,.25f,2,1),steeringReturn=DrivingSettings.Bound(input.steeringReturn,.25f,2,1)};
  Received=copy.sequence;if(queued.Count==32)queued.Dequeue();queued.Enqueue(copy);return true;
 }
 static bool Finite(float v){return !float.IsNaN(v)&&!float.IsInfinity(v);}
 public PvpNativeDrive Take(){if(queued.Count==0)return null;var value=queued.Dequeue();Applied=value.sequence;return value;}
 public void Clear(){queued.Clear();Received=Applied=0;}
 public static void Apply(TaxiController taxi,PvpNativeDrive input,bool safe,float seconds){
  taxi.automated=true;taxi.AutomatedHumanInput=true;taxi.autoThrottle=safe?0:input.throttle;
  taxi.ManualSteeringGain=input==null?1:DrivingSettings.Bound(input.steeringGain,.5f,1.5f,1);
  float rate=input==null?1:DrivingSettings.Bound(Mathf.Abs(input.steer)<.01f?input.steeringReturn:input.steeringResponse,.25f,2,1);
  taxi.autoSteer=safe?0:DriveFlowPolicy.AdvanceSteering(taxi.autoSteer,input.steer,taxi.Speed,seconds*rate);
  taxi.autoBrake=safe||input.brake;taxi.autoServiceBrake=0;
 }
}
public struct PvpPredictedMotion {
 public int pedalDirection,resetEpoch;
 public Vector3 position,velocity,angularVelocity;public Quaternion rotation;
 public static PvpPredictedMotion Read(Rigidbody body){var taxi=body.GetComponent<TaxiController>();return new PvpPredictedMotion{pedalDirection=taxi?taxi.PedalBrakingDirection:0,resetEpoch=taxi?taxi.ResetEpoch:0,position=body.position,rotation=body.rotation,velocity=body.velocity,angularVelocity=body.angularVelocity};}
}
// Compare a snapshot with its acknowledged historical step, not today's moving car.
// Error is transferred to the current simulation gradually; PhysX world replay is not used.
public sealed class PvpNativePredictionHistory {
 readonly Dictionary<int,PvpPredictedMotion> states=new Dictionary<int,PvpPredictedMotion>();readonly Queue<int> order=new Queue<int>();
 public int Count {get{return states.Count;}}public int LastAck {get;private set;}
 public void Remember(int sequence,PvpPredictedMotion state){if(sequence<=0)return;if(!states.ContainsKey(sequence)){order.Enqueue(sequence);if(order.Count>256)states.Remove(order.Dequeue());}states[sequence]=state;}
 public bool Error(int ack,PvpPredictedMotion authority,out Vector3 position,out Quaternion rotation,out Vector3 velocity,out Vector3 angular){
  position=velocity=angular=Vector3.zero;rotation=Quaternion.identity;PvpPredictedMotion old;
  if(ack<=0||ack<=LastAck||!states.TryGetValue(ack,out old))return false;
  LastAck=ack;position=authority.position-old.position;rotation=authority.rotation*Quaternion.Inverse(old.rotation);velocity=authority.velocity-old.velocity;angular=authority.angularVelocity-old.angularVelocity;return true;
 }
 public void Shift(Vector3 position,Quaternion rotation,Vector3 velocity,Vector3 angular){
  // Copy keys to reusable storage; dictionary values may be replaced safely.
  scratch.Clear();scratch.AddRange(states.Keys);foreach(int id in scratch){var value=states[id];value.position+=position;value.rotation=rotation*value.rotation;value.velocity+=velocity;value.angularVelocity+=angular;states[id]=value;}
 }
 readonly List<int> scratch=new List<int>(256);
 public void Clear(){states.Clear();order.Clear();LastAck=0;}
}
public sealed class PvpNativePrediction {
 readonly TaxiController taxi;readonly PvpNativePredictionHistory history=new PvpNativePredictionHistory();
 int sequence,authorityEpoch;bool initialized,locked;double lastServer,lastAckTime;int lastSeenAck;Vector3 error,velocityError,angularError;Quaternion rotationError=Quaternion.identity;
 public int Steps {get;private set;}public int Corrections {get;private set;}public int HardResets {get;private set;}
 public float LargestError {get;private set;}public int LastAck {get{return history.LastAck;}}public bool Initialized {get{return initialized;}}
 public string Diagnostics {get{return "sequence="+sequence+" ack="+lastSeenAck+" historyAck="+history.LastAck+" serverAge="+(Time.realtimeSinceStartupAsDouble-lastServer)+" ackAge="+(Time.realtimeSinceStartupAsDouble-lastAckTime)+" locked="+locked+" error="+error.magnitude;}}
 public PvpNativePrediction(TaxiController taxi){this.taxi=taxi;}
 public void Receive(int ack,PvpPredictedMotion authority,bool reset,bool stop){
  lastServer=Time.realtimeSinceStartupAsDouble;locked=stop;
  if(ack>lastSeenAck){lastAckTime=lastServer;lastSeenAck=ack;}
  if(!initialized||reset||stop||authority.resetEpoch!=authorityEpoch){Seed(authority);return;}
  if(sequence>0)history.Remember(sequence,PvpPredictedMotion.Read(taxi.body));
  Vector3 p,v,a;Quaternion q;if(!history.Error(ack,authority,out p,out q,out v,out a))return;
  LargestError=Mathf.Max(LargestError,p.magnitude);
  if(p.sqrMagnitude>64||Quaternion.Angle(Quaternion.identity,q)>100){Seed(authority);return;}
  error=p.sqrMagnitude>.000625f?p:Vector3.zero;rotationError=Quaternion.Angle(Quaternion.identity,q)>.5f?q:Quaternion.identity;
  velocityError=v.sqrMagnitude>.01f?v:Vector3.zero;angularError=a;Corrections++;
 }
 void Seed(PvpPredictedMotion state){
  taxi.body.position=state.position;taxi.body.rotation=state.rotation;taxi.body.velocity=state.velocity;taxi.body.angularVelocity=state.angularVelocity;
  taxi.transform.SetPositionAndRotation(state.position,state.rotation);taxi.ClearPredictedPedals();taxi.RestorePedalIntent(state.pedalDirection);authorityEpoch=state.resetEpoch;history.Clear();error=velocityError=angularError=Vector3.zero;rotationError=Quaternion.identity;initialized=true;lastAckTime=Time.realtimeSinceStartupAsDouble;HardResets++;
 }
 public void Step(PvpNativeDrive input,float seconds){
  if(!initialized)return;
  if(Steps==0)lastAckTime=Time.realtimeSinceStartupAsDouble;
  if(sequence>0)history.Remember(sequence,PvpPredictedMotion.Read(taxi.body));
  float fraction=1-Mathf.Exp(-8*seconds);Vector3 p=Vector3.ClampMagnitude(error*fraction,3*seconds),v=Vector3.ClampMagnitude(velocityError*fraction,8*seconds),a=Vector3.ClampMagnitude(angularError*fraction,4*seconds);
  Quaternion q=Quaternion.Slerp(Quaternion.identity,rotationError,fraction);
  if(p.sqrMagnitude>1e-12f)taxi.body.position+=p;
  if(Quaternion.Angle(Quaternion.identity,q)>.0001f)taxi.body.rotation=q*taxi.body.rotation;
  if(v.sqrMagnitude>1e-12f)taxi.body.velocity+=v;
  if(a.sqrMagnitude>1e-12f)taxi.body.angularVelocity+=a;
  if(p.sqrMagnitude+v.sqrMagnitude+a.sqrMagnitude>1e-12f||Quaternion.Angle(Quaternion.identity,q)>.0001f)history.Shift(p,q,v,a);
  error-=p;velocityError-=v;angularError-=a;rotationError=rotationError*Quaternion.Inverse(q);
  sequence=input.sequence;Steps++;PvpNativeInputQueue.Apply(taxi,input,locked||input.blocked||Time.realtimeSinceStartupAsDouble-lastServer>.5||Time.realtimeSinceStartupAsDouble-lastAckTime>.5,seconds);
 }
}

// Compact authoritative free-driving chain, including the original cut points.
// Needed by local native H steering; unchanged chains retain their path object.
[Serializable]public sealed class PvpNativeFreeRoute {
 public int[] edges;public float start,end;
 public static PvpNativeFreeRoute Capture(HdRoadGraph graph,HdRoadGraph.RoutePath route){
  if(route==null||route.edges.Count==0)return null;float error;
  return new PvpNativeFreeRoute{edges=route.edges.ToArray(),start=graph.paths[route.edges[0]].Project(route.path.Position(0),out error),end=graph.paths[route.edges[route.edges.Count-1]].Project(route.path.Position(route.path.length),out error)};
 }
 public HdRoadGraph.RoutePath Restore(HdRoadGraph graph,HdRoadGraph.RoutePath current){
  if(edges==null||edges.Length==0||edges.Length>128||float.IsNaN(start)||float.IsNaN(end)||float.IsInfinity(start)||float.IsInfinity(end))throw new ArgumentException("Invalid authoritative free route");
  for(int i=0;i<edges.Length;i++){if(edges[i]<0||edges[i]>=graph.edges.Length||i>0&&Array.IndexOf(graph.edges[edges[i-1]].next,edges[i])<0)throw new ArgumentException("Disconnected authoritative free route");}
  if(start<0||start>graph.paths[edges[0]].length+.01f||end<0||end>graph.paths[edges[edges.Length-1]].length+.01f)throw new ArgumentException("Invalid authoritative route range");
  bool same=current!=null&&current.edges.Count==edges.Length;
  if(same)for(int i=0;i<edges.Length;i++)same&=current.edges[i]==edges[i];
  if(same&&(current.path.Position(0)-graph.paths[edges[0]].Position(start)).sqrMagnitude<.0001f&&(current.path.Position(current.path.length)-graph.paths[edges[edges.Length-1]].Position(end)).sqrMagnitude<.0001f)return current;
  return graph.Compose(new List<int>(edges),start,end);
 }
}
