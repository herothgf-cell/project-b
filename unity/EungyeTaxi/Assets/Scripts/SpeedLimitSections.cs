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
 public static void DrawMap(TrafficSystem traffic,System.Func<Vector3,Vector2> project,Rect clip,float width,bool discs){
  if(Event.current==null||Event.current.type!=EventType.Repaint)return;var s=For(traffic);if(s==null)return;
  var grown=new Rect(clip.x-20,clip.y-20,clip.width+40,clip.height+40);
  foreach(var seg in s.segments){var a=project(seg.a);var b=project(seg.b);if(!grown.Contains(a)&&!grown.Contains(b))continue;var c=ColorOf(seg);c.a=.8f;HudGlyph.MapLine(a,b,c,width,clip);}
  if(!discs)return;var placed=new List<Vector2>();
  foreach(var m in s.signs){if(m.to<=0)continue;var p=project(m.position);if(!clip.Contains(p))continue;bool near=false;foreach(var q in placed)if((q-p).sqrMagnitude<26*26){near=true;break;}if(near)continue;placed.Add(p);Disc(new Rect(p.x-11,p.y-11,22,22),m.to);}
 }
 static GUIStyle discText;
 public static void Disc(Rect r,float kmh){
  SpeedLimitBadge.DrawDisc(r,SchoolColor,Color.white);
  if(discText==null){discText=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold,padding=new RectOffset(0,0,0,0)};}
  discText.fontSize=Mathf.RoundToInt(r.height*.45f);discText.normal.textColor=new Color(.08f,.09f,.12f);GUI.Label(r,kmh.ToString("0"),discText);
 }
}

// Road-side signs and red school-zone paint for the sections above, plus a short notice when the limit changes.
public sealed class SpeedLimitSigns:MonoBehaviour {
 public DriveSession session;
 bool built;int attempts;public int SignCount {get;private set;}public int PaintQuads {get;private set;}
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
 void Build(SpeedLimitSections s){
  var root=new GameObject("제한속도 구간 표지").transform;root.SetParent(transform,false);
  var red=WorldBuilder.Mat("제한속도 표지 적색",new Color(.80f,.10f,.10f));var white=WorldBuilder.Mat("제한속도 표지 백색",new Color(.97f,.97f,.95f));var grey=WorldBuilder.Mat("제한속도 해제 회색",new Color(.45f,.47f,.50f));var pole=WorldBuilder.Mat("제한속도 지주",new Color(.55f,.57f,.60f));var ink=WorldBuilder.Mat("제한속도 해제 사선",new Color(.12f,.12f,.14f));
  foreach(var m in s.signs){Sign(root,m,red,white,grey,pole,ink);SignCount++;}
  // School-zone stretches get red pavement, the usual Korean marking, so the start and end are visible on the road.
  var verts=new List<Vector3>();var tris=new List<int>();
  foreach(var seg in s.segments){if(!seg.school)continue;var f=SpeedLimitSections.Flat(seg.b-seg.a);var side=SpeedLimitSections.Right(f)*1.5f;int i=verts.Count;
   verts.Add(Ground(seg.a-side));verts.Add(Ground(seg.a+side));verts.Add(Ground(seg.b+side));verts.Add(Ground(seg.b-side));// One face whose normal points up (two faces cancel in RecalculateNormals and paint black).
   bool up=Vector3.Cross(verts[i+1]-verts[i],verts[i+2]-verts[i]).y>0;tris.AddRange(up?new[]{i,i+1,i+2,i,i+2,i+3}:new[]{i,i+2,i+1,i,i+3,i+2});PaintQuads++;}
  if(verts.Count>0){var mesh=new Mesh{name="어린이보호구역 적색 포장",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
   var paint=new GameObject("어린이보호구역 적색 포장");paint.transform.SetParent(root,false);paint.AddComponent<MeshFilter>().sharedMesh=mesh;var mr=paint.AddComponent<MeshRenderer>();mr.sharedMaterial=WorldBuilder.Mat("어린이보호구역 포장",new Color(.66f,.20f,.17f));mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
 }
 // The lane line is the lane centre at graph height; the drawn road surface can sit a little higher. Paint just above what is there.
 static Vector3 Ground(Vector3 p){RaycastHit hit;if(Physics.Raycast(p+Vector3.up*2.5f,Vector3.down,out hit,5f,~(1<<8),QueryTriggerInteraction.Ignore)&&!hit.collider.attachedRigidbody)p.y=Mathf.Max(p.y,hit.point.y);return p+Vector3.up*.06f;}
 static GameObject Disc(string name,Transform parent,Vector3 local,float diameter,float depth,Material mat){
  var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=name;Destroy(g.GetComponent<Collider>());g.transform.SetParent(parent,false);g.transform.localPosition=local;g.transform.localRotation=Quaternion.Euler(90,0,0);g.transform.localScale=new Vector3(diameter,depth,diameter);g.GetComponent<Renderer>().sharedMaterial=mat;return g;
 }
 static void Sign(Transform root,SpeedLimitSections.Mark m,Material red,Material white,Material grey,Material pole,Material ink){
  bool end=m.to<=0;float shown=end?m.from:m.to;
  var g=new GameObject(end?"제한속도 해제 "+shown:"제한속도 "+shown+" 시작");g.transform.SetParent(root,false);
  // Local +z points along the driving direction; the face looks back at the approaching driver (local -z).
  g.transform.position=m.position+SpeedLimitSections.Right(m.forward)*3.1f;g.transform.rotation=Quaternion.LookRotation(m.forward);
  WorldBuilder.Box("지주",new Vector3(0,1.4f,.06f),new Vector3(.1f,2.8f,.1f),pole,g.transform,false);
  Disc("테두리",g.transform,new Vector3(0,2.75f,0),1.25f,.03f,end?grey:red);
  Disc("바탕",g.transform,new Vector3(0,2.75f,-.02f),.97f,.03f,white);
  var label=new GameObject("숫자");label.transform.SetParent(g.transform,false);label.transform.localPosition=new Vector3(0,2.75f,-.045f);var mesh=label.AddComponent<TextMesh>();mesh.text=shown.ToString("0");mesh.color=end?new Color(.35f,.37f,.40f):new Color(.08f,.08f,.10f);KoreanText.Fit(mesh,.72f,.5f);
  if(end){var bar=WorldBuilder.Box("해제 사선",new Vector3(0,2.75f,-.06f),new Vector3(1.18f,.09f,.01f),ink,g.transform,false);bar.transform.localRotation=Quaternion.Euler(0,0,45);}
  WorldBuilder.Box("보조판",new Vector3(0,1.9f,-.01f),new Vector3(1.25f,.42f,.03f),white,g.transform,false);
  var plate=new GameObject("보조판 문구");plate.transform.SetParent(g.transform,false);plate.transform.localPosition=new Vector3(0,1.9f,-.035f);var words=plate.AddComponent<TextMesh>();
  words.text=end?(m.school?"보호구역 끝":"여기까지"):(m.school?"어린이보호구역":"여기부터");words.color=end?new Color(.2f,.2f,.22f):m.school?new Color(.7f,.08f,.08f):new Color(.1f,.1f,.12f);KoreanText.Fit(words,1.12f,.3f);
 }
}
