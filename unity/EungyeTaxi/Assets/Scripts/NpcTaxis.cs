using System.Collections.Generic;
using UnityEngine;

// NPC taxis (design 08 ch.5.5 item 7, decided 2026-11-25, Eungye single). A few ordinary cars are painted as taxis.
// An empty NPC taxi passing a stop where a roadside passenger waits (not an app call, not the player's pickup) may
// take that passenger first; it is then busy for a while (roof sign off). Ranks use the same model for their queue.
public static class NpcTaxiRules {
 static float F(string id,double fallback){return (float)ContentTuning.Number(id,fallback);}
 static int I(string id,int fallback){return ContentTuning.Int(id,fallback);}
 public static bool Enabled {get{return I("npcTaxi.enabled",1)!=0;}}
 public static int Count {get{return Mathf.Clamp(I("npcTaxi.count",8),0,30);}}
 public static int TakeChance {get{return Mathf.Clamp(I("npcTaxi.takeChancePercent",50),0,100);}}
 public static float BusySeconds {get{return Mathf.Clamp(F("npcTaxi.busySeconds",120),10,1200);}}
 public static float PassMetres {get{return Mathf.Clamp(F("npcTaxi.passMetres",9),3,25);}}
 // Every n-th ordinary car is a taxi, spreading them over the fleet.
 public static bool IsTaxi(int index,int fleet,int count){if(count<=0||fleet<=0)return false;int step=Mathf.Max(1,fleet/count);return index%step==step/2&&index/step<count;}
}

public sealed class NpcTaxis : MonoBehaviour {
 public DriveSession session;
 sealed class Npc {public TrafficCar car;public float busyUntil;public Renderer sign;}
 readonly List<Npc> taxis=new List<Npc>();
 public int Taken;public string LastTaken="";float nextScan;
 static Material yellow,signOn,signOff,checker;
 static void Materials(){if(yellow)return;yellow=WorldBuilder.Mat("NPC 택시 노랑",new Color(.98f,.74f,.08f));signOn=WorldBuilder.Mat("NPC 택시 표시등 켜짐",new Color(1,.92f,.55f));signOn.EnableKeyword("_EMISSION");signOn.SetColor("_EmissionColor",new Color(1,.85f,.4f)*1.6f);signOff=WorldBuilder.Mat("NPC 택시 표시등 꺼짐",new Color(.45f,.42f,.36f));checker=WorldBuilder.Mat("NPC 택시 띠",new Color(.1f,.1f,.12f));}
 // A parked taxi for rank queues: yellow body, roof sign, collider marked as a staged vehicle.
 public static GameObject BuildModel(string name,Transform parent){
  Materials();var root=new GameObject(name);root.transform.SetParent(parent,false);
  var glass=WorldBuilder.Mat("NPC 택시 유리",new Color(.08f,.15f,.19f));var tyre=WorldBuilder.Mat("AI tyres",new Color(.035f,.04f,.05f));
  var box=root.AddComponent<BoxCollider>();box.center=new Vector3(0,.8f,0);box.size=new Vector3(1.8f,1.25f,4.4f);root.AddComponent<StagedVehicle>();
  WorldBuilder.Box("차체",new Vector3(0,.65f,0),new Vector3(1.8f,.65f,4.4f),yellow,root.transform,false);
  WorldBuilder.Box("창문",new Vector3(0,1.2f,-.2f),new Vector3(1.5f,.62f,2.1f),glass,root.transform,false);
  WorldBuilder.Box("지붕",new Vector3(0,1.55f,-.2f),new Vector3(1.55f,.1f,2.2f),yellow,root.transform,false);
  WorldBuilder.Box("택시 표시등",new Vector3(0,1.72f,-.1f),new Vector3(.7f,.22f,.3f),signOn,root.transform,false);
  foreach(int sd in new[]{-1,1})WorldBuilder.Box("택시 띠",new Vector3(sd*.91f,.7f,0),new Vector3(.02f,.12f,3.8f),checker,root.transform,false);
  for(int i=0;i<4;i++){var w=GameObject.CreatePrimitive(PrimitiveType.Cylinder);w.transform.SetParent(root.transform,false);w.transform.localPosition=new Vector3(i%2==0?-.86f:.86f,.34f,i<2?1.4f:-1.4f);w.transform.localRotation=Quaternion.Euler(0,0,90);w.transform.localScale=new Vector3(.68f,.12f,.68f);w.GetComponent<Renderer>().sharedMaterial=tyre;Destroy(w.GetComponent<Collider>());}
  foreach(var child in root.GetComponentsInChildren<Transform>())child.gameObject.layer=9;
  return root;
 }
 bool Single {get{return session&&session.traffic&&session.traffic.EungyeSingle&&!session.simulationMatch&&!session.IsSimulationBot&&!PvpNativeLan.IsReplica(session);}}
 public void Initialize(DriveSession owner){
  session=owner;taxis.Clear();if(!Single||!NpcTaxiRules.Enabled)return;Materials();
  var fleet=session.traffic.cars;int count=NpcTaxiRules.Count;
  for(int i=0;i<fleet.Count;i++){if(!NpcTaxiRules.IsTaxi(i,fleet.Count,count))continue;var car=fleet[i];if(!car)continue;
   foreach(var r in car.GetComponentsInChildren<Renderer>())if(r.name=="Body"||r.name=="Roof")r.sharedMaterial=yellow;
   var sign=WorldBuilder.Box("NPC 택시 표시등",new Vector3(0,1.72f,-.1f),new Vector3(.7f,.22f,.3f),signOn,car.transform,false);sign.layer=9;
   foreach(int sd in new[]{-1,1}){var band=WorldBuilder.Box("NPC 택시 띠",new Vector3(sd*.91f,.7f,0),new Vector3(.02f,.12f,3.8f),checker,car.transform,false);band.layer=9;}
   car.gameObject.name+=" · 택시";taxis.Add(new Npc{car=car,sign=sign.GetComponent<Renderer>()});}
  Debug.Log("NPC_TAXIS painted="+taxis.Count);
 }
 public int TaxiCount {get{return taxis.Count;}}
 public bool IsNpcTaxi(TrafficCar car){foreach(var t in taxis)if(t.car==car)return true;return false;}
 void Update(){
  if(taxis.Count==0||Time.time<nextScan||session.paused)return;nextScan=Time.time+.25f;
  var j=session.jobs;if(!j||j.stops==null)return;var ranks=session.ranks;
  foreach(var t in taxis){
   if(!t.car||!t.car.Visible)continue;bool busy=Time.time<t.busyUntil;if(t.sign)t.sign.sharedMaterial=busy?signOff:signOn;if(busy)continue;
   var p=t.car.transform.position;
   for(int i=0;i<j.stops.Length;i++){
    if(ranks&&ranks.IsRankStop(i))continue;                 // ranks have their own queue
    if(!j.RoadsideTakeable(i))continue;
    var kerb=j.stops[i].Kerb;var rel=kerb-p;rel.y=0;if(rel.sqrMagnitude>NpcTaxiRules.PassMetres*NpcTaxiRules.PassMetres)continue;
    if(Vector3.Dot(t.car.transform.forward,j.stops[i].Direction)<.7f)continue;   // same side of the road
    int roll=(int)(Mathf.Abs((p.x*73856093f)+(p.z*19349663f)+Time.frameCount)%100);if(roll>=NpcTaxiRules.TakeChance){t.busyUntil=Time.time+3;break;}
    if(!j.TakenByOtherTaxi(i))continue;
    Taken++;LastTaken=KoreanText.Stop(j.stops[i].name);t.busyUntil=Time.time+NpcTaxiRules.BusySeconds;
    if((session.taxi.transform.position-kerb).sqrMagnitude<150*150){var n=session.GetComponent<DriveNotifications>();if(n)n.Card("다른 택시 · "+LastTaken+" 길가 손님을 먼저 태웠어요");}
    Debug.Log("NPC_TAXI took a roadside passenger at "+LastTaken);break;
   }
  }
 }
 // Acceptance probe: let a given NPC taxi pass a stop now.
 public bool TryTakeAt(int stop){var j=session.jobs;if(!j.RoadsideTakeable(stop))return false;if(!j.TakenByOtherTaxi(stop))return false;Taken++;LastTaken=KoreanText.Stop(j.stops[stop].name);return true;}
}
