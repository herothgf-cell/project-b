using System.Collections.Generic;
using UnityEngine;

// Game guidance is mounted on the matching signal, never a second roadside pole.
public class RoadSignage : MonoBehaviour {
 public DriveSession session;
 readonly List<GameObject> signs=new List<GameObject>();
 sealed class Board {public TrafficJunction junction;public int axis,sign;public TextMesh caption;public bool permissive;}
 readonly List<Board> boards=new List<Board>();
 public int Count {get{return signs.Count;}}
 void Start(){
  foreach(var j in session.traffic.junctions)for(int i=0;i<j.lamps.Count;i++){
   var lamps=j.lamps[i];if(lamps==null||lamps.Length==0||!lamps[0])continue;
   var signalRoot=lamps[0].transform.parent;if(signalRoot&&signalRoot.Find("비보호 표지"))continue;
   var forward=session.traffic.AxisDirection(j,j.lampAxes[i])*j.lampSigns[i];
   var centre=Vector3.zero;int count=0;foreach(var lamp in lamps)if(lamp){centre+=lamp.transform.position;count++;}if(count==0)continue;centre/=count;
   var root=new GameObject("게임 신호 상부 표지 · "+j.name);root.transform.SetParent(transform);root.transform.SetPositionAndRotation(centre+Vector3.up*1.15f,Quaternion.LookRotation(forward));
   string caption=j.leftTurnMode==LeftTurnMode.Permissive?"비보호 좌회전\n녹색 · 대향차 양보":"횡단보도";
   var label=Label(root.transform,caption,Vector3.zero,new Color(.07f,.25f,.65f));signs.Add(root);boards.Add(new Board{junction=j,axis=j.lampAxes[i],sign=j.lampSigns[i],caption=label.GetComponentInChildren<TextMesh>(),permissive=j.leftTurnMode==LeftTurnMode.Permissive});
  }
 }
 void LateUpdate(){foreach(var board in boards){var j=board.junction;bool permissive=j.leftTurnMode==LeftTurnMode.Permissive&&(!j.scenarioSignal||board.axis==j.observationAxis&&board.sign==j.observationSign);if(permissive==board.permissive)continue;board.permissive=permissive;board.caption.text=permissive?"비보호 좌회전\n녹색 · 대향차 양보":"횡단보도";KoreanText.Fit(board.caption,4.2f,1.25f);}}
 GameObject Label(Transform parent,string text,Vector3 at,Color color){var holder=new GameObject(text);holder.transform.SetParent(parent,false);holder.transform.localPosition=at;
  Part(holder.transform,"표지판",Vector3.zero,new Vector3(4.5f,1.45f,.08f),color);
  var caption=new GameObject("안내");caption.transform.SetParent(holder.transform,false);caption.transform.localPosition=new Vector3(0,0,-.06f);var mesh=caption.AddComponent<TextMesh>();mesh.text=text;mesh.color=Color.white;KoreanText.Fit(mesh,4.2f,1.25f);return holder;
 }
 static void Part(Transform parent,string name,Vector3 at,Vector3 scale,Color color){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=at;go.transform.localScale=scale;if(Application.isPlaying)Destroy(go.GetComponent<Collider>());else DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().material.color=color;}
}
