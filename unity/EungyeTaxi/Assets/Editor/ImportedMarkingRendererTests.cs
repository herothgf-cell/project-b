using System;
using UnityEngine;

public static class ImportedMarkingRendererTests {
 static void Check(bool pass,string message){if(!pass)throw new Exception("IMPORTED_MARKING_RENDERER: "+message);}
 static HdMark Mark(string id,string type,bool reverse){return new HdMark{id=id,type=type,kind="506",points=reverse?new[]{new MapPoint{x=10,z=0},new MapPoint{x=0,z=0}}:new[]{new MapPoint{x=0,z=0},new MapPoint{x=10,z=0}}};}
 static int Vertices(params HdMark[] marks){var root=new GameObject("marking test");try{HdRouteTrial.DrawMarkings(new MapData{hdMarks=marks},root.transform);var child=root.transform.Find("원본 노면선 1");return child.GetComponent<MeshFilter>().sharedMesh.vertexCount;}finally{UnityEngine.Object.DestroyImmediate(root);}}
 public static void Run(){
  int single=Vertices(Mark("single","111",false));
  Check(single==20,"single solid control uses five strips");
  Check(Vertices(Mark("double","121",false))==2*single,"121 draws two separate solid strokes");
  Check(Vertices(Mark("first","111",false),Mark("reverse","111",true))==single,"reverse duplicate geometry is rendered once");
  Debug.Log("IMPORTED_MARKING_RENDERER_TEST_OK");
 }
}
