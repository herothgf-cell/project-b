using UnityEngine;
namespace Pvp.Map {
public static class PvpMapLoader {
 public static PvpMapRuntime Build(PvpMapDefinition definition,Transform parent=null){
  var map=new PvpMapRuntime(definition);var root=new GameObject("PVP graybox "+definition.MapId);if(parent!=null)root.transform.SetParent(parent,false);
  var resources=root.AddComponent<PvpMapVisualResources>();
  var roadMaterial=resources.CreateMaterial("Standard",new Color(0.22f,0.25f,0.28f));
  var stopMaterial=resources.CreateMaterial("Standard",new Color(1,0.7f,0.1f));
  var directionMaterial=resources.CreateMaterial("Sprites/Default",Color.white);
  foreach(var edge in map.Connections){var a=map.Nodes[edge.From];var b=map.Nodes[edge.To];var delta=b-a;var road=GameObject.CreatePrimitive(PrimitiveType.Cube);road.name="Lane "+edge.From+" > "+edge.To;road.transform.SetParent(root.transform,false);road.transform.position=(a+b)*0.5f+Vector3.down*0.15f;road.transform.rotation=Quaternion.LookRotation(delta);road.transform.localScale=new Vector3(map.RoadWidth,0.25f,delta.magnitude);road.GetComponent<Renderer>().sharedMaterial=roadMaterial;
   for(float t=0.2f;t<0.9f;t+=0.3f){var arrow=new GameObject("Direction");arrow.transform.SetParent(root.transform,false);var line=arrow.AddComponent<LineRenderer>();line.useWorldSpace=true;line.widthMultiplier=0.22f;line.positionCount=3;var forward=delta.normalized;var right=Vector3.Cross(Vector3.up,forward);var center=Vector3.Lerp(a,b,t)+Vector3.up*0.04f;line.SetPositions(new[]{center-forward*2+right,center,center-forward*2-right});line.sharedMaterial=directionMaterial;line.startColor=line.endColor=Color.white;}
  }
  foreach(var stop in map.Stops){var marker=GameObject.CreatePrimitive(PrimitiveType.Cylinder);marker.name="Stop "+stop.Id;marker.transform.SetParent(root.transform,false);marker.transform.position=stop.Position+Vector3.up*0.06f;marker.transform.localScale=new Vector3(4,0.08f,4);marker.GetComponent<Renderer>().sharedMaterial=stopMaterial;var collider=marker.GetComponent<Collider>();if(collider!=null){if(Application.isPlaying)Object.Destroy(collider);else Object.DestroyImmediate(collider);}}
  return map;
 }

}}
