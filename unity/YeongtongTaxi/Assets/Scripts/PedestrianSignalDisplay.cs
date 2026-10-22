using UnityEngine;
// Provisional game operation; scenario observations remain authoritative when active.
public sealed class PedestrianSignalDisplay:MonoBehaviour {
 TrafficSystem system;TrafficJunction junction;int axis,sign;Renderer red,green;Material onRed,onGreen,off;
 public static void Create(TrafficSystem traffic,TrafficJunction j,Vector3 incoming,float halfWidth,float conflict,Transform parent){
  var across=TrafficSystem.Right(incoming);int axis=Mathf.Abs(Vector3.Dot(incoming,traffic.AxisDirection(j,0)))>.7f?0:1;
  for(int side=-1;side<=1;side+=2){var desired=j.center-incoming*(conflict+TrafficJunction.CrosswalkCentreOffset)+across*(halfWidth+1.1f)*side;
   var root=new GameObject("보행자 신호 · 게임 설정 · "+j.name);root.transform.SetParent(parent,false);root.transform.SetPositionAndRotation(desired,Quaternion.LookRotation(across*side));
   var display=root.AddComponent<PedestrianSignalDisplay>();display.system=traffic;display.junction=j;display.axis=axis;display.sign=traffic.Sign(incoming,axis,j);display.Build();}
 }
 void Build(){var dark=WorldBuilder.Mat("Pedestrian housing",new Color(.025f,.03f,.04f));onRed=WorldBuilder.Mat("Pedestrian stop",Color.red);onGreen=WorldBuilder.Mat("Pedestrian go",Color.green);off=WorldBuilder.Mat("Pedestrian unlit",new Color(.07f,.09f,.09f));foreach(var m in new[]{onRed,onGreen}){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",m.color*2);}
  WorldBuilder.Box("보행 신호 기둥",new Vector3(0,1.4f,0),new Vector3(.12f,2.8f,.12f),dark,transform,false);WorldBuilder.Box("보행 신호함",new Vector3(0,2.6f,0),new Vector3(.7f,1.18f,.22f),dark,transform,false);
  red=WorldBuilder.Box("정지 사람",new Vector3(0,2.87f,-.14f),new Vector3(.43f,.43f,.04f),off,transform,false).GetComponent<Renderer>();green=WorldBuilder.Box("보행 사람",new Vector3(0,2.33f,-.14f),new Vector3(.43f,.43f,.04f),off,transform,false).GetComponent<Renderer>();
  var label=new GameObject("보행 안내");label.transform.SetParent(transform,false);label.transform.localPosition=new Vector3(0,1.95f,-.15f);var text=label.AddComponent<TextMesh>();text.text="보행";KoreanText.Fit(text,.65f,.23f);Update();
 }
 void Update(){if(!red)return;bool walk=system.PedestrianGreen(junction,axis,sign);red.sharedMaterial=walk?off:onRed;green.sharedMaterial=walk?onGreen:off;}
}
