using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable] public sealed class UpstreamUturnSnapshot {
 public string connector,junction,lane,id;public int axis,sign;public bool allowed,confirmed,yieldViolated;public RoadObservation observation;
 public UpstreamUturnSnapshot Copy(){return new UpstreamUturnSnapshot{connector=connector,junction=junction,lane=lane,id=id,axis=axis,sign=sign,allowed=allowed,confirmed=confirmed,yieldViolated=yieldViolated,observation=observation==null?null:observation.Copy()};}
}

// A road's common stop plane cannot observe a signed turn completed upstream of it.
// Observe that connector only; crossing its gate while continuing straight is provisional.
public sealed class UpstreamUturnObserver {
 sealed class Gate {public string lane;public RoadMapLane connector;public TrafficJunction junction;public int axis,sign;public Vector3 point,direction;public float tolerance;public AssistedRoadNetwork.Path path;}
 readonly List<Gate> gates=new List<Gate>();RoadMapDocument cached;int revision=-1;UpstreamUturnSnapshot pending;
 readonly TrafficOverlapQuery overlaps=new TrafficOverlapQuery();
 public UpstreamUturnSnapshot Capture(){return pending==null?null:pending.Copy();}
 public void Restore(UpstreamUturnSnapshot value){pending=value==null||value.observation==null?null:value.Copy();}
 public void Reset(){pending=null;}
 // A relocated turn's conflict corridor is its remaining curve and outgoing merge,
 // not the shared junction centre. Use the same body/prediction margins as NPC UturnClear.
 public bool PathClear(AssistedRoadNetwork.Path path,float from,GameObject self){
  int count=overlaps.Sphere(path.Position(path.length*.5f),100,(1<<8)|(1<<9),QueryTriggerInteraction.Ignore);
  var incoming=path.Tangent(0);var start=path.Position(0);var outgoing=path.Tangent(path.length);
  for(int i=0;i<count;i++){
   var hit=overlaps.hits[i];var root=hit.attachedRigidbody?hit.attachedRigidbody.gameObject:hit.gameObject;if(root==self)continue;
   var heading=root.transform.forward;var delta=root.transform.position-start;
   // The queue behind this directed entry cannot obstruct the curve in front of it.
   if(Vector3.Dot(heading,incoming)>.95f&&Vector3.Dot(delta,incoming)<-4.5f&&Mathf.Abs(Vector3.Dot(delta,TrafficSystem.Right(incoming)))<.8f)continue;
   var car=root.GetComponent<TrafficCar>();float speed=car?car.speed:hit.attachedRigidbody?hit.attachedRigidbody.velocity.magnitude:0;
   for(float at=Mathf.Max(0,from-2);at<=path.length+8;at+=1.5f){
    var point=at<=path.length?path.Position(at):path.Position(path.length)+outgoing*(at-path.length);
    var offset=point-root.transform.position;float along=Vector3.Dot(offset,heading),side=Mathf.Abs(Vector3.Dot(offset,TrafficSystem.Right(heading)));
    if((hit.ClosestPoint(point+Vector3.up*.85f)-point-Vector3.up*.85f).sqrMagnitude<1.15f*1.15f||side<3.2f&&along>=-3&&along<Mathf.Max(8,speed*6+5))return false;
   }
  }
  return true;
 }
 void Prepare(RoadMapDocument document,TrafficSystem traffic){
  if(document==cached&&document!=null&&revision==document.revision)return;
  gates.Clear();cached=document;revision=document==null?-1:document.revision;if(document==null)return;
  var lanes=document.lanes.ToDictionary(l=>l.id);
  foreach(var from in document.lanes){if(from.disabled||from.turn!=0||from.change!=0||from.points==null||from.points.Length<2)continue;
   var condition=UturnMovement.Condition(document,from.id);if(condition==null)continue;var junction=traffic.junctions.Find(j=>j.name==condition.junctionId);if(junction==null)continue;
   foreach(var next in from.next){RoadMapLane turn;if(!lanes.TryGetValue(next,out turn)||turn.disabled||turn.turn!=UturnMovement.Turn||turn.ruleId!=junction.name||turn.points==null||turn.points.Length<3)continue;
    var direction=(from.points[from.points.Length-1].V()-from.points[from.points.Length-2].V()).normalized;int axis=Mathf.Abs(Vector3.Dot(direction,traffic.AxisDirection(junction,0)))>.7f?0:1,sign=traffic.Sign(direction,axis,junction);
    var stop=traffic.StopPosition(junction,axis,sign);var normal=traffic.StopNormal(junction,axis,sign);
    if(turn.points.Any(p=>Vector3.Dot(p.V()-stop,normal)>=-.25f))continue;
    gates.Add(new Gate{lane=from.id,connector=turn,junction=junction,axis=axis,sign=sign,point=turn.points[0].V(),direction=direction,tolerance=turn.width*.5f+1,path=new AssistedRoadNetwork.Path(turn.points.Select(p=>p.V()).ToArray())});
   }
  }
 }
 public UpstreamUturnSnapshot Observe(RoadMapDocument document,TrafficSystem traffic,Vector3 before,Vector3 after,Vector3 heading,GameObject self,Func<string> incidentId){
  if(document==null||traffic==null||Vector3.Distance(before,after)>10){Reset();return null;}Prepare(document,traffic);
  if(pending!=null){
   var gate=gates.Find(g=>g.connector.id==pending.connector&&g.junction.name==pending.junction);
   if(gate==null){Reset();return null;}
   float error;float at=gate.path.Project(after,out error);float forward=Vector3.Dot(after-gate.point,gate.direction);
   if(error>gate.tolerance||forward< -3||Vector3.Dot(heading,gate.path.Tangent(at))<.5f){Reset();return null;}
   if(at>2&&Vector3.SignedAngle(gate.direction,heading,Vector3.up)<-15)pending.confirmed=true;
   var body=self?self.GetComponent<Rigidbody>():null;
   if(pending.confirmed&&body&&body.velocity.magnitude>.3f&&!PathClear(gate.path,at,self))pending.yieldViolated=true;
   if(pending.confirmed&&at>gate.path.length*.5f&&Vector3.Dot(heading,gate.direction)<-.7f){var completed=pending;pending=null;return completed;}
   return null;
  }
  foreach(var gate in gates){
   if(Vector3.Dot(heading,gate.direction)<.7f)continue;float a=Vector3.Dot(before-gate.point,gate.direction),b=Vector3.Dot(after-gate.point,gate.direction);
   if(a>=0||b<0||Mathf.Abs(Vector3.Dot(after-gate.point,TrafficSystem.Right(gate.direction)))>gate.tolerance)continue;
   var observed=traffic.Observe(gate.junction,gate.axis,gate.sign,gate.direction,self);
   pending=new UpstreamUturnSnapshot{connector=gate.connector.id,junction=gate.junction.name,lane=gate.lane,axis=gate.axis,sign=gate.sign,id=incidentId(),observation=observed.Copy(),allowed=observed.PedestrianClear&&traffic.UturnPermitted(gate.junction,gate.axis,gate.sign,gate.lane)};break;
  }
  return null;
 }
}
