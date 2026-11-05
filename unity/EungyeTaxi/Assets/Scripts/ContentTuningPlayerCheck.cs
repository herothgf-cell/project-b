using System;
using System.Collections;
using System.IO;
using UnityEngine;

// Explicit acceptance runner; normal play never installs this component.
public sealed class ContentTuningPlayerCheck:MonoBehaviour {
 int checks;float began;string input,originalInput;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Install(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-contentTuningCheck")>=0){var go=new GameObject("Content settings acceptance");DontDestroyOnLoad(go);go.AddComponent<ContentTuningPlayerCheck>();}}
 void Check(bool value,string reason){checks++;Debug.Log("CONTENT_PLAYER "+(value?"PASS ":"FAIL ")+reason);if(!value)throw new Exception(reason);}
 void Update(){if(Time.realtimeSinceStartup-began>180){Debug.LogError("CONTENT_PLAYER timeout");Application.Quit(2);}}
 DriveSession Human(){foreach(var s in FindObjectsOfType<DriveSession>())if(!s.IsSimulationBot)return s;return null;}
 IEnumerator Ready(){while(!Human()||!Human().StartupReady||!Human().business||Human().business.IsRestoring)yield return null;yield return null;}
 IEnumerator Start(){began=Time.realtimeSinceStartup;var run=Run();while(true){object next;try{if(!run.MoveNext())yield break;next=run.Current;}catch(Exception e){Debug.LogError("CONTENT_PLAYER_EXCEPTION "+e);Application.Quit(2);yield break;}yield return next;}}
 IEnumerator Run(){
  Check(ContentTuningRuntime.TestMode,"explicit isolated test mode");input=ContentTuningRuntime.Argument("-contentProfile");Check(!string.IsNullOrEmpty(input)&&File.Exists(input),"explicit test profile exists");originalInput=File.ReadAllText(input);
  yield return null;FindObjectOfType<UnifiedModeBootstrap>().StartSingle();yield return Ready();
  var s=Human();string path=s.business.SavePath,active=ContentTuningRuntime.Snapshot();
  Check(path.StartsWith(ContentTuningRuntime.TestDirectory+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"campaign path is isolated");
  Check(ContentTuning.Int("law.redStraight.penalty",20)==37,"actual session loads changed law value");
  Check(ContentTuning.Int("request.Comfort.success",8)==11,"actual session loads changed request value");
  Check(PassengerRequest.Delta(RequestKind.Comfort,true)==11,"actual request evaluator consumes profile");
  var behavior=PassengerBehaviorState.Create("content-player-food",PassengerType.Food);Check(behavior.Respond(1,true)&&behavior.Board(),"native food choice accepted");Check(behavior.SatisfactionDelta==9,"actual food satisfaction consumes profile");
  Check(s.business.SaveCheckpoint()&&File.Exists(path),"checkpoint written only to isolated path");
  var saved=BusinessProgress.ParseCheckpoint(File.ReadAllText(path));Check(saved.contentProfileJson==active,"full active profile is in actual checkpoint");
  ContentTuning.UseDefaults();string defaults=ContentTuning.CaptureJson();ContentTuningRuntime.Restore(active);File.WriteAllText(input,defaults);
  Check(ContentTuningRuntime.Snapshot()==active,"disk edit does not mutate live session");
  Check(FindObjectOfType<UnifiedModeBootstrap>().ReturnToMenu(),"leave session saves successfully");while(Human())yield return null;yield return null;
  FindObjectOfType<UnifiedModeBootstrap>().StartSingle();yield return Ready();s=Human();
  Check(s.business.SavePath==path,"resume uses same isolated campaign");Check(ContentTuningRuntime.Snapshot()==active&&ContentTuning.Int("law.redStraight.penalty",20)==37,"resume keeps saved settings despite new disk profile");
  Check(s.business.RequestRestart()&&s.business.ConfirmRestart(true),"explicit new test campaign starts");
  Check(ContentTuning.Int("law.redStraight.penalty",20)==20,"new campaign reads new disk settings");
  Check(BusinessProgress.ParseCheckpoint(File.ReadAllText(path)).contentProfileJson==ContentTuningRuntime.Snapshot(),"new campaign checkpoint captures new settings");
  Debug.Log("CONTENT_TUNING_PLAYER_OK checks="+checks+" save="+path);Application.Quit(0);
 }
 void OnApplicationQuit(){if(input!=null&&originalInput!=null)File.WriteAllText(input,originalInput);}
}
