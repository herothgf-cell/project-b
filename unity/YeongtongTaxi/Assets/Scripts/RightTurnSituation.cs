using UnityEngine;

// Explicitly fictional training operation on the existing junction. Never claims surveyed signal timing.
public class RightTurnSituation : MonoBehaviour {
 public DriveSession session;public TrafficJunction junction;public int variant;
 GameObject pedestrian,label;TextMesh sign,dedicatedLight,pedestrianLight;float started;bool approached;
 public bool Active {get{return variant>0;}}
 void Start(){
  if(!session.business.useV2){enabled=false;return;}
  float nearest=float.MaxValue;foreach(var j in session.traffic.junctions){float d=Vector3.Distance(j.center,new Vector3(575,0,257));if(j.signalized&&d<nearest){nearest=d;junction=j;}}
  if(junction==null){enabled=false;return;}
  label=new GameObject("가상 우회전 판단 상황 안내");label.transform.position=junction.center-session.traffic.AxisDirection(junction,0)*65+Vector3.up*5;
  sign=label.AddComponent<TextMesh>();sign.fontSize=40;sign.characterSize=.11f;sign.anchor=TextAnchor.MiddleCenter;sign.color=Color.yellow;KoreanText.Apply(sign);label.AddComponent<FaceCamera>();
  pedestrian=GameObject.CreatePrimitive(PrimitiveType.Capsule);pedestrian.name="상황 보행자 (가상)";pedestrian.transform.localScale=new Vector3(.6f,.85f,.6f);Destroy(pedestrian.GetComponent<Collider>());pedestrian.SetActive(false);label.SetActive(false);
  var dir=session.traffic.AxisDirection(junction,0);var right=TrafficSystem.Right(dir);
  dedicatedLight=LightLabel("임시 우회전전용신호",junction.center-dir*junction.StopDistance(0)+right*(junction.halfWidth[0]+2)+Vector3.up*4);
  pedestrianLight=LightLabel("임시 보행 신호",junction.center+right*(junction.halfWidth[0]+5)-dir*(junction.halfWidth[1]+2)+Vector3.up*3);
 }
 TextMesh LightLabel(string name,Vector3 position){var obj=new GameObject(name);obj.transform.position=position;var t=obj.AddComponent<TextMesh>();t.fontSize=60;t.characterSize=.12f;t.anchor=TextAnchor.MiddleCenter;KoreanText.Apply(t);obj.AddComponent<FaceCamera>();obj.SetActive(false);return t;}
 public bool Select(int value){
  if(value==0){Stop();return true;}
  var exit=GetComponent<ExitQueueSituation>();if(exit&&exit.Active)return false;
  if(junction==null||session.jobs.Active||session.taxi.body.velocity.magnitude>=.56f||Vector3.Distance(session.taxi.transform.position,junction.center)<80)return false;
  variant=value;started=Time.time;approached=false;junction.rightTurnObservation=null;junction.scenarioSignal=false;pedestrian.SetActive(false);label.SetActive(value>0);
  if(value>0){var dir=session.traffic.AxisDirection(junction,0);var point=junction.center-dir*70+TrafficSystem.Right(dir)*(junction.halfWidth[0]-1.75f);session.jobs.nav.SetDestination(point);}
  return true;
 }
 public void Stop(){bool owned=Active;variant=0;if(owned&&junction!=null){junction.rightTurnObservation=null;junction.scenarioSignal=false;}if(pedestrian)pedestrian.SetActive(false);if(label)label.SetActive(false);if(dedicatedLight)dedicatedLight.gameObject.SetActive(false);if(pedestrianLight)pedestrianLight.gameObject.SetActive(false);}
 void Update(){
  if(Input.GetKeyDown(KeyCode.F6)&&!session.paused)Select((variant+1)%7);
  if(!Active)return;
  if(!approached&&Vector3.Distance(session.taxi.transform.position,junction.center)<=85){approached=true;started=Time.time;}
  float t=approached?Time.time-started:0;bool release=t>=16;var dir=session.traffic.AxisDirection(junction,0);var right=TrafficSystem.Right(dir);
  var o=new RoadObservation{known=true,trafficClear=true,forward=variant==1&&!release?SignalColor.Red:SignalColor.Green,pedestrianGreen=variant!=4,dedicated=variant==3||variant==4,right=variant==3&&!release?SignalColor.Red:SignalColor.Green};
  if(variant>=5&&t<12)o.pedestrian=variant==5?PedestrianState.Intending:PedestrianState.Crossing;
  junction.scenarioSignal=true;junction.scenarioForward=o.forward;junction.observationAxis=0;junction.observationSign=1;junction.rightTurnObservation=o;
  dedicatedLight.gameObject.SetActive(o.dedicated);dedicatedLight.text=(o.right==SignalColor.Green?"→":"●")+"\n임시 우회전 전용";dedicatedLight.color=o.right==SignalColor.Green?Color.green:Color.red;
  pedestrianLight.gameObject.SetActive(true);pedestrianLight.text="보행\n"+(o.pedestrianGreen?"녹색":"적색");pedestrianLight.color=o.pedestrianGreen?Color.green:Color.red;
  bool present=o.pedestrian!=PedestrianState.None;pedestrian.SetActive(present);
  // Pedestrian is visible from the advance notice; intent precedes the protected crossing interval.
  pedestrian.transform.position=junction.center+right*(junction.halfWidth[0]+5)+dir*(variant==6?Mathf.Lerp(-junction.halfWidth[1]-1,junction.halfWidth[1]+1,t/12):-junction.halfWidth[1]-1)+Vector3.up*.85f;
  sign.text="가상 우회전 R"+(variant>=5?5:variant)+" · "+junction.name+"\n전방 "+ColorName(o.forward)+" / 전용 "+(o.dedicated?ColorName(o.right):"없음")+"\n보행 신호 "+(o.pedestrianGreen?"녹색":"적색")+" · "+(o.pedestrian==PedestrianState.None?"사람 없음":o.pedestrian==PedestrianState.Intending?"횡단 의도":"횡단 중");
 }
 static string ColorName(SignalColor color){return color==SignalColor.Green?"녹색":color==SignalColor.Red?"적색":"황색";}
 void OnGUI(){if(!enabled)return;var style=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=14};GUI.Label(new Rect(420,16,660,28),"F6 가상 우회전 조건 선택 (빈차·정차·구간 80m 밖) · "+(Active?"R"+(variant>=5?5:variant):"꺼짐"),style);}
}
