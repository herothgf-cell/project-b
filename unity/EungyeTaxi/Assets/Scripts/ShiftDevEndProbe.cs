using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

// Player check for the single shift-length selector and the developer end-day button.
// Uses a private temp campaign, never the player's save.
public sealed class ShiftDevEndProbe : MonoBehaviour {
 public static bool Enabled {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-shiftDevEndCheck")>=0;}}
 public static readonly string CampaignPath=Path.Combine(Path.GetTempPath(),"taxi-shift-"+Guid.NewGuid().ToString("N"),"campaign.json");
 static bool attached;DriveSession s;int checks;bool done;float start;string output;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Attach(){if(Enabled&&!attached){attached=true;new GameObject("Shift/dev end acceptance").AddComponent<ShiftDevEndProbe>();}}
 void Awake(){DontDestroyOnLoad(gameObject);start=Time.realtimeSinceStartup;var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-shiftCaptureOutput");output=i>=0&&i+1<args.Length?Path.GetFullPath(args[i+1]):Path.GetFullPath("artifacts/shift-dev-end-captures");Directory.CreateDirectory(output);}
 void Update(){if(!done&&Time.realtimeSinceStartup-start>240){done=true;Debug.LogError("SHIFT_PLAYER_TIMEOUT");Application.Quit(2);}}
 void Check(bool value,string message){checks++;Debug.Log("SHIFT_PLAYER_"+(value?"PASS ":"FAIL ")+message);if(!value)throw new Exception(message);}
 IEnumerator Start(){var run=Run();while(!done){object next;try{if(!run.MoveNext())yield break;next=run.Current;}catch(Exception e){done=true;Debug.LogException(e);Application.Quit(1);yield break;}yield return next;}}
 IEnumerator Ready(){while(!(s=FindObjectsOfType<DriveSession>().FirstOrDefault(x=>!x.IsSimulationBot))||!s.StartupReady||s.business.IsRestoring)yield return null;yield return null;yield return null;}
 IEnumerator Capture(string name){yield return null;yield return null;yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());Destroy(texture);Debug.Log("SHIFT_PLAYER_CAPTURE "+name);}
 IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
 IEnumerator Run(){
  yield return null;yield return null;
  FindObjectOfType<UnifiedModeBootstrap>().StartSingle();yield return Ready();
  var b=s.business;
  Check(b.SavePath==CampaignPath&&b.preparing&&b.ledger.Day==1,"single uses isolated campaign and day-1 preparation");
  Check(b.CanChangeShiftLength&&b.ShiftMinutes==20,"day-1 preparation offers shift length, default 20 minutes");
  Check(!b.DevEndDayNow(),"dev end refused while preparing");
  yield return Capture("01-day1-prep-20min");
  Check(!b.SetShiftLength(15),"non-choice rejected");
  Check(b.SetShiftLength(10)&&b.ledger.Settings.daySeconds==600&&b.settings.daySeconds==600,"10 minutes applied");
  yield return Capture("02-day1-prep-10min");
  b.ContinueFromMenu();yield return Frames(10);
  Check(!b.preparing&&!b.CanChangeShiftLength&&b.ledger.SecondsLeft<=600,"locked once operating; remaining time within 10 minutes");
  yield return Capture("03-operating-10min");
  s.paused=true;Time.timeScale=0;var panel=DrivingSettingsPanel.For(s);panel.Open();panel.DevTab=true;
  yield return Capture("04-settings-dev-tab");
  Check(b.CanDevChangeShiftLength&&b.DevSetShiftLength(20)&&b.ShiftMinutes==20&&b.ledger.SecondsLeft>600,"dev tab changes shift length mid-shift");
  yield return Capture("04b-settings-dev-tab-20min");
  Check(b.DevEndDayNow(),"dev end accepted while operating");panel.Cancel();
  float wait=Time.realtimeSinceStartup;while(!b.ShowingSummary&&Time.realtimeSinceStartup-wait<20)yield return null;
  Check(b.ShowingSummary&&b.ledger.Last!=null&&b.ledger.Last.endReason=="개발용 바로 퇴근"&&b.ledger.Last.day==1,"day 1 settles with dev end reason");
  yield return Capture("05-summary-after-dev-end");
  Check(b.BeginNextCampaignDay(),"day 2 preparation opens");yield return Frames(10);
  Check(b.preparing&&b.ledger.Day==2&&b.ShiftMinutes==20&&b.CanChangeShiftLength,"day 2 keeps the dev-changed 20 minutes and remains changeable");
  yield return Capture("06-day2-prep-20min");
  Check(b.SetShiftLength(30)&&b.ShiftMinutes==30,"day 2 changed to 30 minutes");
  yield return Capture("07-day2-prep-30min");
  var saved=BusinessProgress.ParseCheckpoint(File.ReadAllText(b.SavePath));
  Check(EconomyLedger.RestoreCampaign(saved.days,saved.account).Settings.daySeconds==1800,"saved campaign restores 30 minutes");
  done=true;Debug.Log("SHIFT_PLAYER_OK checks="+checks);Application.Quit(0);
 }
}
