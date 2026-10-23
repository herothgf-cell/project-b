using System;
using System.Linq;
using UnityEngine;
public static class ProjectMapSceneEditorTests {
 static void Require(bool condition,string name){if(!condition)throw new Exception("Scene editor: "+name);Debug.Log("PROJECT_SCENE_EDITOR_PASS "+name);}
 public static void Run(){
  var previous=RoadMapRuntime.Baseline;var host=new GameObject("Scene editor test");
  try{
   RoadMapRuntime.Baseline=new MapData{mapId="fixture",width=200,depth=200,roads=new MapRoad[0],areas=new MapArea[0],stops=new MapStop[0],hdEdges=new HdEdge[0],buildings=new[]{new MapBuilding{id="original",name="Original",height=4,points=new[]{P(10,10),P(20,10),P(20,20),P(10,20)},triangles=new[]{0,1,2,0,2,3}}}};
   var editor=host.AddComponent<RoadMapEditor>();editor.document=new RoadMapDocument{mapId="fixture"};editor.history=new RoadMapEditHistory(editor.document);
   editor.SelectSceneObject("building:original");Require(editor.SceneObject!=null&&editor.document.scene==null,"select baseline without dirty scene");
   editor.TransformSceneObject(new Vector3(5,0,7),0,1);Require(editor.document.scene!=null&&editor.SceneObject.points[0].x==15,"first transform captures scene");
   editor.document=editor.history.Undo(editor.document);Require(editor.document.scene==null&&editor.SceneObject.points[0].x==10,"undo first transform restores original capture");
   editor.document=editor.history.Redo(editor.document);Require(editor.SceneObject.points[0].x==15,"redo first transform");
   var building=editor.CreateSceneObject("building",new Vector3(70,0,70));string id=building.id;int count=editor.document.scene.objects.Length;
   editor.TransformSceneObject(new Vector3(5,0,0),90,2);Require(Mathf.Abs(editor.SceneObject.points.Average(p=>p.x)-75)<.01f,"move rotate scale centre");
   editor.MoveSceneVertex(0,new Vector3(60,0,60));Require(editor.SceneObject.points[0].x==60,"vertex edit");
   editor.AddSceneVertex();Require(editor.SceneObject.points.Length==5,"vertex insertion");
   editor.RemoveSceneVertex();Require(editor.SceneObject.points.Length==4,"vertex deletion");
   editor.DeleteSceneObject();Require(editor.document.scene.objects.Length==count-1&&!editor.document.scene.objects.Any(o=>o.id==id),"delete scene object");
   editor.document=editor.history.Undo(editor.document);editor.SelectSceneObject(id);Require(editor.SceneObject!=null,"undo scene deletion");
   foreach(string kind in new[]{"road","sidewalk","parking","restaurant","rest","water","land"}){
    var o=editor.CreateSceneObject(kind,new Vector3(120,0,120));Require(o.kind==kind&&o.points.Length==(kind=="road"?2:4),"create "+kind);
   }
   editor.CreateSceneObject("parking",new Vector3(100,0,100));editor.SetSceneServicePosition(new Vector3(102,0,100));editor.TransformSceneObject(new Vector3(5,0,0),90,2);
   Require(Mathf.Abs(editor.SceneObject.serviceX-105)<.01f&&Mathf.Abs(editor.SceneObject.serviceZ-96)<.01f,"service marker follows geometry transform");
   editor.document.lanes=new[]{new RoadMapLane{id="approach",points=new[]{P(10,80),P(190,80)}}};
   Require(editor.BindSceneApproach(new Vector3(50,0,80))&&editor.SceneObject.laneId=="approach"&&Mathf.Abs(editor.SceneObject.laneAt-40)<.01f,"click approach binds lane distance");
   var copy=editor.document.Copy();Require(copy.scene.objects.Length==editor.document.scene.objects.Length&&copy.scene.objects.Last().laneId=="approach","scene editor roundtrip");
   Debug.Log("PROJECT_MAP_SCENE_EDITOR_TEST_OK");
  }finally{RoadMapRuntime.Baseline=previous;UnityEngine.Object.DestroyImmediate(host);}
 }
 static MapPoint P(float x,float z){return new MapPoint{x=x,z=z};}
}
