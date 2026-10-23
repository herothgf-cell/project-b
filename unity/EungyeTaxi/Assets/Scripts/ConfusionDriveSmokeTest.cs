using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

// Wheel physics + production H steering; only initial spawn is positioned.
public class ConfusionDriveSmokeTest : MonoBehaviour {
 public DriveSession session;bool ok=true;StringBuilder report=new StringBuilder();
 void Check(bool b,string n){ok&=b;report.AppendLine((b?"PASS ":"FAIL ")+n);File.WriteAllText("artifacts/confusion-drive-test.txt","success="+ok+"\n"+report);}
 IEnumerator Start(){
  yield return new WaitForSeconds(1.5f);var picker=GetComponent<PracticeSession>();var f=GetComponent<ConfusionPractice>();var taxi=session.taxi;taxi.automated=true;taxi.autoBrake=true;session.assisted.allowAutomated=true;
  foreach(int v in new[]{8,10,12,13,14}){
   Check(picker.OpenPicker(),"open physical practice "+v);picker.SelectedVariant=v;Check(picker.StartSelected(),"start physical practice "+v);Time.timeScale=3;yield return new WaitForSeconds(.5f);
   Check(session.assisted.SetActive(true),"H steering armed "+v);float until=Time.time+180,maxError=0;bool stopped=false;float held=0;
   while(Time.time<until&&!f.State.Finished){
    float error;float at=f.Route.path.Project(taxi.transform.position,out error);maxError=Mathf.Max(maxError,error);
    var front=taxi.transform.position+taxi.transform.forward*2;float gap=f.StopAlong-Vector3.Dot(front-f.Junction.center,f.Direction);
    if(gap>=-.2f&&gap<3&&taxi.body.velocity.magnitude<.05f){stopped=true;held+=Time.fixedDeltaTime;}
    bool wait=v==8?(!stopped||held<.4f):f.State.Blocked||f.State.Oncoming||f.State.Signal==SignalColor.Red;
    float bend=Vector3.Angle(f.Route.path.Tangent(at),f.Route.path.Tangent(Mathf.Min(f.Route.path.length,at+12)));float target=bend>10?2.5f:4;
    if(wait&&!f.State.Entered)target=Mathf.Min(target,Mathf.Sqrt(3*Mathf.Max(0,gap-1)));
    taxi.autoThrottle=taxi.Speed<target-.1f?.22f:0;taxi.autoBrake=taxi.Speed>target+.1f||target<.15f;
    if(!session.assisted.Active)break;yield return new WaitForFixedUpdate();
   }
   taxi.autoThrottle=0;taxi.autoBrake=true;
   Check(f.State.Finished&&f.State.CorrectRoute&&f.AttemptEvent!=null&&f.AttemptEvent.Deduction==0,"physical completion without law deduction "+v+" maxError="+maxError.ToString("F2"));
   Check(maxError<2.5f,"physical route tracking "+v);
   if(GetComponent<PracticeCoach>().ResultOpen)GetComponent<PracticeCoach>().CloseResult();
  }
  File.AppendAllText("artifacts/confusion-drive-test.txt","Physical throttle/brake and H steering; not manual human validation.\n");Application.Quit(ok?0:1);
 }
}
