using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

public class AssistedDriveSmokeTest : MonoBehaviour {
    public DriveSession session;
    readonly StringBuilder report=new StringBuilder();bool ok=true;
    void Check(bool value,string name){report.AppendLine((value?"PASS ":"FAIL ")+name);ok&=value;}
    void Place(AssistedRoadNetwork.Lane lane,float distance){var taxi=session.taxi;session.assisted.SetActive(false);taxi.autoThrottle=0;taxi.autoSteer=0;taxi.autoBrake=false;taxi.body.velocity=Vector3.zero;taxi.body.angularVelocity=Vector3.zero;taxi.body.position=lane.path.Position(distance)+Vector3.up*.65f;taxi.body.rotation=Quaternion.LookRotation(lane.path.Tangent(distance));taxi.transform.SetPositionAndRotation(taxi.body.position,taxi.body.rotation);Physics.SyncTransforms();}
    IEnumerator Start(){
        yield return new WaitForSeconds(1);var taxi=session.taxi;taxi.automated=true;session.assisted.allowAutomated=true;
        foreach(var car in session.traffic.cars){car.Recycle();car.enabled=false;}
        var assist=session.assisted;AssistedRoadNetwork.Lane selected=null;
        foreach(var lane in assist.network.lanes)if(lane.count==2&&lane.index==0&&lane.path.length>90){selected=lane;break;}
        Check(selected!=null,"two-lane road fixture exists");
        if(selected!=null){
            Place(selected,8);yield return new WaitForSeconds(.5f);Check(assist.SetActive(true),"enable on correct lane");
            Check(!assist.RequestLaneChange(1),"reject stationary lane change");
            taxi.autoThrottle=.35f;yield return new WaitForSeconds(3);
            float error;selected.path.Project(taxi.transform.position,out error);Check(assist.Active&&error<1.2f,"physical lane holding error="+error);
            var occupied=networkObstacle(assist.network.Adjacent(selected,1),taxi.transform.position);
            Check(!assist.RequestLaneChange(1)&&!assist.Changing,"occupied adjacent lane rejected without queue");Destroy(occupied);yield return null;
            Check(assist.RequestLaneChange(1),"right change accepted");Check(!assist.RequestLaneChange(1),"no repeat while changing");
            Check(!assist.RequestTurn(-1),"turn cannot interrupt lane change");
            yield return new WaitForSeconds(6);Check(assist.Active&&assist.CurrentLane.index==1&&!assist.Changing,"right change physically completes");
            Check(assist.RequestLaneChange(-1),"left change accepted");yield return new WaitForSeconds(6);
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
            taxi.autoThrottle=.28f;
            for(float elapsed=0;elapsed<18;elapsed+=.1f){yield return new WaitForSeconds(.1f);if(assist.CurrentLane==destination&&!assist.Turning)break;}
            Check(assist.Active&&assist.CurrentLane==destination&&!assist.Turning,"physical turn completes "+turn+" status="+assist.Status);
        }
        taxi.autoThrottle=0;taxi.autoBrake=true;
        string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts"));Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder,"assisted-drive-test.txt"),"success="+ok+"\n"+report);Debug.Log(report.ToString());Application.Quit(ok?0:1);
    }
    GameObject networkObstacle(AssistedRoadNetwork.Lane lane,Vector3 position){float error;float at=lane.path.Project(position,out error);var obj=new GameObject("Occupied lane fixture");obj.layer=9;obj.transform.position=lane.path.Position(at)+Vector3.up*.8f;obj.transform.rotation=Quaternion.LookRotation(lane.path.Tangent(at));obj.AddComponent<BoxCollider>().size=new Vector3(1.8f,1.3f,4);Physics.SyncTransforms();return obj;}
}
