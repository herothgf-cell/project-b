using System.Collections.Generic;
using UnityEngine;

// Speed bumps (design 08 item 7, 2026-11-24, Eungye single). Positions come from the road facility input
// (config/maps/eungye-road-facilities.json, kind "speed-bump": two field-survey points; the three NGII 수인로 side-road points
// were removed in play-test round 5 because that side road is not in the game map and they landed in a main-road lane).
// The bump is painted and slightly raised but has no collider, so it can never count as a property accident.
// Crossing it faster than the comfort speed with a passenger aboard costs ride-comfort points under its own
// key ("bump", not accel/brake, so the coffee mission is unaffected) and the passenger says so; any crossing
// gives the body a small bounce. Braking for a bump ahead counts as required braking.
public static class SpeedBumpRules {
 static float F(string id,double fallback){return (float)ContentTuning.Number(id,fallback);}
 public static bool Enabled {get{return ContentTuning.Int("bump.enabled",1)!=0;}}
 public static float ComfortKmh {get{return Mathf.Clamp(F("bump.comfortKmh",25),5,80);}}
 public static int Penalty {get{return Mathf.Clamp(ContentTuning.Int("bump.penalty",2),0,20);}}
 public static float CooldownSeconds {get{return Mathf.Clamp(F("bump.cooldownSeconds",6),0,60);}}
 public static float Jolt {get{return Mathf.Clamp(F("bump.jolt",1),0,3);}}
 public static float BrakeLookMetres {get{return Mathf.Clamp(F("bump.brakeLookMetres",15),3,60);}}
 // Penalty only over the comfort speed and only with a passenger aboard.
 public static bool Penalised(float kmh,bool carrying){return carrying&&kmh>ComfortKmh;}
 // Upward speed added to the body at the crossing: none at a crawl, up to ~1 m/s at 55 km/h.
 public static float Bounce(float kmh){return Jolt*Mathf.Clamp01((kmh-12)/43f)*1f;}
}

public sealed class SpeedBumps : MonoBehaviour {
 public DriveSession session;
 public sealed class Bump {public string id;public Vector3 a,b,centre,along;public float halfLength,depth;}
 public readonly List<Bump> bumps=new List<Bump>();
 readonly Dictionary<string,float> lastSide=new Dictionary<string,float>();
 public int Crossings,Penalties;public float LastKmh;public string LastBump="";bool praisedThisTrip;string praiseTrip="";
 public void Initialize(DriveSession owner){
  session=owner;bumps.Clear();
  var asset=Resources.Load<TextAsset>("eungye-road-facilities");if(!asset)return;
  var catalog=JsonUtility.FromJson<ReviewedRoadFacilities.Catalog>(asset.text);if(catalog==null||session.world==null||catalog.mapId!=session.world.map.mapId)return;
  foreach(var f in catalog.facilities??new ReviewedRoadFacilities.Facility[0])foreach(var s in f.segments??new ReviewedRoadFacilities.ExplicitSegment[0]){
   if(s.kind!="speed-bump"||s.points==null||s.points.Length<2)continue;var a=s.points[0].V();var b=s.points[s.points.Length-1].V();var across=b-a;across.y=0;if(across.sqrMagnitude<.01f)continue;
   var dir=across.normalized;bumps.Add(new Bump{id=f.id,a=a,b=b,centre=(a+b)*.5f,along=new Vector3(-dir.z,0,dir.x),halfLength=across.magnitude*.5f,depth=s.width});
  }
 }
 bool Single {get{return session&&session.traffic&&session.traffic.EungyeSingle&&!session.simulationMatch&&!session.IsSimulationBot&&!PvpNativeLan.IsReplica(session);}}
 bool Carrying {get{return session.jobs&&session.jobs.trip.State==TripState.OnTrip&&!session.jobs.trip.ArrivalLocked;}}
 // A bump within `metres` ahead of the taxi on its path (used by required braking).
 public bool Ahead(Vector3 position,Vector3 forward,float metres){
  if(!Single||!SpeedBumpRules.Enabled)return false;forward.y=0;if(forward.sqrMagnitude<.01f)return false;forward.Normalize();
  foreach(var bump in bumps){var rel=bump.centre-position;rel.y=0;float ahead=Vector3.Dot(rel,forward);if(ahead<0||ahead>Mathf.Min(metres,SpeedBumpRules.BrakeLookMetres))continue;
   float lateral=Mathf.Abs(Vector3.Dot(rel,Vector3.Cross(Vector3.up,bump.along)));if(lateral<=bump.halfLength+.5f&&Mathf.Abs(Vector3.Dot(forward,bump.along))>.5f)return true;}
  return false;
 }
 void FixedUpdate(){
  if(!Single||!SpeedBumpRules.Enabled||!session.taxi||session.paused)return;
  var t=session.taxi.transform;var front=t.position+t.forward*1.3f;
  foreach(var bump in bumps){
   var rel=front-bump.centre;rel.y=0;float side=Vector3.Dot(rel,bump.along);float lateral=Mathf.Abs(Vector3.Dot(rel,Vector3.Cross(Vector3.up,bump.along)));
   if(lateral>bump.halfLength+1||Mathf.Abs(side)>12){lastSide.Remove(bump.id);continue;}
   float before;if(lastSide.TryGetValue(bump.id,out before)&&Mathf.Sign(before)!=Mathf.Sign(side)&&before!=0)Cross(bump);
   lastSide[bump.id]=side;
  }
 }
 void Cross(Bump bump){
  float kmh=session.taxi.Speed*3.6f;Crossings++;LastKmh=kmh;LastBump=bump.id;
  float up=SpeedBumpRules.Bounce(kmh);if(up>0){var v=session.taxi.body.velocity;v.y=Mathf.Max(v.y,0)+up;session.taxi.body.velocity=v;}
  var n=session.GetComponent<DriveNotifications>();
  if(SpeedBumpRules.Penalised(kmh,Carrying)&&session.manners){
   if(session.manners.Deduct("bump",SpeedBumpRules.Penalty,"방지턱 과속 · 손님이 흔들렸어요",SpeedBumpRules.CooldownSeconds)){Penalties++;if(n)n.Card("방지턱 · "+Mathf.RoundToInt(kmh)+"km/h · “아이고, 허리야…”");}
  }else if(Carrying&&kmh<=SpeedBumpRules.ComfortKmh-5){string trip=session.law?session.law.tripId:"";if(trip!=praiseTrip){praiseTrip=trip;if(n)n.Card("방지턱 · 천천히 넘음 · “부드럽게 가시네요.”");}}
  Debug.Log("SPEED_BUMP crossed "+bump.id+" kmh="+kmh.ToString("0")+" carrying="+Carrying);
 }
}
