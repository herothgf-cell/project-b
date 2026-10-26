using System;
using System.Linq;
using UnityEngine;

public static class ImportedMarkingRendererTests {
 static void Check(bool pass,string message){if(!pass)throw new Exception("IMPORTED_MARKING_RENDERER: "+message);}
 static HdMark Mark(string id,string type,bool reverse){return new HdMark{id=id,type=type,kind="506",points=reverse?new[]{new MapPoint{x=10,z=0},new MapPoint{x=0,z=0}}:new[]{new MapPoint{x=0,z=0},new MapPoint{x=10,z=0}}};}
 static int Vertices(params HdMark[] marks){var root=new GameObject("marking test");try{HdRouteTrial.DrawMarkings(new MapData{hdMarks=marks},root.transform);var child=root.transform.Find("원본 노면선 1");return child.GetComponent<MeshFilter>().sharedMesh.vertexCount;}finally{UnityEngine.Object.DestroyImmediate(root);}}
 static Vector3[] YellowVertices(MapData map){var root=new GameObject("roadside marking test");try{HdRouteTrial.DrawMarkings(map,root.transform);return root.transform.Find("원본 노면선 1").GetComponent<MeshFilter>().sharedMesh.vertices;}finally{UnityEngine.Object.DestroyImmediate(root);}}
 static int QuadsNear(Vector3[] vertices,Vector3 point,float radius){int count=0;for(int i=0;i<vertices.Length;i+=4){var centre=(vertices[i]+vertices[i+1]+vertices[i+2]+vertices[i+3])*.25f;if(Vector3.Distance(centre,point)<radius)count++;}return count;}
 static bool SpansX(Vector3[] vertices,float x){for(int i=0;i<vertices.Length;i+=4){float min=float.MaxValue,max=float.MinValue;for(int j=0;j<4;j++){min=Mathf.Min(min,vertices[i+j].x);max=Mathf.Max(max,vertices[i+j].x);}if(min<x&&max>x)return true;}return false;}
 public static void Run(){
  int single=Vertices(Mark("single","111",false));
  Check(single==20,"single solid control uses five strips");
  Check(Vertices(Mark("double","121",false))==2*single,"121 draws two separate solid strokes");
  Check(Vertices(Mark("first","111",false),Mark("reverse","111",true))==single,"reverse duplicate geometry is rendered once");
  var roadside=new HdMark{id="roadside",type="111",kind="515",points=new[]{new MapPoint{x=-20,z=0},new MapPoint{x=20,z=0}}};
  var crossing=new MapRoad{id="local:cross",width=10,points=new[]{new MapPoint{x=0,z=-20},new MapPoint{x=0,z=20}}};
  var clipped=YellowVertices(new MapData{roads=new[]{crossing},hdMarks=new[]{roadside}});
  Check(clipped.Length>0&&QuadsNear(clipped,new Vector3(-15,0,0),2)>0&&QuadsNear(clipped,new Vector3(15,0,0),2)>0,"transverse roadside marking remains on both sides of crossing road");
  Check(QuadsNear(clipped,Vector3.zero,4.5f)==0,"transverse roadside marking is absent from local drivable surface");
  roadside.kind="501";Check(QuadsNear(YellowVertices(new MapData{roads=new[]{crossing},hdMarks=new[]{roadside}}),Vector3.zero,2)>0,"imported 501 centerline survives a crossing road");
  roadside.kind="515";roadside.points=new[]{new MapPoint{x=5,z=-20},new MapPoint{x=5,z=20}};
  Check(QuadsNear(YellowVertices(new MapData{roads=new[]{crossing},hdMarks=new[]{roadside}}),new Vector3(5,0,0),2)>0,"parallel roadside marking survives beside its road");
  roadside.kind="505";roadside.points=new[]{new MapPoint{x=-20,z=0},new MapPoint{x=20,z=0}};
  Check(QuadsNear(YellowVertices(new MapData{roads=new[]{crossing},hdMarks=new[]{roadside}}),Vector3.zero,4.5f)==0,"road-edge kind 505 clips at a transverse local crossing");
  roadside.kind="515";roadside.type="112";var dashed=YellowVertices(new MapData{roads=new[]{crossing},hdMarks=new[]{roadside}});
  Check(!SpansX(dashed,4.5f)&&SpansX(dashed,6)&&!SpansX(dashed,8.5f),"dashed 515 clips its road-facing end and keeps the source dash pattern");
  var actual=ProjectMapStore.Load(ProjectMapBuild.Root).baseline;
  var realRoad=actual.roads.First(r=>r.id=="37612023:A0020000:232:0");
  var realMarks=actual.hdMarks.Where(m=>m.id=="B2218G610306"||m.id=="B2218G610454").ToArray();
  Check(realMarks.Length==2,"J10580 imported kind-515 source marks exist");
  var actualVertices=YellowVertices(new MapData{roads=new[]{realRoad},hdMarks=realMarks});
  Check(QuadsNear(actualVertices,new Vector3(1066.10f,0,162.93f),1.5f)==0&&QuadsNear(actualVertices,new Vector3(1051.55f,0,123.94f),1.5f)==0,"J10580 transverse 515 paint is absent from composed local approach");
  var north=realMarks.First(m=>m.id=="B2218G610306");var south=realMarks.First(m=>m.id=="B2218G610454");
  Check(QuadsNear(actualVertices,north.points[0].V(),2)>0&&QuadsNear(actualVertices,south.points[south.points.Length-1].V(),2)>0,"J10580 source roadside fragments outside the crossing survive");
  Debug.Log("IMPORTED_MARKING_RENDERER_TEST_OK");
 }
}
