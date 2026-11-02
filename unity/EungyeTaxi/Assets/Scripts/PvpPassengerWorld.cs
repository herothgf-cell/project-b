using System.Collections.Generic;
using UnityEngine;
// One authored bay obstruction, observed by every driver; never campaign state.
public sealed class PvpPassengerWorld:MonoBehaviour {
 PvpSimulationMatch match;StopOccupancyPolicy policy=new StopOccupancyPolicy();
 readonly HashSet<string> protectedTrips=new HashSet<string>();GameObject car;MapStop stop;int index=-1,sequence;float nextAt=60;
 public int ProtectedCount {get{return protectedTrips.Count;}}
 public int BlockedStop {get{return policy.Blocking?index:-1;}}
 public StopOccupancySnapshot State {get{return policy.Capture();}}
 public void Initialize(PvpSimulationMatch owner){match=owner;}
 public void ApplyNetworkView(StopOccupancySnapshot state,int pickup){RemoveCar();policy=StopOccupancyPolicy.Restore(state);index=pickup;if(state!=null&&state.active&&pickup>=0&&pickup<match.Human.jobs.stops.Length){stop=match.Human.jobs.stops[pickup];car=GameObject.CreatePrimitive(PrimitiveType.Cube);car.transform.SetParent(transform);car.transform.SetPositionAndRotation(stop.Kerb+stop.Direction*state.exitMetres+Vector3.up*.8f,Quaternion.LookRotation(stop.Direction));car.transform.localScale=new Vector3(1.8f,1.35f,4.3f);car.GetComponent<Renderer>().sharedMaterial=WorldBuilder.Mat("PVP bay obstruction",new Color(.32f,.39f,.46f));}}
 public bool IsOccupied(int pickup){return policy.Blocking&&pickup==index;}
 public string Status(DriveSession driver){return Affects(driver)?"공유 승하차 공간 점유 · "+Mathf.CeilToInt(policy.Capture().remaining)+"초 대기 후 출차 · 기한 보호1회":"";}
 bool Affects(DriveSession d){return d&&d.jobs.Active&&d.jobs.current!=null&&IsOccupied(d.jobs.trip.State==TripState.ToPickup?d.jobs.current.pickup:d.jobs.PassengerViaPending?d.jobs.PassengerViaStop:d.jobs.current.drop);}
 bool Visible(Vector3 p){foreach(var camera in Camera.allCameras){if(!camera.enabled)continue;var v=camera.WorldToViewportPoint(p+Vector3.up);if(v.z>0&&v.z<camera.farClipPlane&&v.x>-.05f&&v.x<1.05f&&v.y>-.05f&&v.y<1.05f)return true;}return false;}
 bool Clear(Vector3 p,bool spawning){
  foreach(var d in match.Drivers)if(Vector3.Distance(p,d.taxi.transform.position)<(spawning?100:4.8f))return false;
  foreach(var c in Physics.OverlapBox(p+Vector3.up*.8f,new Vector3(1.05f,.65f,2.5f),Quaternion.LookRotation(stop.Direction),~0,QueryTriggerInteraction.Ignore)){
   if(car&&c.transform.IsChildOf(car.transform))continue;
   if(c.bounds.max.y>p.y+.5f)return false;
  }return true;
 }
 public bool TryBegin(int pickup){
  if(!match||match.Finished||policy.Active||match.Human.world.map.mapId!="pvp-city-a"||pickup<0||pickup>=match.Human.jobs.stops.Length)return false;
  stop=match.Human.jobs.stops[pickup];if(!stop.graphStop||!stop.hasPassengerAnchor||Visible(stop.Kerb)||!Clear(stop.Kerb,true)||!Clear(stop.Kerb+stop.Direction*7,true))return false;
  if(!policy.Begin("pvp-bay:"+(++sequence),"S"+(pickup+1).ToString("00")))return false;
  index=pickup;protectedTrips.Clear();car=GameObject.CreatePrimitive(PrimitiveType.Cube);car.name="PVP 공유 임시 정차 차량";car.layer=9;car.transform.SetParent(transform);car.transform.SetPositionAndRotation(stop.Kerb+Vector3.up*.8f,Quaternion.LookRotation(stop.Direction));car.transform.localScale=new Vector3(1.8f,1.35f,4.3f);car.GetComponent<Renderer>().sharedMaterial=WorldBuilder.Mat("PVP bay obstruction",new Color(.32f,.39f,.46f));ProtectAll();return true;
 }
 void ProtectAll(){foreach(var d in match.Drivers){if(!Affects(d))continue;string key=d.SimulationDriver+":"+(string.IsNullOrEmpty(d.jobs.OfferId)?d.law.tripId:d.jobs.OfferId);if(protectedTrips.Contains(key))continue;float extra=policy.Protection();if(extra<=0||!d.jobs.trip.ExtendForSystemDelay(extra))continue;protectedTrips.Add(key);if(d.jobs.trip.State==TripState.OnTrip){d.jobs.current.seconds+=extra;d.requests.ExemptSystemRoute("공유 공간 점유 "+policy.Capture().eventId);}d.jobs.message=Status(d);}}
 void FixedUpdate(){Advance(Time.fixedDeltaTime);}
 public void Advance(float seconds){if(!match||match.Finished||Time.timeScale<=0||seconds<=0)return;
  if(policy.Active){ProtectAll();var old=policy.Capture();bool clear=Clear(stop.Kerb+stop.Direction*Mathf.Min(7,old.exitMetres+1),false);policy.Advance(seconds,clear);if(car)car.transform.position=stop.Kerb+stop.Direction*policy.Capture().exitMetres+Vector3.up*.8f;
   if(!policy.Blocking&&!Visible(car.transform.position)&&Clear(car.transform.position,false)){RemoveCar();policy.Removed();nextAt=match.Elapsed+120;}return;
  }
  if(match.Elapsed<nextAt)return;nextAt=match.Elapsed+30;for(int n=0;n<match.Human.jobs.stops.Length;n++)if(TryBegin((sequence+n)%match.Human.jobs.stops.Length))break;
 }
 void RemoveCar(){if(!car)return;if(Application.isPlaying)Destroy(car);else DestroyImmediate(car);car=null;}
 void OnDestroy(){RemoveCar();}
}
