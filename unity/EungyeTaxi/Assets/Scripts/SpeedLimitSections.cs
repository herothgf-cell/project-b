using System.Collections.Generic;
using UnityEngine;

// 2026-11-24 play-test round 3: the speed limit was only a badge beside the speedometer, so the driver could
// not see where a limited section begins and ends. The same table rule as DrivingLaw (named roads in
// eungye-speed-limits.json and the school-zone reach) is sampled along every lane of the road graph:
//  - segments: the limited stretches, drawn on the full map and the minimap;
//  - marks: where the limit changes in the driving direction, used for road-side signs.
public sealed class SpeedLimitSections {
 public struct Segment {public Vector3 a,b;public float kmh;public bool school;}
 public struct Mark {public Vector3 position,forward;public float from,to;public bool school;}
 public readonly List<Segment> segments=new List<Segment>();
 public readonly List<Mark> marks=new List<Mark>();
 // One sign per road side: lanes of the same direction share it, and it stands right of the outer lane.
 public readonly List<Mark> signs=new List<Mark>();
 public const float Step=6;
 static SpeedLimitSections cached;static HdRoadGraph cachedGraph;static string cachedKey;
 public static readonly Color SchoolColor=new Color(.86f,.13f,.13f),RoadColor=new Color(1f,.50f,.05f);

 public static SpeedLimitSections For(TrafficSystem traffic){
  var graph=traffic?traffic.LimitGraph:null;if(graph==null)return null;
  string key=RoadSpeedLimits.EstimatedRoads+"|"+RoadSpeedLimits.ZoneRadius+"|"+RoadSpeedLimits.DefaultKmh+"|"+RoadSpeedLimits.AlleyKmh;
  if(cached!=null&&cachedGraph==graph&&cachedKey==key)return cached;
  cached=Build(traffic,graph);cachedGraph=graph;cachedKey=key;return cached;
 }

 static List<Vector3> Samples(Vector3[] points){
  var list=new List<Vector3>();if(points==null||points.Length==0)return list;list.Add(points[0]);float carry=0;
  for(int i=1;i<points.Length;i++){var a=points[i-1];var b=points[i];float length=Vector3.Distance(a,b);float at=Step-carry;
   while(at<length){list.Add(Vector3.Lerp(a,b,at/length));at+=Step;}carry=length-(at-Step);}
  if((list[list.Count-1]-points[points.Length-1]).sqrMagnitude>.25f)list.Add(points[points.Length-1]);return list;
 }
 static bool School(float kmh,Vector3 p){var zone=RoadSpeedLimits.ZoneAt(p.x,p.z);return zone!=null&&kmh>0&&Mathf.Abs(zone.kmh-kmh)<.5f;}

 static SpeedLimitSections Build(TrafficSystem traffic,HdRoadGraph graph){
  var s=new SpeedLimitSections();int n=graph.edges.Length;var starts=new float[n];var ends=new float[n];var used=new bool[n];
  var heads=new Vector3[n];var headDir=new Vector3[n];
  for(int e=0;e<n;e++){
   var edge=graph.edges[e];if(edge==null||edge.TraversalDisabled||graph.paths[e]==null)continue;
   var p=Samples(graph.paths[e].points);if(p.Count<2)continue;used[e]=true;
   var limits=new float[p.Count];for(int k=0;k<p.Count;k++)limits[k]=traffic.TableLimit(e,p[k]);
   starts[e]=limits[0];ends[e]=limits[p.Count-1];heads[e]=p[0];headDir[e]=Flat(p[1]-p[0]);
   for(int k=1;k<p.Count;k++){
    var mid=(p[k-1]+p[k])*.5f;float kmh=traffic.TableLimit(e,mid);
    if(kmh>0){bool school=School(kmh,mid);int last=s.segments.Count-1;
     if(last>=0&&k>1&&s.segments[last].b==p[k-1]&&s.segments[last].kmh==kmh&&s.segments[last].school==school&&Vector3.Angle(s.segments[last].b-s.segments[last].a,p[k]-p[k-1])<4){var merged=s.segments[last];merged.b=p[k];s.segments[last]=merged;}
     else s.segments.Add(new Segment{a=p[k-1],b=p[k],kmh=kmh,school=school});}
    if(limits[k]!=limits[k-1])s.marks.Add(new Mark{position=p[k],forward=Flat(p[k]-p[k-1]),from=limits[k-1],to=limits[k],school=School(limits[k],p[k])||School(limits[k-1],p[k-1])});
   }
  }
  // Edge joints: compare the start of each lane with the end of every lane that leads into it.
  for(int e=0;e<n;e++){var next=graph.edges[e]==null?null:graph.edges[e].next;if(!used[e]||next==null)continue;
   foreach(int j in next){if(j<0||j>=n||!used[j]||ends[e]==starts[j])continue;s.marks.Add(new Mark{position=heads[j],forward=headDir[j],from=ends[e],to=starts[j],school=School(starts[j],heads[j])||School(ends[e],heads[j])});}}
  // Group lanes of one direction; keep the outermost (right-most) lane's mark for the sign.
  var taken=new bool[s.marks.Count];
  for(int i=0;i<s.marks.Count;i++){if(taken[i])continue;var best=s.marks[i];taken[i]=true;var right=Right(best.forward);
   for(int k=i+1;k<s.marks.Count;k++){if(taken[k])continue;var m=s.marks[k];if(m.to!=best.to||m.from!=best.from||Vector3.Dot(m.forward,best.forward)<.85f||Vector3.Distance(m.position,best.position)>22)continue;taken[k]=true;if(Vector3.Dot(m.position-best.position,right)>0)best=m;}
   s.signs.Add(best);}
  return s;
 }
 public static Vector3 Flat(Vector3 v){v.y=0;return v.sqrMagnitude>1e-6f?v.normalized:Vector3.forward;}
 public static Vector3 Right(Vector3 forward){return new Vector3(forward.z,0,-forward.x);}
 public static Color ColorOf(Segment s){return s.school?SchoolColor:RoadColor;}

 // Map overlay: limited stretches as coloured lines under the route, and a small limit disc where a section starts.
 // cameras/cameraSize: a start disc already shown by a speed camera icon with the same limit is skipped (2026-11-26).
 public static void DrawMap(TrafficSystem traffic,System.Func<Vector3,Vector2> project,Rect clip,float width,bool discs,List<SpeedCameraMap.Group> cameras=null,float cameraSize=0){
  if(Event.current==null||Event.current.type!=EventType.Repaint)return;var s=For(traffic);if(s==null)return;
  var grown=new Rect(clip.x-20,clip.y-20,clip.width+40,clip.height+40);
  foreach(var seg in s.segments){var a=project(seg.a);var b=project(seg.b);if(!grown.Contains(a)&&!grown.Contains(b))continue;var c=ColorOf(seg);c.a=.8f;HudGlyph.MapLine(a,b,c,width,clip);}
  if(!discs)return;var placed=new List<Vector2>();
  foreach(var m in s.signs){if(m.to<=0)continue;var p=project(m.position);if(!clip.Contains(p)||SpeedCameraMap.Covers(cameras,p,m.to,cameraSize))continue;bool near=false;foreach(var q in placed)if((q-p).sqrMagnitude<26*26){near=true;break;}if(near)continue;placed.Add(p);Disc(new Rect(p.x-11,p.y-11,22,22),m.to);}
 }
 static GUIStyle discText;
 public static void Disc(Rect r,float kmh){
  SpeedLimitBadge.DrawDisc(r,SchoolColor,Color.white);
  if(discText==null){discText=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold,padding=new RectOffset(0,0,0,0)};}
  discText.fontSize=Mathf.RoundToInt(r.height*.45f);discText.normal.textColor=new Color(.08f,.09f,.12f);GUI.Label(r,kmh.ToString("0"),discText);
 }
}

// Road-side signs for the sections above, plus a short notice when the limit changes.
public sealed class SpeedLimitSigns:MonoBehaviour {
 public DriveSession session;
 bool built;int attempts;public int SignCount {get;private set;}
 float shownLimit=-1,pendingLimit=-1,pendingSince,noticeUntil;string notice="";bool noticeSchool,noticeEnd;
 public string Notice {get{return Time.unscaledTime<noticeUntil?notice:"";}}
 void Update(){
  if(!built&&session&&session.traffic&&attempts<300){attempts++;var s=SpeedLimitSections.For(session.traffic);if(s!=null){Build(s);built=true;}}
  if(!session||!session.law||!session.taxi)return;float limit=session.law.CurrentLimit;
  if(limit!=pendingLimit){pendingLimit=limit;pendingSince=Time.unscaledTime;}
  // A lane switch at a junction can flicker the value for a frame; announce only a value held for half a second.
  if(pendingLimit!=shownLimit&&Time.unscaledTime-pendingSince>=.5f){if(shownLimit>=0)Announce(shownLimit,pendingLimit);shownLimit=pendingLimit;}
 }
 void Announce(float from,float to){
  var p=session.taxi.transform.position;var zone=RoadSpeedLimits.ZoneAt(p.x,p.z);noticeSchool=to>0&&zone!=null&&Mathf.Abs(zone.kmh-to)<.5f;noticeEnd=to<=0;
  notice=to<=0?"제한속도 구간 끝":noticeSchool?"어린이보호구역 시작 · 제한 "+to.ToString("0"):from>0?"제한속도 "+from.ToString("0")+" → "+to.ToString("0"):"제한속도 "+to.ToString("0")+" 구간 시작";
  noticeUntil=Time.unscaledTime+3.5f;
 }
 void OnGUI(){
  string text=Notice;if(text.Length==0||!session||session.paused||session.FullMapOpen)return;
  var prior=GUI.matrix;float scale=CockpitUI.Scale;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
  try{float w=Screen.width/scale,h=Screen.height/scale;var r=new Rect(w/2-190,h-262,380,52);
   CasualUI.RoundedFill(r,noticeEnd?new Color(.20f,.27f,.36f,.94f):noticeSchool?new Color(.62f,.08f,.08f,.95f):DrivingHudArt.Navy);
   if(noticeEnd)SpeedLimitBadge.DrawDisc(new Rect(r.x+10,r.y+7,38,38),new Color(.6f,.64f,.7f),Color.white);else SpeedLimitSections.Disc(new Rect(r.x+10,r.y+7,38,38),shownLimit);
   GUI.Label(new Rect(r.x+58,r.y,r.width-66,r.height),text,DrivingHudArt.Text(19,Color.white,true));
  }finally{GUI.matrix=prior;}
 }
 // 2026-11-24 play-test round 5: signs stood on crosswalks and in the carriageway (a fixed 3.1 m from the lane
 // centre), and the red school-zone paint is gone. Each sign now looks for a sidewalk spot beside the section
 // start, clear of junction crosswalks and OSM crossings, and is skipped when there is none.
 public readonly List<Vector3> Placed=new List<Vector3>(),PlacedForward=new List<Vector3>();public readonly List<SpeedLimitSections.Mark> PlacedMarks=new List<SpeedLimitSections.Mark>();public int Skipped {get;private set;}
 void Build(SpeedLimitSections s){
  var root=new GameObject("제한속도 구간 표지").transform;root.SetParent(transform,false);
  var m=new SignMaterials{
   red=WorldBuilder.Mat("규제표지 적색",new Color(.725f,.125f,.13f)),white=WorldBuilder.Mat("표지 백색",new Color(.92f,.92f,.92f)),
   ink=WorldBuilder.Mat("표지 흑색",new Color(.15f,.15f,.15f)),blue=WorldBuilder.Mat("지시표지 청색",new Color(0f,.286f,.545f)),
   pole=WorldBuilder.Mat("표지 지주",new Color(.62f,.64f,.66f))};
  var map=session&&session.world?session.world.map:null;
  foreach(var mark in s.signs){Vector3 at;if(!Spot(map,session.traffic,mark,out at)){Skipped++;continue;}Sign(root,mark,at,m);Placed.Add(at);PlacedForward.Add(mark.forward);PlacedMarks.Add(mark);SignCount++;}
 }
 struct SignMaterials {public Material red,white,ink,blue,pole;}
 public static bool Sidewalk(MapData map,Vector3 p){
  if(map==null)return true;
  if(RoadsidePlacement.OnRoad(map,p,.35f)||RoadsidePlacement.InBuilding(map,p))return false;
  return !ProjectMapScene.Active(map)||ProjectMapScene.OnArea(map,p,true)&&ProjectMapScene.OnArea(map,p+Vector3.forward*.25f,true)&&ProjectMapScene.OnArea(map,p-Vector3.forward*.25f,true)&&ProjectMapScene.OnArea(map,p+Vector3.right*.25f,true)&&ProjectMapScene.OnArea(map,p-Vector3.right*.25f,true);
 }
 public static bool ClearOfCrossings(MapData map,TrafficSystem traffic,Vector3 p,Vector3 forward){
  if(traffic)foreach(var j in traffic.junctions){if(j==null||j.halfWidth==null||j.halfWidth.Length<2)continue;float reach=Mathf.Max(j.halfWidth[0],j.halfWidth[1])+TrafficJunction.CrosswalkCentreOffset+TrafficJunction.CrosswalkDepth+3;var d=p-j.center;d.y=0;if(d.sqrMagnitude<reach*reach)return false;}
  if(map!=null&&map.osmCrossings!=null)foreach(var c in map.osmCrossings){var d=p-new Vector3(c.x,p.y,c.z);float half=Mathf.Max(2,c.width*.5f)+2;if(Mathf.Abs(Vector3.Dot(d,forward))<half&&d.magnitude<25)return false;}
  return true;
 }
 // Just past the change point, on the right-hand sidewalk; then a little further on or just before it.
 public static bool Spot(MapData map,TrafficSystem traffic,SpeedLimitSections.Mark mark,out Vector3 at){
  var right=SpeedLimitSections.Right(mark.forward);
  foreach(float along in new[]{3f,7f,11f,15f,20f,25f,30f,-3f,-7f}){
   if(!ClearOfCrossings(map,traffic,mark.position+mark.forward*along,mark.forward))continue;
   for(float side=2f;side<=16f;side+=.5f){var p=mark.position+mark.forward*along+right*side;if(!Sidewalk(map,p)||!ClearOfCrossings(map,traffic,p,mark.forward))continue;
    // Step a little further from the kerb so the plate does not hang over the carriageway.
    var q=p+right*.6f;if(Sidewalk(map,q)&&ClearOfCrossings(map,traffic,q,mark.forward))p=q;at=Floor(p);return true;}
  }
  at=default(Vector3);return false;
 }
 static Vector3 Floor(Vector3 p){RaycastHit hit;if(Physics.Raycast(p+Vector3.up*3f,Vector3.down,out hit,6f,~(1<<8),QueryTriggerInteraction.Ignore)&&!hit.collider.attachedRigidbody)p.y=hit.point.y;return p;}
 static GameObject Disc(string name,Transform parent,Vector3 local,float diameter,float depth,Material mat){
  var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=name;Destroy(g.GetComponent<Collider>());g.transform.SetParent(parent,false);g.transform.localPosition=local;g.transform.localRotation=Quaternion.Euler(90,0,0);g.transform.localScale=new Vector3(diameter,depth,diameter);g.GetComponent<Renderer>().sharedMaterial=mat;return g;
 }
 static GameObject Plate(string name,Transform parent,Vector3 local,Vector3 size,Material mat){return WorldBuilder.Box(name,local,size,mat,parent,false);}
 static void Words(Transform parent,string name,Vector3 local,string text,Color color,float width,float height){
  var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=local;var mesh=g.AddComponent<TextMesh>();mesh.text=text;mesh.color=color;KoreanText.Fit(mesh,width,height);
 }
 // 규제표지 224 최고속도제한: red ring, white face, black figures (colours from the Korean sign colour table).
 static void LimitDisc(Transform g,float y,float z,float kmh,SignMaterials m){
  // A unit cylinder is 2 m tall, so depth .01 gives a 2 cm disc; the figures sit in front of the white face.
  Disc("규제표지 테두리",g,new Vector3(0,y,z),.9f,.01f,m.red);
  Disc("규제표지 바탕",g,new Vector3(0,y,z-.02f),.68f,.01f,m.white);
  Words(g,"제한 숫자",new Vector3(0,y,z-.045f),kmh.ToString("0"),new Color(.15f,.15f,.15f),.5f,.36f);
 }
 // 지시표지 324 어린이보호: blue square, white adult and child walking hand in hand.
 static void Person(Transform g,float x,float y,float scale,SignMaterials m){
  Disc("머리",g,new Vector3(x,y+.2f*scale,-.04f),.13f*scale,.01f,m.white);
  Plate("몸",g,new Vector3(x,y+.01f*scale,-.035f),new Vector3(.13f*scale,.26f*scale,.01f),m.white);
  var a=Plate("다리",g,new Vector3(x-.04f*scale,y-.2f*scale,-.035f),new Vector3(.05f*scale,.2f*scale,.01f),m.white);a.transform.localRotation=Quaternion.Euler(0,0,-12);
  var b=Plate("다리",g,new Vector3(x+.04f*scale,y-.2f*scale,-.035f),new Vector3(.05f*scale,.2f*scale,.01f),m.white);b.transform.localRotation=Quaternion.Euler(0,0,12);
 }
 static void ChildSign(Transform g,float y,SignMaterials m){
  Plate("어린이보호 바탕",g,new Vector3(0,y,-.01f),new Vector3(.82f,.82f,.03f),m.blue);
  Person(g,-.14f,y,1.15f,m);Person(g,.15f,y-.05f,.8f,m);
  var hands=Plate("손",g,new Vector3(0,y-.02f,-.035f),new Vector3(.2f,.035f,.01f),m.white);hands.transform.localRotation=Quaternion.Euler(0,0,-10);
 }
 // 보조표지: white plate with a black edge and black text, hung under the main sign.
 static void Supplement(Transform g,float y,string text,SignMaterials m){
  Plate("보조표지 테두리",g,new Vector3(0,y,-.005f),new Vector3(.9f,.32f,.02f),m.ink);
  Plate("보조표지",g,new Vector3(0,y,-.02f),new Vector3(.86f,.28f,.02f),m.white);
  Words(g,"보조표지 문구",new Vector3(0,y,-.035f),text,new Color(.15f,.15f,.15f),.78f,.22f);
 }
 static void Sign(Transform root,SpeedLimitSections.Mark mark,Vector3 at,SignMaterials m){
  bool end=mark.to<=0;float shown=end?mark.from:mark.to;bool school=mark.school;
  var g=new GameObject(end?"제한속도 해제 "+shown:"제한속도 "+shown+" 시작");g.transform.SetParent(root,false);
  // Local +z points along the driving direction; the face looks back at the approaching driver (local -z).
  g.transform.position=at;g.transform.rotation=Quaternion.LookRotation(mark.forward);var t=g.transform;
  float top=school&&!end?3.45f:2.8f;
  WorldBuilder.Box("지주",new Vector3(0,top*.5f,.06f),new Vector3(.08f,top,.08f),m.pole,t,false);
  if(end){
   // 해제표지 427: the cancelled sign on a white rectangle crossed by black diagonal hatching.
   float y=2.3f;
   Plate("해제표지 테두리",t,new Vector3(0,y,-.005f),new Vector3(.98f,.98f,.02f),m.ink);
   Plate("해제표지 바탕",t,new Vector3(0,y,-.02f),new Vector3(.92f,.92f,.02f),m.white);
   LimitDisc(t,y,-.05f,shown,m);
   foreach(float d in new[]{-.3f,0f,.3f}){float length=(1.30f-2*Mathf.Abs(d))*.95f;var bar=Plate("해제 빗금",t,new Vector3(d*.707f,y+d*.707f,-.11f),new Vector3(length,.045f,.01f),m.ink);bar.transform.localRotation=Quaternion.Euler(0,0,-45);}
   Supplement(t,1.62f,school?"보호구역 해제":"구간 끝",m);
  }else{
   if(school)ChildSign(t,3.0f,m);
   LimitDisc(t,2.1f,-.01f,shown,m);
   Supplement(t,1.5f,school?"어린이보호구역":"여기부터",m);
  }
 }
}
