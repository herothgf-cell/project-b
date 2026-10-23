using System.Collections;
using System.IO;
using UnityEngine;
public class TrafficSmokeTest : MonoBehaviour {
    public DriveSession session;
    IEnumerator Start() {
        yield return new WaitForSeconds(1);
        var traffic=session.traffic;var taxi=session.taxi;taxi.automated=true;
        // Move the player off the traffic routes while checking the signal independently.
        taxi.body.position=new Vector3(240,.65f,60);taxi.body.velocity=Vector3.zero;Physics.SyncTransforms();
        traffic.manualClock=true;traffic.manualTime=18;
        var car=traffic.cars[0];foreach(var other in traffic.cars)if(other!=car){other.Recycle();other.enabled=false;}car.Place(0);
        float line=car.route.stops[0].distance;yield return new WaitForSeconds(5);
        bool red=car.distance<=line+.05f&&car.speed<.15f&&car.redStops>0;float redDistance=car.distance;
        traffic.manualTime=1;yield return new WaitForSeconds(4);
        bool go=car.distance>redDistance+3&&car.speed>1;
        // Put the actual taxi ahead of a car and verify it stops short of collision.
        foreach(var other in traffic.cars)other.enabled=false;
        car.enabled=true;car.Place(35);traffic.manualTime=1;
        var obstacle=car.route.start+car.route.direction*48;taxi.body.position=obstacle+Vector3.up*.65f;taxi.body.rotation=Quaternion.LookRotation(car.route.direction);taxi.body.velocity=Vector3.zero;taxi.body.angularVelocity=Vector3.zero;Physics.SyncTransforms();
        yield return new WaitForSeconds(5);
        bool obstacleStop=car.speed<.15f&&car.distance<44&&car.waitingForObstacle;float obstacleDistance=car.distance;
        Time.timeScale=0;float frozen=car.distance;yield return new WaitForSecondsRealtime(.25f);bool pause=car.distance==frozen;Time.timeScale=1;
        session.signals.Toggle(-1);bool left=session.signals.direction==-1;bool initialLit=session.signals.Lit;yield return new WaitForSeconds(.4f);bool blink=session.signals.Lit!=initialLit;session.signals.Toggle(1);bool right=session.signals.direction==1;session.signals.Toggle(1);bool off=session.signals.direction==0;
        taxi.ResetTaxi();foreach(var other in traffic.cars){other.enabled=true;}traffic.manualTime=18;car.Place(35);
        session.signals.Toggle(-1);session.drivingCamera.SetFirstPerson(false);yield return new WaitForSeconds(2);
        string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts"));Directory.CreateDirectory(folder);
        yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"traffic-third-person.png"));yield return new WaitForSeconds(.4f);
        // Check the signal at a real waiting position, with the taxi's nose behind the line.
        taxi.body.position=traffic.junctions[0].center-traffic.mainDirection*16.5f+TrafficSystem.Right(traffic.mainDirection)*2.5f+Vector3.up*.65f;taxi.body.velocity=Vector3.zero;Physics.SyncTransforms();
        session.drivingCamera.SetFirstPerson(true);yield return new WaitForSeconds(.5f);
        bool cockpit=Vector3.Distance(Camera.main.transform.position,taxi.transform.TransformPoint(new Vector3(-.43f,1.07f,.6f)))<.1f && (Camera.main.cullingMask&(1<<8))==0;
        var lampPoint=Camera.main.WorldToViewportPoint(traffic.junctions[0].lamps[1][0].transform.position);bool signalVisible=lampPoint.z>0&&lampPoint.x>.05f&&lampPoint.x<.95f&&lampPoint.y>.05f&&lampPoint.y<.95f;
        yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"traffic-first-person.png"));yield return new WaitForSeconds(.4f);
        session.drivingCamera.testRearView=true;yield return new WaitForSeconds(.3f);bool rear=Vector3.Dot(Camera.main.transform.forward,taxi.transform.forward)<-.99f;
        yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"traffic-rear-view.png"));yield return new WaitForSeconds(.4f);
        session.drivingCamera.testRearView=false;session.drivingCamera.SetFirstPerson(false);yield return null;
        bool chase=(Camera.main.cullingMask&(1<<8))!=0;
        bool ok=red&&go&&obstacleStop&&pause&&left&&right&&off&&blink&&cockpit&&signalVisible&&rear&&chase;
        string result="success="+ok+" redStop="+red+" redDistance="+redDistance+" stopLine="+line+" greenDeparture="+go+" obstacleStop="+obstacleStop+" obstacleDistance="+obstacleDistance+" pause="+pause+" indicators="+(left&&right&&off&&blink)+" cockpit="+cockpit+" signalVisible="+signalVisible+" rear="+rear+" chase="+chase+" cars="+traffic.cars.Count;
        File.WriteAllText(Path.Combine(folder,"traffic-smoke.txt"),result);Debug.Log(result);Application.Quit(ok?0:1);
    }
}
