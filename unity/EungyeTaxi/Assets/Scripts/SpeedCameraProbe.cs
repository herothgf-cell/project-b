using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// Speed cameras, player acceptance (Eungye single, 2026-11-24). The taxi is moved along a real lane through camera
// points at set speeds: the warning shows within 150 m, 78 km/h past a 60 camera costs 2,000 won once a day,
// a second pass is not fined, 35 km/h past a school-zone 30 camera is not fined.
public sealed class SpeedCameraProbe : MonoBehaviour {
 const string Flag="-cameraCheck";
 DriveSession s;int checks;readonly StringBuilder report=new StringBuilder();string output;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),Flag)>=0){Application.runInBackground=true;new GameObject("Speed camera acceptance").AddComponent<SpeedCameraProbe>();}}
 void Check(bool ok,string reason){checks++;report.AppendLine((ok?"PASS ":"FAIL ")+reason);Debug.Log("SPEED_CAMERA_PLAYER "+(ok?"PASS ":"FAIL ")+reason);if(!ok)throw new Exception(reason);}
 void Info(string line){report.AppendLine("INFO "+line);}
 // A lane path that passes the camera, starting about 200 m before it.
 HdRoadGraph.RoutePath Approach(SpeedCameraRules.Camera c,out float camAt){
  var g=s.jobs.nav.hd;camAt=0;
  for(int i=0;i<g.edges.Length;i++){if(g.edges[i].change!=0||g.edges[i].TraversalDisabled)continue;float err;float at=g.paths[i].Project(c.V(),out err);if(err>3||at<1||at>g.paths[i].length-1)continue;
   var chain=new List<int>{i};float back=at;int cur=i;
   while(back<210&&chain.Count<20){int best=-1;float score=float.MaxValue;for(int j=0;j<g.edges.Length;j++){if(g.edges[j].change!=0||g.edges[j].TraversalDisabled||g.edges[j].next==null||Array.IndexOf(g.edges[j].next,cur)<0||chain.Contains(j))continue;float angle=Vector3.Angle(g.paths[j].Tangent(g.paths[j].length),g.paths[cur].Tangent(0));if(angle<score){score=angle;best=j;}}
    if(best<0||score>25)break;chain.Insert(0,best);back+=g.paths[best].length;cur=best;}
   if(back<170)continue;
   float startLocal=Mathf.Min(Mathf.Max(0,back-200),g.paths[chain[0]].length-1);
   var route=g.Compose(chain,startLocal,Mathf.Min(g.paths[i].length,at+30));if(route==null)continue;
   float e2;camAt=route.path.Project(c.V(),out e2);if(e2>3||camAt<150)continue;return route;}
  return null;
 }
 IEnumerator Run(SpeedCameraRules.Camera c,float kmh,Action<bool> warned){
  float camAt;var route=Approach(c,out camAt);if(route==null){Info("no approach for "+c.id);warned(false);yield break;}
  var t=s.taxi;var wait=new WaitForFixedUpdate();bool sawWarning=false;float d=0;
  var p0=route.path.Position(0);var d0=route.path.Tangent(0);t.body.velocity=Vector3.zero;t.body.position=p0+Vector3.up*.65f;t.body.rotation=Quaternion.LookRotation(d0);t.transform.SetPositionAndRotation(t.body.position,t.body.rotation);Physics.SyncTransforms();
  yield return new WaitForSeconds(.5f);float v=kmh/3.6f;
  while(d<camAt+20){d+=v*Time.fixedDeltaTime;var tan=route.path.Tangent(d);tan.y=0;tan.Normalize();t.body.MovePosition(route.path.Position(d)+Vector3.up*.65f);t.body.MoveRotation(Quaternion.LookRotation(tan));var vel=tan*v;vel.y=0;t.body.velocity=vel;
   if(s.cameras.Ahead==c&&s.cameras.AheadMetres<=150)sawWarning=true;yield return wait;}
  t.body.velocity=Vector3.zero;yield return new WaitForSeconds(.3f);warned(sawWarning);
 }
 IEnumerator Start(){
  float limit=Time.realtimeSinceStartup+180;
  while(!(s=FindObjectsOfType<DriveSession>().FirstOrDefault(x=>!x.IsSimulationBot))||!s.StartupReady){if(!FindObjectOfType<DriveSession>()){var mode=FindObjectOfType<UnifiedModeBootstrap>();if(mode)mode.StartSingle();}if(Time.realtimeSinceStartup>limit){Finish("startup timeout");yield break;}yield return null;}
  output=ContentTuningRuntime.Argument("-cameraOutput");if(string.IsNullOrEmpty(output))output=Path.GetFullPath("artifacts/camera");Directory.CreateDirectory(output);
  yield return null;string error=null;
  try{s.business.preparing=false;s.paused=false;Time.timeScale=1;s.jobs.ClearForDay();foreach(var c in FindObjectsOfType<TrafficCar>())c.gameObject.SetActive(false);
   Check(s.cameras,"the speed camera component is attached");Check(SpeedCameraRules.Cameras.Length==17,"17 camera records loaded");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  var cam=SpeedCameraRules.Cameras.First(c=>c.id=="H0649");var l=s.business.ledger;
  int cash=l.Account.Cash;bool warned=false;
  yield return Run(cam,78,w=>warned=w);
  try{Check(warned,"the warning (limit and distance) shows within 150 m");Check(s.cameras.Tickets==1&&s.cameras.LastTicket=="H0649","78 km/h past the 60 camera is fined");Check(l.Account.Cash==cash-2000&&l.Account.Expense(l.Day,"fine")==2000,"2,000 won from cash, recorded as a fine ("+cash+" -> "+l.Account.Cash+")");
   var n=s.GetComponent<DriveNotifications>();Check(n&&n.Feed.Any(x=>x.Contains("단속 카메라")&&x.Contains("과태료 2,000원")),"a card tells the driver");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  ScreenCapture.CaptureScreenshot(Path.Combine(output,"camera.png"));yield return null;
  yield return Run(cam,90,w=>warned=w);
  try{Check(s.cameras.Tickets==1&&l.Account.Cash==cash-2000,"the same camera does not fine twice in a day");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  var school=SpeedCameraRules.Cameras.First(c=>c.id=="J1375");
  yield return Run(school,35,w=>warned=w);
  try{Check(warned,"school-zone camera warning shows");Check(s.cameras.Tickets==1,"35 km/h past a 30 camera (within +10) is not fined");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  yield return Run(school,46,w=>warned=w);
  try{Check(s.cameras.Tickets==2&&s.cameras.LastTicket=="J1375"&&l.Account.Cash==cash-4000,"46 km/h past the school-zone 30 camera: 2,000 won");}catch(Exception e){error=e.ToString();}
  Finish(error);
 }
 void Finish(string error){
  if(error!=null)Debug.LogError(error);
  if(output!=null)File.WriteAllText(Path.Combine(output,"report.txt"),report.ToString()+(error??""));
  Debug.Log("SPEED_CAMERA_PLAYER_"+(error==null?"OK":"FAIL")+" checks="+checks+" "+(error??""));Application.Quit(error==null?0:1);
 }
}
