using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
public class FatigueOvertimeProbe:MonoBehaviour {
 bool ok=true;StringBuilder report=new StringBuilder();
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-fatigueOvertimeTest")>=0)new GameObject("Fatigue overtime probe").AddComponent<FatigueOvertimeProbe>();}
 void Check(bool v,string label){ok&=v;report.AppendLine((v?"PASS ":"FAIL ")+label);Directory.CreateDirectory("artifacts");File.WriteAllText("artifacts/fatigue-overtime-runtime.txt","success="+ok+"\n"+report);}
 IEnumerator Capture(string name){
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-fatigueCapture")<0)yield break;
  // Hidden player windows need the same explicit drawable surface as score HUD captures.
  Screen.SetResolution(1280,720,FullScreenMode.Windowed);
  yield return new WaitForSecondsRealtime(.6f);
  yield return new WaitForEndOfFrame();
  string path=Path.GetFullPath("artifacts/fatigue-"+name+".png");
  ScreenCapture.CaptureScreenshot(path);
  yield return new WaitForSecondsRealtime(.5f);
  Check(File.Exists(path),"rendered "+name+" capture saved");
  if(File.Exists(path)){
   var rendered=new Texture2D(2,2);rendered.LoadImage(File.ReadAllBytes(path));
   float darkest=1,lightest=0;
   for(int y=0;y<rendered.height;y+=Mathf.Max(1,rendered.height/24))for(int x=0;x<rendered.width;x+=Mathf.Max(1,rendered.width/32)){float value=rendered.GetPixel(x,y).grayscale;darkest=Mathf.Min(darkest,value);lightest=Mathf.Max(lightest,value);}
   Check(lightest-darkest>.05f,"rendered "+name+" has visible scene/UI contrast");Destroy(rendered);
  }
 }
 IEnumerator Start(){yield return new WaitForSecondsRealtime(2);var s=FindObjectOfType<DriveSession>();var b=s.business;b.enabled=false;b.enabledCampaign=true;b.useV2=true;b.preparing=false;b.ledger=new EconomyLedger(PrototypeSettings.ForOneDay());s.jobs.ClearForDay();s.paused=false;Time.timeScale=1;
  s.taxi.enabled=false;s.taxi.body.detectCollisions=false;s.taxi.body.useGravity=false;s.taxi.automated=true;s.taxi.autoThrottle=1;s.taxi.autoSteer=.8f;s.taxi.autoBrake=true;
  b.ledger.Advance(1200,0);Check(b.CanAccept&&b.ledger.Driver.fatigue==100&&!b.ShowingSummary,"budget enters overtime and calls remain enabled");
  var d=b.ledger.Driver.drowsiness;d.randomState=13;d.Advance(22,true,100);s.taxi.body.velocity=Vector3.forward*4;s.taxi.SendMessage("FixedUpdate");bool suppressed=true;foreach(var w in s.taxi.wheels)suppressed&=w.motorTorque==0&&w.brakeTorque==0&&w.steerAngle==0;Check(b.DrowsyControlsLost&&suppressed,"actual taxi wheel demands suppressed during drowsy control loss");yield return Capture("controls");
  s.paused=true;Check(!b.DrowsyControlsLost&&!b.DrowsinessRunning,"menu suppresses effect");s.paused=false;s.taxi.body.velocity=Vector3.zero;d.Advance(1,b.DrowsinessRunning,100,false);Check(!b.DrowsyControlsLost,"coast to stop expires active control loss");
  d.randomState=3;d.Recover();d.Advance(20,true,100);var json=JsonUtility.ToJson(b.ledger.Snapshot());var saved=JsonUtility.FromJson<EconomySnapshot>(json);var copy=EconomyLedger.RestoreCampaign(saved,b.ledger.Account.Snapshot());copy.Driver.drowsiness.Advance(2,true,100);Check(copy.Driver.drowsiness.Tunnel,"actual JsonUtility roundtrip retains warning and effect");
  yield return Capture("warning");d.Advance(2,true,100);s.taxi.body.velocity=s.taxi.transform.forward*4;yield return Capture("tunnel");
  var rest=s.services.RestArea;Check(rest!=null,"rest hub exists");if(rest!=null){s.taxi.body.position=rest.pump+Vector3.up*.65f;s.taxi.body.rotation=Quaternion.LookRotation(rest.direction);s.taxi.transform.SetPositionAndRotation(s.taxi.body.position,s.taxi.body.rotation);s.taxi.body.velocity=Vector3.zero;Physics.SyncTransforms();Check(s.services.AtRestArea()&&s.services.CanUse(3)&&s.services.CanUse(2),"rest hub permits free sleep and end");s.services.Toggle();Check(s.services.RequestAction(3)&&s.services.ConfirmAction(true)&&b.ledger.Driver.fatigue==60,"overtime rest menu recovers fatigue");double before=b.ledger.Elapsed;Check(b.RequestEndDay()&&b.ledger.Driver.fatigue==20&&b.ledger.Elapsed==before+100,"rest hub close includes free100second rest once");Check(!b.RequestEndDay()&&b.ledger.Elapsed==before+100,"repeat close cannot charge rest time again");Check(b.ledger.Settle()&&b.ShowingSummary,"explicit close settles overtime");}
  Application.Quit(ok?0:1);
 }
}
