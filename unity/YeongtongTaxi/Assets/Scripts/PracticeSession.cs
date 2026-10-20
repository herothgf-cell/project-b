using System;
using System.Collections;
using UnityEngine;

// Separate player process isolates every timer, vehicle, account and history from the paused campaign.
public class PracticeSession : MonoBehaviour {
 public DriveSession session;
 System.Diagnostics.Process child;string resultPath="",inputPath="",eventId="";public string notice="";
 public bool Busy {get{return child!=null;}}
 public bool PickerOpen {get;private set;}
 Vector2 pickerScroll;
 public int SelectedVariant=1;PracticeRoute directExitTemplate;
 public string SelectedTitle {get{return ConfusionPracticeState.Contains(SelectedVariant)?ConfusionPracticeState.Title(SelectedVariant):SelectedVariant==7?"출구 정체 · 대기 / 우회 / 진입 결과 비교":RightPracticeState.Title(SelectedVariant);}}
 public string SelectedConditions {get{return ConfusionPracticeState.Contains(SelectedVariant)?ConfusionPracticeState.Conditions(SelectedVariant):SelectedVariant==7?"신호를 기다리는 차량 8대가 출구 차로에 줄 서 있습니다. 영업 기록 없이 준비된 은계 구간으로 이동합니다.":RightPracticeState.Conditions(SelectedVariant);}}
 public string SelectedGuidance {get{return ConfusionPracticeState.Contains(SelectedVariant)?ConfusionPracticeState.Guidance(SelectedVariant):SelectedVariant==7?"줄 맨 뒤에서 간격을 확보해 합류하거나 직진 후 우회하세요. 앞부분 진입과 상대 진행 방해는 결과에서 구분합니다. 완료 후 같은 조건으로 재시도할 수 있습니다.":RightPracticeState.Guidance(SelectedVariant);}}
 bool pickerPaused,changedConditions,ready;float pickerTime;
 public bool OpenPicker(){
  if(Enabled&&!ready||Busy||PickerOpen||session.business.restartPrompt||session.business.pendingPayment>=0||session.business.taxPrompt||session.services.open||session.law.HistoryOpen)return false;
  if(!Enabled&&!session.business.preparing&&!session.business.ShowingSummary&&!session.paused)return false;
  pickerPaused=session.paused;pickerTime=Time.timeScale;PickerOpen=true;session.paused=true;Time.timeScale=0;return true;
 }
 public void CancelPicker(){if(!PickerOpen)return;PickerOpen=false;session.paused=pickerPaused;Time.timeScale=pickerTime;}
 public bool StartSelected(){
  if(!PickerOpen||Busy)return false;
  if(Enabled){if(!(ConfusionPracticeState.Contains(SelectedVariant)?GetComponent<ConfusionPractice>().StartAttempt(SelectedVariant):SelectedVariant==7?StartDirectExit():StartRight(SelectedVariant)))return false;changedConditions=true;notice="";PickerOpen=false;session.paused=false;Time.timeScale=1;return true;}
  bool ok=Begin(new LawEvent{id=session.law.ids.Next("practice"),day=session.business.ledger.Day,scenario=ConfusionPracticeState.Contains(SelectedVariant)?"C"+SelectedVariant:SelectedVariant==7?"exit":"R"+SelectedVariant},session.business.preparing);
  if(ok)CancelPicker();return ok;
 }
 public bool StartDirectExit(){
  if(directExitTemplate==null)directExitTemplate=GetComponent<ExitQueueSituation>().PreparedTemplate();
  if(directExitTemplate==null){notice="출구 연습 경로를 준비하지 못했습니다. 다른 상황을 선택하거나 재실행하세요.";return false;}
  bool ok=StartExit(directExitTemplate.Copy());if(ok){SelectedVariant=7;notice="";}return ok;
 }
 public bool RetryCurrent(){
  var coach=GetComponent<PracticeCoach>();if(coach.ConfusionMode){if(!Enabled||!ready||Busy)return false;SelectedVariant=GetComponent<ConfusionPractice>().State.Variant;bool ok=GetComponent<ConfusionPractice>().StartAttempt(SelectedVariant);if(ok){PickerOpen=false;session.paused=false;Time.timeScale=1;}return ok;}if(!coach.ExitMode)return RetryRight();
  if(!Enabled||!ready||Busy)return false;
  var situation=GetComponent<ExitQueueSituation>();var source=situation.Template==null?null:situation.Template.Copy();
  if(!StartExit(source))return false;PickerOpen=false;session.paused=false;Time.timeScale=1;return true;
 }
 public bool StartExit(PracticeRoute source){
  if(!Enabled||source==null)return false;
  GetComponent<ConfusionPractice>().Stop();session.manners.enabled=true;GetComponent<RightTurnSituation>().Stop();var situation=GetComponent<ExitQueueSituation>();situation.Stop();
  if(!situation.Replay(source))return false;
  GetComponent<ExitQueueObserver>().ResetAttempt();GetComponent<PracticeCoach>().BeginExit(situation);ready=true;return true;
 }
 public bool RetryRight(){if(!Enabled||!ready||Busy)return false;SelectedVariant=GetComponent<RightTurnSituation>().variant;if(!StartRight(SelectedVariant))return false;changedConditions=true;notice="";PickerOpen=false;session.paused=false;Time.timeScale=1;return true;}
 bool StartRight(int variant){
  GetComponent<ConfusionPractice>().Stop();session.manners.enabled=true;GetComponent<PracticeCoach>().CloseResult();var situation=GetComponent<RightTurnSituation>();var j=situation.junction;if(j==null)return false;
  GetComponent<ExitQueueSituation>().Stop();situation.Stop();
  var dir=session.traffic.AxisDirection(j,0);var right=TrafficSystem.Right(dir);var taxi=session.taxi;
  taxi.SetSpawn(j.center-dir*110+right*(j.halfWidth[0]-1.75f)+Vector3.up*.65f,Quaternion.LookRotation(dir));taxi.ResetTaxi();
  session.jobs.nav.Clear();if(!situation.Select(variant))return false;session.law.ResetObservation();session.manners.Suppress(1);
  var goal=j.center+right*70-dir*1.75f;float at;int edge=session.jobs.nav.hd.Nearest(goal,right,out at);session.jobs.nav.SetHdDestination(edge,at);GetComponent<PracticeCoach>().BeginAttempt(situation);return true;
 }
 public bool Begin(LawEvent record,bool preparation=false){
  if(session.business.restartPrompt||Enabled||Busy||!(session.business.ShowingSummary||preparation&&session.business.preparing||session.paused&&!session.services.open&&!session.law.HistoryOpen&&!session.business.taxPrompt&&session.business.pendingPayment<0)||record==null||Application.isEditor)return false;
  int variant=0;bool confusion=record.scenario!=null&&record.scenario.StartsWith("C")&&int.TryParse(record.scenario.Substring(1),out variant)&&ConfusionPracticeState.Contains(variant);if(!confusion&&record.practice==null&&record.scenario!="exit"&&!(record.scenario!=null&&record.scenario.StartsWith("R")&&int.TryParse(record.scenario.Substring(1),out variant)&&variant>=1&&variant<=6))return false;
  try{
   string token=Guid.NewGuid().ToString("N");resultPath=System.IO.Path.Combine(Application.temporaryCachePath,"taxi-practice-result-"+token+".json");eventId=record.id;
   string arguments="-eungye -practice -practiceResult \""+resultPath+"\" -practiceSource \""+record.id+"\" -screen-width 1600 -screen-height 900 -screen-fullscreen 0";
   if(record.practice!=null){inputPath=System.IO.Path.Combine(Application.temporaryCachePath,"taxi-practice-"+token+".json");System.IO.File.WriteAllText(inputPath,JsonUtility.ToJson(record.practice));arguments+=" -practiceInput \""+inputPath+"\"";}else if(confusion)arguments+=" -practiceConfusion"+variant;else arguments+=record.scenario=="exit"?" -practiceExit":" -practiceRight"+variant;
   if(Array.IndexOf(Environment.GetCommandLineArgs(),"-completionTest")>=0)arguments+=" -practiceAutoFinish";
   if(Array.IndexOf(Environment.GetCommandLineArgs(),"-exitSelectionTest")>=0)arguments+=" -exitSelectionTest";
   child=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo{FileName=System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName,Arguments=arguments,UseShellExecute=false});notice="연습 창 진행 중 · 원래 정산은 정지 상태입니다";return true;
  }catch(Exception e){notice="연습 실행 실패: "+e.Message;child=null;return false;}
 }
 void Update(){if(Enabled&&Input.GetKeyDown(KeyCode.F6)){if(PickerOpen)CancelPicker();else OpenPicker();}if(child==null||!child.HasExited)return;
  try{if(System.IO.File.Exists(resultPath)){var result=JsonUtility.FromJson<PracticeResult>(System.IO.File.ReadAllText(resultPath));if(result!=null&&result.sourceId==eventId){session.business.practices.Add(result);if(session.business.ShowingSummary)session.business.SaveCheckpoint();notice="연습 결과: "+result.action+" · "+result.EvaluationText();}else notice="연습 결과를 확인하지 못했습니다";}else notice="연습 창 종료 · 결과 파일 없음";}
  catch(Exception e){notice="연습 결과 읽기 실패: "+e.Message;}
  finally{child.Dispose();child=null;foreach(var path in new[]{inputPath,resultPath})if(!string.IsNullOrEmpty(path)&&System.IO.File.Exists(path))System.IO.File.Delete(path);inputPath=resultPath="";}
 }
 public void FinishPractice(){WriteResult();Application.Quit();}
 void OnApplicationQuit(){if(Enabled)WriteResult();}
 void WriteResult(){var args=Environment.GetCommandLineArgs();int output=Array.IndexOf(args,"-practiceResult"),source=Array.IndexOf(args,"-practiceSource");if(output<0||output+1>=args.Length||source<0||source+1>=args.Length||session==null||session.law==null)return;
  var events=session.law.ledger.Events;var result=new PracticeResult{sourceId=args[source+1],events=events.Length,validFindings=session.law.ledger.ValidForDay(session.law.Day),score=session.law.ledger.Score(session.law.Day),action=(changedConditions?"조건 변경 포함 · ":"")+(events.Length==0?"종료 · 평가 장면 미완료":events[events.Length-1].action)};
  var coach=GetComponent<PracticeCoach>();if(coach&&coach.Active){var feedback=coach.SnapshotFeedback();result.latestOutcome=feedback.outcome;result.feedback=feedback.Details();}
  try{System.IO.File.WriteAllText(args[output+1],JsonUtility.ToJson(result));}catch(Exception e){Debug.LogWarning(e.Message);}
 }
 IEnumerator AutoFinish(){yield return new WaitForSecondsRealtime(3);FinishPractice();}

 public static bool Enabled {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-practice")>=0;}}
 IEnumerator Start(){
  if(!Enabled)yield break;
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-practiceAutoFinish")>=0)StartCoroutine(AutoFinish());
  yield return new WaitForSecondsRealtime(.8f);session.jobs.enabled=false;session.services.enabled=false;session.roadConditions.enabled=false;
  directExitTemplate=GetComponent<ExitQueueSituation>().PreparedTemplate();
  foreach(var p in session.jobs.people)p.SetActive(false);
  foreach(var c in session.traffic.cars){c.Recycle();c.enabled=false;}
  var args=Environment.GetCommandLineArgs();int input=Array.IndexOf(args,"-practiceInput");
  if(input>=0&&input+1<args.Length){PracticeRoute source=null;try{source=JsonUtility.FromJson<PracticeRoute>(System.IO.File.ReadAllText(args[input+1]));}catch(Exception e){Debug.LogWarning("Practice input unavailable: "+e.Message);}if(source!=null)ready=StartExit(source);yield break;}
  if(Array.IndexOf(args,"-practiceExit")>=0){StartDirectExit();ready=true;yield break;}
  foreach(var arg in args)if(arg.StartsWith("-practiceConfusion")){int choice;if(int.TryParse(arg.Substring(18),out choice)&&ConfusionPracticeState.Contains(choice)){SelectedVariant=choice;ready=GetComponent<ConfusionPractice>().StartAttempt(choice);yield break;}}
  int variant=1;foreach(var arg in Environment.GetCommandLineArgs())if(arg.StartsWith("-practiceRight"))int.TryParse(arg.Substring(14),out variant);
  SelectedVariant=Mathf.Clamp(variant,1,6);ready=StartRight(SelectedVariant);
 }
 void OnGUI(){
  var style=new GUIStyle(GUI.skin.button){font=KoreanText.Font,fontSize=15,wordWrap=true};GUI.depth=-160;
  if(PickerOpen){
   float width=Mathf.Min(900,Screen.width-32),height=Mathf.Min(800,Screen.height-32),x=(Screen.width-width)/2,y=(Screen.height-height)/2;
   GUI.color=new Color(.035f,.075f,.10f,1);GUI.DrawTexture(new Rect(x,y,width,height),Texture2D.whiteTexture);GUI.color=Color.white;
   var text=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=16,wordWrap=true};
   GUI.Label(new Rect(x+22,y+14,width-44,40),"운전 연습 선택 · 영업 기록 없이 시작 / 목록 스크롤",text);
   float col=(width-70)/2,listHeight=Mathf.Max(120,height-440);
   pickerScroll=GUI.BeginScrollView(new Rect(x+22,y+60,width-44,listHeight),pickerScroll,new Rect(0,0,width-65,7*48));
   for(int i=1;i<=14;i++){string title=ConfusionPracticeState.Contains(i)?ConfusionPracticeState.Title(i):i==7?"출구 정체 · 대기 / 우회":RightPracticeState.Title(i);if(GUI.Button(new Rect((i-1)%2*(col+10),(i-1)/2*48,col,42),(SelectedVariant==i?"● ":"")+title,style))SelectedVariant=i;}
   GUI.EndScrollView();float body=y+68+listHeight;
   GUI.Label(new Rect(x+22,body,width-44,77),SelectedConditions,text);
   GUI.Label(new Rect(x+22,body+83,width-44,105),"운전 요령\n"+SelectedGuidance,text);
   GUI.Label(new Rect(x+22,body+193,width-44,95),(ConfusionPracticeState.Contains(SelectedVariant)?"가상 교육 시설·차량 배치입니다. 실제 현장 규제로 사용하지 않습니다.\n":"접근 중 시작 조건 유지 · 판단 위치에서 상황 진행.\n")+(Enabled?"선택하면 출발점으로 돌아갑니다. 이전 연습 판정은 유지합니다.":"별도 연습 창을 엽니다. 영업의 돈·시간·진행은 유지합니다.")+"\n"+notice,new GUIStyle(text){fontSize=14});
   if(GUI.Button(new Rect(x+22,y+height-60,col,43),Enabled?"선택한 상황 다시 시작":"선택한 상황 연습 시작",style))StartSelected();
   if(GUI.Button(new Rect(x+32+col,y+height-60,col,43),"취소 · 돌아가기",style))CancelPicker();return;
  }
  if(!Enabled)return;
  if(GetComponent<PracticeCoach>().ResultOpen)return;
  if(!string.IsNullOrEmpty(notice))GUI.Label(new Rect(Screen.width/2-300,130,600,60),notice,style);
  if(!ready){GUI.Label(new Rect(Screen.width/2-220,80,440,40),"연습 구역 준비 중…",style);return;}
  if(GUI.Button(new Rect(Screen.width/2-300,80,295,40),"F6 조건 변경 · 다시 연습",style))OpenPicker();
  if(GUI.Button(new Rect(Screen.width/2+5,80,295,40),"연습 종료 · 원래 영업으로 복귀",style))FinishPractice();
 }
}
