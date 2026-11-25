using UnityEngine;

// 2026-11-25 map review: a red-brown bicycle crossing painted beside a crosswalk (자전거횡단도).
// The side is stored in the crosswalk facility's text, because the map content hash is taken over the
// serialized document and a new field would change the stored hash of every earlier revision.
// Local +z of a crosswalk points toward its junction, so "안쪽" is the junction side.
// Paint only: pedestrians, signals and lane permissions do not read it. A crossing with a strip is still fitted
// to the road but never beyond its authored span (GeneratedCrosswalkGeometry.Effective), so trimmed corners stay trimmed.
public static class CrosswalkBikeCrossing {
 public const string Inner="자전거횡단도:안쪽",Outer="자전거횡단도:바깥쪽";
 public const float Depth=2f;
 public static int Side(RoadMapFacility f){if(f==null||f.kind!="crosswalk")return 0;return f.text==Inner?1:f.text==Outer?-1:0;}
 public static void Build(RoadMapFacility f,Transform root,Material white){
  int side=Side(f);if(side==0)return;
  var brown=WorldBuilder.Mat("Bicycle crossing",new Color(.42f,.15f,.11f));
  float centre=side*(f.length*.5f+.15f+Depth*.5f);
  WorldBuilder.Box("자전거횡단도",new Vector3(0,.08f,centre),new Vector3(f.width,.012f,Depth),brown,root,false);
  foreach(float edge in new[]{-1f,1f})for(float x=-f.width*.5f+.2f;x<f.width*.5f-.15f;x+=.8f)
   WorldBuilder.Box("자전거횡단도 경계",new Vector3(x,.086f,centre+edge*(Depth*.5f-.2f)),new Vector3(.4f,.014f,.4f),white,root,false);
 }
}
