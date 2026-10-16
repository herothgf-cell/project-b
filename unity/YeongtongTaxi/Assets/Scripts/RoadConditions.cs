using System.Collections.Generic;
using UnityEngine;

public sealed class RoadIncident {
 public Vector3 position,direction;
 public float roadWidth,expiresAt;
 public bool queue;
 public readonly List<GameObject> visuals=new List<GameObject>();
}

// All delays correspond to visible obstructions or sampled moving traffic.
// Costs belong to directed segments; the opposite carriageway is unaffected.
public sealed class RoadConditions : MonoBehaviour {
 public DriveSession session;
 public readonly List<RoadIncident> incidents=new List<RoadIncident>();
 public int Revision {get;private set;}
 public string Notice {get;private set;}
 struct Sample {public Vector3 position,direction;public float delay;}
 readonly List<Sample> samples=new List<Sample>();
 bool automatic;float nextEvent,nextSample;int sequence;
 public void Initialize(DriveSession owner){
  session=owner;automatic=!RoadRulesTrial.Enabled;
  foreach(var arg in System.Environment.GetCommandLineArgs())if(arg.EndsWith("Test")||arg=="-assistedTrial")automatic=false;
  nextEvent=Time.time+15;Notice="도로 상황을 관찰하며 경로를 선택하세요";
 }
 public RoadIncident AddIncident(Vector3 position,Vector3 direction,float roadWidth,float seconds,bool queue=false,bool spawnObjects=false){
  var item=new RoadIncident{position=position,direction=direction.normalized,roadWidth=roadWidth,expiresAt=Time.time+Mathf.Max(0,seconds),queue=queue};
  incidents.Add(item);Revision++;
  if(spawnObjects)CreateVisuals(item);
  Notice=queue?"앞쪽 도로 정체 · 예상 소요 시간을 비교하세요":"정차 차량 발생 · 옆 차로 또는 우회 경로를 확인하세요";
  return item;
 }
 static bool OnSegment(Vector3 point,Vector3 heading,Vector3 a,Vector3 b,Vector3 direction,float width){
  if(Vector3.Dot(heading,direction)<.75f)return false;
  var delta=b-a;delta.y=0;float length=delta.magnitude;if(length<.01f)return false;
  var offset=point-a;offset.y=0;float along=Vector3.Dot(offset,delta/length);
  return along>=0&&along<length&&Mathf.Abs(Vector3.Dot(offset,TrafficSystem.Right(delta/length)))<=width*.5f+.3f;
 }
 public float ExtraSeconds(Vector3 a,Vector3 b,Vector3 direction,float roadWidth){
  float delay=0;
  foreach(var item in incidents){float remaining=item.expiresAt-Time.time;if(remaining<=0||!OnSegment(item.position,item.direction,a,b,direction,roadWidth))continue;
   delay+=roadWidth>=12?Mathf.Min(remaining,item.queue?12:8):Mathf.Min(remaining,90);
  }
  foreach(var sample in samples)if(OnSegment(sample.position,sample.direction,a,b,direction,roadWidth))delay+=sample.delay;
  return delay;
 }
 void Update(){
  for(int i=incidents.Count-1;i>=0;i--)if(incidents[i].expiresAt<=Time.time){Remove(incidents[i]);incidents.RemoveAt(i);Revision++;Notice="정차 구간 해소 · 경로를 다시 비교합니다";}
  if(!automatic||session==null||session.paused||session.business.ShowingSummary)return;
  if(Time.time>=nextSample){nextSample=Time.time+1;samples.Clear();
   foreach(var car in session.traffic.cars)if(car&&car.gameObject.activeInHierarchy&&car.transform.position.y>=0&&car.speed<3)
    samples.Add(new Sample{position=car.transform.position,direction=car.transform.forward,delay=2+(3-car.speed)*1.5f});
  }
  if(Time.time>=nextEvent){nextEvent=Time.time+90;if(incidents.Count<2)TryCreateEvent();}
 }
 bool ClearAt(Vector3 p,Vector3 dir){
  if(Vector3.Distance(p,session.taxi.transform.position)<60)return false;
  foreach(var stop in session.world.map.stops)if(Vector3.Distance(p,stop.Kerb)<16)return false;
  foreach(var junction in session.world.map.targets)if(Vector3.Distance(p,junction.V())<30)return false;
  foreach(var item in incidents)if(Vector3.Distance(p,item.position)<40)return false;
  // Roads and paving have no volume above .5m; buildings and vehicles do.
  return Physics.OverlapBox(p+Vector3.up*.9f,new Vector3(1.05f,.4f,2.5f),Quaternion.LookRotation(dir),(1<<8)|(1<<9)|1,QueryTriggerInteraction.Ignore).Length==0;
 }
 public bool TryCreateEvent(){
  if(session.world.map.hybrid){var graph=session.jobs.nav.hd;bool congested=sequence%2==1;
   for(int k=0;k<graph.edges.Length;k++){int i=(sequence*37+k)%graph.edges.Length;var edge=graph.edges[i];var path=graph.paths[i];if(edge.turn!=0||edge.change!=0||edge.status=="provisional-portal"||path.length<65)continue;
    var p=path.Position(path.length*.5f);var direction=path.Tangent(path.length*.5f);if(!ClearAt(p,direction))continue;
    bool clear=true;foreach(var rule in session.world.map.junctionRules)if(Vector3.Distance(p,rule.V())<30)clear=false;
    if(congested)for(int n=1;n<3;n++)clear&=ClearAt(p-direction*7*n,direction);if(!clear)continue;
    AddIncident(p,direction,3.2f,congested?90:65,congested,true);sequence++;return true;
   }return false;
  }
  var lanes=session.assisted.network.lanes;if(lanes.Count==0)return false;
  bool queue=sequence%2==1;int start=(sequence*37)%lanes.Count;
  for(int i=0;i<lanes.Count;i++){var lane=lanes[(start+i)%lanes.Count];if(lane.index!=lane.count-1||lane.path.length<65)continue;
   float at=lane.path.length*.5f;var p=lane.path.Position(at);var dir=lane.path.Tangent(at);bool clear=ClearAt(p,dir);
   if(queue)for(int k=1;k<3;k++)clear&=ClearAt(p-dir*7*k,dir);
   if(!clear)continue;AddIncident(p,dir,lane.width*lane.count*2,queue?90:65,queue,true);sequence++;return true;
  }
  return false;
 }
 void CreateVisuals(RoadIncident item){
  var paint=WorldBuilder.Mat("정차 차량",item.queue?new Color(.55f,.26f,.16f):new Color(.34f,.42f,.56f));
  var glass=WorldBuilder.Mat("정차 차량 유리",new Color(.08f,.16f,.2f));var amber=WorldBuilder.Mat("비상등",new Color(1,.55f,.02f));
  int count=item.queue?3:1;
  for(int i=0;i<count;i++){
   var car=new GameObject(item.queue?"정체 대기 차량":"비상 정차 차량");car.transform.SetParent(transform);car.transform.SetPositionAndRotation(item.position-item.direction*i*7,Quaternion.LookRotation(item.direction));
   var collider=car.AddComponent<BoxCollider>();collider.center=Vector3.up*.85f;collider.size=new Vector3(1.8f,1.3f,4.2f);
   WorldBuilder.Box("차체",Vector3.up*.65f,new Vector3(1.8f,.65f,4.2f),paint,car.transform,false);
   WorldBuilder.Box("창문",new Vector3(0,1.2f,-.2f),new Vector3(1.5f,.6f,2),glass,car.transform,false);
   foreach(int side in new[]{-1,1})WorldBuilder.Box("비상등",new Vector3(side*.7f,.8f,-2.11f),new Vector3(.2f,.2f,.05f),amber,car.transform,false);
   foreach(var child in car.GetComponentsInChildren<Transform>())child.gameObject.layer=9;
   item.visuals.Add(car);
  }
  var label=new GameObject("임시 도로 상황 안내");label.transform.SetParent(transform);label.transform.position=item.position+Vector3.up*3.4f;
  var text=label.AddComponent<TextMesh>();text.text=item.queue?"정체 대기":"비상 정차";text.font=KoreanText.Font;text.fontSize=40;text.characterSize=.08f;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(1,.7f,.1f);
  if(text.font)label.GetComponent<Renderer>().sharedMaterial=text.font.material;label.AddComponent<FaceCamera>();item.visuals.Add(label);
 }
 void Remove(RoadIncident item){foreach(var obj in item.visuals)if(obj){obj.SetActive(false);Destroy(obj);}item.visuals.Clear();}
 public void ResetForDay(){foreach(var item in incidents)Remove(item);incidents.Clear();samples.Clear();Revision++;nextEvent=Time.time+15;}
 void OnDestroy(){foreach(var item in incidents)Remove(item);}
}
