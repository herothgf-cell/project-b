using System.Collections;
using System.Text;
using UnityEngine;

public class RoadConditionsSmokeTest : MonoBehaviour {
 public DriveSession session;
 bool ok=true;readonly StringBuilder report=new StringBuilder();
 void Check(bool value,string label){ok&=value;report.AppendLine((value?"PASS ":"FAIL ")+label);}
 IEnumerator Start(){
  yield return null;
  foreach(var car in session.traffic.cars)car.gameObject.SetActive(false);
  var conditions=session.roadConditions;conditions.ResetForDay();
  var incident=conditions.AddIncident(new Vector3(50,0,0),Vector3.right,7,60);
  Check(conditions.ExtraSeconds(Vector3.zero,new Vector3(100,0,0),Vector3.right,7)>50,"directed narrow-road obstruction adds wait");
  Check(conditions.ExtraSeconds(new Vector3(100,0,0),Vector3.zero,Vector3.left,7)==0,"opposite carriageway has no incident penalty");
  Check(conditions.ExtraSeconds(Vector3.zero,new Vector3(100,0,0),Vector3.right,14)==8,"wide road retains inner-lane pass option");
  incident.expiresAt=Time.time-1;
  Check(conditions.ExtraSeconds(Vector3.zero,new Vector3(100,0,0),Vector3.right,7)==0,"expired incident has no delay even before cleanup");
  conditions.ResetForDay();Physics.SyncTransforms();
  Check(conditions.TryCreateEvent(),"real NGII map has a clear parked-car placement");
  Check(conditions.incidents.Count==1,"one event placed without duplicates");
  if(conditions.incidents.Count>0){
   var item=conditions.incidents[0];bool visible=false,solid=false;foreach(var obj in item.visuals){visible|=obj.GetComponentInChildren<Renderer>()!=null;solid|=obj.GetComponent<Collider>()!=null&&obj.layer==9;}
   Check(visible&&solid,"event has visible car and traffic collision layer");
   foreach(var stop in session.jobs.stops)Check(Vector3.Distance(stop.Kerb,item.position)>=16,"stop curb remains clear: "+stop.name);
   session.taxi.body.isKinematic=true;session.taxi.transform.SetPositionAndRotation(item.position-item.direction*24+Vector3.up*.65f,Quaternion.LookRotation(item.direction));
   session.taxi.body.position=session.taxi.transform.position;session.taxi.body.rotation=session.taxi.transform.rotation;
  }
  session.jobs.Accept(0);session.jobs.nav.Refresh();
  Check(session.jobs.nav.path.Count>1,"pickup route is available with incident");
  Check(MapCatalog.Available("eungye")== (Resources.Load<TextAsset>("eungye-map")!=null),"region availability matches imported map resource");
  yield return new WaitForSeconds(1);
  string dir=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../..","artifacts"));
  ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(dir,"route-conditions.png"));yield return new WaitForSeconds(.4f);
  var visuals=new System.Collections.Generic.List<GameObject>();foreach(var item in conditions.incidents)visuals.AddRange(item.visuals);
  conditions.ResetForDay();yield return null;bool cleared=conditions.incidents.Count==0;foreach(var obj in visuals)cleared&=obj==null;
  Check(cleared,"new day removes incidents and their colliders");
  System.IO.File.WriteAllText(System.IO.Path.Combine(dir,"road-conditions-smoke.txt"),"success="+ok+"\n"+report);Application.Quit(ok?0:1);
 }
}
