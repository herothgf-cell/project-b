using System;
using UnityEngine;
public static class TaxiArtChecks {
 static int count;static void Check(bool v,string why){count++;if(!v)throw new Exception("TAXI_ART "+why);}
 public static void Run(){
  count=0;
  foreach(string name in new[]{"Body","Wheel"}){var prefab=Resources.Load<GameObject>("TaxiConcept/"+name);Check(prefab,"Blender prefab shipped "+name);Check(prefab.GetComponentsInChildren<Collider>().Length==0,"Blender prefab visual only");Check(prefab.GetComponentsInChildren<Renderer>().Length<=8,"material batches bounded");foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>()){var m=filter.sharedMesh;Check(m&&m.vertexCount>0,"imported mesh exists");foreach(var v in m.vertices)Check(!float.IsNaN(v.x)&&!float.IsNaN(v.y)&&!float.IsNaN(v.z)&&!float.IsInfinity(v.x)&&!float.IsInfinity(v.y)&&!float.IsInfinity(v.z),"imported finite vertex");if(name=="Wheel")Check(m.bounds.min.x>=-.341f&&m.bounds.max.x<=.341f&&m.bounds.min.z>=-.341f&&m.bounds.max.z<=.341f,"Blender tyre radius matches physics");}}
  var size=new Vector3(1.79f,.69f,3.96f);var mesh=TaxiArt.RoundedMesh(size,.25f);Check(mesh.vertexCount>100,"curved mesh exists");Check(mesh.bounds.size.x<=size.x+.001f&&mesh.bounds.size.z<=size.z+.001f,"bounds preserve footprint");
  foreach(var n in mesh.normals)Check(!float.IsNaN(n.x)&&Mathf.Abs(n.magnitude-1)<.001f,"finite unit normals");
  var vertices=mesh.vertices;var indices=mesh.triangles;for(int i=0;i<indices.Length;i+=3){var a=vertices[indices[i]];var b=vertices[indices[i+1]];var c=vertices[indices[i+2]];Check(Vector3.Dot(Vector3.Cross(b-a,c-a),mesh.normals[indices[i]])>=-.00001f,"outward triangles");}
  var root=new GameObject("Art contract");try{TaxiArt.Exterior(root.transform);Check(root.GetComponentsInChildren<Collider>().Length==0,"exterior no collision");Transform steering;var cockpit=TaxiArt.Cockpit(root.transform,out steering);Check(steering&&steering.GetComponent<MeshFilter>().sharedMesh.vertexCount>500,"continuous wheel");Check(cockpit.GetComponentsInChildren<Collider>().Length==0,"cockpit no collision");foreach(var t in cockpit.GetComponentsInChildren<Transform>())Check(t.gameObject.layer==10,"cockpit layer");}finally{UnityEngine.Object.DestroyImmediate(root);}
  foreach(var size2 in new[]{new Vector2(1600,900),new Vector2(2133,900)}){var r=CockpitUI.FirstPersonSpeedRect(size2.x,size2.y);Check(!r.Overlaps(new Rect(size2.x-488,size2.y-238,464,214)),"digital clear of passenger");Check(!r.Overlaps(new Rect(24,size2.y-182,288,158)),"digital clear of controls");Check(r.y>size2.y*.75f,"windshield clear");}
  foreach(int width in new[]{1280,1920})foreach(bool pvp in new[]{false,true}){float height=width*9f/16,scale=UiReadability.HudScale(width,height);var tip=CockpitUI.SlidingTipBounds(width,height,false,true,1,pvp,true);Check(!tip.Overlaps(new Rect(width-488*scale,height-238*scale,464*scale,214*scale)),"first person TIP clears passenger");Check(!tip.Overlaps(new Rect(340*scale,height-470*scale,300*scale,140*scale)),"first person notice clears TIP");}
  Debug.Log("TAXI_ART_CHECKS_OK checks="+count);
 }
 public static void Build(){Run();PassengerActionChecks.Build();}
}
