using UnityEngine;
[System.Serializable] public sealed class ScenarioChoiceSnapshot {
 public string courseId,plan,junctionName;public bool completed,open,offered,alternate,observing,previousPause;public float previousScale,releasedAge;public int day,panelsToday,axis,sign;public Vector3 direction;public RightPracticeSnapshot pedestrian;
}

// Normal-business choice: input selects a plan, never teleports/steers or scores the answer.
public sealed class ScenarioChoiceSituation : MonoBehaviour {
 public DriveSession session;public PrototypeScenario Course {get;private set;}
 public bool Completed {get;private set;}public bool Open {get;private set;}public string Plan {get;private set;}="직접 운전";
 public bool ActiveObservation {get{return observing;}}
 public TrafficJunction ActiveJunction {get{return observing?junction:null;}}
 public bool TryActiveCrossing(out Vector3 a,out Vector3 b){a=b=Vector3.zero;if(!observing||pedestrianState==null||pedestrianState.Observe().pedestrian!=PedestrianState.Crossing)return false;a=CrossingPoint(0);b=CrossingPoint(1);return true;}
 public bool ClosedThisFrame {get{return closedFrame==Time.frameCount;}}
 bool offered,alternate,observing,previousPause;float previousScale;int closedFrame=-1,day=-1,panelsToday;
 TrafficJunction junction;Vector3 direction;int axis,sign;RightPracticeState pedestrianState;GameObject person;float releasedAt;
 public ScenarioChoiceSnapshot CaptureLive(){if(Course==null)return null;return new ScenarioChoiceSnapshot{courseId=Course.id,plan=Plan,completed=Completed,open=Open,offered=offered,alternate=alternate,observing=observing,previousPause=previousPause,previousScale=previousScale,releasedAge=Completed?Mathf.Max(0,Time.time-releasedAt):0,day=day,panelsToday=panelsToday,junctionName=junction==null?null:junction.name,direction=direction,axis=axis,sign=sign,pedestrian=pedestrianState==null?null:pedestrianState.Snapshot()};}
 public void RestoreLive(ScenarioChoiceSnapshot saved){if(saved==null){Stop();return;}var zones=GetComponent<ServiceLawZones>();var course=zones.scenarios.Find(c=>c.id==saved.courseId);if(course==null||saved.alternate&&course.id!="PS08"||saved.panelsToday<0||float.IsNaN(saved.releasedAge)||float.IsInfinity(saved.releasedAge)||saved.releasedAge<0)throw new System.ArgumentException("Invalid saved driving plan");Stop();Course=course;Plan=saved.plan??"직접 운전";Completed=saved.completed;offered=saved.offered;alternate=saved.alternate;day=saved.day;panelsToday=saved.panelsToday;Open=saved.open;previousPause=saved.previousPause;previousScale=saved.previousScale>0?saved.previousScale:1;releasedAt=Time.time-saved.releasedAge;closedFrame=-1;
  if(alternate){zones.BeginAlternative();session.jobs.nav.AvoidEdge=Course.connector;session.jobs.nav.ObserveEdge(Course.alternate);if(Completed)zones.CompleteAlternative();session.jobs.nav.Refresh();}
  if(saved.observing){junction=session.traffic.junctions.Find(j=>j.name==saved.junctionName);if(Course.id!="PS09"||junction==null||saved.axis<0||saved.axis>1||Mathf.Abs(saved.sign)!=1||saved.direction.sqrMagnitude<.9f)throw new System.ArgumentException("Invalid saved pedestrian observation");direction=saved.direction;axis=saved.axis;sign=saved.sign;pedestrianState=RightPracticeState.Restore(saved.pedestrian);CreatePedestrian();observing=true;ObserveRightTurn(false);}
 }
 public void Begin(PrototypeScenario course){Stop();Course=course;Completed=offered=alternate=false;Plan="직접 운전";if(day!=session.law.Day){day=session.law.Day;panelsToday=0;}}
 public void Stop(){Close();if(observing&&junction!=null){junction.scenarioSignal=false;junction.rightTurnObservation=null;}if(person)Destroy(person);observing=false;Course=null;}
 public string DetourDiagnostic {get;private set;}="";
 public bool CanDetour(){if(Course==null||Course.id!="PS08")return false;float at;var hd=session.jobs.nav.hd;int start=hd.Nearest(session.taxi.transform.position,session.taxi.transform.forward,out at);var to=session.jobs.stops[session.jobs.current.drop];var route=hd.Route(start,at,to.laneEdge,to.laneAt,PrototypeScenario.AvoidWeights(hd,Course.connector));DetourDiagnostic="position="+session.taxi.transform.position+" body="+session.taxi.body.position+" heading="+session.taxi.transform.forward+" start="+start+" at="+at+" via="+Course.alternate+" goal="+to.laneEdge+" at="+to.laneAt+" route="+(route==null?"null":string.Join(",",route.edges));return route!=null&&!route.edges.Contains(Course.connector);}
 public bool CanOfferSaved(){int count=0;foreach(var e in session.law.ledger.Events)if(e.family=="선택 안내 노출"){if(Course!=null&&e.scenario==Course.id)return false;if(e.day==session.law.Day)count++;}return count<2;}
 void RecordExposure(){var e=new LawEvent{recoveryPolicyVersion=1,id=session.law.ids.Next("choice-seen"),day=session.law.Day,scenario=Course.id,tripId=session.law.tripId,family="선택 안내 노출",occupied=true,reason="캠페인 최초 안내 · 선택 자체는 점수 없음"};e.Add("안내 노출",LawVerdict.NoOpportunity,0);session.law.ledger.Record(e);}
 public bool Select(int choice){if(!Open)return false;
  if(Course.id=="PS08"&&choice==1){if(!CanDetour())return false;alternate=true;GetComponent<ServiceLawZones>().BeginAlternative();session.jobs.nav.AvoidEdge=Course.connector;session.jobs.nav.ObserveEdge(Course.alternate);session.jobs.nav.Refresh();Plan="다음 연결도로 우회";}
  else Plan=Course.id=="PS09"?(choice==0?"신호·보행자 확인 후 판단":"직접 운전"):(choice==0?"줄 뒤에서 대기":choice==2?"앞부분 진입 가능 여부 검토":"직접 운전");
  Close();session.business.SaveCheckpoint();return true;
 }
 void Close(){if(!Open)return;Open=false;closedFrame=Time.frameCount;session.paused=previousPause;Time.timeScale=previousScale;}
 void Update(){
  if(Course==null)return;if(session.jobs.trip.State!=TripState.OnTrip){Stop();return;}if(Open)return;
  var nav=session.jobs.nav;int required=alternate?Course.alternate:Course.connector;
  var zones=GetComponent<ServiceLawZones>();
  if((nav.PassedObservedEdge==required||!alternate&&Course.id=="PS08"&&zones.Completed)&&!Completed){Completed=true;if(alternate)zones.CompleteAlternative();releasedAt=Time.time;RecordCompletion();}
  if(Completed){if(observing){if(!session.paused&&Time.timeScale>0)ObserveRightTurn();if(MayReleaseCompletedObservation(pedestrianState,Time.time-releasedAt)){junction.scenarioSignal=false;junction.rightTurnObservation=null;observing=false;if(person)Destroy(person);}}return;}
  if(session.paused||Time.timeScale<=0||session.FullMapOpen||session.services.open||session.law.HistoryOpen||session.PracticeMenuOpen)return;
  if(observing)ObserveRightTurn();
  var route=nav.hdRoute;if(route==null)return;int k=route.edges.IndexOf(required);if(k<1)return;float error,progress=nav.ProjectVehicle(out error);float ahead=route.ends[k-1]-progress;
  if(!offered&&(ahead<250||Course.id=="PS08")){offered=true;if(ahead>Mathf.Max(90,session.taxi.body.velocity.magnitude*5)&&CanOfferSaved()){previousPause=session.paused;previousScale=Time.timeScale;session.paused=true;Time.timeScale=0;Open=true;panelsToday++;RecordExposure();return;}}
  if(Course.id=="PS09"&&ahead<110&&ahead>20&&!observing)PrepareRightTurn(route,k);
 }
 public static bool MayReleaseCompletedObservation(RightPracticeState state,float secondsSinceCompletion){return secondsSinceCompletion>4&&state!=null&&state.Observe().pedestrian==PedestrianState.None&&state.CrossingProgress>=1;}
 void PrepareRightTurn(HdRoadGraph.RoutePath route,int k){
  var hd=session.jobs.nav.hd;junction=session.traffic.junctions.Find(j=>j.name==hd.edges[Course.connector].ruleId);if(junction==null)return;
  direction=hd.paths[route.edges[k-1]].Tangent(hd.paths[route.edges[k-1]].length-2);axis=Mathf.Abs(Vector3.Dot(direction,session.traffic.AxisDirection(junction,0)))>.7f?0:1;sign=session.traffic.Sign(direction,axis,junction);direction=session.traffic.AxisDirection(junction,axis)*sign;
  // Never replace an existing fixture or place a person through a live vehicle.
  if(GetComponent<RightTurnSituation>().Active||GetComponent<ExitQueueSituation>().Active||session.ambientPedestrians&&session.ambientPedestrians.CrossingOccupied(junction))return;
  var point=CrossingPoint(.35f);foreach(var car in session.traffic.cars)if(car.Visible&&Vector3.Distance(car.transform.position,point)<8)return;
  pedestrianState=new RightPracticeState(6);CreatePedestrian();observing=true;ObserveRightTurn();
 }
 void CreatePedestrian(){person=GameObject.CreatePrimitive(PrimitiveType.Capsule);person.name="PS09 횡단 보행자 · 창작";person.transform.localScale=new Vector3(.6f,.85f,.6f);Destroy(person.GetComponent<Collider>());person.GetComponent<Renderer>().material.color=new Color(.95f,.45f,.1f);}
 Vector3 CrossingPoint(float t){return junction.center+TrafficSystem.Right(direction)*(junction.halfWidth[axis]+4)+direction*Mathf.Lerp(-junction.halfWidth[1-axis]-1,junction.halfWidth[1-axis]+1,t)+Vector3.up*.85f;}
 void ObserveRightTurn(bool advance=true){var front=session.taxi.transform.position+session.taxi.transform.forward*2;float gap=-junction.StopDistance(axis)-Vector3.Dot(front-junction.center,direction);bool stopped=gap>=-TrafficJunction.StopLineEntryTolerance&&gap<3&&session.taxi.body.velocity.magnitude<.05f&&Vector3.Dot(session.taxi.transform.forward,direction)>.8f;if(advance)pedestrianState.Advance(Time.deltaTime,stopped||Completed);var o=pedestrianState.Observe();o.forward=SignalColor.Red;junction.scenarioSignal=true;junction.observationAxis=axis;junction.observationSign=sign;junction.scenarioForward=o.forward;junction.rightTurnObservation=o;person.SetActive(o.pedestrian!=PedestrianState.None);person.transform.position=CrossingPoint(pedestrianState.CrossingProgress);}
 void RecordCompletion(){var e=new LawEvent{recoveryPolicyVersion=1,id=session.law.ids.Next("course"),day=session.law.Day,tripId=session.law.tripId,scenario=Course.id,family="코스 계획",occupied=true,seconds=(float)session.business.ledger.Elapsed,action=Plan,reason=alternate?"실제 대체 도로 통과 · 원래 출구 합류는 평가 기회 없음":"지정 연결 통과. 선택 자체는 채점하지 않으며 실제 정지·양보 평가는 별도 운행 기록에 남습니다."};e.Add("계획 선택",alternate?LawVerdict.NoOpportunity:LawVerdict.Deferred,0);session.law.ledger.Record(e);}
 void OnGUI(){if(!Open||Course==null)return;float w=Mathf.Min(650,Screen.width-30),h=Course.id=="PS08"?330:240,x=(Screen.width-w)/2,y=(Screen.height-h)/2;GUI.Box(new Rect(x,y,w,h),"");var style=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=18,wordWrap=true};var button=new GUIStyle(GUI.skin.button){font=KoreanText.Font,fontSize=16};GUI.Label(new Rect(x+20,y+15,w-40,95),Course.id=="PS08"?"출구 앞 대기열 · 운전 계획\n줄 뒤 대기 또는 연결도로 우회를 선택하세요. 선택만으로 점수를 매기지 않습니다.":"운행 중 우회전 · 신호와 보행자\n정지선에서 멈추고 횡단 중인 보행자가 지난 뒤 주변 교통을 확인하세요. 직접 운전합니다.",style);
  string[] choices=Course.id=="PS08"?new[]{"줄 뒤에서 대기","다음 연결도로로 우회","앞부분 진입을 검토","직접 운전"}:new[]{"신호·보행자를 확인한 뒤 판단","직접 운전"};for(int i=0;i<choices.Length;i++){GUI.enabled=!(i==1&&Course.id=="PS08")||CanDetour();if(GUI.Button(new Rect(x+20,y+115+i*45,w-40,36),choices[i],button))Select(i);}GUI.enabled=true;
 }
}
