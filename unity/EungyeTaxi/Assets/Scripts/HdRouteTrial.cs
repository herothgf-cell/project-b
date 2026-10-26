using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public class HdRouteTrial : MonoBehaviour {
 // The imported roadside line predates the widened local road surface. Hide
 // only the portion crossed by that surface; longitudinal source lines remain.
 sealed class LocalRoadSurfaceMask {
  const float Cell=20f;
  sealed class Segment {public Vector3 a,b,direction;public float halfWidth,lengthSquared;}
  readonly Dictionary<Vector2Int,List<Segment>> cells=new Dictionary<Vector2Int,List<Segment>>();
  static int Grid(float coordinate){return Mathf.FloorToInt(coordinate/Cell);}
  public LocalRoadSurfaceMask(MapData map){
   foreach(var road in map.roads??new MapRoad[0]){
    if(road.suppressPaint||road.points==null||road.points.Length<2||road.width<=0||
       (road.id??"").StartsWith("fuel:")||(road.id??"").StartsWith("polish:"))continue;
    for(int i=1;i<road.points.Length;i++){
     var a=road.points[i-1].V();var b=road.points[i].V();var delta=b-a;delta.y=0;
     if(delta.sqrMagnitude<.001f)continue;
     var segment=new Segment{a=a,b=b,direction=delta.normalized,halfWidth=road.width*.5f,lengthSquared=delta.sqrMagnitude};
     float padding=segment.halfWidth+.15f;
     for(int x=Grid(Mathf.Min(a.x,b.x)-padding);x<=Grid(Mathf.Max(a.x,b.x)+padding);x++)
      for(int z=Grid(Mathf.Min(a.z,b.z)-padding);z<=Grid(Mathf.Max(a.z,b.z)+padding);z++){
       var key=new Vector2Int(x,z);List<Segment> list;if(!cells.TryGetValue(key,out list)){list=new List<Segment>();cells.Add(key,list);}list.Add(segment);
      }
    }
   }
  }
  public bool CoversTransverse(Vector3 point,Vector3 tangent){
   List<Segment> list;if(!cells.TryGetValue(new Vector2Int(Grid(point.x),Grid(point.z)),out list))return false;
   foreach(var road in list){
    if(Mathf.Abs(Vector3.Dot(tangent,road.direction))>=.85f)continue;
    var to=point-road.a;float at=(to.x*(road.b.x-road.a.x)+to.z*(road.b.z-road.a.z))/road.lengthSquared;
    if(at<0||at>1)continue;
    var nearest=Vector3.Lerp(road.a,road.b,at);if(Mathf.Abs(point.y-nearest.y)>1f)continue;
    float dx=point.x-nearest.x,dz=point.z-nearest.z,limit=road.halfWidth+.15f;
    if(dx*dx+dz*dz<=limit*limit)return true;
   }
   return false;
  }
 }
 // An authored crosswalk supersedes source paint in its striped band. On
 // its junction side, old source paint is hidden only where reviewed lanes
 // now drive; outside lane edges and upstream source lines remain visible.
 sealed class AuthoredJunctionPaintMask {
  sealed class Zone {
   public Vector3 crossing,junction,toward,across;public float halfWidth,halfDepth,reach,coreRadius;
   public bool Crosswalk(Vector3 p){var delta=p-crossing;return Mathf.Abs(Vector3.Dot(delta,toward))<=halfDepth+.15f&&Mathf.Abs(Vector3.Dot(delta,across))<=halfWidth+.15f;}
   public bool JunctionSide(Vector3 p){var delta=p-crossing;float along=Vector3.Dot(delta,toward);
    return along>=-halfDepth-.15f&&along<=reach+.5f&&Mathf.Abs(Vector3.Dot(delta,across))<=halfWidth+1f;
   }
   public bool Core(Vector3 p){if(Vector3.Dot(p-crossing,toward)<-halfDepth-.15f)return false;
    var delta=p-junction;return delta.x*delta.x+delta.z*delta.z<=coreRadius*coreRadius;
   }
   public bool NearBox(float minX,float maxX,float minZ,float maxZ){float margin=Mathf.Max(coreRadius,halfWidth)+2f;
    return maxX>=Mathf.Min(crossing.x,junction.x)-margin&&minX<=Mathf.Max(crossing.x,junction.x)+margin&&
           maxZ>=Mathf.Min(crossing.z,junction.z)-margin&&minZ<=Mathf.Max(crossing.z,junction.z)+margin;
   }
  }
  struct LaneSpan {public Vector3 a,b;public float interior;}
  readonly List<Zone> zones=new List<Zone>();readonly List<LaneSpan> lanes=new List<LaneSpan>();
  public AuthoredJunctionPaintMask(MapData map){
   if(map.editorDocument==null||map.editorDocument.facilities==null||map.junctionRules==null)return;
   foreach(var crossing in map.editorDocument.facilities){if(crossing==null||crossing.kind!="crosswalk"||string.IsNullOrEmpty(crossing.junctionId)||crossing.width<=0)continue;
    HybridJunctionRule junction=null;foreach(var rule in map.junctionRules)if(rule.id==crossing.junctionId){junction=rule;break;}if(junction==null)continue;
    var forward=Quaternion.Euler(0,crossing.yaw,0)*Vector3.forward;var toward=junction.V()-crossing.Position;toward.y=0;
    if(toward.sqrMagnitude<.1f)continue;forward.y=0;forward.Normalize();if(Vector3.Dot(forward,toward)<0)forward=-forward;
    zones.Add(new Zone{crossing=crossing.Position,junction=junction.V(),toward=forward,across=TrafficSystem.Right(forward),
     halfWidth=crossing.width*.5f,halfDepth=Mathf.Max(.2f,crossing.length*.5f),reach=Vector3.Dot(toward,forward),coreRadius=18f});
   }
   if(zones.Count==0)return;
   var widths=new Dictionary<string,float>();if(map.editorDocument.lanes!=null)foreach(var lane in map.editorDocument.lanes)
    if(lane!=null&&!string.IsNullOrEmpty(lane.id))widths[lane.id]=Mathf.Max(.5f,lane.width);
   foreach(var lane in map.hdEdges??new HdEdge[0]){if(lane.disabled||lane.points==null||lane.points.Length<2)continue;
    float width;if(!widths.TryGetValue(lane.id??"",out width))width=3.5f;float interior=width*.5f-.15f;if(interior<=0)continue;
    for(int i=1;i<lane.points.Length;i++){Vector3 a=lane.points[i-1].V(),b=lane.points[i].V();
     if((b-a).sqrMagnitude<.001f)continue;float minX=Mathf.Min(a.x,b.x),maxX=Mathf.Max(a.x,b.x),minZ=Mathf.Min(a.z,b.z),maxZ=Mathf.Max(a.z,b.z);
     if(zones.Exists(zone=>zone.NearBox(minX,maxX,minZ,maxZ)))lanes.Add(new LaneSpan{a=a,b=b,interior=interior});
    }
   }
  }
  public bool MightAffect(HdMark mark){if(zones.Count==0)return false;float minX=float.MaxValue,maxX=float.MinValue,minZ=float.MaxValue,maxZ=float.MinValue;
   foreach(var p in mark.points){minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);minZ=Mathf.Min(minZ,p.z);maxZ=Mathf.Max(maxZ,p.z);}
   return zones.Exists(zone=>zone.NearBox(minX,maxX,minZ,maxZ));
  }
  bool LaneInterior(Vector3 p){foreach(var lane in lanes){var delta=lane.b-lane.a;float length=delta.x*delta.x+delta.z*delta.z;if(length<.001f)continue;
    float at=Mathf.Clamp01(((p.x-lane.a.x)*delta.x+(p.z-lane.a.z)*delta.z)/length);var nearest=Vector3.Lerp(lane.a,lane.b,at);
    if(Mathf.Abs(p.y-nearest.y)>1f)continue;float dx=p.x-nearest.x,dz=p.z-nearest.z;if(dx*dx+dz*dz<lane.interior*lane.interior)return true;
   }return false;
  }
  public bool Covers(Vector3 p){foreach(var zone in zones)if(zone.Crosswalk(p))return true;
   foreach(var zone in zones)if((zone.JunctionSide(p)||zone.Core(p))&&LaneInterior(p))return true;
   return false;
  }
 }
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
  var roadsideMask=new LocalRoadSurfaceMask(map);var authoredMask=new AuthoredJunctionPaintMask(map);
  var drawn=new HashSet<string>();
  foreach(var mark in map.hdMarks??new HdMark[0]){
   if(mark.points==null||mark.points.Length<2)continue;
   string forward=MarkKey(mark,false),reverse=MarkKey(mark,true);if(!drawn.Add(string.CompareOrdinal(forward,reverse)<0?forward:reverse))continue;
   int color=mark.type.StartsWith("1")?1:mark.type.StartsWith("3")?2:0;
   var ps=new Vector3[mark.points.Length];for(int i=0;i<ps.Length;i++)ps[i]=mark.points[i].V(.075f);
   var path=new AssistedRoadNetwork.Path(ps);bool dashed=mark.type=="112"||mark.type=="212"||mark.type=="312";
   bool roadside=mark.kind=="505"||mark.kind=="515";
   bool editedYellow=color==1&&(mark.kind=="501"||roadside)&&authoredMask.MightAffect(mark);
   float step=roadside||editedYellow ? .5f : dashed ? 6 : 2,paint=roadside||editedYellow ? .5f : dashed ? 3 : 2.05f;
   for(float d=0;d<path.length;d+=step){if((roadside||editedYellow)&&dashed&&d%6>=3)continue;var a=path.Position(d);var b=path.Position(Mathf.Min(path.length,d+paint));if((b-a).sqrMagnitude<.0001f)continue;
    if(editedYellow&&authoredMask.Covers((a+b)*.5f))continue;
    if(roadside&&roadsideMask.CoversTransverse((a+b)*.5f,(b-a).normalized))continue;
    var right=TrafficSystem.Right((b-a).normalized);int strokes=mark.type=="121"?2:1;
    for(int stroke=0;stroke<strokes;stroke++){float offset=strokes==2?(stroke==0?-.11f:.11f):0;float half=strokes==2?.055f:.075f;var shift=right*offset;var side=right*half;int k=vertices[color].Count;
     vertices[color].AddRange(new[]{a+shift-side,b+shift-side,b+shift+side,a+shift+side});triangles[color].AddRange(new[]{k,k+1,k+2,k,k+2,k+3});
    }
   }
  }
  for(int c=0;c<3;c++){var mesh=new Mesh();mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.SetVertices(vertices[c]);mesh.SetTriangles(triangles[c],0);mesh.RecalculateNormals();var g=new GameObject("원본 노면선 "+c);g.transform.SetParent(parent);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=new[]{white,yellow,blue}[c];}
 }
 static string MarkKey(HdMark mark,bool reverse){
  var key=new StringBuilder(mark.type).Append('|').Append(mark.kind).Append('|');
  for(int i=0;i<mark.points.Length;i++){var point=mark.points[reverse?mark.points.Length-1-i:i];key.Append(point.x.ToString("R",CultureInfo.InvariantCulture)).Append(',').Append(point.z.ToString("R",CultureInfo.InvariantCulture)).Append(';');}
  return key.ToString();
 }
}
