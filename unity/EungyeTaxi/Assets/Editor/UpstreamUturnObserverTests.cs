using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
public static class UpstreamUturnObserverTests {
 static void Check(bool value,string message){if(!value)throw new Exception("UPSTREAM UTURN: "+message);}
 public static void Run(){var root=new GameObject("upstream Uturn fixture");try{
  var traffic=root.AddComponent<TrafficSystem>();traffic.manualClock=true;traffic.manualTime=2;
  var j=new TrafficJunction{name="upstream",direction=Vector3.forward,halfWidth=new[]{10f,10f},leftTurnMode=LeftTurnMode.ProtectedShared};traffic.junctions.Add(j);
  var incoming=new RoadMapLane{id="incoming",ruleId=j.name,points=new[]{new MapPoint{x=6,z=-100},new MapPoint{x=6,z=-30}}};
  var outgoing=new RoadMapLane{id="outgoing",points=new[]{new MapPoint{x=-6,z=-30},new MapPoint{x=-6,z=-100}}};
  var curve=UturnMovement.Geometry(incoming,outgoing);var turn=new RoadMapLane{id="join:upstream",ruleId=j.name,turn=-2,points=curve.Select(p=>new MapPoint{x=p.x,z=p.z}).ToArray()};incoming.next=new[]{turn.id};
  var document=new RoadMapDocument{lanes=new[]{incoming,outgoing,turn},facilities=new[]{new RoadMapFacility{id="condition",kind="sign",rule=true,type=UturnMovement.LeftGreen,laneId=incoming.id,junctionId=j.name,axis=0,sign=1}}};
  typeof(TrafficSystem).GetField("roadMap",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(traffic,new MapData{editorDocument=document});
  var observer=new UpstreamUturnObserver();int count=0;Func<string> id=()=>"incident-"+(++count);var gate=curve[0];
  Check(observer.Observe(document,traffic,gate-Vector3.forward,gate+Vector3.forward*.1f,Vector3.forward,root,id)==null,"gate alone never scores straight driver");
  var saved=JsonUtility.FromJson<UpstreamUturnSnapshot>(JsonUtility.ToJson(observer.Capture()));Check(saved!=null&&saved.allowed,"left green permission captured at gate");
  observer=new UpstreamUturnObserver();observer.Restore(saved);traffic.manualTime=20;UpstreamUturnSnapshot result=null;var previous=gate+Vector3.forward*.1f;
  for(int i=1;i<curve.Length;i++){var heading=(curve[Math.Min(i+1,curve.Length-1)]-curve[i-1]).normalized;var completed=observer.Observe(document,traffic,previous,curve[i],heading,root,id);if(completed!=null){Check(result==null,"one event only");result=completed;}previous=curve[i];}
  Check(result!=null&&result.allowed&&result.id==saved.id,"restored turn retains original legal entry despite later red");
  observer.Reset();traffic.manualTime=20;observer.Observe(document,traffic,gate-Vector3.forward,gate+Vector3.forward*.1f,Vector3.forward,root,id);traffic.manualTime=2;result=null;previous=gate+Vector3.forward*.1f;
  for(int i=1;i<curve.Length;i++){var completed=observer.Observe(document,traffic,previous,curve[i],(curve[Math.Min(i+1,curve.Length-1)]-curve[i-1]).normalized,root,id);if(completed!=null)result=completed;previous=curve[i];}
  Check(result!=null&&!result.allowed,"later green never erases forbidden entry");
  observer.Reset();j.observationAxis=0;j.observationSign=1;j.rightTurnObservation=new RoadObservation{known=true,pedestrian=PedestrianState.Crossing};
  observer.Observe(document,traffic,gate-Vector3.forward,gate+Vector3.forward*.1f,Vector3.forward,root,id);Check(observer.Capture()!=null&&!observer.Capture().allowed,"actual crossing pedestrian blocks an otherwise legal left phase");j.rightTurnObservation=null;
  observer.Reset();observer.Observe(document,traffic,gate-Vector3.forward,gate+Vector3.forward*.1f,Vector3.forward,root,id);
  Check(observer.Observe(document,traffic,gate+Vector3.forward*.1f,gate+Vector3.forward*10,Vector3.forward,root,id)==null&&observer.Capture()==null,"through driver abandons candidate without event");
  observer.Observe(document,traffic,gate-Vector3.forward,gate+Vector3.forward*.1f,Vector3.forward,root,id);observer.Reset();Check(observer.Capture()==null,"reset clears provisional observation");
  var body=root.AddComponent<Rigidbody>();body.velocity=Vector3.forward*2;
  var blocker=new GameObject("intersection traffic outside upstream curve");blocker.transform.SetParent(root.transform);blocker.layer=8;blocker.AddComponent<Rigidbody>().isKinematic=true;blocker.AddComponent<BoxCollider>().size=new Vector3(1.8f,1.5f,4.2f);blocker.transform.forward=Vector3.right;
  foreach(bool obstructs in new[]{false,true}){
   blocker.transform.position=obstructs?curve[32]:j.center;Physics.SyncTransforms();observer.Reset();observer.Restore(saved);result=null;previous=gate+Vector3.forward*.1f;
   for(int i=1;i<curve.Length;i++){var completed=observer.Observe(document,traffic,previous,curve[i],(curve[Math.Min(i+1,curve.Length-1)]-curve[i-1]).normalized,root,id);if(completed!=null)result=completed;previous=curve[i];}
   Check(result!=null&&result.yieldViolated==obstructs,obstructs?"real curve obstruction records yield violation":"unrelated intersection crossing traffic causes no upstream yield penalty");
  }
  UnityEngine.Object.DestroyImmediate(blocker);
  // Repositioning the same curve over the canonical plane opts it out of this observer.
  foreach(var lane in document.lanes)foreach(var p in lane.points)p.z+=20;document.revision++;
  Check(observer.Observe(document,traffic,gate+Vector3.forward*19,gate+Vector3.forward*21,Vector3.forward,root,id)==null&&observer.Capture()==null,"ordinary global-stop Uturn remains with existing observer");
  Debug.Log("UPSTREAM_UTURN_OBSERVER_OK entry phase, save restore, one event, through, reset, global gate exclusion");
 }finally{UnityEngine.Object.DestroyImmediate(root);}}
}
