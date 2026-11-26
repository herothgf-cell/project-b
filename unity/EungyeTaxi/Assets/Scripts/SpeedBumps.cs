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
 // Korean-style high hump (play feedback 2026-11-26): game-only, on residential 번길 side roads. Tall and short, so it
 // jolts already at town speed; the passenger minds it above its own lower comfort speed.
 public static int KoreanCount {get{return Mathf.Clamp(ContentTuning.Int("bump.korean.count",6),0,20);}}
 public static float KoreanComfortKmh {get{return Mathf.Clamp(F("bump.korean.comfortKmh",15),5,60);}}
 public static int KoreanPenalty {get{return Mathf.Clamp(ContentTuning.Int("bump.korean.penalty",1),0,20);}}
 public static float KoreanBounce(float kmh){return Jolt*Mathf.Clamp01((kmh-6)/24f)*1.7f;}
 public static float KoreanSpacing {get{return 150;}}
}

public sealed class SpeedBumps : MonoBehaviour {
 public DriveSession session;
 public sealed class Bump {public string id;public Vector3 a,b,centre,along;public float halfLength,depth;public bool korean;}
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
 bool koreanBuilt;public string KoreanStatus="not built";
 // Places the game-only Korean humps once the lane graph exists: middle of long straight 번길 lane edges, away from
 // junctions, stops and crossings, one per road position (both directions share it), spread out.
 void BuildKorean(){
  koreanBuilt=true;KoreanStatus="single="+Single+" enabled="+SpeedBumpRules.Enabled+" count="+SpeedBumpRules.KoreanCount+" map="+(session.world?session.world.map.mapId:"-");if(!Single||!SpeedBumpRules.Enabled||SpeedBumpRules.KoreanCount<=0||session.world==null||session.world.map.mapId!="eungye")return;
  var graph=session.jobs.nav.hd;var map=session.world.map;var env=session.roadEnvironment;var candidates=new List<Bump>();int streets=0;
  for(int i=0;i<graph.edges.Length;i++){var e=graph.edges[i];if(e.change!=0||e.turn!=0||e.TraversalDisabled||string.IsNullOrEmpty(e.roadName)||!e.roadName.EndsWith("번길")||e.id.StartsWith("join:"))continue;streets++;
   var path=graph.paths[i];if(path.length<45)continue;float at=path.length*.5f;var p=path.Position(at);var dir=path.Tangent(at);dir.y=0;if(dir.sqrMagnitude<.5f)continue;dir.Normalize();
   if(Vector3.Angle(path.Tangent(at-8),path.Tangent(at+8))>6)continue;
   bool near=false;foreach(var j in map.targets)if(Vector3.Distance(p,j.V())<28){near=true;break;}
   if(!near)foreach(var st in map.stops)if(Vector3.Distance(p,st.Kerb)<25){near=true;break;}
   if(!near&&map.osmCrossings!=null)foreach(var c in map.osmCrossings)if(Vector3.Distance(p,new Vector3(c.x,0,c.z))<20){near=true;break;}
   if(!near)foreach(var b in bumps)if(Vector3.Distance(p,b.centre)<SpeedBumpRules.KoreanSpacing){near=true;break;}
   if(near)continue;
   var right=TrafficSystem.Right(dir);float l=0,r=0;if(env!=null){while(l<8&&env.OnRoad(p-right*(l+.1f),0))l+=.1f;while(r<8&&env.OnRoad(p+right*(r+.1f),0))r+=.1f;}else{l=r=3;}
   if(l+r<3)continue;var a=p-right*l;var bb=p+right*r;
   candidates.Add(new Bump{id="korean:"+e.roadName+"@"+Mathf.RoundToInt(p.x)+","+Mathf.RoundToInt(p.z),a=a,b=bb,centre=(a+bb)*.5f,along=dir,halfLength=(l+r)*.5f,depth=3.6f,korean=true});}
  // Deterministic spread: take candidates in id order, keeping the spacing among the chosen ones.
  candidates.Sort((x,y)=>string.CompareOrdinal(x.id,y.id));int added=0;
  foreach(var c in candidates){if(added>=SpeedBumpRules.KoreanCount)break;bool close=false;foreach(var b in bumps)if(Vector3.Distance(c.centre,b.centre)<SpeedBumpRules.KoreanSpacing){close=true;break;}if(close)continue;bumps.Add(c);Draw(c);added++;}
  KoreanStatus+=" placed "+added+" of "+candidates.Count+" (번길 edges "+streets+")";
  Debug.Log("SPEED_BUMP korean humps "+added+" of "+candidates.Count+" candidates");
 }
 // Yellow hump with white diagonal stripes, 3.6 m along the road, no collider.
 void Draw(Bump b){
  var root=new GameObject("과속방지턱 "+b.id);root.transform.SetParent(transform);var across=b.b-b.a;across.y=0;
  root.transform.SetPositionAndRotation(new Vector3(b.centre.x,b.centre.y,b.centre.z),Quaternion.LookRotation(b.along));
  float width=across.magnitude;var yellow=WorldBuilder.Mat("한국형 방지턱 노랑",new Color(.95f,.75f,.08f));var white=WorldBuilder.Mat("한국형 방지턱 흰색",new Color(.95f,.95f,.93f));var asphalt=WorldBuilder.Mat("한국형 방지턱 아스팔트",new Color(.2f,.2f,.22f));
  WorldBuilder.Box("방지턱 몸체",new Vector3(0,.05f,0),new Vector3(width,.1f,3.6f),asphalt,root.transform,false);
  WorldBuilder.Box("방지턱 꼭대기",new Vector3(0,.12f,0),new Vector3(width,.04f,1.6f),asphalt,root.transform,false);
  int k=0;for(float x=-width*.5f+.7f;x<width*.5f-.6f;x+=.9f,k++){var stripe=WorldBuilder.Box("방지턱 사선",new Vector3(x,.145f,0),new Vector3(.4f,.012f,2.2f),k%2==0?yellow:white,root.transform,false);stripe.transform.localRotation=Quaternion.Euler(0,45,0);}
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
 // Update, not FixedUpdate: the start-up screen pauses time, and the humps must exist before the first drive.
 void Update(){if(!koreanBuilt&&session&&session.StartupReady&&session.jobs&&session.jobs.nav&&session.jobs.nav.hd!=null)BuildKorean();}
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
  float up=bump.korean?SpeedBumpRules.KoreanBounce(kmh):SpeedBumpRules.Bounce(kmh);if(up>0){var v=session.taxi.body.velocity;v.y=Mathf.Max(v.y,0)+up;session.taxi.body.velocity=v;if(bump.korean)session.taxi.body.angularVelocity+=session.taxi.transform.right*(-up*.9f);}
  var n=session.GetComponent<DriveNotifications>();
  if(bump.korean){KoreanCross(kmh,n);Debug.Log("SPEED_BUMP crossed "+bump.id+" kmh="+kmh.ToString("0")+" carrying="+Carrying);return;}
  if(SpeedBumpRules.Penalised(kmh,Carrying)&&session.manners){
   if(session.manners.Deduct("bump",SpeedBumpRules.Penalty,"방지턱 과속 · 손님이 흔들렸어요",SpeedBumpRules.CooldownSeconds)){Penalties++;if(n)n.Card("방지턱 · "+Mathf.RoundToInt(kmh)+"km/h · “아이고, 허리야…”");}
  }else if(Carrying&&kmh<=SpeedBumpRules.ComfortKmh-5){string trip=session.law?session.law.tripId:"";if(trip!=praiseTrip){praiseTrip=trip;if(n)n.Card("방지턱 · 천천히 넘음 · “부드럽게 가시네요.”");}}
  Debug.Log("SPEED_BUMP crossed "+bump.id+" kmh="+kmh.ToString("0")+" carrying="+Carrying);
 }
 public int KoreanCrossings,KoreanPenalties;
 void KoreanCross(float kmh,DriveNotifications n){
  KoreanCrossings++;
  if(Carrying&&kmh>SpeedBumpRules.KoreanComfortKmh&&session.manners){if(session.manners.Deduct("bump",SpeedBumpRules.KoreanPenalty,"덜컹 · 높은 방지턱을 빨리 넘음",SpeedBumpRules.CooldownSeconds)){KoreanPenalties++;praiseTrip=session.law?session.law.tripId:"";if(n)n.Card("높은 방지턱 · 덜컹! · "+Mathf.RoundToInt(kmh)+"km/h · “어이쿠, 천천히 좀요.”");}}
  else if(Carrying&&kmh<=SpeedBumpRules.KoreanComfortKmh){string trip=session.law?session.law.tripId:"";if(trip!=praiseTrip){praiseTrip=trip;if(n)n.Card("높은 방지턱 · 살살 넘음 · “편하네요.”");}}
  else if(!Carrying&&kmh>SpeedBumpRules.KoreanComfortKmh+10&&n)n.Card("높은 방지턱 · 덜컹!");
 }
}
