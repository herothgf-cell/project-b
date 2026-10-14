using System.Collections;
using System.Text;
using UnityEngine;

public class RoadRulesSmokeTest : MonoBehaviour {
    public DriveSession session;bool ok=true;readonly StringBuilder report=new StringBuilder();
    void Check(bool value,string name){ok&=value;report.AppendLine((value?"PASS ":"FAIL ")+name);}
    IEnumerator Start(){
        yield return new WaitForSeconds(1);var taxi=session.taxi;var assist=session.assisted;var traffic=session.traffic;var j=traffic.junctions[0];
        foreach(var car in traffic.cars){car.Recycle();car.enabled=false;}
        taxi.automated=true;assist.allowAutomated=true;traffic.manualClock=true;Time.timeScale=2;
        Check(!session.business.enabledCampaign,"trial does not load or save campaign");
        Check(Mathf.Abs(j.halfWidth[0]-(RoadRulesTrial.selection==1?7:3.5f))<.01f,"signal geometry follows carriageway width");
        Check(j.leftLamps[0].Length==(j.leftTurnMode==LeftTurnMode.Permissive?0:3),"signal head matches explicit left-turn mode");
        var originalMode=j.leftTurnMode;j.leftTurnMode=LeftTurnMode.ProtectedShared;traffic.manualTime=1;
        foreach(var car in traffic.cars)if(car.route.turn<0&&car.route.stops[0].junction==j){car.Place(0);car.speed=9;Check(traffic.YieldClear(j,-traffic.mainDirection,1,taxi.gameObject)==(j.laneCount[0]>1),"right yields to protected left when receiving paths overlap");car.Recycle();break;}
        j.leftTurnMode=originalMode;
        if(j.leftTurnMode==LeftTurnMode.Permissive){
            foreach(var car in traffic.cars)if(car.route.turn<0&&car.route.stops[0].junction==j){
                traffic.manualTime=1;car.Place(car.route.stops[0].distance);car.speed=0;
                Check(traffic.YieldClear(j,-traffic.mainDirection,1,taxi.gameObject),"green right has priority over waiting opposing permissive left");
                car.Place(car.route.stops[0].distance+5);car.speed=3;
                Check(!traffic.YieldClear(j,-traffic.mainDirection,1,taxi.gameObject),"right still yields to permissive left already inside junction");
                car.Recycle();break;
            }
        }
        if(j.leftTurnMode==LeftTurnMode.Permissive){
            traffic.manualTime=1;
            foreach(var car in traffic.cars)if(car.route.axis==0&&car.route.turn==0&&Vector3.Dot(car.route.direction,Vector3.forward)<-.9f){
                car.Place(Vector3.Dot(j.center+Vector3.forward*30-car.route.start,car.route.direction));car.speed=9;
                Check(!traffic.YieldClear(j,Vector3.forward,-1,taxi.gameObject),"permissive left yields to approaching opposing vehicle");
                Check(traffic.YieldClear(j,Vector3.forward,1,taxi.gameObject),"right turn does not conflict with opposite straight");
                var line=j.center-Vector3.forward*j.StopDistance(0)+Vector3.right*1.75f;
                session.manners.BeginRide();taxi.transform.rotation=Quaternion.identity;taxi.body.velocity=Vector3.forward*2;
                session.manners.CheckSignal(line-Vector3.forward,line+Vector3.forward);
                taxi.transform.rotation=Quaternion.Euler(0,30,0);session.manners.CheckSignal(line+Vector3.forward,line+Vector3.forward*2);
                Check(session.manners.ledger.Score==100,"legal right beside opposite straight is not penalized");
                session.manners.BeginRide();taxi.transform.rotation=Quaternion.identity;taxi.body.velocity=Vector3.forward*2;
                session.manners.CheckSignal(line-Vector3.forward,line+Vector3.forward);
                session.manners.CheckSignal(line+Vector3.forward,line+Vector3.forward*2);
                Check(session.manners.ledger.Score==100,"waiting straight inside permissive junction does not incur yield penalty");
                taxi.transform.rotation=Quaternion.Euler(0,-30,0);
                session.manners.CheckSignal(line+Vector3.forward*2,line+Vector3.forward*3);
                Check(session.manners.ledger.Score<100,"player cutting across opposing traffic receives yield penalty");
                car.Recycle();Check(traffic.YieldClear(j,Vector3.forward,-1,taxi.gameObject),"left becomes available when opponent clears");break;
            }
        }
        foreach(bool stopped in new[]{false,true}){
            traffic.manualTime=18;session.manners.BeginRide();taxi.body.velocity=Vector3.zero;taxi.transform.rotation=Quaternion.identity;
            var line=j.center-Vector3.forward*j.StopDistance(0)+Vector3.right*1.75f;
            if(stopped)session.manners.CheckSignal(line-Vector3.forward*.5f,line-Vector3.forward*.5f);
            session.manners.CheckSignal(line-Vector3.forward,line+Vector3.forward);
            var p=line+Vector3.forward;for(int i=0;i<6;i++){var q=p+Vector3.right*2;session.manners.CheckSignal(p,q);p=q;}
            Check((session.manners.ledger.Score==100)==stopped,"player red right requires actual stop stopped="+stopped);
        }
        traffic.manualTime=13;session.manners.BeginRide();var yellowLine=j.center-Vector3.forward*j.StopDistance(0)+Vector3.right*1.75f;
        var yellowPoint=yellowLine-Vector3.forward;for(int i=0;i<18;i++){var q=yellowPoint+Vector3.forward*2;session.manners.CheckSignal(yellowPoint,q);yellowPoint=q;}
        Check(session.manners.ledger.Score<100,"new straight entry on yellow is penalized");
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-rulesOnly")>=0){Finish();yield break;}
        int turns=0;
        foreach(var lane in assist.network.lanes){
            if(Vector3.Distance(lane.path.Position(lane.path.length),j.center)>20||lane.path.length<45)continue;
            foreach(int turn in new[]{-1,1}){
                var join=assist.network.Connection(lane,turn);if(join==null)continue;
                assist.SetActive(false);taxi.autoThrottle=0;taxi.autoBrake=false;taxi.body.velocity=Vector3.zero;taxi.body.angularVelocity=Vector3.zero;
                var heading=lane.path.Tangent(lane.path.length-23);var position=lane.path.Position(lane.path.length-23)+Vector3.up*.65f;
                taxi.transform.SetPositionAndRotation(position,Quaternion.LookRotation(heading));taxi.body.position=position;taxi.body.rotation=taxi.transform.rotation;Physics.SyncTransforms();
                int axis=Mathf.Abs(Vector3.Dot(heading,traffic.mainDirection))>.7f?0:1;int sign=traffic.Sign(heading,axis);
                traffic.manualTime=(j.leftTurnMode==LeftTurnMode.Permissive?axis:axis*2+(sign>0?0:1))*17+1;
                session.manners.BeginRide();int roadEvents=session.manners.boundaryEvents,redEvents=session.manners.redEvents;yield return new WaitForSeconds(.6f);
                bool enabled=assist.SetActive(true);bool requested=assist.RequestTurn(turn);taxi.autoThrottle=.25f;float until=Time.time+22;
                while(Time.time<until&&assist.Active){yield return new WaitForFixedUpdate();if(assist.CurrentLane==join.target&&!assist.Turning)break;}
                float error;join.target.path.Project(taxi.transform.position,out error);
                Check(enabled&&requested&&assist.Active&&assist.CurrentLane==join.target&&!assist.Turning&&error<1.3f&&session.manners.boundaryEvents==roadEvents&&session.manners.redEvents==redEvents,
                    "physical turn axis="+axis+" sign="+sign+" turn="+turn+" error="+error+" roadEvents="+(session.manners.boundaryEvents-roadEvents)+" redEvents="+(session.manners.redEvents-redEvents)+" status="+assist.Status);
                taxi.autoThrottle=0;taxi.autoBrake=true;turns++;
            }
        }
        Check(turns==8,"all four approaches tested left and right");
        Finish();
    }
    void Finish(){
        Time.timeScale=1;string path=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../..","artifacts","road-rules-"+RoadRulesTrial.selection+".txt"));
        System.IO.File.WriteAllText(path,"success="+ok+"\n"+report);Debug.Log(report.ToString());Application.Quit(ok?0:1);
    }
}
