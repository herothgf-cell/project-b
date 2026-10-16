using System.Collections;
using System.Text;
using System.IO;
using UnityEngine;
public class ScenarioSmokeTest : MonoBehaviour {
 public DriveSession session;bool ok=true;StringBuilder report=new StringBuilder();
 void Check(bool pass,string text){ok&=pass;report.AppendLine((pass?"PASS ":"FAIL ")+text);}
 IEnumerator Start(){
  yield return new WaitForSeconds(1);session.taxi.automated=true;session.taxi.body.isKinematic=true;session.taxi.transform.position=new Vector3(-200,1,-200);Time.timeScale=5;
  var queue=GetComponent<ExitQueueSituation>();Check(queue.Begin(),"reproducible right-turn signal queue available");
  if(queue.Active){yield return new WaitForSeconds(5);int stopped=0;foreach(var car in session.traffic.cars)if(car.speed<.2f&&(car.waitingForSignal||car.waitingForObstacle))stopped++;Check(stopped>=5,"queue forms through AI following: "+stopped);float distance=0;for(int i=0;i<8;i++)distance+=session.traffic.cars[i].totalTravel;yield return new WaitForSeconds(33);float after=0;for(int i=0;i<8;i++)after+=session.traffic.cars[i].totalTravel;Check(after>distance+30,"queue discharges after arrow green");}
  var right=GetComponent<RightTurnSituation>();Check(!right.Select(1),"active queue owns junction operation");var template=queue.Template.Copy();queue.Stop();Check(queue.Replay(template),"recorded exit path recreates scenario");Check(queue.Template.points.Length==template.points.Length,"exit practice preserves path");queue.Stop();session.taxi.transform.position=new Vector3(-200,1,-200);
  for(int i=1;i<=6;i++){Check(right.Select(i),"select R case "+i);yield return new WaitForFixedUpdate();yield return null;var j=right.junction;var o=session.traffic.Observe(j,0,1,session.traffic.AxisDirection(j,0),session.taxi.gameObject);o.trafficClear=true;
   Check(o.known&&o.dedicated==(i==3||i==4),"shared observed signal case "+i);
   Check(o.MayTurnRight(false)==(i==2||i==4),"unstopped permission case "+i);
   if(i>=5)Check(!o.PedestrianClear,"pedestrian intention/crossing active "+i);
  }
  Time.timeScale=1;File.WriteAllText(Path.GetFullPath("artifacts/scenario-v2-test.txt"),"success="+ok+"\n"+report);Application.Quit(ok?0:1);
 }
}
