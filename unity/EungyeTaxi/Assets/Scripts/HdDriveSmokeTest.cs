using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

public class HdDriveSmokeTest : MonoBehaviour {
 public DriveSession session;
 bool ok=true;readonly StringBuilder report=new StringBuilder();
 void Check(bool value,string message){ok&=value;report.AppendLine((value?"PASS ":"FAIL ")+message);Debug.Log((value?"PASS ":"FAIL ")+message);}
 IEnumerator Start(){
  yield return new WaitForSeconds(1);
  var trial=GetComponent<HdRouteTrial>();var taxi=session.taxi;var assist=session.assisted;var nav=session.jobs.nav;
  taxi.automated=true;assist.allowAutomated=true;Time.timeScale=3;
  Check(nav.hd!=null&&assist.hd.nav==nav,"shared HD navigation and steering");
  Check(!session.business.enabledCampaign,"campaign disabled");
  Check(session.traffic.cars.Count==0&&session.traffic.junctions.Count==0,"no fabricated traffic or signal connections");
  for(int i=0;i<session.world.map.hdTrials.Length;i++){
   trial.Select(i);taxi.autoThrottle=0;taxi.autoBrake=true;yield return new WaitForSeconds(.6f);
   Check(nav.hdRoute!=null,"route "+i);if(nav.hdRoute==null)continue;
   var original=nav.hdRoute.path;Check(assist.SetActive(true),"arm "+i);
   if(i==1||i==3)Check(assist.RequestTurn(1),"right turn request accepted "+i);
   if(i==1){var accepted=nav.hdRoute;Check(!assist.RequestTurn(-1)&&nav.hdRoute==accepted,"unsupported opposite turn rejected without changing route");}
   if(i==4){Check(!assist.RequestLaneChange(1),"stationary manual change rejected");int side=nav.hd.edges[session.world.map.hdTrials[i].expected[1]].change;Check(nav.RequestHdMovement(side,true),"verified lane change request accepted");}
   if(i==0)ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts/hd-start.png")));
   float maxError=0,maxTilt=0;bool sawTurn=false,sawChange=false,sawIndicator=false,arrived=false;int contacts=0;
   for(float elapsed=0;elapsed<100;elapsed+=.1f){
    float error;float at=original.Project(taxi.transform.position,out error);maxError=Mathf.Max(maxError,error);maxTilt=Mathf.Max(maxTilt,Vector3.Angle(taxi.transform.up,Vector3.up));
    if(i==1&&assist.Turning&&!sawTurn)ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts/hd-right-turn.png")));
    sawTurn|=assist.Turning;sawChange|=assist.Changing;sawIndicator|=session.signals.direction!=0;
    // Test harness is the driver: choose test speed and brake near the goal.
    // H must never modify that driver's longitudinal demand.
    float remain=original.length-at;float speed=remain<8?1.5f:4.5f;
    taxi.autoThrottle=taxi.Speed<speed?1:0;taxi.autoBrake=taxi.Speed>speed+1;
    if(remain<2.3f){taxi.autoThrottle=0;taxi.autoBrake=true;if(taxi.body.velocity.magnitude<.56f){arrived=true;break;}}
    if(!assist.Active&&remain>2.5f)break;
    float steer=0;var demand=new DriveMath.Demand{motor=.65f,brake=.27f};assist.Apply(ref steer,ref demand);
    if(demand.motor!=.65f||demand.brake!=.27f){Check(false,"longitudinal demand mutated "+i);break;}
    if(Physics.OverlapBox(taxi.transform.position+Vector3.up*.42f,new Vector3(.75f,.25f,1.7f),taxi.transform.rotation,1,QueryTriggerInteraction.Ignore).Length>0)contacts++;
    yield return new WaitForSeconds(.1f);
   }
   Check(arrived,"physical arrival "+i+" status="+assist.Status);
   Check(maxError<1.5f&&maxTilt<12,"stable "+i+" maxError="+maxError+" maxTilt="+maxTilt);
   Check(contacts==0,"no static body overlap "+i+" contacts="+contacts);
   Check(i<4?sawTurn:sawChange,"maneuver exercised "+i);
   Check(sawIndicator,"indicators and side camera trigger "+i);
   Check(Vector3.Distance(taxi.transform.position,nav.destination+Vector3.up*.65f)<4,"near real goal "+i);
  }
  // Miss the right-turn lane in the actual data: choose its adjacent straight
  // lane near the junction. Replan from that lane, or explicitly report no route.
  trial.Select(1);var old=nav.hdRoute;var curve=nav.hd.paths[session.world.map.hdTrials[1].goalEdge];var junction=curve.Position(0);int wrong=-1;float wrongAt=0,best=8;
  for(int i=0;i<nav.hd.edges.Length;i++){
   if(nav.hd.edges[i].change!=0||nav.hd.edges[i].turn!=0||old.edges.Contains(i))continue;
   float error;float at=nav.hd.paths[i].Project(junction,out error);
   if(error>2.8f&&error<best&&Vector3.Dot(nav.hd.paths[i].Tangent(at),curve.Tangent(0))>.8f){wrong=i;wrongAt=at;best=error;}
  }
  Check(wrong>=0,"actual adjacent missed-turn lane exists");
  if(wrong>=0){taxi.body.position=nav.hd.paths[wrong].Position(wrongAt)+Vector3.up*.65f;taxi.body.rotation=Quaternion.LookRotation(nav.hd.paths[wrong].Tangent(wrongAt));taxi.transform.SetPositionAndRotation(taxi.body.position,taxi.body.rotation);Physics.SyncTransforms();int before=nav.recalculationCount;nav.Refresh();
   Check(nav.recalculationCount>before&&nav.hdRoute!=old,"missed-turn replans instead of crossing back to old route");
   if(nav.hdRoute!=null){float at;int initial=nav.hd.Nearest(taxi.transform.position,taxi.transform.forward,out at);Check(nav.hdRoute.edges[0]==initial,"replanned route starts on actual current lane");}
  }
  // Wrong direction/off-map must not fabricate a nearby route.
  trial.Select(0);taxi.body.position=new Vector3(-50,.65f,-50);taxi.transform.position=taxi.body.position;Physics.SyncTransforms();nav.hdRoute=null;nav.Refresh();
  Check(nav.hdRoute==null&&nav.path.Count==0,"off-map gives explicit no route");
  Check(!assist.SetActive(true),"cannot arm without route");
  trial.Select(4);yield return new WaitForSeconds(.4f);assist.SetActive(true);taxi.autoBrake=false;taxi.autoThrottle=-1;yield return new WaitForSeconds(1);
  Check(!assist.Active,"reverse returns manual");
  Time.timeScale=1;taxi.autoThrottle=0;taxi.autoBrake=true;
  string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts"));Directory.CreateDirectory(folder);
  File.WriteAllText(Path.Combine(folder,"hd-drive-test.txt"),"success="+ok+"\n"+report);
  Debug.Log("HD_DRIVE_TEST_DONE success="+ok);Application.Quit(ok?0:1);
 }
}
