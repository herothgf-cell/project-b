using System;
using System.Collections;
using UnityEngine;

// Separate player process isolates every timer, vehicle, account and history from the paused campaign.
public class PracticeSession : MonoBehaviour {
 public DriveSession session;
 public static bool Enabled {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-practice")>=0;}}
 public static bool Launch(LawEvent record){
  if(record.practice==null){int variant;return record.scenario!=null&&record.scenario.StartsWith("R")&&int.TryParse(record.scenario.Substring(1),out variant)&&Launch(variant);}
  if(Application.isEditor)return false;
  try{string path=System.IO.Path.Combine(Application.temporaryCachePath,"taxi-practice-"+Guid.NewGuid().ToString("N")+".json");System.IO.File.WriteAllText(path,JsonUtility.ToJson(record.practice));System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo{FileName=System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName,Arguments="-eungye -practice -practiceInput \""+path+"\" -screen-width 1600 -screen-height 900 -screen-fullscreen 0",UseShellExecute=false});return true;}catch(Exception e){Debug.LogWarning("Practice launch failed: "+e.Message);return false;}
 }
 public static bool Launch(int variant){
  if(Application.isEditor||variant<1||variant>6)return false;
  try{System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo{FileName=System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName,Arguments="-eungye -practice -practiceRight"+variant+" -screen-width 1600 -screen-height 900 -screen-fullscreen 0",UseShellExecute=false});return true;}catch(Exception e){Debug.LogWarning("Practice launch failed: "+e.Message);return false;}
 }
 IEnumerator Start(){
  if(!Enabled){enabled=false;yield break;}
  yield return new WaitForSeconds(.8f);session.jobs.enabled=false;session.services.enabled=false;session.roadConditions.enabled=false;
  foreach(var p in session.jobs.people)p.SetActive(false);
  foreach(var c in session.traffic.cars){c.Recycle();c.enabled=false;}
  var args=Environment.GetCommandLineArgs();int input=Array.IndexOf(args,"-practiceInput");
  if(input>=0&&input+1<args.Length){PracticeRoute source=null;try{source=JsonUtility.FromJson<PracticeRoute>(System.IO.File.ReadAllText(args[input+1]));}catch(Exception e){Debug.LogWarning("Practice input unavailable: "+e.Message);}if(source!=null)GetComponent<ExitQueueSituation>().Replay(source);yield break;}
  int variant=1;foreach(var arg in Environment.GetCommandLineArgs())if(arg.StartsWith("-practiceRight"))int.TryParse(arg.Substring(14),out variant);
  variant=Mathf.Clamp(variant,1,6);var situation=GetComponent<RightTurnSituation>();var j=situation.junction;if(j==null)yield break;
  var dir=session.traffic.AxisDirection(j,0);var right=TrafficSystem.Right(dir);var taxi=session.taxi;
  taxi.body.velocity=taxi.body.angularVelocity=Vector3.zero;taxi.body.position=j.center-dir*110+right*(j.halfWidth[0]-1.75f)+Vector3.up*.65f;taxi.body.rotation=Quaternion.LookRotation(dir);taxi.transform.SetPositionAndRotation(taxi.body.position,taxi.body.rotation);Physics.SyncTransforms();
  situation.Select(variant);session.law.ResetObservation();
  var goal=j.center+right*70-dir*1.75f;float at;int edge=session.jobs.nav.hd.Nearest(goal,right,out at);session.jobs.nav.SetHdDestination(edge,at);
 }
 void OnGUI(){if(!Enabled)return;var style=new GUIStyle(GUI.skin.button){font=KoreanText.Font,fontSize=16};GUI.depth=-150;
  if(GUI.Button(new Rect(Screen.width/2-220,130,440,40),"연습 전용 · 영업 기록 보존 / 창을 닫고 원래 결과로 복귀",style))Application.Quit();
 }
}
