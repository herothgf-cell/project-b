using System.Linq;
using UnityEngine;
public sealed class RoadMapWalker:MonoBehaviour {
 public RoadMapFacility crossing;public RoadMapSignalProgram program;public TrafficSystem system;float progress=-1;public bool Crossing {get{return progress>=0;}}
 void Start(){var shape=GameObject.CreatePrimitive(PrimitiveType.Capsule);shape.transform.SetParent(transform,false);shape.transform.localPosition=Vector3.up*.9f;shape.transform.localScale=new Vector3(.5f,.9f,.5f);shape.GetComponent<Renderer>().sharedMaterial=WorldBuilder.Mat("Editor pedestrian",new Color(.95f,.6f,.2f));shape.GetComponent<Collider>().isTrigger=true;var body=shape.AddComponent<Rigidbody>();body.isKinematic=true;Place(0);}
 void Place(float t){var across=Quaternion.Euler(0,crossing.yaw,0)*Vector3.right;transform.position=crossing.Position+across*((t-.5f)*(crossing.width+2));}
 void Update(){if(program==null||!system)return;bool green=program.groups.Any(g=>g.pedestrian&&g.crosswalkId==crossing.id&&program.Color(g.id,system.Clock)==SignalColor.Green);if(progress<0&&green)progress=0;if(progress>=0){progress+=Time.deltaTime*1.4f/(crossing.width+2);Place(Mathf.Clamp01(progress));if(progress>=1)progress=-1;}}
}
