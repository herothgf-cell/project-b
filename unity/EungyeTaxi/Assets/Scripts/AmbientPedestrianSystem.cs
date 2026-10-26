using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public sealed class AmbientPedestrianState {
 public string id;
 public float progress,rest;
 public bool crossing,reverse,greenConsumed,contactLatched,deferredReveal;
}
[Serializable] public sealed class AmbientPedestrianSnapshot {public AmbientPedestrianState[] actors;}

public sealed class AmbientPedestrianSystem : MonoBehaviour {
 public DriveSession session;
 public AmbientPedestrianPlacement placement;
 public readonly List<AmbientPedestrian> actors=new List<AmbientPedestrian>();
 public string PlacementReport {get{return placement==null?null:string.Join("\n",placement.report.ToArray());}}
 public int SidewalkCount {get{int n=0;foreach(var a in actors)if(!a.route.crossing)n++;return n;}}
 public int CrossingCount {get{int n=0;foreach(var a in actors)if(a.route.crossing)n++;return n;}}
 public void Initialize(DriveSession owner){session=owner;placement=new AmbientPedestrianPlacement(owner.world.map,owner.traffic,owner.roadEnvironment);int index=0;foreach(var route in placement.routes){var go=new GameObject("일반 보행자 · "+route.id);go.transform.SetParent(transform);var actor=go.AddComponent<AmbientPedestrian>();actor.Initialize(this,route,index++);actors.Add(actor);}Debug.Log("AMBIENT_PEDESTRIANS placed="+actors.Count+" sidewalks="+SidewalkCount+" crossings="+CrossingCount+"\n"+PlacementReport);}
 public AmbientPedestrian At(string id){return actors.Find(a=>a.route.id==id);}
 public bool ScenarioOwns(TrafficJunction j){if(j==null)return false;var right=session.GetComponent<RightTurnSituation>();if(right&&right.Active&&right.junction==j)return true;var choice=session.GetComponent<ScenarioChoiceSituation>();return choice&&choice.ActiveJunction==j;}
 public bool CrossingOccupied(TrafficJunction j){foreach(var a in actors)if(a.route.junction==j&&a.Crossing)return true;return false;}
 public bool MayStartCrossing(AmbientPedestrian actor){if(ScenarioOwns(actor.route.junction))return false;var route=actor.route;var traffic=session.traffic;Vector3 from=actor.state.reverse?route.b:route.a,to=actor.state.reverse?route.a:route.b;var middle=(from+to)*.5f;
  foreach(var car in traffic.cars){if(!car||!car.Visible)continue;float near=DistanceToSegment(car.transform.position,from,to);if(near<5.5f)return false;for(float ahead=0;ahead<=Mathf.Max(8,car.speed*2);ahead+=2)if(DistanceToSegment(car.route.Position(Mathf.Min(car.route.length,car.distance+ahead)),from,to)<2.6f&&Vector3.Distance(car.transform.position,middle)<22)return false;}
  var taxi=session.taxi;if(taxi&&taxi.body){var body=taxi.GetComponent<BoxCollider>();if(body&&TaxiBodyNearSegment(body,from,to,5.5f))return false;}return true;
 }
 static bool TaxiBodyNearSegment(BoxCollider body,Vector3 a,Vector3 b,float margin){float length=Vector3.Distance(a,b);int steps=Mathf.Max(1,Mathf.CeilToInt(length/2));for(int i=0;i<=steps;i++){var point=Vector3.Lerp(a,b,i/(float)steps)+Vector3.up*.85f;if(Vector3.Distance(point,body.ClosestPoint(point))<margin)return true;}return false;}
 static float DistanceToSegment(Vector3 point,Vector3 a,Vector3 b){var d=b-a;d.y=0;point.y=a.y;var x=point-a;x.y=0;float t=Mathf.Clamp01(Vector3.Dot(x,d)/Mathf.Max(.001f,d.sqrMagnitude));return (x-d*t).magnitude;}
 // Returns the first route-local braking gap. This samples beyond a stop line too.
 float SegmentGap(TrafficCar car,float lookahead,Vector3 a,Vector3 b){if(Vector3.Distance(car.route.Position(car.distance),(a+b)*.5f)>lookahead+Vector3.Distance(a,b)*.5f+5)return float.PositiveInfinity;for(float ahead=0;ahead<=lookahead+4;ahead+=1){var p=car.route.Position(Mathf.Min(car.route.length,car.distance+ahead));if(Mathf.Abs(p.y-a.y)>2)continue;if(DistanceToSegment(p,a,b)<1.5f)return Mathf.Max(0,ahead-3);}return float.PositiveInfinity;}
 public float OccupiedCrossingGap(TrafficCar car,float lookahead){if(car==null||car.route==null)return float.PositiveInfinity;float best=float.PositiveInfinity;foreach(var actor in actors)if(actor.Crossing)best=Mathf.Min(best,SegmentGap(car,lookahead,actor.route.a,actor.route.b));Vector3 a,b;var right=session.GetComponent<RightTurnSituation>();if(right&&right.TryActiveCrossing(out a,out b))best=Mathf.Min(best,SegmentGap(car,lookahead,a,b));var choice=session.GetComponent<ScenarioChoiceSituation>();if(choice&&choice.TryActiveCrossing(out a,out b))best=Mathf.Min(best,SegmentGap(car,lookahead,a,b));return best;}
 public bool ObservedPedestrian(TrafficJunction j,Vector3 incoming,GameObject self){if(!CrossingOccupied(j))return false;incoming.y=0;incoming.Normalize();Vector3 start=j.center-incoming*28,end=j.center+incoming*28;if(OccupiedRoute(j,start,end))return true;var signal=self?self.GetComponent<TaxiSignals>():null;return signal&&signal.direction>0&&OccupiedRoute(j,j.center,j.center+TrafficSystem.Right(incoming)*28);}
 public bool OccupiedRoute(TrafficJunction j,Vector3 from,Vector3 to){foreach(var actor in actors)if(actor.Crossing&&actor.route.junction==j&&SegmentsNear(from,to,actor.route.a,actor.route.b,1.6f))return true;return false;}
 static bool SegmentsNear(Vector3 a,Vector3 b,Vector3 c,Vector3 d,float margin){for(int i=0;i<=10;i++)if(DistanceToSegment(Vector3.Lerp(a,b,i/10f),c,d)<margin)return true;return false;}
 public AmbientPedestrianSnapshot Capture(){ReconcileVisibility();var values=new AmbientPedestrianState[actors.Count];for(int i=0;i<actors.Count;i++)values[i]=actors[i].Capture();return new AmbientPedestrianSnapshot{actors=values};}
 public void Validate(AmbientPedestrianSnapshot saved){if(saved==null)return;if(saved.actors==null||saved.actors.Length!=actors.Count)throw new ArgumentException("Invalid ambient pedestrian count");var seen=new HashSet<string>();foreach(var value in saved.actors){var actor=value==null?null:At(value.id);if(actor==null||!seen.Add(value.id)||float.IsNaN(value.progress)||float.IsInfinity(value.progress)||value.progress<0||value.progress>1||float.IsNaN(value.rest)||float.IsInfinity(value.rest)||value.rest<0||value.rest>120||value.crossing&&!actor.route.crossing||actor.route.crossing&&(!value.crossing&&value.progress>.0001f||value.crossing&&(value.progress<=0||value.progress>=1||value.rest>.0001f)))throw new ArgumentException("Invalid ambient pedestrian state");}}
 public void Restore(AmbientPedestrianSnapshot saved){Validate(saved);if(saved==null){foreach(var actor in actors)actor.ResetSafe();return;}foreach(var value in saved.actors)At(value.id).Restore(value);}
 public void ReconcileVisibility(){foreach(var actor in actors)actor.ReconcileVisibility();}
}

public sealed class AmbientPedestrian : MonoBehaviour {
 public AmbientPedestrianSystem system;
 public AmbientPedestrianRoute route;
 public AmbientPedestrianState state;
 public bool Crossing {get{return route.crossing&&state.crossing;}}
 GameObject leftLeg,rightLeg,leftArm,rightArm,visual;
 float speed,phase;
 public void Initialize(AmbientPedestrianSystem owner,AmbientPedestrianRoute assigned,int index){system=owner;route=assigned;state=new AmbientPedestrianState{id=route.id};speed=1.2f+(index%3)*.1f;BuildVisual(index);ResetSafe();}
  void BuildVisual(int index){visual=new GameObject("보행 외형");visual.transform.SetParent(transform,false);Color[] coats={new Color(.38f,.38f,.43f),new Color(.43f,.30f,.24f),new Color(.43f,.29f,.48f)};Material coat=WorldBuilder.Mat("Ambient coat "+index,coats[index%3]),trouser=WorldBuilder.Mat("Ambient trousers "+index,new Color(.15f,.16f,.18f)),skin=WorldBuilder.Mat("Ambient face "+index,new Color(.7f,.53f,.42f));Part("몸통",new Vector3(0,1.04f,0),new Vector3(.43f,.68f,.28f),coat,visual.transform);Part("머리",new Vector3(0,1.68f,0),new Vector3(.32f,.32f,.32f),skin,visual.transform);leftLeg=Part("왼발",new Vector3(-.14f,.38f,0),new Vector3(.16f,.68f,.18f),trouser,visual.transform);rightLeg=Part("오른발",new Vector3(.14f,.38f,0),new Vector3(.16f,.68f,.18f),trouser,visual.transform);leftArm=Part("왼팔",new Vector3(-.33f,1.02f,0),new Vector3(.15f,.61f,.18f),coat,visual.transform);rightArm=Part("오른팔",new Vector3(.33f,1.02f,0),new Vector3(.15f,.61f,.18f),coat,visual.transform);}
 static GameObject Part(string name,Vector3 position,Vector3 size,Material paint,Transform parent){var p=GameObject.CreatePrimitive(PrimitiveType.Cube);p.name=name;p.transform.SetParent(parent,false);p.transform.localPosition=position;p.transform.localScale=size;p.GetComponent<Renderer>().sharedMaterial=paint;Destroy(p.GetComponent<Collider>());return p;}
 public void ResetSafe(){state.progress=0;state.rest=1;state.crossing=false;state.reverse=false;state.greenConsumed=false;state.contactLatched=false;state.deferredReveal=false;Place();ReconcileVisibility();}
 public AmbientPedestrianState Capture(){return new AmbientPedestrianState{id=route.id,progress=state.progress,rest=state.rest,crossing=state.crossing,reverse=state.reverse,greenConsumed=state.greenConsumed,contactLatched=state.contactLatched,deferredReveal=state.deferredReveal};}
 public void Restore(AmbientPedestrianState saved){state=new AmbientPedestrianState{id=route.id,progress=saved.progress,rest=saved.rest,crossing=saved.crossing,reverse=saved.reverse,greenConsumed=saved.greenConsumed,contactLatched=saved.contactLatched,deferredReveal=saved.deferredReveal};Place();}
 public void ForceCrossingForProbe(float progress){if(!route.crossing)throw new InvalidOperationException("Not a crossing actor");state.crossing=true;state.progress=Mathf.Clamp(progress,.01f,.99f);state.greenConsumed=true;state.rest=0;Place();}
  void Place(){float pathProgress=state.reverse?1-state.progress:state.progress;transform.position=route.Point(pathProgress);var dir=route.Point(Mathf.Clamp01(pathProgress+(state.reverse?-.01f:.01f)))-transform.position;dir.y=0;if(dir.sqrMagnitude>.001f)transform.rotation=Quaternion.LookRotation(dir);}
 void Update(){if(system==null)return;ReconcileVisibility();if(system.session.paused||Time.timeScale<=0)return;float dt=Time.deltaTime;if(route.crossing)TickCrossing(dt);else TickSidewalk(dt);Place();var moving=route.crossing?state.crossing:state.rest<=0;phase+=moving?dt*8:0;float swing=moving?Mathf.Sin(phase)*20:0;leftLeg.transform.localRotation=Quaternion.Euler(swing,0,0);rightLeg.transform.localRotation=Quaternion.Euler(-swing,0,0);leftArm.transform.localRotation=Quaternion.Euler(-swing,0,0);rightArm.transform.localRotation=Quaternion.Euler(swing,0,0);Contact();}
 void TickSidewalk(float dt){if(state.rest>0){state.rest=Mathf.Max(0,state.rest-dt);return;}state.progress+=dt*speed/route.Length;if(state.progress>=1){state.progress=0;state.reverse=!state.reverse;state.rest=1.2f;}}
 void TickCrossing(float dt){var traffic=system.session.traffic;bool green=traffic.PedestrianGreen(route.junction,route.axis,route.sign);if(!green)state.greenConsumed=false;if(state.crossing){state.progress=Mathf.Min(1,state.progress+dt*speed/route.Length);if(state.progress>=1){state.crossing=false;state.progress=0;state.reverse=!state.reverse;state.rest=1.5f;state.greenConsumed=true;}return;}
  state.rest=Mathf.Max(0,state.rest-dt);if(state.rest>0||!green||state.greenConsumed||traffic.PedestrianGreenRemaining(route.junction,route.axis,route.sign)<=2||!system.MayStartCrossing(this)){return;}state.crossing=true;state.progress=0;state.greenConsumed=true;
 }
 public bool VisualActive {get{return visual&&visual.activeSelf;}}
 public void ReconcileVisibility(){if(!visual||system==null)return;if(!route.crossing){visual.SetActive(true);return;}if(state.crossing){visual.SetActive(true);return;}if(system.ScenarioOwns(route.junction)){state.deferredReveal=true;visual.SetActive(false);return;}if(state.deferredReveal){var taxi=system.session.taxi;bool far=taxi&&Vector3.Distance(taxi.transform.position,transform.position)>80;var camera=system.session.drivingCamera?system.session.drivingCamera.GetComponent<Camera>():null;bool offscreen=false;if(camera){var screen=camera.WorldToViewportPoint(transform.position+Vector3.up);offscreen=screen.z<=0||screen.x<-.05f||screen.x>1.05f||screen.y<-.05f||screen.y>1.05f;}if(!far||!offscreen){visual.SetActive(false);return;}state.deferredReveal=false;}visual.SetActive(true);}
 void Contact(){if(!visual.activeSelf){state.contactLatched=false;return;}var taxi=system.session.taxi;if(!taxi||!taxi.body)return;var body=taxi.GetComponent<BoxCollider>();if(!body)return;var chest=transform.position+Vector3.up*.85f;float distance=Vector3.Distance(chest,body.ClosestPoint(chest));if(distance>.85f){state.contactLatched=false;return;}if(state.contactLatched||distance>.32f||taxi.body.velocity.magnitude<1.5f)return;state.contactLatched=true;system.session.manners.Deduct("collision",10,"보행자 접촉",3);}
}
