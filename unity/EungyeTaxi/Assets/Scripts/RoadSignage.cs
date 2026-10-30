using System.Collections.Generic;
using UnityEngine;

// Only a real movement condition gets a sign; a vehicle signal is not a crosswalk billboard.
public class RoadSignage : MonoBehaviour {
 public DriveSession session;
 sealed class Board {public TrafficJunction junction;public int axis,sign;public Transform signal;public Vector3 centre,forward;public GameObject panel;}
 readonly List<Board> boards=new List<Board>();
 public int Count {get{int count=0;foreach(var b in boards)if(b.panel&&b.panel.activeInHierarchy)count++;return count;}}
 void Start(){
  foreach(var j in session.traffic.junctions)for(int i=0;i<j.lamps.Count;i++){
   var lamps=j.lamps[i];if(lamps==null||lamps.Length==0||!lamps[0])continue;
   var signal=lamps[0].transform.parent;var existing=signal?signal.Find("비보호 표지"):null;
   var centre=Vector3.zero;int count=0;foreach(var lamp in lamps)if(lamp){centre+=lamp.transform.position;count++;}if(count==0)continue;
   var b=new Board{junction=j,axis=j.lampAxes[i],sign=j.lampSigns[i],signal=signal,centre=centre/count,forward=session.traffic.AxisDirection(j,j.lampAxes[i])*j.lampSigns[i],panel=existing?existing.gameObject:null};boards.Add(b);Refresh(b);
  }
 }
 void LateUpdate(){foreach(var b in boards)Refresh(b);}
 void Refresh(Board b){
  if(!b.signal)return;
  bool visible=b.junction.leftTurnMode==LeftTurnMode.Permissive&&(!b.junction.scenarioSignal||b.axis==b.junction.observationAxis&&b.sign==b.junction.observationSign);
  if(visible&&!b.panel){b.panel=CreatePermissive(b.signal,Vector3.zero);b.panel.transform.SetPositionAndRotation(b.centre+Vector3.up*1.15f,Quaternion.LookRotation(b.forward));}
  if(b.panel&&b.panel.activeSelf!=visible)b.panel.SetActive(visible);
 }
 // Simplified 329-style vertical face. Dimensions are metres, not a road-spanning game banner.
 public static GameObject CreatePermissive(Transform parent,Vector3 at){
  var root=new GameObject("비보호 표지");root.transform.SetParent(parent,false);root.transform.localPosition=at;
  Part(root.transform,"회색 뒷판",Vector3.zero,new Vector3(.7f,.95f,.06f),new Color(.35f,.38f,.4f));
  Part(root.transform,"흰 테두리",new Vector3(0,0,-.035f),new Vector3(.69f,.94f,.008f),Color.white);
  Part(root.transform,"청색 앞면",new Vector3(0,0,-.043f),new Vector3(.64f,.89f,.008f),new Color(.025f,.18f,.58f));
  var points=new[]{new Vector2(.14f,-.08f),new Vector2(.14f,.15f),new Vector2(.12f,.23f),new Vector2(.07f,.28f),new Vector2(-.12f,.28f)};
  for(int i=1;i<points.Length;i++){var delta=points[i]-points[i-1];var mid=(points[i]+points[i-1])*.5f;var stroke=Part(root.transform,"좌회전 화살표",new Vector3(mid.x,mid.y,-.052f),new Vector3(delta.magnitude+.025f,.065f,.008f),Color.white);stroke.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);}
  foreach(int side in new[]{-1,1}){var tip=Part(root.transform,"좌회전 화살촉",new Vector3(-.13f,.28f+side*.055f,-.053f),new Vector3(.19f,.06f,.008f),Color.white);tip.transform.localRotation=Quaternion.Euler(0,0,side*40);}
  var caption=new GameObject("비보호 문구");caption.transform.SetParent(root.transform,false);caption.transform.localPosition=new Vector3(0,-.3f,-.06f);var text=caption.AddComponent<TextMesh>();text.text="비보호";text.color=Color.white;KoreanText.Fit(text,.55f,.19f);return root;
 }
 static GameObject Part(Transform parent,string name,Vector3 at,Vector3 scale,Color color){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=at;go.transform.localScale=scale;if(Application.isPlaying)Destroy(go.GetComponent<Collider>());else DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().material.color=color;return go;}
}
