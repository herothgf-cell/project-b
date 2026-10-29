using System;
using System.Collections;
using UnityEngine;
using Pvp.Driving;
namespace Pvp {
public sealed class PvpPracticeProbe:MonoBehaviour {
    IEnumerator Start(){
        yield return null;
        try {
            var bootstrap=GetComponent<PvpBootstrap>();bootstrap.StartPractice();
            var session=GetComponent<PvpPracticeSession>();session.SetScripted(true);
            var before=session.Capture();var input=new PvpVehicleInput(1,0,0);
            for(int i=0;i<100;i++)session.Step(input,.02f);
            var accelerated=session.Capture();
            if(Vector3.Distance(before.Position,accelerated.Position)<10||accelerated.Speed<10)throw new Exception("acceleration integration");
            input=new PvpVehicleInput(0,1,0);for(int i=0;i<100;i++)session.Step(input,.02f);
            if(Mathf.Abs(session.Capture().Speed)>.001f)throw new Exception("brake integration");
            session.NextTarget();if(!session.CurrentRoute.Found)throw new Exception("legal target route");
            session.Recover();if(session.Capture().Speed!=0)throw new Exception("recovery stop");
            if(bootstrap.CurrentSettings!=null)throw new Exception("offline practice invented a match");
            foreach(var behavior in FindObjectsOfType<MonoBehaviour>())if(behavior.GetType().Name=="DriveSession")throw new Exception("campaign session exists");
            Debug.Log("PVP_PRACTICE_PLAYER_OK distance="+session.DrivenDistance+" map="+session.Map.MapHash+" acceleration/brake/route/recovery/offline");
        }catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
        yield return new WaitForEndOfFrame();
        var args=Environment.GetCommandLineArgs();int capture=Array.IndexOf(args,"-pvpCapturePath");
        if(capture>=0&&capture+1<args.Length){// Capture the game after the startup splash, not its first black frame.
            float deadline=Time.realtimeSinceStartup+15;
            while(!UnityEngine.Rendering.SplashScreen.isFinished && Time.realtimeSinceStartup<deadline)yield return null;
            yield return new WaitForSecondsRealtime(3);
            yield return new WaitForSecondsRealtime(3);
            for(int frame=0;frame<10;frame++)yield return null;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(args[capture+1]);yield return null;yield return null;}
        yield return new WaitForSeconds(.5f);Application.Quit(0);
    }
}}
