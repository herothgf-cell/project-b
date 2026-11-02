using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
// The runner compiles AdvanceBehavior and availability getters verbatim from
// PassengerJobs.cs. Only Unity/session collaborators are doubled below.
namespace UnityEngine {
 public class Object {public static implicit operator bool(Object value){return !ReferenceEquals(value,null);}}
 public class MonoBehaviour:Object {public T GetComponent<T>() where T:class{return null;}}
 public struct Vector3 {public float x,y,z;public float magnitude {get{return (float)Math.Sqrt(x*x+y*y+z*z);}}}
 public static class Mathf {public static float Max(float a,float b){return Math.Max(a,b);}public static float Clamp01(float a){return Math.Min(1,Math.Max(0,a));}}
 public static class Time {public static float timeScale=1;}
}
public sealed class PassengerBehaviorUI:UnityEngine.Object {public void NotifyStateChanged(){}}
public sealed class TestBody {public UnityEngine.Vector3 velocity;}
public sealed class TestTaxi {public TestBody body=new TestBody();}
public sealed class TestManners {public TestScore ledger=new TestScore();}
public sealed class TestScore {public int Score=100;}
public sealed class TestRequests:UnityEngine.Object {public bool HasPendingChoice;public int ScheduledChoiceCount;public bool SafeToRespond(){return true;}public void SuspendUnansweredForSleep(){}}
public sealed class TestBusiness {public int Saves;public void SaveCheckpoint(){Saves++;}}
public sealed class TestSession {public bool paused;public TestRequests requests=new TestRequests();public TestTaxi taxi=new TestTaxi();public TestManners manners=new TestManners();public TestBusiness business=new TestBusiness();}
public sealed class TestNavigation {public float remaining;}
public sealed class TestOffer {public int pickup,drop;}
public sealed class TestDrunk {public bool Waiting;}
public partial class PassengerJobs:UnityEngine.MonoBehaviour {
 public PassengerBehaviorState Behavior;public PassengerSituationState Situation;public TestSession session=new TestSession();public readonly FareTrip trip=new FareTrip();public TestNavigation nav=new TestNavigation();public TestOffer current=new TestOffer();public TestDrunk DrunkRide;
 float boardingDistance=100;
 public bool Active {get{return trip.State==TripState.ToPickup||trip.State==TripState.OnTrip;}}
 public bool SituationPending {get{return Situation!=null&&Situation.Pending!=PassengerSituationKind.None;}}
 bool At(int index){return true;}bool Board(){return trip.Board();}bool CompleteDelivery(bool wake){return false;}void ApplyBehaviorDirt(){}
}
public static class PassengerRestAdapterTests {
 static int checks;
 static void Check(bool ok,string reason){checks++;if(!ok){Console.WriteLine("FAIL "+reason);Environment.Exit(1);}}
 static T Snapshot<T>(T value){using(var stream=new MemoryStream()){var formatter=new BinaryFormatter();formatter.Serialize(stream,value);stream.Position=0;return (T)formatter.Deserialize(stream);}}
 static PassengerJobs Fixture(string reason,float remaining){
  var jobs=new PassengerJobs();jobs.trip.Accept(5000,100);jobs.trip.Board();jobs.nav.remaining=remaining;
  var passenger=PassengerBehaviorState.Create("rest-adapter",PassengerType.Sleeping);passenger.Variant=0;passenger.Board();passenger.Advance(21,false,true,.3f,100);if(reason=="quiet")passenger.Quiet=true;
  jobs.Behavior=passenger;jobs.Situation=PassengerSituationState.Create("trip",0,0,0);
  if(reason=="budget")jobs.Situation.Reactions=2;if(reason=="cooldown"){jobs.Situation.Elapsed=5;jobs.Situation.LastReactionAt=0;}
  jobs.Situation.ApplyPhysicalObservation(passenger,1,0,0);
  jobs.Situation.Observe(1,false,true,reason!="modal",passenger.Quiet,reason=="budget"?3:1,1,0,0,false);
  return jobs;
 }
 public static void Main(){
  foreach(float remaining in new[]{15f,14.9f,1f})foreach(string reason in new[]{"quiet","budget","cooldown","modal"}){
   var j=Fixture(reason,remaining);Check(j.Behavior.Choice==-1&&j.Behavior.HasChoice&&!j.BehaviorChoicesOpen&&!j.SituationPending,"late initial choice is unavailable after suppressed jolt "+reason+"/"+remaining);
   j.AdvanceBehavior(14);Check(!j.Behavior.Sleeping&&j.Behavior.JoltCalmRemaining==1,"actual jobs adapter advances late calm timer "+reason);
   j.Behavior=Snapshot(j.Behavior);j.Situation=Snapshot(j.Situation);j.AdvanceBehavior(1);
   Check(j.Behavior.Sleeping&&!j.SituationPending&&j.Behavior.Choice==-1,"actual jobs adapter resumes rest after restored unavailable initial choice "+reason+"/"+remaining);
  }
  var valid=Fixture("quiet",16);Check(valid.BehaviorChoicesOpen,"initial choice remains valid before last15percent");valid.AdvanceBehavior(30);Check(!valid.Behavior.Sleeping&&valid.Behavior.Choice==-1,"actual jobs adapter preserves valid pending initial choice");
  var pending=Fixture("quiet",15);pending.Situation.Pending=PassengerSituationKind.Jolt;pending.Situation.Reactions=1;pending.AdvanceBehavior(30);Check(!pending.Behavior.Sleeping&&pending.SituationPending,"real situation reply remains pending even near arrival");
  var request=Fixture("quiet",15);request.session.requests.HasPendingChoice=true;request.AdvanceBehavior(30);Check(!request.Behavior.Sleeping&&request.session.requests.HasPendingChoice,"real existing request is not automatically answered or retired");
  Console.WriteLine("PASS PassengerRestAdapterTests "+checks);
 }
}
