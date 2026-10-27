using System.Linq;
using UnityEngine;
public sealed class RoadMapPreview : MonoBehaviour {
 Camera cameraView;RenderTexture texture;string version="";GameObject geometry;
 readonly System.Collections.Generic.List<Material> ownedMaterials=new System.Collections.Generic.List<Material>();
 readonly System.Collections.Generic.List<Mesh> ownedMeshes=new System.Collections.Generic.List<Mesh>();
 public RenderTexture Texture {get{return texture;}}
 public void Clear(){version="";if(geometry){geometry.SetActive(false);Destroy(geometry);}geometry=null;foreach(var mesh in ownedMeshes)if(mesh)Destroy(mesh);ownedMeshes.Clear();foreach(var material in ownedMaterials)if(material)Destroy(material);ownedMaterials.Clear();}
 Material PreviewMaterial(string name,Color color){var m=WorldBuilder.Mat(name,color);ownedMaterials.Add(m);return m;}
 public void Draw(RoadMapDocument d,Vector2 centre,float span,ProjectMapScene scene=null){
  string key=d.GetHashCode()+":"+d.id+":"+d.revision+":"+centre+":"+span;if(key==version)return;Clear();version=key;
  if(!cameraView){var c=new GameObject("편집 3D 카메라");c.transform.SetParent(transform);cameraView=c.AddComponent<Camera>();cameraView.enabled=false;cameraView.cullingMask=1<<30;cameraView.clearFlags=CameraClearFlags.SolidColor;cameraView.backgroundColor=new Color(.2f,.26f,.3f);texture=new RenderTexture(950,600,16);cameraView.targetTexture=texture;cameraView.farClipPlane=5000;}
  geometry=new GameObject("편집 미리보기");geometry.transform.SetParent(transform);
  var road=PreviewMaterial("Preview asphalt",new Color(.2f,.23f,.26f));var paint=PreviewMaterial("Preview lanes",new Color(.3f,.9f,.85f));
  var floor=PreviewMaterial("Preview ground",new Color(.2f,.29f,.21f));WorldBuilder.Box("지면",new Vector3(centre.x,-.2f,centre.y),new Vector3(span*3,.2f,span*3),floor,geometry.transform,false);
  if(scene!=null){
   var materials=new System.Collections.Generic.Dictionary<string,Material>();
   foreach(var o in scene.objects.Where(x=>x.enabled&&x.points!=null&&x.points.Length>1)){
    if(o.points.Max(p=>p.x)<centre.x-span||o.points.Min(p=>p.x)>centre.x+span||o.points.Max(p=>p.z)<centre.y-span||o.points.Min(p=>p.z)>centre.y+span)continue;
    Material material;if(!materials.TryGetValue(o.kind,out material)){material=PreviewMaterial("Preview "+o.kind,RoadMapEditor.SceneColor(o.kind));materials[o.kind]=material;}
    if(o.kind=="road"){for(int i=1;i<o.points.Length;i++)Segment(o.name,o.points[i-1].V(.02f),o.points[i].V(.02f),o.width,.05f,material);continue;}
    Polygon(o,material);
    if(o.kind=="parking"||o.kind=="restaurant"||o.kind=="rest"){var bay=WorldBuilder.Box("주차 / 서비스 "+o.name,new Vector3(o.serviceX,.15f,o.serviceZ),new Vector3(2.6f,.05f,5.5f),paint,geometry.transform,false);bay.transform.rotation=Quaternion.Euler(0,o.yaw,0);}
   }
  }
  foreach(var l in d.lanes.Where(l=>!l.disabled)){for(int i=1;i<l.points.Length;i++){var a=l.points[i-1].V(l.heights!=null&&i-1<l.heights.Length?l.heights[i-1]:0);var b=l.points[i].V(l.heights!=null&&i<l.heights.Length?l.heights[i]:0);if(Mathf.Max(a.x,b.x)<centre.x-span||Mathf.Min(a.x,b.x)>centre.x+span||Mathf.Max(a.z,b.z)<centre.y-span||Mathf.Min(a.z,b.z)>centre.y+span)continue;if(scene==null)Segment("차로 "+l.id,a,b,l.width,.08f,road);Segment("차로 중심",a+Vector3.up*.1f,b+Vector3.up*.1f,.12f,.025f,paint);}}
  RoadMapFacilities.Build(d,geometry.transform,null);foreach(Transform item in geometry.GetComponentsInChildren<Transform>())item.gameObject.layer=30;
  var target=new Vector3(centre.x,0,centre.y);cameraView.transform.position=target+new Vector3(0,span*.65f,-span*.55f);cameraView.transform.LookAt(target);cameraView.fieldOfView=58;cameraView.Render();
 }
 void Segment(string name,Vector3 a,Vector3 b,float width,float height,Material material){if((b-a).sqrMagnitude<.00001f)return;var obj=WorldBuilder.Box(name,(a+b)*.5f,new Vector3(width,height,Vector3.Distance(a,b)),material,geometry.transform,false);obj.transform.rotation=Quaternion.LookRotation(b-a);}
 void Polygon(ProjectMapObject o,Material material){
  int[] roof;try{roof=ProjectMapScene.Triangulate(o.points);}catch(System.ArgumentException){return;}
  float height=o.kind=="building"?Mathf.Max(.2f,o.height):Mathf.Max(.025f,o.height);
  var vertices=new System.Collections.Generic.List<Vector3>();var faces=new System.Collections.Generic.List<int>();
  foreach(var p in o.points)vertices.Add(p.V(height));
  for(int i=0;i<roof.Length;i+=3){int a=roof[i],b=roof[i+1],c=roof[i+2];faces.AddRange(Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]).y>=0?new[]{a,b,c}:new[]{c,b,a});}
  float area=0;for(int i=0;i<o.points.Length;i++){var a=o.points[i];var b=o.points[(i+1)%o.points.Length];area+=a.x*b.z-b.x*a.z;}
  if(height>.03f)for(int i=0;i<o.points.Length;i++){int j=(i+1)%o.points.Length,n=vertices.Count;vertices.AddRange(new[]{o.points[i].V(),o.points[j].V(),o.points[j].V(height),o.points[i].V(height)});faces.AddRange(area>0?new[]{n,n+2,n+1,n,n+3,n+2}:new[]{n,n+1,n+2,n,n+2,n+3});}
  var mesh=new Mesh{name="Preview polygon "+o.id};mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.SetVertices(vertices);mesh.SetTriangles(faces,0);mesh.RecalculateNormals();mesh.RecalculateBounds();ownedMeshes.Add(mesh);
  var obj=new GameObject(o.kind+" "+o.name);obj.transform.SetParent(geometry.transform);obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=material;
 }
 void OnDestroy(){Clear();if(texture){texture.Release();Destroy(texture);}}
}

public sealed class RoadMapLamp : MonoBehaviour {
 public TrafficSystem system;public RoadMapSignalProgram program;public string groupId;public Renderer red,yellow,green;Material off,onRed,onYellow,onGreen;
 public void Initialize(){off=WorldBuilder.Mat("Editor lamp off",new Color(.07f,.08f,.09f));onRed=WorldBuilder.Mat("Editor red",Color.red);onYellow=WorldBuilder.Mat("Editor yellow",Color.yellow);onGreen=WorldBuilder.Mat("Editor green",Color.green);foreach(var m in new[]{onRed,onYellow,onGreen}){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",m.color*2);}Update();}
 void Update(){if(!red)return;var color=program==null?SignalColor.Red:program.Color(groupId,system?system.Clock:0);red.sharedMaterial=color==SignalColor.Red?onRed:off;if(yellow)yellow.sharedMaterial=color==SignalColor.Yellow?onYellow:off;green.sharedMaterial=color==SignalColor.Green?onGreen:off;}
}
public static class RoadMapFacilities {
 public static void Build(RoadMapDocument d,Transform parent,TrafficSystem system){if(d==null)return;var white=WorldBuilder.Mat("Editor paint",Color.white);var yellow=WorldBuilder.Mat("Editor centre",Color.yellow);var dark=WorldBuilder.Mat("Editor housing",new Color(.05f,.06f,.08f));var blue=WorldBuilder.Mat("Editor sign",new Color(.04f,.2f,.6f));foreach(var f in d.facilities){var root=new GameObject("사용자 시설 "+f.id);root.transform.SetParent(parent,false);root.transform.SetPositionAndRotation(f.Position,Quaternion.Euler(0,f.yaw,0));root.transform.localScale=Vector3.one*f.scale;
   if(f.kind=="crosswalk"){if(system){var program=d.signals.FirstOrDefault(p=>p.junctionId==f.junctionId);if(program!=null&&program.groups.Any(g=>g.pedestrian&&g.crosswalkId==f.id)){var walker=new GameObject("사용자 횡단 보행자").AddComponent<RoadMapWalker>();walker.transform.SetParent(parent);walker.crossing=f;walker.program=program;walker.system=system;}}for(float x=-f.width*.5f+.3f;x<f.width*.5f;x+=.95f)WorldBuilder.Box("횡단보도",new Vector3(x,.083f,0),new Vector3(.5f,.015f,f.length),white,root.transform,false);continue;}if(f.kind=="stopLine"||f.kind=="line"||f.kind=="centreLine"){WorldBuilder.Box(f.kind,new Vector3(0,.085f,0),new Vector3(f.width,.02f,f.length),f.kind=="centreLine"?yellow:white,root.transform,false);continue;}
   // Match authored approach identity; a nearer opposite signal must not steal the board.
   bool mounted=false;
   if(f.kind=="sign"&&UturnMovement.IsCondition(f.type)&&system){
    var junction=system.junctions.FirstOrDefault(j=>j.name==f.junctionId);
    if(junction!=null)for(int i=0;i<junction.lamps.Count;i++){
     if(junction.lampAxes[i]!=f.axis||junction.lampSigns[i]!=f.sign)continue;
     var lamps=junction.lamps[i];if(lamps==null||lamps.Length==0||!lamps[0])continue;
     var signal=lamps[0].transform.parent;var housing=signal.Find("Signal housing");if(!housing)continue;
     root.transform.SetParent(signal,false);
     // Reserve the existing permissive/generic upper board, then add a gap.
     root.transform.SetPositionAndRotation(housing.position+Vector3.up*(1.15f+.575f+.6f*Mathf.Abs(f.scale)+.2f),signal.rotation);
     root.transform.localScale=Vector3.one*f.scale;mounted=true;break;
    }
   }
   if(!mounted)WorldBuilder.Box("지주",new Vector3(0,f.height*.5f,0),new Vector3(.12f,f.height,.12f),dark,root.transform,false);
   if(f.kind=="sign"){float boardHeight=mounted?0:f.height;WorldBuilder.Box("표지",new Vector3(0,boardHeight,0),new Vector3(f.width,1.2f,.12f),blue,root.transform,false);var label=new GameObject("표지 문구");label.transform.SetParent(root.transform,false);label.transform.localPosition=new Vector3(0,boardHeight,-.09f);var mesh=label.AddComponent<TextMesh>();mesh.text=(f.type=="제한속도"?f.speed.ToString("0")+" km/h":f.type)+"\n"+f.text;mesh.color=Color.white;KoreanText.Fit(mesh,f.width*.9f,1.05f);continue;}
   bool pedestrian=f.kind=="pedestrianSignal";WorldBuilder.Box("신호함",new Vector3(0,f.height,0),new Vector3(pedestrian?.65f:2.2f,pedestrian?1.2f:.7f,.3f),dark,root.transform,false);var display=root.AddComponent<RoadMapLamp>();display.system=system;display.program=d.signals.FirstOrDefault(p=>p.junctionId==f.junctionId);display.groupId=f.groupId;
   display.red=WorldBuilder.Box("적색",new Vector3(pedestrian?0:-.65f,f.height+(pedestrian?.27f:0),-.18f),new Vector3(.42f,.42f,.07f),dark,root.transform,false).GetComponent<Renderer>();if(!pedestrian)display.yellow=WorldBuilder.Box("황색",new Vector3(0,f.height,-.18f),new Vector3(.42f,.42f,.07f),dark,root.transform,false).GetComponent<Renderer>();display.green=WorldBuilder.Box("녹색",new Vector3(pedestrian?0:.65f,f.height-(pedestrian?.27f:0),-.18f),new Vector3(.42f,.42f,.07f),dark,root.transform,false).GetComponent<Renderer>();display.Initialize();var caption=new GameObject("제어 진행 안내");caption.transform.SetParent(root.transform,false);caption.transform.localPosition=new Vector3(0,f.height-.85f,-.18f);var captionMesh=caption.AddComponent<TextMesh>();captionMesh.text=f.text;captionMesh.color=Color.white;KoreanText.Fit(captionMesh,2.3f,.35f);
  }}
}
