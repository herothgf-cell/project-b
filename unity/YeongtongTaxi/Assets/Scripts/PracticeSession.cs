using System;
using System.Collections;
using UnityEngine;

// Separate player process isolates every timer, vehicle, account and history from the paused campaign.
public class PracticeSession : MonoBehaviour {
 public DriveSession session;
 System.Diagnostics.Process child;string resultPath="",inputPath="",eventId="";public string notice="";
 public bool Busy {get{return child!=null;}}
 public bool Begin(LawEvent record,bool preparation=false){
  if(Enabled||Busy||!(session.business.ShowingSummary||preparation&&session.business.preparing)||record==null||Application.isEditor)return false;
  int variant=0;if(record.practice==null&&!(record.scenario!=null&&record.scenario.StartsWith("R")&&int.TryParse(record.scenario.Substring(1),out variant)&&variant>=1&&variant<=6))return false;
  try{
   string token=Guid.NewGuid().ToString("N");resultPath=System.IO.Path.Combine(Application.temporaryCachePath,"taxi-practice-result-"+token+".json");eventId=record.id;
   string arguments="-eungye -practice -practiceResult \""+resultPath+"\" -practiceSource \""+record.id+"\" -screen-width 1600 -screen-height 900 -screen-fullscreen 0";
   if(record.practice!=null){inputPath=System.IO.Path.Combine(Application.temporaryCachePath,"taxi-practice-"+token+".json");System.IO.File.WriteAllText(inputPath,JsonUtility.ToJson(record.practice));arguments+=" -practiceInput \""+inputPath+"\"";}else arguments+=" -practiceRight"+variant;
   if(Array.IndexOf(Environment.GetCommandLineArgs(),"-completionTest")>=0)arguments+=" -practiceAutoFinish";
   child=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo{FileName=System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName,Arguments=arguments,UseShellExecute=false});notice="연습 창 진행 중 · 원래 정산은 정지 상태입니다";return true;
  }catch(Exception e){notice="연습 실행 실패: "+e.Message;child=null;return false;}
 }
 void Update(){if(child==null||!child.HasExited)return;
  try{if(System.IO.File.Exists(resultPath)){var result=JsonUtility.FromJson<PracticeResult>(System.IO.File.ReadAllText(resultPath));if(result!=null&&result.sourceId==eventId){session.business.practices.Add(result);if(session.business.ShowingSummary)session.business.SaveCheckpoint();notice="연습 결과: "+result.action+" · "+result.EvaluationText();}else notice="연습 결과를 확인하지 못했습니다";}else notice="연습 창 종료 · 결과 파일 없음";}
  catch(Exception e){notice="연습 결과 읽기 실패: "+e.Message;}
  finally{child.Dispose();child=null;foreach(var path in new[]{inputPath,resultPath})if(!string.IsNullOrEmpty(path)&&System.IO.File.Exists(path))System.IO.File.Delete(path);inputPath=resultPath="";}
 }
 public void FinishPractice(){WriteResult();Application.Quit();}
 void OnApplicationQuit(){if(Enabled)WriteResult();}
 void WriteResult(){var args=Environment.GetCommandLineArgs();int output=Array.IndexOf(args,"-practiceResult"),source=Array.IndexOf(args,"-practiceSource");if(output<0||output+1>=args.Length||source<0||source+1>=args.Length||session==null||session.law==null)return;
  var events=session.law.ledger.Events;var result=new PracticeResult{sourceId=args[source+1],events=events.Length,validFindings=session.law.ledger.ValidForDay(session.law.Day),score=session.law.ledger.Score(session.law.Day),action=events.Length==0?"종료 · 평가 장면 미완료":events[events.Length-1].action};
  try{System.IO.File.WriteAllText(args[output+1],JsonUtility.ToJson(result));}catch(Exception e){Debug.LogWarning(e.Message);}
 }
 IEnumerator AutoFinish(){yield return new WaitForSecondsRealtime(3);FinishPractice();}

 public static bool Enabled {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-practice")>=0;}}
 IEnumerator Start(){
  if(!Enabled)yield break;
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-practiceAutoFinish")>=0)StartCoroutine(AutoFinish());
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
  if(GUI.Button(new Rect(Screen.width/2-220,130,440,40),"연습 전용 · 영업 기록 보존 / 창을 닫고 원래 결과로 복귀",style))FinishPractice();
 }
}
