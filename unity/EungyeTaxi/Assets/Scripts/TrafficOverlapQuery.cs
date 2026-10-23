using UnityEngine;

// One owner per simulation component. Saturation must never hide an obstacle.
public sealed class TrafficOverlapQuery {
 public Collider[] hits=new Collider[64];
 public int Box(Vector3 center,Vector3 half,Quaternion rotation,int mask,QueryTriggerInteraction triggers=QueryTriggerInteraction.UseGlobal){
  while(true){int count=Physics.OverlapBoxNonAlloc(center,half,hits,rotation,mask,triggers);if(count<hits.Length)return count;System.Array.Resize(ref hits,hits.Length*2);}
 }
 public int Sphere(Vector3 center,float radius,int mask,QueryTriggerInteraction triggers){while(true){int count=Physics.OverlapSphereNonAlloc(center,radius,hits,mask,triggers);if(count<hits.Length)return count;System.Array.Resize(ref hits,hits.Length*2);}}
}
