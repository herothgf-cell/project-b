using UnityEngine;
using System.Collections.Generic;

public class HdRouteTrial : MonoBehaviour {
 public DriveSession session;
 public int selection;
 public bool finished;
 GameObject marker;GUIStyle style;
 public static bool Enabled {get{var a=System.Environment.GetCommandLineArgs();return System.Array.IndexOf(a,"-route42")>=0||System.Array.IndexOf(a,"-hdTest")>=0;}}
 void Start(){
  session.jobs.enabled=false;session.manners.enabled=false;session.roadConditions.enabled=false;
  marker=WorldBuilder.Box("시험 도착 정차",Vector3.zero,new Vector3(3,.03f,3),WorldBuilder.Mat("HD goal",new Color(.2f,.9f,.4f)),transform,false);
  Select(0);
 }
 public void Select(int index){
  if(Time.timeScale==0)return;selection=index;finished=false;
  var test=session.world.map.hdTrials[index];var graph=session.jobs.nav.hd;
  session.assisted.SetActive(false);var taxi=session.taxi;var p=graph.paths[test.startEdge];
  var position=p.Position(test.startAt)+Vector3.up*.65f;var rotation=Quaternion.LookRotation(p.Tangent(test.startAt));
  taxi.SetSpawn(position,rotation);taxi.ResetTaxi();session.jobs.nav.SetHdDestination(test.goalEdge,test.goalAt);
  marker.transform.position=session.jobs.nav.destination+Vector3.up*.09f;
  session.signals.Clear();
 }
 void Update(){
  if(Time.timeScale==0)return;
  for(int i=0;i<5;i++)if(Input.GetKeyDown(KeyCode.F6+i))Select(i);
  if(Input.GetKeyDown(KeyCode.R))session.jobs.nav.SetHdDestination(session.world.map.hdTrials[selection].goalEdge,session.world.map.hdTrials[selection].goalAt);
  if(Vector3.Distance(session.taxi.transform.position,session.jobs.nav.destination+Vector3.up*.65f)<3&&session.taxi.body.velocity.magnitude<.56f)finished=true;
 }
 void OnGUI(){
  if(style==null)style=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=15,wordWrap=true};
  float y=Screen.height-205;GUI.Box(new Rect(16,y,790,140),GUIContent.none);
  GUI.Label(new Rect(28,y+5,760,25),"국도 42호선 실제 차로 · "+session.world.map.hdTrials[selection].name+(finished?" · 정차 완료":""),style);
  for(int i=0;i<5;i++)if(GUI.Button(new Rect(28+i*151,y+36,145,30),"F"+(6+i)+" "+(i==4?"차로 변경":(i<2?"A ":"B ")+(i%2==0?"좌회전":"우회전"))))Select(i);
  GUI.Label(new Rect(28,y+72,760,55),"H: 내비 경로 보조 조향 · W/S/Space: 직접 가감속 · R: 현재 시험 재시작\n초록 도착점에 직접 정차하세요. 곁도로 자료는 회전 직후 끝납니다. 신호·일반 차량 운영은 이 시험에서 제외됩니다.",style);
 }
 public static void DrawMarkings(MapData map,Transform parent){
  var white=WorldBuilder.Mat("HD white",new Color(.95f,.94f,.87f));var yellow=WorldBuilder.Mat("HD yellow",new Color(1,.72f,.1f));var blue=WorldBuilder.Mat("HD blue",new Color(.12f,.4f,1));
  var vertices=new[]{new List<Vector3>(),new List<Vector3>(),new List<Vector3>()};var triangles=new[]{new List<int>(),new List<int>(),new List<int>()};
  foreach(var mark in map.hdMarks??new HdMark[0]){
   if(mark.points.Length<2)continue;int color=mark.type.StartsWith("1")?1:mark.type.StartsWith("3")?2:0;
   var ps=new Vector3[mark.points.Length];for(int i=0;i<ps.Length;i++)ps[i]=mark.points[i].V(.075f);
   var path=new AssistedRoadNetwork.Path(ps);bool dashed=mark.type=="112"||mark.type=="212"||mark.type=="312";
   float step=dashed?6:2,paint=dashed?3:2.05f;
   for(float d=0;d<path.length;d+=step){var a=path.Position(d);var b=path.Position(Mathf.Min(path.length,d+paint));var side=TrafficSystem.Right((b-a).normalized)*.075f;int k=vertices[color].Count;
    vertices[color].AddRange(new[]{a-side,b-side,b+side,a+side});triangles[color].AddRange(new[]{k,k+1,k+2,k,k+2,k+3});
   }
  }
  for(int c=0;c<3;c++){var mesh=new Mesh();mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.SetVertices(vertices[c]);mesh.SetTriangles(triangles[c],0);mesh.RecalculateNormals();var g=new GameObject("원본 노면선 "+c);g.transform.SetParent(parent);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=new[]{white,yellow,blue}[c];}
 }
}
