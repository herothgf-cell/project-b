using System.Collections.Generic;
using UnityEngine;

// Simplified game signs describe configured approaches, not surveyed street furniture.
public class RoadSignage : MonoBehaviour {
 public DriveSession session;
 readonly List<GameObject> signs=new List<GameObject>();
 public int Count {get{return signs.Count;}}
 void Start(){
  foreach(var j in session.traffic.junctions)for(int i=0;i<j.lampAxes.Count;i++){
   int axis=j.lampAxes[i],sign=j.lampSigns[i];var forward=session.traffic.AxisDirection(j,axis)*sign;var right=TrafficSystem.Right(forward);
   var position=j.center-forward*(j.StopDistance(axis)+12)+right*(j.halfWidth[axis]+2.5f);bool clear=false;
   for(int n=0;n<8;n++){if(position.x>2&&position.z>2&&position.x<session.world.map.width-2&&position.z<session.world.map.depth-2&&!session.roadEnvironment.OnRoad(position,1.8f)&&!Physics.CheckBox(position+Vector3.up*4,new Vector3(2.2f,1.7f,.2f),Quaternion.LookRotation(forward),1,QueryTriggerInteraction.Ignore)){clear=true;break;}position+=right*1.5f;}
   if(!clear)continue;
   var root=new GameObject("게임 표지 · "+j.name);root.transform.SetParent(transform);root.transform.SetPositionAndRotation(position,Quaternion.LookRotation(forward));
   Part(root.transform,"표지 기둥",new Vector3(0,1.75f,0),new Vector3(.1f,3.5f,.1f),Color.gray);
   Label(root.transform,"횡단보도\n게임 설정",new Vector3(0,3.2f,-.09f),new Color(.07f,.25f,.65f));
   // Permissive-left boards belong to the matching signal housing, not roadside posts.
   signs.Add(root);
  }
 }
 GameObject Label(Transform parent,string text,Vector3 at,Color color){var holder=new GameObject(text);holder.transform.SetParent(parent,false);holder.transform.localPosition=at;
  Part(holder.transform,"표지판",Vector3.zero,new Vector3(3.1f,1.15f,.08f),color);
  var caption=new GameObject("안내");caption.transform.SetParent(holder.transform,false);caption.transform.localPosition=new Vector3(0,0,-.06f);var mesh=caption.AddComponent<TextMesh>();mesh.text=text;mesh.color=Color.white;KoreanText.Fit(mesh,2.78f,.93f);return holder;
 }
 static void Part(Transform parent,string name,Vector3 at,Vector3 scale,Color color){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=at;go.transform.localScale=scale;Destroy(go.GetComponent<Collider>());go.GetComponent<Renderer>().material.color=color;}
}
