using System.Collections;
using System.IO;
using UnityEngine;
public class DriveSmokeTest : MonoBehaviour {
    public DriveSession session;
    IEnumerator Start() {
        var taxi=session.taxi;taxi.automated=true;
        yield return new WaitForSeconds(2);var start=taxi.transform.position; Debug.Log("SMOKE start="+start+" physics="+taxi.body.position);
        taxi.autoThrottle=1;yield return new WaitForSeconds(2);float speed=taxi.Speed;float moved=Vector3.Distance(start,taxi.transform.position);
        taxi.autoThrottle=0;taxi.autoBrake=true;yield return new WaitForSeconds(1.5f);float stopped=Mathf.Abs(taxi.Speed);
        taxi.autoBrake=false;taxi.autoThrottle=-1;yield return new WaitForSeconds(1.5f);float reverse=taxi.Speed;
        taxi.autoThrottle=0;taxi.ResetTaxi();yield return new WaitForSeconds(1);float resetDistance=Vector3.Distance(start,taxi.transform.position);Debug.Log("SMOKE reset="+taxi.transform.position+" physics="+taxi.body.position);
        taxi.autoThrottle=1;taxi.autoSteer=.6f;var before=taxi.transform.forward;yield return new WaitForSeconds(2);float turn=Vector3.Angle(before,taxi.transform.forward);
        taxi.autoThrottle=0;taxi.autoSteer=0;taxi.ResetTaxi();yield return new WaitForSeconds(1);
        // Follow the real T1-T2 corridor to check that geometry and colliders allow arrival.
        float deadline=Time.time+45; var destination=session.world.map.targets[1].V();
        while(Time.time<deadline && session.visits==0) {
            var offset=destination-taxi.transform.position;offset.y=0;
            taxi.autoSteer=Mathf.Clamp(Vector3.SignedAngle(taxi.transform.forward,offset,Vector3.up)/22,-1,1);
            taxi.autoThrottle=taxi.Speed<10?1:0; yield return null;
        }
        bool arrived=session.visits>0;taxi.autoThrottle=0;taxi.autoSteer=0;taxi.ResetTaxi();yield return new WaitForSeconds(1);
        Time.timeScale=0; var pausePosition=taxi.body.position; yield return new WaitForSecondsRealtime(.3f); bool pauseOk=Vector3.Distance(pausePosition,taxi.body.position)<.001f; taxi.ResetTaxi(); Time.timeScale=1; yield return new WaitForSeconds(.5f);
        bool ok=speed>2&&moved>2&&stopped<.5f&&reverse<-.5f&&resetDistance<1&&turn>3&&arrived&&pauseOk;
        string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts"));Directory.CreateDirectory(folder);
        string result="success="+ok+" forwardMs="+speed+" moved="+moved+" stoppedMs="+stopped+" reverseMs="+reverse+" resetDistance="+resetDistance+" turnDegrees="+turn+" arrivedT2="+arrived+" pauseOk="+pauseOk+" GPU="+SystemInfo.graphicsDeviceName;
        File.WriteAllText(Path.Combine(folder,"drive-smoke.txt"),result);Debug.Log(result);
        yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"first-drive.png"));yield return new WaitForSeconds(1);Application.Quit(ok?0:1);
    }
}
