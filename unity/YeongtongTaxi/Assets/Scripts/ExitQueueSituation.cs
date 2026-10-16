using UnityEngine;

// A reproducible signal queue uses real TrafficCar following and the shared lane graph.
public class ExitQueueSituation : MonoBehaviour {
 public DriveSession session;public TrafficJunction junction;public Vector3 approach;public int axis,sign;
 public bool Active {get{return junction!=null;}}
 float started;bool practiceArrived;RoadIncident incident;GameObject label;TextMesh text;
 public PracticeRoute Template {get;private set;}
 public bool Begin(){
  var right=GetComponent<RightTurnSituation>();if(right&&right.Active)return false;
  if(Active||session.jobs.Active||session.taxi.body.velocity.magnitude>=.56f)return false;
  TrafficRoute selected=null;TrafficStop selectedStop=null;
  foreach(var car in session.traffic.cars){foreach(var stop in car.route.stops){
   if(stop.Movement(car.route)<=0||stop.distance<65||stop.junction.halfWidth[stop.axis]<6||Vector3.Distance(session.taxi.transform.position,stop.junction.center)<100)continue;
   if(Vector3.Angle(car.route.Tangent(stop.distance),car.route.Tangent(stop.distance-56))>5)continue;selected=car.route;selectedStop=stop;break;
  }if(selected!=null)break;}
  if(selected==null||session.traffic.cars.Count<8)return false;
  junction=selectedStop.junction;axis=selectedStop.axis;approach=selected.Tangent(selectedStop.distance);sign=session.traffic.Sign(approach,axis,junction);started=Time.time;
  junction.scenarioSignal=true;junction.scenarioForward=SignalColor.Green;junction.observationAxis=axis;junction.observationSign=sign;
  junction.rightTurnObservation=new RoadObservation{known=true,dedicated=true,right=SignalColor.Red,pedestrian=PedestrianState.None};
  float begin=selectedStop.distance-65,end=Mathf.Min(selected.length,selectedStop.exitDistance+35);var points=new System.Collections.Generic.List<float>();
  for(float at=begin;at<end;at+=2){var p=selected.Position(at);points.Add(p.x);points.Add(p.z);}var last=selected.Position(end);points.Add(last.x);points.Add(last.z);
  Template=new PracticeRoute{junction=junction.name,axis=axis,stop=65,exit=selectedStop.exitDistance-begin,points=points.ToArray()};
  for(int i=0;i<8;i++){var c=session.traffic.cars[i];c.Continue(selected);c.Place(selectedStop.distance-.25f-i*7);}
  var position=selected.Position(Mathf.Max(0,selectedStop.distance-65));session.jobs.nav.SetDestination(position);
  label=new GameObject("가상 출구 정체 안내");label.transform.position=position+TrafficSystem.Right(approach)*5+Vector3.up*4;text=label.AddComponent<TextMesh>();text.fontSize=40;text.characterSize=.1f;text.anchor=TextAnchor.MiddleCenter;text.color=Color.yellow;KoreanText.Apply(text);label.AddComponent<FaceCamera>();
  return true;
 }
 public bool Replay(PracticeRoute source){
  if(source==null||source.points==null||source.points.Length<4||source.points.Length%2!=0)return false;
  junction=session.traffic.junctions.Find(j=>j.name==source.junction);if(junction==null)return false;
  var path=new Vector3[source.points.Length/2];for(int i=0;i<path.Length;i++)path[i]=new Vector3(source.points[i*2],0,source.points[i*2+1]);
  var route=new TrafficRoute{axis=source.axis,turn=1};route.SetPoints(path);route.stops.Add(new TrafficStop{junction=junction,axis=source.axis,distance=source.stop,exitDistance=source.exit,turn=1});
  practiceArrived=false;Template=source.Copy();axis=source.axis;approach=route.Tangent(source.stop);sign=session.traffic.Sign(approach,axis,junction);started=Time.time;
  junction.scenarioSignal=true;junction.scenarioForward=SignalColor.Green;junction.observationAxis=axis;junction.observationSign=sign;junction.rightTurnObservation=new RoadObservation{known=true,dedicated=true,right=SignalColor.Red,pedestrian=PedestrianState.None};
  for(int i=0;i<8;i++){var c=session.traffic.cars[i];c.district=null;c.Continue(route);c.Place(source.stop-.25f-i*7);c.enabled=true;}
  var taxi=session.taxi;taxi.body.velocity=taxi.body.angularVelocity=Vector3.zero;taxi.body.position=path[0]-approach*25+Vector3.up*.65f;taxi.body.rotation=Quaternion.LookRotation(approach);taxi.transform.SetPositionAndRotation(taxi.body.position,taxi.body.rotation);Physics.SyncTransforms();session.law.ResetObservation();
  taxi.SetSpawn(taxi.transform.position,taxi.transform.rotation);if(PracticeSession.Enabled)taxi.ResetTaxi();
  session.jobs.nav.SetDestination(path[path.Length-1]);incident=session.roadConditions.AddIncident(route.Position(source.stop),approach,3.2f,30,true,false);
  label=new GameObject("출구 상황 복습 안내");label.transform.position=path[0]+TrafficSystem.Right(approach)*5+Vector3.up*4;text=label.AddComponent<TextMesh>();text.fontSize=40;text.characterSize=.1f;text.anchor=TextAnchor.MiddleCenter;text.color=Color.yellow;KoreanText.Apply(text);label.AddComponent<FaceCamera>();return true;
 }
 void Update(){
  if(!session.business.useV2)return;
  if(!PracticeSession.Enabled&&Input.GetKeyDown(KeyCode.F5)&&!session.paused){if(Active)Stop();else Begin();}
  if(!Active)return;
  if(PracticeSession.Enabled&&!practiceArrived){
   started=Time.time;var tail=session.traffic.cars[7].transform.position;
   if(Vector3.Distance(session.taxi.transform.position,tail)<22)practiceArrived=true;
  }
  float elapsed=Time.time-started;if(PracticeSession.Enabled&&incident!=null)incident.expiresAt=Time.time+Mathf.Max(0,30-elapsed);junction.rightTurnObservation.right=elapsed<30?SignalColor.Red:SignalColor.Green;
  text.text="가상 출구 신호 대기열 · "+junction.name+"\n뒤에서 대기 / 직진 후 내비 재탐색\n우회전전용신호 "+(elapsed<30?"적색":"녹색 화살표");
  if(elapsed>=65&&!PracticeSession.Enabled)Stop();
 }
 public void Stop(){if(PracticeSession.Enabled&&Active)foreach(var c in session.traffic.cars){c.Recycle();c.enabled=false;}if(incident!=null){session.roadConditions.incidents.Remove(incident);incident=null;}if(junction!=null){junction.scenarioSignal=false;junction.rightTurnObservation=null;junction=null;}if(label)Destroy(label);}
 void OnGUI(){if(PracticeSession.Enabled)return;if(!session.business.useV2||!session.ShowHints||session.business.ShowingSummary||session.business.preparing||session.law.HistoryOpen||session.services.open)return;GUI.Label(new Rect(420,44,700,25),"F5 출구 정체 상황 (빈차·정차·충분히 떨어진 구간) · "+(Active?junction.name:"준비"),new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=14});}
}
