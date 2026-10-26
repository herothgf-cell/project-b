using UnityEngine;

// Only catalog-validated fictional bays. The current map has no safe <=150m
// candidate pair: waiting is the shipped fallback, never sidewalk improvisation.
public sealed class StopOccupancySystem : MonoBehaviour {
 public DriveSession session;StopOccupancyPolicy policy=new StopOccupancyPolicy();GameObject car;
 int sequence;double nextAt=60,lastElapsed=-1;string attemptedTrip="";MapStop stop;
 public bool IsOccupied(string stopId){var s=policy.Capture();return policy.Blocking&&s.stopId==stopId;}
 public bool IsOccupied(int index){var e=session.jobs.Catalog.Entry(index);return e!=null&&IsOccupied(e.StopId);}
 public bool TryGetAlternative(string stopId,out MapStop alternative){alternative=null;return false;}
 public bool AffectsActive {get{var j=session.jobs;return j.Active&&j.current!=null&&IsOccupied(j.trip.State==TripState.ToPickup?j.current.pickup:j.PassengerViaPending?j.PassengerViaStop:j.current.drop);}}
 public string Status {get{var s=policy.Capture();return AffectsActive?"승하차 공간 일시 점유 · "+(s.remaining>0?Mathf.CeilToInt(s.remaining)+"초 후 이동":"차량이 앞쪽으로 이동 중")+"\n검증된 근거리 대체 지점 없음 · 원래 위치 유지 · 기한 보호1회":"";}}
 public bool Resolve(bool useAlternative){if(!AffectsActive||useAlternative||session.paused||session.taxi.body.velocity.magnitude>.1f)return false;Protect();session.jobs.message=Status;return true;}
 bool Visible(Vector3 p){foreach(var camera in Camera.allCameras){if(!camera.enabled)continue;var v=camera.WorldToViewportPoint(p+Vector3.up);if(v.z>0&&v.z<camera.farClipPlane&&v.x>-.05f&&v.x<1.05f&&v.y>-.05f&&v.y<1.05f)return true;}return false;}
 bool Clear(Vector3 p,bool newSpawn=false){
  var j=session.jobs;if(Vector3.Distance(p,session.taxi.transform.position)<(newSpawn?7:4.8f))return false;
  foreach(var c in session.traffic.cars)if(c&&c.Visible&&Vector3.Distance(p,c.transform.position)<7)return false;
  if(j.people!=null)foreach(var person in j.people)if(person&&person.activeInHierarchy&&Vector3.Distance(p,person.transform.position)<2)return false;
  foreach(var collider in Physics.OverlapBox(p+Vector3.up*.9f,new Vector3(1.05f,.55f,2.5f),Quaternion.LookRotation(stop.Direction),~0,QueryTriggerInteraction.Ignore))if(collider&&!(car&&collider.transform.IsChildOf(car.transform))&&collider.bounds.max.y>p.y+.5f)return false;
  return true;
 }
 bool Spawn(int index){
  var entry=session.jobs.Catalog.Entry(index);if(entry==null||!session.jobs.Catalog.IsAdditional(index)||entry.EvidenceStatus!="game-only-field-unverified")return false;
  var s=entry.Stop;if(Vector3.Distance(s.Kerb,session.taxi.transform.position)<100||Visible(s.Kerb))return false;
  stop=s;if(!Clear(s.Kerb,true)||!Clear(s.Kerb+s.Direction*7,true))return false;
  if(!policy.Begin("occupancy:"+session.law.ids.campaign+":"+(++sequence),entry.StopId))return false;
  MakeCar();Protect();session.jobs.RefreshWaitingPassengers();session.business.SaveCheckpoint();return true;
 }
 void MakeCar(){if(car||stop==null)return;car=GameObject.CreatePrimitive(PrimitiveType.Cube);car.name="일시 정차 차량 · "+policy.Capture().eventId;car.transform.SetPositionAndRotation(stop.Kerb+stop.Direction*policy.Capture().exitMetres+Vector3.up*.8f,Quaternion.LookRotation(stop.Direction));car.transform.localScale=new Vector3(1.8f,1.35f,4.3f);car.GetComponent<Renderer>().sharedMaterial=WorldBuilder.Mat("Occupancy car",new Color(.32f,.39f,.46f));var cabin=GameObject.CreatePrimitive(PrimitiveType.Cube);cabin.transform.SetParent(car.transform,false);cabin.transform.localPosition=new Vector3(0,.5f,-.08f);cabin.transform.localScale=new Vector3(.8f,.4f,.5f);cabin.GetComponent<Renderer>().sharedMaterial=WorldBuilder.Mat("Occupancy windows",new Color(.08f,.15f,.2f));if(Application.isPlaying)Destroy(cabin.GetComponent<Collider>());else DestroyImmediate(cabin.GetComponent<Collider>());}
 void Protect(){if(!AffectsActive)return;float extra=policy.Protection();if(extra<=0)return;if(session.jobs.trip.ExtendForSystemDelay(extra)){policy.Protected(session.law.tripId);if(session.jobs.trip.State==TripState.OnTrip){session.jobs.current.seconds+=extra;session.requests.ExemptSystemRoute("승하차 공간 점유 "+policy.Capture().eventId);}session.jobs.message=Status;session.business.SaveCheckpoint();}}
 void Update(){
  if(!session||!session.business||!session.business.enabledCampaign||!session.business.ledger.Operating||session.business.preparing||session.paused||session.PracticeBusy||PracticeSession.Enabled)return;
  double now=session.business.ledger.Elapsed;
  float delta=lastElapsed<0?0:(float)System.Math.Max(0,now-lastElapsed);lastElapsed=now;
  if(policy.Active){
   if(!car&&stop!=null&&Clear(stop.Kerb+stop.Direction*policy.Capture().exitMetres))MakeCar();
   Protect();var old=policy.Capture();bool clear=car&&Clear(stop.Kerb+stop.Direction*Mathf.Min(7,old.exitMetres+1));policy.Advance(delta,clear);
   var state=policy.Capture();if(car)car.transform.position=stop.Kerb+stop.Direction*state.exitMetres+Vector3.up*.8f;
   if(car&&!policy.Blocking&&!Visible(car.transform.position)&&Vector3.Distance(car.transform.position,session.taxi.transform.position)>50){Destroy(car);car=null;policy.Removed();nextAt=now+120;session.business.SaveCheckpoint();}
   return;
  }
  var j=session.jobs;if(j.Active&&j.current!=null){string key=session.law.tripId+":"+j.trip.State;if(key!=attemptedTrip){attemptedTrip=key;if(string.IsNullOrEmpty(j.current.scenarioId)&&DrunkRideState.Hash(j.current.candidateSeed+":occupancy")%100<35)Spawn(j.trip.State==TripState.ToPickup?j.current.pickup:j.current.drop);}}
  else if(now>=nextAt){nextAt=now+120;var entries=j.Catalog.Entries;for(int n=0;n<entries.Count;n++){int index=(n+sequence)%entries.Count;if(Spawn(index))break;}}
 }
 void OnGUI(){if(!AffectsActive||session.paused||session.FullMapOpen||session.ChoiceOpen)return;var prior=GUI.matrix;float scale=CockpitUI.Scale;GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float x=(Screen.width/scale-540)/2;var rect=new Rect(x,130,540,104);KoreanText.Panel(rect);GUI.Label(new Rect(x+12,138,516,58),Status,new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=14,wordWrap=true});GUI.enabled=session.taxi.body.velocity.magnitude<=.1f;if(GUI.Button(new Rect(x+12,199,516,28),"안전한 곳에서 대기 · 영업/연료 시간은 계속 흐름",KoreanText.Button(13)))Resolve(false);GUI.enabled=true;GUI.matrix=prior;}
 public StopOccupancySnapshot Capture(){var s=policy.Capture();s.sequence=sequence;s.nextAt=nextAt;s.attemptedTrip=attemptedTrip;return s;}
 public void Restore(StopOccupancySnapshot s){lastElapsed=session.business.ledger.Elapsed;policy=StopOccupancyPolicy.Restore(s);if(car){Destroy(car);car=null;}if(s==null||s.version==0)return;sequence=s.sequence;nextAt=s.nextAt;attemptedTrip=s.attemptedTrip;stop=null;if(policy.Active){int i=session.jobs.Catalog.IndexOf(s.stopId);if(!session.jobs.Catalog.IsAdditional(i))throw new System.ArgumentException("Saved occupancy bay unavailable");stop=session.jobs.stops[i];} }
 void OnDestroy(){if(car){if(Application.isPlaying)Destroy(car);else DestroyImmediate(car);}}
}
