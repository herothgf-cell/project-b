using System.Linq;
using UnityEngine;
public sealed class RoadMapWalker:MonoBehaviour {
 public RoadMapFacility crossing;public RoadMapSignalProgram program;public TrafficSystem system;public Vector3 landingA,landingB;public bool hasLandings;float progress=-1,speed=1.4f;bool reverse,consumed;public bool Crossing {get{return progress>=0;}}
 void Start(){var shape=GameObject.CreatePrimitive(PrimitiveType.Capsule);shape.transform.SetParent(transform,false);shape.transform.localPosition=Vector3.up*.9f;shape.transform.localScale=new Vector3(.5f,.9f,.5f);shape.GetComponent<Renderer>().sharedMaterial=WorldBuilder.Mat("Editor pedestrian",new Color(.95f,.6f,.2f));shape.GetComponent<Collider>().isTrigger=true;var body=shape.AddComponent<Rigidbody>();body.isKinematic=true;Place(0);}
 void Place(float t){if(hasLandings)transform.position=Vector3.Lerp(landingA,landingB,reverse?1-t:t);else{var across=Quaternion.Euler(0,crossing.yaw,0)*Vector3.right;transform.position=crossing.Position+across*((t-.5f)*(crossing.width+2));}}
 void Update(){Advance(Time.deltaTime);}
 public void Advance(float seconds){if(program==null||!system||seconds<=0)return;bool green=program.groups.Any(g=>g.pedestrian&&g.crosswalkId==crossing.id&&program.Color(g.id,system.Clock)==SignalColor.Green);if(!green)consumed=false;
  float length=hasLandings?Vector3.Distance(landingA,landingB):crossing.width+2;
  if(progress<0&&green&&!consumed){float remaining=program.SecondsToChange(crossing.axis,crossing.sign,0,system.Clock,true);if(remaining>=length/2.2f+1){speed=Mathf.Max(1.4f,length/(remaining-1));progress=0;consumed=true;}}
  if(progress>=0){progress+=seconds*speed/Mathf.Max(.1f,length);Place(Mathf.Clamp01(progress));if(progress>=1){progress=-1;reverse=!reverse;}}
 }
}
