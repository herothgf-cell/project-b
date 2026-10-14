using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

// Test driver supplies decisions; gameplay controller still performs all steering/physics.
public class AssistedTrialSmokeTest : MonoBehaviour {
    public DriveSession session;
    IEnumerator Start(){
        yield return new WaitForSeconds(1);var trial=session.GetComponent<AssistedRouteTrial>();var taxi=session.taxi;var assist=session.assisted;
        if(!trial||trial.primary==null||trial.primary.Count<2){Debug.LogError("TRIAL_TEST_NO_ROUTE");Application.Quit(1);yield break;}
        taxi.automated=true;assist.allowAutomated=true;var report=new StringBuilder();bool ok=true;float[] times=new float[4];int run=0;
        trial.Restart(true);Physics.SyncTransforms();
        bool spawnClear=Physics.OverlapBox(taxi.transform.position+Vector3.up*.4f,new Vector3(1.1f,.6f,2.2f),taxi.transform.rotation,1<<9).Length==0;
        report.AppendLine("spawn_clear_after_restart="+spawnClear);ok&=spawnClear;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-captureOnly")>=0){
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts","assisted-trial-screen.png")));
            yield return new WaitForSeconds(.5f);Debug.Log(report.ToString());Application.Quit(ok?0:1);yield break;
        }
        foreach(bool congested in new[]{false,true})foreach(bool detour in new[]{false,true}){
            trial.Restart(congested);var route=detour?trial.alternative:trial.primary;int index=0;float deadline=Time.time+240;
            // Isolate the controlled route event from unrelated AI interactions for a repeatable comparison.
            foreach(var car in session.traffic.cars){car.Recycle();car.enabled=false;}
            Time.timeScale=2;
            while(!trial.Finished&&Time.time<deadline&&assist.Active){
                if(!assist.Changing&&!assist.Turning){
                    for(int i=index;i<route.Count;i++)if(assist.CurrentLane==route[i]){index=i;break;}
                    if(index+1<route.Count){var next=route[index+1];if(next.corridor==assist.CurrentLane.corridor){if(taxi.Speed>.6f)assist.RequestLaneChange(next.index-assist.CurrentLane.index);}
                        else for(int turn=-1;turn<=1;turn++){var join=assist.network.Connection(assist.CurrentLane,turn);if(join!=null&&join.target==next&&turn!=0&&assist.PendingTurn!=turn)assist.RequestTurn(turn);}
                    }
                }
                float gap=100;var p=taxi.transform.position;var forward=taxi.transform.forward;
                foreach(var j in session.traffic.junctions){int axis=Mathf.Abs(Vector3.Dot(forward,session.traffic.mainDirection))>.7f?0:1;var incoming=(axis==0?session.traffic.mainDirection:TrafficSystem.Right(session.traffic.mainDirection))*Mathf.Sign(Vector3.Dot(forward,axis==0?session.traffic.mainDirection:TrafficSystem.Right(session.traffic.mainDirection)));
                    var stop=j.center-incoming*(axis==0?14:9);float along=Vector3.Dot(stop-p,incoming)-2.3f,lateral=Mathf.Abs(Vector3.Dot(j.center-p,TrafficSystem.Right(incoming)));
                    if(!assist.Turning&&along>-.5f&&along<35&&lateral<(axis==0?5:10)&&session.traffic.MovementSignal(j,axis,assist.PendingTurn)!=SignalColor.Green)gap=Mathf.Min(gap,Mathf.Max(0,along));
                }
                for(float d=3;d<20;d+=1){bool blocked=false;foreach(var h in Physics.OverlapBox(p+forward*d+Vector3.up*.2f,new Vector3(.85f,.45f,.7f),taxi.transform.rotation,1<<9))if(h.attachedRigidbody!=taxi.body)blocked=true;if(blocked){gap=Mathf.Min(gap,Mathf.Max(0,d-3));break;}}
                var goalDelta=trial.goal-p;goalDelta.y=0;if(index==route.Count-1&&Vector3.Dot(goalDelta,forward)>-1)gap=Mathf.Min(gap,Mathf.Max(0,goalDelta.magnitude-1));
                float target=Mathf.Min(6,Mathf.Sqrt(gap*3));taxi.autoThrottle=taxi.Speed<target-.2f?.35f:0;taxi.autoBrake=taxi.Speed>target+.2f||gap<.5f;
                yield return new WaitForFixedUpdate();
            }
            taxi.autoThrottle=0;taxi.autoBrake=true;Time.timeScale=1;times[run++]=trial.Elapsed;ok&=trial.Finished;
            report.AppendLine("congested="+congested+" detour="+detour+" finished="+trial.Finished+" seconds="+trial.Elapsed+" lane="+(assist.CurrentLane==null?-1:assist.CurrentLane.id)+" status="+assist.Status);
            if(!trial.Finished)break;
        }
        string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts"));Directory.CreateDirectory(folder);
        if(run==4){report.AppendLine("normal_direct_faster="+(times[0]<times[1])+" congested_detour_faster="+(times[3]<times[2]));ok&=times[0]<times[1]&&times[3]<times[2];}
        File.WriteAllText(Path.Combine(folder,"assisted-trial-test.txt"),"success="+ok+"\n"+report);Debug.Log(report.ToString());Application.Quit(ok?0:1);
    }
}
