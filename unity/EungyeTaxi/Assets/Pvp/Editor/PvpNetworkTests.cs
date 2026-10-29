using System;
using UnityEngine;
using Pvp.Network;
using Pvp.Map;
using Pvp.Driving;
namespace Pvp.Tests.EditMode {
public static class PvpNetworkTests {
 static void Check(bool yes,string name){if(!yes)throw new Exception("PVP_NETWORK: "+name);}
 public static void Run(){
  Check(typeof(PvpInputBuffer).GetMethod("InputForTick")!=null,"held input sampling missing"); Check(typeof(PvpBootstrap).Assembly.GetType("Pvp.Network.PvpInputBuffer")!=null,"burst input coalescing buffer missing"); PvpTask2Tests.Run();var definition=PvpMapDefinition.CreateDefault();
  try{var map=new PvpMapRuntime(definition);var hello=PvpAdmission.Hello(map.MapHash);Check(PvpAdmission.Reject(hello,map.MapHash)==null,"valid admission");
   hello.Protocol="other";Check(PvpAdmission.Reject(hello,map.MapHash)=="protocol mismatch","protocol rejection");hello=PvpAdmission.Hello(map.MapHash);hello.Build="other";Check(PvpAdmission.Reject(hello,map.MapHash)=="build mismatch","build rejection");hello=PvpAdmission.Hello("other");Check(PvpAdmission.Reject(hello,map.MapHash)=="map mismatch","map rejection");hello=PvpAdmission.Hello(map.MapHash);hello.Rules="other";Check(PvpAdmission.Reject(hello,map.MapHash)=="settings mismatch","rules rejection");
   Check(!new PvpDriveCommand{Sequence=1,Throttle=float.NaN}.Valid,"NaN rejected");Check(!new PvpDriveCommand{Sequence=1,Steering=2}.Valid,"range rejected");Check(!new PvpDriveCommand{Sequence=0}.Valid,"sequence rejected");
   var burst=new PvpInputBuffer();burst.Enqueue(new PvpDriveCommand{Sequence=1,Throttle=1});burst.Enqueue(new PvpDriveCommand{Sequence=2,Brake=1});PvpDriveCommand latest;
   Check(burst.TakeLatest(out latest)&&latest.Sequence==2&&latest.Brake==1&&burst.Count==0,"burst uses latest control in one authority tick");Check(!burst.TakeLatest(out latest),"burst cannot advance extra ticks");
   bool duplicateRejected=false;try{burst.Enqueue(new PvpDriveCommand{Sequence=2});}catch(ArgumentException){duplicateRejected=true;}Check(duplicateRejected,"replayed input rejected");
   var heldBuffer=new PvpInputBuffer();heldBuffer.Enqueue(new PvpDriveCommand{Sequence=1,Throttle=1,Connected=true});heldBuffer.Enqueue(new PvpDriveCommand{Sequence=2,Throttle=1,Connected=true});
   var batchedMotor=new PvpNetworkMotor(map,0);var evenMotor=new PvpNetworkMotor(map,0);int heldAck;
   for(int i=0;i<2;i++){batchedMotor.Step(heldBuffer.InputForTick(out heldAck));evenMotor.Step(new PvpVehicleInput(1,0,0));Check(heldAck==2,"coalesced ACK stable");}
   Check(Vector3.Distance(batchedMotor.Capture().State.Position,evenMotor.Capture().State.Position)<.0001f,"batched throttle stays held between packets");
   heldBuffer.Enqueue(new PvpDriveCommand{Sequence=3,Brake=1,Connected=true});Check(heldBuffer.InputForTick(out heldAck).Brake==1&&heldAck==3,"latest brake immediate");
   for(int i=1;i<PvpInputBuffer.FreshTicks;i++)Check(heldBuffer.InputForTick(out heldAck).Connected,"fresh held input");Check(!heldBuffer.InputForTick(out heldAck).Connected,"stale held input decelerates");
   var prediction=new PvpPrediction(map,0);var server=new PvpNetworkMotor(map,0);var input=new PvpVehicleInput(1,0,.1f);
   PvpDriveSnapshot snapshot=default(PvpDriveSnapshot);
   for(int i=1;i<=100;i++){var command=prediction.Predict(input);server.Step(command.Input);if(i==75){snapshot=server.Capture();snapshot.Ack=75;snapshot.Tick=75;}}
   prediction.Reconcile(snapshot);Check(prediction.PendingCount==25,"pending replay pruning");Check(Vector3.Distance(prediction.State.State.Position,server.Capture().State.Position)<.0001f,"replay convergence");Check(Mathf.Abs(prediction.State.State.YawDegrees-server.Capture().State.YawDegrees)<.0001f,"yaw convergence");
   prediction.Reconcile(snapshot);Check(prediction.PendingCount==25,"duplicate snapshot ignored");
   bool failed=false;try{snapshot.Ack=200;snapshot.Tick=200;prediction.Reconcile(snapshot);}catch(ArgumentException){failed=true;}Check(failed,"future ACK rejected");
   var bounded=new PvpPrediction(map,0);for(int i=0;i<128;i++)bounded.Predict(input);failed=false;try{bounded.Predict(input);}catch(InvalidOperationException){failed=true;}Check(failed,"bounded history");
   Debug.Log("PVP_NETWORK_MODEL_OK admission/range/sequence/replay/duplicate/future-ack/bounded-history/burst/held/freshness");
  }finally{UnityEngine.Object.DestroyImmediate(definition);}
 }
}}