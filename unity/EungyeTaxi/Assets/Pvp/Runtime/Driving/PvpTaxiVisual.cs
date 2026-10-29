using UnityEngine;
using Pvp.Map;

namespace Pvp.Driving {
// Presentation only: root is at road level; this helper never adds driving physics.
public static class PvpTaxiVisual {
 public static GameObject Build(Transform parent,bool taxi=true,Color? bodyColor=null) {
  var root=new GameObject(taxi?"PVP taxi visual":"PVP traffic car visual");root.transform.SetParent(parent,false);
  var resources=root.AddComponent<PvpMapVisualResources>();
  var paint=resources.CreateMaterial("Standard",bodyColor??new Color(1,.72f,.12f));
  var dark=resources.CreateMaterial("Standard",new Color(.035f,.045f,.055f));
  var glass=resources.CreateMaterial("Standard",new Color(.15f,.29f,.36f));
  var metal=resources.CreateMaterial("Standard",new Color(.66f,.72f,.76f));
  var lamp=resources.CreateMaterial("Standard",new Color(1,.97f,.76f));
  var red=resources.CreateMaterial("Standard",new Color(.8f,.08f,.06f));
  Part(root.transform,"Lower body",new Vector3(0,.61f,0),new Vector3(1.8f,.48f,3.7f),paint);
  Part(root.transform,"Hood",new Vector3(0,.87f,1.22f),new Vector3(1.72f,.15f,1.1f),paint);
  Part(root.transform,"Cabin",new Vector3(0,1.12f,-.12f),new Vector3(1.55f,.58f,1.94f),glass);
  Part(root.transform,"Roof",new Vector3(0,1.45f,-.16f),new Vector3(1.57f,.12f,1.68f),paint);
  Part(root.transform,"Rear deck",new Vector3(0,.91f,-1.43f),new Vector3(1.72f,.16f,.76f),paint);
  foreach(float side in new[]{-1f,1f}) {
   Part(root.transform,"Window pillar",new Vector3(side*.79f,1.16f,-.17f),new Vector3(.055f,.57f,.09f),paint);
   Part(root.transform,"Door trim",new Vector3(side*.91f,.74f,-.1f),new Vector3(.025f,.07f,2.9f),dark);
   Part(root.transform,"Mirror",new Vector3(side*1.01f,1.04f,.69f),new Vector3(.24f,.16f,.28f),paint);
   foreach(float axle in new[]{-1.22f,1.22f}) {
    var wheel=Part(root.transform,"Tyre",new Vector3(side*.86f,.35f,axle),new Vector3(.7f,.13f,.7f),dark,PrimitiveType.Cylinder);wheel.transform.localRotation=Quaternion.Euler(0,0,90);
    var hub=Part(root.transform,"Wheel hub",new Vector3(side*1.002f,.35f,axle),new Vector3(.39f,.015f,.39f),metal,PrimitiveType.Cylinder);hub.transform.localRotation=Quaternion.Euler(0,0,90);
   }
   Part(root.transform,"Headlamp",new Vector3(side*.61f,.79f,1.86f),new Vector3(.43f,.18f,.035f),lamp);
   Part(root.transform,"Tail lamp",new Vector3(side*.65f,.8f,-1.86f),new Vector3(.34f,.2f,.035f),red);
  }
  Part(root.transform,"Front bumper",new Vector3(0,.49f,1.87f),new Vector3(1.65f,.14f,.08f),metal);
  Part(root.transform,"Grille",new Vector3(0,.72f,1.87f),new Vector3(.62f,.17f,.035f),dark);
  Part(root.transform,"Rear bumper",new Vector3(0,.49f,-1.87f),new Vector3(1.65f,.14f,.08f),metal);
  if(taxi) {
   Part(root.transform,"Taxi roof sign",new Vector3(0,1.62f,-.12f),new Vector3(.72f,.23f,.3f),lamp);
   var label=new GameObject("Taxi lettering");label.transform.SetParent(root.transform,false);label.transform.localPosition=new Vector3(0,1.62f,.038f);
   var text=label.AddComponent<TextMesh>();text.text="TAXI";text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.fontSize=48;text.characterSize=.055f;text.color=new Color(.08f,.15f,.18f);
  }
  return root;
 }
 static GameObject Part(Transform parent,string name,Vector3 position,Vector3 size,Material material,PrimitiveType kind=PrimitiveType.Cube) {
  var part=GameObject.CreatePrimitive(kind);part.name=name;part.transform.SetParent(parent,false);part.transform.localPosition=position;part.transform.localScale=size;part.GetComponent<Renderer>().sharedMaterial=material;
  var collider=part.GetComponent<Collider>();collider.enabled=false;if(Application.isPlaying)Object.Destroy(collider);else Object.DestroyImmediate(collider);return part;
 }
}
}
