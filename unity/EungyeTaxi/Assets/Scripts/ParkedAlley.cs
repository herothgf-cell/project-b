using System;
using System.Collections.Generic;
using UnityEngine;

// Alley parking (design 08 ch.5.6 item 2.3, Eungye single, decided 2026-11-24). Parked cars along one kerb of the
// alleys listed in Resources/eungye-parked-cars.json (은행로144번길, where a roadside parking row was seen on the
// street view). A game staging: count and positions are not a survey. Ordinary traffic does not use these alleys,
// so nobody queues behind a parked car; the player squeezes past on the own side of the centre line.
public static class ParkedAlleyRules {
 [Serializable] public sealed class Road {public string name,kerb,evidence;public bool excludeTraffic;}
 [Serializable] sealed class File {public int schema;public string mapId,note;public Road[] roads;}
 static Road[] roads;static bool loaded;
 static float F(string id,double fallback){return (float)ContentTuning.Number(id,fallback);}
 static int I(string id,int fallback){return ContentTuning.Int(id,fallback);}
 public static bool Enabled {get{return I("road.parked.enabled",1)!=0;}}
 public static int GroupMin {get{return Mathf.Clamp(I("road.parked.groupMin",3),1,8);}}
 public static int GroupMax {get{return Mathf.Clamp(I("road.parked.groupMax",5),GroupMin,10);}}
 public static float Gap {get{return Mathf.Clamp(F("road.parked.gapMetres",6.5),5,12);}}
 public static float GroupSpacing {get{return Mathf.Clamp(F("road.parked.groupSpacingMetres",20),8,200);}}
 public static float ClearMetres {get{return Mathf.Clamp(F("road.parked.clearMetres",15),5,60);}}
 public static float NightMultiplier {get{return Mathf.Clamp(F("road.parked.nightMultiplier",1.5),1,3);}}
 // The taxi (half width .9) must pass a parked car on its own side of the centre line with this margin.
 public static float PassMargin {get{return Mathf.Clamp(F("road.parked.passMarginMetres",.3),.1f,1);}}
 public static Road[] Roads {get{if(!loaded){loaded=true;var asset=Resources.Load<TextAsset>("eungye-parked-cars");Configure(asset?asset.text:null);}return roads??new Road[0];}}
 public static void Configure(string json){loaded=true;roads=null;if(string.IsNullOrEmpty(json))return;var f=JsonUtility.FromJson<File>(json);if(f!=null&&f.schema==1&&f.roads!=null)roads=f.roads;}
 public static Road For(string roadName){if(string.IsNullOrEmpty(roadName))return null;foreach(var r in Roads)if(r!=null&&r.name==roadName)return r;return null;}
 // Ordinary traffic never routes through an alley that has parked cars.
 public static bool ExcludesTraffic(string roadName){var r=For(roadName);return Enabled&&r!=null&&r.excludeTraffic;}
 // Group sizes and positions along one lane: groups of GroupMin..GroupMax cars Gap apart, GroupSpacing between
 // groups. Pure and deterministic per lane id; positions that fail the site checks are skipped by the caller.
 public static List<float> Slots(string laneId,float from,float to,float multiplier){
  var list=new List<float>();uint h=Hash(laneId);float at=from;int group=0;
  while(at<to){
   int size=GroupMin+(int)((h>>(group%8*4))%(uint)(GroupMax-GroupMin+1));size=Mathf.Max(1,Mathf.RoundToInt(size*Mathf.Max(1,multiplier)));
   for(int i=0;i<size&&at<=to;i++){list.Add(at);at+=Gap;}
   at+=GroupSpacing-Gap;group++;
  }
  return list;
 }
 static uint Hash(string s){uint h=2166136261;foreach(char c in s??""){h^=c;h*=16777619;}return h;}
 // How far the taxi centre must sit from the lane centre (toward the centre line) to pass a car whose inner
 // side is `inner` metres from the lane centre; 0 when the lane centre already clears it.
 public static float Shift(float inner){return Mathf.Max(0,.9f+PassMargin-inner);}
 // Placement is allowed when that shift keeps the taxi centre on its own side of the centre line (half the
 // distance between the two lane centres), with half a metre to spare.
 public static bool Passable(float inner,float centreLine){return inner>-centreLine&&Shift(inner)<=Mathf.Max(0,centreLine-.5f);}
}

public sealed class ParkedAlley : MonoBehaviour {
 public DriveSession session;
 public sealed class Car {public GameObject body;public Vector3 position,direction;public string lane,road;public float inner,centreLine,at;}
 public readonly List<Car> cars=new List<Car>();
 public readonly List<string> skipped=new List<string>();
 bool evening,built;
 public void Initialize(DriveSession owner){session=owner;}
 bool Single {get{return session&&session.traffic&&session.traffic.EungyeSingle&&!session.simulationMatch&&!session.IsSimulationBot&&session.world&&session.world.map.hybrid&&session.jobs&&session.jobs.nav&&session.jobs.nav.hd!=null;}}
 void Update(){
  if(!session||!session.StartupReady)return;
  bool night=session.business&&session.business.EveningDrive;
  if(!built||night!=evening){evening=night;built=true;Build(night?ParkedAlleyRules.NightMultiplier:1);}
 }
 public void Clear(){foreach(var c in cars)if(c.body){c.body.SetActive(false);Destroy(c.body);}cars.Clear();skipped.Clear();}
 public void Build(float multiplier){
  Clear();if(!Single||!ParkedAlleyRules.Enabled)return;
  var graph=session.jobs.nav.hd;var map=session.world.map;
  foreach(var road in ParkedAlleyRules.Roads){
   // One kerb: lane edges heading the same way as the road's first lane edge.
   Vector3 reference=Vector3.zero;
   for(int i=0;i<graph.edges.Length;i++){var e=graph.edges[i];if(e.roadName!=road.name||e.change!=0||e.turn!=0||e.TraversalDisabled||e.id.StartsWith("join:"))continue;
    var path=graph.paths[i];if(path.length<12)continue;
    var heading=path.Position(path.length)-path.Position(0);heading.y=0;heading.Normalize();
    if(reference==Vector3.zero)reference=heading;
    if(road.kerb=="one-side"&&Vector3.Dot(heading,reference)<0)continue;
    float other=OppositeLaneDistance(graph,i,path.Position(path.length*.5f),path.Tangent(path.length*.5f));
    foreach(float at in ParkedAlleyRules.Slots(e.id,4,path.length-4,multiplier))Place(road,e.id,path,at,other,map);
   }
  }
  Physics.SyncTransforms();
  Debug.Log("PARKED_ALLEY cars="+cars.Count+" skipped="+skipped.Count+" multiplier="+multiplier.ToString("0.0"));
  foreach(var line in skipped)Debug.Log("PARKED_ALLEY skip "+line);
 }
 // Half the distance to the nearest opposite-direction lane centre (searched up to 9 m) = where the centre line
 // is. Defaults to 1.75 when no opposite lane is found.
 static float OppositeLaneDistance(HdRoadGraph graph,int self,Vector3 p,Vector3 tangent){
  float best=9f;var right=TrafficSystem.Right(tangent);
  for(int k=0;k<graph.edges.Length;k++){if(k==self||graph.edges[k].change!=0)continue;var b=graph.PathBounds(k);if(b.SqrDistance(p)>100)continue;
   float err;float at=graph.paths[k].Project(p,out err);if(err>9||Vector3.Dot(graph.paths[k].Tangent(at),tangent)>-.8f)continue;
   var d=graph.paths[k].Position(at)-p;d.y=0;float across=-Vector3.Dot(d,right);if(across>.5f&&across<best)best=across;}
  return best>=9f?1.75f:best*.5f;
 }
 void Place(ParkedAlleyRules.Road road,string lane,AssistedRoadNetwork.Path path,float at,float centreLine,MapData map){
  var p=path.Position(at);var dir=path.Tangent(at);dir.y=0;if(dir.sqrMagnitude<.5f)return;dir.Normalize();var right=TrafficSystem.Right(dir);string where=lane+"@"+at.ToString("0");
  if(Vector3.Angle(path.Tangent(Mathf.Max(0,at-3)),path.Tangent(Mathf.Min(path.length,at+3)))>8){skipped.Add(where+" bend");return;}
  float clear=ParkedAlleyRules.ClearMetres;
  foreach(var j in map.targets)if(Vector3.Distance(p,j.V())<clear+10){skipped.Add(where+" junction");return;}
  if(map.junctionRules!=null)foreach(var r in map.junctionRules)if(Vector3.Distance(p,r.V())<clear+10){skipped.Add(where+" junction rule");return;}
  foreach(var s in map.stops)if(Vector3.Distance(p,s.Kerb)<clear){skipped.Add(where+" stop");return;}
  if(map.osmCrossings!=null)foreach(var c in map.osmCrossings)if(Vector3.Distance(p,new Vector3(c.x,0,c.z))<clear){skipped.Add(where+" crossing");return;}
  // Hug the kerb: measure the paved road to the right (narrowest of three samples) and stand .2 m inside it.
  // In the game map the alley lanes lie close to the kerb, so the car may cover part of the lane; ordinary traffic
  // does not use the alley and the taxi passes on its own side of the centre line.
  float paved=8;foreach(float z in new[]{-2.25f,0,2.25f}){var q=p+dir*z;float r=0;while(r<8&&session.roadEnvironment.OnRoad(q+right*(r+.1f),0))r+=.1f;paved=Mathf.Min(paved,r);}
  float offset=paved-1.1f;
  if(offset<-.5f){skipped.Add(where+" no kerb room (paved "+paved.ToString("0.0")+" m right)");return;}
  float inner=offset-.9f;
  if(!ParkedAlleyRules.Passable(inner,centreLine)){skipped.Add(where+" too narrow (inner "+inner.ToString("0.00")+", centre line "+centreLine.ToString("0.00")+")");return;}
  var spot=p+right*offset;spot.y=p.y;
  if(Physics.CheckBox(spot+Vector3.up*.95f,new Vector3(1,.45f,2.4f),Quaternion.LookRotation(dir),(1<<8)|(1<<9)|1,QueryTriggerInteraction.Ignore)){skipped.Add(where+" occupied");return;}
  foreach(var c in cars)if(Vector3.Distance(c.position,spot)<4.8f){skipped.Add(where+" overlaps");return;}
  var car=new Car{position=spot,direction=dir,lane=lane,road=road.name,inner=inner,centreLine=centreLine,at=at};
  car.body=Build(spot,dir,cars.Count);cars.Add(car);
 }
 static readonly Color[] paints={new Color(.86f,.86f,.84f),new Color(.12f,.13f,.15f),new Color(.55f,.57f,.6f),new Color(.2f,.27f,.45f),new Color(.6f,.12f,.1f),new Color(.9f,.9f,.92f)};
 GameObject Build(Vector3 p,Vector3 dir,int index){
  var root=new GameObject("골목 주차 차량 (게임 연출)");root.transform.SetParent(transform);root.transform.SetPositionAndRotation(p,Quaternion.LookRotation(dir));
  bool truck=index%4==3;var paint=WorldBuilder.Mat("골목 주차 도색 "+index%paints.Length,paints[index%paints.Length]);var glass=WorldBuilder.Mat("골목 주차 유리",new Color(.08f,.15f,.19f));var tyre=WorldBuilder.Mat("AI tyres",new Color(.035f,.04f,.05f));
  var box=root.AddComponent<BoxCollider>();
  if(truck){ // 1-tonne truck: cab in front, open cargo bed behind
   box.center=new Vector3(0,.95f,0);box.size=new Vector3(1.75f,1.7f,4.7f);
   WorldBuilder.Box("적재함",new Vector3(0,.75f,-.75f),new Vector3(1.75f,.5f,3.1f),WorldBuilder.Mat("적재함",new Color(.42f,.44f,.46f)),root.transform,false);
   WorldBuilder.Box("운전석",new Vector3(0,1.15f,1.55f),new Vector3(1.7f,1.25f,1.5f),paint,root.transform,false);
   WorldBuilder.Box("앞유리",new Vector3(0,1.45f,2.31f),new Vector3(1.5f,.5f,.03f),glass,root.transform,false);
  }else{
   box.center=new Vector3(0,.8f,0);box.size=new Vector3(1.8f,1.25f,4.4f);
   WorldBuilder.Box("차체",new Vector3(0,.65f,0),new Vector3(1.8f,.65f,4.4f),paint,root.transform,false);
   WorldBuilder.Box("창문",new Vector3(0,1.2f,-.2f),new Vector3(1.5f,.62f,2.1f),glass,root.transform,false);
   WorldBuilder.Box("지붕",new Vector3(0,1.55f,-.2f),new Vector3(1.55f,.1f,2.2f),paint,root.transform,false);
  }
  for(int i=0;i<4;i++){var w=GameObject.CreatePrimitive(PrimitiveType.Cylinder);w.transform.SetParent(root.transform,false);w.transform.localPosition=new Vector3(i%2==0?-.86f:.86f,.34f,i<2?1.4f:-1.4f);w.transform.localRotation=Quaternion.Euler(0,0,90);w.transform.localScale=new Vector3(.68f,.12f,.68f);w.GetComponent<Renderer>().sharedMaterial=tyre;Destroy(w.GetComponent<Collider>());}
  // Play feedback 2026-11-26: the parking was not readable as parking. Each car stands in a painted white bay
  // (2.3 x 5.4 m outline on the road surface), so the row reads as an on-street parking zone.
  var paintLine=WorldBuilder.Mat("골목 주차 구획선",new Color(.92f,.92f,.9f));
  WorldBuilder.Box("주차 구획선",new Vector3(-1.15f,.085f,0),new Vector3(.12f,.01f,5.4f),paintLine,root.transform,false);
  WorldBuilder.Box("주차 구획선",new Vector3(1.15f,.085f,0),new Vector3(.12f,.01f,5.4f),paintLine,root.transform,false);
  WorldBuilder.Box("주차 구획선",new Vector3(0,.085f,2.7f),new Vector3(2.42f,.01f,.12f),paintLine,root.transform,false);
  WorldBuilder.Box("주차 구획선",new Vector3(0,.085f,-2.7f),new Vector3(2.42f,.01f,.12f),paintLine,root.transform,false);
  foreach(var child in root.GetComponentsInChildren<Transform>())child.gameObject.layer=9;
  return root;
 }
 void OnDestroy(){Clear();}
}
