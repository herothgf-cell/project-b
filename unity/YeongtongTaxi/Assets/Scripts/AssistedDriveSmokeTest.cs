using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

public class AssistedDriveSmokeTest : MonoBehaviour {
    public DriveSession session;
    readonly StringBuilder report=new StringBuilder();bool ok=true;
    void Check(bool value,string name){report.AppendLine((value?"PASS ":"FAIL ")+name);ok&=value;}
    void DriverDemand(string context){var taxi=session.taxi;var saved=taxi.body.velocity;taxi.body.velocity=taxi.transform.forward*18;float steering=0;var demand=new DriveMath.Demand{motor=.73f,brake=.17f};session.assisted.Apply(ref steering,ref demand);Check(demand.motor==.73f&&demand.brake==.17f,"driver throttle and brake unchanged: "+context);taxi.body.velocity=saved;}
    void DriverSpeed(float target){var taxi=session.taxi;taxi.autoThrottle=taxi.Speed<target?.35f:0;taxi.autoBrake=taxi.Speed>target+.25f;}
    IEnumerator DriveFor(float seconds,float target){for(float t=0;t<seconds;t+=.1f){DriverSpeed(target);yield return new WaitForSeconds(.1f);}}
    void Place(AssistedRoadNetwork.Lane lane,float distance){var taxi=session.taxi;session.assisted.SetActive(false);taxi.autoThrottle=0;taxi.autoSteer=0;taxi.autoBrake=false;taxi.body.velocity=Vector3.zero;taxi.body.angularVelocity=Vector3.zero;taxi.body.position=lane.path.Position(distance)+Vector3.up*.65f;taxi.body.rotation=Quaternion.LookRotation(lane.path.Tangent(distance));taxi.transform.SetPositionAndRotation(taxi.body.position,taxi.body.rotation);Physics.SyncTransforms();}
    IEnumerator Start(){
        yield return new WaitForSeconds(1);var taxi=session.taxi;taxi.automated=true;session.assisted.allowAutomated=true;
        foreach(var car in session.traffic.cars){car.Recycle();car.enabled=false;}
        var assist=session.assisted;AssistedRoadNetwork.Lane selected=null;
        Check(!session.drivingCamera.FirstPerson&&assist.Active,"third person starts with assisted driving status="+assist.Status);
        session.drivingCamera.SetFirstPerson(true);
        Check(!assist.Active,"first person defaults to manual driving");
        session.drivingCamera.SetFirstPerson(false);
        Check(assist.Active,"third person restores assisted driving");
        assist.SetActive(false);session.drivingCamera.SetFirstPerson(false);
        Check(!assist.Active,"same view preserves manual override");
        session.drivingCamera.SetFirstPerson(true);session.drivingCamera.SetFirstPerson(false);
        Check(assist.Active,"view change reapplies default after override");
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-viewDefaultsOnly")>=0){Debug.Log(report.ToString());Application.Quit(ok?0:1);yield break;}
        foreach(var lane in assist.network.lanes)if(lane.count==2&&lane.index==0&&lane.path.length>90){selected=lane;break;}
        Check(selected!=null,"two-lane road fixture exists");
        if(selected!=null){
            Place(selected,8);yield return new WaitForSeconds(.5f);Check(assist.SetActive(true),"enable on correct lane");
            Check(!assist.RequestLaneChange(1),"reject stationary lane change");
            DriverDemand("straight");yield return DriveFor(3,6);
            float error;selected.path.Project(taxi.transform.position,out error);Check(assist.Active&&error<1.2f,"physical lane holding error="+error);
            var occupied=networkObstacle(assist.network.Adjacent(selected,1),taxi.transform.position);
            Check(!assist.RequestLaneChange(1)&&!assist.Changing,"occupied adjacent lane rejected without queue");Destroy(occupied);yield return null;
            Check(assist.RequestLaneChange(1),"right change accepted");Check(!assist.RequestLaneChange(1),"no repeat while changing");
            Check(!assist.RequestTurn(-1),"turn cannot interrupt lane change");
            DriverDemand("lane change");yield return DriveFor(6,6);Check(assist.Active&&assist.CurrentLane.index==1&&!assist.Changing,"right change physically completes");
            Check(assist.RequestLaneChange(-1),"left change accepted");yield return DriveFor(6,6);
            Check(assist.Active&&assist.CurrentLane.index==0&&!assist.Changing,"left change physically completes");
            var before=taxi.transform.position;Time.timeScale=0;Check(!assist.RequestLaneChange(1),"paused input ignored");yield return new WaitForSecondsRealtime(.2f);Check(Vector3.Distance(before,taxi.transform.position)<.01f,"pause holds body");Time.timeScale=1;
            taxi.ResetTaxi();Check(!assist.Active&&assist.PendingTurn==0,"reset clears mode and turn");
            Place(selected,8);yield return new WaitForSeconds(.5f);assist.SetActive(true);taxi.autoThrottle=-1;yield return new WaitForSeconds(.5f);Check(!assist.Active,"reverse returns to manual");
        }
        // Exercise both curved connections on real roads, not transform teleportation.
        foreach(int turn in new[]{-1,1}){
            AssistedRoadNetwork.Lane approach=null;
            foreach(var lane in assist.network.lanes)if(lane.path.length>45&&assist.network.Connection(lane,turn)!=null){approach=lane;break;}
            Check(approach!=null,"turn fixture "+turn);if(approach==null)continue;
            Place(approach,approach.path.length-24);yield return new WaitForSeconds(.5f);Check(assist.SetActive(true),"enable turn "+turn);
            Check(assist.RequestTurn(turn),"reserve turn "+turn);var destination=assist.network.Connection(approach,turn).target;
            DriverDemand("turn approach "+turn);bool checkedTurn=false;
            for(float elapsed=0;elapsed<22;elapsed+=.1f){DriverSpeed(4);yield return new WaitForSeconds(.1f);if(assist.Turning&&!checkedTurn){DriverDemand("active turn "+turn);checkedTurn=true;}if(assist.CurrentLane==destination&&!assist.Turning)break;}
            Check(assist.Active&&assist.CurrentLane==destination&&!assist.Turning,"physical turn completes "+turn+" status="+assist.Status);
        }
        // Isolate full-throttle assisted speed from junctions and real-map obstacles.
        var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.name="Assisted speed test ground";
        ground.transform.position=new Vector3(-600,-.5f,-1000);ground.transform.localScale=new Vector3(1000,1,40);
        assist.SetActive(false);
        assist.network.Build(new MapData{roads=new[]{new MapRoad{id="speed-fixture",width=14,lanes=4,points=new[]{new MapPoint{x=-1000,z=-1000},new MapPoint{x=-200,z=-1000}}}}});
        var straight=assist.network.lanes[0];Place(straight,20);yield return new WaitForSeconds(.5f);
        Check(assist.SetActive(true),"enable isolated straight speed run");taxi.autoThrottle=1;
        float maxError=0,maxTilt=0;
        for(int i=0;i<140;i++){yield return new WaitForSeconds(.1f);float error;straight.path.Project(taxi.transform.position,out error);maxError=Mathf.Max(maxError,error);maxTilt=Mathf.Max(maxTilt,Vector3.Angle(taxi.transform.up,Vector3.up));}
        Check(assist.Active&&taxi.Speed*3.6f>60,"assistance does not impose a cruising speed cap kph="+(taxi.Speed*3.6f));
        Check(maxError<1&&maxTilt<10,"straight stable error="+maxError+" tilt="+maxTilt);
        // Diagnose small corrections at speed through the complete controller/physics demand path.
        taxi.body.position+=TrafficSystem.Right(straight.path.Tangent(20))*.6f;taxi.transform.position=taxi.body.position;Physics.SyncTransforms();
        float minimum=taxi.Speed;
        for(int i=0;i<30;i++){yield return new WaitForSeconds(.1f);minimum=Mathf.Min(minimum,taxi.Speed);}
        Check(assist.Active&&minimum*3.6f>40,"small lane correction preserves speed minKph="+(minimum*3.6f));
        taxi.autoThrottle=0;taxi.autoBrake=true;yield return new WaitForSeconds(5);
        Check(Mathf.Abs(taxi.Speed)<.5f,"manual brake still stops assisted taxi");
        Place(straight,straight.path.length-2);yield return new WaitForSeconds(.5f);assist.SetActive(true);DriverDemand("unconnected road end");
        Place(straight,straight.path.length-.2f);yield return new WaitForSeconds(.5f);assist.SetActive(true);DriverDemand("end handoff");Check(!assist.Active,"unconnected endpoint releases steering instead of auto braking");
        string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts"));Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder,"assisted-drive-test.txt"),"success="+ok+"\n"+report);Debug.Log(report.ToString());Application.Quit(ok?0:1);
    }
    GameObject networkObstacle(AssistedRoadNetwork.Lane lane,Vector3 position){float error;float at=lane.path.Project(position,out error);var obj=new GameObject("Occupied lane fixture");obj.layer=9;obj.transform.position=lane.path.Position(at)+Vector3.up*.8f;obj.transform.rotation=Quaternion.LookRotation(lane.path.Tangent(at));obj.AddComponent<BoxCollider>().size=new Vector3(1.8f,1.3f,4);Physics.SyncTransforms();return obj;}
}
